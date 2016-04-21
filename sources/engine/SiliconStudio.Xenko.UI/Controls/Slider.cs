﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Input;
using SiliconStudio.Xenko.UI.Events;

namespace SiliconStudio.Xenko.UI.Controls
{
    /// <summary>
    /// Represents a slider element.
    /// </summary>
    [DataContract(nameof(Slider))]
    public class Slider : UIElement
    {
        private float value;

        private bool shouldSnapToTicks;

        private Orientation orientation;

        /// <summary>
        /// The key to the TrackBackgroundImage dependency property.
        /// </summary>
        public static readonly PropertyKey<Sprite> TrackBackgroundImagePropertyKey = new PropertyKey<Sprite>(nameof(TrackBackgroundImagePropertyKey), typeof(Slider), DefaultValueMetadata.Static<Sprite>(null), ObjectInvalidationMetadata.New<Sprite>(InvalidateTrackBackground));

        /// <summary>
        /// The key to the TrackForegroundImage dependency property.
        /// </summary>
        public static readonly PropertyKey<Sprite> TrackForegroundImagePropertyKey = new PropertyKey<Sprite>(nameof(TrackForegroundImagePropertyKey), typeof(Slider), DefaultValueMetadata.Static<Sprite>(null));

        /// <summary>
        /// The key to the ThumbImage dependency property.
        /// </summary>
        public static readonly PropertyKey<Sprite> ThumbImagePropertyKey = new PropertyKey<Sprite>(nameof(ThumbImagePropertyKey), typeof(Slider), DefaultValueMetadata.Static<Sprite>(null));

        /// <summary>
        /// The key to the TickImage dependency property.
        /// </summary>
        public static readonly PropertyKey<Sprite> TickImagePropertyKey = new PropertyKey<Sprite>(nameof(TickImagePropertyKey), typeof(Slider), DefaultValueMetadata.Static<Sprite>(null));

        /// <summary>
        /// The key to the MouseOverThumbImage dependency property.
        /// </summary>
        public static readonly PropertyKey<Sprite> MouseOverThumbImagePropertyKey = new PropertyKey<Sprite>(nameof(MouseOverThumbImagePropertyKey), typeof(Slider), DefaultValueMetadata.Static<Sprite>(null));

        /// <summary>
        /// The key to the Minimum dependency property.
        /// </summary>
        public static readonly PropertyKey<float> MinimumPropertyKey = new PropertyKey<float>(nameof(MinimumPropertyKey), typeof(Slider), DefaultValueMetadata.Static<float>(0), ObjectInvalidationMetadata.New<float>(ValidateExtremum));

        /// <summary>
        /// The key to the Maximum dependency property.
        /// </summary>
        public static readonly PropertyKey<float> MaximumPropertyKey = new PropertyKey<float>(nameof(MaximumPropertyKey), typeof(Slider), DefaultValueMetadata.Static<float>(1), ObjectInvalidationMetadata.New<float>(ValidateExtremum));

        /// <summary>
        /// The key to the Step dependency property.
        /// </summary>
        public static readonly PropertyKey<float> StepPropertyKey = new PropertyKey<float>(nameof(StepPropertyKey), typeof(Slider), DefaultValueMetadata.Static(0.1f));

        /// <summary>
        /// The key to the TickFrequency dependency property.
        /// </summary>
        public static readonly PropertyKey<float> TickFrequencyPropertyKey = new PropertyKey<float>(nameof(TickFrequencyPropertyKey), typeof(Slider), DefaultValueMetadata.Static(10f), ObjectInvalidationMetadata.New<float>(TickFrequencyInvalidated));

        /// <summary>
        /// The key to the TickFrequency dependency property.
        /// </summary>
        public static readonly PropertyKey<float> TickOffsetPropertyKey = new PropertyKey<float>(nameof(TickOffsetPropertyKey), typeof(Slider), DefaultValueMetadata.Static(10f));

        /// <summary>
        /// The key to the TrackStartingOffsets dependency property.
        /// </summary>
        public static readonly PropertyKey<Vector2> TrackStartingOffsetsrPropertyKey = new PropertyKey<Vector2>(nameof(TrackStartingOffsetsrPropertyKey), typeof(Slider), DefaultValueMetadata.Static(new Vector2()));

        private static void InvalidateTrackBackground(object propertyowner, PropertyKey<Sprite> propertykey, Sprite propertyoldvalue)
        {
            var slider = (Slider)propertyowner;

            slider.InvalidateMeasure();

            if (propertyoldvalue != null)
                propertyoldvalue.SizeChanged -= slider.OnSizeChanged;

            if(slider.TrackBackgroundImage != null)
                slider.TrackBackgroundImage.SizeChanged += slider.OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            InvalidateMeasure();
        }

