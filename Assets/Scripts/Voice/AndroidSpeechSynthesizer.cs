using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Text-to-speech using the phone's built-in Android TextToSpeech engine, Ukrainian voice (uk-UA).
    /// Needs the Ukrainian voice in the phone's TTS settings (Google Speech Services usually has it).
    /// </summary>
    public class AndroidSpeechSynthesizer : ISpeechSynthesizer
    {
        private const int Success = 0;          // TextToSpeech.SUCCESS
        private const int QueueFlush = 0;       // TextToSpeech.QUEUE_FLUSH (replace what is being said)
        private const int LangMissingData = -1; // TextToSpeech.LANG_MISSING_DATA
        private const int LangNotSupported = -2;

        public event Action<string> Problem;

        private AndroidJavaObject tts;   // android.speech.tts.TextToSpeech
        private bool ready;
        private string pendingText;      // asked to speak before the engine finished starting
        private int utteranceCounter;

        public AndroidSpeechSynthesizer()
        {
            try
            {
                AndroidJavaObject activity;
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    activity = player.GetStatic<AndroidJavaObject>("currentActivity");

                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener(this));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Problem?.Invoke("Не вдалося запустити озвучення.");
            }
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || tts == null)
                return;

            if (!ready)
            {
                pendingText = text;
                return;
            }

            try
            {
                using (var parameters = new AndroidJavaObject("android.os.Bundle"))
                    tts.Call<int>("speak", text, QueueFlush, parameters, "narration" + (++utteranceCounter));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Stop()
        {
            pendingText = null;
            if (tts != null && ready)
                tts.Call<int>("stop");
        }

        public void Dispose()
        {
            if (tts == null)
                return;

            tts.Call("shutdown");
            tts.Dispose();
            tts = null;
            ready = false;
        }

        // Runs on the Unity main thread (posted from the Android callback).
        private void OnInit(int status)
        {
            if (tts == null)
                return;

            if (status != Success)
            {
                Report("Озвучення недоступне на цьому телефоні.");
                return;
            }

            using (var locale = new AndroidJavaObject("java.util.Locale", "uk", "UA"))
            {
                int result = tts.Call<int>("setLanguage", locale);
                if (result == LangMissingData || result == LangNotSupported)
                    Report("Немає українського голосу. Встановіть його: Налаштування → Синтез мовлення → Google → Українська.");
            }

            ready = true;

            if (pendingText != null)
            {
                string text = pendingText;
                pendingText = null;
                Speak(text);
            }
        }

        private void Report(string message)
        {
            Debug.LogWarning($"[Voice] TTS: {message}");
            Problem?.Invoke(message);
        }

        /// <summary>Implements the Java interface TextToSpeech.OnInitListener.</summary>
        [Preserve]
        private class InitListener : AndroidJavaProxy
        {
            private readonly AndroidSpeechSynthesizer owner;

            public InitListener(AndroidSpeechSynthesizer owner)
                : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                this.owner = owner;
            }

            [Preserve]
            public void onInit(int status)
            {
                MainThreadQueue.Post(() => owner.OnInit(status));
            }
        }
    }
}
