using System.Collections.Generic;
using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleGrantMoneyOnAcquire(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context))
            return;

        int money = Mathf.Max(0, effect.amount * Mathf.Max(1, context.acquiredRelicAmount));
        if (money > 0)
            context.runState.AddMoney(money);
    }

    private static void HandleIncreasePartyMaxHpOnAcquire(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context))
            return;
        if (context.runState.partyMembers == null || context.runState.partyMembers.Count == 0)
            return;

        int hpBonus = Mathf.Max(0, effect.amount * Mathf.Max(1, context.acquiredRelicAmount));
        if (hpBonus <= 0)
            return;

        for (int i = 0; i < context.runState.partyMembers.Count; i++)
        {
            RunPartyMemberState member = context.runState.partyMembers[i];
            if (member == null)
                continue;

            RunPartyMemberMaxHpService.AddPreCalamityMaxHp(member, hpBonus, context.runState.activeCalamities);
        }
    }

    private static void HandleQueueNextBattleAllyStatusOnAcquire(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context))
            return;
        if (effect.statusData == null)
            return;

        int stacks = Mathf.Max(0, effect.amount * Mathf.Max(1, context.acquiredRelicAmount));
        if (stacks <= 0)
            return;

        context.runState.QueueNextBattleAllyStatusGrant(effect.statusData, stacks);
    }

    private static void HandleGrantRandomRelicsOnAcquire(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context))
            return;
        if (effect.relicTable == null)
            return;

        int grantCount = Mathf.Max(0, effect.amount * Mathf.Max(1, context.acquiredRelicAmount));
        if (grantCount <= 0)
            return;

        HashSet<Relic_SO> excludedRelics = BuildOwnedRelicSet(context.runState);
        System.Random rng = new System.Random();
        for (int i = 0; i < grantCount; i++)
        {
            Relic_SO selected = effect.relicTable.DrawRandomRelic(rng, excludedRelics);
            if (selected == null)
                break;

            excludedRelics.Add(selected);
            context.runState.AddRelic(selected, 1);
        }
    }

    private static bool IsAcquiredRelicEffectActive(Relic_SO relicData, RelicExecutionContext context)
    {
        return context != null
            && context.trigger == Relic_SO.RelicTrigger.OnRelicAcquired
            && context.runState != null
            && relicData != null
            && context.acquiredRelicData == relicData;
    }

    private static HashSet<Relic_SO> BuildOwnedRelicSet(RunState runState)
    {
        HashSet<Relic_SO> owned = new HashSet<Relic_SO>();
        if (runState == null || runState.relicInventory == null)
            return owned;

        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState state = runState.relicInventory[i];
            if (state != null && state.relicData != null && state.amount > 0)
                owned.Add(state.relicData);
        }

        return owned;
    }
}
