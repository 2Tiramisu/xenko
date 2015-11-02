﻿using System.Collections.Generic;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Engine.Design;
using SiliconStudio.Core.Reflection;
using System.Reflection;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Xenko.Animations;
using SiliconStudio.Xenko.SpriteStudio.Runtime;

namespace SiliconStudio.Xenko.Engine
{
    [DataContract("SpriteStudioComponent")]
    [Display(99, "Sprite Studio", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SpriteStudioProcessor))]
    [DefaultEntityComponentRenderer(typeof(SpriteStudioRenderer))]
    [DataSerializerGlobal(null, typeof(List<SpriteStudioNodeState>))]
    public class SpriteStudioComponent : EntityComponent
    {
        public static PropertyKey<SpriteStudioComponent> Key = new PropertyKey<SpriteStudioComponent>("Key", typeof(SpriteStudioComponent));

        public override PropertyKey GetDefaultKey()
        {
            return Key;
        }

        [DataMember(1)]
        public SpriteStudioSheet Sheet { get; set; }

        [DataMemberIgnore]
        public List<SpriteStudioNodeState> Nodes { get; } = new List<SpriteStudioNodeState>();

        [DataMemberIgnore]
        internal List<SpriteStudioNodeState> SortedNodes { get; set; }
    }
}