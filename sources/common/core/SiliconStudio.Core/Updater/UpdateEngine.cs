using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Core.Updater
{
    public static unsafe class UpdateEngine
    {
        static readonly Dictionary<UpdateKey, UpdatableMember> UpdateKeys = new Dictionary<UpdateKey, UpdatableMember>();
        static readonly Dictionary<Type, UpdateMemberResolver> MemberResolvers = new Dictionary<Type, UpdateMemberResolver>();

        public static void RegisterMember(Type owner, string name, UpdatableMember updatableMember)
        {
            UpdateKeys[new UpdateKey(owner, name)] = updatableMember;
        }

        public static void RegisterMemberResolver(UpdateMemberResolver resolver)
        {
            MemberResolvers[resolver.SupportedType] = resolver;
        }

        struct AnimationBuilderStackEntry
        {
            public Type Type;

            // String
            public int StartIndex;
            public int EndIndex;

            public int ObjectStartOffset;

            public UpdatableMember Member;
            public UpdateOperationType LeaveOperation;
            public int LeaveOffset;

            public AnimationBuilderStackEntry(Type type, int startIndex, int endIndex)
            {
                Type = type;
                StartIndex = startIndex;
                EndIndex = endIndex;

                ObjectStartOffset = 0;

                Member = null;
                LeaveOperation = UpdateOperationType.Invalid;
                LeaveOffset = 0;
            }
        }

        private const char PathDelimiter = '.';
        private const char PathIndexerOpen = '[';
        private const char PathIndexerClose = ']';
        private const char PathCastOpen = '(';
        private const char PathCastClose = ')';
        private static readonly char[] PathGroupDelimiters = new[] { PathDelimiter, PathIndexerOpen };

        struct ComputeUpdateOperationState
        {
            public List<UpdateOperation> UpdateOperations;
            public List<AnimationBuilderStackEntry> StackPath;
            public int NewOffset;
            public int PreviousOffset;
            public int ParseElementStart;
            public int ParseElementEnd;
        }

        /// <summary>
        /// Compiles a list of update operations into a <see cref="CompiledUpdate"/>, for use with <see cref="Run"/>.
        /// </summary>
        /// <param name="rootObjectType">The type of the root object.</param>
        /// <param name="animationPaths">The different paths and source offsets to use when <see cref="Run"/> is applied.</param>
        /// <returns>A <see cref="CompiledUpdate"/> object that can be used for <see cref="Run"/>.</returns>
        public static CompiledUpdate Compile(Type rootObjectType, List<UpdateMemberInfo> animationPaths)
        {
            var currentPath = string.Empty;
            var temporaryObjectsList = new List<object>();

            var state = new ComputeUpdateOperationState();
            state.UpdateOperations = new List<UpdateOperation>();
            state.StackPath = new List<AnimationBuilderStackEntry>
            {
                new AnimationBuilderStackEntry(rootObjectType, 0, 0),
            };

            foreach (var animationPath in animationPaths)
            {
                var commonPathParts = 1;

                // Detect how much of the path is unmodified (note: we start from 1 because root always stay there)
                for (int index = 1; index < state.StackPath.Count; index++)
                {
                    var pathPart = state.StackPath[index];

                    // Check if next path part is the same (first check length then content)
                    if (((animationPath.Name.Length == pathPart.EndIndex) ||
                         (animationPath.Name.Length > pathPart.EndIndex && (animationPath.Name[pathPart.EndIndex] == PathDelimiter || animationPath.Name[pathPart.EndIndex] == PathIndexerOpen)))
                        && string.Compare(animationPath.Name, pathPart.StartIndex, currentPath, pathPart.StartIndex, pathPart.EndIndex - pathPart.StartIndex, StringComparison.Ordinal) == 0)
                    {
                        commonPathParts++;
                        continue;
                    }

                    break;
                }

                PopObjects(ref state, commonPathParts);

                // Parse the new path parts
                state.ParseElementStart = state.StackPath.Last().EndIndex;

                // Compute offset from start of current object
                state.NewOffset = state.StackPath.Last().ObjectStartOffset;

                while (state.ParseElementStart < animationPath.Name.Length)
                {
                    var containerType = state.StackPath.Last().Type;

                    // We have only two cases for now: array or property/field name
                    bool isIndexerAccess = animationPath.Name[state.ParseElementStart] == PathIndexerOpen;
                    if (isIndexerAccess)
                    {
                        // Parse until end of indexer
                        state.ParseElementEnd = animationPath.Name.IndexOf(PathIndexerClose, state.ParseElementStart + 1);
                        if (state.ParseElementEnd == -1)
                            throw new InvalidOperationException("Property path parse error: could not find indexer end ']'");

                        // Include the indexer close
                        state.ParseElementEnd++;

                        // Parse integer
                        // TODO: Avoid substring?
                        var indexerString = animationPath.Name.Substring(state.ParseElementStart + 1, state.ParseElementEnd - state.ParseElementStart - 2);

                        // T[], IList<T>, etc...
                        // Try to resolve using custom resolver
                        UpdatableMember updatableMember = null;
                        var parentType = containerType;
                        while (parentType != null)
                        {
                            UpdateMemberResolver resolver;
                            if (MemberResolvers.TryGetValue(parentType, out resolver))
                            {
                                updatableMember = resolver.ResolveIndexer(indexerString);
                                if (updatableMember != null)
                                    break;
                            }

                            parentType = parentType.GetTypeInfo().BaseType;
                        }

                        // Try interfaces
                        if (updatableMember == null)
                        {
                            foreach (var implementedInterface in containerType.GetTypeInfo().ImplementedInterfaces)
                            {
                                UpdateMemberResolver resolver;
                                if (MemberResolvers.TryGetValue(implementedInterface, out resolver))
                                {
                                    updatableMember = resolver.ResolveIndexer(indexerString);
                                    if (updatableMember != null)
                                        break;
                                }
                            }
                        }

                        if (updatableMember == null)
                        {
                            throw new InvalidOperationException(string.Format("Property path parse error: could not find binding info for index {0} in type {1}", indexerString, containerType));
                        }

                        ProcessMember(ref state, animationPath, updatableMember, temporaryObjectsList);
                    }
                    else
                    {
                        // Note: first character might be a . delimiter, if so, skip it
                        var propertyStart = state.ParseElementStart;
                        if (animationPath.Name[propertyStart] == PathDelimiter)
                            propertyStart++;

                        // Check if it started with a parenthesis (to perform a cast operation)
                        if (animationPath.Name[propertyStart] == PathCastOpen)
                        {
                            // Parse until end of cast operation
                            state.ParseElementEnd = animationPath.Name.IndexOf(PathCastClose, ++propertyStart);
                            if (state.ParseElementEnd == -1)
                                throw new InvalidOperationException("Property path parse error: could not find cast operation ending ')'");

                            var typeName = animationPath.Name.Substring(propertyStart, state.ParseElementEnd - propertyStart);

                            // Include the indexer close
                            state.ParseElementEnd++;

                            var type = AssemblyRegistry.GetType(typeName);
                            if (type == null)
                            {
                                throw new InvalidOperationException($"Could not resolve type {typeName}");
                            }

                            // Push entry with new type
                            state.StackPath.Add(new AnimationBuilderStackEntry(type, state.ParseElementStart, state.ParseElementEnd)
                            {
                                ObjectStartOffset = state.NewOffset,
                            });
                        }
                        else
                        {
                            UpdatableMember updatableMember;

                            // Parse until end next group (or end)
                            state.ParseElementEnd = animationPath.Name.IndexOfAny(PathGroupDelimiters, state.ParseElementStart + 1);
                            if (state.ParseElementEnd == -1)
                                state.ParseElementEnd = animationPath.Name.Length;

                            // TODO: Avoid substring?
                            var propertyName = animationPath.Name.Substring(propertyStart, state.ParseElementEnd - propertyStart);
                            if (!UpdateKeys.TryGetValue(new UpdateKey(containerType, propertyName), out updatableMember))
                            {
                                // Try to resolve using custom resolver
                                var parentType = containerType;
                                while (parentType != null)
                                {
                                    UpdateMemberResolver resolver;
                                    if (MemberResolvers.TryGetValue(parentType, out resolver))
                                    {
                                        updatableMember = resolver.ResolveProperty(propertyName);
                                        if (updatableMember != null)
                                            break;
                                    }

                                    parentType = parentType.GetTypeInfo().BaseType;
                                }

                                if (updatableMember == null)
                                {
                                    throw new InvalidOperationException(string.Format("Property path parse error: could not find binding info for member {0} in type {1}", propertyName, containerType));
                                }
                            }

                            // Process member
                            ProcessMember(ref state, animationPath, updatableMember, temporaryObjectsList);
                        }
                    }

                    state.ParseElementStart = state.ParseElementEnd;
                }

                currentPath = animationPath.Name;
            }

            // Totally pop the stack (we might still have stuff to copy back into properties
            PopObjects(ref state, 0);

            return new CompiledUpdate
            {
                TemporaryObjects = temporaryObjectsList.ToArray(),
                UpdateOperations = state.UpdateOperations.ToArray(),
            };
        }

        private static void PopObjects(ref ComputeUpdateOperationState state, int desiredStackSize)
        {
            // Leave the objects that are not part of the path anymore
            while (state.StackPath.Count > desiredStackSize)
            {
                // Pop entry
                var stackPathPart = state.StackPath.Last();
                state.StackPath.RemoveAt(state.StackPath.Count - 1);

                // Perform any necessary exit action
                if (stackPathPart.LeaveOperation != UpdateOperationType.Invalid)
                {
                    state.UpdateOperations.Add(new UpdateOperation
                    {
                        Type = stackPathPart.LeaveOperation,
                        Member = stackPathPart.Member,
                    });

                    // We execute a leave operation, previous stack will be restored
                    state.PreviousOffset = stackPathPart.LeaveOffset;
                }
            }
        }

        private static void ProcessMember(ref ComputeUpdateOperationState state, UpdateMemberInfo animationPath, UpdatableMember updatableMember, List<object> temporaryObjectsList)
        {
            int leaveOffset = 0;
            var leaveOperation = UpdateOperationType.Invalid;

            var updatableField = updatableMember as UpdatableField;
            if (updatableField != null)
            {
                // Apply field offset
                state.NewOffset += updatableField.Offset;

                if (state.ParseElementEnd == animationPath.Name.Length)
                {
                    // Leaf node, perform the set operation
                    state.UpdateOperations.Add(new UpdateOperation
                    {
                        Type = updatableField.GetSetOperationType(),
                        Member = updatableField,
                        AdjustOffset = state.NewOffset - state.PreviousOffset,
                        DataOffset = animationPath.DataOffset,
                    });
                    state.PreviousOffset = state.NewOffset;
                }
                else if (!updatableField.MemberType.GetTypeInfo().IsValueType)
                {
                    // Only in case of objects we need to enter into them
                    state.UpdateOperations.Add(new UpdateOperation
                    {
                        Type = UpdateOperationType.EnterObjectField,
                        Member = updatableField,
                        AdjustOffset = state.NewOffset - state.PreviousOffset,
                    });
                    leaveOperation = UpdateOperationType.Leave;
                    leaveOffset = state.NewOffset;
                    state.PreviousOffset = state.NewOffset = 0;
                }
            }
            else
            {
                var updatableProperty = updatableMember as UpdatablePropertyBase;
                if (updatableProperty != null)
                {
                    if (state.ParseElementEnd == animationPath.Name.Length)
                    {
                        // Leaf node, perform the set the value
                        state.UpdateOperations.Add(new UpdateOperation
                        {
                            Type = updatableProperty.GetSetOperationType(),
                            Member = updatableProperty,
                            AdjustOffset = state.NewOffset - state.PreviousOffset,
                            DataOffset = animationPath.DataOffset,
                        });
                        state.PreviousOffset = state.NewOffset;
                    }
                    else
                    {
                        // Otherwise enter into the property
                        bool isStruct = updatableProperty.MemberType.GetTypeInfo().IsValueType;
                        int temporaryObjectIndex = -1;

                        if (isStruct)
                        {
                            // Struct properties need a storage area so that we can later set the updated value back into the property
                            leaveOperation = UpdateOperationType.LeaveAndCopyStructPropertyBase;
                            temporaryObjectIndex = temporaryObjectsList.Count;
                            temporaryObjectsList.Add(Activator.CreateInstance(updatableProperty.MemberType));
                        }
                        else
                        {
                            leaveOperation = UpdateOperationType.Leave;
                        }

                        state.UpdateOperations.Add(new UpdateOperation
                        {
                            Type = updatableProperty.GetEnterOperationType(),
                            Member = updatableProperty,
                            AdjustOffset = state.NewOffset - state.PreviousOffset,
                            DataOffset = temporaryObjectIndex,
                        });

                        leaveOffset = state.NewOffset;
                        state.PreviousOffset = state.NewOffset = 0;
                    }
                }
            }

            // No need to add the last part of the path, as we rarely set and then enter (and if we do we need to reevaluate updated value anyway)
            if (state.ParseElementEnd < animationPath.Name.Length)
            {
                state.StackPath.Add(new AnimationBuilderStackEntry(updatableMember.MemberType, state.ParseElementStart, state.ParseElementEnd)
                {
                    Member = updatableMember,
                    LeaveOperation = leaveOperation,
                    LeaveOffset = leaveOffset,
                    ObjectStartOffset = state.NewOffset
                });
            }
        }

        /// <summary>
        /// Updates the specified <see cref="target"/> object with new data.
        /// </summary>
        /// <param name="target">The object to update.</param>
        /// <param name="compiledUpdate">The precompiled list of update operations, generated by <see cref="Compile"/>.</param>
        /// <param name="updateData">The data source for blittable struct.</param>
        /// <param name="updateObjects">The data source for objects and non-blittable struct</param>
        public static void Run(object target, CompiledUpdate compiledUpdate, IntPtr updateData, UpdateObjectData[] updateObjects)
        {
            var operations = compiledUpdate.UpdateOperations;
            var temporaryObjects = compiledUpdate.TemporaryObjects;

            var stack = new Stack<UpdateStackEntry>();

            // Current object being processed
            object currentObj = target;

            // This object needs to be pinned since we will have a pointer to its memory
            // Note that the stack don't need to have each of its object pinned since we store entries as object + offset
            Interop.Pin(currentObj);

            // pinned test (this will need to be on a stack somehow)
            IntPtr currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj);

            var operationCount = operations.Length;
            var operation = Interop.Pin(ref operations[0]);
            for (int index = 0; index < operationCount; index++)
            {
                // Adjust offset
                currentPtr += operation.AdjustOffset;

                switch (operation.Type)
                {
                    case UpdateOperationType.EnterObjectProperty:
                    {
                        // Compute offset and push to stack
                        stack.Push(new UpdateStackEntry
                        {
                            Object = currentObj,
                            Offset = (int) ((byte*) currentPtr - (byte*) UpdateEngineHelper.ObjectToPtr(currentObj))
                        });

                        // Get object
                        currentObj = ((UpdatableProperty)operation.Member).GetObject(currentPtr);
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj);
                        break;
                    }
                    case UpdateOperationType.EnterStructPropertyBase:
                    {
                        // Compute offset and push to stack
                        stack.Push(new UpdateStackEntry
                        {
                            Object = currentObj,
                            Offset = (int) ((byte*) currentPtr - (byte*) UpdateEngineHelper.ObjectToPtr(currentObj))
                        });

                        currentObj = temporaryObjects[operation.DataOffset];
                        currentPtr = ((UpdatablePropertyBase)operation.Member).GetStructAndUnbox(currentPtr, currentObj);

                        break;
                    }
                    case UpdateOperationType.EnterObjectField:
                    {
                        // Compute offset and push to stack
                        stack.Push(new UpdateStackEntry
                        {
                            Object = currentObj,
                            Offset = (int) ((byte*) currentPtr - (byte*) UpdateEngineHelper.ObjectToPtr(currentObj))
                        });

                        // Get object
                        currentObj = ((UpdatableField)operation.Member).GetObject(currentPtr);
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj);
                        break;
                    }
                    case UpdateOperationType.EnterObjectCustom:
                    {
                        // Compute offset and push to stack
                        stack.Push(new UpdateStackEntry
                        {
                            Object = currentObj,
                            Offset = (int)((byte*)currentPtr - (byte*)UpdateEngineHelper.ObjectToPtr(currentObj))
                        });

                        // Get object
                        currentObj = ((UpdatableCustomAccessor)operation.Member).GetObject(currentPtr);
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj);
                        break;
                    }

                    case UpdateOperationType.LeaveAndCopyStructPropertyBase:
                    {
                        // Save back struct pointer
                        var oldPtr = currentPtr;

                        // Restore currentObj and currentPtr from stack
                        var stackEntry = stack.Pop();
                        currentObj = stackEntry.Object;
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj) + stackEntry.Offset;

                        // Use setter to set back struct
                        ((UpdatablePropertyBase)operation.Member).SetBlittable(currentPtr, oldPtr);

                        break;
                    }
                    case UpdateOperationType.Leave:
                    {
                        // Restore currentObj and currentPtr from stack
                        var stackEntry = stack.Pop();
                        currentObj = stackEntry.Object;
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj) + stackEntry.Offset;
                        break;
                    }
                    case UpdateOperationType.ConditionalSetObjectProperty:
                    {
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatableProperty)operation.Member).SetObject(currentPtr, updateObject.Value);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittablePropertyBase:
                    {
                        // TODO: This case can happen quite often (i.e. a float property) and require an extra indirection
                        // We could probably avoid it by having common types as non virtual methods (i.e. object, int, float, maybe even Vector3/4?)
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                            ((UpdatablePropertyBase)operation.Member).SetBlittable(currentPtr, (IntPtr)data);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetStructPropertyBase:
                    {
                        // TODO: This case can happen quite often (i.e. a float property) and require an extra indirection
                        // We could probably avoid it by having common types as non virtual methods (i.e. object, int, float, maybe even Vector3/4?)
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatablePropertyBase)operation.Member).SetStruct(currentPtr, updateObject.Value);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetObjectField:
                    {
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatableField)operation.Member).SetObject(currentPtr, updateObject.Value);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                            ((UpdatableField)operation.Member).SetBlittable(currentPtr, (IntPtr)data);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField4:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                        {
                            *(int*)currentPtr = *data;
                        }
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField8:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                        {
                            *(Blittable8*)currentPtr = *(Blittable8*)data;
                        }
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField12:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                        {
                            *(Blittable12*)currentPtr = *(Blittable12*)data;
                        }
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField16:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                        {
                            *(Blittable16*)currentPtr = *(Blittable16*)data;
                        }
                        break;
                    }
                    case UpdateOperationType.ConditionalSetStructField:
                    {
                        // Use setter to set back struct
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatableField)operation.Member).SetStruct(currentPtr, updateObject.Value);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetObjectCustom:
                    {
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatableCustomAccessor)operation.Member).SetObject(currentPtr, updateObject.Value);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                operation = Interop.IncrementPinned(operation);
            }
        }

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        struct Blittable8
        {
        }

        [StructLayout(LayoutKind.Sequential, Size = 12)]
        struct Blittable12
        {
        }

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct Blittable16
        {
        }

        struct UpdateKey
        {
            public readonly Type Owner;
            public readonly string Name;

            public UpdateKey(Type owner, string name)
            {
                Owner = owner;
                Name = name;
            }

            public override string ToString()
            {
                return $"{Owner.Name}.{Name}";
            }
        }

        struct UpdateStackEntry
        {
            public object Object;
            public int Offset;

            public UpdateStackEntry(object o, int offset)
            {
                Object = o;
                Offset = offset;
            }
        }
    }
}