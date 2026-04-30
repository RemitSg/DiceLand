using NUnit.Framework;
using UnityEngine;

public class BattleManagerResolverServiceTests
{
    [Test]
    public void TryResolve_WhenCurrentExists_ReturnsCurrentWithoutFinder()
    {
        GameObject managerObject = new GameObject("BattleManager");
        BattleManager manager = managerObject.AddComponent<BattleManager>();
        bool finderCalled = false;

        bool result = BattleManagerResolverService.TryResolve(
            manager,
            () =>
            {
                finderCalled = true;
                return null;
            },
            out BattleManager resolved,
            out string warningMessage);

        Assert.IsTrue(result);
        Assert.AreSame(manager, resolved);
        Assert.IsFalse(finderCalled);
        Assert.IsNull(warningMessage);

        Object.DestroyImmediate(managerObject);
    }

    [Test]
    public void TryResolve_WhenCurrentMissing_UsesFinder()
    {
        GameObject managerObject = new GameObject("BattleManager");
        BattleManager manager = managerObject.AddComponent<BattleManager>();

        bool result = BattleManagerResolverService.TryResolve(
            null,
            () => manager,
            out BattleManager resolved,
            out string warningMessage);

        Assert.IsTrue(result);
        Assert.AreSame(manager, resolved);
        Assert.IsNull(warningMessage);

        Object.DestroyImmediate(managerObject);
    }

    [Test]
    public void TryResolve_WhenNoManagerFound_ReturnsFalseAndWarning()
    {
        bool result = BattleManagerResolverService.TryResolve(
            null,
            () => null,
            out BattleManager resolved,
            out string warningMessage);

        Assert.IsFalse(result);
        Assert.IsNull(resolved);
        StringAssert.Contains("BattleManager", warningMessage);
    }
}
