﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OptiKey.UI.ValueConverters
{
    public class ObjectIsType : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var paramType = parameter as Type;
            if(value == DependencyProperty.UnsetValue || value == null || paramType == null) return false;
            return paramType.IsInstanceOfType(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
