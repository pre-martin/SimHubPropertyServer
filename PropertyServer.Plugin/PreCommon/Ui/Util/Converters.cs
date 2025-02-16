using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimHub.Plugins.PreCommon.Ui.Util
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = value != null && (bool)value;
            boolValue = (parameter != null && parameter.ToString().ToLower() == "negate") ? !boolValue : boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}