public struct BattleSingleAttackResult
{
    public bool IsDefenseOnly;
    public bool ShouldDealDamage;
    public int FinalPower;
    public int DisplayPowerModifier;
    public int HitPower;
    public int HitPowerModifier;
    public string FormattedPower;
}

public static class BattleSingleAttackResultService
{
    public static BattleSingleAttackResult Resolve(
        int basePower,
        int bonusPower,
        int statusPowerModifier,
        bool isDefenseSkill)
    {
        int finalPower = BattlePowerCalculationService.ClampFinalPower(basePower, bonusPower);
        bool shouldDealDamage = !isDefenseSkill;
        return new BattleSingleAttackResult
        {
            IsDefenseOnly = isDefenseSkill,
            ShouldDealDamage = shouldDealDamage,
            FinalPower = finalPower,
            DisplayPowerModifier = statusPowerModifier + bonusPower,
            HitPower = shouldDealDamage ? finalPower : 0,
            HitPowerModifier = shouldDealDamage ? bonusPower : 0,
            FormattedPower = BattlePowerCalculationService.FormatPowerWithBonus(basePower, bonusPower)
        };
    }
}
