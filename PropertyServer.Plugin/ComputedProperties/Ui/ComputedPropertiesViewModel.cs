// Copyright (C) 2026 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using SimHub.Plugins.PreCommon.Ui.Util;

namespace SimHub.Plugins.ComputedProperties.Ui
{
    public class ComputedPropertiesViewModel : ObservableObject
    {
        public ComputedPropertiesViewModel(ObservableCollection<ScriptData> scripts, IScriptValidator scriptValidator)
        {
            Scripts = scripts;
            ScriptValidator = scriptValidator;
            FilteredScripts = CollectionViewSource.GetDefaultView(Scripts);
            FilteredScripts.Filter = FilterScripts;
        }

        /// <summary>
        /// For IDE only
        /// </summary>
        public ComputedPropertiesViewModel() : this(new ObservableCollection<ScriptData>(), null)
        {
        }

        public IScriptValidator ScriptValidator { get; }

        public ObservableCollection<ScriptData> Scripts { get; }

        public ICollectionView FilteredScripts { get; }

        private string _filterText = string.Empty;

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    FilteredScripts.Refresh();
                }
            }
        }

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

        public void AddScript(ScriptData scriptData)
        {
            // Insert at the correct sorted position
            var index = 0;
            while (index < Scripts.Count &&
                   string.Compare(Scripts[index].Name, scriptData.Name, StringComparison.OrdinalIgnoreCase) < 0)
            {
                index++;
            }

            Scripts.Insert(index, scriptData);
            SelectedScript = scriptData;
        }

        public void UpdateScript(ScriptData scriptData)
        {
            // Remove and re-insert to update position if name changed
            if (Scripts.Contains(scriptData))
            {
                Scripts.Remove(scriptData);
                AddScript(scriptData);
                SelectedScript = scriptData;
            }
        }

        private bool FilterScripts(object obj)
        {
            if (string.IsNullOrWhiteSpace(_filterText)) return true;
            return obj is ScriptData script &&
                   script.Name.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}