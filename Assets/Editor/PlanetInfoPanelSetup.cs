using System;
using System.IO;
using System.Text;
using SolarSystemAR.Core;
using SolarSystemAR.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SolarSystemAR.Editor
{
    public static class PlanetInfoPanelSetup
    {
        private const string Folder = "Assets/UI/PlanetInfo";
        private const string PrefabPath = Folder + "/PlanetInfoPanel.prefab";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        private static readonly Color Cyan = new Color(0.24f, 0.88f, 1f);

        [MenuItem("Solar System/Setup Planet Info Panel")]
        public static void Setup()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before setup.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "MainARScene")
                throw new InvalidOperationException("Open MainARScene before setup.");
            if (UnityEngine.Object.FindAnyObjectByType<AppController>() == null)
                throw new InvalidOperationException("The scene needs its existing AppController.");
            if (Shader.Find("TextMeshPro/Distance Field") == null ||
                AssetDatabase.LoadAssetAtPath<Font>(FontPath) == null)
            {
                string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly).resolvedPath;
                UnityEditor.AssetPackage.Package.Import(Path.Combine(package, "Package Resources/TMP Essential Resources.unitypackage"), false);
                AssetDatabase.Refresh();
            }
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            TMP_FontAsset font = CreateFont();
            Sprite circle = CreateCircle();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                GameObject root = Build(font, circle);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
            }
            if (UnityEngine.Object.FindAnyObjectByType<PlanetInfoPanel>(FindObjectsInactive.Include) == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(instance, "Add Planet Info Panel");
            }
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            {
                var events = new GameObject("Planet Info EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                Undo.RegisterCreatedObjectUndo(events, "Add UI EventSystem");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Planet Info Panel ready. Save MainARScene; play and use Mars > Fact > History > Earth > Overview.");
        }

        // Headless entry point: preserve the existing scene contents and only add the prefab/EventSystem.
        public static void SetupMainScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainARScene.unity");
            Setup();
            ValidateCommands();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static TMP_FontAsset CreateFont()
        {
            string path = Folder + "/PlanetInfoUkrainian.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            // Unity 6's built-in LegacyRuntime font has no importable font face.
            // Use the actual OFL-licensed TTF supplied with TMP Essential Resources.
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (source == null)
                throw new InvalidOperationException("TMP font import is not complete. Wait for import, then run Solar System > Setup Planet Info Panel again.");
            var importer = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
            if (importer != null && !importer.includeFontData)
            {
                importer.includeFontData = true;
                importer.SaveAndReimport();
                source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            }
            var font = TMP_FontAsset.CreateFontAsset(source,
                64, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            if (font == null)
                throw new InvalidOperationException("Cannot create the panel font from " + FontPath + ". Check its Font Import Settings > Include Font Data.");
            font.name = "PlanetInfoUkrainian";
            var chars = new StringBuilder();
            for (int c = 32; c <= 126; c++) chars.Append((char)c);
            for (int c = 0x400; c <= 0x45F; c++) chars.Append((char)c);
            chars.Append("Ґґ’‘“”«»–—…№°");
            if (!font.TryAddCharacters(chars.ToString(), out string missing))
                throw new InvalidOperationException("Font lacks required characters: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            EditorUtility.SetDirty(font);
            return font;
        }

        private static Sprite CreateCircle()
        {
            string path = Folder + "/PlaceholderCircle.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(127.5f, 127.5f));
                        texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(127.5f - distance)));
                    }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject Build(TMP_FontAsset font, Sprite circle)
        {
            var root = new GameObject("Planet Info Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform safe = Rect("Safe Area", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform card = Rect("Holographic Card", safe, Vector2.one, Vector2.one, new Vector2(-776, -546), new Vector2(-16, -16));
            card.pivot = Vector2.one;
            card.anchoredPosition = new Vector2(-16, -16);
            card.sizeDelta = new Vector2(760, 530);
            var group = card.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
            var background = card.gameObject.AddComponent<Image>();
            background.color = new Color(0.025f, 0.065f, 0.11f, 0.94f);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.65f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Text("Database", card, font, "БАЗА ПЛАНЕТ  /  ROBOT GUIDE", 18, Cyan,
                new Vector2(24, -40), new Vector2(-176, -14));
            var name = Text("Planet Name", card, font, "Планета", 48, Color.white,
                new Vector2(24, -112), new Vector2(-178, -48));
            name.enableAutoSizing = true;
            name.fontSizeMin = 28;
            name.fontSizeMax = 48;
            var heading = Text("Section", card, font, "ПРО ПЛАНЕТУ", 22, Cyan,
                new Vector2(24, -170), new Vector2(-24, -136));
            heading.enableAutoSizing = true;
            heading.fontSizeMin = 16;
            heading.fontSizeMax = 22;
            RectTransform portrait = Rect("Circular Portrait", card, Vector2.one, Vector2.one,
                new Vector2(-156, -136), new Vector2(-24, -4));
            var maskImage = portrait.gameObject.AddComponent<Image>();
            maskImage.sprite = circle;
            maskImage.color = Cyan;
            portrait.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform photoRect = Rect("Planet Image", portrait, Vector2.zero, Vector2.one,
                new Vector2(3, 3), new Vector2(-3, -3));
            var photo = photoRect.gameObject.AddComponent<Image>();
            photo.sprite = circle;
            photo.preserveAspect = true;
            photo.raycastTarget = false;
            var label = Text("Missing Image Label", portrait, font, "ЗОБРАЖЕННЯ\nВІДСУТНЄ", 15,
                new Color(0.02f, 0.07f, 0.12f), Vector2.zero, Vector2.zero);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8, 8);
            label.rectTransform.offsetMax = new Vector2(-8, -8);
            label.alignment = TextAlignmentOptions.Center;
            var line = Rect("Scan Line", card, new Vector2(0, 1), Vector2.one,
                new Vector2(24, -180), new Vector2(-24, -178));
            line.gameObject.AddComponent<Image>().color = Cyan;
            RectTransform viewport = Rect("Scroll Viewport", card, Vector2.zero, Vector2.one,
                new Vector2(24, 46), new Vector2(-30, -194));
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0.001f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            scroll.viewport = viewport;
            var body = Text("Content", viewport, font, "", 27, new Color(0.88f, 0.96f, 1), Vector2.zero, Vector2.zero);
            body.rectTransform.pivot = new Vector2(0.5f, 1);
            body.rectTransform.sizeDelta = Vector2.zero;
            body.lineSpacing = 8;
            var fit = body.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = body.rectTransform;
            var footer = Text("Footer", card, font, "///  АРХІВ ЕКСКУРСОВОДА     ·     ПРОКРУТІТЬ ТЕКСТ", 16, Cyan, Vector2.zero, Vector2.zero);
            footer.rectTransform.anchorMin = Vector2.zero;
            footer.rectTransform.anchorMax = new Vector2(1, 0);
            footer.rectTransform.offsetMin = new Vector2(24, 12);
            footer.rectTransform.offsetMax = new Vector2(-24, 36);
            footer.enableAutoSizing = true;
            footer.fontSizeMin = 10;
            footer.fontSizeMax = 16;
            var panel = root.AddComponent<PlanetInfoPanel>();
            var serialized = new SerializedObject(panel);
            Set(serialized, "safeArea", safe); Set(serialized, "card", card);
            Set(serialized, "visibility", group); Set(serialized, "planetName", name);
            Set(serialized, "section", heading); Set(serialized, "body", body);
            Set(serialized, "placeholderLabel", label); Set(serialized, "planetImage", photo);
            Set(serialized, "placeholder", circle); Set(serialized, "scroll", scroll);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.ApplyCompactStyle();
            return root;
        }

        private static void Set(SerializedObject target, string property, UnityEngine.Object value) =>
            target.FindProperty(property).objectReferenceValue = value;

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            return rect;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value,
            float size, Color color, Vector2 min, Vector2 max)
        {
            var rect = Rect(name, parent, new Vector2(0, 1), Vector2.one, min, max);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.raycastTarget = false; text.richText = false;
            return text;
        }

        [MenuItem("Solar System/Validate Planet Info Commands")]
        public static void ValidateCommands()
        {
            // Exercise the real controller on isolated objects, leaving the scene's state untouched.
            var root = new GameObject("Planet Info Validation");
            root.SetActive(false);
            try
            {
                var solar = root.AddComponent<SolarSystemController>();
                var app = root.AddComponent<AppController>();
                var solarFields = new SerializedObject(solar);
                var planets = solarFields.FindProperty("planets");
                planets.arraySize = 2;
                var ids = new[] { PlanetId.Mars, PlanetId.Earth };
                for (int i = 0; i < ids.Length; i++)
                {
                    var obj = new GameObject(ids[i].ToString()); obj.transform.SetParent(root.transform);
                    var orbit = obj.AddComponent<PlanetOrbit>();
                    var fields = new SerializedObject(orbit);
                    fields.FindProperty("planetId").intValue = (int)ids[i]; fields.ApplyModifiedPropertiesWithoutUndo();
                    planets.GetArrayElementAtIndex(i).objectReferenceValue = orbit;
                }
                solarFields.ApplyModifiedPropertiesWithoutUndo();
                var appFields = new SerializedObject(app);
                Set(appFields, "solarSystem", solar);
                var catalog = appFields.FindProperty("planetCatalog"); catalog.arraySize = 2;
                var mars = AssetDatabase.LoadAssetAtPath<PlanetData>("Assets/Data/Planets/MarsData.asset");
                var earth = AssetDatabase.LoadAssetAtPath<PlanetData>("Assets/Data/Planets/EarthData.asset");
                catalog.GetArrayElementAtIndex(0).objectReferenceValue = mars;
                catalog.GetArrayElementAtIndex(1).objectReferenceValue = earth;
                appFields.ApplyModifiedPropertiesWithoutUndo();
                int narration = 0; app.NarrationRequested += _ => narration++;
                app.Execute(new AppCommand { Type = CommandType.SelectPlanet, Planet = PlanetId.Mars });
                Check(app.CurrentPlanetInfo.Name == "Марс" && narration == 1, "Mars intro and speech");
                app.Execute(new AppCommand { Type = CommandType.ShowFact });
                Check(app.CurrentPlanetInfo.Text == mars.interestingFact && narration == 2, "Mars fact");
                app.Execute(new AppCommand { Type = CommandType.ShowHistory });
                Check(app.CurrentPlanetInfo.Text == mars.discoveryHistory && narration == 3, "Mars history");
                app.Execute(new AppCommand { Type = CommandType.SelectPlanet, Planet = PlanetId.Earth });
                Check(app.CurrentPlanetInfo.Name == "Земля" && app.CurrentPlanetInfo.Image == earth.image, "Earth card");
                var card = app.CurrentPlanetInfo;
                app.Execute(new AppCommand { Type = CommandType.ShowHelp });
                app.Execute(new AppCommand { Type = CommandType.RepeatNarration });
                app.Execute(new AppCommand { Type = CommandType.StopNarration });
                Check(ReferenceEquals(card, app.CurrentPlanetInfo), "Narration must not replace card");
                foreach (var command in new[] { CommandType.ShowOverview, CommandType.Cancel })
                {
                    app.Execute(new AppCommand { Type = command });
                    Check(app.CurrentPlanetInfo == null, "Hide card");
                    app.Execute(new AppCommand { Type = CommandType.SelectPlanet, Planet = PlanetId.Mars });
                    Check(app.CurrentPlanetInfo.Name == "Марс", "Reopen card");
                }
                appFields.Update(); catalog = appFields.FindProperty("planetCatalog"); catalog.ClearArray();
                appFields.ApplyModifiedPropertiesWithoutUndo();
                app.Execute(new AppCommand { Type = CommandType.SelectPlanet, Planet = PlanetId.Earth });
                Check(app.CurrentPlanetInfo.Name == "Земля" && app.CurrentPlanetInfo.Image == null, "Missing data fallback");
                app.Execute(new AppCommand { Type = CommandType.ShowHistory });
                Check(!string.IsNullOrWhiteSpace(app.CurrentPlanetInfo.Text), "Missing history fallback");
                Debug.Log("PLANET INFO VALIDATION PASSED");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Check(bool condition, string scenario)
        {
            if (!condition) throw new InvalidOperationException("Planet info validation failed: " + scenario);
        }
    }
}
