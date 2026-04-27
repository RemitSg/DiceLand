public struct BattleDicePowerPreparation
{
    public int SkillUseBonus;
    public int ActionBonus;
    public int BonusPower;
    public int FinalPower;
    public int FollowUpBasePower;
    public int DisplayPowerModifier;
    public string FormattedPower;
}

public static class BattleDicePowerPreparationService
{
    public static BattleDicePowerPreparation Resolve(
        int basePower,
        int statusPowerModifier,
        int relicPowerBonus,
        int skillUsePowerBonus,
        int actionPowerBonus)
    {
        int skillUseBonus = relicPowerBonus + skillUsePowerBonus;
        int bonusPower = skillUseBonus + actionPowerBonus;
        return new BattleDicePowerPreparation
        {
            SkillUseBonus = skillUseBonus,
            ActionBonus = actionPowerBonus,
            BonusPower = bonusPower,
            FinalPower = BattlePowerCalculationService.ClampFinalPower(basePower, bonusPower),
            FollowUpBasePower = BattlePowerCalculationService.ClampFinalPower(basePower, skillUseBonus),
            DisplayPowerModifier = statusPowerModifier + bonusPower,
            FormattedPower = BattlePowerCalculationService.FormatPowerWithBonus(basePower, bonusPower)
        };
    }
}
