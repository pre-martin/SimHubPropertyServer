// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// Class responsible for converting from the SimHub internal data model into our wrapped model.
    /// </summary>
    public abstract class Converter
    {

        public static void Convert(TreeElement parent, ObservableCollection<DataPlugins.ShakeItV3.EffectsContainers.EffectsContainerBase> simHubEffectsContainers, IList<EffectsContainerBase> effectsContainers)
        {
            foreach (var simHubEffectsContainer in simHubEffectsContainers)
            {
                var effectsContainer = Convert(parent, simHubEffectsContainer);
                effectsContainers.Add(effectsContainer);
            }
        }

        public static EffectsContainerBase Convert(TreeElement parent, DataPlugins.ShakeItV3.EffectsContainers.EffectsContainerBase simHubEffectsContainer)
        {
            if (simHubEffectsContainer is DataPlugins.ShakeItV3.EffectsContainers.GroupContainer simHubGroupContainer)
            {
                var groupContainer = new GroupContainer(parent, simHubGroupContainer);
                return groupContainer;
            }

            var effectsContainerBase = new EffectsContainerBase(parent, simHubEffectsContainer);
            return effectsContainerBase;
        }
    }
}