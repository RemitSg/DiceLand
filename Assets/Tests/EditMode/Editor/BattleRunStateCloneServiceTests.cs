using NUnit.Framework;

public class BattleRunStateCloneServiceTests
{
    [Test]
    public void CloneLootStates_SkipsNullsAndClonesValues()
    {
        RunLootState loot = new RunLootState { amount = 2 };
        System.Collections.Generic.List<RunLootState> clone = BattleRunStateCloneService.CloneLootStates(
            new System.Collections.Generic.List<RunLootState> { null, loot });

        Assert.AreEqual(1, clone.Count);
        Assert.AreNotSame(loot, clone[0]);
        Assert.AreEqual(2, clone[0].amount);
    }

    [Test]
    public void CloneRelicStates_SkipsNullsAndClonesValues()
    {
        RunRelicState relic = new RunRelicState { amount = 3 };
        System.Collections.Generic.List<RunRelicState> clone = BattleRunStateCloneService.CloneRelicStates(
            new System.Collections.Generic.List<RunRelicState> { null, relic });

        Assert.AreEqual(1, clone.Count);
        Assert.AreNotSame(relic, clone[0]);
        Assert.AreEqual(3, clone[0].amount);
    }
}
