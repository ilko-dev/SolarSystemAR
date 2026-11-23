using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Text-to-speech using the phone's built-in Android TextToSpeech engine, Ukrainian voice (uk-UA).
    /// Needs the Ukrainian voice in the phone's TTS settings (Google Speech Services usually has it).
    ///
    /// Every sentence is queued as its own "utterance" with an id like "7:2" (speech 7, sentence 2);
    /// Android tells us when each one is finished, which moves the captions to the next sentence.
    /// </summary>
    public class AndroidSpeechSynthesizer : SentenceSpeechSynthesizer
    {
        private const int Success = 0;          // TextToSpeech.SUCCESS
        private const int QueueFlush = 0;       // TextToSpeech.QUEUE_FLUSH (replace what is being said)
        private const int QueueAdd = 1;         // TextToSpeech.QUEUE_ADD (say after the previous one)
        private const int LangMissingData = -1; // TextToSpeech.LANG_MISSING_DATA
        private const int LangNotSupported = -2;

        private AndroidJavaObject tts;   // android.speech.tts.TextToSpeech
        private bool ready;
        private bool broken;             // engine failed to start: captions run on estimated time

        // Asked to speak before the engine finished starting.
        private IReadOnlyList<string> pendingParts;
        private int pendingSession;

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
                broken = true;
            }
        }

        protected override bool StartVoice(IReadOnlyList<string> parts, int voiceSession)
        {
            if (tts == null || broken)
                return false;

            if (!ready)
            {
                pendingParts = parts;
                pendingSession = voiceSession;
                return true;
            }

            return Enqueue(parts, voiceSession);
        }

        protected override void StopVoice()
        {
            pendingParts = null;
            if (tts != null && ready)
                tts.Call<int>("stop");
        }

        public override void Dispose()
        {
            if (tts == null)
                return;

            tts.Call("shutdown");
            tts.Dispose();
            tts = null;
            ready = false;
        }

        private bool Enqueue(IReadOnlyList<string> parts, int voiceSession)
        {
            try
            {
                using (var parameters = new AndroidJavaObject("android.os.Bundle"))
                {
                    for (int i = 0; i < parts.Count; i++)
                    {
                        int result = tts.Call<int>("speak", parts[i], i == 0 ? QueueFlush : QueueAdd, parameters,
                            voiceSession + ":" + i);
                        if (result != Success)
                        {
                            tts.Call<int>("stop");
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        // Runs on the Unity main thread (posted from the Android callback).
        private void OnInit(int status)
        {
            if (tts == null)
                return;

            if (status != Success)
            {
                broken = true;
                OnVoiceUnavailable(pendingSession);
                pendingParts = null;
                Report("Озвучення недоступне на цьому телефоні.");
                return;
            }

            using (var locale = new AndroidJavaObject("java.util.Locale", "uk", "UA"))
            {
                int result = tts.Call<int>("setLanguage", locale);
                if (result == LangMissingData || result == LangNotSupported)
                    Report("Немає українського голосу. Встановіть його: Налаштування → Синтез мовлення → Google → Українська.");
            }

            tts.Call<int>("setOnUtteranceCompletedListener", new CompletedListener(this));
            ready = true;

            if (pendingParts != null)
            {
                IReadOnlyList<string> parts = pendingParts;
                pendingParts = null;
                if (!Enqueue(parts, pendingSession))
                    OnVoiceUnavailable(pendingSession);
            }
        }

        // Runs on the Unity main thread. Called for finished, failed and stopped sentences alike.
        private void OnUtteranceCompleted(string utteranceId)
        {
            string[] parts = utteranceId?.Split(':');
            if (parts != null && parts.Length == 2
                && int.TryParse(parts[0], out int voiceSession) && int.TryParse(parts[1], out int sentence))
            {
                OnVoiceSentenceDone(voiceSession, sentence);
            }
        }

        private void Report(string message)
        {
            Debug.LogWarning($"[Voice] TTS: {message}");
            ReportProblem(message);
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

        /// <summary>
        /// Implements the Java interface TextToSpeech.OnUtteranceCompletedListener.
        /// (The newer UtteranceProgressListener is an abstract class, which Unity's AndroidJavaProxy
        /// can't implement without a Java file; this older interface works on all Android versions.)
        /// </summary>
        [Preserve]
        private class CompletedListener : AndroidJavaProxy
        {
            private readonly AndroidSpeechSynthesizer owner;

            public CompletedListener(AndroidSpeechSynthesizer owner)
                : base("android.speech.tts.TextToSpeech$OnUtteranceCompletedListener")
            {
                this.owner = owner;
            }

            [Preserve]
            public void onUtteranceCompleted(string utteranceId)
            {
                MainThreadQueue.Post(() => owner.OnUtteranceCompleted(utteranceId));
            }
        }
    }
}
