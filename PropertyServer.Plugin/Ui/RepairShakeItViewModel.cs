using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using SimHub.Plugins.PropertyServer.ShakeIt;
using SimHub.Plugins.PropertyServer.Ui.Util;

namespace SimHub.Plugins.PropertyServer.Ui
{
    /// <summary>
    /// ViewModel for the "Repair ShakeIt" view.
    /// </summary>
    public class RepairShakeItViewModel : NotifyPropertyChanges
    {
        public ShakeItBassAccessor ShakeItBassAccessor { get; set; }
        public ICommand ScanShakeItBassCommand { get; }
        public ICommand RepairCommand { get; }
        public bool ShowDuplicatesList => Duplicates == null || !Duplicates.IsEmpty;

        private Dictionary<Guid, List<EffectsContainerBase>> _guidToEffectsData;
        private ICollectionView _duplicates;

        public ICollectionView Duplicates
        {
            get => _duplicates;
            private set
            {
                SetField(ref _duplicates, value);
                OnPropertyChanged(nameof(ShowDuplicatesList));
            }
        }

        public RepairShakeItViewModel()
        {
            ScanShakeItBassCommand = new RelayCommand<object>(o => FindShakeItBassDuplicates());
            RepairCommand = new RelayCommand<object>(e => Duplicates != null && !Duplicates.IsEmpty, o => Repair());
        }

        private void FindShakeItBassDuplicates()
        {
            _guidToEffectsData = ShakeItBassAccessor.GroupEffectsByGuid();

            IList<EffectsContainerBase> duplicates =
                _guidToEffectsData.Where(pair => pair.Value.Count > 1).SelectMany(pair => pair.Value).ToList();

            var duplicatesCollectionView = CollectionViewSource.GetDefaultView(duplicates);
            duplicatesCollectionView.GroupDescriptions.Add(new PropertyGroupDescription("ContainerId"));
            Duplicates = duplicatesCollectionView;
        }

        private void Repair()
        {
            if (_guidToEffectsData == null) return;

            foreach (var pair in _guidToEffectsData.Where(pair => pair.Value.Count > 1))
            {
                for (var i = 1; i < pair.Value.Count; i++)
                {
                    pair.Value[i].ContainerId = Guid.NewGuid();
                }
            }

            FindShakeItBassDuplicates();
        }
    }
}