// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Collections.Generic;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// View on the class ShakeIt Bass GroupContainer.
    /// </summary>
    public class GroupContainer : EffectsContainerBase
    {
        public IList<EffectsContainerBase> EffectsContainers { get; } = new List<EffectsContainerBase>();

        public override string FullName() => Description;
    }
}
