using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Render the real overlay in a disposable scene and sandboxed progression profile.</summary>
public static class AbilityMenuVisualTools
{
    public const string OutputPath = "outputs/agent-battle/ability-ui-20261029";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [MenuItem("Tools/Ability Visuals/Configure Art Imports")]
    public static void ConfigureArtImports()
    {
        foreach (string name in new[] { "ForestSanctuary", "GildedNodeFrame", "GildedPanelFrame" })
        {
            string path = "Assets/Resources/AbilityMenu/" + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            if (name.EndsWith("Frame", StringComparison.Ordinal))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = name == "GildedNodeFrame"
                    ? new Vector4(320f, 240f, 320f, 240f)
                    : new Vector4(300f, 300f, 300f, 300f);
            }
            else
                importer.textureType = TextureImporterType.Default;
            importer.SaveAndReimport();
        }
    }

    [MenuItem("Tools/Ability Visuals/Capture Talent Page")]
    public static void CaptureDefault()
    {
        Capture("talent-page.png", 1920, 1080, "start-money-1", false);
    }

    public static string Capture(string fileName, int width, int height, string selectedId, bool unlockEntry)
    {
        Scene original = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewPreviewScene();
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        Texture2D pixels = null;
        GameObject host = null;
        try
        {
            host = new GameObject("AbilityVisualPreview");
            SceneManager.MoveGameObjectToScene(host, preview);
            AbilityMenuOverlay overlay = host.AddComponent<AbilityMenuOverlay>();
            // Match the fonts wired to the production menu, including unsaved Inspector edits.
            MainMenuSceneController menu = UnityEngine.Object.FindObjectOfType<MainMenuSceneController>();
            SerializedObject menuData = menu != null ? new SerializedObject(menu) : null;
            Font fallback = AssetDatabase.LoadAssetAtPath<Font>("Assets/Assets/Fonts/Aa新华惊马体.ttf");
            Font brush = menuData != null ? menuData.FindProperty("settingsTitleFont").objectReferenceValue as Font : fallback;
            Font body = menuData != null ? menuData.FindProperty("settingsBodyFont").objectReferenceValue as Font : fallback;
            Font buttons = menuData != null ? menuData.FindProperty("settingsButtonFont").objectReferenceValue as Font : fallback;
            overlay.ConfigureFonts(brush, body, buttons);
            AbilityTreeConfig_SO config = AssetDatabase.LoadAssetAtPath<AbilityTreeConfig_SO>("Assets/Configs/DefaultAbilityTreeConfig.asset");
            string sandbox = Path.Combine(OutputPath, "preview-profile");
            Directory.CreateDirectory(sandbox);
            var wallet = new AbilityPointWallet(Path.Combine(sandbox, "wallet.json"));
            wallet.SetPoints(0);
            string progressionPath = Path.Combine(sandbox, "progression.json");
            // An isolated preview profile, never the player's ability save.
            File.WriteAllText(progressionPath, "{\"version\":1,\"unlockedNodeIds\":[],\"enabledNodeIds\":[]}");
            if (unlockEntry)
                new AbilityProgressionService(config, wallet, progressionPath).TryUnlock("journey-start");
            overlay.Show(config, wallet, null, progressionPath);
            Canvas canvas = (Canvas)typeof(AbilityMenuOverlay).GetField("canvas", Private).GetValue(overlay);
            SceneManager.MoveGameObjectToScene(canvas.gameObject, preview);
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = (RectTransform)canvas.transform;
            float viewWidth = 1080f * width / height;
            canvasRect.sizeDelta = new Vector2(viewWidth, 1080f);
            canvasRect.position = Vector3.zero;
            RectTransform composition = canvas.transform.Find("SafeAreaComposition") as RectTransform;
            composition.localScale = Vector3.one * Mathf.Min(viewWidth / 1920f, 1f);
            composition.anchoredPosition = Vector2.zero;
            if (!string.IsNullOrEmpty(selectedId))
            {
                typeof(AbilityMenuOverlay).GetField("selectedNodeId", Private).SetValue(overlay, selectedId);
                typeof(AbilityMenuOverlay).GetMethod("Refresh", Private).Invoke(overlay, null);
            }
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            GameObject cameraObject = new GameObject("AbilityPreviewCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.transform.position = new Vector3(0f, 0f, -1500f);
            camera.orthographic = true;
            camera.orthographicSize = 540f;
            camera.farClipPlane = 3000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.03f, 0.04f);
            camera.targetTexture = target;
            canvas.worldCamera = camera;
            // EnvelopeParent and content fitters normally update on the next editor tick.
            var background = canvas.transform.Find("ForestSanctuary") as RectTransform;
            AspectRatioFitter aspect = background.GetComponent<AspectRatioFitter>();
            aspect.enabled = false;
            float backdropWidth = Mathf.Max(viewWidth, 1080f * aspect.aspectRatio);
            background.anchorMin = background.anchorMax = new Vector2(0.5f, 0.5f);
            background.sizeDelta = new Vector2(backdropWidth, backdropWidth / aspect.aspectRatio);
            background.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            foreach (ContentSizeFitter fitter in canvas.GetComponentsInChildren<ContentSizeFitter>())
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)fitter.transform);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory(OutputPath);
            string path = Path.Combine(OutputPath, fileName);
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            return Path.GetFullPath(path);
        }
        finally
        {
            RenderTexture.active = previous;
            if (host != null)
                UnityEngine.Object.DestroyImmediate(host);
            if (pixels != null)
                UnityEngine.Object.DestroyImmediate(pixels);
            if (target != null)
                UnityEngine.Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(preview);
            if (original.IsValid() && original.isLoaded)
                SceneManager.SetActiveScene(original);
        }
    }
}
