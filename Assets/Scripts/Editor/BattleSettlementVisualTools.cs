using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Renders the production settlement in a disposable scene, with real artwork and synthetic rewards. Never resolves an encounter or writes a save.</summary>
public static class BattleSettlementVisualTools
{
    public const string OutputPath = "outputs/agent-battle/settlement-ui-20261102";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Battle Visuals/Capture Settlement Panel")]
    public static void CaptureDefault() => Capture("settlement-phone.png", 2340, 1080, 4);

    public static void BatchPreview()
    {
        ValidateSceneWiring();
        CaptureDefault();
        Capture("settlement-counting.png", 2340, 1080, 0.35f);
        Capture("settlement-transfers.png", 2340, 1080, 1.25f);
        Capture("settlement-wallet.png", 2340, 1080, 1.82f);
        Capture("settlement-desktop.png", 1920, 1080, 4);
        Capture("settlement-small.png", 1280, 720, 4);
        Capture("settlement-safe-area.png", 2340, 1080, 4, new Rect(110, 24, 2120, 1032));
        Capture("settlement-many.png", 1920, 1080, 4, null, 7, 17);
        Capture("settlement-defeat.png", 1920, 1080, 4, null, 2, 0, false);
        Debug.Log("Battle settlement captures: " + Path.GetFullPath(OutputPath));
    }

