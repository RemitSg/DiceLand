using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>Reproducible event art imports and real-component captures without opening player saves.</summary>
public static class MapEventStoryVisualTools
{
    public const string OutputPath = "outputs/agent-battle/event-story-20261109";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("Tools/Map Visuals/Configure Story Event Art")]
    public static void ConfigureArt()
    {
        foreach (string file in Directory.GetFiles("Assets/Resources/EventUI/Scenes", "*.png"))
        {
            string path = file.Replace('\\', '/'); AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidDataException(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = false; importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        const string fontPath = "Assets/Resources/EventUI/Typography/EventStorySerif.asset";
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (font == null)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/EventUI/Typography/SourceHanSerifSC-Regular.otf");
            font = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = "Event Story Serif SDF";
            font.TryAddCharacters("旁白旅途事件选择行动确认后果预览点击继续结束金币生命值abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789。，！？：；（）%+- ");
            AssetDatabase.CreateAsset(font, fontPath); AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D texture in font.atlasTextures) if (texture != null && !AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, font);
        }
        const string themePath = "Assets/Resources/EventUI/Theme.asset";
        MapEventStoryTheme theme = AssetDatabase.LoadAssetAtPath<MapEventStoryTheme>(themePath);
        if (theme == null) { theme = ScriptableObject.CreateInstance<MapEventStoryTheme>(); AssetDatabase.CreateAsset(theme, themePath); }
        theme.headingFont = theme.bodyFont = font; EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssetIfDirty(font); AssetDatabase.SaveAssetIfDirty(theme);
    }

    public static string Capture(string fileName, int width = 1920, int height = 1080, string eventName = "医疗箱", bool result = false, bool unavailable = false, Rect? safe = null)
    {
        Directory.CreateDirectory(OutputPath);
        string source = MapEventVisualTools.Capture(fileName, width, height, unavailable: unavailable, result: result, safe: safe, eventName: eventName);
        string destination = Path.Combine(OutputPath, fileName); File.Copy(source, destination, true); return Path.GetFullPath(destination);
    }

    [MenuItem("Tools/Map Visuals/Record Story Event")]
    public static void RecordDefault() => Record();
    public static string Record(int width = 960, int height = 540, int fps = 20)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use the disposable Edit Mode preview for capture.");
        string framesPath = Path.Combine(OutputPath, "frames"); Directory.CreateDirectory(framesPath);
        int executions = 0, count = fps * 11, resultFrame = -1, closeFrame = -1;
        MapEventVisualTools.Capture("story-recording-last.png", width, height, eventName: "医疗箱", customize: (overlay, camera) =>
        {
            MapEvent_SO data = (MapEvent_SO)typeof(MapEventOverlay).GetField("currentEvent", Private).GetValue(overlay);
            BoardTileData tile = (BoardTileData)typeof(MapEventOverlay).GetField("currentTile", Private).GetValue(overlay);
            overlay.ConfigurePreviewPlayback(true);
            overlay.Show(tile, data, (BoardTileData t, MapEvent_SO e, MapEventOptionData option, out string message, out string result, out MapEvent_SO next) =>
            { executions++; message = option.consequenceDescription; result = option.resultContent; next = null; return true; });
            overlay.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
            typeof(MapEventOverlay).GetMethod("RefreshLayout", Private).Invoke(overlay, null);
            Canvas canvas = overlay.GetComponentInChildren<Canvas>(true);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active, cameraTarget = camera.targetTexture;
            var frames = new List<Frame>();
            try
            {
                camera.targetTexture = target;
                for (int i = 0; i < count; i++)
                {
                    overlay.AdvancePresentation(1f / fps);
                    // Read all pages first. Each action either reveals this page or turns to the next one.
                    if (i > fps * 3 && i % fps == 0 && !overlay.IsNarrationComplete) overlay.AdvanceDialogue();
                    if (i == fps * 5 && overlay.AreChoicesReady)
                        canvas.GetComponentsInChildren<MapEventChoiceFeedback>(true).ElementAt(1).OnPointerEnter(null);
                    if (i == fps * 6 && overlay.AreChoicesReady)
                        canvas.GetComponentsInChildren<MapEventChoiceFeedback>(true).First().OnPointerEnter(null);
                    if (i >= fps * 6.5f && resultFrame < 0 && overlay.AreChoicesReady)
                    {
                        canvas.GetComponentsInChildren<Button>(true).Single(b => b.name == "ConfirmButton").onClick.Invoke(); resultFrame = i;
                    }
                    if (resultFrame >= 0 && i > resultFrame + fps * 2 && closeFrame < 0 && overlay.IsNarrationComplete)
                    { overlay.AdvanceDialogue(); closeFrame = i; }
                    Canvas.ForceUpdateCanvases();
                    foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
                    foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>()) graphic.Rebuild(CanvasUpdate.PreRender);
                    camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply(false);
                    File.WriteAllBytes(Path.Combine(framesPath, i.ToString("D4") + ".png"), pixels.EncodeToPNG());
                    frames.Add(new Frame { frame = i, typing = overlay.IsTyping, choices = overlay.AreChoicesReady, closing = overlay.IsClosing, visible = overlay.IsVisible });
                }
                var recording = new Recording { width = width, height = height, fps = fps, frames = count, choicesExecuted = executions,
                    resultFrame = resultFrame, closeFrame = closeFrame, samples = frames.ToArray(),
                    source = "Unity MCP; production MapEventOverlay fixed-step playback in a disposable scene; preview handler, no player saves or rewards changed" };
                File.WriteAllText(Path.Combine(OutputPath, "recording.json"), JsonUtility.ToJson(recording, true));
                if (executions != 1 || resultFrame < 0 || closeFrame < 0 || overlay.IsVisible) throw new InvalidOperationException("Story recording did not finish the complete event exactly once.");
            }
            finally { camera.targetTexture = cameraTarget; RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels); }
        });
        return Path.GetFullPath(Path.Combine(OutputPath, "recording.json"));
    }
    [Serializable] private class Frame { public int frame; public bool typing, choices, closing, visible; }
    [Serializable] private class Recording
    {
        public string source; public int width, height, fps, frames, choicesExecuted, resultFrame, closeFrame; public Frame[] samples;
    }
}
