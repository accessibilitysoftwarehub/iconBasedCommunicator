﻿using OptiKey.Properties;
using OptiKey.Services;
using log4net;
using Prism.Mvvm;

namespace OptiKey.Models
{
    public class CapturingStateManager : BindableBase, ICapturingStateManager
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IAudioService audioService;

        public CapturingStateManager(IAudioService audioService)
        {
            this.audioService = audioService;
        }

        private bool capturingMultiKeySelection;
        public bool CapturingMultiKeySelection
        {
            get { return capturingMultiKeySelection; }
            set
            {
                if (SetProperty(ref capturingMultiKeySelection, value))
                {
                    Log.DebugFormat("CapturingMultiKeySelection changed to {0}", value);

                    audioService.PlaySound(
                        value
                            ? Settings.Default.MultiKeySelectionCaptureStartSoundFile
                            : Settings.Default.MultiKeySelectionCaptureEndSoundFile,
                        value
                            ? Settings.Default.MultiKeySelectionCaptureStartSoundVolume
                            : Settings.Default.MultiKeySelectionCaptureEndSoundVolume);
                }
            }
        }
    }
}
