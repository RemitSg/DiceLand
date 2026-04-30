using NUnit.Framework;

public class BattleEncounterLootUtilityTests
{
    [Test]
    public void ResolveRngSeed_UsesCurrentEncounterSalt()
    {
        Assert.AreEqual(123 ^ 0x51A7, BattleEncounterLootUtility.ResolveRngSeed(123));
    }

    [Test]
    public void HasOverrideLootTables_RequiresNonEmptyList()
    {
        Assert.IsFalse(BattleEncounterLootUtility.HasOverrideLootTables(null));
        Assert.IsFalse(BattleEncounterLootUtility.HasOverrideLootTables(new System.Collections.Generic.List<LootTable_SO>()));
        Assert.IsTrue(BattleEncounterLootUtility.HasOverrideLootTables(new System.Collections.Generic.List<LootTable_SO> { null }));
    }
}
