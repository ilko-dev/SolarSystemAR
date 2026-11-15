using UnityEngine;

namespace SolarSystemAR.Voice
{
    /// <summary>Stand-in voice for the PC: prints what would be spoken to the Console.</summary>
    public class LogSpeechSynthesizer : ISpeechSynthesizer
    {
        public void Speak(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                Debug.Log($"[Voice] Speaking: {text}");
        }

        public void Stop()
        {
            Debug.Log("[Voice] Speech stopped.");
        }
    }
}
