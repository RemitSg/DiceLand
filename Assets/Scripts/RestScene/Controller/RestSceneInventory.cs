using UnityEngine;

/// <summary>Reads the live visit snapshot and shares the station modal lock until inventory fully closes.</summary>
[DisallowMultipleComponent]
public sealed class RestSceneInventory : MonoBehaviour
{
    private MapHUD hud;

    private void Start()
    {
        GameObject host = new GameObject("RestInventory");
        host.transform.SetParent(transform, false);
        hud = host.AddComponent<MapHUD>();
        hud.InitializeInventoryOnly();
        hud.InventoryHidden += HandleHidden;
        // Replace the map toggle with a visit-aware open action.
        UnityEngine.UI.Button entry = host.GetComponentInChildren<UnityEngine.UI.Button>(true);
        entry.onClick.RemoveAllListeners();
        entry.onClick.AddListener(Open);
        RefreshEntry();
    }

    private void Update() { RefreshEntry(); }

    private void RefreshEntry()
    {
        if (hud == null) return;
        bool hasRun = RestSceneRuntime.HasPendingVisit && MapRunRuntime.GetRunStateForInventory(RestSceneRuntime.ReturnSceneName) != null;
        hud.SetInventoryEntryInteractable(hasRun && !RestSceneModalState.IsLocked);
    }

    public void Open()
    {
        RunState state = RestSceneRuntime.HasPendingVisit ? MapRunRuntime.GetRunStateForInventory(RestSceneRuntime.ReturnSceneName) : null;
        if (hud == null || state == null) return;
        if (!RestSceneModalState.TryAcquire(this)) return;
        hud.Refresh(state, null);
        hud.ShowInventoryPanel();
    }

    private void HandleHidden() { RestSceneModalState.Release(this); RefreshEntry(); }

    private void OnDisable()
    {
        if (hud != null) hud.HideInventoryPanel();
        RestSceneModalState.Release(this);
    }

    private void OnDestroy()
    {
        if (hud != null) hud.InventoryHidden -= HandleHidden;
        RestSceneModalState.Release(this);
    }
}
