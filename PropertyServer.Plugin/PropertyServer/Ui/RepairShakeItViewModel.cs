// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using SimHub.Plugins.PreCommon.Ui.Util;
using SimHub.Plugins.PropertyServer.ShakeIt;

namespace SimHub.Plugins.PropertyServer.Ui
{
    /// <summary>
    /// ViewModel for the "Repair ShakeIt" view.
    /// </summary>
    public class RepairShakeItViewModel : ObservableObject
    {
        public ShakeItAccessor ShakeItAccessor { get; set; }
        public ICommand ScanShakeItBassCommand { get; }
        public ICommand ScanShakeItMotorsCommand { get; }
        public ICommand RepairCommand { get; }
        public bool ShowScanHint => Profiles == null;
        public bool ShowDuplicatesList => Profiles != null && Profiles.Count > 0;
        public bool ShowNoResults => Profiles != null && Profiles.Count == 0;

        private ObservableCollection<ProfileHolder> _profiles;
        private int _currentMode = 1; // 1 = Bass, 2 = Motors

        public ObservableCollection<ProfileHolder> Profiles
        {
            get => _profiles;
            private set
            {
                SetProperty(ref _profiles, value);
                OnPropertyChanged(nameof(ShowScanHint));
                OnPropertyChanged(nameof(ShowDuplicatesList));
                OnPropertyChanged(nameof(ShowNoResults));
            }
        }

        public RepairShakeItViewModel()
        {
            ScanShakeItBassCommand = new RelayCommand<object>(o => FindShakeItBassDuplicates());
            ScanShakeItMotorsCommand = new RelayCommand<object>(o => FindShakeItMotorsDuplicates());
            RepairCommand = new RelayCommand<object>(
                e => Profiles != null && Profiles.Any(p => p.IsChecked),
                o => Repair()
            );
        }

        private void FindShakeItBassDuplicates()
        {
            FindShakeItDuplicates(ShakeItAccessor.BassProfiles());
            _currentMode = 1;
        }

        private void FindShakeItMotorsDuplicates()
        {
            FindShakeItDuplicates(ShakeItAccessor.MotorsProfiles());
            _currentMode = 2;
        }

        private void FindShakeItDuplicates(ICollection<Profile> profiles)
        {
            var tempProfiles = new ObservableCollection<ProfileHolder>();
            foreach (var profile in profiles)
            {
                var guidToEffects = ShakeItAccessor.GroupEffectsByGuid(profile);
                var duplicates = guidToEffects.Where(kv => kv.Value.Count > 1).SelectMany(kv => kv.Value).ToList();
                if (duplicates.Count > 0)
                {
                    var profileHolder = new ProfileHolder(profile.Name, guidToEffects, duplicates);
                    tempProfiles.Add(profileHolder);
                }
            }

            Profiles = tempProfiles;
        }

        private void Repair()
        {
            var profile = Profiles?.FirstOrDefault(p => p.IsChecked);
            if (profile == null) return;

            var guidToEffects = profile.GuidToEffects;
            foreach (var kv in guidToEffects.Where(kv => kv.Value.Count > 1))
            {
                for (var i = 1; i < kv.Value.Count; i++)
                {
                    kv.Value[i].ContainerId = Guid.NewGuid();
                }
            }

            if (_currentMode == 1) FindShakeItBassDuplicates();
            else FindShakeItMotorsDuplicates();
        }
    }

    public class ProfileHolder
    {
        public Dictionary<Guid, List<EffectsContainerBase>> GuidToEffects { get; }

        public ProfileHolder(string name, Dictionary<Guid, List<EffectsContainerBase>> guidToEffects, List<EffectsContainerBase> duplicates)
        {
            Name = name;
            GuidToEffects = guidToEffects;

            var duplicatesHolder = duplicates.Select(ecb => new EffectsHolder(ecb)).ToList();
            var duplicatesCollectionView = CollectionViewSource.GetDefaultView(duplicatesHolder);
            duplicatesCollectionView.GroupDescriptions.Add(new PropertyGroupDescription("ContainerId"));
            Duplicates = duplicatesCollectionView;
        }

        public bool IsChecked { get; set; }

        public string Name { get; set; }

        public ICollectionView Duplicates { get; set; }
    }

    public class EffectsHolder
    {
        private readonly EffectsContainerBase _effectsContainerBase;

        public EffectsHolder(EffectsContainerBase effectsContainerBase)
        {
            _effectsContainerBase = effectsContainerBase;
        }

        public Guid ContainerId => _effectsContainerBase.ContainerId;

        public string RecursiveName => _effectsContainerBase.RecursiveName;
    }
}