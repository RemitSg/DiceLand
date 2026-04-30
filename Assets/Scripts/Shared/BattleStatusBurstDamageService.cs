using System;
using UnityEngine;

public static class BattleStatusBurstDamageService
{
    public static int CalculateBurstDamage(int stacks, int damagePerStack)
    {
        return Mathf.Max(0, stacks) * Mathf.Max(0, damagePerStack);
    }
}

public static class BattleStatusBurstDispatcher
{
    public static int DispatchBurst(
        int stacks,
        int damagePerStack,
        Func<int, int> modifyDamage,
        Action<int, int> applyDamage,
        Action clearStacks,
        bool clearStacksAfterBurst,
        Action<int, int> logApplied)
    {
        int burstDamage = BattleStatusBurstDamageService.CalculateBurstDamage(stacks, damagePerStack);
        if (burstDamage <= 0)
            return 0;

        int modifiedDamage = modifyDamage != null ? modifyDamage(burstDamage) : burstDamage;
        applyDamage?.Invoke(modifiedDamage, burstDamage);

        if (clearStacksAfterBurst)
            clearStacks?.Invoke();

        logApplied?.Invoke(modifiedDamage, burstDamage);
        return modifiedDamage;
    }
}
