// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Windows;

namespace SimHub.Plugins.PropertyServer.Ui
{
    public partial class SettingsControl
    {
        public SettingsControl()
        {
            InitializeComponent();
        }

        private void RepairButton_Click(object sender, RoutedEventArgs e)
        {
            var repairShakeItWindow = new RepairShakeItWindow();
            Configuration.ShowChildWindow(this, repairShakeItWindow, null);
        }
    }
}