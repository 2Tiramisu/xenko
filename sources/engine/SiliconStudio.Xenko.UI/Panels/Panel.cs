﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.UI.Controls;

namespace SiliconStudio.Paradox.UI.Panels
{
    /// <summary>
    /// Provides a base class for all Panel elements. Use Panel elements to position and arrange child objects Paradox applications.
    /// </summary>
    [DebuggerDisplay("Panel - Name={Name}")]
    public abstract class Panel : UIElement, IScrollAnchorInfo
    {
        /// <summary>
        /// The key to the ZIndex dependency property.
        /// </summary>
        public readonly static PropertyKey<int> ZIndexPropertyKey = new PropertyKey<int>("ZIndexKey", typeof(Panel), DefaultValueMetadata.Static(0), ObjectInvalidationMetadata.New<int>(PanelZSortedChildInvalidator));

        /// <summary>
        /// The key to the PanelArrangeMatrix dependency property. This property can be used by panels to arrange they children as they want.
        /// </summary>
        protected readonly static PropertyKey<Matrix> PanelArrangeMatrixPropertyKey = new PropertyKey<Matrix>("PanelArrangeMatrixKey", typeof(Panel), DefaultValueMetadata.Static(Matrix.Identity), ObjectInvalidationMetadata.New<Matrix>(InvalidateArrangeMatrix));

        private static void InvalidateArrangeMatrix(object propertyOwner, PropertyKey<Matrix> propertyKey, Matrix propertyOldValue)
        {
            var element = (UIElement)propertyOwner;
            var parentPanel = element.VisualParent as Panel;

            if(parentPanel!=null) // if the element is not added to a panel yet, the invalidation will occur during the add of the child
                parentPanel.childrenWithArrangeMatrixInvalidated.Add(element);
        }

        private readonly bool[] shouldAnchor = new bool[3];
        
        /// <summary>
        /// A comparer sorting the <see cref="Panel"/> children by increasing Z-Index.
        /// </summary>
        protected class PanelChildrenComparer : Comparer<UIElement>
        {
            public override int Compare(UIElement x, UIElement y)
            {
                if (x == y)
                    return 0;

                if (x == null)
                    return 1;

                if (y == null)
                    return -1;

                return x.DependencyProperties.Get(ZIndexPropertyKey) - y.DependencyProperties.Get(ZIndexPropertyKey);
            }
        }
        /// <summary>
        /// A instance of <see cref="PanelChildrenComparer"/> that can be used to sort panels children by increasing Z-Indices.
        /// </summary>
        protected readonly static PanelChildrenComparer PanelChildrenSorter = new PanelChildrenComparer();

        private readonly HashSet<UIElement> childrenWithArrangeMatrixInvalidated = new HashSet<UIElement>();
        private Matrix[] childrenArrangeWorldMatrix = new Matrix[2];

        /// <summary>
        /// Gets the <see cref="UIElementCollection"/> of child elements of this Panel.
        /// </summary>
        public UIElementCollection Children { get; private set; }

        /// <summary>
        /// Invalidation callback that sort panel children back after a modification of a child ZIndex.
        /// </summary>
        /// <param name="element">The element which had is ZIndex modified</param>
        /// <param name="key">The key of the modified property</param>
        /// <param name="oldValue">The value of the property before modification</param>
        private static void PanelZSortedChildInvalidator(object element, PropertyKey<int> key, int oldValue)
        {
            var uiElement = (UIElement)element;
            var parentAsPanel = uiElement.VisualParent as Panel;

            if(parentAsPanel == null)
                return;

            parentAsPanel.VisualChildrenCollection.Sort(PanelChildrenSorter);
        }

        /// <summary>
        /// Creates a new empty Panel.
        /// </summary>
        protected Panel()
        {
            // activate anchoring by default
            for (int i = 0; i < shouldAnchor.Length; i++)
                shouldAnchor[i] = true;

            Children = new UIElementCollection();
            Children.CollectionChanged += LogicalChildrenChanged;
        }

