public readonly struct BattleCombatPhaseBeginPlan
{
    public readonly bool MarkResolvingCombat;
    public readonly bool CommitReservedEnergy;
    public readonly bool LockCombatInput;
    public readonly bool HideAllySpeedButtons;
    public readonly bool DisableAllySpeedButtons;
    public readonly bool HideEnemySpeedButtons;
    public readonly bool HideSkillInfoOverlay;

    public BattleCombatPhaseBeginPlan(
        bool markResolvingCombat,
        bool commitReservedEnergy,
        bool lockCombatInput,
        bool hideAllySpeedButtons,
        bool disableAllySpeedButtons,
        bool hideEnemySpeedButtons,
        bool hideSkillInfoOverlay)
    {
        MarkResolvingCombat = markResolvingCombat;
        CommitReservedEnergy = commitReservedEnergy;
        LockCombatInput = lockCombatInput;
        HideAllySpeedButtons = hideAllySpeedButtons;
        DisableAllySpeedButtons = disableAllySpeedButtons;
        HideEnemySpeedButtons = hideEnemySpeedButtons;
        HideSkillInfoOverlay = hideSkillInfoOverlay;
    }
}

public static class BattleCombatPhaseBeginPlanService
{
    public static BattleCombatPhaseBeginPlan Resolve()
    {
        return new BattleCombatPhaseBeginPlan(
            markResolvingCombat: true,
            commitReservedEnergy: true,
            lockCombatInput: true,
            hideAllySpeedButtons: true,
            disableAllySpeedButtons: true,
            hideEnemySpeedButtons: true,
            hideSkillInfoOverlay: true);
    }
}
