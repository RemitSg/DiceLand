using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Import only map art and render production map components in a disposable preview scene.</summary>
public static class MapVisualTools
{
    public const string OutputPath = "outputs/agent-battle/map-ui-20261102";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string[] Names = { "Sword", "Elite", "Event", "Merchant", "Treasure", "Forge", "Rest", "Key", "Boss", "Footprint", "Compass", "Coins", "TileFace", "PanelFrame", "PartyCard", "ButtonFace" };

    [MenuItem("Tools/Map Visuals/Configure Forest Art Imports")]
    public static void ConfigureArtImports()
    {
        const string atlasPath = "Assets/Resources/MapUI/MapAtlas.png";
        TextureImporter atlas = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
        if (atlas == null) throw new FileNotFoundException(atlasPath);
        ConfigureSprite(atlas);
        atlas.spriteImportMode = SpriteImportMode.Multiple;
        atlas.textureCompression = TextureImporterCompression.Uncompressed;
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(atlas);
        provider.InitSpriteEditorDataProvider();
        Dictionary<string, SpriteRect> previous = provider.GetSpriteRects().ToDictionary(rect => rect.name);
        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            source.LoadImage(File.ReadAllBytes(atlasPath));
            Color32[] colors = source.GetPixels32();
            float[] rows = { 0f, 0.267f, 0.507f, 0.735f, 1f };
            SpriteRect[] entries = new SpriteRect[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                int column = i % 4, row = i / 4;
                int left = Mathf.RoundToInt(column * source.width / 4f), right = Mathf.RoundToInt((column + 1) * source.width / 4f);
                int bottom = source.height - Mathf.RoundToInt(rows[row + 1] * source.height);
                int top = source.height - Mathf.RoundToInt(rows[row] * source.height);
                int minX = right, maxX = left, minY = top, maxY = bottom;
                for (int y = bottom; y < top; y++)
                    for (int x = left; x < right; x++)
                        if (colors[y * source.width + x].a >= 32)
                        {
                            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                        }
                if (maxX <= minX || maxY <= minY) throw new InvalidDataException("Missing map sprite " + Names[i]);
                minX = Mathf.Max(left, minX - 2); minY = Mathf.Max(bottom, minY - 2);
                maxX = Mathf.Min(right, maxX + 3); maxY = Mathf.Min(top, maxY + 3);
                float border = i == 12 ? 12f : i == 13 ? 40f : i == 14 ? 30f : i == 15 ? 24f : 0f;
                entries[i] = new SpriteRect { name = Names[i], rect = new Rect(minX, minY, maxX - minX, maxY - minY),
                    alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f), border = Vector4.one * border,
                    spriteID = previous.TryGetValue(Names[i], out SpriteRect existing) ? existing.spriteID : GUID.Generate() };
            }
            provider.SetSpriteRects(entries);
            ISpriteNameFileIdDataProvider nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameIds != null)
            {
                Dictionary<string, SpriteNameFileIdPair> previousIds = nameIds.GetNameFileIdPairs().ToDictionary(pair => pair.name);
                nameIds.SetNameFileIdPairs(entries.Select(entry => previousIds.TryGetValue(entry.name, out SpriteNameFileIdPair pair)
                    ? pair : new SpriteNameFileIdPair(entry.name, entry.spriteID)));
            }
            provider.Apply();
            atlas.SaveAndReimport();
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        foreach (MapBackgroundPhase phase in Enum.GetValues(typeof(MapBackgroundPhase)))
        {
            string path = "Assets/Resources/MapUI/Forest" + phase + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            ConfigureSprite(importer);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        const string pawnPath = "Assets/Resources/MapUI/ChibiPawn.png";
        TextureImporter pawn = AssetImporter.GetAtPath(pawnPath) as TextureImporter;
        if (pawn == null) throw new FileNotFoundException(pawnPath);
        ConfigureSprite(pawn);
        pawn.spriteImportMode = SpriteImportMode.Single;
        pawn.textureCompression = TextureImporterCompression.Uncompressed;
        pawn.maxTextureSize = 512;
        pawn.SaveAndReimport();
        MapForestArt.Reload();
        if (Resources.LoadAll<Sprite>("MapUI/MapAtlas").Length != 16) throw new InvalidDataException("Expected sixteen imported map sprites.");
    }

    private static void ConfigureSprite(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }

    [MenuItem("Tools/Map Visuals/Capture Forest Map")]
    public static void CaptureDefault()
    {
        Capture("map-phone-morning.png", 2340, 1080, MapBackgroundPhase.Morning, false, 1);
    }

    public static void BatchPreview()
    {
        Directory.CreateDirectory(OutputPath);
        try
        {
            ConfigureArtImports();
            foreach (MapBackgroundPhase phase in Enum.GetValues(typeof(MapBackgroundPhase)))
                Capture("map-phone-" + phase.ToString().ToLowerInvariant() + ".png", 2340, 1080, phase, false, 1);
            Capture("map-wide-16x9.png", 1920, 1080, MapBackgroundPhase.Morning, true, 4);
            Capture("map-tablet-4x3.png", 1600, 1200, MapBackgroundPhase.Morning, false, 4);
            Capture("map-phone-hostile.png", 2340, 1080, MapBackgroundPhase.Morning, true, 4);
            Capture("map-phone-notch.png", 2400, 1080, MapBackgroundPhase.Dusk, false, 1, new Rect(90f, 0f, 2220f, 1080f));
            File.WriteAllText(Path.Combine(OutputPath, "preview-complete.json"), "{\"success\":true,\"images\":8,\"source\":\"production MapHUD/BoardManager/MapBackgroundController; disposable reference-layout scene\",\"playerSavesUsed\":false}");
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(OutputPath, "preview-error.txt"), error.ToString());
            Debug.LogException(error);
            throw;
        }
    }

    public static string Capture(string fileName, int width, int height, MapBackgroundPhase phase, bool hostile, int partyCount, Rect? safeArea = null)
    {
        Scene original = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewPreviewScene();
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        Texture2D pixels = null;
        GameObject host = null;
        try
        {
            host = new GameObject("MapVisualPreview");
            SceneManager.MoveGameObjectToScene(host, preview);
            GameObject cameraObject = new GameObject("MapPreviewCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.aspect = width / (float)height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.04f, 0.04f);
            camera.farClipPlane = 100f;
            GameObject light = new GameObject("MapPreviewLight", typeof(Light));
            SceneManager.MoveGameObjectToScene(light, preview);
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
            if (font == null) throw new InvalidDataException("Production map font not found.");
            BoardManager board = host.AddComponent<BoardManager>();
            typeof(BoardManager).GetField("worldTextFont", Private).SetValue(board, font);
            List<BoardTileData> tiles = ReferenceBoard();
            Vector2Int spawn = new Vector2Int(3, 5);
            Vector2Int selected = hostile ? new Vector2Int(3, 3) : spawn;
            board.LoadBoard(7, spawn, new Vector2Int(3, 3), selected, tiles);
            RunState state = new RunState { difficultyName = "Easy", money = 0, currentKeys = 0, requiredKeys = 2, playerGridPosition = spawn };
            string[] characterGuids = AssetDatabase.FindAssets("t:CharacterData_SO", new[] { "Assets/Data" });
            CharacterData_SO first = null;
            foreach (string guid in characterGuids)
            {
                CharacterData_SO character = AssetDatabase.LoadAssetAtPath<CharacterData_SO>(AssetDatabase.GUIDToAssetPath(guid));
                if (first == null) first = character;
                if (character != null && character.characterName == "Chara") { first = character; break; }
            }
            for (int i = 0; i < partyCount; i++)
                state.partyMembers.Add(new RunPartyMemberState { memberId = "Preview" + i, characterData = first, level = 1, currentHP = 17, maxHP = 17 });
            ActionPointSystem points = host.AddComponent<ActionPointSystem>();
            points.Initialize(7);
            if (phase == MapBackgroundPhase.Noon) points.TrySpend(4);
            if (phase == MapBackgroundPhase.Dusk) points.TrySpend(6);
            MapViewController view = host.AddComponent<MapViewController>();
            view.Initialize(camera, board, 2.8f);
            GameObject backgroundRoot = new GameObject("MapBackground");
            SceneManager.MoveGameObjectToScene(backgroundRoot, preview);
            MapBackgroundController background = host.AddComponent<MapBackgroundController>();
            background.Initialize(backgroundRoot.transform, camera, board, points);
            if (phase == MapBackgroundPhase.Midnight)
            {
                SpriteRenderer backdrop = backgroundRoot.transform.Find("ImageBackdrop").GetComponent<SpriteRenderer>();
                backdrop.sprite = MapForestArt.Background(MapBackgroundPhase.Midnight);
            }
            TileResolver resolver = host.AddComponent<TileResolver>();
            PlayerMapController player = host.AddComponent<PlayerMapController>();
            player.Initialize(host.transform, board, points, resolver, state);
            board.TryGetTile(spawn, out BoardTileData spawnTile);
            player.SetSpawn(spawnTile);
            MapHUD hud = host.AddComponent<MapHUD>();
            hud.ConfigureFonts(font, font, font);
            string[] tables = AssetDatabase.FindAssets("t:LevelTable_SO", new[] { "Assets/Data" });
            if (tables.Length > 0) hud.ConfigureLevelTable(AssetDatabase.LoadAssetAtPath<LevelTable_SO>(AssetDatabase.GUIDToAssetPath(tables[0])));
            hud.Refresh(state, points);
            board.TryGetTile(selected, out BoardTileData tile);
            if (hostile)
            {
                string[] groups = AssetDatabase.FindAssets("t:EnemyGroup_SO", new[] { "Assets/Data" });
                if (groups.Length > 0) tile.assignedEnemyGroup = AssetDatabase.LoadAssetAtPath<EnemyGroup_SO>(AssetDatabase.GUIDToAssetPath(groups[0]));
                tile.previewDangerRating = 6;
                tile.previewTag = "森林守卫";
            }
            hud.ShowTile(tile, hostile ? 1 : 0, hostile);
            Canvas canvas = host.GetComponentInChildren<Canvas>();
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            RectTransform canvasRect = (RectTransform)canvas.transform;
            canvasRect.anchorMin = canvasRect.anchorMax = canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1080f * width / height, 1080f);
            canvasRect.position = new Vector3(0f, 0f, -2f);
            canvasRect.localScale = Vector3.one * (camera.orthographicSize / 540f);
            hud.ApplyViewport(new Vector2(width, height), safeArea ?? new Rect(0f, 0f, width, height));
            Canvas.ForceUpdateCanvases();
            foreach (TextMeshProUGUI text in canvas.GetComponentsInChildren<TextMeshProUGUI>())
                text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
                graphic.Rebuild(CanvasUpdate.PreRender);
            Canvas.ForceUpdateCanvases();
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
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
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            HashSet<UnityEngine.Object> generated = CollectGeneratedResources(host);
            EditorSceneManager.ClosePreviewScene(preview);
            foreach (UnityEngine.Object resource in generated)
                if (resource != null) UnityEngine.Object.DestroyImmediate(resource);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }

    private static HashSet<UnityEngine.Object> CollectGeneratedResources(GameObject host)
    {
        HashSet<UnityEngine.Object> result = new HashSet<UnityEngine.Object>();
        if (host == null) return result;
        foreach (MonoBehaviour component in host.GetComponents<MonoBehaviour>())
            foreach (FieldInfo field in component.GetType().GetFields(Private))
            {
                object value = field.GetValue(component);
                if (value is Sprite sprite) CollectSprite(result, sprite);
                else if (value is Material material && !AssetDatabase.Contains(material)) result.Add(material);
                else if (value is Dictionary<Sprite, Sprite> cache)
                    foreach (Sprite cached in cache.Values) CollectSprite(result, cached);
            }
        return result;
    }

    private static void CollectSprite(HashSet<UnityEngine.Object> result, Sprite sprite)
    {
        if (sprite == null || AssetDatabase.Contains(sprite)) return;
        result.Add(sprite);
        if (sprite.texture != null && !AssetDatabase.Contains(sprite.texture)) result.Add(sprite.texture);
    }

    private static List<BoardTileData> ReferenceBoard()
    {
        string[] rows = { "BRTVKLV", "VFLPBBB", "BBBBSTV", "BLVORVV", "VVBSVRB", "VLFVBVT", "BBKFVRK" };
        // B normal, L elite, V event, R rest, T treasure, K key, F forge, S merchant, O boss, P pawn.
        List<BoardTileData> tiles = new List<BoardTileData>();
        for (int row = 0; row < 7; row++)
            for (int column = 0; column < 7; column++)
            {
                char code = rows[row][column];
                MapTileType type = code == 'B' ? MapTileType.Battle : code == 'L' ? MapTileType.Elite : code == 'V' ? MapTileType.Event :
                    code == 'R' ? MapTileType.Rest : code == 'T' ? MapTileType.Treasure : code == 'K' ? MapTileType.Key : code == 'F' ? MapTileType.Forge :
                    code == 'S' ? MapTileType.Merchant : code == 'O' ? MapTileType.Boss : MapTileType.Empty;
                tiles.Add(new BoardTileData { coord = new Vector2Int(column, 6 - row), tileType = type, isVisited = code == 'P',
                    isResolved = code == 'P', isSpawnTile = code == 'P', dangerBand = MapDangerBand.Outer });
            }
        return tiles;
    }
}
