// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.PreCommon.Ui.Util;

namespace SimHub.Plugins.ComputedProperties.Ui
{
    public class EditScriptWindowViewModel : ObservableObject
    {
        private readonly IComputedPropertiesManager _computedPropertiesManager;

        /// <summary>
        /// <c>ExpressionValue</c> allows us to get the syntax highlighting, which is partly "internal".
        /// </summary>
        public ExpressionValue Formula { get; } = new ExpressionValue { Interpreter = Interpreter.Javascript };

        private readonly ScriptData _scriptData;

        public string Name
        {
            get => _scriptData.Name;
            set
            {
                _scriptData.Name = value;
                UpdateButtonState();
            }
        }

        private TextDocument _script = new TextDocument();

        public TextDocument Script
        {
            get => _script;
            set => SetProperty(ref _script, value);
        }

        public ICommand InsertSampleCommand { get; }

        private bool _isOkEnabled;

        public bool IsOkEnabled
        {
            get => _isOkEnabled;
            set => SetProperty(ref _isOkEnabled, value);
        }

        private string _problems = string.Empty;

        public string Problems
        {
            get => _problems;
            set => SetProperty(ref _problems, value);
        }

        private CancellationTokenSource _debounceTokenSource;

        public EditScriptWindowViewModel(IComputedPropertiesManager computedPropertiesManager, ScriptData scriptData)
        {
            _computedPropertiesManager = computedPropertiesManager;
            _scriptData = scriptData;
            InsertSampleCommand = new RelayCommand<object>(_ => InsertSample());

            Script = new TextDocument(scriptData.Script);
            UpdateButtonState();
        }

        /// <summary>
        /// Default constructor for UI design only
        /// </summary>
        public EditScriptWindowViewModel()
        {
        }

        public ScriptData GetScriptData()
        {
            _scriptData.Script = Script.Text;
            return _scriptData;
        }

        private void UpdateButtonState()
        {
            IsOkEnabled = !string.IsNullOrWhiteSpace(Name);
        }

        /// <summary>
        /// Inserts a minimal, functioning script into the editor.
        /// </summary>
        private void InsertSample()
        {
            Script = new TextDocument(@"
/** Initialization. Only called by the plugin once for each script. */
function init(context) {
  // create a new property in SimHub
  createProperty('MyNewDateAndTime');
  // instruct the plugin to call 'calculate' (see below) whenever 'DataCorePlugin.CurrentDateTime' changes
  subscribe(context, 'DataCorePlugin.CurrentDateTime', 'calculate');
}

/** Called by the plugin whenever a corresponding property value (subscription) has changed. */
function calculate() {
  // set the value of the computed property.
  setPropertyValue('MyNewDateAndTime', 'Time and date: ' + $prop('DataCorePlugin.CurrentDateTime'));
}
");
        }

        /// <summary>
        /// Uses debouncing to validate the script code.
        /// </summary>
        public void OnScriptChanged(string code)
        {
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), token);
                    if (!token.IsCancellationRequested && _computedPropertiesManager != null && !string.IsNullOrWhiteSpace(code))
                    {
                        try
                        {
                            _computedPropertiesManager.ValidateScript(code);
                            Problems = string.Empty;
                        }
                        catch (Exception e)
                        {
                            Problems = e.Message;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignore cancellation
                }
            }, token);
       }
    }
}