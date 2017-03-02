﻿using System;
using OptiKey.Enums;

namespace OptiKey.Models
{
    public class NotificationEventArgs
    {
        public NotificationEventArgs(string title, string content, NotificationTypes notificationType, Action callback)
        {
            Title = title;
            Content = content;
            NotificationType = notificationType;
            Callback = callback;
        }

        public string Title { get; private set; }
        public string Content { get; private set; }
        public NotificationTypes NotificationType { get; private set; }
        public Action Callback { get; private set; }
    }
}
