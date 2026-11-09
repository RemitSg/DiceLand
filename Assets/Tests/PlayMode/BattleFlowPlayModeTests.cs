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
    public IEnumerator RoundStart_WhenBurningStatusExists_AppliesTickBeforeTargetingInput()
    {
        fixture.BuildBattleWithEnemyRoundStartBurn(allyPower: 1, enemyPower: 1, enemyHp: 10, burnStacks: 3);

        fixture.ClickBattleButton();
        yield return null;

        Assert.AreEqual(7, fixture.EnemyCurrentHp);
        Assert.AreEqual("Targeting", fixture.CurrentBattlePhase);
    }
}
