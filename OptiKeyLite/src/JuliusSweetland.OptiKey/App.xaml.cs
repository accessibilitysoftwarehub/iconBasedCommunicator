﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using OptiKey.Enums;
using OptiKey.Extensions;
using OptiKey.Models;
using OptiKey.Observables.PointSources;
using OptiKey.Observables.TriggerSources;
using OptiKey.Properties;
using OptiKey.Services;
using OptiKey.Static;
using OptiKey.UI.ViewModels;
using OptiKey.UI.Windows;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using NBug.Core.UI;
using Octokit;
using Octokit.Reactive;
using Application = System.Windows.Application;
using FileMode = System.IO.FileMode;
using System.Drawing;
using System.Windows.Forms;

namespace OptiKey
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constants

        private const string GazeTrackerUdpRegex = @"^STREAM_DATA\s(?<instanceTime>\d+)\s(?<x>-?\d+(\.[0-9]+)?)\s(?<y>-?\d+(\.[0-9]+)?)";
        private const string GitHubRepoName = "optikey";
        private const string GitHubRepoOwner = "optikey";

        #endregion

        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Action applyTheme;
        private MainWindow mainWindow;

        #endregion

        #region Ctor

        public App()
        {
            //Setup unhandled exception handling and NBug
            AttachUnhandledExceptionHandlers();

            //Log startup diagnostic info
            Log.Info("STARTING UP.");
            LogDiagnosticInfo();

            //Attach shutdown handler
            Current.Exit += (o, args) =>
            {
                Log.Info("PERSISTING USER SETTINGS AND SHUTTING DOWN.");
                Settings.Default.Save();
            };

            HandleCorruptSettings();

            //Upgrade settings (if required) - this ensures that user settings migrate between version changes
            if (Settings.Default.SettingsUpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.SettingsUpgradeRequired = false;
                Settings.Default.Save();
                Settings.Default.Reload();
            }

            //Adjust log4net logging level if in debug mode
            ((Hierarchy)LogManager.GetRepository()).Root.Level = Settings.Default.Debug ? Level.Debug : Level.Info;
            ((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);

            //Apply resource language (and listen for changes)
            Action<Languages> applyResourceLanguage = language => OptiKey.Properties.Resources.Culture = language.ToCultureInfo();
            Settings.Default.OnPropertyChanges(s => s.UiLanguage).Subscribe(applyResourceLanguage);
            applyResourceLanguage(Settings.Default.UiLanguage);

            //Logic to initially apply the theme and change the theme on setting changes
            applyTheme = () =>
            {
                var themeDictionary = new ThemeResourceDictionary
                {
                    Source = new Uri(Settings.Default.Theme, UriKind.Relative)
                };
                
                var previousThemes = Resources.MergedDictionaries
                    .OfType<ThemeResourceDictionary>()
                    .ToList();
                    
                //N.B. Add replacement before removing the previous as having no applicable resource
                //dictionary can result in the first element not being rendered (usually the first key).
                Resources.MergedDictionaries.Add(themeDictionary);
                previousThemes.ForEach(rd => Resources.MergedDictionaries.Remove(rd));
            };
            
            Settings.Default.OnPropertyChanges(settings => settings.Theme).Subscribe(_ => applyTheme());
        }

        #endregion

        #region On Startup

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                Log.Info("Boot strapping the services and UI.");

                //Apply theme
                applyTheme();
                
                //Define MainViewModel before services so I can setup a delegate to call into the MainViewModel
                //This is to work around the fact that the MainViewModel is created after the services.
                MainViewModel mainViewModel = null;
                Action<KeyValue> fireKeySelectionEvent = kv =>
                {
                    if (mainViewModel != null) //Access to modified closure is a good thing here, for once!
                    {
                        mainViewModel.FireKeySelectionEvent(kv);
                    }
                };

                //Create services
                var errorNotifyingServices = new List<INotifyErrors>();
                IAudioService audioService = new AudioService();
                IDictionaryService dictionaryService = new DictionaryService(Settings.Default.AutoCompleteMethod);
                IPublishService publishService = new PublishService();
                ISuggestionStateService suggestionService = new SuggestionStateService();
                ICalibrationService calibrationService = CreateCalibrationService();
                ICapturingStateManager capturingStateManager = new CapturingStateManager(audioService);
                ILastMouseActionStateManager lastMouseActionStateManager = new LastMouseActionStateManager();
                IKeyStateService keyStateService = new KeyStateService(suggestionService, capturingStateManager, lastMouseActionStateManager, calibrationService, fireKeySelectionEvent);
                IInputService inputService = CreateInputService(keyStateService, dictionaryService, audioService, calibrationService, capturingStateManager, errorNotifyingServices);
                IKeyboardOutputService keyboardOutputService = new KeyboardOutputService(keyStateService, suggestionService, publishService, dictionaryService, fireKeySelectionEvent);
                IMouseOutputService mouseOutputService = new MouseOutputService(publishService);
                errorNotifyingServices.Add(audioService);
                errorNotifyingServices.Add(dictionaryService);
                errorNotifyingServices.Add(publishService);
                errorNotifyingServices.Add(inputService);

                //Release keys on application exit
                ReleaseKeysOnApplicationExit(keyStateService, publishService);                

                //Compose UI
                mainWindow = new MainWindow(audioService, dictionaryService, inputService, keyStateService);               

                //for testing just hard code the size and position of the mainWindow    

                //get the size of the screen
                //Rectangle screenSize = mainWindow.GetScreen().Bounds;
                //double windowWidth = screenSize.Width;
                //double windowHeight = screenSize.Height * 0.35;

                //Rect windowDimensions = new Rect(0.00, 0.00, windowWidth, windowHeight);               
                //Settings.Default.MainWindowFloatingSizeAndPosition = windowDimensions;
                Settings.Default.MainWindowDockPosition = DockEdges.Bottom;
                Settings.Default.MainWindowState = WindowStates.Docked;                
                
                IWindowManipulationService mainWindowManipulationService = new WindowManipulationService(
                    mainWindow,
                    () => Settings.Default.MainWindowOpacity,
                    () => Settings.Default.MainWindowState,
                    () => Settings.Default.MainWindowPreviousState,
                    () => Settings.Default.MainWindowFloatingSizeAndPosition,                    
                    () => Settings.Default.MainWindowDockPosition,
                    () => Settings.Default.MainWindowDockSize,
                    () => Settings.Default.MainWindowFullDockThicknessAsPercentageOfScreen,
                    () => Settings.Default.MainWindowCollapsedDockThicknessAsPercentageOfFullDockThickness,
                    () => Settings.Default.MainWindowMinimisedPosition,
                    o => Settings.Default.MainWindowOpacity = o,
                    state => Settings.Default.MainWindowState = state,
                    state => Settings.Default.MainWindowPreviousState = state,
                    rect => Settings.Default.MainWindowFloatingSizeAndPosition = rect,
                    pos => Settings.Default.MainWindowDockPosition = pos,
                    size => Settings.Default.MainWindowDockSize = size,
                    t => Settings.Default.MainWindowFullDockThicknessAsPercentageOfScreen = t,
                    t => Settings.Default.MainWindowCollapsedDockThicknessAsPercentageOfFullDockThickness = t);
                errorNotifyingServices.Add(mainWindowManipulationService);
                mainWindow.WindowManipulationService = mainWindowManipulationService;

                mainViewModel = new MainViewModel(
                    audioService, calibrationService, dictionaryService, keyStateService,
                    suggestionService, capturingStateManager, lastMouseActionStateManager,
                    inputService, keyboardOutputService, mouseOutputService, mainWindowManipulationService, errorNotifyingServices);

                mainWindow.MainView.DataContext = mainViewModel;

                //Setup actions to take once main view is loaded (i.e. the view is ready, so hook up the services which kicks everything off)
                Action postMainViewLoaded = mainViewModel.AttachServiceEventHandlers;
                if(mainWindow.MainView.IsLoaded)
                {
                    postMainViewLoaded();
                }
                else
                {
                    RoutedEventHandler loadedHandler = null;
                    loadedHandler = (s, a) =>
                    {
                        postMainViewLoaded();
                        mainWindow.MainView.Loaded -= loadedHandler; //Ensure this handler only triggers once
                    };
                    mainWindow.MainView.Loaded += loadedHandler;
                }               

                //Show the main window
                mainWindow.Show();               

                //Display splash screen and check for updates (and display message) after the window has been sized and positioned for the 1st time
                EventHandler sizeAndPositionInitialised = null;
                sizeAndPositionInitialised = async (_, __) =>
                {
                    mainWindowManipulationService.SizeAndPositionInitialised -= sizeAndPositionInitialised; //Ensure this handler only triggers once                    
                    inputService.RequestResume(); //Start the input service                    
                };
                if (mainWindowManipulationService.SizeAndPositionIsInitialised)
                {
                    sizeAndPositionInitialised(null, null);
                }
                else
                {
                    mainWindowManipulationService.SizeAndPositionInitialised += sizeAndPositionInitialised;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error starting up application", ex);
                throw;
            }
        }

        private void closeOptiKey(object sender, EventArgs e)
        {
            mainWindow.Close();
        }

        #endregion

        #region Attach Unhandled Exception Handlers

        private static void AttachUnhandledExceptionHandlers()
        {
            Current.DispatcherUnhandledException += (sender, args) => Log.Error("A DispatcherUnhandledException has been encountered...", args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => Log.Error("An UnhandledException has been encountered...", args.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (sender, args) => Log.Error("An UnobservedTaskException has been encountered...", args.Exception);

#if !DEBUG
            Application.Current.DispatcherUnhandledException += NBug.Handler.DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += NBug.Handler.UnhandledException;
            TaskScheduler.UnobservedTaskException += NBug.Handler.UnobservedTaskException;

            NBug.Settings.ProcessingException += (exception, report) =>
            {
                //Add latest log file contents as custom info in the error report
                var rootAppender = ((Hierarchy)LogManager.GetRepository())
                    .Root.Appenders.OfType<FileAppender>()
                    .FirstOrDefault();

                if (rootAppender != null)
                {
                    using (var fs = new FileStream(rootAppender.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var sr = new StreamReader(fs, Encoding.Default))
                        {
                            var logFileText = sr.ReadToEnd();
                            report.CustomInfo = logFileText;
                        }
                    }
                }
            };

            NBug.Settings.CustomUIEvent += (sender, args) =>
            {
                var crashWindow = new CrashWindow
                {
                    Topmost = true,
                    ShowActivated = true
                };
                crashWindow.ShowDialog();

                //The crash report has not been created yet - the UIDialogResult SendReport param determines what happens next
                args.Result = new UIDialogResult(ExecutionFlow.BreakExecution, SendReport.Send);
            };

            NBug.Settings.InternalLogWritten += (logMessage, category) => Log.DebugFormat("NBUG:{0} - {1}", category, logMessage);
#endif
        }

        #endregion

        #region Handle Corrupt Settings

        private static void HandleCorruptSettings()
        {
            try
            {
                //Attempting to read a setting from a corrupt user config file throws an exception
                var upgradeRequired = Settings.Default.SettingsUpgradeRequired;
            }
            catch (ConfigurationErrorsException cee)
            {
                Log.Warn("User settings file is corrupt and needs to be corrected. Alerting user and shutting down.");
                string filename = ((ConfigurationErrorsException)cee.InnerException).Filename;

                //if (MessageBox.Show(
                //        OptiKey.Properties.Resources.CORRUPTED_SETTINGS_MESSAGE,
                //        OptiKey.Properties.Resources.CORRUPTED_SETTINGS_TITLE,
                //        MessageBoxButton.YesNo,
                //        MessageBoxImage.Error) == MessageBoxResult.Yes)
                //{
                //    File.Delete(filename);
                //    try
                //    {
                //        System.Windows.Forms.Application.Restart();
                //    }
                //    catch {} //Swallow any exceptions (e.g. DispatcherExceptions) - we're shutting down so it doesn't matter.
                //}
                Current.Shutdown(); //Avoid the inevitable crash by shutting down gracefully
            }
        }

        #endregion

        #region Create Service Methods

        private static ICalibrationService CreateCalibrationService()
        {
            switch (Settings.Default.PointsSource)
            {
                case PointsSources.TheEyeTribe:
                    return new TheEyeTribeCalibrationService();

                case PointsSources.TobiiEyeX:
                case PointsSources.TobiiRex:
                case PointsSources.TobiiPcEyeGo:
                    return new TobiiEyeXCalibrationService();
            }

            return null;
        }

        private static IInputService CreateInputService(
            IKeyStateService keyStateService,
            IDictionaryService dictionaryService,
            IAudioService audioService,
            ICalibrationService calibrationService,
            ICapturingStateManager capturingStateManager,
            List<INotifyErrors> errorNotifyingServices)
        {
            Log.Info("Creating InputService.");

            //Instantiate point source
            IPointSource pointSource;
            switch (Settings.Default.PointsSource)
            {
                case PointsSources.GazeTracker:
                    pointSource = new GazeTrackerSource(
                        Settings.Default.PointTtl,
                        Settings.Default.GazeTrackerUdpPort,
                        new Regex(GazeTrackerUdpRegex));
                    break;

                case PointsSources.TheEyeTribe:
                    var theEyeTribePointService = new TheEyeTribePointService();
                    errorNotifyingServices.Add(theEyeTribePointService);
                    pointSource = new PointServiceSource(
                        Settings.Default.PointTtl,
                        theEyeTribePointService);
                    break;

                case PointsSources.TobiiEyeX:
                case PointsSources.TobiiRex:
                case PointsSources.TobiiPcEyeGo:
                    var tobiiEyeXPointService = new TobiiEyeXPointService();
                    var tobiiEyeXCalibrationService = calibrationService as TobiiEyeXCalibrationService;
                    if (tobiiEyeXCalibrationService != null)
                    {
                        tobiiEyeXCalibrationService.EyeXHost = tobiiEyeXPointService.EyeXHost;
                    }
                    errorNotifyingServices.Add(tobiiEyeXPointService);
                    pointSource = new PointServiceSource(
                        Settings.Default.PointTtl,
                        tobiiEyeXPointService);
                    break;

                case PointsSources.MousePosition:
                    pointSource = new MousePositionSource(
                        Settings.Default.PointTtl);
                    break;

                default:
                    throw new ArgumentException("'PointsSource' settings is missing or not recognised! Please correct and restart OptiKey.");
            }

            //Instantiate key trigger source
            ITriggerSource keySelectionTriggerSource;
            switch (Settings.Default.KeySelectionTriggerSource)
            {
                case TriggerSources.Fixations:
                    keySelectionTriggerSource = new KeyFixationSource(
                       Settings.Default.KeySelectionTriggerFixationLockOnTime,
                       Settings.Default.KeySelectionTriggerFixationResumeRequiresLockOn,
                       Settings.Default.KeySelectionTriggerFixationDefaultCompleteTime,
                       Settings.Default.KeySelectionTriggerFixationCompleteTimesByIndividualKey
                        ? Settings.Default.KeySelectionTriggerFixationCompleteTimesByKeyValues
                        : null, 
                       Settings.Default.KeySelectionTriggerIncompleteFixationTtl,
                       pointSource.Sequence);
                    break;

                case TriggerSources.KeyboardKeyDownsUps:
                    keySelectionTriggerSource = new KeyboardKeyDownUpSource(
                        Settings.Default.KeySelectionTriggerKeyboardKeyDownUpKey,
                        pointSource.Sequence);
                    break;

                case TriggerSources.MouseButtonDownUps:
                    keySelectionTriggerSource = new MouseButtonDownUpSource(
                        Settings.Default.KeySelectionTriggerMouseDownUpButton,
                        pointSource.Sequence);
                    break;

                default:
                    throw new ArgumentException(
                        "'KeySelectionTriggerSource' setting is missing or not recognised! Please correct and restart OptiKey.");
            }

            //Instantiate point trigger source
            ITriggerSource pointSelectionTriggerSource;
            switch (Settings.Default.PointSelectionTriggerSource)
            {
                case TriggerSources.Fixations:
                    pointSelectionTriggerSource = new PointFixationSource(
                        Settings.Default.PointSelectionTriggerFixationLockOnTime,
                        Settings.Default.PointSelectionTriggerFixationCompleteTime,
                        Settings.Default.PointSelectionTriggerLockOnRadiusInPixels,
                        Settings.Default.PointSelectionTriggerFixationRadiusInPixels,
                        pointSource.Sequence);
                    break;

                case TriggerSources.KeyboardKeyDownsUps:
                    pointSelectionTriggerSource = new KeyboardKeyDownUpSource(
                        Settings.Default.PointSelectionTriggerKeyboardKeyDownUpKey,
                        pointSource.Sequence);
                    break;

                case TriggerSources.MouseButtonDownUps:
                    pointSelectionTriggerSource = new MouseButtonDownUpSource(
                        Settings.Default.PointSelectionTriggerMouseDownUpButton,
                        pointSource.Sequence);
                    break;

                default:
                    throw new ArgumentException(
                        "'PointSelectionTriggerSource' setting is missing or not recognised! "
                        + "Please correct and restart OptiKey.");
            }

            var inputService = new InputService(keyStateService, dictionaryService, audioService, capturingStateManager,
                pointSource, keySelectionTriggerSource, pointSelectionTriggerSource);
            inputService.RequestSuspend(); //Pause it initially
            return inputService;
        }

        #endregion
        
        #region Log Diagnostic Info
        
        private static void LogDiagnosticInfo()
        {
            Log.InfoFormat("Assembly version: {0}", DiagnosticInfo.AssemblyVersion);
            var assemblyFileVersion = DiagnosticInfo.AssemblyFileVersion;
            if (!string.IsNullOrEmpty(assemblyFileVersion))
            {
                Log.InfoFormat("Assembly file version: {0}", assemblyFileVersion);
            }
            if(DiagnosticInfo.IsApplicationNetworkDeployed)
            {
                Log.InfoFormat("ClickOnce deployment version: {0}", DiagnosticInfo.DeploymentVersion);
            }
            Log.InfoFormat("Running as admin: {0}", DiagnosticInfo.RunningAsAdministrator);
            Log.InfoFormat("Process elevated: {0}", DiagnosticInfo.IsProcessElevated);
            Log.InfoFormat("Process bitness: {0}", DiagnosticInfo.ProcessBitness);
            Log.InfoFormat("OS version: {0}", DiagnosticInfo.OperatingSystemVersion);
            Log.InfoFormat("OS service pack: {0}", DiagnosticInfo.OperatingSystemServicePack);
            Log.InfoFormat("OS bitness: {0}", DiagnosticInfo.OperatingSystemBitness);
        }
        
        #endregion
        

        #region Release Keys On App Exit

        private static void ReleaseKeysOnApplicationExit(IKeyStateService keyStateService, IPublishService publishService)
        {
            Current.Exit += (o, args) =>
            {
                if (keyStateService.SimulateKeyStrokes)
                {
                    publishService.ReleaseAllDownKeys();
                }
            };
        }

        #endregion
    }
}
