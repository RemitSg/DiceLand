using UnityEngine.UI;

public partial class MapEventOverlay
{
    private MapHUD inventoryHud;
    private Button inventoryEntry;
    private bool openedInventory;

    public void ConfigureInventory(MapHUD hud)
    {
        inventoryHud = hud;
        if (inventoryEntry != null) inventoryEntry.gameObject.SetActive(hud != null);
    }

    private void BuildInventoryEntry()
    {
        inventoryEntry = MapInventoryEntry.Create(safeAreaRect, () =>
        {
            if (inventoryHud == null || !IsVisible) return;
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

    private void OnDisable() { HideOwnedInventory(); }
}
