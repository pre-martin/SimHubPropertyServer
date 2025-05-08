// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using SimHub.Plugins.DataPlugins.ShakeItV3.Settings;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// Wrapper for the SimHub class "ShakeItProfile".
    /// </summary>
    /// <remarks>
    /// Modifications to the structure are not supported! Most properties are read-only.
    /// </remarks>
    public class Profile : TreeElement
    {
        private readonly ShakeItProfile _simHubShakeItProfile;
        private readonly List<EffectsContainerBase> _effectsContainers = new List<EffectsContainerBase>();

        public Profile(ShakeItProfile simHubShakeItProfile) : base(null)
        {
            _simHubShakeItProfile = simHubShakeItProfile;
            Converter.Convert(this, simHubShakeItProfile.EffectsContainers, _effectsContainers);
        }

        public Guid ProfileId
        {
            get => _simHubShakeItProfile.ProfileId;
            set => _simHubShakeItProfile.ProfileId = value;
        }

        public string Name => _simHubShakeItProfile.Name;

        public IList<EffectsContainerBase> EffectsContainers => _effectsContainers.AsReadOnly();

        public override string RecursiveName => Name;
    }
}