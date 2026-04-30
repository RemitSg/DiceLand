using NUnit.Framework;

public class BattleStatusBurstDamageServiceTests
{
    [Test]
    public void CalculateBurstDamage_MultipliesDamagePerStackByStacks()
    {
        Assert.AreEqual(6, BattleStatusBurstDamageService.CalculateBurstDamage(2, 3));
    }

    [Test]
    public void CalculateBurstDamage_ClampsNegativeInputs()
    {
        Assert.AreEqual(0, BattleStatusBurstDamageService.CalculateBurstDamage(-1, 3));
        Assert.AreEqual(0, BattleStatusBurstDamageService.CalculateBurstDamage(2, -1));
    }

    [Test]
    public void DispatchBurst_AppliesModifiedDamageClearsAndLogs()
    {
        int appliedDamage = 0;
        int originalDamage = 0;
        bool cleared = false;
        int loggedDamage = 0;
        int loggedOriginal = 0;

        int result = BattleStatusBurstDispatcher.DispatchBurst(
            stacks: 3,
            damagePerStack: 2,
            modifyDamage: damage => damage + 4,
            applyDamage: (modified, original) =>
            {
                appliedDamage = modified;
                originalDamage = original;
            },
            clearStacks: () => cleared = true,
            clearStacksAfterBurst: true,
            logApplied: (modified, original) =>
            {
                loggedDamage = modified;
                loggedOriginal = original;
            });

        Assert.AreEqual(10, result);
        Assert.AreEqual(10, appliedDamage);
        Assert.AreEqual(6, originalDamage);
        Assert.IsTrue(cleared);
        Assert.AreEqual(10, loggedDamage);
        Assert.AreEqual(6, loggedOriginal);
    }

    [Test]
    public void DispatchBurst_SkipsCallbacksWhenBaseDamageIsZero()
    {
        int callbackCount = 0;

        int result = BattleStatusBurstDispatcher.DispatchBurst(
            stacks: 0,
            damagePerStack: 2,
            modifyDamage: damage => damage + 1,
            applyDamage: (_, _) => callbackCount++,
            clearStacks: () => callbackCount++,
            clearStacksAfterBurst: true,
            logApplied: (_, _) => callbackCount++);

        Assert.AreEqual(0, result);
        Assert.AreEqual(0, callbackCount);
    }
}
