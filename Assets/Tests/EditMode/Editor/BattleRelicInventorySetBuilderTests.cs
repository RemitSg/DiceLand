using NUnit.Framework;

public class BattleRelicInventorySetBuilderTests
{
    [Test]
    public void BuildOwnedRelicSet_ReturnsEmptyForNullInventory()
    {
        Assert.IsEmpty(BattleRelicInventorySetBuilder.BuildOwnedRelicSet(null));
    }

    [Test]
    public void BuildOwnedRelicSet_SkipsNullOrEmptyStates()
    {
        System.Collections.Generic.HashSet<Relic_SO> owned = BattleRelicInventorySetBuilder.BuildOwnedRelicSet(
            new System.Collections.Generic.List<RunRelicState>
            {
                null,
                new RunRelicState { amount = 0 }
            });

        Assert.IsEmpty(owned);
    }
}
