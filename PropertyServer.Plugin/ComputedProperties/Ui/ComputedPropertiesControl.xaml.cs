// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using SimHub.Plugins.UI;

namespace SimHub.Plugins.ComputedProperties.Ui
{
    public partial class ComputedPropertiesControl
    {
        public ComputedPropertiesControl()
        {
            InitializeComponent();
        }

        private async void NewScript_Click(object sender, RoutedEventArgs e)
        {
            var myContext = (ComputedPropertiesViewModel)DataContext;
            var scriptData = new ScriptData();
            var editWindow = new EditScriptWindow
            {
                DataContext = new EditScriptWindowViewModel(myContext.ComputedPropertiesManager, scriptData)
            };

            var result = await editWindow.ShowDialogWindowAsync(this, DialogOptions.Resizable, 800, 800);
            if (result == DialogResult.OK)
            {
                myContext.Scripts.Add(((EditScriptWindowViewModel)editWindow.DataContext).GetScriptData());
            }
        }

        private async void Entry_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = ((ComputedPropertiesViewModel)DataContext).SelectedScript;
            if (selectedItem == null) return;

            var myContext = (ComputedPropertiesViewModel)DataContext;
            var editWindow = new EditScriptWindow
            {
                // Create a clone of ScriptData, so that the editor does not work on the original data.
                DataContext = new EditScriptWindowViewModel(myContext.ComputedPropertiesManager, selectedItem.Clone())
            };
            var result = await editWindow.ShowDialogWindowAsync(this, DialogOptions.Resizable, 800, 800);
            if (result == DialogResult.OK)
            {
                // OK: Copy all data from the dialog (view model) into the underlying DataContext.
                var scriptData = ((EditScriptWindowViewModel)editWindow.DataContext).GetScriptData();
                var existingEntry= myContext.Scripts.SingleOrDefault(data => data.Guid == scriptData.Guid);
                if (existingEntry != null)
                {
                    existingEntry.Name = scriptData.Name;
                    existingEntry.Script = scriptData.Script;
                    existingEntry.Reset();
                }
            }
        }
    }
}