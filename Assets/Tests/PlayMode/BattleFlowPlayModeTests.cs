using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class BattleFlowPlayModeTests
{
    private BattleFlowPlayModeFixture fixture;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        fixture = new BattleFlowPlayModeFixture();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        fixture?.TearDown();
        fixture = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator BattleButtonClick_EntersTargetingPhaseAndAutoConnectsEnemy()
    {
        fixture.BuildOneOnOneBattle(allyPower: 4, enemyPower: 1, enemyHp: 12);

        fixture.ClickBattleButton();
        yield return null;

        Assert.AreEqual("Targeting", fixture.CurrentBattlePhase);
        Assert.IsTrue(fixture.AllyButtonInteractable);
        Assert.IsNotNull(fixture.EnemyConnectedTarget);
    }

    [UnityTest]
    public IEnumerator ContestedRound_WhenAllyWinsAndEnemyHpFallsToZero_ShowsSettlement()
    {
        fixture.BuildOneOnOneBattle(allyPower: 8, enemyPower: 1, enemyHp: 4);

        fixture.ClickBattleButton();
        yield return null;

        fixture.ForceContestedConnection();
        fixture.ClickBattleButton();
        yield return fixture.WaitUntilCombatSettles();

        Assert.IsTrue(fixture.EnemyIsDead);
        Assert.AreEqual(0, fixture.EnemyCurrentHp);
        Assert.IsTrue(fixture.SettlementOverlayVisible);
    }

    [UnityTest]
    public IEnumerator AllySingleAttackRound_WhenEnemyDoesNotContest_DamagesEnemyAndReturnsReady()
    {
        fixture.BuildOneOnOneBattle(allyPower: 3, enemyPower: 1, enemyHp: 12);

        fixture.ClickBattleButton();
        yield return null;

        fixture.ForceAllySingleAttackConnection();
        fixture.ClickBattleButton();
        yield return fixture.WaitUntilCombatSettles();

        Assert.AreEqual("Ready", fixture.CurrentBattlePhase);
        Assert.Greater(fixture.EnemyCurrentHp, 0);
        Assert.Less(fixture.EnemyCurrentHp, 12);
        Assert.IsFalse(fixture.SettlementOverlayVisible);
    }

    [UnityTest]
    public IEnumerator EnemySingleAttackRound_WhenAllyDies_ShowsDefeatSettlement()
    {
        fixture.BuildOneOnOneBattle(allyPower: 1, enemyPower: 30, enemyHp: 20, allyHp: 6);

        fixture.ClickBattleButton();
        yield return null;

        fixture.ForceEnemySingleAttackConnection();
        fixture.ClickBattleButton();
        yield return fixture.WaitUntilCombatSettles();

        Assert.IsTrue(fixture.AllyIsDead);
        Assert.IsTrue(fixture.SettlementOverlayVisible);
    }

    [UnityTest]
    public IEnumerator TutorialPolicy_WhenCombatBlocked_DoesNotEnterResolvingCombat()
    {
        fixture.BuildOneOnOneBattle(allyPower: 8, enemyPower: 1, enemyHp: 4);
        fixture.BeginBlockingTutorialStep();

        fixture.ClickBattleButton();
        yield return null;

        fixture.ForceContestedConnection();
        fixture.ClickBattleButton();
        yield return null;

        Assert.IsFalse(fixture.IsResolvingCombat);
        Assert.AreEqual("Targeting", fixture.CurrentBattlePhase);
    }

    [UnityTest]
    public IEnumerator EliteVictory_WhenRelicChoiceEnabled_SettlementContainsRelicChoice()
    {
        fixture.BuildEliteBattleWithRelicChoices(allyPower: 8, enemyPower: 1, enemyHp: 4);

        fixture.ClickBattleButton();
        yield return null;

        fixture.ForceContestedConnection();
        fixture.ClickBattleButton();
        yield return fixture.WaitUntilCombatSettles();

        Assert.IsTrue(fixture.SettlementOverlayVisible);
        Assert.IsTrue(fixture.LastSettlementHasRelicChoice);
    }

    [UnityTest]
    public IEnumerator EliteRelicChoice_WhenPlayerSelectsChoice_RecordsSelectionAndClosesModal()
    {
        fixture.BuildEliteBattleWithRelicChoices(allyPower: 8, enemyPower: 1, enemyHp: 4);

        fixture.ClickBattleButton();
        yield return null;

        fixture.ForceContestedConnection();
        fixture.ClickBattleButton();
        yield return fixture.WaitUntilCombatSettles();

        fixture.ClickRelicChoiceEntrance();
        yield return null;

        Assert.IsTrue(fixture.RelicChoiceModalVisible);

        string expectedChoiceName = fixture.RelicChoiceOptionName(1);
        fixture.ClickRelicChoiceOption(1);
        fixture.ClickRelicChoiceConfirmButton();
        yield return null;

        Assert.IsFalse(fixture.RelicChoiceModalVisible);
        Assert.AreEqual(expectedChoiceName, fixture.SelectedRelicChoiceName);
        Assert.AreEqual(expectedChoiceName, fixture.PresentedRelicChoiceEntranceName);
    }

    [UnityTest]
    public IEnumerator EliteRelicChoice_WhenReturningBeforeSelection_BlocksReturnAndShowsRequiredChoice()
    {
        fixture.BuildEliteBattleWithRelicChoices(allyPower: 8, enemyPower: 1, enemyHp: 4);

        fixture.ClickBattleButton();
        yield return null;

        fixture.ForceContestedConnection();
        fixture.ClickBattleButton();
        yield return fixture.WaitUntilCombatSettles();

        fixture.ClickSettlementReturnButton();
        yield return null;

        Assert.IsTrue(fixture.SettlementOverlayVisible);
        Assert.IsTrue(fixture.RelicChoiceModalVisible);
        Assert.IsNull(fixture.SelectedRelicChoiceName);
        Assert.AreEqual("返回前请选择一个精英遗物奖励。", fixture.RelicChoiceModalTitle);
    }

    [UnityTest]
    public IEnumerator DefeatSettlement_WhenRetryClicked_RequeuesEncounterAndHidesSettlement()
    {
        fixture.BuildBattleWithRetryEncounter(allyPower: 1, enemyPower: 30, enemyHp: 20, allyHp: 6);

        fixture.ClickBattleButton();
        yield return null;

        fixture.ForceEnemySingleAttackConnection();
        fixture.ClickBattleButton();
        yield return fixture.WaitUntilCombatSettles();

        Assert.IsTrue(fixture.SettlementOverlayVisible);

        fixture.ClickSettlementRetryButton();
        yield return null;

        Assert.IsFalse(fixture.SettlementOverlayVisible);
        Assert.IsTrue(fixture.CurrentEncounterQueued);
        Assert.IsFalse(fixture.SpeedButtonCombatLocked);
    }

    [UnityTest]
    public IEnumerator RoundStart_WhenBurningStatusExists_AppliesTickBeforeTargetingInput()
    {
        fixture.BuildBattleWithEnemyRoundStartBurn(allyPower: 1, enemyPower: 1, enemyHp: 10, burnStacks: 3);

        fixture.ClickBattleButton();
        yield return null;

        Assert.AreEqual(7, fixture.EnemyCurrentHp);
        Assert.AreEqual("Targeting", fixture.CurrentBattlePhase);
    }

    [UnityTest]
    public IEnumerator RoundStart_WhenEnemyRetreats_ShowsSettlementWithoutCombatInput()
    {
        fixture.BuildBattleWithEnemyRoundStartRetreat(allyPower: 1, enemyPower: 1, enemyHp: 10);

        fixture.ClickBattleButton();
        yield return null;

        Assert.IsTrue(fixture.SettlementOverlayVisible);
        Assert.IsFalse(fixture.IsResolvingCombat);
    }

    [UnityTest]
    public IEnumerator ClashVictory_WhenSkillHasOnUseAndOnHitStatusEffects_AppliesEachEffectOnce()
    {
        fixture.BuildBattleWithAllyStatusEffectSkill(allyPower: 8, enemyPower: 1, enemyHp: 50);

        fixture.ClickBattleButton();
        yield return null;

        Assert.AreEqual("PlayMode Status Strike", fixture.AllySelectedSkillName);
        Assert.AreEqual(2, fixture.AllySelectedSkillStatusEffectCount);

        fixture.ForceContestedConnection();
        fixture.ClickBattleButton();
        yield return fixture.WaitUntilCombatSettles();

        Assert.AreEqual(2, fixture.EnemyBurnStacks);
    }
}
