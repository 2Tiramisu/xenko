﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Xenko.Particles.Tests
{
    class Program
    {
        static readonly Stopwatch Watch = new Stopwatch();
        static readonly Stopwatch WatchParticles = new Stopwatch();

        private delegate void RunTestDelegate(int count);

        static void RunTest(RunTestDelegate test, int particleCount, int times)
        {
            Watch.Restart();
            for (var i = 0; i < times; i++)
            {
                test(particleCount);
            }
            var timeUmnArray = Watch.Elapsed.TotalMilliseconds;
            System.Console.Out.WriteLine($"{timeUmnArray:0000.000} ms to run {test.Method.Name}");
        }

        static void Main(string[] args)
        {
            const int particleCount = 50000;
            const int numberOfTests = 30;
#if DEBUG
            System.Console.Out.WriteLine($"This test is DEBUG so it's slow");
            System.Console.Out.WriteLine($"Its purpose is to ASSERT the particle engine works properly");
#else
            System.Console.Out.WriteLine($"This test id RELEASE so it skips assertion");
            System.Console.Out.WriteLine($"Its purpose is to MEASURE how fast the particle engine works");
#endif

            System.Console.Out.WriteLine();
            System.Console.Out.WriteLine($"Testing the ParticlePool");
            WatchParticles.Restart();
            RunTest(TestPoolAsStack, particleCount, numberOfTests);

            RunTest(TestPoolAsRing, particleCount, numberOfTests);
            var totalMSecs = WatchParticles.Elapsed.TotalMilliseconds;

            RunTest(TestAccessAsStack, particleCount, numberOfTests);

            RunTest(TestAccessAsRing, particleCount, numberOfTests);

            System.Console.Out.WriteLine();
            System.Console.Out.WriteLine($"Ground truth speed");
            RunTest(GroundTruth, particleCount, numberOfTests);

            // Later - test Emitter updates

            // Later - test adding/removing fields

            // Later - test adding/removing modules

            // Much later - test draw calls

            System.Console.Out.WriteLine($"We can have {(particleCount * numberOfTests * 2)/ totalMSecs} particles on this system with update time <= 1ms on one thread");

            System.Console.ReadLine();

        }

        private static void Assert(bool condition, string message, [CallerFilePath] string callingFilePath = "", [CallerLineNumber] int callerLine = 0)
        {
            Debug.Assert(condition, $"Assert failed in {callingFilePath} at line[{callerLine}]: {message}");
        }
    

        private static void TestPoolAsRing(int particleCount) => TestPool(particleCount, ParticlePool.ListPolicy.Ring);
        private static void TestPoolAsStack(int particleCount) => TestPool(particleCount, ParticlePool.ListPolicy.Stack);
        private static void TestAccessAsRing(int particleCount) => TestSetGet(particleCount, ParticlePool.ListPolicy.Ring);
        private static void TestAccessAsStack(int particleCount) => TestSetGet(particleCount, ParticlePool.ListPolicy.Stack);

        /// <summary>
        /// This test simulates how an Emitter would access and update the particles.
        /// Some numbers are fixed, like particle total count and field offsets.
        /// </summary>
        private unsafe static void TestPool(int particleCount, ParticlePool.ListPolicy listPolicy)
        {
            var particlePool = new ParticlePool(0, particleCount, listPolicy);

            const bool forceCreation = true;
            particlePool.FieldExists(ParticleFields.Position     , forceCreation);
            particlePool.FieldExists(ParticleFields.RemainingLife, forceCreation);
            particlePool.FieldExists(ParticleFields.Velocity     , forceCreation);
            particlePool.FieldExists(ParticleFields.Size         , forceCreation);

            var positionField = particlePool.GetField(ParticleFields.Position);
            var lifetimeField = particlePool.GetField(ParticleFields.RemainingLife);
            var velocityField = particlePool.GetField(ParticleFields.Velocity);
            var sizeField     = particlePool.GetField(ParticleFields.Size);

            for (int idx = 0; idx < particleCount; idx++)
            {
                particlePool.AddParticle();
            }

            var i = 0;
            var vecToSet = new Vector3(0, 0, 0);
            foreach (var particle in particlePool)
            {
                vecToSet.Y = i;

                *((Vector3*)particle[positionField]) = vecToSet;

                *((float*)particle[lifetimeField]) = i;

                *((Vector3*)particle[velocityField]) = vecToSet;

                *((float*)particle[sizeField]) = i;

                i++;
            }

#if DEBUG
            i = 0;
            foreach (var particle in particlePool)
            {
                Assert(particle.Get(positionField).Equals(new Vector3(0, i, 0)), $"Position.Y does not equal {i}");
                Assert(Math.Abs(particle.Get(lifetimeField) - i) <= MathUtil.ZeroTolerance, $"Remaining life does not equal {i}");
                Assert(particle.Get(velocityField).Equals(new Vector3(0, i, 0)), $"Velocity.Y does not equal {i}");
                Assert(Math.Abs(particle.Get(sizeField) - i) <= MathUtil.ZeroTolerance, $"Size does not equal {i}");
                i++;
            }
#endif
            i = 0;
            var dt = 0.033f;
            foreach (var particle in particlePool)
            {
                var pos = ((Vector3*)particle[positionField]);
                var vel = ((Vector3*)particle[velocityField]);

                *pos += *vel * dt;

                *((float*)particle[lifetimeField]) += 1;
                
                i++;
            }

#if DEBUG
            i = 0;
            foreach (var particle in particlePool)
            {
                vecToSet.Y = i;
                Assert(particle.Get(positionField).Equals(vecToSet + particle.Get(velocityField) * dt), "Particle's position is different!");
                Assert(Math.Abs(particle.Get(lifetimeField) - i - 1) <= MathUtil.ZeroTolerance, "Particle's lifetime is different!");
                i++;
            }
#endif

            var testVec = new Vector3(0, 1, 0);
            // Perf test - many mundane operations at once
            foreach (var particle in particlePool)
            {
                var vel = ((Vector3*)particle[velocityField]);

                *vel += testVec;
                *vel += testVec;
                *vel += testVec;
                *vel += testVec;
                *vel += testVec;
            }

            foreach (var particle in particlePool)
            {
                *((float*)particle[sizeField]) += 1;
                *((float*)particle[sizeField]) += 1;
                *((float*)particle[sizeField]) += 1;
                *((float*)particle[sizeField]) += 1;
                *((float*)particle[sizeField]) += 1;
            }

            foreach (var particle in particlePool)
            {
                var pos = ((Vector3*)particle[positionField]);

                *pos += testVec;
                *pos += testVec;
                *pos += testVec;
                *pos += testVec;
                *pos += testVec;
            }

            foreach (var particle in particlePool)
            {
                *((float*)particle[lifetimeField]) += 1;
                *((float*)particle[lifetimeField]) += 1;
                *((float*)particle[lifetimeField]) += 1;
                *((float*)particle[lifetimeField]) += 1;
                *((float*)particle[lifetimeField]) += 1;
            }            
        }

        private static void TestSetGet(int particleCount, ParticlePool.ListPolicy listPolicy)
        {
            var particlePool = new ParticlePool(0, particleCount, listPolicy);

            const bool forceCreation = true;
            particlePool.FieldExists(ParticleFields.Position, forceCreation);
            particlePool.FieldExists(ParticleFields.RemainingLife, forceCreation);
            particlePool.FieldExists(ParticleFields.Velocity, forceCreation);
            particlePool.FieldExists(ParticleFields.Size, forceCreation);

            var positionField = particlePool.GetField(ParticleFields.Position);
            var lifetimeField = particlePool.GetField(ParticleFields.RemainingLife);
            var velocityField = particlePool.GetField(ParticleFields.Velocity);
            var sizeField = particlePool.GetField(ParticleFields.Size);

            for (int idx = 0; idx < particleCount; idx++)
            {
                particlePool.AddParticle();
            }

            var i = 0;
            var vecToSet = new Vector3(0, 0, 0);
            foreach (var particle in particlePool)
            {
                vecToSet.Y = i;
                particle.Set(positionField, vecToSet);
                particle.Set(lifetimeField, i);
                particle.Set(velocityField, vecToSet);
                particle.Set(sizeField, i);
                i++;
            }

#if DEBUG
            i = 0;
            foreach (var particle in particlePool)
            {
                Assert(particle.Get(positionField).Equals(new Vector3(0, i, 0)), $"Position.Y does not equal {i}");
                Assert(Math.Abs(particle.Get(lifetimeField) - i) <= MathUtil.ZeroTolerance, $"Remaining life does not equal {i}");
                Assert(particle.Get(velocityField).Equals(new Vector3(0, i, 0)), $"Velocity.Y does not equal {i}");
                Assert(Math.Abs(particle.Get(sizeField) - i) <= MathUtil.ZeroTolerance, $"Size does not equal {i}");
                i++;
            }
#endif
            i = 0;
            var dt = 0.033f;
            foreach (var particle in particlePool)
            {
                particle.Set(positionField, particle.Get(positionField) + particle.Get(velocityField) * dt);

                particle.Set(lifetimeField, particle.Get(lifetimeField) + 1);

                i++;
            }

#if DEBUG
            i = 0;
            foreach (var particle in particlePool)
            {
                vecToSet.Y = i;
                Assert(particle.Get(positionField).Equals(vecToSet + particle.Get(velocityField) * dt), "Particle's position is different!");
                Assert(Math.Abs(particle.Get(lifetimeField) - i - 1) <= MathUtil.ZeroTolerance, "Particle's lifetime is different!");
                i++;
            }
#endif

            var testVec = new Vector3(0, 1, 0);
            // Perf test - many mundane operations at once
            foreach (var particle in particlePool)
            {
                particle.Set(velocityField, particle.Get(velocityField) + testVec);
                particle.Set(velocityField, particle.Get(velocityField) + testVec);
                particle.Set(velocityField, particle.Get(velocityField) + testVec);
                particle.Set(velocityField, particle.Get(velocityField) + testVec);
                particle.Set(velocityField, particle.Get(velocityField) + testVec);
            }

            foreach (var particle in particlePool)
            {
                particle.Set(sizeField, particle.Get(sizeField) + 1);
                particle.Set(sizeField, particle.Get(sizeField) + 1);
                particle.Set(sizeField, particle.Get(sizeField) + 1);
                particle.Set(sizeField, particle.Get(sizeField) + 1);
                particle.Set(sizeField, particle.Get(sizeField) + 1);
            }

            foreach (var particle in particlePool)
            {
                particle.Set(positionField, particle.Get(positionField) + testVec);
                particle.Set(positionField, particle.Get(positionField) + testVec);
                particle.Set(positionField, particle.Get(positionField) + testVec);
                particle.Set(positionField, particle.Get(positionField) + testVec);
                particle.Set(positionField, particle.Get(positionField) + testVec);
            }

            foreach (var particle in particlePool)
            {
                particle.Set(lifetimeField, particle.Get(lifetimeField) + 1);
                particle.Set(lifetimeField, particle.Get(lifetimeField) + 1);
                particle.Set(lifetimeField, particle.Get(lifetimeField) + 1);
                particle.Set(lifetimeField, particle.Get(lifetimeField) + 1);
                particle.Set(lifetimeField, particle.Get(lifetimeField) + 1);
            }
        }

        private static void GroundTruth(int particleCount)
        {
            var positionField = new Vector3[particleCount];
            var lifetimeField = new float  [particleCount];
            var velocityField = new Vector3[particleCount];
            var sizeField     = new float  [particleCount];

            var vecToSet = new Vector3(0, 0, 0);
            for(var i = 0; i < particleCount; i++)
            {
                vecToSet.Y = i;

                positionField[i] = vecToSet;
                lifetimeField[i] = i;
                velocityField[i] = vecToSet;
                sizeField[i] = i;

            }

            var dt = 0.033f;
            for (var i = 0; i < particleCount; i++)
            {
                positionField[i] += velocityField[i]*dt;
                lifetimeField[i] += 1;
            }

            var testVec = new Vector3(0, 1, 0);

            for (var i = 0; i < particleCount; i++)
            {
                velocityField[i] += testVec;
                velocityField[i] += testVec;
                velocityField[i] += testVec;
                velocityField[i] += testVec;
                velocityField[i] += testVec;
            }

            for (var i = 0; i < particleCount; i++)
            {
                sizeField[i] += 1;
                sizeField[i] += 1;
                sizeField[i] += 1;
                sizeField[i] += 1;
                sizeField[i] += 1;
            }

            for (var i = 0; i < particleCount; i++)
            {
                positionField[i] += testVec;
                positionField[i] += testVec;
                positionField[i] += testVec;
                positionField[i] += testVec;
                positionField[i] += testVec;
            }

            for (var i = 0; i < particleCount; i++)
            {
                lifetimeField[i] += 1;
                lifetimeField[i] += 1;
                lifetimeField[i] += 1;
                lifetimeField[i] += 1;
                lifetimeField[i] += 1;
            }

        }
    }
}
