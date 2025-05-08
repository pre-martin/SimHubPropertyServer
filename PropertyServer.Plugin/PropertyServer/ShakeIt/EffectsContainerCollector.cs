// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// Helper class to collect ShakeIt effect groups and effects by given criteria.
    /// </summary>
    public class EffectsContainerCollector
    {
        private Dictionary<Guid, List<EffectsContainerBase>> GuidToEffect { get; } = new Dictionary<Guid, List<EffectsContainerBase>>();

        /// <summary>
        /// Groups all effect groups and effects of a given profile by their Guid.
        /// </summary>
        /// <remarks>
        /// In theory, there should be no duplicates (i.e. more than one element for a given Guid). Practically, this happens.
        /// </remarks>
        public Dictionary<Guid, List<EffectsContainerBase>> ByGuid(Profile profile)
        {
            CollectByGuid(profile.EffectsContainers);
            return GuidToEffect;
        }

        private void CollectByGuid(IEnumerable<EffectsContainerBase> effectsContainers)
        {
            foreach (var effectsContainer in effectsContainers)
            {
                if (!GuidToEffect.ContainsKey(effectsContainer.ContainerId))
                {
                    GuidToEffect[effectsContainer.ContainerId] = new List<EffectsContainerBase>();
                }

                GuidToEffect[effectsContainer.ContainerId].Add(effectsContainer);

                if (effectsContainer is GroupContainer groupContainer)
                {
                    CollectByGuid(groupContainer.EffectsContainers);
                }
            }
        }
    }
}