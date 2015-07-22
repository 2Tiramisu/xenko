// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.ComponentModel;

using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Paradox.Engine.Design;
using SiliconStudio.Paradox.Engine.Processors;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Engine
{
    /// <summary>
    /// Add a background to an <see cref="Entity"/>.
    /// </summary>
    [DataContract("BackgroundComponent")]
    [Display(96, "Background", Expand = ExpandRule.Once)]
    [DefaultEntityComponentRenderer(typeof(BackgroundComponentRenderer))]
    [DefaultEntityComponentProcessor(typeof(BackgroundComponentProcessor))]
    public class BackgroundComponent : EntityComponent
    {
        public static PropertyKey<BackgroundComponent> Key = new PropertyKey<BackgroundComponent>("Key", typeof(BackgroundComponent));

        /// <summary>
        /// Create an empty Background component.
        /// </summary>
        public BackgroundComponent()
        {
            Intensity = 1f;
        }

        /// <summary>
        /// Gets or sets the texture to use as background
        /// </summary>
        /// <userdoc>The reference to the texture to use as background</userdoc>
        [DataMember(10)]
        [Display("Texture")]
        public Texture Texture { get; set; }

        /// <summary>
        /// Gets or sets the intensity.
        /// </summary>
        /// <value>The intensity.</value>
        /// <userdoc>The intensity of the background color</userdoc>
        [DataMember(20)]
        [DefaultValue(1.0f)]
        [DataMemberRange(0.0, 100.0, 0.01f, 1.0f)]
        public float Intensity { get; set; }

        public override PropertyKey GetDefaultKey()
        {
            return Key;
        }
    }
}