using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Records production map components at a fixed frame step in a disposable preview scene.</summary>
public static class MapTeleportVisualTools
{
    public const string OutputPath = "outputs/agent-battle/teleport-20261109";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Map Visuals/Record Calamity Teleport")]
    public static void Record() => Capture();

    public static string Capture(int width = 1280, int height = 720, int fps = 30)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Record from Edit Mode using the disposable map preview.");
        if (width < 320 || height < 180 || fps < 10 || fps > 60) throw new ArgumentOutOfRangeException();
        Directory.CreateDirectory(Path.Combine(OutputPath, "frames"));
        UnityEngine.Random.State random = UnityEngine.Random.state;
        try
        {
            MapVisualTools.Capture("teleport-last.png", width, height, MapBackgroundPhase.Morning, false, 1,
                customize: (host, camera) => RecordFrames(host, camera, width, height, fps));
            return Path.GetFullPath(Path.Combine(OutputPath, "capture.json"));
        }
        finally { UnityEngine.Random.state = random; }
    }

    private static void RecordFrames(GameObject host, Camera camera, int width, int height, int fps)
    {
        BoardManager board = host.GetComponent<BoardManager>();
        PlayerMapController player = host.GetComponent<PlayerMapController>();
        MapViewController view = host.GetComponent<MapViewController>();
        MapHUD hud = host.GetComponent<MapHUD>();
        ActionPointSystem points = host.GetComponent<ActionPointSystem>();
        RunState state = (RunState)typeof(PlayerMapController).GetField("runState", Private).GetValue(player);
        Calamity_SO calamity = AssetDatabase.LoadAssetAtPath<Calamity_SO>("Assets/Data/Calamity/N1/DimensionalDisorder.asset");
        if (calamity == null) throw new InvalidDataException("Dimensional Disorder calamity asset is missing.");
        Vector2Int origin = state.playerGridPosition;
        board.TryGetTile(new Vector2Int(1, 1), out BoardTileData destination);
        // One eligible target makes the recording reproducible without replacing the actual selection service.
        foreach (BoardTileData tile in board.GetAllTiles()) tile.dangerBand = MapDangerBand.Mid;
        destination.dangerBand = MapDangerBand.Outer;
        destination.isVisited = destination.isResolved = false;
        camera.orthographicSize = 4.6f;
        typeof(MapViewController).GetField("targetZoomSize", Private).SetValue(view, camera.orthographicSize);
        view.FocusOnPosition(player.PawnWorldPosition, true);
        Canvas canvas = host.GetComponentInChildren<Canvas>();
        canvas.transform.SetParent(camera.transform, false);
        RectTransform rect = (RectTransform)canvas.transform;
        rect.localPosition = new Vector3(0, 0, 2);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
        hud.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
        hud.ShowStatus("探索 · 次元紊乱");
        Rebuild(canvas);
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousCameraTarget = camera.targetTexture;
        MapTeleportEffect effect = null;
        int trigger = Mathf.RoundToInt(.6f * fps), count = Mathf.RoundToInt(3.6f * fps), particles = 0;
        int initialPoints = points.CurrentPoints;
        var samples = new List<Frame>();
        try
        {
            camera.targetTexture = target;
            for (int i = 0; i < count; i++)
            {
                if (i == trigger)
                {
                    List<string> feedback = CalamitySelectionService.ApplyBuiltInMapEffects(calamity, state, board, null, tile =>
                    {
                        effect = player.TeleportTo(tile, view, true);
                        hud.ShowTile(tile, 1, true);
                    });
                    if (effect == null) throw new InvalidOperationException("Production teleport did not create a presentation.");
                    particles = effect.ParticleCount;
                    hud.ShowStatus(string.Join("\n", feedback));
                    Rebuild(canvas);
                }
                if (i > trigger && effect != null) effect.Advance(1f / fps);
                view.AdvanceCameraSmoothing(1f / fps);
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply(false);
                File.WriteAllBytes(Path.Combine(OutputPath, "frames", i.ToString("D4") + ".png"), pixels.EncodeToPNG());
                samples.Add(new Frame { frame = i, pawn = player.PawnWorldPosition, camera = camera.transform.position,
                    logical = state.playerGridPosition, teleporting = player.IsTeleporting });
            }
            bool reached = Vector3.Distance(player.PawnWorldPosition, board.GridToWorld(destination.coord) + new Vector3(0, 0, -.2f)) < .001f;
            var evidence = new Recording { source = "Unity MCP / production calamity selection, pawn, portal mesh, camera and HUD; fixed-step preview capture",
                width = width, height = height, fps = fps, frames = count, origin = origin, destination = destination.coord,
                particles = particles, pawnReachedDestination = reached, inputUnlocked = !player.IsTeleporting,
                actionPointsUnchanged = points.CurrentPoints == initialPoints, destinationUnvisited = !destination.isVisited,
                samples = samples.ToArray() };
            File.WriteAllText(Path.Combine(OutputPath, "capture.json"), JsonUtility.ToJson(evidence, true));
            if (!reached || player.IsTeleporting || points.CurrentPoints != initialPoints)
                throw new InvalidOperationException("Teleport recording failed its final state checks.");
        }
        finally
        {
            camera.targetTexture = previousCameraTarget;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    private static void Rebuild(Canvas canvas)
    {
        Canvas.ForceUpdateCanvases();
        foreach (TextMeshProUGUI text in canvas.GetComponentsInChildren<TextMeshProUGUI>()) text.ForceMeshUpdate(true, true);
        foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>()) graphic.Rebuild(CanvasUpdate.PreRender);
        Canvas.ForceUpdateCanvases();
    }

    [Serializable] private class Frame { public int frame; public Vector3 pawn, camera; public Vector2Int logical; public bool teleporting; }
    [Serializable] private class Recording
    {
        public string source;
        public int width, height, fps, frames, particles;
        public Vector2Int origin, destination;
        public bool pawnReachedDestination, inputUnlocked, actionPointsUnchanged, destinationUnvisited;
        public Frame[] samples;
    }
}
