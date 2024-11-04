﻿using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FanvilMonitoring.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
            return booleanValue ? Brushes.GreenYellow : Brushes.Gray;
        return Brushes.GreenYellow;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}