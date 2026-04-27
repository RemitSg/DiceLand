using System;

public static class BattleDicePowerModifierCollectionService
{
    public static int GetSkillUseBonus(
        EnemySkill_SO skill,
        int diceIndex,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null,
        int relicPowerBonus = 0)
    {
        return relicPowerBonus + BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            diceIndex,
            EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
            selfSpeed,
            targetSpeed,
            getSelfStatusStacksByAsset,
            getSelfStatusStacksById,
            getTargetStatusStacksByAsset,
            getTargetStatusStacksById,
            targetSkill);
    }

    public static int GetActionBonus(
        EnemySkill_SO skill,
        int diceIndex,
        EnemySkill_SO.DiceEffectTrigger trigger,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null)
    {
        return BattlePowerModifierService.EvaluatePowerModifier(
            skill,
            diceIndex,
            trigger,
            selfSpeed,
            targetSpeed,
            getSelfStatusStacksByAsset,
            getSelfStatusStacksById,
            getTargetStatusStacksByAsset,
            getTargetStatusStacksById,
            targetSkill);
    }

    public static int GetClashWinBonus(
        EnemySkill_SO skill,
        int diceIndex,
        bool shouldApply,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null)
    {
        if (!shouldApply)
            return 0;

        return GetActionBonus(
            skill,
            diceIndex,
            EnemySkill_SO.DiceEffectTrigger.OnClashWin,
            selfSpeed,
            targetSpeed,
            getSelfStatusStacksByAsset,
            getSelfStatusStacksById,
            getTargetStatusStacksByAsset,
            getTargetStatusStacksById,
            targetSkill);
    }
}
