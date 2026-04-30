using NUnit.Framework;

public class BattleLivingCountServiceTests
{
    [Test]
    public void CountLiving_ReturnsZeroForNullCollection()
    {
        Assert.AreEqual(0, BattleLivingCountService.CountLiving<BattleAllyController>(null, ally => ally != null && !ally.IsDead));
    }
}
