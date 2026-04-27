public struct BattleSingleAttackDiceResolution
{
    public int BasePower;
    public int StatusPowerModifier;
    public int SkillUseBonus;
    public int ActionBonus;
    public int BonusPower;
    public BattleDicePowerPreparation PowerInput;
    public BattleSingleAttackResult AttackResult;
}

public static class BattleSingleAttackDiceResolutionService
{
    public static BattleSingleAttackDiceResolution Resolve(
        int basePower,
        int statusPowerModifier,
        int skillUseBonus,
        int actionBonus,
        bool isDefenseSkill)
    {
        BattleDicePowerPreparation powerInput = BattleDicePowerPreparationService.Resolve(
            basePower,
            statusPowerModifier,
            0,
            skillUseBonus,
            actionBonus);
        BattleSingleAttackResult attackResult = BattleSingleAttackResultService.Resolve(
            basePower,
            powerInput.BonusPower,
            statusPowerModifier,
            isDefenseSkill);

        return new BattleSingleAttackDiceResolution
        {
            BasePower = basePower,
            StatusPowerModifier = statusPowerModifier,
            SkillUseBonus = powerInput.SkillUseBonus,
            ActionBonus = powerInput.ActionBonus,
            BonusPower = powerInput.BonusPower,
            PowerInput = powerInput,
            AttackResult = attackResult
        };
    }
}
