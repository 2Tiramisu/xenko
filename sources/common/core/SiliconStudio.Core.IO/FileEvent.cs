// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

namespace SiliconStudio.Core.IO
{
    /// <summary>
    /// � file event used notified by <see cref="DirectoryWatcher"/>
    /// </summary>
    public sealed class FileEvent : EventArgs
    {
        private readonly FileEventChangeType changeType;
        private readonly string name;
        private readonly string fullPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEvent"/> class.
        /// </summary>
        /// <param name="changeType">Type of the change.</param>
        /// <param name="name">The name.</param>
        /// <param name="fullPath">The full path.</param>
        public FileEvent(FileEventChangeType changeType, string name, string fullPath)
        {
            this.changeType = changeType;
            this.name = name;
            this.fullPath = fullPath;
        }

        /// <summary>
        /// Gets the type of the change.
        /// </summary>
        /// <value>The type of the change.</value>
        public FileEventChangeType ChangeType
        {
            get
            {
                return changeType;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath
        {
            get
            {
                return fullPath;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", changeType, fullPath);
        }
    }
}