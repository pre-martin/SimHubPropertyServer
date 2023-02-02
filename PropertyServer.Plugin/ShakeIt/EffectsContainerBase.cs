// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// View on the class ShakeIt Bass EffectsContainerBase.
    /// </summary>
    /// <remarks>
    /// The corresponding SimHub class is abstract and has subclasses for each concrete effect. As we are not interested in the concrete
    /// effects, we use this class also for the effects, as it already contains all the data we are interested in.
    /// </remarks>
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
