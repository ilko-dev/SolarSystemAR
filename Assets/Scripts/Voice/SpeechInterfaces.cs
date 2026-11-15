using System;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Speech-to-text. Implementations: TypedTextRecognizer (PC/Editor, you type instead of speaking)
    /// and, on Android, the phone's built-in Google recognizer.
    /// All events are raised on Unity's main thread.
    /// </summary>
    public interface ISpeechRecognizer
    {
        /// <summary>Final recognized phrase.</summary>
        event Action<string> Recognized;

        /// <summary>Recognition ended without a phrase. The argument is a short Ukrainian message for the user.</summary>
        event Action<string> Failed;

        bool IsListening { get; }

        void StartListening();

        void Cancel();
    }

    /// <summary>
    /// Text-to-speech. Implementations: LogSpeechSynthesizer (PC/Editor, writes to the Console)
    /// and, on Android, the phone's built-in Google TTS voice.
    /// </summary>
    public interface ISpeechSynthesizer
    {
        void Speak(string text);

        void Stop();
    }
}
