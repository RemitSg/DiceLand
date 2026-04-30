using NUnit.Framework;

public class BattleRelicBridgeTests
{
    [Test]
    public void Process_SkipsWhenEncounterHasNoRelics()
    {
        bool processed = false;

        bool result = BattleRelicBridge.Process(
            null,
            new RelicExecutionContext(),
            (_, _) => processed = true);

        Assert.IsFalse(result);
        Assert.IsFalse(processed);
    }

    [Test]
    public void Process_RunsWhenEncounterHasRelics()
    {
        BattleEncounterData encounter = new BattleEncounterData();
        encounter.relicInventory.Add(new RunRelicState());
        bool processed = false;

        bool result = BattleRelicBridge.Process(
            encounter,
            new RelicExecutionContext(),
            (_, _) => processed = true);

        Assert.IsTrue(result);
        Assert.IsTrue(processed);
    }
}
