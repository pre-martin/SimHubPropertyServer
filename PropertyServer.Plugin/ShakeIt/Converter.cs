// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// Class responsible for converting from the SimHub internal data model into our independent model.
    /// </summary>
    public class Converter
    {
        public GroupContainer Convert(DataPlugins.ShakeItV3.EffectsContainers.GroupContainer simHubGroupContainer)
        {
            return new GroupContainer
            {
                ContainerId = simHubGroupContainer.ContainerId, ContainerName = simHubGroupContainer.ContainerName,
                Description = simHubGroupContainer.Description, Gain = simHubGroupContainer.Gain,
                IsMuted = simHubGroupContainer.IsMuted
            };
        }

        public EffectsContainerBase Convert(DataPlugins.ShakeItV3.EffectsContainers.EffectsContainerBase simHubEffectsContainerBase)
        {
            return new EffectsContainerBase
            {
                ContainerId = simHubEffectsContainerBase.ContainerId, ContainerName = simHubEffectsContainerBase.ContainerName,
                Description = simHubEffectsContainerBase.Description, Gain = simHubEffectsContainerBase.Gain,
                IsMuted = simHubEffectsContainerBase.IsMuted
            };
        }
    }
}