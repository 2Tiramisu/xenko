﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SiliconStudio.Core.Updater;

namespace SiliconStudio.Paradox.Engine.Tests
{
    [TestFixture]
    public class EntityUpdateEngineTest
    {
        [Test]
        public unsafe void TestComponentAccess()
        {
            var entity = new Entity();

            entity.AddChild(new Entity("child1")
            {
                new LightComponent()
            });

            var modelComponent = new ModelComponent();

            var compiledUpdate = UpdateEngine.Compile(typeof(Entity), new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("[SiliconStudio.Paradox.Engine.ModelComponent,SiliconStudio.Paradox.Engine.Key]", 0),
                new UpdateMemberInfo("child1[SiliconStudio.Paradox.Engine.LightComponent,SiliconStudio.Paradox.Engine.Key].Intensity", 0),
            });

            var testData = new TestData[] { 32.0f };

            fixed (TestData* dataPtr = testData)
            {
                UpdateEngine.Run(entity, compiledUpdate, (IntPtr)dataPtr, new[] { new UpdateObjectData(modelComponent) });
            }

            Assert.That(entity.Get(ModelComponent.Key), Is.EqualTo(modelComponent));
            Assert.That(entity.GetChild(0).Get(LightComponent.Key).Intensity, Is.EqualTo(32.0f));
        }

        struct TestData
        {
            public float Factor;
            public float Value;

            public static implicit operator TestData(float value)
            {
                return new TestData { Factor = 1.0f, Value = value };
            }
        }
    }
}