public enum BattleButtonClickBlockReason
{
    None = 0,
    SettlementVisible = 1,
    TutorialBlocking = 2,
    SkillInfoVisible = 3
}

public readonly struct BattleButtonClickBlockResult
{
    public BattleButtonClickBlockResult(BattleButtonClickBlockReason reason)
    {
        Reason = reason;
    }

    public BattleButtonClickBlockReason Reason { get; }
    public bool ShouldBlock => Reason != BattleButtonClickBlockReason.None;
}

public static class BattleButtonUiOverlayStateService
{
    public static BattleButtonClickBlockResult ResolveClickBlock(
        bool settlementVisible,
        bool tutorialBlocking,
        bool skillInfoVisible)
    {
        if (settlementVisible)
            return new BattleButtonClickBlockResult(BattleButtonClickBlockReason.SettlementVisible);
        if (tutorialBlocking)
            return new BattleButtonClickBlockResult(BattleButtonClickBlockReason.TutorialBlocking);
        if (skillInfoVisible)
            return new BattleButtonClickBlockResult(BattleButtonClickBlockReason.SkillInfoVisible);

        return new BattleButtonClickBlockResult(BattleButtonClickBlockReason.None);
    }
}
