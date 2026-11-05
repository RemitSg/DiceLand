using UnityEngine;

public partial class BattleSettlementOverlay
{
    private MapTreasureOverlay sharedRelicPanel;

    private bool TryOpenSharedRelicPanel(BattleSettlementRelicChoiceData data)
    {
        if (MapTreasureArt.Load() == null) return false;
        if (data.rewardApplied) return true;
        HideLootDetailModal();
        activeRelicChoiceData = data;
        SetRelicChoiceModalVisible(false);
        if (sharedRelicPanel == null)
        {
            GameObject host = new GameObject("SettlementRelicSelection");
            host.transform.SetParent(transform, false);
            sharedRelicPanel = host.AddComponent<MapTreasureOverlay>();
            sharedRelicPanel.ConfigureForestPresentation();
            sharedRelicPanel.Hidden += UpdateLootSlotColors;
        }
        sharedRelicPanel.ShowRelicSelection(data, (BattleSettlementRelicChoiceData selection, out string message) =>
        {
            if (!IsVisible || selection != activeRelicChoiceData || selection.rewardApplied || selection.selectedChoice == null ||
                selection.selectedChoice.relicData == null || !selection.choices.Contains(selection.selectedChoice))
            {
                message = string.IsNullOrWhiteSpace(selection?.requiredMessage) ? GameText.Get("battle.relic.choice_required", relicChoiceRequiredMessage) : selection.requiredMessage;
                return false;
            }
            // Do not grant here. TryApplyRequiredRelicChoice is still the sole battle-return commit point.
            UpdateLootSlotColors();
            message = GameText.Get("battle.settlement.relic_choice_confirmed", "已确认遗物选择。");
            return true;
        });
        Canvas sharedCanvas = sharedRelicPanel.GetComponentInChildren<Canvas>(true);
        sharedCanvas.sortingOrder = overlayCanvas.sortingOrder + 20;
        return true;
    }

    private void HideSharedRelicPanel()
    {
        if (sharedRelicPanel != null) sharedRelicPanel.HideImmediately();
    }
}
