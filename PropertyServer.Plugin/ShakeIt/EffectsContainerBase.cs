// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// View on the class ShakeIt Bass EffectsContainerBase.
    /// </summary>
    public class EffectsContainerBase
    {
        public Guid ContainerId { get; set; }
        public string ContainerName { get; set; }
        public string Description { get; set; }
        public double Gain { get; set; }
        public bool IsMuted { get; set; }

        public virtual string FullName()
        {
            return string.IsNullOrWhiteSpace(Description) ? ContainerName : ContainerName + $" ({Description})";
        }
    }
}
