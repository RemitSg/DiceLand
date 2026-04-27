using System;

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
    public static BattleSingleAttackDiceResolution ResolveWithModifiers(
        EnemySkill_SO skill,
        int diceIndex,
        int basePower,
        int statusPowerModifier,
        bool isDefenseSkill,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null,
        int relicPowerBonus = 0)
    {
        int skillUseBonus = BattleDicePowerModifierCollectionService.GetSkillUseBonus(
            skill,
            diceIndex,
            selfSpeed,
            targetSpeed,
            getSelfStatusStacksByAsset,
            getSelfStatusStacksById,
            getTargetStatusStacksByAsset,
            getTargetStatusStacksById,
            targetSkill,
            relicPowerBonus);
        int actionBonus = BattleDicePowerModifierCollectionService.GetActionBonus(
            skill,
            diceIndex,
            EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
            selfSpeed,
            targetSpeed,
            getSelfStatusStacksByAsset,
            getSelfStatusStacksById,
            getTargetStatusStacksByAsset,
            getTargetStatusStacksById,
            targetSkill);

        return Resolve(basePower, statusPowerModifier, skillUseBonus, actionBonus, isDefenseSkill);
    }

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
