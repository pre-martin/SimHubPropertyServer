// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using SimHub.Plugins.Styles;

namespace SimHub.Plugins.PropertyServer.Ui
{
    public partial class SettingsControl
    {
        public SettingsControl()
        {
            InitializeComponent();
        }

        private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

        private void RepairButton_Click(object sender, RoutedEventArgs e)
        {
            var repairShakeItWindow = new RepairShakeItWindow();
            Configuration.ShowChildWindow(this, repairShakeItWindow, null);
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await SHMessageBox.Show(
                $"This will download a new version of the plugin.\n" +
                "SimHub will restart automatically after the update.\n" +
                "The release notes can be found at https://github.com/pre-martin/SimHubPropertyServer/releases.\n\n" +
                "Be sure to check if there is also a new version of the Stream Deck plugin available!",
                "Confirm download", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (result != DialogResult.OK) return;

            await ViewModel.Update();
        }

        private async void SecretArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                await ViewModel.CheckForNewVersion(true);
            }
        }
    }
}