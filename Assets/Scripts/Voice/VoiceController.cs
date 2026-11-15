using System.Collections.Generic;
using SolarSystemAR.Core;
using UnityEngine;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Glue between speech and the app:
    ///  - mic button (bottom-right) -> recognizer -> VoiceCommandParser -> AppController.Execute
    ///  - AppController.NarrationRequested / NarrationStopped -> synthesizer (reads text aloud)
    /// Created automatically by VoiceBootstrap; no scene setup needed.
    /// </summary>
    public class VoiceController : MonoBehaviour
    {
        private const string NotUnderstoodSpeech = "Не зрозумів. Повторіть, будь ласка.";

        [SerializeField] private AppController appController;
        [SerializeField] private GuideController guideController;

        private ISpeechRecognizer recognizer;
        private ISpeechSynthesizer synthesizer;

        private string status = "Натисніть «Говорити» і скажіть команду";
        private string typedText = string.Empty;
        private bool focusTextBox;

        private void Awake()
        {
            if (appController == null)
                appController = FindAnyObjectByType<AppController>();
            if (guideController == null)
                guideController = FindAnyObjectByType<GuideController>();

            recognizer = CreateRecognizer();
            synthesizer = CreateSynthesizer();
        }

        private void OnEnable()
        {
            recognizer.Recognized += OnRecognized;
            recognizer.Failed += OnRecognitionFailed;

            if (appController != null)
            {
                appController.NarrationRequested += OnNarrationRequested;
                appController.NarrationStopped += OnNarrationStopped;
            }
        }

        private void OnDisable()
        {
            recognizer.Recognized -= OnRecognized;
            recognizer.Failed -= OnRecognitionFailed;

            if (appController != null)
            {
                appController.NarrationRequested -= OnNarrationRequested;
                appController.NarrationStopped -= OnNarrationStopped;
            }

            recognizer.Cancel();
            synthesizer.Stop();
        }

        // Step 3 swaps these for the Android implementations on the phone.
        private static ISpeechRecognizer CreateRecognizer()
        {
            return new TypedTextRecognizer();
        }

        private static ISpeechSynthesizer CreateSynthesizer()
        {
            return new LogSpeechSynthesizer();
        }

        // ---------- listening ----------

        public void ToggleListening()
        {
            if (recognizer.IsListening)
            {
                recognizer.Cancel();
                guideController?.StopListening();
                status = "Скасовано";
                return;
            }

            synthesizer.Stop(); // don't let the mic hear the guide talking
            guideController?.SetListening();
            status = "Слухаю…";
            typedText = string.Empty;
            focusTextBox = true;
            recognizer.StartListening();
        }

        private void OnRecognized(string text)
        {
            guideController?.StopListening();

            AppCommand command = VoiceCommandParser.Parse(text, BuildContext());
            if (command == null)
            {
                status = $"Ви сказали: «{text}» — не зрозумів, повторіть";
                Debug.Log($"[Voice] Not understood: \"{text}\"");
                synthesizer.Speak(NotUnderstoodSpeech);
                return;
            }

            status = $"Ви сказали: «{text}»";
            Debug.Log($"[Voice] \"{text}\" -> {command.Type} {command.Planet} {command.QuestionId}");

            if (appController == null)
                return;

            // "Детальніше про Марс": select Mars first, since detail view works on the selected planet.
            if (command.Type == CommandType.EnterDetailView && command.Planet.HasValue
                && appController.State.SelectedPlanet != command.Planet)
            {
                appController.Execute(new AppCommand { Type = CommandType.SelectPlanet, Planet = command.Planet });
            }

            appController.Execute(command);
        }

        private void OnRecognitionFailed(string message)
        {
            guideController?.StopListening();
            status = message;
            Debug.Log($"[Voice] Recognition failed: {message}");
        }

        private VoiceParseContext BuildContext()
        {
            var questions = new List<VoiceQuestion>();

            if (appController != null)
            {
                foreach (PlanetData data in appController.PlanetCatalog)
                {
                    if (data == null || data.questions == null)
                        continue;

                    foreach (PlanetQuestion q in data.questions)
                    {
                        if (q != null && !string.IsNullOrEmpty(q.id) && !string.IsNullOrEmpty(q.question))
                            questions.Add(new VoiceQuestion(data.id, q.id, q.question));
                    }
                }
            }

            return new VoiceParseContext
            {
                SelectedPlanet = appController != null ? appController.State.SelectedPlanet : null,
                Questions = questions,
            };
        }

        // ---------- speaking ----------

        private void OnNarrationRequested(string text)
        {
            synthesizer.Speak(text);
        }

        private void OnNarrationStopped()
        {
            synthesizer.Stop();
        }

        // ---------- on-screen UI (bottom-right corner) ----------

        private void OnGUI()
        {
            Matrix4x4 previousMatrix = GUI.matrix;

            // Same scaling as the team's test panel, so sizes match on phones.
            float scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float width = Screen.width / scale;
            float height = Screen.height / scale;
            float areaWidth = Mathf.Min(410f, width - 20f);
            float left = width - areaWidth - 10f;

            var labelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
            };
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };

            GUI.Label(new Rect(left, height - 210, areaWidth, 50), status, labelStyle);

            if (recognizer is TypedTextRecognizer typed && typed.IsListening)
                DrawTypingBox(typed, left, height - 150, areaWidth);

            string micLabel = recognizer.IsListening ? "Стоп" : "Говорити";
            if (GUI.Button(new Rect(width - 170, height - 95, 160, 85), micLabel, buttonStyle))
                ToggleListening();

            GUI.matrix = previousMatrix;
        }

        private void DrawTypingBox(TypedTextRecognizer typed, float left, float top, float areaWidth)
        {
            // Enter submits, like a chat box.
            Event e = Event.current;
            bool enterPressed = e.type == EventType.KeyDown
                                && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                                && GUI.GetNameOfFocusedControl() == "VoiceTextBox";

            var fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 18 };
            GUI.SetNextControlName("VoiceTextBox");
            typedText = GUI.TextField(new Rect(left, top, areaWidth - 70, 45), typedText, fieldStyle);

            if (focusTextBox)
            {
                GUI.FocusControl("VoiceTextBox");
                focusTextBox = false;
            }

            if (GUI.Button(new Rect(left + areaWidth - 65, top, 65, 45), "OK") || enterPressed)
            {
                if (enterPressed)
                    e.Use();
                typed.Submit(typedText);
            }
        }
    }
}
