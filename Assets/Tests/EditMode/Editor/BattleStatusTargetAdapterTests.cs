using NUnit.Framework;

public class BattleStatusTargetAdapterTests
{
    [Test]
    public void GetStacks_UsesStatusDataWhenAvailable()
    {
        BattleStatus_SO status = UnityEngine.ScriptableObject.CreateInstance<BattleStatus_SO>();
        BattleStatusTargetAdapter adapter = new BattleStatusTargetAdapter(
            "Target",
            () => 10,
            () => 20,
            data => data == status ? 3 : 0,
            _ => 1,
            (_, _) => { },
            (_, _, _) => { },
            (_, _, _, _) => 0);

        Assert.AreEqual(3, adapter.GetStacks(status, "fallback"));
        UnityEngine.Object.DestroyImmediate(status);
    }

    [Test]
    public void GetStacks_UsesFallbackIdWhenStatusDataMissing()
    {
        BattleStatusTargetAdapter adapter = new BattleStatusTargetAdapter(
            "Target",
            () => 10,
            () => 20,
            _ => 0,
            id => id == "burn" ? 4 : 0,
            (_, _) => { },
            (_, _, _) => { },
            (_, _, _, _) => 0);

        Assert.AreEqual(4, adapter.GetStacks(null, "burn"));
    }
}
