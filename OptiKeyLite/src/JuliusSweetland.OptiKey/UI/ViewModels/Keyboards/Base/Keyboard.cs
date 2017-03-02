﻿namespace OptiKey.UI.ViewModels.Keyboards.Base
{
    public abstract class Keyboard : IKeyboard
    {
        protected Keyboard(bool simulateKeyStrokes = true, bool multiKeySelectionSupported = false)
        {
            SimulateKeyStrokes = simulateKeyStrokes;
            MultiKeySelectionSupported = multiKeySelectionSupported;
        }

        public bool SimulateKeyStrokes { get; private set; }
        public bool MultiKeySelectionSupported { get; private set; }
    }
}
