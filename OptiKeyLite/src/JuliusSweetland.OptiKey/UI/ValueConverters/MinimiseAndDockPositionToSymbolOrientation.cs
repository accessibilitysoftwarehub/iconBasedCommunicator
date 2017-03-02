﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using OptiKey.Enums;

namespace OptiKey.UI.ValueConverters
{
    public class MinimiseAndDockPositionToSymbolOrientation : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2
                && values.All(v => v != DependencyProperty.UnsetValue))
            {
                var minimisedPosition = (MinimisedEdges)values[0];
                var dockPosition = (DockEdges)values[1];

                switch (minimisedPosition == MinimisedEdges.SameAsDockedPosition ? dockPosition.ToMinimisedEdge() : minimisedPosition)
                {
                    case MinimisedEdges.Right:
                        return SymbolOrientations.Right;

                    case MinimisedEdges.Bottom:
                        return SymbolOrientations.Bottom;

                    case MinimisedEdges.Left:
                        return SymbolOrientations.Left;

                    default: //case MinimisedEdges.Top:
                        return SymbolOrientations.Top;
                }
            }
            
            return SymbolOrientations.Top; //Default
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
