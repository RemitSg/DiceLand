using System;
using NUnit.Framework;

public class BattleHealingEffectResolutionServiceTests
{
    [Test]
    public void ResolveHealingAmount_FixedAmount_ClampsNegativeHealAmount()
    {
        EnemySkill_SO.HealingEffectEntry effect = new EnemySkill_SO.HealingEffectEntry
        {
            amountMode = EnemySkill_SO.HealAmountMode.FixedAmount,
            healAmount = -3
        };

        int result = BattleHealingEffectResolutionService.ResolveHealingAmount(
            effect,
            dealtDamage: 5,
            resolveStatusStacks: _ => 7);

        Assert.AreEqual(0, result);
    }

    [Test]
    public void ResolveHealingAmount_DamageDealt_UsesNonNegativeDamage()
    {
        EnemySkill_SO.HealingEffectEntry effect = new EnemySkill_SO.HealingEffectEntry
        {
            amountMode = EnemySkill_SO.HealAmountMode.DamageDealt,
            healAmount = 10
        };

        Assert.AreEqual(6, BattleHealingEffectResolutionService.ResolveHealingAmount(effect, 6, _ => 99));
        Assert.AreEqual(0, BattleHealingEffectResolutionService.ResolveHealingAmount(effect, -6, _ => 99));
    }

    [Test]
    public void ResolveHealingAmount_StatusStacks_MultipliesStacksByAtLeastOneHealAmount()
    {
        EnemySkill_SO.HealingEffectEntry effect = new EnemySkill_SO.HealingEffectEntry
        {
            amountMode = EnemySkill_SO.HealAmountMode.StatusStacks,
            healAmount = 0
        };

        int result = BattleHealingEffectResolutionService.ResolveHealingAmount(
            effect,
            dealtDamage: 0,
            resolveStatusStacks: mode => mode == EnemySkill_SO.DamageTargetMode.CurrentTarget ? 4 : 0);

        Assert.AreEqual(4, result);
    }

    [Test]
    public void ResolveStatusStacks_UsesStatusAssetBeforeLegacyBurnFallback()
    {
        BattleStatus_SO status = CreateStatus("poison");
        EnemySkill_SO.HealingEffectEntry effect = new EnemySkill_SO.HealingEffectEntry
        {
            statusData = status,
            statusType = EnemySkill_SO.DiceStatusType.Burn
        };

        int result = BattleHealingEffectResolutionService.ResolveStatusStacks(
            effect,
            resolveStatusDataStacks: data => data == status ? 3 : 0,
            resolveStatusIdStacks: id => id == "burn" ? 9 : 0);

        Assert.AreEqual(3, result);
    }

    [Test]
    public void ResolveStatusStacks_UsesBurnFallbackOnlyForBurnStatusType()
    {
        EnemySkill_SO.HealingEffectEntry burnEffect = new EnemySkill_SO.HealingEffectEntry
        {
            statusType = EnemySkill_SO.DiceStatusType.Burn
        };
        EnemySkill_SO.HealingEffectEntry electroEffect = new EnemySkill_SO.HealingEffectEntry
        {
            statusType = EnemySkill_SO.DiceStatusType.Electro
        };

        Assert.AreEqual(5, BattleHealingEffectResolutionService.ResolveStatusStacks(burnEffect, null, id => id == "burn" ? 5 : 0));
        Assert.AreEqual(0, BattleHealingEffectResolutionService.ResolveStatusStacks(electroEffect, null, id => 5));
    }

    [Test]
    public void IsConditionMet_StatusConditionsUseConditionStatusBeforeHealingStatus()
    {
        BattleStatus_SO conditionStatus = CreateStatus("mark");
        BattleStatus_SO healingStatus = CreateStatus("burn");
        EnemySkill_SO.HealingEffectEntry effect = new EnemySkill_SO.HealingEffectEntry
        {
            useCondition = true,
            condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast,
            conditionStatusData = conditionStatus,
            statusData = healingStatus,
            requiredConditionStatusStacks = 2
        };

        bool result = BattleHealingEffectResolutionService.IsConditionMet(
            effect,
            selfSpeed: 1,
            targetSpeed: 1,
            resolveConditionStatusDataStacks: data => data == conditionStatus ? 2 : 0,
            resolveConditionStatusIdStacks: _ => 0);

        Assert.IsTrue(result);
    }

    [Test]
    public void IsConditionMet_DelegatesSpeedConditionsToModifierConditionService()
    {
        EnemySkill_SO.HealingEffectEntry effect = new EnemySkill_SO.HealingEffectEntry
        {
            useCondition = true,
            condition = EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget,
            requiredConditionSpeedDifference = 2
        };

        Assert.IsTrue(BattleHealingEffectResolutionService.IsConditionMet(effect, 5, 3, null, null));
        Assert.IsFalse(BattleHealingEffectResolutionService.IsConditionMet(effect, 4, 3, null, null));
    }

    [Test]
    public void ShouldApplyEffect_RequiresMatchingTrigger()
    {
        EnemySkill_SO.HealingEffectEntry effect = new EnemySkill_SO.HealingEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit
        };

        bool result = BattleHealingEffectResolutionService.ShouldApplyEffect(
            effect,
            EnemySkill_SO.DiceEffectTrigger.OnClashWin,
            selfSpeed: 1,
            targetSpeed: 1,
            resolveConditionStatusDataStacks: null,
            resolveConditionStatusIdStacks: null);

        Assert.IsFalse(result);
    }

    [Test]
    public void ShouldApplyEffect_RequiresConditionWhenTriggerMatches()
    {
        EnemySkill_SO.HealingEffectEntry effect = new EnemySkill_SO.HealingEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            useCondition = true,
            condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
            conditionStatusType = EnemySkill_SO.DiceStatusType.Burn
        };

        bool result = BattleHealingEffectResolutionService.ShouldApplyEffect(
            effect,
            EnemySkill_SO.DiceEffectTrigger.OnHit,
            selfSpeed: 1,
            targetSpeed: 1,
            resolveConditionStatusDataStacks: null,
            resolveConditionStatusIdStacks: id => id == "burn" ? 0 : 3);

        Assert.IsFalse(result);
    }

    [Test]
    public void DispatchHeal_LogsOnlyWhenActualHealIsPositive()
    {
        string log = null;

        int actualHeal = BattleHealingEffectDispatcher.DispatchHeal(
            healAmount: 5,
            heal: amount => amount - 2,
            logApplied: healed => log = $"healed:{healed}");

        Assert.AreEqual(3, actualHeal);
        Assert.AreEqual("healed:3", log);
    }

    [Test]
    public void DispatchHeal_SkipsLogWhenNoHealingApplied()
    {
        bool logged = false;

        int actualHeal = BattleHealingEffectDispatcher.DispatchHeal(
            healAmount: 5,
            heal: _ => 0,
            logApplied: _ => logged = true);

        Assert.AreEqual(0, actualHeal);
        Assert.IsFalse(logged);
    }

    private static BattleStatus_SO CreateStatus(string id)
    {
        BattleStatus_SO status = UnityEngine.ScriptableObject.CreateInstance<BattleStatus_SO>();
        status.name = id;
        return status;
    }
}
