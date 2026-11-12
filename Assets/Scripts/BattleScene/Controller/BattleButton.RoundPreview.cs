using UnityEngine;

public partial class BattleButton
{
    private BattleRoundPreviewOverlay roundPreview;
    private bool roundPreviewDirty = true;
    private int roundPreviewTargetingRevision;
    private string roundPreviewLanguage;
    private Canvas actionButtonCanvas;
    private readonly Vector3[] actionButtonCorners = new Vector3[4];

    private void RefreshRoundPreview()
    {
        if (headlessExecution || sharedEnergyRoot == null)
            return;

        bool visible = currentPhase == BattlePhase.Targeting && !isResolvingCombat && !SpeedButton.CombatLocked
            && activeRoundAllies != null && activeRoundEnemies != null && !SpeedButton.InputSuppressed
            && !(battleSkillInfoOverlay != null && battleSkillInfoOverlay.IsVisible)
            && !(battlePauseMenuOverlay != null && battlePauseMenuOverlay.IsVisible)
            && !(battleSettlementOverlay != null && battleSettlementOverlay.IsVisible)
            && !(tutorialBattleGuideOverlay != null && (tutorialBattleGuideOverlay.IsModalVisible || tutorialBattleGuideOverlay.IsTargetHighlightVisible));
        if (!visible)
        {
            roundPreview?.SetVisible(false);
            return;
        }

        if (roundPreview == null)
        {
            roundPreview = new BattleRoundPreviewOverlay(sharedEnergyRoot,
                sharedEnergyFont != null ? sharedEnergyFont : tutorialGuideKeywordFont, tutorialGuideBodyFont);
            actionButtonCanvas = GetComponentInParent<Canvas>();
        }

        // Wait until the frame's targeting callbacks have settled. Reconnecting may briefly
        // release a contested enemy slot and reassign it before the final plan is ready.
        if (roundPreviewDirty || roundPreviewTargetingRevision != SpeedButton.TargetingRevision || roundPreviewLanguage != GameText.LanguageCode)
        {
            roundPreviewDirty = false;
            roundPreviewTargetingRevision = SpeedButton.TargetingRevision;
            roundPreviewLanguage = GameText.LanguageCode;
            roundPreview.Show(BattleCombatActionBuilder.Build(activeRoundAllies, activeRoundEnemies));
        }
        else
            roundPreview.SetVisible(true);

        roundPreview.ApplyViewport(Screen.safeArea, new Vector2(Screen.width, Screen.height), GetActionButtonScreenBounds());
        roundPreview.UpdateBadges(Camera.main);
    }

    private Rect GetActionButtonScreenBounds()
    {
        if (!(transform is RectTransform button) || actionButtonCanvas == null)
            return default;

        Canvas canvas = actionButtonCanvas.rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        button.GetWorldCorners(actionButtonCorners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, actionButtonCorners[0]);
        Vector2 max = min;
        for (int i = 1; i < actionButtonCorners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, actionButtonCorners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
