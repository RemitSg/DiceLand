using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Production crafting UI over a disposable map, using real recipes and an isolated run state.</summary>
public static class MapCraftingVisualTools
{
    public const string OutputPath = "outputs/agent-battle/crafting-panel-20261104";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Map Visuals/Capture Crafting Panel")]
    public static void CaptureDefault()
    {
        Capture("single-insufficient.png", 2340, 1080);
        Capture("multiple-ready.png", 2340, 1080, true, true);
        Capture("multiple-16x9.png", 1920, 1080, true);
    }

    public static string Capture(string name, int width, int height, bool multiple = false, bool ready = false, Rect? safe = null, int blueprintID = 202)
    {
        string source = MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, safe, (host, camera) =>
        {
            MapHUD hud = host.GetComponent<MapHUD>();
            RunState state = (RunState)typeof(MapHUD).GetField("currentRunState", Private).GetValue(hud);
            state.money = 5;
            CraftingBlueprint_SO[] all = AssetDatabase.FindAssets("t:CraftingBlueprint_SO", new[] { "Assets/Data" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<CraftingBlueprint_SO>).Where(b => b != null).ToArray();
            CraftingBlueprint_SO chosen = all.First(b => b.blueprintID == blueprintID);
            state.AddBlueprint(chosen);
            if (multiple)
                foreach (CraftingBlueprint_SO recipe in all.Where(b => b != chosen).Take(14)) state.AddBlueprint(recipe);
            if (ready)
                foreach (CraftingMaterialRequirement requirement in RunCraftingService.GetRequirements(chosen)) state.AddLoot(requirement.lootItem, requirement.amount);
            hud.Refresh(state, host.GetComponent<ActionPointSystem>());
            GameObject forgeHost = new GameObject("CraftingPanelPreview"); forgeHost.transform.SetParent(host.transform, false);
            MapCraftingOverlay overlay = forgeHost.AddComponent<MapCraftingOverlay>();
            TMP_FontAsset keyword = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("1927579a4b7620b428469b73f8d5046f"));
            TMP_FontAsset body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
            overlay.ConfigureFonts(keyword, body);
            overlay.Show(state, recipe => { RunCraftingService.TryCraft(state, recipe, out string message); return message; });
            Canvas canvas = forgeHost.GetComponentInChildren<Canvas>();
            canvas.gameObject.SetActive(false);
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            RectTransform rect = (RectTransform)canvas.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = new Vector2(1080f * width / height, 1080);
            rect.position = new Vector3(0, 0, -3); rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
            canvas.gameObject.SetActive(true);
            overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
            typeof(MapCraftingOverlay).GetMethod("RefreshLayout", Private).Invoke(overlay, null);
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
            {
                if (graphic is TMP_Text || graphic is TMP_SubMeshUI) continue;
                graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
        });
        return PersistCapture(source, name);
    }

    public static string CaptureInventory(string name, int width, int height, bool blueprints, bool detail = false, int blueprintID = 103)
    {
        string source = MapVisualTools.Capture(name, width, height, MapBackgroundPhase.Morning, false, 1, null, (host, camera) =>
        {
            MapHUD hud = host.GetComponent<MapHUD>();
            RunState state = (RunState)typeof(MapHUD).GetField("currentRunState", Private).GetValue(hud);
            state.lootInventory.Clear(); state.weaponInventory.Clear(); state.blueprintInventory.Clear();
            CraftingBlueprint_SO[] all = AssetDatabase.FindAssets("t:CraftingBlueprint_SO", new[] { "Assets/Data/Scroll" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CraftingBlueprint_SO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(b => b != null).OrderBy(b => b.blueprintID == blueprintID ? 0 : 1).ThenBy(b => b.blueprintID).ToArray();
            foreach (CraftingBlueprint_SO recipe in all)
            {
                state.AddBlueprint(recipe); state.AddWeapon(recipe.targetWeapon);
            }
            hud.Refresh(state, host.GetComponent<ActionPointSystem>()); hud.ShowInventoryPanel();
            Button tab = (Button)typeof(MapHUD).GetField(blueprints ? "blueprintTabButton" : "weaponTabButton", Private).GetValue(hud);
            tab.onClick.Invoke();
            if (detail)
                host.GetComponentsInChildren<Image>().First(i => i.name == "Icon" && i.transform.parent.name == "Slot_0")
                    .transform.parent.GetComponent<Button>().onClick.Invoke();
        });
        return PersistCapture(source, name);
    }

    private static string PersistCapture(string source, string name)
    {
        Directory.CreateDirectory(OutputPath);
        string destination = Path.GetFullPath(Path.Combine(OutputPath, name));
        File.Copy(source, destination, true);
        return destination;
    }
}
