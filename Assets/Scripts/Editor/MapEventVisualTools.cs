using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real event UI over the disposable map preview; never reads saves or executes event rewards.</summary>
public static class MapEventVisualTools
{
    public const string OutputPath = "outputs/agent-battle/event-ui-20261102";

    [MenuItem("Tools/Map Visuals/Capture Event Panel")]
    public static void CaptureDefault() { Capture("event-campfire.png", 2340, 1080); }

    public static void BatchPreview()
    {
        CaptureDefault();
        Capture("event-16x9.png", 1920, 1080);
        Capture("event-tablet.png", 1600, 1200);
        Capture("event-safe-area.png", 2400, 1080, safe: new Rect(140f, 34f, 2210f, 1012f));
        Capture("event-long.png", 2340, 1080, longContent: true);
        Capture("event-unavailable.png", 2340, 1080, unavailable: true);
        Capture("event-result.png", 2340, 1080, result: true);
        File.WriteAllText(Path.Combine(OutputPath, "preview-complete.json"), "{\"success\":true,\"images\":7,\"playerSavesUsed\":false,\"eventEffectsExecuted\":false}");
    }

    public static string Capture(string name, int width, int height, bool longContent = false, bool unavailable = false, bool result = false, Rect? safe = null)
    {
        MapEvent_SO campfire = null;
        foreach (string guid in AssetDatabase.FindAssets("t:MapEvent_SO", new[] { "Assets/Data/Events" }))
        {
            MapEvent_SO item = AssetDatabase.LoadAssetAtPath<MapEvent_SO>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && item.eventName == "熄灭的营火") { campfire = item; break; }
        }
        if (campfire == null) throw new InvalidDataException("Production campfire event not found.");
        MapEvent_SO sample = UnityEngine.Object.Instantiate(campfire);
        try
        {
            if (longContent)
            {
                sample.eventContent = string.Join("\n\n", new string[12].Fill(campfire.eventContent));
                sample.options = new List<MapEventOptionData>();
                for (int i = 0; i < 8; i++)
                    sample.options.Add(new MapEventOptionData { optionLabel = "长选项阅读样例 " + (i + 1), consequenceDescription = string.Join("；", new string[5].Fill(campfire.options[0].consequenceDescription)) });
            }
            string rendered = MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, safe, (host, camera) =>
            {
                BoardManager board = host.GetComponent<BoardManager>();
                board.TryGetTile(new Vector2Int(1, 3), out BoardTileData tile);
                tile.tileType = MapTileType.Event;
                tile.isVisited = true;
                tile.assignedEvent = sample;
                host.GetComponent<MapHUD>().ShowTile(tile, 0, false);
                GameObject eventHost = new GameObject("EventPanelPreview");
                eventHost.transform.SetParent(host.transform, false);
                MapEventOverlay overlay = eventHost.AddComponent<MapEventOverlay>();
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
                overlay.ConfigureFonts(font, font);
                overlay.Show(tile, sample, PreviewChoice, (t, e, option) => unavailable && option == sample.options[0]
                    ? new MapEventOverlay.EventOptionPreviewData { isSelectable = false, unavailableReason = "当前不满足该选项的执行条件。" } : null);
                Canvas canvas = overlay.GetComponentInChildren<Canvas>();
                canvas.GetComponent<CanvasScaler>().enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                RectTransform rect = (RectTransform)canvas.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1080f * width / height, 1080f);
                rect.position = new Vector3(0f, 0f, -3f);
                rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
                overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0f, 0f, width, height));
                if (result) canvas.transform.Find("SafeArea/Panel/ConfirmButton").GetComponent<Button>().onClick.Invoke();
                typeof(MapEventOverlay).GetMethod("RefreshLayout", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(overlay, null);
                Canvas.ForceUpdateCanvases();
                foreach (TextMeshProUGUI text in canvas.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate(true, true);
                foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>()) graphic.Rebuild(CanvasUpdate.PreRender);
            });
            Directory.CreateDirectory(OutputPath);
            string destination = Path.Combine(OutputPath, name);
            File.Copy(rendered, destination, true);
            return Path.GetFullPath(destination);
        }
        finally { UnityEngine.Object.DestroyImmediate(sample); }
    }

    private static bool PreviewChoice(BoardTileData tile, MapEvent_SO data, MapEventOptionData option, out string message, out string result, out MapEvent_SO next)
    {
        message = option.consequenceDescription;
        result = option.resultContent;
        next = option.nextEvent;
        return true;
    }

    private static string[] Fill(this string[] values, string value)
    {
        for (int i = 0; i < values.Length; i++) values[i] = value;
        return values;
    }
}
