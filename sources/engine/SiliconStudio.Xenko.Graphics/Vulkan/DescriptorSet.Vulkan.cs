﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_XENKO_GRAPHICS_API_VULKAN
using System;
using SharpVulkan;
using SiliconStudio.Xenko.Shaders;

namespace SiliconStudio.Xenko.Graphics
{
    public partial struct DescriptorSet
    {
        internal readonly SharpVulkan.DescriptorSet NativeDescriptorSet;
        internal readonly GraphicsDevice GraphicsDevice;
        internal readonly DescriptorSetLayout Description;
        
        public bool IsValid => Description != null;

        private unsafe DescriptorSet(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout desc)
        {
            Description = desc;
            GraphicsDevice = graphicsDevice;

            var nativeLayoutCopy = desc.NativeLayout;
            var allocateInfo = new DescriptorSetAllocateInfo
            {
                StructureType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = pool.NativeDescriptorPool,
                DescriptorSetCount = 1,
                SetLayouts = new IntPtr(&nativeLayoutCopy)
            };

            SharpVulkan.DescriptorSet descriptorSet;
            graphicsDevice.NativeDevice.AllocateDescriptorSets(ref allocateInfo, &descriptorSet);
            NativeDescriptorSet = descriptorSet;
        }

        /// <summary>
        /// Sets a descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="value">The descriptor.</param>
        public void SetValue(int slot, object value)
        {
            var srv = value as GraphicsResource;
            if (srv != null)
            {
                SetShaderResourceView(slot, srv);
            }
            else
            {
                var sampler = value as SamplerState;
                if (sampler != null)
                {
                    SetSamplerState(slot, sampler);
                }
            }
        }

        /// <summary>
        /// Sets a shader resource view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="shaderResourceView">The shader resource view.</param>
        public unsafe void SetShaderResourceView(int slot, GraphicsResource shaderResourceView)
        {
            if (!Description.Builder.Entries[slot].IsUsed)
                return;

            var write = new WriteDescriptorSet
            {
                StructureType = StructureType.WriteDescriptorSet,
                DescriptorCount = 1,
                DestinationSet = NativeDescriptorSet,
                DestinationBinding = (uint)slot,
                DestinationArrayElement = 0,
            };

            var texture = shaderResourceView as Texture;
            if (texture != null)
            {
                var imageInfo = new DescriptorImageInfo { ImageView = texture.NativeImageView, ImageLayout = ImageLayout.ShaderReadOnlyOptimal };

                write.DescriptorType = DescriptorType.SampledImage;
                //write.DescriptorType = Description.ImmutableSamplers[slot] != Sampler.Null ? DescriptorType.CombinedImageSampler : DescriptorType.SampledImage;
                write.ImageInfo = new IntPtr(&imageInfo);

                GraphicsDevice.NativeDevice.UpdateDescriptorSets(1, &write, 0, null);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Sets a sampler state descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="samplerState">The sampler state.</param>
        public unsafe void SetSamplerState(int slot, SamplerState samplerState)
        {
            if (!Description.Builder.Entries[slot].IsUsed)
                return;

            var imageInfo = new DescriptorImageInfo { Sampler = samplerState.NativeSampler };

            var write = new WriteDescriptorSet
            {
                StructureType = StructureType.WriteDescriptorSet,
                DescriptorCount = 1,
                DestinationSet = NativeDescriptorSet,
                DestinationBinding = (uint)slot,
                DestinationArrayElement = 0,
                DescriptorType = DescriptorType.Sampler,
                ImageInfo = new IntPtr(&imageInfo),
            };

            GraphicsDevice.NativeDevice.UpdateDescriptorSets(1, &write, 0, null);
        }

        /// <summary>
        /// Sets a constant buffer view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="buffer">The constant buffer.</param>
        /// <param name="offset">The constant buffer view start offset.</param>
        /// <param name="size">The constant buffer view size.</param>
        public unsafe void SetConstantBuffer(int slot, Buffer buffer, int offset, int size)
        {
            if (!Description.Builder.Entries[slot].IsUsed)
                return;

            var bufferInfo = new DescriptorBufferInfo { Buffer = buffer.NativeBuffer, Offset = (ulong)offset, Range = ~0UL /*size*/ }; // WholeSize, because size < bufferSize seems not to work?

            var write = new WriteDescriptorSet
            {
                StructureType = StructureType.WriteDescriptorSet,
                DescriptorCount = 1,
                DestinationSet = NativeDescriptorSet,
                DestinationBinding = (uint)slot,
                DestinationArrayElement = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                BufferInfo = new IntPtr(&bufferInfo)
            };
            
            GraphicsDevice.NativeDevice.UpdateDescriptorSets(1, &write, 0, null);
        }

        /// <summary>
        /// Sets an unordered access view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        public void SetUnorderedAccessView(int slot, GraphicsResource unorderedAccessView)
        {
            // TODO D3D12
            throw new NotImplementedException();
        }
    }
}
#endif