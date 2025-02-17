// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// Wrapper on the class ShakeIt Bass EffectsContainerBase.
    /// </summary>
    /// <remarks>
    /// The corresponding SimHub class is abstract and has subclasses for each concrete effect. As we are not interested in the concrete
    /// effects, we use this class also for the effects, as it already contains all the data we are interested in.
    /// <p/>
    /// Most properties are read-only.
    /// </remarks>
    public class EffectsContainerBase : TreeElement
    {
        private readonly DataPlugins.ShakeItV3.EffectsContainers.EffectsContainerBase _simHubEffectsContainerBase;

        public EffectsContainerBase(TreeElement parent,
            DataPlugins.ShakeItV3.EffectsContainers.EffectsContainerBase simHubEffectsContainerBase) : base(parent)
        {
            _simHubEffectsContainerBase = simHubEffectsContainerBase;
        }

        public Guid ContainerId
        {
            get => _simHubEffectsContainerBase.ContainerId;
            set => _simHubEffectsContainerBase.ContainerId = value;
        }

        public string ContainerName => _simHubEffectsContainerBase.ContainerName;
        public string Description => _simHubEffectsContainerBase.Description;
        public double Gain => _simHubEffectsContainerBase.Gain;
        public bool IsMuted => _simHubEffectsContainerBase.IsMuted;

        public virtual string FullName()
        {
            return string.IsNullOrWhiteSpace(Description) ? ContainerName : ContainerName + $" ({Description})";
        }

        public override string RecursiveName
        {
            get
            {
                if (Parent == null)
                {
                    return Description.Trim();
                }

                return Parent.RecursiveName + " / " + Description;
            }
        }
    }
}