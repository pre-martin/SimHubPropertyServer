// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Windows;
using SimHub.Plugins.PropertyServer.ShakeIt;

namespace SimHub.Plugins.PropertyServer.Ui
{
    public partial class RepairShakeItWindow
    {
        public RepairShakeItWindow(PluginManager pluginManager)
        {
            var shakeItBassAccessor = new ShakeItAccessor();
            shakeItBassAccessor.Init(pluginManager);

            InitializeComponent();
            ((RepairShakeItViewModel)DataContext).ShakeItAccessor = shakeItBassAccessor;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}