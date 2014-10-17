﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;

using SiliconStudio.ActionStack;
using SiliconStudio.Presentation.Commands;
using SiliconStudio.Presentation.ViewModel;
using SiliconStudio.Quantum;
using SiliconStudio.Quantum.Commands;

namespace SiliconStudio.Presentation.Quantum
{
    /// <summary>
    /// A base class to wrap one or multiple <see cref="INodeCommand"/> instances into a <see cref="CancellableCommand"/>.
    /// </summary>
    public abstract class NodeCommandWrapperBase : CancellableCommand, INodeCommandWrapper
    {
        /// <summary>
        /// Gets the how to combine a set of <see cref="NodeCommandWrapperBase"/> in a <see cref="CombinedObservableNode"/>
        /// </summary>
        public abstract CombineMode CombineMode { get; }

        /// <summary>
        /// Gets or sets the path to the <see cref="ObservableNode"/> related to this command.
        /// </summary>
        public string ObservableNodePath { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancellableCommand"/> class.
        /// </summary>
        /// <param name="serviceProvider">A service provider that can provide a <see cref="IActionStack"/> to use for this view model.</param>
        /// <param name="dirtiables">The <see cref="IDirtiableViewModel"/> instances associated to this command.</param>
        protected NodeCommandWrapperBase(IViewModelServiceProvider serviceProvider, IEnumerable<IDirtiableViewModel> dirtiables)
            : base(serviceProvider, dirtiables)
        {
        }
    }
}
