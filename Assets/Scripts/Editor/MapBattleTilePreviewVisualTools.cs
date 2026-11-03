using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Renders the actual preview controller, camera, pawn and heraldic overlay without committing a move.</summary>
public static class MapBattleTilePreviewVisualTools
{
    public const string OutputPath = "outputs/agent-battle/battle-tile-ui-20261103";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Map Visuals/Capture Heraldic Battle Tile Preview")]
    public static void BatchPreview()
    {
        Capture("battle-preview-elite.png", 2340, 1080, MapTileType.Elite, 2, new[] { "炎" });
        Capture("battle-preview-ordinary.png", 1920, 1080, MapTileType.Battle, 4, new[] { "水", "血", "暗" });
        Capture("battle-preview-boss-locked.png", 2340, 1080, MapTileType.Boss, 8, new[] { "暗", "毒", "血" }, "首领尚未解锁：1/3 钥匙。请先收集钥匙。");
        Capture("battle-preview-tablet.png", 1600, 1200, MapTileType.Elite, 3, new[] { "光", "电" });
        Capture("battle-preview-portrait.png", 1080, 1920, MapTileType.Elite, 2, new[] { "炎", "水", "速" });
        Capture("battle-preview-safe-area.png", 2340, 1080, MapTileType.Elite, 2, new[] { "炎" }, safe: new Rect(96, 30, 2148, 1020));
        Capture("battle-preview-failure.png", 1920, 1080, MapTileType.Boss, 7, new[] { "血", "暗" },
            "本次移动未能完成，请重试或返回地图。\n" + string.Concat(Enumerable.Repeat("请返回地图调整路线，当前无法进入这场战斗。", 6)), retryableFailure: true);
        Capture("battle-preview-action-point-blocked.png", 2340, 1080, MapTileType.Elite, 2, new[] { "炎" }, "行动点不足，需要 2 点。");
        File.WriteAllText(Path.Combine(OutputPath, "preview-complete.json"),
            "{\"success\":true,\"images\":8,\"actualPreviewController\":true,\"playerSavesUsed\":false,\"movesCommitted\":false}");
    }

    public static string Capture(string name, int width, int height, MapTileType type, int danger, string[] attributes,
        string blocked = null, Rect? safe = null, bool retryableFailure = false)
    {
        string source = MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, customize: (host, camera) =>
        {
            BoardManager board = host.GetComponent<BoardManager>();
            PlayerMapController player = host.GetComponent<PlayerMapController>();
            MapViewController view = host.GetComponent<MapViewController>();
            MapHUD hud = host.GetComponent<MapHUD>();
            Canvas hudCanvas = hud.GetComponentInChildren<Canvas>();
            BoardTileData tile;
            if (!board.TryGetTile(new Vector2Int(2, 5), out tile)) throw new InvalidDataException("Sample battle tile is missing.");
            tile.tileType = type;
            tile.previewDangerRating = danger;
            tile.dangerBand = type == MapTileType.Boss ? MapDangerBand.Core : MapDangerBand.Outer;
            tile.isVisited = false;
            tile.isResolved = false;
            board.SetSelectedTile(tile.coord);
            board.RefreshTile(tile.coord);
            hud.ShowTile(tile, 2, true);
            MapBattleTilePreviewOverlay overlay = host.AddComponent<MapBattleTilePreviewOverlay>();
            overlay.ConfigureTheme(Resources.Load<MapBattlePreviewTheme>("MapUI/BattlePreviewTheme"));
            ConfigureMapFonts(overlay);
            overlay.ConfigureMapHud(hud);
            MapBattleTilePreviewController controller = host.AddComponent<MapBattleTilePreviewController>();
            controller.Configure(view, player, board, overlay, (_, __) => throw new InvalidOperationException("Preview must not commit."), _ => { });
            if (!controller.BeginPreview(new MapBattleTilePreviewContext
            {
                tile = tile, dangerRating = danger, moveCost = 2, attributeNames = attributes,
                canPreview = true, canConfirm = string.IsNullOrEmpty(blocked) || retryableFailure, blockReason = retryableFailure ? string.Empty : blocked ?? string.Empty,
                confirmLabel = "进入战斗 · 预计 2 AP"
            }, true)) throw new InvalidDataException("Production preview controller did not enter Ready.");
            if (retryableFailure) overlay.ShowFailure(blocked);
            Canvas canvas = (Canvas)typeof(MapBattleTilePreviewOverlay).GetField("canvas", Private).GetValue(overlay);
            PrepareCanvas(hudCanvas, camera, width, height, true);
            PrepareCanvas(canvas, camera, width, height);
            overlay.ApplyResponsiveLayout(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>()) graphic.Rebuild(CanvasUpdate.PreRender);
            Canvas.ForceUpdateCanvases();
            if (hudCanvas.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "TilePanel" && r.gameObject.activeSelf))
                throw new InvalidDataException("Duplicate tile details remain visible behind the preview.");
        });
        Directory.CreateDirectory(OutputPath);
        string destination = Path.GetFullPath(Path.Combine(OutputPath, name));
        File.Copy(source, destination, true);
        return destination;
    }

    private static void ConfigureMapFonts(MapBattleTilePreviewOverlay overlay)
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/MapScene.scene");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/MapScene.scene", OpenSceneMode.Additive);
        try
        {
            RunGameManager manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RunGameManager>(true)).First();
            SerializedObject settings = new SerializedObject(manager);
            overlay.ConfigureFonts(settings.FindProperty("mapKeywordFont").objectReferenceValue as TMP_FontAsset,
                settings.FindProperty("mapBodyFont").objectReferenceValue as TMP_FontAsset,
                settings.FindProperty("mapAttributeFont").objectReferenceValue as TMP_FontAsset);
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void PrepareCanvas(Canvas canvas, Camera camera, int width, int height, bool forestHud = false)
    {
        canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        canvas.transform.SetParent(camera.transform, false);
        float factor = forestHud ? height / 1080f : Mathf.Sqrt(width / 1920f * height / 1080f);
        float distance = Mathf.Max(1f, camera.nearClipPlane + 0.5f);
        float worldHeight = camera.orthographic ? camera.orthographicSize * 2f : 2f * distance * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
        RectTransform rect = (RectTransform)canvas.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(width, height) / factor;
        rect.localPosition = new Vector3(0, 0, distance);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * (worldHeight / rect.sizeDelta.y);
    }
}
