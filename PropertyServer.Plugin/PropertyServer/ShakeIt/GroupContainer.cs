// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Collections.Generic;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// Wrapper for the SimHub class "GroupContainer".
    /// </summary>
    /// <remarks>
    /// Modifications to the structure are not supported! Most properties are read-only.
    /// </remarks>
    public class GroupContainer : EffectsContainerBase
    {
        private readonly DataPlugins.ShakeItV3.EffectsContainers.GroupContainer _simHubGroupContainer;
        private readonly List<EffectsContainerBase> _effectsContainers = new List<EffectsContainerBase>();

        public GroupContainer(TreeElement parent, DataPlugins.ShakeItV3.EffectsContainers.GroupContainer simHubGroupContainer)
            : base(parent, simHubGroupContainer)
        {
            _simHubGroupContainer = simHubGroupContainer;
            Converter.Convert(this, simHubGroupContainer.EffectsContainers, _effectsContainers);
        }

        public IList<EffectsContainerBase> EffectsContainers => _effectsContainers.AsReadOnly();

        public override string FullName() => Description;
    }
}