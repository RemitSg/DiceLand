using NUnit.Framework;

public class BattleSettlementLootEntryBuilderTests
{
    [Test]
    public void Build_ReturnsEmptyForNullRewards()
    {
        System.Collections.Generic.List<BattleSettlementLootEntry> entries =
            BattleSettlementLootEntryBuilder.Build(null, null);

        Assert.IsNotNull(entries);
        Assert.IsEmpty(entries);
    }
}
