﻿using System;
using System.Reactive;
using System.Windows;
using EyeXFramework;
using OptiKey.Enums;
using log4net;
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using OptiKey.Properties;

namespace OptiKey.Services
{
    public class TobiiEyeXPointService : IPointService
    {
        #region Fields

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private GazePointDataStream gazeDataStream;
        private FixationDataStream fixationDataStream;

        private event EventHandler<Timestamped<Point>> pointEvent;

        #endregion

        #region Ctor

        public TobiiEyeXPointService()
        {
            EyeXHost = new EyeXHost();

            //Disconnect (deactivate) from the TET server on shutdown - otherwise the process can hang
            Application.Current.Exit += (sender, args) =>
            {
                if (EyeXHost != null)
                {
                    Log.Info("Disposing of the EyeXHost.");
                    EyeXHost.Dispose();
                }
            };
        }

        #endregion

        #region Properties

        public EyeXHost EyeXHost { get; private set; }

        #endregion

        #region Events

        public event EventHandler<Exception> Error;

        public event EventHandler<Timestamped<Point>> Point
        {
            add
            {
                if (pointEvent == null)
                {
                    Log.Info("Checking the state of the Tobii service...");

                    switch (EyeXHost.EyeXAvailability)
                    {
                        case EyeXAvailability.NotAvailable:
                            PublishError(this, new ApplicationException(Resources.TOBII_EYEX_ENGINE_NOT_FOUND));
                            return;

                        case EyeXAvailability.NotRunning:
                            PublishError(this, new ApplicationException(Resources.TOBII_EYEX_ENGINE_NOT_RUNNING));
                            return;
                    }

                    Log.Info("Attaching eye tracking device status changed listener to the Tobii service.");

                    EyeXHost.EyeTrackingDeviceStatusChanged += (s, e) => Log.InfoFormat("Tobii EyeX tracking device status changed to {0} (IsValid={1})", e, e.IsValid);

                    if (Settings.Default.TobiiEyeXProcessingLevel == DataStreamProcessingLevels.None ||
                       Settings.Default.TobiiEyeXProcessingLevel == DataStreamProcessingLevels.Low)
                    {
                        gazeDataStream = EyeXHost.CreateGazePointDataStream(
                            Settings.Default.TobiiEyeXProcessingLevel == DataStreamProcessingLevels.None
                                ? GazePointDataMode.Unfiltered //None
                                : GazePointDataMode.LightlyFiltered); //Low

                        EyeXHost.Start(); // Start the EyeX host

                        gazeDataStream.Next += (s, data) =>
                        {
                            if (pointEvent != null
                                && !double.IsNaN(data.X)
                                && !double.IsNaN(data.Y))
                            {
                                pointEvent(this, new Timestamped<Point>(new Point(data.X, data.Y),
                                    new DateTimeOffset(DateTime.UtcNow).ToUniversalTime())); //EyeX does not publish a useable timestamp
                            }
                        };
                    }
                    else
                    {
                        fixationDataStream = EyeXHost.CreateFixationDataStream(
                            Settings.Default.TobiiEyeXProcessingLevel == DataStreamProcessingLevels.Medium
                                ? FixationDataMode.Sensitive //Medium
                                : FixationDataMode.Slow); //Hight

                        EyeXHost.Start(); // Start the EyeX host

                        fixationDataStream.Next += (s, data) =>
                        {
                            if (pointEvent != null
                                && !double.IsNaN(data.X)
                                && !double.IsNaN(data.Y))
                            {
                                pointEvent(this, new Timestamped<Point>(new Point(data.X, data.Y),
                                    new DateTimeOffset(DateTime.UtcNow).ToUniversalTime())); //EyeX does not publish a useable timestamp
                            }
                        };
                    }
                }

                pointEvent += value;
            }
            remove
            {
                pointEvent -= value;

                if (pointEvent == null)
                {
                    Log.Info("Last listener of Point event has unsubscribed. Disposing gaze data stream.");
                    gazeDataStream.Dispose();
                }
            }
        }

        #endregion

        #region Publish Error

        private void PublishError(object sender, Exception ex)
        {
            Log.Error("Publishing Error event (if there are any listeners)", ex);
            if (Error != null)
            {
                Error(sender, ex);
            }
        }

        #endregion
    }
}