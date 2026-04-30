using System.Collections.Generic;
using NUnit.Framework;

public class BattleRoundStartStatusEffectServiceTests
{
    [Test]
    public void Dispatch_TicksLivingAlliesThenEnemiesAndTransitionsAfterTicks()
    {
        List<string> events = new List<string>();

        BattleRoundStartStatusEffectService.Dispatch(
            allies: new[] { "ally-a", "dead-ally", "ally-b" },
            enemies: new[] { "enemy-a", "dead-enemy" },
            shouldTickAlly: ally => ally != "dead-ally",
            tickAlly: ally => events.Add($"tick:{ally}"),
            shouldTickEnemy: enemy => enemy != "dead-enemy",
            tickEnemy: enemy => events.Add($"tick:{enemy}"),
            shouldAttemptEnemyTransition: enemy => enemy == "enemy-a",
            transitionEnemy: enemy => events.Add($"transition:{enemy}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "tick:ally-a",
                "tick:ally-b",
                "tick:enemy-a",
                "transition:enemy-a"
            },
            events);
    }

    [Test]
    public void Dispatch_AllowsNullCollectionsAndCallbacks()
    {
        Assert.DoesNotThrow(() =>
            BattleRoundStartStatusEffectService.Dispatch<string, string>(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
    }
}
