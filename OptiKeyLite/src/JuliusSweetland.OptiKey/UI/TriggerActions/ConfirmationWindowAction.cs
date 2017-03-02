﻿using System;
using System.Windows;
using System.Windows.Interactivity;
using OptiKey.UI.Utilities;
using OptiKey.UI.Windows;
using log4net;
using Prism.Interactivity.InteractionRequest;

namespace OptiKey.UI.TriggerActions
{
    public class ConfirmationWindowAction : TriggerAction<FrameworkElement>
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected override void Invoke(object parameter)
        {
            var args = parameter as InteractionRequestedEventArgs;
            if (args != null)
            {
                Log.Info("Opening confirmation window");
                var childWindow = new ConfirmationWindow { DataContext = args.Context };

                EventHandler closeHandler = null;
                closeHandler = (sender, e) =>
                {
                    childWindow.Closed -= closeHandler;
                    args.Callback();
                };
                childWindow.Closed += closeHandler;

                childWindow.Owner = AssociatedObject != null
                    ? AssociatedObject as Window ?? VisualAndLogicalTreeHelper.FindVisualParent<Window>(AssociatedObject)
                    : childWindow.Owner;

                childWindow.ShowDialog();
            }
        }
    }
}
