using System.Collections.Generic;
using UnityEngine;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Stand-in voice for the PC: prints what would be spoken to the Console.
    /// Captions still advance sentence by sentence, timed as if it were spoken.
    /// </summary>
    public class LogSpeechSynthesizer : SentenceSpeechSynthesizer
    {
        protected override bool StartVoice(IReadOnlyList<string> parts, int voiceSession)
        {
            Debug.Log($"[Voice] Speaking: {string.Join(" ", parts)}");
            return false; // no real voice: advance by estimated time
        }

        protected override void StopVoice()
        {
            if (IsSpeaking)
                Debug.Log("[Voice] Speech stopped.");
        }
    }
}
