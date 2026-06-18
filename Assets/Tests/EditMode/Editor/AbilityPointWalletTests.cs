using System.IO;
using NUnit.Framework;

public class AbilityPointWalletTests
{
    private string testPath;

    [SetUp]
    public void SetUp()
    {
        testPath = Path.Combine(Path.GetTempPath(), $"ability-wallet-{System.Guid.NewGuid():N}.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(testPath))
            File.Delete(testPath);
    }

    [Test]
    public void GetCurrentPoints_ReturnsZeroWhenFileDoesNotExist()
    {
        AbilityPointWallet wallet = new AbilityPointWallet(testPath);

        Assert.AreEqual(0, wallet.GetCurrentPoints());
    }

    [Test]
    public void AddPoints_PersistsNewTotal()
    {
        AbilityPointWallet wallet = new AbilityPointWallet(testPath);

        Assert.AreEqual(5, wallet.AddPoints(5));
        Assert.AreEqual(8, wallet.AddPoints(3));

        AbilityPointWallet reloaded = new AbilityPointWallet(testPath);
        Assert.AreEqual(8, reloaded.GetCurrentPoints());
    }

    [Test]
    public void SetPoints_ClampsNegativeValuesToZero()
    {
        AbilityPointWallet wallet = new AbilityPointWallet(testPath);

        wallet.SetPoints(-10);

        Assert.AreEqual(0, wallet.GetCurrentPoints());
    }

    [Test]
    public void TrySpendPoints_DeductsOnlyWhenEnoughPointsExist()
    {
        AbilityPointWallet wallet = new AbilityPointWallet(testPath);
        wallet.SetPoints(5);

        Assert.IsTrue(wallet.TrySpendPoints(3, out int remainingAfterSuccess));
        Assert.AreEqual(2, remainingAfterSuccess);
        Assert.AreEqual(2, wallet.GetCurrentPoints());

        Assert.IsFalse(wallet.TrySpendPoints(3, out int remainingAfterFailure));
        Assert.AreEqual(2, remainingAfterFailure);
        Assert.AreEqual(2, wallet.GetCurrentPoints());
    }

    [Test]
    public void Clear_RemovesStoredPoints()
    {
        AbilityPointWallet wallet = new AbilityPointWallet(testPath);
        wallet.SetPoints(12);

        wallet.Clear();

        Assert.AreEqual(0, wallet.GetCurrentPoints());
        Assert.IsFalse(File.Exists(testPath));
    }
}
