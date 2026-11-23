using System;
using System.Collections.Generic;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Speech-to-text. Implementations: TypedTextRecognizer (PC/Editor, you type instead of speaking)
    /// and AndroidSpeechRecognizer (the phone's built-in Google recognizer).
    /// All events are raised on Unity's main thread.
    /// </summary>
    public interface ISpeechRecognizer : IDisposable
    {
        /// <summary>Recognized phrase, best guess first (Android returns several alternatives).</summary>
        event Action<IReadOnlyList<string>> Recognized;

        /// <summary>Recognition ended without a phrase. The argument is a short Ukrainian message for the user.</summary>
        event Action<string> Failed;

        bool IsListening { get; }

        void StartListening();

        void Cancel();
    }

    /// <summary>
    /// Text-to-speech. Implementations: LogSpeechSynthesizer (PC/Editor, writes to the Console)
    /// and AndroidSpeechSynthesizer (the phone's built-in TTS voice). Both speak sentence by sentence.
    /// All events are raised on Unity's main thread.
    /// </summary>
    public interface ISpeechSynthesizer : IDisposable
    {
        /// <summary>Setup problem worth showing to the user (e.g. no Ukrainian voice installed).</summary>
        event Action<string> Problem;

        /// <summary>The voice started saying this sentence (used for captions).</summary>
        event Action<string> SentenceStarted;

        /// <summary>The voice has nothing more to say: it finished or was stopped.</summary>
        event Action Finished;

        bool IsSpeaking { get; }

        /// <summary>Replaces whatever is being said.</summary>
        void Speak(string text);

        void Stop();

        /// <summary>Call every frame (keeps timing going when there is no real voice).</summary>
        void Tick(float deltaTime);
    }
}