        private static void ValidateExtremum(object propertyowner, PropertyKey<float> propertykey, float propertyoldvalue)
        {
            var slider = (Slider)propertyowner;

            if (slider.Maximum < slider.Minimum)
            {
                slider.DependencyProperties.Set(propertykey, propertyoldvalue);

                // ReSharper disable once NotResolvedInText
                throw new ArgumentOutOfRangeException("Maximum should be greater or equal than Minimum.");
            }
        }

        private static void TickFrequencyInvalidated(object propertyowner, PropertyKey<float> propertykey, float propertyoldvalue)
        {
            var slider = (Slider)propertyowner;

            if (slider.TickFrequency < 1)
                slider.TickFrequency = 1;

            slider.Value = slider.value; // snap to tick if enabled
        }

        static Slider()
        {
            EventManager.RegisterClassHandler(typeof(Slider), ValueChangedEvent, ValueChangedClassHandler);
        }

        /// <summary>
        /// Create a new instance of slider.
        /// </summary>
        public Slider()
        {
            CanBeHitByUser = true;
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            DrawLayerNumber += 4; // track background, track foreground, ticks, thumb
        }

        /// <summary>
        /// Gets or sets the image to display as Track background.
        /// </summary>
        [DataMemberIgnore]
        public Sprite TrackBackgroundImage
        {
            get { return DependencyProperties.Get(TrackBackgroundImagePropertyKey); }
            set { DependencyProperties.Set(TrackBackgroundImagePropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the image to display as Track foreground.
        /// </summary>
        [DataMemberIgnore]
        public Sprite TrackForegroundImage
        {
            get { return DependencyProperties.Get(TrackForegroundImagePropertyKey); }
            set { DependencyProperties.Set(TrackForegroundImagePropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the image to display as slider thumb (button).
        /// </summary>
        [DataMemberIgnore]
        public Sprite ThumbImage
        {
            get { return DependencyProperties.Get(ThumbImagePropertyKey); }
            set { DependencyProperties.Set(ThumbImagePropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the image to display as slider thumb (button) when the mouse is over the slider.
        /// </summary>
        [DataMemberIgnore]
        public Sprite MouseOverThumbImage
        {
            get { return DependencyProperties.Get(MouseOverThumbImagePropertyKey); }
            set { DependencyProperties.Set(MouseOverThumbImagePropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the image to display as tick.
        /// </summary>
        [DataMemberIgnore]
        public Sprite TickImage
        {
            get { return DependencyProperties.Get(TickImagePropertyKey); }
            set { DependencyProperties.Set(TickImagePropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the smallest possible value of the slider.
        /// </summary>
        [DataMemberIgnore]
        public float Minimum
        {
            get { return DependencyProperties.Get(MinimumPropertyKey); }
            set { DependencyProperties.Set(MinimumPropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the greatest possible value of the slider.
        /// </summary>
        [DataMemberIgnore]
        public float Maximum
        {
            get { return DependencyProperties.Get(MaximumPropertyKey); }
            set { DependencyProperties.Set(MaximumPropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the step of a <see cref="Value"/> change.
        /// </summary>
        [DataMemberIgnore]
        public float Step
        {
            get { return DependencyProperties.Get(StepPropertyKey); }
            set { DependencyProperties.Set(StepPropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the current value of the slider.
        /// </summary>
        /// <remarks>value is truncated between <see cref="Minimum"/> and <see cref="Maximum"/></remarks>
        [DataMember]
        [DefaultValue(0.0f)]
        public float Value
        {
            get { return value; }
            set
            {
                var oldValue = Value;

                this.value = Math.Min(Maximum, Math.Max(Minimum, value));
                if(ShouldSnapToTicks)
                    this.value = CalculateClosestTick(this.value);

                if(Math.Abs(oldValue - this.value) > MathUtil.ZeroTolerance)
                    RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
            }
        }

        /// <summary>
        /// Gets or sets the frequency of the ticks on the slider track.
        /// </summary>
        /// <remarks>Provided value is truncated to be greater or equal the 1</remarks>
        [DataMemberIgnore]
        public float TickFrequency
        {
            get { return DependencyProperties.Get(TickFrequencyPropertyKey); }
            set { DependencyProperties.Set(TickFrequencyPropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the offset in virtual pixels between the center of the track and center of the ticks (for an not-stretched slider).
        /// </summary>
        [DataMemberIgnore]
        public float TickOffset
        {
            get { return DependencyProperties.Get(TickOffsetPropertyKey); }
            set { DependencyProperties.Set(TickOffsetPropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the left/right offsets specifying where the track region starts. 
        /// </summary>
        [DataMemberIgnore]
        public Vector2 TrackStartingOffsets
        {
            get { return DependencyProperties.Get(TrackStartingOffsetsrPropertyKey); }
            set { DependencyProperties.Set(TrackStartingOffsetsrPropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the value indicating if the default direction of the slider should reversed or not.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        public bool IsDirectionReversed { get; set; }

        /// <summary>
        /// Gets or sets the value indicating if the ticks should be displayed or not.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        public bool AreTicksDisplayed { get; set; }

        /// <summary>
        /// Gets or sets the value indicating if the slider <see cref="Value"/> should be snapped to the ticks or not.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        public bool ShouldSnapToTicks
        {
            get { return shouldSnapToTicks; }
            set 
            { 
                shouldSnapToTicks = value;
                Value = Value; // snap if enabled
            }
        }

        /// <summary>
        /// Gets or sets the orientation of the slider.
        /// </summary>
        [DataMember]
        [DefaultValue(default(Orientation))]
        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;

                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Snap the current <see cref="Value"/> to the closest tick.
        /// </summary>
        public void SnapToClosestTick()
        {
            Value = CalculateClosestTick(Value);
        }

        /// <summary>
        /// Calculate the value of the closest tick to the provided value.
        /// </summary>
        /// <param name="rawValue">The current raw value</param>
        /// <returns>The value adjusted to the closest tick</returns>
        protected float CalculateClosestTick(float rawValue)
        {
            var absoluteValue = rawValue - Minimum;
            var step = (Maximum - Minimum) / TickFrequency;
            var times = (float)Math.Round(absoluteValue / step);
            return times * step;
        }

        /// <summary>
        /// Increase the <see cref="Value"/> by <see cref="Step"/>.
        /// </summary>
        /// <remarks>If <see cref="ShouldSnapToTicks"/> is <value>True</value> then it increases of at least one tick.</remarks>
        public void Increase()
        {
            Value += CalculateIncreamentValue();
        }

        /// <summary>
        /// Decrease the <see cref="Value"/> by <see cref="Step"/>.
        /// </summary>
        /// <remarks>If <see cref="ShouldSnapToTicks"/> is <value>True</value> then it decreases of at least one tick.</remarks>
        public void Decrease()
        {
            Value -= CalculateIncreamentValue();
        }

        private float CalculateIncreamentValue()
        {
            return shouldSnapToTicks? Math.Max(Step, (Maximum - Minimum) / TickFrequency): Step;
        }
        
        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            var image = TrackBackgroundImage;
            if (image == null)
                return base.MeasureOverride(availableSizeWithoutMargins);

            var idealSize = image.SizeInPixels.Y;
            var desiredSize = new Vector3(idealSize, idealSize, 0)
            {
                [(int)Orientation] = availableSizeWithoutMargins[(int)Orientation]
            };

            return desiredSize;
        }

        /// <summary>
        /// Occurs when the value of the slider changed.
        /// </summary>
        /// <remarks>A ValueChanged event is bubbling</remarks>
        public event EventHandler<RoutedEventArgs> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ValueChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>(
            "ValueChanged",
            RoutingStrategy.Bubble,
            typeof(Slider));
        
        /// <summary>
        /// The class handler of the event <see cref="ValueChanged"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnValueChanged(RoutedEventArgs args)
        {

        }

        private static void ValueChangedClassHandler(object sender, RoutedEventArgs args)
        {
            var slider = (Slider)sender;

            slider.OnValueChanged(args);
        }

        protected override void OnTouchDown(TouchEventArgs args)
        {
            base.OnTouchDown(args);

            SetValueFromTouchPosition(args.WorldPosition);
        }

        protected override void OnTouchMove(TouchEventArgs args)
        {
            base.OnTouchMove(args);

            SetValueFromTouchPosition(args.WorldPosition);
        }

        internal override void OnKeyDown(KeyEventArgs args)
        {
            base.OnKeyDown(args);

            if(args.Key == Keys.Right)
                Increase();
            if(args.Key == Keys.Left)
                Decrease();
        }

        /// <summary>
        /// Set <see cref="Value"/> from the world position of a touch event.
        /// </summary>
        /// <param name="touchPostionWorld">The world position of the touch</param>
        protected void SetValueFromTouchPosition(Vector3 touchPostionWorld)
        {
            var axis = (int)Orientation;
            var offsets = TrackStartingOffsets;
            var elementSize = RenderSize[axis];
            var touchPosition = touchPostionWorld[axis] - WorldMatrixInternal[12 + axis] + elementSize/2;
            var ratio = (touchPosition - offsets.X) / (elementSize - offsets.X - offsets.Y);
            Value = (Orientation == Orientation.Vertical ^ IsDirectionReversed) ? 1 - ratio : ratio;
        }
    }
}
