using System;
using UnityEngine;

namespace SolarSystemAR.Voice
{
    /// <summary>Stand-in voice for the PC: prints what would be spoken to the Console.</summary>
    public class LogSpeechSynthesizer : ISpeechSynthesizer
    {
        // Never raised: the Console voice has nothing to set up.
        public event Action<string> Problem { add { } remove { } }

        public void Speak(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                Debug.Log($"[Voice] Speaking: {text}");
        }

        public void Stop()
        {
            Debug.Log("[Voice] Speech stopped.");
        }

        public void Dispose()
        {
        }
    }
}
