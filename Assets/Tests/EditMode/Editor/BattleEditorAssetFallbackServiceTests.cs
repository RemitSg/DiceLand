using NUnit.Framework;

public class BattleEditorAssetFallbackServiceTests
{
    [Test]
    public void FindRelicTablesByName_ReturnsEmptyListForBlankName()
    {
        Assert.IsNotNull(BattleEditorAssetFallbackService.FindRelicTablesByName(""));
        Assert.AreEqual(0, BattleEditorAssetFallbackService.FindRelicTablesByName("").Count);
    }

    [Test]
    public void FindAllRelics_ReturnsList()
    {
        Assert.IsNotNull(BattleEditorAssetFallbackService.FindAllRelics());
    }
}
