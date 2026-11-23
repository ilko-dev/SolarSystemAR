using System;
using System.IO;
using SolarSystemAR.Core;
using SolarSystemAR.UI;
using UnityEditor;
using UnityEngine;

namespace SolarSystemAR.Editor
{
    /// <summary>Renders UI globe sprites from the project's existing planet surface maps.</summary>
    public static class PlanetPortraitSetup
    {
        private const string Folder = "Assets/UI/PlanetInfo/Portraits";
        private const string Prefab = "Assets/UI/PlanetInfo/PlanetInfoPanel.prefab";

        [InitializeOnLoadMethod]
        private static void Schedule()
        {
            EditorApplication.update -= WhenReady;
            EditorApplication.update += WhenReady;
        }

        private static void WhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= WhenReady;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefab) != null) Refresh();
        }

        [MenuItem("Solar System/Refresh Planet Portraits")]
        public static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            bool changed = false;
            foreach (PlanetId id in Enum.GetValues(typeof(PlanetId)))
            {
                var data = AssetDatabase.LoadAssetAtPath<PlanetData>($"Assets/Data/Planets/{id}Data.asset");
                string sourcePath = $"Assets/Textures/{id}.png";
                if (data == null || data.image != null || !File.Exists(sourcePath)) continue;
                string path = $"{Folder}/{id}.png";
                if (!File.Exists(path)) RenderGlobe(sourcePath, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                data.image = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                EditorUtility.SetDirty(data);
                changed = true;
            }
            // Preserve all existing prefab object IDs and references.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefab) != null)
            {
                var root = PrefabUtility.LoadPrefabContents(Prefab);
                try
                {
                    var panel = root.GetComponent<PlanetInfoPanel>();
                    var fields = new SerializedObject(panel);
                    var card = (RectTransform)fields.FindProperty("card").objectReferenceValue;
                    if (card.sizeDelta != new Vector2(500, 320))
                    {
                        panel.ApplyCompactStyle();
                        card.sizeDelta = new Vector2(500, 320);
                        card.anchoredPosition = new Vector2(-16, -64);
                        PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                        changed = true;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("Compact planet panel and planet portraits ready.");
            }
        }

        private static void RenderGlobe(string sourcePath, string destination)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var output = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(sourcePath)))
                    throw new InvalidOperationException("Cannot load planet texture: " + sourcePath);
                source.wrapMode = TextureWrapMode.Repeat;
                var pixels = new Color[256 * 256];
                Vector3 light = new Vector3(-0.4f, 0.5f, 1).normalized;
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        float nx = (x - 127.5f) / 124f;
                        float ny = (y - 127.5f) / 124f;
                        float radius = nx * nx + ny * ny;
                        if (radius >= 1) continue;
                        float nz = Mathf.Sqrt(1 - radius);
                        float u = 0.5f + Mathf.Atan2(nx, nz) / (2 * Mathf.PI);
                        float v = 0.5f + Mathf.Asin(ny) / Mathf.PI;
                        Color color = source.GetPixelBilinear(u, v);
                        float shade = 0.22f + 0.78f * Mathf.Max(0, Vector3.Dot(new Vector3(nx, ny, nz), light));
                        color *= shade;
                        color.a = Mathf.Clamp01((1 - Mathf.Sqrt(radius)) * 124);
                        pixels[y * 256 + x] = color;
                    }
                output.SetPixels(pixels);
                output.Apply();
                File.WriteAllBytes(destination, output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(output);
            }
        }
    }
}
