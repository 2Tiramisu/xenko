﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;

namespace SiliconStudio.Assets
{
    /// <summary>
    /// A container for part assets.
    /// </summary>
    public interface IAssetPartContainer
    {
        /// <summary>
        /// Collects the part assets.
        /// </summary>
        IEnumerable<AssetPart> CollectParts();
    }
}