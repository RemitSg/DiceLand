using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MenuAction = MainMenuSceneController.MenuAction;

/// <summary>Explicit import setup and disposable renders; never changes player saves.</summary>
public static class MainMenuVisualTools
{
    public const string OutputPath = "outputs/agent-battle/main-menu-20261030";
    public const string FontPath = "Assets/Assets/Fonts/Hi字楷体简.ttf";

    [MenuItem("Tools/Main Menu Visuals/Configure Art Imports")]
    public static void ConfigureArtImports()
    {
        foreach (string name in new[] { "ForestJourney", "DiceLandLogo", "GildedButton" })
        {
            string path = "Assets/Resources/MainMenu/" + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing menu art: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency = name != "ForestJourney";
            if (name == "GildedButton") ConfigureButtonSprite(importer, path);
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureButtonSprite(TextureImporter importer, string path)
    {
        // Crop only the Sprite import rectangle; retain the generated PNG and its alpha intact.
        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            source.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = source.GetPixels32();
            int left=source.width, right=0, bottom=source.height, top=0;
            for (int y=0; y<source.height; y++)
                for (int x=0; x<source.width; x++)
                    if (pixels[y*source.width+x].a > 12)
                    { left=Mathf.Min(left,x); right=Mathf.Max(right,x); bottom=Mathf.Min(bottom,y); top=Mathf.Max(top,y); }
            left=Mathf.Max(0,left-2); bottom=Mathf.Max(0,bottom-2);
            right=Mathf.Min(source.width-1,right+2); top=Mathf.Min(source.height-1,top+2);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            // This project's Unity 2022 importer supports the legacy sheet API.
#pragma warning disable 0618
            importer.spritesheet = new[] { new SpriteMetaData { name="GildedButton",
                rect=new Rect(left,bottom,right-left+1,top-bottom+1), pivot=new Vector2(0.5f,0.5f), alignment=9,
                border=new Vector4(150f,26f,150f,26f) } };
#pragma warning restore 0618
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
    }

    [MenuItem("Tools/Main Menu Visuals/Configure Menu Font")]
    public static void ConfigureMenuFont()
    {
        MainMenuSceneController menu = UnityEngine.Object.FindObjectOfType<MainMenuSceneController>();
        if (menu == null) throw new InvalidOperationException("Open the Menu Selection scene first.");
        Undo.RecordObject(menu, "Configure forest menu font");
        SerializedObject data = new SerializedObject(menu);
        data.FindProperty("forestMenuFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        data.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
    }

    [MenuItem("Tools/Main Menu Visuals/Capture Phone Menu")]
    public static void CapturePhone() => Capture("menu-phone.png", 2340, 1080);

    public static string Capture(string fileName, int width, int height, bool difficulty = false, bool hasSave = false,
        MenuAction focus = MenuAction.NewGame, float hoverSeconds = 0.6f)
    {
        Scene original = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewPreviewScene();
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        Texture2D pixels = null;
        GameObject host = null;
        try
        {
            MainMenuSceneController menu = UnityEngine.Object.FindObjectOfType<MainMenuSceneController>();
            DifficultyConfig_SO[] configs = menu != null ? menu.GetDifficultyConfigurations() : Array.Empty<DifficultyConfig_SO>();
            host = new GameObject("MainMenuVisualPreview");
            SceneManager.MoveGameObjectToScene(host, preview);
            MainMenuForestView view = host.AddComponent<MainMenuForestView>();
            var labels = new Dictionary<MenuAction, string>
            {
                { MenuAction.NewGame, "新游戏" }, { MenuAction.Continue, "继续游戏" }, { MenuAction.Ability, "能力" },
                { MenuAction.Tutorial, "教程" }, { MenuAction.Settings, "设置" }, { MenuAction.Contributors, "制作人员" },
                { MenuAction.Exit, "退出" }, { MenuAction.DemoTest, "测试战斗" }
            };
            view.Build(AssetDatabase.LoadAssetAtPath<Font>(FontPath), labels, configs, null, null);
            view.SetContinueAvailable(hasSave);
            view.Hover(focus);
            MethodInfo tick = typeof(MainMenuForestView).GetMethod("TickMotion", BindingFlags.Instance | BindingFlags.NonPublic);
            int previewFrames = Mathf.CeilToInt(Mathf.Clamp(hoverSeconds, 0f, 1.5f) * 60f);
            for (int i = 0; i < previewFrames; i++) tick.Invoke(view, new object[] { 1f / 60f });
            if (difficulty) view.ShowDifficulty();
            Canvas canvas = view.Canvas;
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = (RectTransform)canvas.transform;
            canvasRect.sizeDelta = new Vector2(1080f * width / height, 1080f);
            canvasRect.position = Vector3.zero;
            view.ApplyLayout(new Vector2(width, height), new Rect(0f, 0f, width, height));
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            GameObject cameraObject = new GameObject("MainMenuPreviewCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.transform.position = new Vector3(0f, 0f, -1500f);
            camera.orthographic = true;
            camera.orthographicSize = 540f;
            camera.farClipPlane = 3000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.025f, 0.03f);
            camera.targetTexture = target;
            canvas.worldCamera = camera;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory(OutputPath);
            string path = Path.Combine(OutputPath, fileName);
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            return Path.GetFullPath(path);
        }
        finally
        {
            RenderTexture.active = previous;
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            EditorSceneManager.ClosePreviewScene(preview);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }
}
