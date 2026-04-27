using NUnit.Framework;

public class BattleClashResultServiceTests
{
    [Test]
    public void Resolve_AttackVsAttackAllyWin_UsesFollowUpPowerAndWinBonus()
    {
        BattleClashResult result = BattleClashResultService.Resolve(
            allyPower: 7,
            enemyPower: 5,
            allyDefenseSkill: false,
            enemyDefenseSkill: false,
            allyFollowUpBasePower: 4,
            enemyFollowUpBasePower: 3,
            allyWinAttackBonus: 2,
            enemyWinAttackBonus: 0);

        Assert.AreEqual(BattleClashOutcome.AllyAttackWins, result.Outcome);
        Assert.IsTrue(result.HasPresentationWinner);
        Assert.IsTrue(result.AllyWonPresentation);
        Assert.IsTrue(result.AllyTriggersClashWin);
        Assert.IsFalse(result.EnemyTriggersClashWin);
        Assert.AreEqual(6, result.AllyHitPower);
        Assert.AreEqual(3, result.EnemyHitPower);
        Assert.AreEqual(6, result.PresentationHitPower);
    }

    [Test]
    public void Resolve_AttackVsAttackTie_HasNoWinnerOrClashWinTrigger()
    {
        BattleClashResult result = BattleClashResultService.Resolve(
            allyPower: 5,
            enemyPower: 5,
            allyDefenseSkill: false,
            enemyDefenseSkill: false,
            allyFollowUpBasePower: 4,
            enemyFollowUpBasePower: 3,
            allyWinAttackBonus: 9,
            enemyWinAttackBonus: 9);

        Assert.AreEqual(BattleClashOutcome.AttackTie, result.Outcome);
        Assert.IsFalse(result.HasPresentationWinner);
        Assert.IsFalse(result.AllyTriggersClashWin);
        Assert.IsFalse(result.EnemyTriggersClashWin);
        Assert.AreEqual(4, result.AllyHitPower);
        Assert.AreEqual(3, result.EnemyHitPower);
    }

    [Test]
    public void Resolve_AllyAttackBreaksEnemyDefense_UsesRestoredAttackMinusDefensePower()
    {
        BattleClashResult result = BattleClashResultService.Resolve(
            allyPower: 8,
            enemyPower: 4,
            allyDefenseSkill: false,
            enemyDefenseSkill: true,
            allyFollowUpBasePower: 6,
            enemyFollowUpBasePower: 2,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.AreEqual(BattleClashOutcome.AllyBreaksEnemyDefense, result.Outcome);
        Assert.IsTrue(result.HasPresentationWinner);
        Assert.IsTrue(result.AllyWonPresentation);
        Assert.IsTrue(result.AllyTriggersClashWin);
        Assert.IsFalse(result.EnemyTriggersClashWin);
        Assert.AreEqual(4, result.AllyHitPower);
    }

    [Test]
    public void Resolve_EnemyDefenseBeatsAllyAttack_TriggersEnemyClashWinWithoutPresentationWinner()
    {
        BattleClashResult result = BattleClashResultService.Resolve(
            allyPower: 4,
            enemyPower: 6,
            allyDefenseSkill: false,
            enemyDefenseSkill: true,
            allyFollowUpBasePower: 5,
            enemyFollowUpBasePower: 3,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.AreEqual(BattleClashOutcome.EnemyDefenseWins, result.Outcome);
        Assert.IsFalse(result.HasPresentationWinner);
        Assert.IsFalse(result.AllyWonPresentation);
        Assert.IsFalse(result.AllyTriggersClashWin);
        Assert.IsTrue(result.EnemyTriggersClashWin);
        Assert.AreEqual(0, result.AllyHitPower);
        Assert.AreEqual(2, result.ClashDifference);
    }

    [Test]
    public void Resolve_AllyDefenseBeatsEnemyAttack_TriggersAllyClashWinAndReportsEnergyRestore()
    {
        BattleClashResult result = BattleClashResultService.Resolve(
            allyPower: 7,
            enemyPower: 4,
            allyDefenseSkill: true,
            enemyDefenseSkill: false,
            allyFollowUpBasePower: 5,
            enemyFollowUpBasePower: 3,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.AreEqual(BattleClashOutcome.AllyDefenseWins, result.Outcome);
        Assert.IsFalse(result.HasPresentationWinner);
        Assert.IsTrue(result.AllyTriggersClashWin);
        Assert.IsFalse(result.EnemyTriggersClashWin);
        Assert.AreEqual(3, result.DefenseEnergyRestoreAmount);
        Assert.AreEqual(0, result.EnemyHitPower);
    }

    [Test]
    public void Resolve_BothDefenseOnlyTriggersClashWinSide()
    {
        BattleClashResult result = BattleClashResultService.Resolve(
            allyPower: 3,
            enemyPower: 6,
            allyDefenseSkill: true,
            enemyDefenseSkill: true,
            allyFollowUpBasePower: 3,
            enemyFollowUpBasePower: 6,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.AreEqual(BattleClashOutcome.BothDefenseEnemyWins, result.Outcome);
        Assert.IsFalse(result.HasPresentationWinner);
        Assert.IsFalse(result.AllyTriggersClashWin);
        Assert.IsTrue(result.EnemyTriggersClashWin);
        Assert.AreEqual(0, result.AllyHitPower);
        Assert.AreEqual(0, result.EnemyHitPower);
    }
}
