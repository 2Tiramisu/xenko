﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Updater;

namespace SiliconStudio.Xenko.Engine.Tests
{
    [TestFixture]
    public class TestUpdateEngine
    {
        [Test]
        public void TestIntField()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntField", 0),
            };

            var blittableData = new TestData[] { 123 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.IntField, Is.EqualTo(123));
        }

        [Test]
        public void TestIntProperty()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntProperty", 0),
            };

            var blittableData = new TestData[] { 123 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.IntProperty, Is.EqualTo(123));
        }

        [Test]
        public void TestObjectField()
        {
            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("ObjectField", 0),
            };

            var blittableData = new TestData[0];
            var objectData = new[] { new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.ObjectField, Is.EqualTo(test2));
        }

        [Test]
        public void TestObjectProperty()
        {
            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("ObjectProperty", 0),
            };

            var blittableData = new TestData[0];
            var objectData = new[] { new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.ObjectProperty, Is.EqualTo(test2));
        }

        [Test]
        public void TestCastQualifiedName()
        {
            var test = new TestClass()
            {
                ObjectField = new TestClass(),
                ObjectProperty = new TestClass(),
            };

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("ObjectField.(SiliconStudio.Xenko.Engine.Tests.TestClass,SiliconStudio.Xenko.Engine.Tests).IntField", 0),
                new UpdateMemberInfo("ObjectProperty.(SiliconStudio.Xenko.Engine.Tests.TestClass,SiliconStudio.Xenko.Engine.Tests).IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(((TestClass)test.ObjectField).IntField, Is.EqualTo(123));
            Assert.That(((TestClass)test.ObjectProperty).IntField, Is.EqualTo(456));
        }


        [Test]
        public void TestIntArray()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntArray[0]", 0),
                new UpdateMemberInfo("IntArray[2]", 0),
                new UpdateMemberInfo("IntArray[3]", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.IntArray[0], Is.EqualTo(123));
            Assert.That(test.IntArray[1], Is.EqualTo(0));
            Assert.That(test.IntArray[2], Is.EqualTo(123));
            Assert.That(test.IntArray[3], Is.EqualTo(456));
        }

        [Test]
        public void TestIntList()
        {
            var test = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntList[0]", 0),
                new UpdateMemberInfo("IntList[2]", 0),
                new UpdateMemberInfo("IntList[3]", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new UpdateObjectData[0];

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.IntList[0], Is.EqualTo(123));
            Assert.That(test.IntList[1], Is.EqualTo(0));
            Assert.That(test.IntList[2], Is.EqualTo(123));
            Assert.That(test.IntList[3], Is.EqualTo(456));
        }

        [Test]
        public void TestNonBlittableStruct()
        {
            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("NonBlittableStructField.TestClassField", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassProperty", 0),
                new UpdateMemberInfo("NonBlittableStructProperty.TestClassField", 0),
                new UpdateMemberInfo("NonBlittableStructProperty.TestClassProperty", 0),
            };

            var blittableData = new TestData[0];
            var objectData = new[] { new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.NonBlittableStructField.TestClassField, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructField.TestClassProperty, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructProperty.TestClassField, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructProperty.TestClassProperty, Is.EqualTo(test2));
        }

        [Test]
        public void TestTestClassArray()
        {
            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("TestClassArray[0]", 0),
                new UpdateMemberInfo("TestClassArray[0].IntField", 0),
                new UpdateMemberInfo("TestClassArray[1]", 1),
                new UpdateMemberInfo("TestClassArray[1].IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new[] { new UpdateObjectData(test), new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.TestClassArray[0], Is.EqualTo(test));
            Assert.That(test.TestClassArray[0].IntField, Is.EqualTo(123));
            Assert.That(test.TestClassArray[1], Is.EqualTo(test2));
            Assert.That(test.TestClassArray[1].IntField, Is.EqualTo(456));
        }

        [Test]
        public void TestTestClassList()
        {
            UpdateEngine.RegisterMemberResolver(new ListUpdateResolver<TestClass>());

            var test = new TestClass();
            var test2 = new TestClass();

            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("TestClassList[0]", 0),
                new UpdateMemberInfo("TestClassList[0].IntField", 0),
                new UpdateMemberInfo("TestClassList[1]", 1),
                new UpdateMemberInfo("TestClassList[1].IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new[] { new UpdateObjectData(test), new UpdateObjectData(test2) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.TestClassList[0], Is.EqualTo(test));
            Assert.That(test.TestClassList[0].IntField, Is.EqualTo(123));
            Assert.That(test.TestClassList[1], Is.EqualTo(test2));
            Assert.That(test.TestClassList[1].IntField, Is.EqualTo(456));
        }

        [Test]
        public void TestManyProperties()
        {
            var test = new TestClass();
            var test2 = new TestClass();
            var test3 = new TestClass();

            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ListUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<TestClass>());

            // Combine many of the other tests in one, to easily test if switching from one member to another works well
            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("IntField", 0),
                new UpdateMemberInfo("IntProperty", 8),
                new UpdateMemberInfo("NonBlittableStructField.TestClassField", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassProperty", 0),
                new UpdateMemberInfo("NonBlittableStructProperty.TestClassField", 0),
                new UpdateMemberInfo("NonBlittableStructProperty.TestClassProperty", 0),
                new UpdateMemberInfo("ObjectField", 0),
                new UpdateMemberInfo("ObjectProperty", 0),
                new UpdateMemberInfo("IntArray[0]", 0),
                new UpdateMemberInfo("IntArray[2]", 0),
                new UpdateMemberInfo("IntArray[3]", 8),
                new UpdateMemberInfo("IntList[0]", 0),
                new UpdateMemberInfo("IntList[2]", 0),
                new UpdateMemberInfo("IntList[3]", 8),
                new UpdateMemberInfo("TestClassArray[0]", 0),
                new UpdateMemberInfo("TestClassArray[0].IntField", 0),
                new UpdateMemberInfo("TestClassArray[1]", 1),
                new UpdateMemberInfo("TestClassArray[1].IntField", 8),
            };

            var blittableData = new TestData[] { 123, 456 };
            var objectData = new[] { new UpdateObjectData(test2), new UpdateObjectData(test3) };

            RunUpdateEngine(test, updateMemberInfo, blittableData, objectData);

            Assert.That(test.IntField, Is.EqualTo(123));
            Assert.That(test.IntProperty, Is.EqualTo(456));
            Assert.That(test.NonBlittableStructField.TestClassField, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructField.TestClassProperty, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructProperty.TestClassField, Is.EqualTo(test2));
            Assert.That(test.NonBlittableStructProperty.TestClassProperty, Is.EqualTo(test2));
            Assert.That(test.ObjectField, Is.EqualTo(test2));
            Assert.That(test.ObjectProperty, Is.EqualTo(test2));
            Assert.That(test.IntArray[0], Is.EqualTo(123));
            Assert.That(test.IntArray[1], Is.EqualTo(0));
            Assert.That(test.IntArray[2], Is.EqualTo(123));
            Assert.That(test.IntArray[3], Is.EqualTo(456));
            Assert.That(test.IntList[0], Is.EqualTo(123));
            Assert.That(test.IntList[1], Is.EqualTo(0));
            Assert.That(test.IntList[2], Is.EqualTo(123));
            Assert.That(test.IntList[3], Is.EqualTo(456));
            Assert.That(test.TestClassArray[0], Is.EqualTo(test2));
            Assert.That(test.TestClassArray[0].IntField, Is.EqualTo(123));
            Assert.That(test.TestClassArray[1], Is.EqualTo(test3));
            Assert.That(test.TestClassArray[1].IntField, Is.EqualTo(456));
        }

        [Test]
        public void TestNullSkip()
        {
            var test = new TestClass { IntList = null, IntArray = null };

            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ListUpdateResolver<int>());
            UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<TestClass>());

            // Combine many of the other tests in one, to easily test if switching from one member to another works well
            var updateMemberInfo = new List<UpdateMemberInfo>
            {
                new UpdateMemberInfo("NonBlittableStructField.TestClassField.IntField", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassField.IntProperty", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassProperty.IntField", 0),
                new UpdateMemberInfo("NonBlittableStructField.TestClassProperty.IntProperty", 0),
                new UpdateMemberInfo("ObjectField.(SiliconStudio.Xenko.Engine.Tests.TestClass,SiliconStudio.Xenko.Engine.Tests).IntField", 0),
                new UpdateMemberInfo("ObjectProperty.(SiliconStudio.Xenko.Engine.Tests.TestClass,SiliconStudio.Xenko.Engine.Tests).IntField", 0),
                new UpdateMemberInfo("IntArray[0]", 0),
                new UpdateMemberInfo("IntArray[2]", 0),
                new UpdateMemberInfo("IntArray[3]", 0),
                new UpdateMemberInfo("IntField", 0),
                new UpdateMemberInfo("IntList[0]", 0),
                new UpdateMemberInfo("IntList[2]", 0),
                new UpdateMemberInfo("IntList[3]", 0),
                new UpdateMemberInfo("TestClassArray[0].IntField", 0),
                new UpdateMemberInfo("TestClassArray[1].IntField", 0),
                new UpdateMemberInfo("IntProperty", 0),
            };

            var blittableData = new TestData[] { 123 };

            // Just check that it doesn't crash and some set are properly done
            RunUpdateEngine(test, updateMemberInfo, blittableData, null);

            Assert.That(test.IntField, Is.EqualTo(123));
            Assert.That(test.IntProperty, Is.EqualTo(123));

            // Also try with null array
            test.TestClassArray = null;
            blittableData[0] = 456;
            RunUpdateEngine(test, updateMemberInfo, blittableData, null);
            Assert.That(test.IntField, Is.EqualTo(456));
            Assert.That(test.IntProperty, Is.EqualTo(456));
        }

        private static unsafe void RunUpdateEngine(TestClass test, List<UpdateMemberInfo> updateMemberInfo, TestData[] blittableData, UpdateObjectData[] objectData)
        {
            var compiledUpdate = UpdateEngine.Compile(test.GetType(), updateMemberInfo);

            fixed (TestData* dataPtr = blittableData)
            {
                UpdateEngine.Run(test, compiledUpdate, (IntPtr)dataPtr, objectData);
            }
        }

        struct TestData
        {
            public float Factor;
            public int Value;

            public static implicit operator TestData(int value)
            {
                return new TestData { Factor = 1.0f, Value = value };
            }
        }
    }

    [DataContract]
    public struct NonBlittableStruct
    {
        public TestClass TestClassField;
        public TestClass TestClassProperty { get; set; }
    }

    [DataContract]
    public class TestClass
    {
        public int IntField;
        public int IntProperty { get; set; }

        public object ObjectField;
        public object ObjectProperty { get; set; }

        public NonBlittableStruct NonBlittableStructField;
        public NonBlittableStruct NonBlittableStructProperty { get; set; }
        public IList<int> NonBlittableStructList;

        public int[] IntArray;
        public TestClass[] TestClassArray;

        public IList<int> IntList;
        public IList<TestClass> TestClassList;

        public TestClass()
        {
            IntArray = new int[4];
            IntList = new List<int> { 0, 0, 0, 0 };
            TestClassArray = new TestClass[2];
            TestClassList = new List<TestClass> { null, null };
        }
    }

}