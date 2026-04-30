using System;
using System.Collections;
using System.Collections.Generic;

public static class BattleCombatActionExecutionDispatcher
{
    public static IEnumerator Dispatch(
        IReadOnlyList<BattleCombatAction> actions,
        BattleCombatActionExecutionContext context)
    {
        if (context == null)
            yield break;

        yield return Dispatch(
            actions,
            context.ResolveClash,
            context.ResolveAllySingleAttack,
            context.ResolveEnemySingleAttack,
            context.IsBattleCompleted,
            context.OnBattleCompleted,
            context.Result);
    }

    public static IEnumerator Dispatch(
        IReadOnlyList<BattleCombatAction> actions,
        Func<BattleCombatAction, IEnumerator> resolveClash,
        Func<BattleCombatAction, IEnumerator> resolveAllySingleAttack,
        Func<BattleCombatAction, IEnumerator> resolveEnemySingleAttack,
        Func<bool> isBattleCompleted,
        Action onBattleCompleted,
        BattleCombatActionExecutionResult result = null)
    {
        if (actions == null)
            yield break;

        for (int i = 0; i < actions.Count; i++)
        {
            BattleCombatAction action = actions[i];
            if (action == null)
                continue;

            IEnumerator resolver = ResolveAction(
                action,
                resolveClash,
                resolveAllySingleAttack,
                resolveEnemySingleAttack);

            if (resolver != null)
                yield return resolver;

            if (isBattleCompleted != null && isBattleCompleted())
            {
                result?.MarkBattleCompletedDuringAction();
                onBattleCompleted?.Invoke();
                yield break;
            }
        }
    }

    private static IEnumerator ResolveAction(
        BattleCombatAction action,
        Func<BattleCombatAction, IEnumerator> resolveClash,
        Func<BattleCombatAction, IEnumerator> resolveAllySingleAttack,
        Func<BattleCombatAction, IEnumerator> resolveEnemySingleAttack)
    {
        switch (action.Type)
        {
            case BattleCombatActionType.Clash:
                return resolveClash?.Invoke(action);
            case BattleCombatActionType.AllySingleAttack:
                return resolveAllySingleAttack?.Invoke(action);
            case BattleCombatActionType.EnemySingleAttack:
                return resolveEnemySingleAttack?.Invoke(action);
            default:
                return null;
        }
    }
}

public sealed class BattleCombatActionExecutionResult
{
    public bool BattleCompletedDuringAction { get; private set; }

    public void MarkBattleCompletedDuringAction()
    {
        BattleCompletedDuringAction = true;
    }
}

public sealed class BattleCombatActionExecutionContext
{
    public readonly Func<BattleCombatAction, IEnumerator> ResolveClash;
    public readonly Func<BattleCombatAction, IEnumerator> ResolveAllySingleAttack;
    public readonly Func<BattleCombatAction, IEnumerator> ResolveEnemySingleAttack;
    public readonly Func<bool> IsBattleCompleted;
    public readonly Action OnBattleCompleted;
    public readonly BattleCombatActionExecutionResult Result;

    public BattleCombatActionExecutionContext(
        Func<BattleCombatAction, IEnumerator> resolveClash,
        Func<BattleCombatAction, IEnumerator> resolveAllySingleAttack,
        Func<BattleCombatAction, IEnumerator> resolveEnemySingleAttack,
        Func<bool> isBattleCompleted,
        Action onBattleCompleted,
        BattleCombatActionExecutionResult result = null)
    {
        ResolveClash = resolveClash;
        ResolveAllySingleAttack = resolveAllySingleAttack;
        ResolveEnemySingleAttack = resolveEnemySingleAttack;
        IsBattleCompleted = isBattleCompleted;
        OnBattleCompleted = onBattleCompleted;
        Result = result ?? new BattleCombatActionExecutionResult();
    }
}
