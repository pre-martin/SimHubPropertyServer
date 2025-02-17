// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Collections.ObjectModel;
using SimHub.Plugins.PreCommon.Ui.Util;

namespace SimHub.Plugins.ComputedProperties.Ui
{
    public class ComputedPropertiesViewModel : ObservableObject
    {
        public ComputedPropertiesViewModel(ObservableCollection<ScriptData> scripts,
            IComputedPropertiesManager computedPropertiesManager)
        {
            Scripts = scripts;
            ComputedPropertiesManager = computedPropertiesManager;
        }

        /// <summary>
        /// For IDE only
        /// </summary>
        public ComputedPropertiesViewModel() : this(new ObservableCollection<ScriptData>(), null)
        {
        }

        public IComputedPropertiesManager ComputedPropertiesManager { get; }

        public ObservableCollection<ScriptData> Scripts { get; set; }

        private ScriptData _selectedScript;

        public ScriptData SelectedScript
        {
            get => _selectedScript;
            set => SetProperty(ref _selectedScript, value);
        }

        public void DeleteScript(ScriptData scriptData)
        {
            Scripts.Remove(scriptData);
        }
    }
}