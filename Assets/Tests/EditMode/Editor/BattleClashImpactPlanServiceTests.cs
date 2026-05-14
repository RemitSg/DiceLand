using NUnit.Framework;

public class BattleClashImpactPlanServiceTests
{
    [Test]
    public void Resolve_AttackVsAttackAllyWin_AppliesAllyHitAndEnemyMiss()
    {
        BattleClashImpactPlan plan = BattleClashImpactPlanService.Resolve(
            new BattleClashResult
            {
                Outcome = BattleClashOutcome.AllyAttackWins,
                AllyHitPower = 7
            },
            allyPower: 8,
            enemyPower: 5,
            allyBonusPower: 2,
            enemyBonusPower: 1,
            allySkillUseBonus: 3,
            enemySkillUseBonus: 4,
            allyWinAttackBonus: 5,
            enemyWinAttackBonus: 6);

        Assert.IsTrue(plan.ApplyAllyHit);
        Assert.IsTrue(plan.ApplyAllyClashWinEffects);
        Assert.IsTrue(plan.ApplyEnemyClashLoseEffects);
        Assert.IsTrue(plan.ApplyEnemyMiss);
        Assert.IsFalse(plan.ApplyEnemyHit);
        Assert.AreEqual(7, plan.AllyHitPower);
        Assert.AreEqual(8, plan.AllyHitPowerModifier);
    }

    [Test]
    public void Resolve_AttackVsAttackEnemyWin_AppliesEnemyHitGatekeeperAndAllyMiss()
    {
        BattleClashImpactPlan plan = BattleClashImpactPlanService.Resolve(
            new BattleClashResult
            {
                Outcome = BattleClashOutcome.EnemyAttackWins,
                EnemyHitPower = 6
            },
            allyPower: 4,
            enemyPower: 9,
            allyBonusPower: 2,
            enemyBonusPower: 1,
            allySkillUseBonus: 3,
            enemySkillUseBonus: 4,
            allyWinAttackBonus: 5,
            enemyWinAttackBonus: 6);

        Assert.IsTrue(plan.ApplyEnemyHit);
        Assert.IsTrue(plan.ApplyEnemyClashWinEffects);
        Assert.IsTrue(plan.ApplyAllyMiss);
        Assert.IsTrue(plan.ApplyHornedGatekeeperClashWin);
        Assert.AreEqual(6, plan.EnemyHitPower);
        Assert.AreEqual(10, plan.EnemyHitPowerModifier);
        Assert.AreEqual(6, plan.HornedGatekeeperClashDifference);
    }

    [Test]
    public void Resolve_AttackTie_AppliesBothMissOnly()
    {
        BattleClashImpactPlan plan = BattleClashImpactPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.AttackTie },
            allyPower: 5,
            enemyPower: 5,
            allyBonusPower: 0,
            enemyBonusPower: 0,
            allySkillUseBonus: 0,
            enemySkillUseBonus: 0,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.IsTrue(plan.ApplyAllyMiss);
        Assert.IsTrue(plan.ApplyEnemyMiss);
        Assert.IsFalse(plan.ApplyAllyHit);
        Assert.IsFalse(plan.ApplyEnemyHit);
    }

    [Test]
    public void Resolve_EnemyDefenseWin_ShowsEnemyZeroDamageAndGatekeeperDifference()
    {
        BattleClashImpactPlan plan = BattleClashImpactPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.EnemyDefenseWins },
            allyPower: 3,
            enemyPower: 8,
            allyBonusPower: 4,
            enemyBonusPower: 0,
            allySkillUseBonus: 0,
            enemySkillUseBonus: 0,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.IsTrue(plan.ShowZeroDamageOnEnemy);
        Assert.IsTrue(plan.ApplyEnemyClashWinEffects);
        Assert.IsTrue(plan.ApplyHornedGatekeeperClashWin);
        Assert.AreEqual(5, plan.HornedGatekeeperClashDifference);
        Assert.AreEqual(4, plan.EnemyZeroDamagePowerModifier);
    }

    [Test]
    public void Resolve_AllyDefenseWin_RestoresEnergyAndAppliesDefenseRelics()
    {
        BattleClashImpactPlan plan = BattleClashImpactPlanService.Resolve(
            new BattleClashResult
            {
                Outcome = BattleClashOutcome.AllyDefenseWins,
                DefenseEnergyRestoreAmount = 3
            },
            allyPower: 7,
            enemyPower: 4,
            allyBonusPower: 0,
            enemyBonusPower: 2,
            allySkillUseBonus: 0,
            enemySkillUseBonus: 0,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.IsTrue(plan.ApplyPlayerDefenseClashWinRelics);
        Assert.IsTrue(plan.ApplyAllyClashWinEffects);
        Assert.IsTrue(plan.ApplyEnemyClashLoseEffects);
        Assert.IsTrue(plan.ShowZeroDamageOnAlly);
        Assert.IsTrue(plan.RestoreAllyDefenseEnergy);
        Assert.AreEqual(3, plan.DefenseEnergyRestoreAmount);
        Assert.AreEqual(2, plan.AllyZeroDamagePowerModifier);
    }

    [Test]
    public void Resolve_BothDefenseEnemyWin_AppliesEnemyClashWinAndGatekeeper()
    {
        BattleClashImpactPlan plan = BattleClashImpactPlanService.Resolve(
            new BattleClashResult { Outcome = BattleClashOutcome.BothDefenseEnemyWins },
            allyPower: 2,
            enemyPower: 6,
            allyBonusPower: 0,
            enemyBonusPower: 0,
            allySkillUseBonus: 0,
            enemySkillUseBonus: 0,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.IsFalse(plan.ApplyAllyHit);
        Assert.IsFalse(plan.ApplyEnemyHit);
        Assert.IsTrue(plan.ApplyEnemyClashWinEffects);
        Assert.IsTrue(plan.ApplyHornedGatekeeperClashWin);
        Assert.AreEqual(4, plan.HornedGatekeeperClashDifference);
        Assert.IsFalse(plan.ApplyPlayerDefenseClashWinRelics);
        Assert.IsFalse(plan.ShowZeroDamageOnAlly);
        Assert.IsFalse(plan.ShowZeroDamageOnEnemy);
    }
}
