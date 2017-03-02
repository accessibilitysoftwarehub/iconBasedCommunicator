﻿using System;
using System.Runtime.InteropServices;

namespace OptiKey.Enums
{
    /// <summary>
    /// Specifies constants that define which mouse button was pressed. An exact duplicate of Windows.Forms.MouseButtons.
    /// This implementation also allows me to localise the string representation of the values manually.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    [ComVisible(true)]
    [Flags]
    public enum MouseButtons
    {
        Left = 1048576,
        None = 0,
        Right = 2097152,
        Middle = 4194304,
        XButton1 = 8388608,
        XButton2 = 16777216,
    }
}
