// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SimHub.Plugins.PropertyServer.Ui
{
    /// <summary>
    /// Takes two booleans (<c>IsNewVersionAvailable</c> and <c>IsVersionCheckError</c>) and determines the background color
    /// for the "new version" label.
    /// </summary>
    public class VersionCheckMessageBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return Brushes.Transparent;
            }

            var isNewVersionAvailable = values[0] is bool b1 && b1;
            var isVersionCheckError = values[1] is bool b2 && b2;
            if (isVersionCheckError)
            {
                return Brushes.Transparent;
            }

            return isNewVersionAvailable
                ? new SolidColorBrush(Color.FromRgb(170, 80, 80))
                : new SolidColorBrush(Color.FromRgb(30, 120, 30));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}