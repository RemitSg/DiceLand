using System.Collections.Generic;
using UnityEngine;

public static class RelicSystem
{
    public static int CalculateMerchantOfferPrice(RunState runState, MerchantOfferState offer)
    {
        int basePrice = Mathf.Max(0, offer != null ? offer.price : 0);
        if (runState == null || offer == null)
            return basePrice;

        RelicExecutionContext context = RelicExecutionContext.CreateMerchantPriceCalculation(runState, offer, basePrice);
        Process(runState, Relic_SO.RelicTrigger.OnMerchantPriceCalculated, context);

        float multiplier = Mathf.Max(0f, 1f - (Mathf.Max(0, context.merchantDiscountPercent) / 100f));
        return Mathf.Max(0, Mathf.FloorToInt((basePrice * multiplier) + 0.5f));
    }

    public static void Process(RunState runState, Relic_SO.RelicTrigger trigger, RelicExecutionContext context)
    {
        if (runState == null)
            return;

        context.trigger = trigger;
        context.runState = runState;
        Process(runState.relicInventory, context);
    }

    public static void Process(IReadOnlyList<RunRelicState> relicInventory, RelicExecutionContext context)
    {
        if (context == null || relicInventory == null || relicInventory.Count == 0)
            return;

        for (int i = 0; i < relicInventory.Count; i++)
        {
            RunRelicState relicState = relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0)
                continue;

            ApplyBuiltInEffects(relicState.relicData, relicState, context);

            if (relicState.relicData.customEffectHandler == null)
                continue;

            relicState.relicData.customEffectHandler.TryApply(relicState.relicData, relicState, context, out _);
        }
    }

    private static void ApplyBuiltInEffects(Relic_SO relicData, RunRelicState relicState, RelicExecutionContext context)
    {
        List<Relic_SO.RelicBuiltInEffectEntry> effects = relicData.builtInEffects;
        if (effects == null || effects.Count == 0)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            Relic_SO.RelicBuiltInEffectEntry effect = effects[i];
            if (effect == null || effect.trigger != context.trigger)
                continue;

            if (effect.chance < 1f && Random.value > Mathf.Clamp01(effect.chance))
                continue;

            switch (effect.effectType)
            {
                case Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost:
                    if (context.trigger != Relic_SO.RelicTrigger.OnMoveCostAboutToSpend)
                        continue;
                    if (effect.requireUnvisitedTile && !context.isDestinationUnvisited)
                        continue;

                    context.ignoreMoveCost = true;
                    if (context.feedbackMessages != null)
                        context.feedbackMessages.Add($"{relicData.DisplayName} 生效：本次探索不消耗行动点。");
                    break;

                case Relic_SO.RelicBuiltInEffectType.AddExtraStatusStacksOnApply:
                    if (context.trigger != Relic_SO.RelicTrigger.OnStatusApplied || !context.sourceIsPlayer)
                        continue;
                    if (effect.statusData != null && effect.statusData != context.statusData)
                        continue;

                    context.additionalStatusStacks += Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    break;

                case Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSelfHasStatus:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer)
                        continue;

                    BattleAllyController ally = context.source as BattleAllyController;
                    if (ally == null || effect.conditionStatusData == null)
                        continue;

                    int currentStacks = ally.GetStatusStacks(effect.conditionStatusData);
                    if (currentStacks < Mathf.Max(1, effect.requiredStatusStacks))
                        continue;

                    int relicAmount = Mathf.Max(1, relicState.amount);
                    context.additionalPowerBonus += effect.powerAmount * relicAmount;
                    context.additionalMinPowerBonus += effect.minPowerAmount * relicAmount;
                    context.additionalMaxPowerBonus += effect.maxPowerAmount * relicAmount;
                    break;

                case Relic_SO.RelicBuiltInEffectType.ReduceIncomingSingleAttackDamageFlat:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeSingleAttackDamage)
                        continue;

                    BattleAllyController targetAlly = context.target as BattleAllyController;
                    if (targetAlly == null)
                        continue;

                    int reductionAmount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (reductionAmount <= 0)
                        continue;

                    context.incomingDamageFlatModifier -= reductionAmount;
                    if (context.feedbackMessages != null)
                        context.feedbackMessages.Add($"{relicData.DisplayName} 生效：本次单方面攻击伤害 -{reductionAmount}。");
                    break;

                case Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceBasedOnLastRoundSkillMatch:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer)
                        continue;

                    BattleAllyController sourceAlly = context.source as BattleAllyController;
                    if (sourceAlly == null || context.skillData == null)
                        continue;

                    int magnitude = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (magnitude <= 0)
                        continue;

                    bool sameAsLastRound = sourceAlly.LastRoundUsedSkill != null && sourceAlly.LastRoundUsedSkill == context.skillData;
                    context.additionalPowerBonus += sameAsLastRound ? -magnitude : magnitude;
                    break;

                case Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent:
                    if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPriceCalculated || context.merchantOffer == null)
                        continue;

                    int discountPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (discountPercent <= 0)
                        continue;

                    context.merchantDiscountPercent += discountPercent;
                    break;
            }
        }
    }
}
