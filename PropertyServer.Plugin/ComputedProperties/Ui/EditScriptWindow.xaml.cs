// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Windows;
using System.Windows.Forms;
using SimHub.Plugins.OutputPlugins.Dash.WPFUI;

namespace SimHub.Plugins.ComputedProperties.Ui
{
    public partial class EditScriptWindow
    {

        public EditScriptWindow()
        {
            InitializeComponent();
            ShowOk = true;
            ShowCancel = true;
        }

        private EditScriptWindowViewModel ViewModel => (EditScriptWindowViewModel)DataContext;

        public override string Title => "Edit script";

        private void TextEditor_OnTextChanged(object sender, EventArgs e)
        {
            ViewModel.OnScriptChanged(CodeEditor.Text);
        }

        private async void InsertProperty_Click(object sender, RoutedEventArgs e)
        {
            var pp = new PropertiesPicker();
            var result = await pp.ShowDialogAsync(this);
            if (result == DialogResult.OK)
            {
                if (pp.Result != null)
                {
                    CodeEditor.SelectedText = $"'{pp.Result.GetPropertyName()}'";
                }
            }
        }
    }
}