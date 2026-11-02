using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Production merchant UI over the disposable map; synthetic stock and no purchases or saves.</summary>
public static class MapMerchantVisualTools
{
    public const string OutputPath = "outputs/agent-battle/merchant-ui-20261102";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Map Visuals/Capture Merchant Panel")]
    public static void CaptureDefault() => Capture("merchant-phone.png", 2340, 1080);

    public static void CaptureVariant()
    {
        string[] args = Environment.GetCommandLineArgs();
        int flag = Array.IndexOf(args, "-merchantCaptureScenario");
        string variant = flag >= 0 && flag + 1 < args.Length ? args[flag + 1] : "phone";
        string path;
        switch (variant)
        {
            case "phone": path = Capture("merchant-phone.png", 2340, 1080); break;
            case "wide": path = Capture("merchant-16x9.png", 1920, 1080); break;
            case "tablet": path = Capture("merchant-tablet.png", 1600, 1200); break;
            case "safe": path = Capture("merchant-safe-area.png", 2400, 1080, safe: new Rect(140, 34, 2210, 1012)); break;
            case "long": path = Capture("merchant-long.png", 2340, 1080, longContent: true); break;
            case "poor": path = Capture("merchant-unaffordable.png", 2340, 1080, poor: true); break;
            case "soldout": path = Capture("merchant-sold-out.png", 2340, 1080, soldOut: true); break;
            case "empty": path = Capture("merchant-empty.png", 2340, 1080, empty: true); break;
            default: throw new ArgumentException("Unknown merchant preview scenario: " + variant);
        }
        Debug.Log("Merchant preview complete: " + path);
    }
    public static string Capture(string name, int width, int height, bool longContent = false, bool poor = false, bool soldOut = false, bool empty = false, Rect? safe = null)
    {
        List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        try
        {
            List<MerchantOfferState> offers = SampleOffers();
            if (longContent && offers.Count > 0)
            {
                Relic_SO relic = UnityEngine.Object.Instantiate(offers[0].relicData);
                owned.Add(relic);
                relic.relicName = "长名称与完整物品说明的阅读检查样例";
                relic.description = string.Join("\n\n", Enumerable.Repeat("获得 25 金币，恢复 3 行动点，并获得 1 把钥匙。这段文字仅用于检查长说明的完整换行和独立滚动。", 16));
                offers[0].relicData = relic;
                offers.AddRange(offers.ToArray());
                offers.AddRange(offers.Take(3).ToArray());
            }
            if (soldOut && offers.Count > 0) offers[0].isSoldOut = true;
            if (empty) offers.Clear();
            string source = MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, safe, (host, camera) =>
            {
                // Update only the temporary preview's existing HUD state, never a live run.
                MapHUD hud = host.GetComponent<MapHUD>();
                TMP_Text money = (TMP_Text)typeof(MapHUD).GetField("moneyText", Private).GetValue(hud);
                money.text = poor ? "0" : "150";
                GameObject merchantHost = new GameObject("MerchantPanelPreview");
                merchantHost.transform.SetParent(host.transform, false);
                MapMerchantOverlay overlay = merchantHost.AddComponent<MapMerchantOverlay>();
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
                overlay.ConfigureFonts(font, font);
                typeof(MapMerchantOverlay).GetField("fallbackIconSprite", Private).SetValue(overlay, MapForestArt.Get("Treasure"));
                overlay.Show(new RunState { money = poor ? 0 : 150 }, new BoardTileData { tileType = MapTileType.Merchant, merchantStockGenerated = true, merchantOffers = offers },
                    (tile, offer) => string.Empty, tile => string.Empty, tile => 5);
                Canvas canvas = merchantHost.GetComponentInChildren<Canvas>();
                // Re-enable after the overlay-to-world-space conversion so RectMask2D caches
                // start in the capture camera's coordinate system, rather than the game view's.
                canvas.gameObject.SetActive(false);
                canvas.GetComponent<CanvasScaler>().enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                RectTransform rect = (RectTransform)canvas.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1080f * width / height, 1080);
                rect.position = new Vector3(0, 0, -3);
                rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
                canvas.gameObject.SetActive(true);
                overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
                typeof(MapMerchantOverlay).GetMethod("RefreshLayout", Private).Invoke(overlay, null);
                Canvas.ForceUpdateCanvases();
                foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
                foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
                {
                    // TMP owns the geometry and materials of its atlas submeshes.
                    if (graphic is TMP_Text || graphic is TMP_SubMeshUI) continue;
                    graphic.SetAllDirty();
                    graphic.Rebuild(CanvasUpdate.PreRender);
                }
                Canvas.ForceUpdateCanvases();
                Directory.CreateDirectory(OutputPath);
                File.WriteAllLines(Path.Combine(OutputPath, name + ".layout.txt"), canvas.GetComponentsInChildren<Graphic>(true).Select(graphic =>
                {
                    Mesh mesh = graphic.canvasRenderer.GetMesh();
                    TMP_Text text = graphic as TMP_Text;
                    return graphic.transform.parent.name + "/" + graphic.name + " | active=" + graphic.isActiveAndEnabled + " | cull=" + graphic.canvasRenderer.cull +
                        " | vertices=" + (mesh != null ? mesh.vertexCount : 0) + " | color=" + graphic.canvasRenderer.GetColor() +
                        " | position=" + graphic.rectTransform.anchoredPosition + " | size=" + graphic.rectTransform.rect.size +
                        (text != null ? " | text=" + text.text.Replace("\n", "\\n") : string.Empty);
                }));
            });
            Directory.CreateDirectory(OutputPath);
            string destination = Path.Combine(OutputPath, name);
            File.Copy(source, destination, true);
            return Path.GetFullPath(destination);
        }
        finally
        {
            foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            // Flush destroyed preview canvases before the next capture reuses UI meshes.
            Canvas.ForceUpdateCanvases();
        }
    }

    private static List<MerchantOfferState> SampleOffers()
    {
        List<MerchantOfferState> offers = new List<MerchantOfferState>();
        Relic_SO water = AssetDatabase.LoadAssetAtPath<Relic_SO>("Assets/Data/Relics/029 旅人的水壶/旅人的水壶.asset");
        if (water == null) throw new InvalidDataException("Production water flask asset not found.");
        offers.Add(new MerchantOfferState { itemType = MerchantItemType.Relic, relicData = water, price = 25 });
        LootItem_SO[] loot = Assets<LootItem_SO>();
        Weapon_SO[] weapons = Assets<Weapon_SO>();
        CraftingBlueprint_SO[] blueprints = Assets<CraftingBlueprint_SO>();
        Relic_SO[] relics = Assets<Relic_SO>();
        LootItem_SO uncommon = loot.FirstOrDefault(x => x.rarity == LootRarity.Uncommon) ?? loot.First();
        offers.Add(new MerchantOfferState { itemType = MerchantItemType.Loot, lootItem = uncommon, price = 10 });
        Weapon_SO rare = weapons.FirstOrDefault(x => x.rarity == Rarity.Rare) ?? weapons.First();
        offers.Add(new MerchantOfferState { itemType = MerchantItemType.Weapon, weaponData = rare, price = 30 });
        Relic_SO epic = relics.FirstOrDefault(x => x.rarity == LootRarity.Epic) ?? relics.First();
        offers.Add(new MerchantOfferState { itemType = MerchantItemType.Relic, relicData = epic, price = 40 });
        Relic_SO legendary = relics.FirstOrDefault(x => x.rarity == LootRarity.Legendary) ?? relics.First();
        offers.Add(new MerchantOfferState { itemType = MerchantItemType.Relic, relicData = legendary, price = 100 });
        offers.Add(new MerchantOfferState { itemType = MerchantItemType.Blueprint, blueprintData = blueprints.First(), price = 18, isSoldOut = true });
        return offers;
    }

    private static T[] Assets<T>() where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" })
        .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal).Select(AssetDatabase.LoadAssetAtPath<T>).Where(x => x != null).ToArray();
}
