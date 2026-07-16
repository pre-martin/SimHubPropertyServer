// Copyright (C) 2026 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Diagnostics;
using System.Windows.Navigation;

namespace SimHub.Plugins.PropertyServer.Ui
{
    public partial class UpdateConfirmWindow
    {
        public event System.EventHandler UpdateConfirmed;

        public UpdateConfirmWindow()
        {
            InitializeComponent();
        }

        private void UpdateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateConfirmed?.Invoke(this, System.EventArgs.Empty);
            Close(null);
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close(null);
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true,
            });
            e.Handled = true;
        }
    }
}
