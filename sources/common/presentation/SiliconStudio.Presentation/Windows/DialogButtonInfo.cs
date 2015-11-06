﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Presentation.ViewModel;

namespace SiliconStudio.Presentation.Windows
{
    /// <summary>
    /// Represents a button in a <see cref="MessageDialogBase"/>.
    /// </summary>
    public sealed class DialogButtonInfo : ViewModelBase
    {
        private bool isCancel;
        private bool isDefault;
        private int result;
        private object content;

        /// <summary>
        /// Specifies whether or not this button is the cancel button.
        /// </summary>
        public bool IsCancel
        {
            get { return isCancel; }
            set { SetValue(ref isCancel, value); }
        }

        /// <summary>
        /// Specifies whether or not this button is the default button.
        /// </summary>
        public bool IsDefault
        {
            get { return isDefault; }
            set { SetValue(ref isDefault, value); }
        }

        /// <summary>
        /// The result associated with this button.
        /// </summary>
        /// <seealso cref="MessageDialogBase.Result"/>
        public int Result
        {
            get { return result; }
            set { SetValue(ref result, value); }
        }

        /// <summary>
        /// The content of this button.
        /// </summary>
        /// <seealso cref="System.Windows.Controls.Button.Content"/>
        public object Content
        {
            get { return content; }
            set { SetValue(ref content, value); }
        }
    }
}
