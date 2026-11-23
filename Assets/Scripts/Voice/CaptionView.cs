using SolarSystemAR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Subtitles: shows the sentence the guide is saying right now in a dark bar near the bottom of the screen,
    /// above the mic button. Builds its own UI from code (no scene or prefab setup), uses the same
    /// Ukrainian font and dark colour as the planet info card, and never blocks touches.
    /// Added automatically next to VoiceController.
    /// </summary>
    public class CaptionView : MonoBehaviour
    {
        // Sizes in the same units as the mic button's on-screen GUI (VoiceController.OnGUI),
        // so the caption sits full width just above the bottom row (status text + mic button) on every screen.
        private const float BottomGap = 105f;    // mic button row is 95 high, plus a small gap
        private const float SideMargin = 10f;
        private const float MaxWidth = 760f;
        private const float FontSize = 18f;
        private const float Padding = 10f;
        private const float FadeSeconds = 0.2f;

        private static readonly Color Background = new Color(0.025f, 0.045f, 0.075f, 0.7f);

        private VoiceController voice;
        private RectTransform bar;
        private TextMeshProUGUI label;
        private CanvasGroup group;
        private bool shown;

        private void Awake()
        {
            Build();
        }

        // VoiceController is looked up in Start: Unity adds this component (RequireComponent) just before it.
        private void Start()
        {
            voice = GetComponent<VoiceController>();
            Subscribe();
        }

        private void OnEnable()
        {
            if (voice != null)
                Subscribe();
        }

        private void Subscribe()
        {
            if (voice == null)
                return;

            voice.CaptionChanged -= Show;
            voice.CaptionChanged += Show;
            Show(voice.Caption);
        }

        private void OnDisable()
        {
            if (voice != null)
                voice.CaptionChanged -= Show;
            shown = false;
            if (group != null)
                group.alpha = 0f;
        }

        private void Show(string text)
        {
            shown = !string.IsNullOrEmpty(text);
            if (shown)
                label.text = text; // when hiding, keep the last sentence visible while it fades out
        }

        private void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, shown ? 1f : 0f, Time.unscaledDeltaTime / FadeSeconds);
            if (group.alpha > 0f)
                UpdateLayout();
        }

        private void UpdateLayout()
        {
            float scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f); // same as VoiceController.OnGUI
            Rect safe = Screen.safeArea;

            // Full width inside the notch-safe area, capped on wide (landscape) screens.
            float left = Mathf.Max(safe.xMin, 0f) + SideMargin * scale;
            float right = Mathf.Min(safe.xMax, Screen.width) - SideMargin * scale;
            float width = Mathf.Min(right - left, MaxWidth * scale);
            label.fontSize = FontSize * scale;
            float padding = Padding * scale;
            label.margin = new Vector4(padding, padding, padding, padding);
            float textHeight = label.GetPreferredValues(label.text, width - 2f * padding, 0f).y;

            bar.sizeDelta = new Vector2(width, textHeight + 2f * padding);
            float centre = Mathf.Clamp(Screen.width / 2f, left + width / 2f, right - width / 2f);
            bar.anchoredPosition = new Vector2(centre, BottomGap * scale);
        }

        private void Build()
        {
            var canvasObject = new GameObject("Captions", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // above the info card
            group = canvasObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // Bar: positioned in screen pixels from the bottom-left corner, grows upwards.
            var barObject = new GameObject("Caption Bar", typeof(RectTransform));
            barObject.transform.SetParent(canvasObject.transform, false);
            bar = (RectTransform)barObject.transform;
            bar.anchorMin = bar.anchorMax = Vector2.zero;
            bar.pivot = new Vector2(0.5f, 0f);
            var image = barObject.AddComponent<Image>();
            image.color = Background;
            image.raycastTarget = false;

            var textObject = new GameObject("Caption Text", typeof(RectTransform));
            textObject.transform.SetParent(bar, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = FindUkrainianFont();
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;

            UpdateLayout();
        }

        // The planet info card already has a font with Ukrainian letters; the TMP default font has none.
        private static TMP_FontAsset FindUkrainianFont()
        {
            PlanetInfoPanel panel = FindAnyObjectByType<PlanetInfoPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.font != null)
                        return text.font;
                }
            }

            Debug.LogWarning("[Voice] Captions: planet info panel font not found, Ukrainian letters may not show.");
            return TMP_Settings.defaultFontAsset;
        }
    }
}
