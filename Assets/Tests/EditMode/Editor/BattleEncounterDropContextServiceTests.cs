using NUnit.Framework;

public class BattleEncounterDropContextServiceTests
{
    [Test]
    public void ShouldUseEliteRelicChoice_RequiresEliteEncounter()
    {
        Assert.IsFalse(BattleEncounterDropContextService.ShouldUseEliteRelicChoice(null));
        Assert.IsFalse(BattleEncounterDropContextService.ShouldUseEliteRelicChoice(new BattleEncounterData { tileType = MapTileType.Battle }));
        Assert.IsTrue(BattleEncounterDropContextService.ShouldUseEliteRelicChoice(new BattleEncounterData { tileType = MapTileType.Elite }));
    }

    [Test]
    public void ResolveRelicChoiceCount_ClampsToAtLeastOne()
    {
        Assert.AreEqual(1, BattleEncounterDropContextService.ResolveRelicChoiceCount(0));
        Assert.AreEqual(3, BattleEncounterDropContextService.ResolveRelicChoiceCount(3));
    }
}
