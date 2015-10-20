﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under MIT License. See LICENSE.md for details.
// Source: http://stackoverflow.com/questions/4968755/mono-cecil-call-generic-base-class-method-from-other-assembly
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.AssemblyProcessor
{
    public static class CecilExtensions
    {
        // Not sure why Cecil made ContainsGenericParameter internal, but let's work around it by reflection.
        private static readonly MethodInfo containsGenericParameterGetMethod = typeof(MemberReference).GetProperty("ContainsGenericParameter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetMethod;

        public static IEnumerable<TypeDefinition> EnumerateTypes(this AssemblyDefinition assembly)
        {
            foreach (var type in assembly.MainModule.Types)
            {
                yield return type;
                foreach (var nestedType in type.NestedTypes)
                {
                    yield return nestedType;
                }
            }
        }

        public static TypeReference MakeGenericType(this TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            if (arguments.Length == 0)
                return self;

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        public static FieldReference MakeGeneric(this FieldReference self, params TypeReference[] arguments)
        {
            if (arguments.Length == 0)
                return self;

            return new FieldReference(self.Name, self.FieldType, self.DeclaringType.MakeGenericType(arguments));
        }

        public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] arguments)
        {
            if (arguments.Length == 0)
                return self;

            var reference = new MethodReference(self.Name, self.ReturnType, self.DeclaringType.MakeGenericType(arguments))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var genericParameter in self.GenericParameters)
            {
                var genericParameterCopy = new GenericParameter(genericParameter.Name, reference)
                {
                    Attributes = genericParameter.Attributes,
                };
                reference.GenericParameters.Add(genericParameterCopy);
                foreach (var constraint in genericParameter.Constraints)
                    genericParameterCopy.Constraints.Add(constraint);
            }

            return reference;
        }

        public static MethodReference MakeGenericMethod(this MethodReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            var method = new GenericInstanceMethod(self);
            foreach(var argument in arguments)
                method.GenericArguments.Add(argument);
            return method;
        }

        public static TypeDefinition GetTypeResolved(this ModuleDefinition moduleDefinition, string typeName)
        {
            foreach (var exportedType in moduleDefinition.ExportedTypes)
            {
                if (exportedType.FullName == typeName)
                {
                    var typeDefinition = exportedType.Resolve();
                    return typeDefinition;
                }
            }

            return moduleDefinition.GetType(typeName);
        }

        public static TypeDefinition GetTypeResolved(this ModuleDefinition moduleDefinition, string @namespace, string typeName)
        {
            foreach (var exportedType in moduleDefinition.ExportedTypes)
            {
                if (exportedType.Namespace == @namespace && exportedType.Name == typeName)
                {
                    var typeDefinition = exportedType.Resolve();
                    return typeDefinition;
                }
            }

            return moduleDefinition.GetType(@namespace, typeName);
        }

        /// <summary>
        /// Finds the corlib assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Missing mscorlib.dll from assembly</exception>
        public static AssemblyDefinition FindCorlibAssembly(AssemblyDefinition assembly)
        {
            AssemblyNameReference corlibReference = null;
            
            // First, check current assemblies reference for the highest version of mscorlib referenced (if any)
            foreach (var assemblyNameReference in assembly.MainModule.AssemblyReferences)
            {
                if (assemblyNameReference.Name.ToLower() == "mscorlib"
                    && (corlibReference == null || assemblyNameReference.Version > corlibReference.Version))
                {
                    corlibReference = assemblyNameReference;
                }
            }

            // Use CoreLibrary (note: we want mscorlib, not System.Runtime)
            if (corlibReference == null)
                corlibReference = assembly.MainModule.TypeSystem.CoreLibrary as AssemblyNameReference;

            if (corlibReference == null || corlibReference.Name != "mscorlib")
                corlibReference = new AssemblyNameReference("mscorlib", new Version(4, 0, 0, 0));

            return assembly.MainModule.AssemblyResolver.Resolve(corlibReference);
        }

        /// <summary>
        /// Get Program Files x86
        /// </summary>
        /// <returns></returns>
        public static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        public static GenericInstanceType ChangeGenericInstanceType(this GenericInstanceType type, TypeReference elementType, IEnumerable<TypeReference> genericArguments)
        {
            if (elementType != type.ElementType || genericArguments != type.GenericArguments)
            {
                var result = new GenericInstanceType(elementType);
                foreach (var genericArgument in genericArguments)
                    result.GenericArguments.Add(genericArgument);
                if (type.HasGenericParameters)
                    SetGenericParameters(result, type.GenericParameters);
                return result;
            }
            return type;
        }

        public static ArrayType ChangeArrayType(this ArrayType type, TypeReference elementType, int rank)
        {
            if (elementType != type.ElementType || rank != type.Rank)
            {
                var result = new ArrayType(elementType, rank);
                if (type.HasGenericParameters)
                    SetGenericParameters(result, type.GenericParameters);
                return result;
            }
            return type;
        }

        public static TypeReference ChangeGenericParameters(this TypeReference type, IEnumerable<GenericParameter> genericParameters)
        {
            if (type.GenericParameters == genericParameters)
                return type;

            TypeReference result;
            var arrayType = type as ArrayType;
            if (arrayType != null)
            {
                result = new ArrayType(arrayType.ElementType, arrayType.Rank);
            }
            else
            {
                var genericInstanceType = type as GenericInstanceType;
                if (genericInstanceType != null)
                {
                    result = new GenericInstanceType(genericInstanceType.ElementType);
                }
                else if (type.GetType() == typeof(TypeReference).GetType())
                {
                    result = new TypeReference(type.Namespace, type.Name, type.Module, type.Scope, type.IsValueType);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            SetGenericParameters(result, genericParameters);

            return result;
        }

        /// <summary>
        /// Sometimes, TypeReference.IsValueType is not properly set (since it needs to load dependent assembly).
        /// THis do so when necessary.
        /// </summary>
        /// <param name="typeReference"></param>
        /// <returns></returns>
        public static TypeReference FixupValueType(TypeReference typeReference)
        {
            // Make sure IsValueType are properly set from resolved type (not encoded in CustomAttributes, but we depend on a valid value for some of the serializer/update engine codegen)
            switch (typeReference.MetadataType)
            {
                case MetadataType.Class:
                case MetadataType.GenericInstance:
                    var typeDefinition = typeReference.Resolve();
                    if (typeDefinition.IsValueType && !typeReference.IsValueType)
                        typeReference.IsValueType = typeDefinition.IsValueType;
                    break;
            }

            return typeReference;
        }

        private static void SetGenericParameters(TypeReference result, IEnumerable<GenericParameter> genericParameters)
        {
            foreach (var genericParameter in genericParameters)
                result.GenericParameters.Add(genericParameter);
        }

        public static string GenerateGenerics(this TypeReference type, bool empty = false)
        {
            var genericInstanceType = type as GenericInstanceType;
            if (!type.HasGenericParameters && genericInstanceType == null)
                return string.Empty;

            var result = new StringBuilder();

            // Try to process generic instantiations
            if (genericInstanceType != null)
            {
                result.Append("<");

                bool first = true;
                foreach (var genericArgument in genericInstanceType.GenericArguments)
                {
                    if (!first)
                        result.Append(",");
                    first = false;
                    if (!empty)
                        result.Append(ConvertCSharp(genericArgument, empty));
                }

                result.Append(">");

                return result.ToString();
            }

            if (type.HasGenericParameters)
            {
                result.Append("<");

                bool first = true;
                foreach (var genericParameter in type.GenericParameters)
                {
                    if (!first)
                        result.Append(",");
                    first = false;
                    if (!empty)
                        result.Append(ConvertCSharp(genericParameter, empty));
                }

                result.Append(">");

                return result.ToString();
            }

            return result.ToString();
        }

        public unsafe static string ConvertTypeId(this TypeReference type)
        {
            var typeName = type.ConvertCSharp(false);
            var typeId = ObjectId.FromBytes(Encoding.UTF8.GetBytes(typeName));

            var typeIdHash = (uint*)&typeId;
            return string.Format("new {0}(0x{1:x8}, 0x{2:x8}, 0x{3:x8}, 0x{4:x8})", typeof(ObjectId).FullName, typeIdHash[0], typeIdHash[1], typeIdHash[2], typeIdHash[3]);
        }

        /// <summary>
        /// Generates type name valid to use from C# source file.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="empty"></param>
        /// <returns></returns>
        public static string ConvertCSharp(this TypeReference type, bool empty = false)
        {
            // Try to process arrays
            var arrayType = type as ArrayType;
            if (arrayType != null)
            {
                return ConvertCSharp(arrayType.ElementType, empty) + "[]";
            }

            // Remove the `X at end of generic definition.
            var typeName = type.GetElementType().FullName;
            var genericSeparatorIndex = typeName.LastIndexOf('`');
            if (genericSeparatorIndex != -1)
                typeName = typeName.Substring(0, genericSeparatorIndex);

            // Replace / into . (nested types)
            typeName = typeName.Replace('/', '.');

            // Try to process generic instantiations
            var genericInstanceType = type as GenericInstanceType;
            if (genericInstanceType != null)
            {
                var result = new StringBuilder();

                // Use ElementType so that we have only the name without the <> part.
                result.Append(typeName);
                result.Append("<");

                bool first = true;
                foreach (var genericArgument in genericInstanceType.GenericArguments)
                {
                    if (!first)
                        result.Append(",");
                    first = false;
                    if (!empty)
                        result.Append(ConvertCSharp(genericArgument, empty));
                }

                result.Append(">");

                return result.ToString();
            }

            if (type.HasGenericParameters)
            {
                var result = new StringBuilder();

                // Use ElementType so that we have only the name without the <> part.
                result.Append(typeName);
                result.Append("<");

                bool first = true;
                foreach (var genericParameter in type.GenericParameters)
                {
                    if (!first)
                        result.Append(",");
                    first = false;
                    if (!empty)
                        result.Append(ConvertCSharp(genericParameter, empty));
                }

                result.Append(">");

                return result.ToString();
            }

            return typeName;
        }

        /// <summary>
        /// Generates the Mono.Cecil TypeReference from its .NET <see cref="Type"/> counterpart.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="assemblyResolver">The assembly resolver.</param>
        /// <returns></returns>
        public static TypeReference GenerateTypeCecil(this Type type, BaseAssemblyResolver assemblyResolver)
        {
            var assemblyDefinition = assemblyResolver.Resolve(type.Assembly.FullName);
            TypeReference typeReference;

            if (type.IsNested)
            {
                var declaringType = GenerateTypeCecil(type.DeclaringType, assemblyResolver);
                typeReference = declaringType.Resolve().NestedTypes.FirstOrDefault(x => x.Name == type.Name);
            }
            else if (type.IsArray)
            {
                var elementType = GenerateTypeCecil(type.GetElementType(), assemblyResolver);
                typeReference = new ArrayType(elementType, type.GetArrayRank());
            }
            else
            {
                typeReference = assemblyDefinition.MainModule.GetTypeResolved(type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName);
            }

            if (typeReference == null)
                throw new InvalidOperationException("Could not resolve cecil type.");

            if (type.IsGenericType)
            {
                var genericInstanceType = new GenericInstanceType(typeReference);
                foreach (var argType in type.GetGenericArguments())
                {
                    TypeReference argTypeReference;
                    if (argType.IsGenericParameter)
                    {
                        argTypeReference = new GenericParameter(argType.Name, typeReference);
                    }
                    else
                    {
                        argTypeReference = GenerateTypeCecil(argType, assemblyResolver);
                    }
                    genericInstanceType.GenericArguments.Add(argTypeReference);
                }

                typeReference = genericInstanceType;
            }

            return typeReference;
        }

        public static bool ContainsGenericParameter(this MemberReference memberReference)
        {
            return (bool)containsGenericParameterGetMethod.Invoke(memberReference, null);
        }

        /// <summary>
        /// Generates type name similar to Type.AssemblyQualifiedName.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ConvertAssemblyQualifiedName(this TypeReference type)
        {
            var result = new StringBuilder(256);
            ConvertAssemblyQualifiedName(type, result);
            return result.ToString();
        }

        private static void ConvertAssemblyQualifiedName(this TypeReference type, StringBuilder result)
        {
            int start, end;

            var arrayType = type as ArrayType;
            if (arrayType != null)
            {
                // If it's an array, process element type, and add [] after
                type = arrayType.ElementType;
            }

            // Add FUllName from GetElementType() (remove generics etc...)
            start = result.Length;
            result.Append(type.GetElementType().FullName);
            end = result.Length;

            // Replace / into + (nested types)
            result = result.Replace('/', '+', start, end);

            // Try to process generic instantiations
            var genericInstanceType = type as GenericInstanceType;
            if (genericInstanceType != null)
            {
                // Ideally we would like to have access to Mono.Cecil TypeReference.ContainsGenericParameter, but it's internal.
                // This doesn't cover every case but hopefully this should be enough for serialization
                bool containsGenericParameter = false;
                foreach (var genericArgument in genericInstanceType.GenericArguments)
                {
                    if (genericArgument.IsGenericParameter)
                        containsGenericParameter = true;
                }

                if (!containsGenericParameter)
                {
                    // Use ElementType so that we have only the name without the <> part.
                    result.Append('[');

                    bool first = true;
                    foreach (var genericArgument in genericInstanceType.GenericArguments)
                    {
                        if (!first)
                            result.Append(",");
                        result.Append('[');
                        first = false;
                        result.Append(ConvertAssemblyQualifiedName(genericArgument));
                        result.Append(']');
                    }

                    result.Append(']');
                }
            }

            // Try to process arrays
            if (arrayType != null)
            {
                result.Append('[');
                if (arrayType.Rank > 1)
                    result.Append(',', arrayType.Rank - 1);
                result.Append(']');
            }

            result.Append(", ");
            start = result.Length;
            result.Append(type.Module.Assembly.FullName);
            end = result.Length;

#if SILICONSTUDIO_PLATFORM_MONO_MOBILE
            // Xamarin iOS and Android remap some assemblies
            const string oldTypeEnding = "2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
            const string newTypeEnding = "4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            result = result.Replace(oldTypeEnding, newTypeEnding, start, end);
#endif
        }
    }
}