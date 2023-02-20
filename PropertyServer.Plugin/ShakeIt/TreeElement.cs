// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// This class describes an element in a tree structure.
    /// </summary>
    public abstract class TreeElement
    {
        protected TreeElement(TreeElement parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Parent of this element or <c>null</c> if this element is at the root.
        /// </summary>
        protected TreeElement Parent { get; }

        /// <summary>
        /// Returns the name of this element, including all names of all parent elements.
        /// </summary>
        public abstract string RecursiveName { get; }
    }
}