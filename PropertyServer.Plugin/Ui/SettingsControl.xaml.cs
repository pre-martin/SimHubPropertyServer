// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

namespace SimHub.Plugins.PropertyServer.Ui
{
    public partial class SettingsControl
    {
        public PluginManager PluginManager { get; set; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        private void RepairButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var repairShakeItWindow = new RepairShakeItWindow(PluginManager);
            Configuration.ShowChildWindow(this, repairShakeItWindow, null);
        }
    }
}