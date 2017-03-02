﻿using System.Windows;
using WindowsInput.Native;

namespace OptiKey.Services
{
    public interface IPublishService : INotifyErrors
    {
        void KeyDown(VirtualKeyCode virtualKeyCode);
        void KeyUp(VirtualKeyCode virtualKeyCode);
        void KeyDownUp(VirtualKeyCode virtualKeyCode);
        void LeftMouseButtonClick();
        void LeftMouseButtonDoubleClick();
        void LeftMouseButtonDown();
        void LeftMouseButtonUp();
        void MiddleMouseButtonClick();
        void MiddleMouseButtonDown();
        void MiddleMouseButtonUp();
        void MouseMouseToPoint(Point point);
        void ReleaseAllDownKeys();
        void RightMouseButtonClick();
        void RightMouseButtonDown();
        void RightMouseButtonUp();
        void ScrollMouseWheelUp(int clicks);
        void ScrollMouseWheelDown(int clicks);
        void ScrollMouseWheelLeft(int clicks);
        void ScrollMouseWheelRight(int clicks);
        void TypeText(string text);
    }
}
