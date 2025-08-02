// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SimHub.Plugins.PropertyServer.Ui
{
    public class IpPortValidationRule : ValidationRule
    {
        // regex to match:
        // - 1 to 4 digit inputs (may start with 1-9)
        // - 5 digit inputs (may start with 1-5)
        // - special cases for 65, 655 and 6553
        private readonly Regex _portRegex =
            new Regex("^([1-9][0-9]{0,3}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$");


        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return _portRegex.IsMatch(value as string ?? string.Empty)
                ? new ValidationResult(true, null)
                : new ValidationResult(false, "Only 1 to 65535 is valid");
        }
    }
}