﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core.Storage;

namespace SiliconStudio.Core.IO
{
    public interface IDatabaseStream
    {
        ObjectId ObjectId { get; }
    }
}