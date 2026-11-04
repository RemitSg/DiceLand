using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Imports the treasure presentation and captures the real overlay over a disposable map.</summary>
public static class MapTreasureVisualTools
{
    public const string OutputPath = "outputs/agent-battle/treasure-panel-20261104";
    private const string ArtPath = "Assets/Resources/TreasureUI/";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int referenceSeed;

    [MenuItem("Tools/Map Visuals/Configure Treasure Art")]
    public static void ConfigureAssets()
    {
        foreach (string file in new[] { "Card.png", "Header.png", "Rewards.png", "Panel.png", "Button.png" })
        {
            string path = ArtPath + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = file == "Header.png" || file == "Panel.png" ? SpriteImportMode.Single : SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.isReadable = false;
            importer.sRGBTexture = true; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (file == "Rewards.png") ConfigureRewardAtlas(importer, path);
            if (file == "Card.png" || file == "Button.png") ConfigureCutout(importer, path, file == "Card.png" ? "Card" : "Button");
            if (file == "Panel.png") importer.spriteBorder = Vector4.one * 110;
            importer.SaveAndReimport();
        }
        MapTreasureArt theme = AssetDatabase.LoadAssetAtPath<MapTreasureArt>(ArtPath + "Theme.asset");
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<MapTreasureArt>();
            AssetDatabase.CreateAsset(theme, ArtPath + "Theme.asset");
        }
        theme.card = AssetDatabase.LoadAllAssetsAtPath(ArtPath + "Card.png").OfType<Sprite>().Single();
        theme.header = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Header.png");
        theme.panelFrame = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Panel.png");
        theme.button = AssetDatabase.LoadAllAssetsAtPath(ArtPath + "Button.png").OfType<Sprite>().Single();
        Dictionary<string, Sprite> icons = AssetDatabase.LoadAllAssetsAtPath(ArtPath + "Rewards.png").OfType<Sprite>().ToDictionary(s => s.name);
        theme.coins = icons["Coins"];
        theme.relicBags = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary" }.Select(n => icons[n]).ToArray();
        theme.headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Assets/Fonts/Hi字楷体简 SDF.asset");
        theme.bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Assets/Fonts/AlibabaPuHuiTi-3-95-ExtraBold SDF.asset");
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssetIfDirty(theme);
    }

    private static void ConfigureCutout(TextureImporter importer, string path, string name)
    {
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories(); factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        SpriteRect prior = provider.GetSpriteRects().FirstOrDefault();
        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            source.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = source.GetPixels32();
            int minX = source.width, maxX = 0, minY = source.height, maxY = 0;
            for (int y = 0; y < source.height; y++)
                for (int x = 0; x < source.width; x++)
                    if (pixels[y * source.width + x].a >= 48)
                    { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
            SpriteRect rect = new SpriteRect { name = name, rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                alignment = SpriteAlignment.Center, pivot = Vector2.one * 0.5f, spriteID = prior != null ? prior.spriteID : GUID.Generate() };
            provider.SetSpriteRects(new[] { rect });
            ISpriteNameFileIdDataProvider ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (ids != null) ids.SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(name, rect.spriteID) });
            provider.Apply();
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
    }

    private static void ConfigureRewardAtlas(TextureImporter importer, string path)
    {
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories(); factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        Dictionary<string, SpriteRect> previous = provider.GetSpriteRects().ToDictionary(r => r.name);
        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            source.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = source.GetPixels32();
            string[] names = { "Coins", "Common", "Uncommon", "Rare", "Epic", "Legendary" };
            SpriteRect[] entries = new SpriteRect[6];
            for (int i = 0; i < names.Length; i++)
            {
                int left = i % 3 * source.width / 3, right = (i % 3 + 1) * source.width / 3;
                int bottom = (1 - i / 3) * source.height / 2, top = (2 - i / 3) * source.height / 2;
                int minX = right, minY = top, maxX = left, maxY = bottom;
                for (int y = bottom; y < top; y++)
                    for (int x = left; x < right; x++)
                        if (pixels[y * source.width + x].a >= 48)
                        { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
                if (maxX <= minX || maxY <= minY) throw new InvalidDataException("Missing reward icon: " + names[i]);
                minX = Mathf.Max(left, minX - 3); minY = Mathf.Max(bottom, minY - 3);
                maxX = Mathf.Min(right, maxX + 4); maxY = Mathf.Min(top, maxY + 4);
                entries[i] = new SpriteRect { name = names[i], rect = new Rect(minX, minY, maxX - minX, maxY - minY),
                    alignment = SpriteAlignment.Center, pivot = Vector2.one * 0.5f,
                    spriteID = previous.TryGetValue(names[i], out SpriteRect existing) ? existing.spriteID : GUID.Generate() };
            }
            provider.SetSpriteRects(entries);
            ISpriteNameFileIdDataProvider ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (ids != null)
            {
                Dictionary<string, SpriteNameFileIdPair> previousIds = ids.GetNameFileIdPairs().ToDictionary(p => p.name);
                ids.SetNameFileIdPairs(entries.Select(e => previousIds.TryGetValue(e.name, out SpriteNameFileIdPair pair) ? pair : new SpriteNameFileIdPair(e.name, e.spriteID)));
            }
            provider.Apply();
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
    }

    [MenuItem("Tools/Map Visuals/Capture Treasure Panel")]
    public static void CaptureDefault()
    {
        Capture("offers-16x9.png", 1920, 1080);
        Capture("offers-wide.png", 2340, 1080);
        Capture("relic-selection.png", 1920, 1080, "bag");
    }

    public static string Capture(string name, int width, int height, string stage = "offers", Rect? safe = null)
    {
        string source = MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, safe, (host, camera) =>
        {
            MapHUD hud = host.GetComponent<MapHUD>();
            RunState state = (RunState)typeof(MapHUD).GetField("currentRunState", Private).GetValue(hud);
            state.money = 20;
            TreasureRewardTable_SO table = Resources.Load<TreasureRewardTable_SO>("DefaultTreasureRewardTable");
            BoardTileData tile = new BoardTileData { coord = new Vector2Int(1, 1), tileType = MapTileType.Treasure, encounterSeed = 12345 };
            IReadOnlyList<TreasureRewardChoice> choices = null;
            if (referenceSeed <= 0)
            {
                System.Random samples = new System.Random(20261104);
                int bestScore = -1;
                for (int i = 0; i < 100000; i++)
                {
                    int seed = samples.Next(1, int.MaxValue);
                    var candidate = TreasureRewardService.BuildChoices(table, seed);
                    int score = (candidate[0].moneyAmount == 45 ? 4 : 0) +
                        (candidate[1].blueprint != null && candidate[1].blueprint.DisplayName.Contains("霜铁") ? 2 : 0) +
                        (candidate[2].relicBagRarity == LootRarity.Epic ? 1 : 0);
                    if (score > bestScore) { bestScore = score; referenceSeed = seed; }
                    if (score == 7) break;
                }
            }
            tile.encounterSeed = referenceSeed;
            choices = TreasureRewardService.BuildTileChoices(state, table, tile);
            hud.Refresh(state, host.GetComponent<ActionPointSystem>());
            GameObject go = new GameObject("TreasurePanelPreview"); go.transform.SetParent(host.transform, false);
            MapTreasureOverlay overlay = go.AddComponent<MapTreasureOverlay>();
            overlay.ConfigureForestPresentation();
            overlay.Show(tile, choices,
                (BoardTileData t, TreasureRewardChoice c, out TreasureRewardResult result) =>
                { result = TreasureRewardService.ApplyTileChoice(state, table, t, c); hud.Refresh(state, host.GetComponent<ActionPointSystem>()); return result.success; },
                (BoardTileData t, BattleSettlementRelicChoiceData c, out string message) =>
                { TreasureRewardResult result = TreasureRewardService.ApplyTileRelicChoice(state, t, c); message = result.message; return result.success; });
            if (stage == "result") Find(go.transform, "TreasureChoice_0").Find("ChooseButton").GetComponent<Button>().onClick.Invoke();
            if (stage == "bag" || stage == "selected")
            {
                Find(go.transform, "TreasureChoice_2").Find("ChooseButton").GetComponent<Button>().onClick.Invoke();
                if (stage == "selected") Find(go.transform, "TreasureRelicChoice_0").GetComponent<Button>().onClick.Invoke();
            }
            Canvas canvas = go.GetComponentInChildren<Canvas>();
            canvas.gameObject.SetActive(false); canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            RectTransform rect = (RectTransform)canvas.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = new Vector2(1080f * width / height, 1080);
            rect.position = new Vector3(0, 0, -3); rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
            canvas.gameObject.SetActive(true);
            overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
            {
                if (graphic is TMP_Text || graphic is TMP_SubMeshUI) continue;
                graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
        });
        Directory.CreateDirectory(OutputPath);
        string destination = Path.Combine(OutputPath, name);
        File.Copy(source, destination, true);
        return Path.GetFullPath(destination);
    }

    private static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

    public static string CaptureElite(string name, int width = 1920, int height = 1080)
    {
        string source = BattleSettlementVisualTools.Capture(name, width, height, 4f, null, 2, 6, true, battle =>
        {
            BattleSettlementData data = BattleSettlementVisualTools.SampleData();
            RelicTable_SO table = Resources.Load<TreasureRewardTable_SO>("DefaultTreasureRewardTable").RelicTable;
            if (!BattleEliteRelicChoiceBuilderService.TryBuildFromTable(out BattleSettlementRelicChoiceData choice, table, new System.Random(31), null, 3))
                throw new InvalidDataException("Cannot build the production elite relic choice.");
            data.relicChoice = choice;
            battle.ShowResult(data); battle.AdvancePresentation(4);
            typeof(BattleSettlementOverlay).GetMethod("OpenRelicChoiceModal", Private).Invoke(battle, new object[] { choice });
            MapTreasureOverlay shared = battle.GetComponentInChildren<MapTreasureOverlay>(true);
            Canvas main = (Canvas)typeof(BattleSettlementOverlay).GetField("overlayCanvas", Private).GetValue(battle);
            Canvas canvas = shared.GetComponentInChildren<Canvas>(true);
            Camera camera = main.worldCamera;
            canvas.gameObject.SetActive(false); canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            RectTransform rect = (RectTransform)canvas.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = new Vector2(1080f * width / height, 1080);
            rect.position = new Vector3(0, 0, -4); rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
            canvas.gameObject.SetActive(true); shared.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
            {
                if (graphic is TMP_Text || graphic is TMP_SubMeshUI) continue;
                graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
        });
        Directory.CreateDirectory(OutputPath);
        string destination = Path.Combine(OutputPath, name); File.Copy(source, destination, true);
        return Path.GetFullPath(destination);
    }

    // A disposable fixture for native UGUI play-mode checks; it never reads or writes a player's run snapshot.
    public static PlayFixture CreatePlayFixture() => new PlayFixture();
    public static BattlePlayFixture CreateBattlePlayFixture() => new BattlePlayFixture();

    public sealed class BattlePlayFixture
    {
        public readonly GameObject host;
        public readonly BattleSettlementOverlay battle;
        public readonly BattleSettlementRelicChoiceData choice;
        public readonly BattleEncounterData encounter = new BattleEncounterData { tileType = MapTileType.Elite };
        public readonly Button bagButton;
        public BattlePlayFixture()
        {
            RelicTable_SO table = Resources.Load<TreasureRewardTable_SO>("DefaultTreasureRewardTable").RelicTable;
            if (!BattleEliteRelicChoiceBuilderService.TryBuildFromTable(out choice, table, new System.Random(31), null, 3))
                throw new InvalidDataException("Cannot build elite test rewards.");
            host = new GameObject("BattleRelicPlayFixture"); battle = host.AddComponent<BattleSettlementOverlay>();
            MapTreasureArt theme = MapTreasureArt.Load(); battle.ConfigureFonts(theme.headingFont, theme.bodyFont);
            BattleSettlementData data = new BattleSettlementData { isVictory = true, relicChoice = choice };
            battle.ShowResult(data); battle.AdvancePresentation(4);
            var views = (System.Collections.IList)typeof(BattleSettlementOverlay).GetField("lootViews", Private).GetValue(battle);
            bagButton = (Button)views[0].GetType().GetField("button").GetValue(views[0]);
        }
        public MapTreasureOverlay Shared => host.GetComponentInChildren<MapTreasureOverlay>(true);
        public bool ApplyReturn() => BattleRelicChoiceSelectionService.TryApply(encounter, choice, out _);
        public void Dispose() { UnityEngine.Object.Destroy(host); }
    }

    public sealed class PlayFixture
    {
        public readonly GameObject host;
        public readonly MapTreasureOverlay overlay;
        public readonly RunState state = new RunState();
        public BoardTileData tile;
        public int claimCalls, confirmCalls, hiddenCalls;
        public int expectedMoney;

        public PlayFixture()
        {
            host = new GameObject("TreasurePlayFixture");
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject events = new GameObject("TreasureTestEvents", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                events.transform.SetParent(host.transform, false);
            }
            overlay = host.AddComponent<MapTreasureOverlay>(); overlay.ConfigureForestPresentation();
            overlay.Hidden += () => hiddenCalls++;
            Show();
        }

        public void Show(int count = 3)
        {
            tile = new BoardTileData { tileType = MapTileType.Treasure, coord = Vector2Int.one, encounterSeed = 12345 };
            TreasureRewardTable_SO table = Resources.Load<TreasureRewardTable_SO>("DefaultTreasureRewardTable");
            List<TreasureRewardChoice> offers = TreasureRewardService.BuildTileChoices(state, table, tile).ToList();
            expectedMoney = offers[0].moneyAmount;
            while (offers.Count < count) offers.AddRange(TreasureRewardService.BuildChoices(table, offers.Count + 37));
            offers = offers.Take(count).ToList();
            overlay.Show(tile, offers, (BoardTileData t, TreasureRewardChoice choice, out TreasureRewardResult result) =>
            { claimCalls++; result = TreasureRewardService.ApplyTileChoice(state, table, t, choice); return result.success; },
            (BoardTileData t, BattleSettlementRelicChoiceData choice, out string message) =>
            { confirmCalls++; var result = TreasureRewardService.ApplyTileRelicChoice(state, t, choice); message = result.message; return result.success; });
        }

        public void Dispose() { UnityEngine.Object.Destroy(host); }
    }
}
