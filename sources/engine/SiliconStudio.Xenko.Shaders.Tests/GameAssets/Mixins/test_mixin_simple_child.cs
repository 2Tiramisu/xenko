﻿// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Paradox Shader Mixin Code Generator.
// To generate it yourself, please install SiliconStudio.Paradox.VisualStudio.Package .vsix
// and re-save the associated .pdxfx.
// </auto-generated>

using System;
using SiliconStudio.Core;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders;
using SiliconStudio.Core.Mathematics;
using Buffer = SiliconStudio.Paradox.Graphics.Buffer;

namespace Test3
{
    internal static partial class ShaderMixins
    {
        internal partial class ChildMixin  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSource mixin, ShaderMixinContext context)
            {
                context.Mixin(mixin, "C1");
                context.Mixin(mixin, "C2");
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("ChildMixin", new ChildMixin());
            }
        }
    }
    internal static partial class ShaderMixins
    {
        internal partial class DefaultSimpleChild  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSource mixin, ShaderMixinContext context)
            {
                context.Mixin(mixin, "A");
                context.Mixin(mixin, "B");
                context.Mixin(mixin, "C");
                context.Mixin(mixin, "ChildMixin");
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("DefaultSimpleChild", new DefaultSimpleChild());
            }
        }
    }
}