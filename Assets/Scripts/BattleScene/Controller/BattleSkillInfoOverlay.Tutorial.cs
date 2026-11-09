using UnityEngine;

public partial class BattleSkillInfoOverlay
{
    public bool IsAllySelectionVisible => IsVisible && viewMode == SkillViewMode.Ally;
    public bool HasTutorialEnergySkillSelected => IsAllySelectionVisible &&
        currentAlly != null && currentAlly.GetSelectedSkillForButton(currentAllySourceButton) != null &&
        currentAlly.GetSelectedSkillForButton(currentAllySourceButton).energyCost > 0;

    public bool TryGetTutorialCloseRect(out Rect rect)
    {
        return TutorialHighlightCoordinateUtility.TryGetScreenRect(
            footerClose != null ? (RectTransform)footerClose.transform : null, out rect);
    }

    public bool TryGetTutorialSkillListRect(out Rect rect)
    {
        rect = default;
        return IsAllySelectionVisible && TutorialHighlightCoordinateUtility.TryGetScreenRect(
            skillListScroll != null ? skillListScroll.viewport : null, out rect);
    }

    public bool TryGetTutorialReadingRect(out Rect rect)
    {
        var reading = viewMode == SkillViewMode.EnemyData ? enemyProfileScroll : detailScrollRect;
        return TutorialHighlightCoordinateUtility.TryGetScreenRect(
            reading != null ? reading.viewport : null, out rect);
    }
}
