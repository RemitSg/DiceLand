public static class BattleCombatPhaseGuardService
{
    public static bool ShouldBlockInteraction(
        bool settlementOverlayVisible,
        bool skillInfoOverlayVisible,
        bool tutorialGuideOverlayVisible)
    {
        return settlementOverlayVisible || skillInfoOverlayVisible || tutorialGuideOverlayVisible;
    }
}
