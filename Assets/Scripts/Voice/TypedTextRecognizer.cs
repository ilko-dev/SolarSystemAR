using System;
using System.Collections.Generic;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Stand-in recognizer for the PC: "listening" opens a text box (drawn by VoiceController),
    /// and whatever you type is treated as if you had said it.
    /// </summary>
    public class TypedTextRecognizer : ISpeechRecognizer
    {
        public event Action<IReadOnlyList<string>> Recognized;
        public event Action<string> Failed;

        public bool IsListening { get; private set; }

        public void StartListening()
        {
            IsListening = true;
        }

        public void Cancel()
        {
            IsListening = false;
        }

        public void Dispose()
        {
            IsListening = false;
        }

        /// <summary>Called by the text box when you press Enter / OK.</summary>
        public void Submit(string text)
        {
            if (!IsListening)
                return;

            IsListening = false;

            if (string.IsNullOrWhiteSpace(text))
                Failed?.Invoke("Нічого не введено.");
            else
                Recognized?.Invoke(new[] { text.Trim() });
        }
    }
}
