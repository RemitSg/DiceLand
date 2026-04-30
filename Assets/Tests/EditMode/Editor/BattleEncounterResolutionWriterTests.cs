using System.Collections.Generic;
using NUnit.Framework;

public class BattleEncounterResolutionWriterTests
{
    [Test]
    public void ApplyCompletionResolution_WritesClonedRewards()
    {
        BattleEncounterData encounter = new BattleEncounterData();
        List<RunPartyMemberState> party = new List<RunPartyMemberState>
        {
            new RunPartyMemberState { memberId = "A" }
        };
        List<RunLootState> loot = new List<RunLootState>
        {
            new RunLootState { amount = 1 }
        };
        List<RunRelicState> relics = new List<RunRelicState>
        {
            new RunRelicState { amount = 1 }
        };

        BattleEncounterResolutionWriter.ApplyCompletionResolution(encounter, true, 12, 5, party, loot, relics);

        Assert.IsTrue(encounter.wasVictory);
        Assert.AreEqual(12, encounter.experienceGained);
        Assert.AreEqual(5, encounter.moneyGained);
        Assert.AreSame(party, encounter.resolvedPartyMembers);
        Assert.AreNotSame(loot, encounter.resolvedLoot);
        Assert.AreNotSame(relics, encounter.resolvedRelics);
    }

    [Test]
    public void ApplyForcedRetreatResolution_WritesOverride()
    {
        BattleEncounterData encounter = new BattleEncounterData();
        List<LootTable_SO> overrideTables = new List<LootTable_SO> { null };

        BattleEncounterResolutionWriter.ApplyForcedRetreatResolution(
            encounter,
            6,
            2,
            new List<RunPartyMemberState>(),
            new List<RunLootState>(),
            new List<RunRelicState>(),
            "Retreated",
            overrideTables);

        Assert.IsTrue(encounter.wasVictory);
        Assert.IsTrue(encounter.resolutionOverride.forceVictory);
        Assert.AreEqual("Retreated", encounter.resolutionOverride.subtitleOverride);
        Assert.AreEqual(0, encounter.resolutionOverride.lootTables.Count);
    }
}
