using UnityEngine;
using UnityEngine.UI;

public partial class MapEventOverlay
{
    private MapHUD inventoryHud;
    private Button inventoryEntry;
    private bool openedInventory;
    private bool canLeaveReforgeEvent;
    private MapEventOptionData reforgeMaterialOption;
    private ScriptableObject reforgeMaterial;

    internal ScriptableObject SelectedReforgeItem => selectedOption == reforgeMaterialOption ? reforgeMaterial : null;

    public void ConfigureInventory(MapHUD hud)
    {
        inventoryHud = hud;
        if (inventoryEntry != null) inventoryEntry.gameObject.SetActive(hud != null);
    }

    private void BuildInventoryEntry()
    {
        inventoryEntry = MapInventoryEntry.Create(safeAreaRect, () =>
        {
            if (inventoryHud == null || !CanReceiveStoryInput) return;
            openedInventory = true;
            inventoryHud.ShowInventoryPanel(EventOverlayCanvasSortingOrder + 20);
        });
        inventoryEntry.gameObject.SetActive(inventoryHud != null);
    }

    private void HideOwnedInventory()
    {
        if (openedInventory && inventoryHud != null) inventoryHud.HideInventoryPanel();
        openedInventory = false;
    }

    private void ClearReforgeSelection()
    {
        canLeaveReforgeEvent = false;
        reforgeMaterialOption = null;
        reforgeMaterial = null;
    }

    private void OpenReforgePicker(MapEventOptionData option)
    {
        if (inventoryHud == null || !MapEventInventoryReforgeService.RequiresItem(option)) return;

        MapHUD.InventoryTab tab = MapHUD.InventoryTab.Loot;
        if (option.builtInEffectType == MapEventOptionBuiltInEffectType.ReforgeSelectedRelicSameRarity) tab = MapHUD.InventoryTab.Relics;
        else if (option.builtInEffectType == MapEventOptionBuiltInEffectType.ReforgeSelectedBlueprintSameRarity) tab = MapHUD.InventoryTab.Blueprints;

        BoardTileData tile = currentTile;
        MapEvent_SO eventData = currentEvent;
        openedInventory = inventoryHud.ShowInventorySelection(tab, item => MapEventInventoryReforgeService.HasReplacement(option, item), item =>
        {
            openedInventory = false;
            if (!IsVisible || currentTile != tile || currentEvent != eventData || isShowingResultStep) return;
            reforgeMaterialOption = option;
            reforgeMaterial = item;
            SelectOption(option);
        }, EventOverlayCanvasSortingOrder + 20);
    }

    private void UpdateReforgeAvailability()
    {
        canLeaveReforgeEvent = false;
        foreach (Button button in optionButtons)
        {
            if (button.interactable || !MapEventInventoryReforgeService.RequiresItem(optionLookup[button])) return;
        }
        canLeaveReforgeEvent = optionButtons.Count > 0;
    }

    private void UpdateChoiceButtonLabel()
    {
        if (canLeaveReforgeEvent) SetButtonLabel(GameText.Get("ui.event.reforge.leave", "离开"));
        else if (MapEventInventoryReforgeService.RequiresItem(selectedOption))
            SetButtonLabel(SelectedReforgeItem == null
                ? GameText.Get("ui.event.reforge.choose_material", "选择物品")
                : GameText.Get("ui.event.reforge.confirm", "确认重铸"));
        else SetButtonLabel(StoryText("confirm_choice", "确认选择", "Confirm choice"));
    }

    private void OnDisable() { HideOwnedInventory(); ResetPresentation(); }
}