    private static void ValidateSceneWiring()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.scene", OpenSceneMode.Additive);
        try
        {
            BattleSettlementOverlay overlay = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BattleSettlementOverlay>(true)).FirstOrDefault();
            if (overlay == null) throw new InvalidDataException("Production battle settlement component is missing.");
            TMP_FontAsset expected = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
            foreach (string field in new[] { "titleFont", "bodyFont", "buttonFont" })
                if (typeof(BattleSettlementOverlay).GetField(field, Private).GetValue(overlay) != expected)
                    throw new InvalidDataException("Production settlement font differs from map: " + field);
            overlay.ShowResult(SampleData());
            overlay.AdvancePresentation(4);
            if (overlay.IsPresenting) throw new InvalidDataException("Production settlement did not complete.");
            Canvas canvas = (Canvas)typeof(BattleSettlementOverlay).GetField("overlayCanvas", Private).GetValue(overlay);
            if (canvas.GetComponentsInChildren<TMP_Text>().First(t => t.name == "MoneyReward").text != "金币 +23")
                throw new InvalidDataException("Production settlement does not show encounter gold.");
            Directory.CreateDirectory(OutputPath);
            File.WriteAllText(Path.Combine(OutputPath, "scene-wiring.txt"), "SampleScene: real serialized settlement component; three map fonts; real portraits and loot; staged gold/XP completed; no save or scene write.\n");
            overlay.HideResult();
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }

    public static BattleSettlementData SampleData(int partyCount = 2, int lootCount = 6, bool victory = true)
    {
        BattleSettlementData data = new BattleSettlementData { isVictory = victory, subtitle = victory ? "战斗胜利，剩余可行动角色：" + partyCount : "队伍已在战斗中全灭。",
            experienceGained = victory ? 13 : 0, moneyGained = victory ? 23 : 0, startingMoney = 150, hasMoneyBalance = true };
        CharacterData_SO[] characters = AssetDatabase.FindAssets("t:CharacterData_SO", new[] { "Assets/Data/Characters" })
            .Select(g => AssetDatabase.LoadAssetAtPath<CharacterData_SO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null).OrderBy(c => c.characterName == "Chara" ? 0 : c.characterName == "Man" ? 1 : 2).ToArray();
        for (int i = 0; i < partyCount; i++)
        {
            CharacterData_SO character = characters.Length > 0 ? characters[i % characters.Length] : null;
            bool first = i == 0;
            data.partyEntries.Add(new BattleSettlementEntry { displayName = character != null ? character.characterName : "角色" + i,
                portrait = character != null ? character.icon ?? character.battleSprite : null, isAlive = victory,
                currentHP = victory ? first ? 1002 : 981 : 0, maxHP = first ? 1002 : 999,
                level = 1, currentExperience = first ? 0 : 1, maxExperience = first ? 10 : 20, experienceGained = victory ? 13 : 0,
                resultingLevel = first && victory ? 2 : 1, resultingExperience = victory ? first ? 3 : 14 : first ? 0 : 1,
                resultingMaxExperience = 20, resultingHP = victory ? first ? 1002 : 981 : 0, resultingMaxHP = first ? 1002 : 999,
                experienceRequirements = first && victory ? new List<int> { 10, 20 } : new List<int> { first ? 10 : 20 } });
        }
        string[] itemNames = { "影爪碎片", "铁矿", "浪涌核心", "焰翅", "水球", "磁电晶" };
        LootItem_SO[] items = AssetDatabase.FindAssets("t:LootItem_SO", new[] { "Assets/Data/Loots/LootItems" })
            .Select(g => AssetDatabase.LoadAssetAtPath<LootItem_SO>(AssetDatabase.GUIDToAssetPath(g))).Where(i => i != null).ToArray();
        for (int i = 0; i < lootCount; i++)
        {
            LootItem_SO item = items.FirstOrDefault(v => v.lootName == itemNames[i % itemNames.Length]);
            if (item == null) throw new InvalidDataException("Missing production loot: " + itemNames[i % itemNames.Length]);
            data.lootEntries.Add(new BattleSettlementLootEntry { lootItem = item, displayName = item.lootName,
                icon = item.icon, rarity = item.rarity, amount = 1, description = item.description, attribute = item.attribute });
        }
        return data;
    }

    public static string Capture(string name, int width, int height, float seconds, Rect? safe = null, int partyCount = 2, int lootCount = 6, bool victory = true, Action<BattleSettlementOverlay> customize = null)
    {
        Scene original = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewPreviewScene();
        RenderTexture previous = RenderTexture.active, target = null;
        Texture2D pixels = null;
        GameObject host = null;
        Camera camera = null;
        try
        {
            host = new GameObject("SettlementVisualPreview", typeof(EventSystem)); SceneManager.MoveGameObjectToScene(host, preview);
            GameObject cameraHost = new GameObject("SettlementPreviewCamera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraHost, preview);
            camera = cameraHost.GetComponent<Camera>(); camera.scene = preview; camera.orthographic = true; camera.orthographicSize = 5;
            camera.aspect = width / (float)height; camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.035f, 0.025f);
            GameObject light = new GameObject("SettlementPreviewLight", typeof(Light)); SceneManager.MoveGameObjectToScene(light, preview);
            light.GetComponent<Light>().type = LightType.Directional;
            GameObject background = new GameObject("ForestBackdrop", typeof(SpriteRenderer)); background.transform.SetParent(host.transform, false);
            SpriteRenderer forest = background.GetComponent<SpriteRenderer>(); forest.sprite = MapForestArt.Background(MapBackgroundPhase.Morning);
            if (forest.sprite != null)
            {
                float scale = Mathf.Max(camera.orthographicSize * 2 / forest.sprite.bounds.size.y, camera.orthographicSize * 2 * camera.aspect / forest.sprite.bounds.size.x);
                background.transform.localScale = Vector3.one * scale;
            }
            BattleSettlementOverlay overlay = host.AddComponent<BattleSettlementOverlay>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
            overlay.ConfigureFonts(font, font);
            overlay.ShowResult(SampleData(partyCount, lootCount, victory));
            Canvas main = (Canvas)typeof(BattleSettlementOverlay).GetField("overlayCanvas", Private).GetValue(overlay);
            SceneManager.MoveGameObjectToScene(main.gameObject, preview);
            ConvertCanvas(main, camera, width, height, -3);
            overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
            // Convert the coin canvas as soon as it is created, so normalized sources and the wallet
            // share the same capture camera throughout the exact production animation.
            for (float elapsed = 0; elapsed < seconds; elapsed += 1f / 60f)
            {
                overlay.AdvancePresentation(Mathf.Min(1f / 60f, seconds - elapsed));
                foreach (Canvas canvas in host.GetComponentsInChildren<Canvas>(true))
                    if (canvas.renderMode != RenderMode.WorldSpace) ConvertCanvas(canvas, camera, width, height, -4);
                Canvas.ForceUpdateCanvases();
            }
            customize?.Invoke(overlay);
            foreach (TMP_Text text in main.GetComponentsInChildren<TMP_Text>(true)) if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in main.GetComponentsInChildren<Graphic>())
            {
                if (graphic is TMP_Text || graphic is TMP_SubMeshUI) continue;
                graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            Directory.CreateDirectory(OutputPath);
            string path = Path.Combine(OutputPath, name); File.WriteAllBytes(path, pixels.EncodeToPNG());
            File.WriteAllLines(path + ".layout.txt", main.GetComponentsInChildren<TMP_Text>(true).Select(t => t.name + " | " + t.text + " | active=" + t.isActiveAndEnabled + " | truncated=" + t.isTextTruncated));
            return Path.GetFullPath(path);
        }
        finally
        {
            RenderTexture.active = previous;
            if (camera != null) camera.targetTexture = null;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            EditorSceneManager.ClosePreviewScene(preview);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ConvertCanvas(Canvas canvas, Camera camera, int width, int height, float depth)
    {
        bool active = canvas.gameObject.activeSelf; canvas.gameObject.SetActive(false);
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); if (scaler != null) scaler.enabled = false;
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
        RectTransform rect = (RectTransform)canvas.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080f * width / height, 1080); rect.position = new Vector3(0, 0, depth);
        rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
        canvas.gameObject.SetActive(active);
    }
}
