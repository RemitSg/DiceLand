using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real items and both production entry points, in disposable scenes without player saves.</summary>
public static class MapItemDetailVisualTools
{
    public const string OutputPath = "outputs/agent-battle/item-detail-20261104";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Map Visuals/Capture Item Details")]
    public static void CaptureDefault()
    {
        CaptureBattle("battle-crystal-16x9.png", 1920, 1080);
        CaptureMap("inventory-weapon.png", 2340, 1080, 1);
        CaptureMap("inventory-blueprint.png", 2340, 1080, 2);
        CaptureMap("inventory-relic.png", 1600, 1200, 3);
    }

    public static string CaptureBattle(string name, int width, int height, Rect? safe = null)
    {
        string source = BattleSettlementVisualTools.Capture(name, width, height, 4, safe, 1, 1, true, overlay =>
        {
            LootItem_SO crystal = AssetDatabase.LoadAssetAtPath<LootItem_SO>("Assets/Data/Loots/LootItems/004 磁电晶/MagtronicOrb.asset");
            BattleSettlementData data = BattleSettlementVisualTools.SampleData(1, 0);
            data.lootEntries.Add(new BattleSettlementLootEntry { lootItem = crystal, icon = crystal.icon,
                displayName = crystal.lootName, amount = 1, rarity = crystal.rarity, description = crystal.description, attribute = crystal.attribute });
            overlay.ShowResult(data); overlay.AdvancePresentation(4);
            typeof(BattleSettlementOverlay).GetMethod("ShowLootDetailModal", Private).Invoke(overlay, new object[] { data.lootEntries[0], null });
            overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
            MapInventoryDetailView view = (MapInventoryDetailView)typeof(BattleSettlementOverlay).GetField("lootDetailView", Private).GetValue(overlay);
            view.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
            Canvas.ForceUpdateCanvases();
        });
        return Persist(source, name);
    }

    // kind: 0 material, 1 weapon, 2 blueprint, 3 relic.
    public static string CaptureMap(string name, int width, int height, int kind = 0, Rect? safe = null, bool scrollToEnd = false)
    {
        string source = MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, safe, (host, camera) =>
        {
            MapHUD hud = host.GetComponent<MapHUD>();
            RunState state = (RunState)typeof(MapHUD).GetField("currentRunState", Private).GetValue(hud);
            string tab = "lootTabButton";
            if (kind == 0) state.AddLoot(AssetDatabase.LoadAssetAtPath<LootItem_SO>("Assets/Data/Loots/LootItems/004 磁电晶/MagtronicOrb.asset"), 4);
            else if (kind == 1 || kind == 2)
            {
                CraftingBlueprint_SO blueprint = All<CraftingBlueprint_SO>().First(b => b.blueprintID == 202);
                if (kind == 1) { state.AddWeapon(blueprint.targetWeapon); tab = "weaponTabButton"; }
                else { state.AddBlueprint(blueprint); tab = "blueprintTabButton"; }
            }
            else
            {
                Relic_SO relic = All<Relic_SO>().OrderByDescending(r => (r.description ?? "").Length + (r.additionalDescription ?? "").Length).First();
                state.AddRelic(relic, 1, false); tab = "relicTabButton";
            }
            hud.Refresh(state, host.GetComponent<ActionPointSystem>()); hud.ShowInventoryPanel();
            ((Button)typeof(MapHUD).GetField(tab, Private).GetValue(hud)).onClick.Invoke();
            Button slot = host.GetComponentsInChildren<Button>().First(b => b.name == "Slot_0");
            slot.onClick.Invoke();
            if (scrollToEnd)
            {
                var detail = (MapInventoryDetailView)typeof(MapHUD).GetField("runtimeInventoryDetailView", Private).GetValue(hud);
                var scroll = (ScrollRect)typeof(MapInventoryDetailView).GetField("descriptionScroll", Private).GetValue(detail);
                Canvas.ForceUpdateCanvases(); scroll.StopMovement(); scroll.verticalNormalizedPosition = 0;
            }
        });
        return Persist(source, name);
    }

    private static T[] All<T>() where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Data" })
        .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(v => v != null).ToArray();
    private static string Persist(string source, string name)
    {
        Directory.CreateDirectory(OutputPath);
        string path = Path.GetFullPath(Path.Combine(OutputPath, name)); File.Copy(source, path, true); return path;
    }
}
