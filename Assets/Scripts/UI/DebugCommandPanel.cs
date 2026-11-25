
using System;
using UnityEngine;
using SolarSystemAR.Core;

namespace SolarSystemAR.UI
{
    public class DebugCommandPanel : MonoBehaviour
    {
        [SerializeField] private AppController appController;
        [SerializeField] private GuideController guideController;

        private bool isExpanded = true;
        private Vector2 scrollPosition;

        private void Awake()
        {
            // Test UI only in the Editor and in builds with "Development Build" ticked.
            if (!Debug.isDebugBuild)
                enabled = false;
        }

        private void OnGUI()
        {
            if (appController == null)
                return;

            Matrix4x4 previousMatrix = GUI.matrix;

            float scale = Mathf.Clamp(
                Screen.height / 900f, 1f, 2.5f);

            GUI.matrix = Matrix4x4.Scale(
                new Vector3(scale, scale, 1f));

            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (GUI.Button(
                new Rect(width - 160, 10, 150, 40),
                isExpanded ? "Hide Test UI" : "Show Test UI"))
            {
                isExpanded = !isExpanded;
            }

            if (isExpanded)
            {
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 16
                };

                GUILayout.BeginArea(
                    new Rect(10, 60,
                        Mathf.Min(410, width - 20),
                        height - 70),
                    GUI.skin.box);

                scrollPosition = GUILayout.BeginScrollView(
                    scrollPosition);

                var state = appController.State;

                GUILayout.Label(
                    $"Mode: {state.Mode} | " +
                    $"Planet: {state.SelectedPlanet?.ToString() ?? "None"}");

                GUILayout.Label("SELECT PLANET");

                foreach (PlanetId id in Enum.GetValues(typeof(PlanetId)))
                {
                    SendButton(
                        id.ToString(),
                        CommandType.SelectPlanet,
                        buttonStyle,
                        id);
                }

                GUILayout.Label("TOUR");

                SendButton("Start Tour", CommandType.StartTour, buttonStyle);
                SendButton("Next", CommandType.NextPlanet, buttonStyle);
                SendButton("Previous", CommandType.PreviousPlanet, buttonStyle);
                SendButton("Overview", CommandType.ShowOverview, buttonStyle);

                GUILayout.Label("MOVEMENT");

                SendButton("Pause", CommandType.PauseOrbits, buttonStyle);
                SendButton("Resume", CommandType.ResumeOrbits, buttonStyle);
                    
                GUILayout.Label("DETAIL VIEW");

                SendButton("Enter Detail View",
                    CommandType.EnterDetailView, buttonStyle);

                SendButton("Exit Detail View",
                    CommandType.ExitDetailView, buttonStyle);


                GUILayout.Label("NARRATION");

                SendButton("Fact", CommandType.ShowFact, buttonStyle);
                SendButton("History", CommandType.ShowHistory, buttonStyle);
                SendButton("Stop", CommandType.StopNarration, buttonStyle);
                SendButton("Repeat", CommandType.RepeatNarration, buttonStyle);
                SendButton("Cancel", CommandType.Cancel, buttonStyle);
                SendButton("Help", CommandType.ShowHelp, buttonStyle);
                SendButton("List Questions", CommandType.ListQuestions, buttonStyle);

                PlanetData data = appController.GetSelectedPlanetData();

                if (data != null && data.questions != null)
                {
                    GUILayout.Label("QUESTIONS");

                    foreach (var question in data.questions)
                    {
                        if (question == null)
                            continue;

                        string label = string.IsNullOrEmpty(question.question)
                            ? question.id
                            : question.question;

                        if (GUILayout.Button(
                            label, buttonStyle, GUILayout.Height(40)))
                        {
                            appController.Execute(new AppCommand
                            {
                                Type = CommandType.AskQuestion,
                                QuestionId = question.id
                            });
                        }
                    }
                }

                if (guideController != null &&
                    GUILayout.Button(
                        "Test Listening",
                        buttonStyle,
                        GUILayout.Height(40)))
                {
                    guideController.SetListening();
                }

                GUILayout.Label("Last response:");
                GUILayout.Label(
                    appController.LastNarrationText ?? "None");

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }

            GUI.matrix = previousMatrix;
        }

        private void SendButton(
            string label,
            CommandType type,
            GUIStyle style,
            PlanetId? planet = null)
        {
            if (GUILayout.Button(
                label, style, GUILayout.Height(40)))
            {
                appController.Execute(new AppCommand
                {
                    Type = type,
                    Planet = planet
                });
            }
        }
    }
}
