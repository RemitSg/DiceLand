using NUnit.Framework;

public class BattleSettlementRelicChoiceBuilderTests
{
    [Test]
    public void Build_ReturnsNullForNullOrEmptyRelics()
    {
        Assert.IsNull(BattleSettlementRelicChoiceBuilder.Build(null));
        Assert.IsNull(BattleSettlementRelicChoiceBuilder.Build(new Relic_SO[0]));
    }

    [Test]
    public void Build_SkipsNullRelics()
    {
        Assert.IsNull(BattleSettlementRelicChoiceBuilder.Build(new Relic_SO[] { null }));
    }
}
