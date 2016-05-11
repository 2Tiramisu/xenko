﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Diagnostics;

using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Xenko.UI.Panels
{
    /// <summary>
    /// Represents the base primitive for all the grid-like controls
    /// </summary>
    [DataContract(nameof(GridBase))]
    [DebuggerDisplay("GridBase - Name={Name}")]
    public abstract class GridBase : Panel
    {
        /// <summary>
        /// The key to the Column attached dependency property. This defines the column an item is inserted into.
        /// </summary>
        /// <remarks>First column has 0 as index</remarks>
        [DataMemberRange(0, int.MaxValue)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> ColumnPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(ColumnPropertyKey), typeof(GridBase), 0, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the Row attached dependency property. This defines the row an item is inserted into.
        /// </summary>
        /// <remarks>First row has 0 as index</remarks>
        [DataMemberRange(0, int.MaxValue)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> RowPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(RowPropertyKey), typeof(GridBase), 0, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the Layer attached dependency property. This defines the layer an item is inserted into.
        /// </summary>
        /// <remarks>First layer has 0 as index</remarks>
        [DataMemberRange(0, int.MaxValue)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> LayerPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(LayerPropertyKey), typeof(GridBase), 0, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the ColumnSpan attached dependency property. This defines the number of columns an item takes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value must be strictly positive</exception>
        [DataMemberRange(1, int.MaxValue)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> ColumnSpanPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(ColumnSpanPropertyKey), typeof(GridBase), 1, SpanValidator, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the RowSpan attached dependency property. This defines the number of rows an item takes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value must be strictly positive</exception>
        [DataMemberRange(1, int.MaxValue)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> RowSpanPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(RowSpanPropertyKey), typeof(GridBase), 1, SpanValidator, InvalidateParentGridMeasure);

        /// <summary>
        /// The key to the LayerSpan attached dependency property. This defines the number of layers an item takes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value must be strictly positive</exception>
        [DataMemberRange(1, int.MaxValue)]
        [Display(category: LayoutCategory)]
        public static readonly PropertyKey<int> LayerSpanPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(LayerSpanPropertyKey), typeof(GridBase), 1, SpanValidator, InvalidateParentGridMeasure);

        private static void InvalidateParentGridMeasure(object propertyowner, PropertyKey<int> propertykey, int propertyoldvalue)
        {
            var element = (UIElement)propertyowner;
            var parentGridBase = element.Parent as GridBase;

            parentGridBase?.InvalidateMeasure();
        }

        private static void SpanValidator(ref int value)
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>
        /// Get an element span values as an <see cref="Int3"/>.
        /// </summary>
        /// <param name="element">The element from which extract the span values</param>
        /// <returns>The span values of the element</returns>
        protected Int3 GetElementSpanValues(UIElement element)
        {
            return new Int3(
                element.DependencyProperties.Get(ColumnSpanPropertyKey),
                element.DependencyProperties.Get(RowSpanPropertyKey),
                element.DependencyProperties.Get(LayerSpanPropertyKey));
        }

        /// <summary>
        /// Get the positions of an element in the grid as an <see cref="Int3"/>.
        /// </summary>
        /// <param name="element">The element from which extract the position values</param>
        /// <returns>The position of the element</returns>
        protected Int3 GetElementGridPositions(UIElement element)
        {
            return new Int3(
                element.DependencyProperties.Get(ColumnPropertyKey),
                element.DependencyProperties.Get(RowPropertyKey),
                element.DependencyProperties.Get(LayerPropertyKey));
        }
        /// <summary>
        /// Get an element span values as an <see cref="Vector3"/>.
        /// </summary>
        /// <param name="element">The element from which extract the span values</param>
        /// <returns>The span values of the element</returns>
        protected Vector3 GetElementSpanValuesAsFloat(UIElement element)
        {
            var intValues = GetElementSpanValues(element);

            return new Vector3(intValues.X, intValues.Y, intValues.Z);
        }

        /// <summary>
        /// Get the positions of an element in the grid as an <see cref="Vector3"/>.
        /// </summary>
        /// <param name="element">The element from which extract the position values</param>
        /// <returns>The position of the element</returns>
        protected Vector3 GetElementGridPositionsAsFloat(UIElement element)
        {
            var intValues = GetElementGridPositions(element);

            return new Vector3(intValues.X, intValues.Y, intValues.Z);
        }
    }
}
