// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using SimHub.Plugins.Styles;
using SimHub.Plugins.UI;

namespace SimHub.Plugins.ComputedProperties.Ui
{
    public partial class ComputedPropertiesControl
    {
        public ComputedPropertiesControl()
        {
            InitializeComponent();
        }

        private ComputedPropertiesViewModel ViewModel => (ComputedPropertiesViewModel)DataContext;

        private async void NewScript_Click(object sender, RoutedEventArgs e)
        {
            var scriptData = new ScriptData();
            var editWindow = new EditScriptWindow
            {
                DataContext = new EditScriptWindowViewModel(ViewModel.ComputedPropertiesManager, scriptData)
            };

            var result = await editWindow.ShowDialogWindowAsync(this, DialogOptions.Resizable, 1000, 800);
            if (result == DialogResult.OK)
            {
                ViewModel.Scripts.Add(((EditScriptWindowViewModel)editWindow.DataContext).GetScriptData());
            }
        }

        private async void Entry_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = ViewModel.SelectedScript;
            if (selectedItem == null) return;

            var myContext = (ComputedPropertiesViewModel)DataContext;
            var editWindow = new EditScriptWindow
            {
                // Create a clone of ScriptData, so that the editor does not work on the original data.
                DataContext = new EditScriptWindowViewModel(myContext.ComputedPropertiesManager, selectedItem.Clone())
            };
            var result = await editWindow.ShowDialogWindowAsync(this, DialogOptions.Resizable, 1000, 800);
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

        private async void Entry_DeleteClick(object sender, RoutedEventArgs e)
        {
            var scriptData = ((FrameworkElement)sender).DataContext as ScriptData;
            if (scriptData == null) return;

            var linesOfCode = string.IsNullOrWhiteSpace(scriptData?.Script)
                ? 0
                : scriptData.Script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;

            var result = await SHMessageBox.Show($"Are you sure you want to delete the script \n\"{scriptData.Name}\"? \nIt has {linesOfCode} lines of code.", "Confirm delete",
                MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result != DialogResult.OK) return;
            ViewModel.DeleteScript(scriptData);
        }
    }
}