using System.Collections.Generic;
using NUnit.Framework;

public class BattleSettlementPartyMemberFactoryTests
{
    [Test]
    public void BuildRunState_UsesDisplayNameFallback()
    {
        RunPartyMemberState state = BattleSettlementPartyMemberFactory.BuildRunState(
            displayName: "",
            fallbackName: "Fallback",
            characterData: null,
            weaponData: null,
            level: 2,
            experience: 3,
            maxHP: 10,
            currentHP: 4);

        Assert.AreEqual("Fallback", state.memberId);
        Assert.AreEqual(2, state.level);
        Assert.AreEqual(4, state.currentHP);
    }

    [Test]
    public void BuildSettlementEntry_MapsInitialAndResultingValues()
    {
        BattleSettlementEntry entry = BattleSettlementPartyMemberFactory.BuildSettlementEntry(
            displayName: "A",
            initialCurrentHP: 1,
            initialMaxHP: 2,
            initialLevel: 3,
            isAlive: true,
            initialExperience: 4,
            maxExperience: 5,
            gainedExperience: 6,
            finalCurrentHP: 7,
            finalMaxHP: 8,
            finalLevel: 9,
            finalExperience: 10);

        Assert.AreEqual("A", entry.displayName);
        Assert.AreEqual(1, entry.currentHP);
        Assert.AreEqual(7, entry.resultingHP);
        Assert.AreEqual(10, entry.resultingExperience);
    }

    [Test]
    public void BuildRunStatesAndSettlementEntries_MapSharedProgressionList()
    {
        List<BattleSettlementPartyProgression> progressions = new List<BattleSettlementPartyProgression>
        {
            new BattleSettlementPartyProgression
            {
                DisplayName = "",
                FallbackName = "Fallback",
                InitialCurrentHP = 3,
                InitialMaxHP = 8,
                InitialLevel = 1,
                InitialExperience = 4,
                MaxExperience = 10,
                IsAlive = true,
                FinalCurrentHP = 5,
                FinalMaxHP = 9,
                FinalLevel = 2,
                FinalExperience = 1
            }
        };

        List<RunPartyMemberState> runStates = BattleSettlementPartyMemberFactory.BuildRunStates(progressions);
        List<BattleSettlementEntry> settlementEntries = BattleSettlementPartyMemberFactory.BuildSettlementEntries(progressions, gainedExperience: 6);

        Assert.AreEqual(1, runStates.Count);
        Assert.AreEqual("Fallback", runStates[0].memberId);
        Assert.AreEqual(2, runStates[0].level);
        Assert.AreEqual(5, runStates[0].currentHP);
        Assert.AreEqual(1, settlementEntries.Count);
        Assert.AreEqual(4, settlementEntries[0].currentExperience);
        Assert.AreEqual(6, settlementEntries[0].experienceGained);
        Assert.AreEqual(1, settlementEntries[0].resultingExperience);
    }
}
