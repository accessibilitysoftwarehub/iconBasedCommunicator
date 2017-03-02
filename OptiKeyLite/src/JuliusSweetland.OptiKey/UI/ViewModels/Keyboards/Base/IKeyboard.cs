﻿namespace OptiKey.UI.ViewModels.Keyboards.Base
{
    public interface IKeyboard
    {
        bool SimulateKeyStrokes { get; }
        bool MultiKeySelectionSupported { get; }
    }
}
