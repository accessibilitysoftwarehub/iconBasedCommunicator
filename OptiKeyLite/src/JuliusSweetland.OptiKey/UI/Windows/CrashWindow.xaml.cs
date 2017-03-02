﻿using System;
using System.Windows;
using System.Windows.Threading;
using OptiKey.Properties;

namespace OptiKey.UI.Windows
{
    /// <summary>
    /// Interaction logic for CrashWindow.xaml
    /// </summary>
    public partial class CrashWindow : Window
    {
        public CrashWindow()
        {
            InitializeComponent();

            this.Loaded += (sender, args) =>
            {
                var dt = new DispatcherTimer { Interval = new TimeSpan(0, 0, Settings.Default.AutoCloseCrashMessageSeconds) };
                dt.Tick += (o, eventArgs) =>
                {
                    this.Close();
                };
                dt.Start();
            };
        }
    }
}
