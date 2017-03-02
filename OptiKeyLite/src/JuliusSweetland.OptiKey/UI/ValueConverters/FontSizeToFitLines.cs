﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace OptiKey.UI.ValueConverters
{
    public class FontSizeToFitLines : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null 
                && values.Count() == 3
                && values.All(v => v != DependencyProperty.UnsetValue))
            {
                var noOfLines = (int)values[0];
                var height = (double)values[1];
                var fontFamily = (FontFamily)values[2];

                var lineHeight = height/noOfLines;

                return lineHeight / fontFamily.LineSpacing;
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
