#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Renders production inventory controls in an automatically disposed preview scene.</summary>
public static class MapInventoryVisualTools
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Map Visuals/Capture Inventory")]
    public static void CaptureDefaults()
    {
        Capture("inventory-loot.png", 2340, 1080, "lootTabButton");
        Capture("inventory-weapons.png", 2340, 1080, "weaponTabButton");
        Capture("inventory-blueprints.png", 2340, 1080, "blueprintTabButton");
        Capture("inventory-relics.png", 2340, 1080, "relicTabButton");
    }

    public static string Capture(string name, int width, int height, string tab, bool detail = false, bool showEvent = false)
    {
        return MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, null, (host, camera) =>
        {
            MapHUD hud = host.GetComponent<MapHUD>();
            MapTreasureArt theme = MapTreasureArt.Load();
            if (theme != null) hud.ConfigureFonts(theme.headingFont, theme.bodyFont);
            RunState state = CreateState();
            hud.Refresh(state, null);
            if (showEvent)
            {
                MapEventOverlay overlay = host.AddComponent<MapEventOverlay>();
                MapTreasureArt art = MapTreasureArt.Load();
                overlay.ConfigureFonts(art.headingFont, art.bodyFont); overlay.ConfigureInventory(hud);
                MapEvent_SO data = Load<MapEvent_SO>().FirstOrDefault();
                overlay.Show(new BoardTileData { tileType = MapTileType.Event }, data, null);
                Canvas eventCanvas = overlay.GetComponentsInChildren<Canvas>(true).Single(c => c.name == "EventOverlayCanvas");
                Canvas source = host.GetComponentsInChildren<Canvas>(true).First(c => c.name == "MapHudCanvas");
                eventCanvas.GetComponent<CanvasScaler>().enabled = false;
                eventCanvas.renderMode = RenderMode.WorldSpace; eventCanvas.worldCamera = camera;
                RectTransform rect = (RectTransform)eventCanvas.transform;
                rect.sizeDelta = ((RectTransform)source.transform).sizeDelta;
                rect.position = source.transform.position + Vector3.back * 0.1f;
                rect.localScale = source.transform.localScale;
                overlay.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
            }
            hud.ShowInventoryPanel();
            ((Button)typeof(MapHUD).GetField(tab, Private).GetValue(hud)).onClick.Invoke();
            if (detail)
            {
                Button item = hud.GetComponentsInChildren<Button>().First(b => b.name == "Slot_0");
                item.onClick.Invoke();
                hud.GetComponentInChildren<MapInventoryDetailView>().ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
            }
        });
    }

    public static RunState CreateState()
    {
        RunState state = new RunState();
        int index = 0;
        foreach (LootItem_SO data in Load<LootItem_SO>().Where(d => d.icon != null).Take(14)) state.AddLoot(data, 2 + index++ % 11);
        foreach (Weapon_SO data in Load<Weapon_SO>().Where(d => d.icon != null).Take(18)) state.AddWeapon(data, 1);
        foreach (CraftingBlueprint_SO data in Load<CraftingBlueprint_SO>().Take(20)) state.AddBlueprint(data, 1);
        // Preview owned relics without running acquisition effects.
        foreach (Relic_SO data in Load<Relic_SO>().Where(d => d.icon != null).Take(22)) state.relicInventory.Add(new RunRelicState { relicData = data, amount = 1 });
        return state;
    }

    public static string CaptureWeaponDetails(string name, int width, int height,
        string weaponPath = "Assets/Data/Weapons/006 曦誓剑/006 曦誓剑.asset", Rect? safeArea = null)
    {
        return MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, safeArea, (host, camera) =>
        {
            Weapon_SO weapon = AssetDatabase.LoadAssetAtPath<Weapon_SO>(weaponPath);
            if (weapon == null) throw new ArgumentException("Weapon asset not found: " + weaponPath);
            MapHUD hud = host.GetComponent<MapHUD>();
            MapTreasureArt theme = MapTreasureArt.Load(); hud.ConfigureFonts(theme.headingFont, theme.bodyFont);
            RunState state = CreateState();
            state.weaponInventory.RemoveAll(entry => entry.weaponData == weapon);
            state.weaponInventory.Insert(0, new RunWeaponState { weaponData = weapon, amount = 1 });
            CharacterData_SO member = Load<CharacterData_SO>().Where(data => data.compatibleWeaponType == weapon.weaponType)
                .OrderBy(data => data.characterName == "Chara" ? 0 : 1).FirstOrDefault();
            if (member != null) state.partyMembers.Add(new RunPartyMemberState { memberId = "WeaponPreview", characterData = member,
                level = 1, currentHP = 20, maxHP = 20 });
            hud.Refresh(state, null); hud.ShowInventoryPanel();
            ((Button)typeof(MapHUD).GetField("weaponTabButton", Private).GetValue(hud)).onClick.Invoke();
            hud.GetComponentsInChildren<Button>().Single(button => button.name == "Slot_0").onClick.Invoke();
            hud.GetComponentInChildren<MapInventoryDetailView>().ApplyViewport(new Vector2(width, height),
                safeArea ?? new Rect(0, 0, width, height));
        });
    }

    private static T[] Load<T>() where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Data" })
        .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).Where(data => data != null).ToArray();
}
#endif
