using SolarSystemAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR.UI
{
    /// <summary>Presentation only. Stays enabled while hidden to receive the next card.</summary>
    public sealed class PlanetInfoPanel : MonoBehaviour
    {
        [SerializeField] private AppController appController;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform card;
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private TMP_Text planetName;
        [SerializeField] private TMP_Text section;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text placeholderLabel;
        [SerializeField] private Image planetImage;
        [SerializeField] private Sprite placeholder;
        [SerializeField] private ScrollRect scroll;
        private bool shown;
        private Rect previousSafeArea;
        private Vector2 previousScreen;
        private float reveal;

        private void OnEnable()
        {
            if (appController == null) appController = FindAnyObjectByType<AppController>();
            visibility.alpha = 0;
            ApplyCompactStyle();
            if (appController != null)
            {
                appController.PlanetInfoChanged += Display;
                Display(appController.CurrentPlanetInfo);
            }
            else Display(null);
            UpdateLayout();
        }

        private void OnDisable()
        {
            if (appController != null) appController.PlanetInfoChanged -= Display;
            visibility.blocksRaycasts = false;
            visibility.interactable = false;
            visibility.alpha = 0;
        }

        private void Update()
        {
            reveal = Mathf.MoveTowards(reveal, shown ? 1 : 0, Time.unscaledDeltaTime / 0.28f);
            float eased = 1f - Mathf.Pow(1f - reveal, 3);
            visibility.alpha = eased;
            card.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
            card.anchoredPosition = new Vector2(Mathf.Lerp(10, -16, eased), -64);
            if (previousSafeArea != Screen.safeArea ||
                previousScreen != new Vector2(Screen.width, Screen.height)) UpdateLayout();
        }

        private void Display(PlanetInfoContent content)
        {
            shown = content != null;
            visibility.blocksRaycasts = shown;
            visibility.interactable = shown;
            if (!shown) return;
            // Restart even when another planet or section replaces an already visible card.
            reveal = 0;
            visibility.alpha = 0;
            planetName.text = content.Name;
            section.text = content.Heading;
            body.text = content.Text;
            planetImage.sprite = content.Image != null ? content.Image : placeholder;
            planetImage.color = content.Image != null ? Color.white
                : Color.HSVToRGB(((int)content.Planet * 0.117f + 0.48f) % 1f, 0.45f, 0.8f);
            placeholderLabel.gameObject.SetActive(content.Image == null);
            placeholderLabel.text = $"{content.Name}\nЗОБРАЖЕННЯ\nВІДСУТНЄ";
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }

        private void UpdateLayout()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            previousSafeArea = Screen.safeArea;
            previousScreen = new Vector2(Screen.width, Screen.height);
            safeArea.anchorMin = previousSafeArea.min / previousScreen;
            safeArea.anchorMax = previousSafeArea.max / previousScreen;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            Vector2 size = safeArea.rect.size;
            bool landscape = Screen.width > Screen.height;
            card.sizeDelta = new Vector2(Mathf.Min(500, size.x * (landscape ? 0.36f : 1f) - 32),
                Mathf.Min(320, size.y * (landscape ? 0.48f : 0.32f)));
        }

        // Also updates already-created prefab instances without requiring Setup again.
        public void ApplyCompactStyle()
        {
            foreach (string label in new[] { "Database", "Footer" })
            {
                var decorative = card.Find(label);
                if (decorative != null) decorative.gameObject.SetActive(false);
            }
            planetName.fontSizeMin = 26;
            planetName.fontSizeMax = 36;
            planetName.fontSize = 36;
            planetName.rectTransform.offsetMin = new Vector2(20, -68);
            planetName.rectTransform.offsetMax = new Vector2(-124, -18);
            var portrait = (RectTransform)planetImage.transform.parent;
            portrait.offsetMin = new Vector2(-112, -112);
            portrait.offsetMax = new Vector2(-16, -16);
            section.fontSizeMin = 14;
            section.fontSizeMax = 18;
            section.fontSize = 18;
            section.rectTransform.offsetMin = new Vector2(20, -106);
            section.rectTransform.offsetMax = new Vector2(-124, -76);
            var line = card.Find("Scan Line") as RectTransform;
            if (line != null)
            {
                line.offsetMin = new Vector2(20, -124);
                line.offsetMax = new Vector2(-20, -123);
            }
            scroll.viewport.offsetMin = new Vector2(20, 18);
            scroll.viewport.offsetMax = new Vector2(-20, -138);
            body.fontSize = 23;
            body.lineSpacing = 3;
            card.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.075f, 0.9f);
        }
    }
}
