using NUnit.Framework;

public class BattleStatusResolutionServiceTests
{
    [Test]
    public void TryResolve_UsesStatusDataWhenConfigured()
    {
        BattleStatus_SO status = UnityEngine.ScriptableObject.CreateInstance<BattleStatus_SO>();
        status.name = "custom-status";

        bool resolved = BattleStatusResolutionService.TryResolve(
            status,
            EnemySkill_SO.DiceStatusType.Burn,
            out BattleResolvedStatus result);

        Assert.IsTrue(resolved);
        Assert.AreSame(status, result.StatusData);
        Assert.AreEqual(status.StatusId, result.StatusId);
        Assert.AreEqual(status.DisplayName, result.StatusName);
    }

    [Test]
    public void TryResolve_UsesBurnFallbackWhenNoStatusData()
    {
        bool resolved = BattleStatusResolutionService.TryResolve(
            null,
            EnemySkill_SO.DiceStatusType.Burn,
            out BattleResolvedStatus result);

        Assert.IsTrue(resolved);
        Assert.IsNull(result.StatusData);
        Assert.AreEqual("burn", result.StatusId);
        Assert.AreEqual("燃烧", result.StatusName);
    }

    [Test]
    public void TryResolve_ReturnsFalseForUnsupportedLegacyStatus()
    {
        bool resolved = BattleStatusResolutionService.TryResolve(
            null,
            EnemySkill_SO.DiceStatusType.Electro,
            out BattleResolvedStatus result);

        Assert.IsFalse(resolved);
        Assert.IsNull(result.StatusData);
        Assert.AreEqual(string.Empty, result.StatusId);
        Assert.AreEqual(string.Empty, result.StatusName);
    }
}
