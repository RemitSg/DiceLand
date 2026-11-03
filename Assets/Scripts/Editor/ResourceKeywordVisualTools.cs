using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Renders the production resource labels with disposable UI samples, without rewards or saves.</summary>
public static class ResourceKeywordVisualTools
{
    public const string OutputPath = "outputs/agent-battle/resource-keywords-20261102";
    public const string AttributeHealthOutputPath = "outputs/agent-battle/attribute-health-keywords-20261103";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Description = "获得 25 金币，恢复 3 行动点，并获得 1 把钥匙。\n金钱可用于购买补给；行动点用于探索；钥匙用于挑战首领。";
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));

    [MenuItem("Tools/Map Visuals/Capture Resource Keywords")]
    public static void BatchPreview()
    {
        Directory.CreateDirectory(OutputPath);
        CaptureEvent("event-keywords.png", 2340, 1080, false);
        CaptureEvent("event-tablet-keywords.png", 1600, 1200, false);
        CaptureEvent("event-result-keywords.png", 2340, 1080, true);
        CaptureMerchant();
        CaptureBattleLoot();
        CaptureLegacyTutorial();
        File.WriteAllText(Path.Combine(OutputPath, "preview-complete.json"), "{\"success\":true,\"images\":6,\"syntheticPresentationSamples\":true,\"playerSavesUsed\":false,\"rewardsExecuted\":false}");
    }

    [MenuItem("Tools/Map Visuals/Capture Attribute And Health Keywords")]
    public static void CaptureAttributeAndHealthKeywords()
    {
        Directory.CreateDirectory(AttributeHealthOutputPath);
        Capture("keyword-overview.png", 2340, 1080, (host, camera) =>
        {
            GameObject root = new GameObject("KeywordOverview", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(host.transform, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 8100;
            PrepareCanvas(canvas, host, camera, 2340, 1080);
            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(root.transform, false);
            RectTransform panel = (RectTransform)panelObject.transform;
            panel.anchoredPosition = new Vector2(130f, 0f);
            panel.sizeDelta = new Vector2(1460f, 900f);
            panelObject.GetComponent<Image>().color = new Color(0.025f, 0.065f, 0.045f, 0.98f);
            OverviewLabel(panel, "Heading", "属性与生命关键词", 48, -36f, 1330f, 68f);
            string[] attributes = { "炎", "水", "毒", "血", "暗", "电", "光", "速", "无" };
            for (int i = 0; i < attributes.Length; i++)
                OverviewLabel(panel, "Attribute" + i, attributes[i] + "属性", 42,
                    -148f - i / 3 * 94f, 380f, 70f, 62f + i % 3 * 456f);
            OverviewLabel(panel, "Health", "恢复 10% 最大生命值，当前血量 20/20。", 36, -468f, 1330f, 108f);
            OverviewLabel(panel, "Aliases", "生命值 / 血量 / HP / 回血", 32, -588f, 1330f, 64f);
            OverviewLabel(panel, "Resources", "获得 25 金币，恢复 3 行动点，获得 1 把钥匙和 13EXP。", 30, -688f, 1330f, 72f);
            OverviewLabel(panel, "OrdinaryWords", "普通词保持原样：无法、速度、流血、中毒、水壶、灯光。", 27, -790f, 1330f, 66f);
        }, AttributeHealthOutputPath);
        CaptureHealthEvent("keyword-event-phone.png", 2340, 1080);
        CaptureHealthEvent("keyword-event-tablet.png", 1600, 1200);
        Capture("keyword-battle-preview.png", 2340, 1080, (host, camera) =>
        {
            MapBattleTilePreviewOverlay overlay = host.AddComponent<MapBattleTilePreviewOverlay>();
            TMP_FontAsset attributeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("1927579a4b7620b428469b73f8d5046f"));
            overlay.ConfigureFonts(Font, Font, attributeFont);
            overlay.Show(new MapBattleTilePreviewContext
            {
                tile = new BoardTileData { tileType = MapTileType.Elite }, canPreview = true, canConfirm = true,
                dangerRating = 2, attributeNames = new[] { "炎", "水", "血" }, confirmLabel = "进入战斗"
            });
            PrepareCanvas((Canvas)Get(overlay, "canvas"), host, camera, 2340, 1080);
            overlay.ApplyResponsiveLayout(new Vector2(2340f, 1080f));
        }, AttributeHealthOutputPath);
        File.WriteAllText(Path.Combine(AttributeHealthOutputPath, "preview-complete.json"),
            "{\"success\":true,\"images\":4,\"syntheticPresentationSamples\":true,\"playerSavesUsed\":false,\"rewardsExecuted\":false}");
    }

    private static void OverviewLabel(RectTransform panel, string name, string value, int size, float y, float width, float height, float x = 62f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = Font;
        text.fontSize = size;
        text.color = new Color(0.94f, 0.91f, 0.79f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0f, 1f);
        text.rectTransform.anchoredPosition = new Vector2(x, y);
        text.rectTransform.sizeDelta = new Vector2(width, height);
        text.text = value;
        ResourceKeywordText.Attach(text);
    }

    private static void CaptureHealthEvent(string name, int width, int height)
    {
        MapEvent_SO sample = ScriptableObject.CreateInstance<MapEvent_SO>();
        try
        {
            sample.eventName = "林间的补给";
            sample.eventContent = "你在林间发现了一只旧木箱。\n\n石面刻着九种属性：炎、水、毒、血、暗、电、光、速、无。\n\n补给可以恢复生命值。生命上限与当前血量会分别显示；血属性使用血滴，血量使用心形。";
            sample.options = new List<MapEventOptionData>
            {
                new MapEventOptionData { optionLabel = "治疗伤势", requiredMoney = 10, consequenceDescription = "恢复 10% 最大生命值。", resultContent = "恢复生命值，当前血量有所增加。" },
                new MapEventOptionData { optionLabel = "检查符文", consequenceDescription = "查看炎属性与水属性。" },
                new MapEventOptionData { optionLabel = "继续前进", consequenceDescription = "不消耗金币，返回地图。" }
            };
            Capture(name, width, height, (host, camera) =>
            {
                MapEventOverlay overlay = host.AddComponent<MapEventOverlay>();
                MapEventVisualTools.CopyProductionSettings(overlay);
                overlay.Show(new BoardTileData { tileType = MapTileType.Event, assignedEvent = sample }, sample, PreviewChoice);
                PrepareCanvas((Canvas)Get(overlay, "canvas"), host, camera, width, height);
                overlay.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
                Call(overlay, "RefreshLayout");
            }, AttributeHealthOutputPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(sample); }
    }

    private static void CaptureEvent(string name, int width, int height, bool result)
    {
        MapEvent_SO sample = ScriptableObject.CreateInstance<MapEvent_SO>();
        try
        {
            sample.eventName = "林间的补给";
            sample.eventContent = "你在林间发现了一只旧木箱。\n\n" + Description;
            sample.options = new List<MapEventOptionData>
            {
                new MapEventOptionData { optionLabel = "交换补给", requiredMoney = 10, consequenceDescription = "恢复 3 行动点，获得 1 把钥匙。", resultContent = Description },
                new MapEventOptionData { optionLabel = "购买全部物资", requiredMoney = 80, consequenceDescription = "获得 2 把钥匙。" },
                new MapEventOptionData { optionLabel = "收下赠礼", consequenceDescription = "获得 25 金币。" }
            };
            Capture(name, width, height, (host, camera) =>
            {
                MapEventOverlay overlay = host.AddComponent<MapEventOverlay>();
                overlay.ConfigureFonts(Font, Font);
                BoardTileData eventTile = new BoardTileData { tileType = MapTileType.Event, assignedEvent = sample };
                overlay.Show(eventTile, sample, PreviewChoice, (tile, data, option) => option == sample.options[1]
                    ? new MapEventOverlay.EventOptionPreviewData { isSelectable = false, unavailableReason = "金钱不足，需要 80 金钱。" } : null);
                Canvas canvas = (Canvas)Get(overlay, "canvas");
                PrepareCanvas(canvas, host, camera, width, height);
                overlay.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
                if (result) canvas.transform.Find("SafeArea/Panel/ConfirmButton").GetComponent<Button>().onClick.Invoke();
                Call(overlay, "RefreshLayout");
            });
        }
        finally { UnityEngine.Object.DestroyImmediate(sample); }
    }

    private static bool PreviewChoice(BoardTileData tile, MapEvent_SO data, MapEventOptionData option, out string message, out string result, out MapEvent_SO next)
    {
        message = option.consequenceDescription;
        result = option.resultContent;
        next = null;
        return true;
    }

    private static void CaptureMerchant()
    {
        LootItem_SO item = SampleLoot();
        try
        {
            Capture("merchant-keywords.png", 2340, 1080, (host, camera) =>
            {
                MapMerchantOverlay overlay = host.AddComponent<MapMerchantOverlay>();
                overlay.ConfigureFonts(Font, Font);
                BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant, merchantStockGenerated = true, merchantPurchaseDiscountStacks = 1 };
                tile.merchantOffers.Add(new MerchantOfferState { itemType = MerchantItemType.Loot, lootItem = item, price = 25 });
                overlay.Show(new RunState { money = 50 }, tile, null, _ => string.Empty, _ => 5);
                PrepareCanvas((Canvas)Get(overlay, "canvas"), host, camera, 2340, 1080);
            });
        }
        finally { UnityEngine.Object.DestroyImmediate(item); }
    }

    private static void CaptureBattleLoot()
    {
        LootItem_SO item = SampleLoot();
        try
        {
            Capture("battle-loot-keywords.png", 2340, 1080, (host, camera) =>
            {
                BattleSettlementOverlay overlay = host.AddComponent<BattleSettlementOverlay>();
                Set(overlay, "bodyFont", Font);
                Set(overlay, "titleFont", Font);
                Set(overlay, "buttonFont", Font);
                Call(overlay, "EnsureCanvas");
                Canvas canvas = (Canvas)Get(overlay, "overlayCanvas");
                PrepareCanvas(canvas, host, camera, 2340, 1080);
                ((TMP_Text)Get(overlay, "titleText")).text = "战斗胜利";
                ((TMP_Text)Get(overlay, "subtitleText")).text = "获得 25 金币";
                BattleSettlementLootEntry entry = new BattleSettlementLootEntry { lootItem = item, displayName = item.lootName, amount = 1, icon = item.icon, description = item.description };
                Call(overlay, "BuildLootSection", new BattleSettlementData { isVictory = true, lootEntries = new List<BattleSettlementLootEntry> { entry } });
                Call(overlay, "SetVisible", true);
                Call(overlay, "ShowLootDetailModal", entry, null);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Get(overlay, "panelRoot"));
            });
        }
        finally { UnityEngine.Object.DestroyImmediate(item); }
    }

    private static void CaptureLegacyTutorial()
    {
        Capture("tutorial-keywords.png", 2340, 1080, (host, camera) =>
        {
            MainMenuTutorialOverlay overlay = host.AddComponent<MainMenuTutorialOverlay>();
            overlay.ConfigureFonts(AssetDatabase.LoadAssetAtPath<Font>("Assets/Assets/Fonts/AlibabaPuHuiTi-3-115-Black.ttf"));
            overlay.Show(null, null, null, titleOverride: "资源与探索", bodyOverride: Description, hideSections: true);
            // The old UI uses a built-in font for its headings and buttons; use the real
            // Chinese face for this preview as the production menu does during setup.
            foreach (Text label in overlay.PresentationCanvas.GetComponentsInChildren<Text>(true)) label.font = overlay.DebugSummaryBodyText.font;
            PrepareCanvas(overlay.PresentationCanvas, host, camera, 2340, 1080);
        });
    }

    private static LootItem_SO SampleLoot()
    {
        LootItem_SO item = ScriptableObject.CreateInstance<LootItem_SO>();
        item.lootName = "旅途补给";
        item.description = Description;
        item.icon = MapForestArt.Get("Treasure");
        return item;
    }

    private static void Capture(string name, int width, int height, Action<GameObject, Camera> build, string outputPath = OutputPath)
    {
        string source = MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, customize: (host, camera) =>
        {
            build(host, camera);
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in host.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in host.GetComponentsInChildren<Graphic>()) graphic.Rebuild(CanvasUpdate.PreRender);
            Canvas.ForceUpdateCanvases();
        });
        Directory.CreateDirectory(outputPath);
        File.Copy(source, Path.Combine(outputPath, name), true);
    }

    private static void PrepareCanvas(Canvas canvas, GameObject host, Camera camera, int width, int height)
    {
        if (canvas.transform.parent == null) SceneManager.MoveGameObjectToScene(canvas.gameObject, host.scene);
        canvas.transform.SetParent(host.transform, false);
        canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        RectTransform rect = (RectTransform)canvas.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080f * width / height, 1080f);
        rect.position = new Vector3(0, 0, -3);
        rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
    }

    private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static void Call(object target, string method, params object[] arguments) => target.GetType().GetMethod(method, Private).Invoke(target, arguments);
}
