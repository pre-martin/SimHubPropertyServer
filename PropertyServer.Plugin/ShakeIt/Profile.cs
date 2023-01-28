// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// View on the class ShakeIt Bass Profile.
    /// </summary>
    public class Profile
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public IList<EffectsContainerBase> EffectsContainers { get; } = new List<EffectsContainerBase>();
    }
}
