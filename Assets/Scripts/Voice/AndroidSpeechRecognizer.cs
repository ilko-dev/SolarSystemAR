using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Scripting;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Speech-to-text using the phone's built-in Android SpeechRecognizer (Google), Ukrainian (uk-UA).
    /// Free, no API key; usually needs internet. Asks for microphone permission the first time.
    /// Android requires the recognizer to be created and used on the Android UI thread,
    /// so every call goes through RunOnUiThread; results come back via MainThreadQueue.
    /// </summary>
    public class AndroidSpeechRecognizer : ISpeechRecognizer
    {
        private const string Language = "uk-UA";

        public event Action<IReadOnlyList<string>> Recognized;
        public event Action<string> Failed;

        public bool IsListening { get; private set; }

        private AndroidJavaObject activity;
        private AndroidJavaObject recognizer;   // android.speech.SpeechRecognizer, UI thread only
        private int session;                    // ignores late callbacks from a cancelled session

        public void StartListening()
        {
            if (IsListening)
                return;

            IsListening = true;
            session++;

            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                StartRecognizer(session);
                return;
            }

            int requestSession = session;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => MainThreadQueue.Post(() => StartRecognizer(requestSession));
            callbacks.PermissionDenied += _ => MainThreadQueue.Post(() =>
                Fail(requestSession, "Немає доступу до мікрофона. Дозвольте його в налаштуваннях телефону."));
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
        }

        public void Cancel()
        {
            if (!IsListening)
                return;

            IsListening = false;
            session++;
            RunOnUiThread(() => recognizer?.Call("cancel"));
        }

        public void Dispose()
        {
            IsListening = false;
            session++;
            AndroidJavaObject old = recognizer;
            recognizer = null;
            if (old != null)
                RunOnUiThread(() => old.Call("destroy"));
        }

        private void StartRecognizer(int forSession)
        {
            if (!IsListening || forSession != session)
                return;

            RunOnUiThread(() =>
            {
                try
                {
                    var recognizerClass = new AndroidJavaClass("android.speech.SpeechRecognizer");

                    if (!recognizerClass.CallStatic<bool>("isRecognitionAvailable", Activity))
                    {
                        MainThreadQueue.Post(() => Fail(forSession,
                            "Розпізнавання мовлення недоступне. Перевірте, що встановлено додаток Google."));
                        return;
                    }

                    if (recognizer == null)
                    {
                        recognizer = recognizerClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", Activity);
                        recognizer.Call("setRecognitionListener", new Listener(this));
                    }

                    var intent = new AndroidJavaObject("android.content.Intent", "android.speech.action.RECOGNIZE_SPEECH");
                    intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form");
                    intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE", Language);
                    intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_PREFERENCE", Language);
                    intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.MAX_RESULTS", 5);

                    recognizer.Call("startListening", intent);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    MainThreadQueue.Post(() => Fail(forSession, "Не вдалося запустити розпізнавання."));
                }
            });
        }

        // ---------- results (posted from the Android thread) ----------

        private void OnResults(List<string> phrases)
        {
            int forSession = session;
            MainThreadQueue.Post(() =>
            {
                if (!IsListening || forSession != session)
                    return;

                IsListening = false;
                if (phrases.Count == 0)
                    Failed?.Invoke("Не почув. Спробуйте ще раз.");
                else
                    Recognized?.Invoke(phrases);
            });
        }

        private void OnError(int code)
        {
            int forSession = session;
            MainThreadQueue.Post(() => Fail(forSession, ErrorMessage(code)));
        }

        private void Fail(int forSession, string message)
        {
            if (!IsListening || forSession != session)
                return;

            IsListening = false;
            Debug.Log($"[Voice] Recognizer: {message}");
            Failed?.Invoke(message);
        }

        private static string ErrorMessage(int code)
        {
            // Codes from android.speech.SpeechRecognizer.ERROR_*
            switch (code)
            {
                case 1:
                case 2:
                case 4:
                case 11: return "Немає зв'язку з сервісом розпізнавання. Перевірте інтернет.";
                case 3: return "Проблема з мікрофоном.";
                case 6:
                case 7: return "Не почув. Спробуйте ще раз.";
                case 8: return "Розпізнавання зайняте, спробуйте ще раз.";
                case 9: return "Немає дозволу на мікрофон.";
                case 12:
                case 13: return "Українська мова недоступна для розпізнавання на цьому телефоні.";
                default: return $"Помилка розпізнавання ({code}). Спробуйте ще раз.";
            }
        }

        // ---------- Android plumbing ----------

        private AndroidJavaObject Activity
        {
            get
            {
                if (activity == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                }

                return activity;
            }
        }

        private void RunOnUiThread(Action action)
        {
            Activity.Call("runOnUiThread", new AndroidJavaRunnable(action));
        }

        /// <summary>Implements the Java interface android.speech.RecognitionListener.</summary>
        [Preserve]
        private class Listener : AndroidJavaProxy
        {
            private readonly AndroidSpeechRecognizer owner;

            public Listener(AndroidSpeechRecognizer owner) : base("android.speech.RecognitionListener")
            {
                this.owner = owner;
            }

            [Preserve] public void onReadyForSpeech(AndroidJavaObject bundle) { }
            [Preserve] public void onBeginningOfSpeech() { }
            [Preserve] public void onRmsChanged(float rmsdB) { }
            [Preserve] public void onBufferReceived(byte[] buffer) { }
            [Preserve] public void onEndOfSpeech() { }
            [Preserve] public void onPartialResults(AndroidJavaObject bundle) { }
            [Preserve] public void onEvent(int eventType, AndroidJavaObject bundle) { }

            [Preserve]
            public void onError(int error)
            {
                owner.OnError(error);
            }

            [Preserve]
            public void onResults(AndroidJavaObject bundle)
            {
                var phrases = new List<string>();
                try
                {
                    AndroidJavaObject list = bundle?.Call<AndroidJavaObject>("getStringArrayList", "results_recognition");
                    int count = list != null ? list.Call<int>("size") : 0;
                    for (int i = 0; i < count; i++)
                    {
                        string text = list.Call<AndroidJavaObject>("get", i)?.Call<string>("toString");
                        if (!string.IsNullOrWhiteSpace(text))
                            phrases.Add(text);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                owner.OnResults(phrases);
            }
        }
    }
}
