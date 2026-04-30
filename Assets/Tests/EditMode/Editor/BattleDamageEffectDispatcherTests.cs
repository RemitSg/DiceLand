using NUnit.Framework;

public class BattleDamageEffectDispatcherTests
{
    [Test]
    public void DispatchDamage_AppliesModifiedDamageAndLogs()
    {
        int appliedModified = 0;
        int appliedOriginal = 0;
        int loggedDamage = 0;

        int result = BattleDamageEffectDispatcher.DispatchDamage(
            baseDamage: 4,
            modifyDamage: damage => damage + 2,
            applyDamage: (modified, original) =>
            {
                appliedModified = modified;
                appliedOriginal = original;
            },
            logApplied: modified => loggedDamage = modified);

        Assert.AreEqual(6, result);
        Assert.AreEqual(6, appliedModified);
        Assert.AreEqual(4, appliedOriginal);
        Assert.AreEqual(6, loggedDamage);
    }

    [Test]
    public void DispatchDamage_SkipsCallbacksWhenDamageIsZero()
    {
        int callbackCount = 0;

        int result = BattleDamageEffectDispatcher.DispatchDamage(
            baseDamage: 0,
            modifyDamage: damage => damage + 1,
            applyDamage: (_, _) => callbackCount++,
            logApplied: _ => callbackCount++);

        Assert.AreEqual(0, result);
        Assert.AreEqual(0, callbackCount);
    }
}
