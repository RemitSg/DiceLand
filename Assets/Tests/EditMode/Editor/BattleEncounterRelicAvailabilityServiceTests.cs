using NUnit.Framework;

public class BattleEncounterRelicAvailabilityServiceTests
{
    [Test]
    public void HasBattleRelics_ReturnsFalseForNullEncounterOrInventory()
    {
        Assert.IsFalse(BattleEncounterRelicAvailabilityService.HasBattleRelics(null));
        Assert.IsFalse(BattleEncounterRelicAvailabilityService.HasBattleRelics(new BattleEncounterData()));
    }

    [Test]
    public void HasBattleRelics_ReturnsTrueWhenInventoryHasAnyRelic()
    {
        BattleEncounterData encounter = new BattleEncounterData();
        encounter.relicInventory = new System.Collections.Generic.List<RunRelicState> { new RunRelicState() };

        Assert.IsTrue(BattleEncounterRelicAvailabilityService.HasBattleRelics(encounter));
    }
}
