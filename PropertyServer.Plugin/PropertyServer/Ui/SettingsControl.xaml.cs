// Copyright (C) 2026 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Windows;
using System.Windows.Input;

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

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var updateConfirmWindow = new UpdateConfirmWindow();
            updateConfirmWindow.UpdateConfirmed += async (dialogSender, dialogArgs) =>
            {
                await ViewModel.Update();
            };

            Configuration.ShowChildWindow(this, updateConfirmWindow, null);
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