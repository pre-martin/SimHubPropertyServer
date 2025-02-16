// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;

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

        public override string Title => "Edit script";

        private void TextEditor_OnTextChanged(object sender, EventArgs e)
        {
            if (DataContext is EditScriptWindowViewModel viewModel)
            {
                viewModel.OnScriptChanged(CodeEditor.Text);
            }
        }
    }
}