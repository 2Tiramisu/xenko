﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SiliconStudio.Presentation.Extensions
{
    public static class BindingExtensions
    {
        private static readonly MethodInfo CloneMethodInfo = typeof(BindingBase).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// This methods is a wrapper to the internal method Clone of the <see cref="BindingBase"/>. Using this method should be considered unsafe.
        /// </summary>
        /// <param name="bindingBase">The BindingBase to clone.</param>
        /// <param name="mode">The BindingMode value to set for the clone.</param>
        /// <returns>A clone of the given <see cref="BindingBase"/></returns>
        public static BindingBase CloneBinding(this BindingBase bindingBase, BindingMode mode)
        {
            return (BindingBase)CloneMethodInfo.Invoke(bindingBase, new object[] { mode });
        }
    }
}
