using System;
using UnityEngine;

public static class BattleDamageEffectDispatcher
{
    public static int DispatchDamage(
        int baseDamage,
        Func<int, int> modifyDamage,
        Action<int, int> applyDamage,
        Action<int> logApplied)
    {
        int clampedBaseDamage = Mathf.Max(0, baseDamage);
        if (clampedBaseDamage <= 0)
            return 0;

        int modifiedDamage = Mathf.Max(0, modifyDamage != null ? modifyDamage(clampedBaseDamage) : clampedBaseDamage);
        applyDamage?.Invoke(modifiedDamage, clampedBaseDamage);
        logApplied?.Invoke(modifiedDamage);
        return modifiedDamage;
    }
}
