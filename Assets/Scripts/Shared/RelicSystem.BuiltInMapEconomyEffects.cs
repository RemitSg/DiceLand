using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleChanceToIgnoreMoveCost(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMoveCostAboutToSpend)
            return;
        if (effect.requireUnvisitedTile && !context.isDestinationUnvisited)
            return;

        context.ignoreMoveCost = true;
        AddRelicFeedback(context, relicData, "本次探索不消耗行动点。");
        return;
    }

    private static void HandleIgnoreMoveCostWhenDestinationVisited(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMoveCostAboutToSpend)
            return;
        if (context.isDestinationUnvisited)
            return;

        context.ignoreMoveCost = true;
        AddRelicFeedback(context, relicData, "前往已探索过的格子不消耗行动点。");
        return;
    }

    private static void HandleAddMerchantPurchaseDiscountStackOnPurchase(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPurchaseCompleted || context.tile == null)
            return;

        int maxDiscountStacks = Mathf.Max(1, effect.maxStacks);
        context.tile.merchantPurchaseDiscountStacks = Mathf.Min(
            maxDiscountStacks,
            Mathf.Max(0, context.tile.merchantPurchaseDiscountStacks) + Mathf.Max(1, relicState.amount));
        return;
    }

    private static void HandleReduceMerchantPricePercentPerPurchaseStack(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPriceCalculated || context.tile == null)
            return;

        int purchaseDiscountStacks = Mathf.Clamp(
            Mathf.Max(0, context.tile.merchantPurchaseDiscountStacks),
            0,
            Mathf.Max(1, effect.maxStacks));
        if (purchaseDiscountStacks <= 0)
            return;

        int extraDiscountPercent = Mathf.Max(0, effect.amount) * purchaseDiscountStacks * Mathf.Max(1, relicState.amount);
        if (extraDiscountPercent <= 0)
            return;

        context.merchantPriceMultiplier *= Mathf.Max(0f, 1f - (extraDiscountPercent / 100f));
        return;
    }

    private static void HandleReduceMerchantPricePercent(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPriceCalculated || context.merchantOffer == null)
            return;

        int discountPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (discountPercent <= 0)
            return;

        context.merchantDiscountPercent += discountPercent;
        return;
    }

    private static void HandleSetMerchantInitialStockCount(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMerchantStockCountCalculated)
            return;

        int targetCount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (targetCount > context.merchantStockTargetCount)
            context.merchantStockTargetCount = targetCount;
    }

    private static void HandleAllowMerchantPurchaseWithPositiveMoney(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPurchaseAffordabilityChecked)
            return;
        if (context.runState == null || context.runState.money <= 0)
            return;

        context.allowMerchantPurchaseWithPositiveMoney = true;
    }

    private static void HandleHealPartyPercentOnEnteringUnvisitedMerchantTile(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMoveCostAboutToSpend)
            return;
        if (!context.isDestinationUnvisited || context.tile == null || context.tile.tileType != MapTileType.Merchant)
            return;
        if (context.runState == null || context.runState.partyMembers == null || context.runState.partyMembers.Count == 0)
            return;

        int healPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (healPercent <= 0)
            return;

        int totalHealed = 0;
        for (int memberIndex = 0; memberIndex < context.runState.partyMembers.Count; memberIndex++)
        {
            RunPartyMemberState member = context.runState.partyMembers[memberIndex];
            if (member == null || member.maxHP <= 0 || member.currentHP <= 0 || member.currentHP >= member.maxHP)
                continue;

            int partyHealAmount = Mathf.Max(1, Mathf.CeilToInt(member.maxHP * (healPercent / 100f)));
            int previousHp = member.currentHP;
            member.currentHP = Mathf.Clamp(member.currentHP + partyHealAmount, 0, member.maxHP);
            totalHealed += member.currentHP - previousHp;
        }

        if (totalHealed > 0)
            AddRelicFeedback(context, relicData, $"进入未探索的商店格时，队伍共恢复 {totalHealed} 点生命。");
        return;
    }

    private static void HandleRestoreActionPointsOnKeyCollectedByPercentMax(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnKeyCollected || context.actionPointSystem == null)
            return;

        int restorePercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (restorePercent <= 0)
            return;

        int restoreAmount = Mathf.Max(1, Mathf.CeilToInt(context.actionPointSystem.CurrentMaxPoints * (restorePercent / 100f)));
        int restoredPoints = Mathf.Clamp(
            context.actionPointSystem.CurrentPoints + restoreAmount,
            0,
            context.actionPointSystem.CurrentMaxPoints);
        int actualRestore = restoredPoints - context.actionPointSystem.CurrentPoints;
        if (actualRestore <= 0)
            return;

        context.actionPointSystem.RestoreState(
            context.actionPointSystem.BasePoints,
            context.actionPointSystem.BonusPoints,
            restoredPoints);

        AddRelicFeedback(context, relicData, $"收集钥匙时恢复 {actualRestore} 点行动点。");
        return;
    }

    private static void HandleGrantMoneyOnActionPointsSpent(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnActionPointsSpent || context.runState == null)
            return;

        int money = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (money > 0 && context.actionPointsSpent > 0)
            context.runState.AddMoney(money);
    }
}
