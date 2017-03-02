﻿using System;
using System.Collections.Generic;

namespace OptiKey.Services
{
    public interface IAudioService : INotifyErrors
    {
        List<string> GetAvailableVoices();
        void PlaySound(string file, int volume);
        bool SpeakNewOrInterruptCurrentSpeech(string textToSpeak, Action onComplete, int? volume = null, int? rate = null, string voice = null);
    }
}
