using System.Collections.Generic;
using NUnit.Framework;

public class BattleCombatActionOrderingServiceTests
{
    [Test]
    public void Sort_OrdersBySpeedThenTiePriorityThenSequence()
    {
        BattleCombatAction slowEarly = Action(speed: 4, tiePriority: 0, sequence: 0);
        BattleCombatAction fastLate = Action(speed: 8, tiePriority: 1, sequence: 3);
        BattleCombatAction tiedPreferred = Action(speed: 6, tiePriority: 0, sequence: 2);
        BattleCombatAction tiedLater = Action(speed: 6, tiePriority: 1, sequence: 1);

        List<BattleCombatAction> actions = new List<BattleCombatAction>
        {
            slowEarly,
            tiedLater,
            fastLate,
            tiedPreferred
        };

        BattleCombatActionOrderingService.Sort(actions);

        CollectionAssert.AreEqual(
            new[] { fastLate, tiedPreferred, tiedLater, slowEarly },
            actions);
    }

    [Test]
    public void Compare_WhenAllOrderingFieldsMatch_ReturnsZero()
    {
        BattleCombatAction first = Action(speed: 5, tiePriority: 1, sequence: 2);
        BattleCombatAction second = Action(speed: 5, tiePriority: 1, sequence: 2);

        Assert.AreEqual(0, BattleCombatActionOrderingService.Compare(first, second));
    }

    private static BattleCombatAction Action(int speed, int tiePriority, int sequence)
    {
        return new BattleCombatAction
        {
            PrioritySpeed = speed,
            TiePriority = tiePriority,
            SequenceIndex = sequence
        };
    }
}