        /// <summary>
        /// Action to take when the Children collection is modified.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="trackingCollectionChangedEventArgs">Argument indicating what changed in the collection</param>
        protected void LogicalChildrenChanged(object sender, TrackingCollectionChangedEventArgs trackingCollectionChangedEventArgs)
        {
            var modifiedElement = (UIElement)trackingCollectionChangedEventArgs.Item;
            var elementIndex = trackingCollectionChangedEventArgs.Index;
            switch (trackingCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnLogicalChildAdded(modifiedElement, elementIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OnLogicalChildRemoved(modifiedElement, elementIndex);
                    break;
                default:
                    throw new NotImplementedException();
            }
            InvalidateMeasure();
        }

        /// <summary>
        /// Action to perform when a logical child is removed.
        /// </summary>
        /// <param name="oldElement">The element that has been removed</param>
        /// <param name="index">The index of the child removed in the collection</param>
        protected virtual void OnLogicalChildRemoved(UIElement oldElement, int index)
        {
            if (oldElement.Parent == null)
                throw new UIInternalException("The parent of the removed children UIElement not null");
            SetParent(oldElement, null);
            SetVisualParent(oldElement, null);
        }

        /// <summary>
        /// Action to perform when a logical child is added.
        /// </summary>
        /// <param name="newElement">The element that has been added</param>
        /// <param name="index">The index in the collection where the child has been added</param>
        protected virtual void OnLogicalChildAdded(UIElement newElement, int index)
        {
            if (newElement == null)
                throw new InvalidOperationException("Cannot add a null UIElement to the children list.");
            SetParent(newElement, this);
            SetVisualParent(newElement, this);
            VisualChildrenCollection.Sort(PanelChildrenSorter);
            if(Children.Count > childrenArrangeWorldMatrix.Length)
                childrenArrangeWorldMatrix = new Matrix[2*Children.Count];
        }

        protected override void UpdateWorldMatrix(ref Matrix parentWorldMatrix, bool parentWorldChanged)
        {
            var shouldUpdateAllChridrenMatrix = parentWorldChanged || ArrangeChanged || LocalMatrixChanged;

            base.UpdateWorldMatrix(ref parentWorldMatrix, parentWorldChanged);

            var childIndex = 0;
            foreach (var child in VisualChildrenCollection)
            {
                var shouldUpdateChildWorldMatrix = shouldUpdateAllChridrenMatrix || childrenWithArrangeMatrixInvalidated.Contains(child);
                {
                    var childMatrix = child.DependencyProperties.Get(PanelArrangeMatrixPropertyKey);
                    Matrix.Multiply(ref childMatrix, ref WorldMatrixInternal, out childrenArrangeWorldMatrix[childIndex]);
                }

                ((IUIElementUpdate)child).UpdateWorldMatrix(ref childrenArrangeWorldMatrix[childIndex], shouldUpdateChildWorldMatrix);

                ++childIndex;
            }
            childrenWithArrangeMatrixInvalidated.Clear();
        }

        /// <summary>
        /// Change the anchoring activation state of the given direction.
        /// </summary>
        /// <param name="direction">The direction in which activate or deactivate the anchoring</param>
        /// <param name="enable"><value>true</value> to enable anchoring, <value>false</value> to disable the anchoring</param>
        public void ActivateAnchoring(Orientation direction, bool enable)
        {
            shouldAnchor[(int)direction] = enable;
        }

        public virtual bool ShouldAnchor(Orientation direction)
        {
            return shouldAnchor[(int)direction];
        }

        public virtual Vector2 GetSurroudingAnchorDistances(Orientation direction, float position)
        {
            var maxPosition = RenderSize[(int)direction];
            var validPosition = Math.Max(0, Math.Min(position, maxPosition));

            return new Vector2(-validPosition, maxPosition - validPosition);
        }

        public ScrollViewer ScrollOwner { get; set; }
    }
}