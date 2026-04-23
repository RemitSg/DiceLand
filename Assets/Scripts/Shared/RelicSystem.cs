using System.Collections.Generic;
using UnityEngine;

public static class RelicSystem
{
    public static int CalculateMerchantOfferPrice(RunState runState, MerchantOfferState offer)
    {
        return CalculateMerchantOfferPrice(runState, null, offer);
    }

    public static int CalculateMerchantOfferPrice(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        int basePrice = Mathf.Max(0, offer != null ? offer.price : 0);
        if (runState == null || offer == null)
            return basePrice;

        RelicExecutionContext context = RelicExecutionContext.CreateMerchantPriceCalculation(runState, tile, offer, basePrice);
        Process(runState, Relic_SO.RelicTrigger.OnMerchantPriceCalculated, context);

        float multiplier = Mathf.Max(0f, 1f - (Mathf.Max(0, context.merchantDiscountPercent) / 100f));
        multiplier *= Mathf.Max(0f, context.merchantPriceMultiplier);
        return Mathf.Max(0, Mathf.FloorToInt((basePrice * multiplier) + 0.5f));
    }

    public static void NotifyMerchantPurchaseCompleted(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        if (runState == null || tile == null || offer == null)
            return;

        RelicExecutionContext context = RelicExecutionContext.CreateMerchantPurchaseCompleted(runState, tile, offer);
        Process(runState, Relic_SO.RelicTrigger.OnMerchantPurchaseCompleted, context);
    }

    public static void ResetMerchantVisitState(BoardTileData tile)
    {
        if (tile == null)
            return;

        tile.merchantPurchaseDiscountStacks = 0;
    }

    public static int CalculateRestReforgeLimit(RunState runState, int baseLimit)
    {
        int resolvedBaseLimit = Mathf.Max(0, baseLimit);
        if (runState == null)
            return resolvedBaseLimit;

        RelicExecutionContext context = RelicExecutionContext.CreateRestReforgeLimitCalculation(runState, resolvedBaseLimit);
        Process(runState, Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated, context);
        return Mathf.Max(0, context.baseRestReforgeCount + context.additionalRestReforgeCount);
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

                case Relic_SO.RelicBuiltInEffectType.IgnoreMoveCostWhenDestinationVisited:
                    if (context.trigger != Relic_SO.RelicTrigger.OnMoveCostAboutToSpend)
                        continue;
                    if (context.isDestinationUnvisited)
                        continue;

                    context.ignoreMoveCost = true;
                    if (context.feedbackMessages != null)
                        context.feedbackMessages.Add($"{relicData.DisplayName} 生效：前往已探索过的格子不消耗行动点。");
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

                case Relic_SO.RelicBuiltInEffectType.ReduceIncomingSingleAttackDamagePercentWhenAttackerSlowerThanTarget:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeSingleAttackDamage)
                        continue;

                    BattleAllyController percentReductionTarget = context.target as BattleAllyController;
                    BattleEnemyController percentReductionSource = context.source as BattleEnemyController;
                    if (percentReductionTarget == null || percentReductionSource == null)
                        continue;

                    if (context.incomingSourceSpeed >= context.incomingTargetSpeed)
                        continue;

                    int reductionPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (reductionPercent <= 0)
                        continue;

                    context.incomingDamagePercentModifier += reductionPercent;
                    if (context.feedbackMessages != null)
                    {
                        context.feedbackMessages.Add(
                            $"{relicData.DisplayName} 生效：{percentReductionSource.GetDebugDisplayName()} 速度低于 {percentReductionTarget.GetDebugDisplayName()}，本次即将到来的伤害 -{reductionPercent}% 。");
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.ReduceIncomingAttackDamagePercentWhenAttackerSlowerThanTarget:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeAttackDamage)
                        continue;

                    BattleAllyController attackReductionTarget = context.target as BattleAllyController;
                    BattleEnemyController attackReductionSource = context.source as BattleEnemyController;
                    if (attackReductionTarget == null || attackReductionSource == null)
                        continue;

                    if (context.incomingSourceSpeed >= context.incomingTargetSpeed)
                        continue;

                    int attackReductionPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (attackReductionPercent <= 0)
                        continue;

                    context.incomingDamagePercentModifier += attackReductionPercent;
                    if (context.feedbackMessages != null)
                    {
                        context.feedbackMessages.Add(
                            $"{relicData.DisplayName} 生效：{attackReductionSource.GetDebugDisplayName()} 速度低于 {attackReductionTarget.GetDebugDisplayName()}，本次即将到来的伤害 -{attackReductionPercent}%。");
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.ReduceIncomingDamagePercentWhenSpeedGreaterThan:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeDamage)
                        continue;

                    BattleAllyController speedProtectedAlly = context.target as BattleAllyController;
                    if (speedProtectedAlly == null)
                        continue;

                    if (context.currentSpeed <= Mathf.Max(0, effect.requiredSpeed))
                        continue;

                    int generalReductionPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (generalReductionPercent <= 0)
                        continue;

                    context.incomingDamagePercentModifier += generalReductionPercent;
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

                case Relic_SO.RelicBuiltInEffectType.ModifyPlayerClashDiceWhenFasterThanTarget:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer || !context.isClashPreparation)
                        continue;

                    BattleAllyController fasterSourceAlly = context.source as BattleAllyController;
                    BattleEnemyController slowerTargetEnemy = context.target as BattleEnemyController;
                    if (fasterSourceAlly == null || slowerTargetEnemy == null)
                        continue;

                    if (fasterSourceAlly.Speed <= slowerTargetEnemy.Speed)
                        continue;

                    int clashBonus = effect.powerAmount * Mathf.Max(1, relicState.amount);
                    if (clashBonus <= 0)
                        continue;

                    context.additionalPowerBonus += clashBonus;
                    break;

                case Relic_SO.RelicBuiltInEffectType.ReduceTargetClashMinPowerWhenTargetHasStatus:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer || !context.isClashPreparation)
                        continue;

                    BattleEnemyController statusTargetEnemy = context.target as BattleEnemyController;
                    if (statusTargetEnemy == null || effect.conditionStatusData == null)
                        continue;

                    int requiredTargetStatusStacks = Mathf.Max(1, effect.requiredStatusStacks);
                    if (statusTargetEnemy.GetStatusStacks(effect.conditionStatusData) < requiredTargetStatusStacks)
                        continue;

                    int targetMinReduction = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (targetMinReduction <= 0)
                        continue;

                    context.additionalTargetMinPowerBonus -= targetMinReduction;
                    break;

                case Relic_SO.RelicBuiltInEffectType.PreserveRandomRelicMaterialOnReforge:
                    if (context.trigger != Relic_SO.RelicTrigger.OnRelicReforge)
                        continue;
                    if (context.firstReforgeRelicMaterial == null || context.secondReforgeRelicMaterial == null)
                        continue;
                    if (context.preservedReforgeMaterialSlot >= 0)
                        continue;

                    context.preservedReforgeMaterialSlot = context.firstReforgeRelicMaterial == context.secondReforgeRelicMaterial
                        ? 0
                        : Random.Range(0, 2);

                    if (context.feedbackMessages != null)
                    {
                        Relic_SO preservedRelic = context.preservedReforgeMaterialSlot == 0
                            ? context.firstReforgeRelicMaterial
                            : context.secondReforgeRelicMaterial;

                        if (preservedRelic != null)
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：遗物锻造时保留了 {preservedRelic.DisplayName}。");
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.GrantRandomLootOfSameRarityOnLootReforge:
                    if (context.trigger != Relic_SO.RelicTrigger.OnLootReforge)
                        continue;
                    if (context.reforgeResultLoot == null || context.lootReforgePool == null || context.lootReforgePool.Count == 0)
                        continue;

                    int bonusLootCount = Mathf.Max(1, effect.amount * Mathf.Max(1, relicState.amount));
                    List<LootItem_SO> sameRarityCandidates = new List<LootItem_SO>();
                    for (int lootIndex = 0; lootIndex < context.lootReforgePool.Count; lootIndex++)
                    {
                        LootItem_SO candidate = context.lootReforgePool[lootIndex];
                        if (candidate == null || candidate.rarity != context.reforgeResultLoot.rarity)
                            continue;

                        sameRarityCandidates.Add(candidate);
                    }

                    if (sameRarityCandidates.Count <= 0)
                        continue;

                    if (context.bonusLootReforgeRewards == null)
                        context.bonusLootReforgeRewards = new List<LootItem_SO>();

                    for (int rewardIndex = 0; rewardIndex < bonusLootCount; rewardIndex++)
                    {
                        LootItem_SO bonusLoot = sameRarityCandidates[Random.Range(0, sameRarityCandidates.Count)];
                        if (bonusLoot == null)
                            continue;

                        context.bonusLootReforgeRewards.Add(bonusLoot);
                        if (context.feedbackMessages != null)
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：额外获得了 {(!string.IsNullOrWhiteSpace(bonusLoot.lootName) ? bonusLoot.lootName : bonusLoot.name)}。");
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.IncreaseRestReforgeLimit:
                    if (context.trigger != Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated)
                        continue;

                    int addedReforgeCount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (addedReforgeCount <= 0)
                        continue;

                    context.additionalRestReforgeCount += addedReforgeCount;
                    break;

                case Relic_SO.RelicBuiltInEffectType.AddMerchantPurchaseDiscountStackOnPurchase:
                    if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPurchaseCompleted || context.tile == null)
                        continue;

                    int maxDiscountStacks = Mathf.Max(1, effect.maxStacks);
                    context.tile.merchantPurchaseDiscountStacks = Mathf.Min(
                        maxDiscountStacks,
                        Mathf.Max(0, context.tile.merchantPurchaseDiscountStacks) + Mathf.Max(1, relicState.amount));
                    break;

                case Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercentPerPurchaseStack:
                    if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPriceCalculated || context.tile == null)
                        continue;

                    int purchaseDiscountStacks = Mathf.Clamp(
                        Mathf.Max(0, context.tile.merchantPurchaseDiscountStacks),
                        0,
                        Mathf.Max(1, effect.maxStacks));
                    if (purchaseDiscountStacks <= 0)
                        continue;

                    int extraDiscountPercent = Mathf.Max(0, effect.amount) * purchaseDiscountStacks * Mathf.Max(1, relicState.amount);
                    if (extraDiscountPercent <= 0)
                        continue;

                    context.merchantPriceMultiplier *= Mathf.Max(0f, 1f - (extraDiscountPercent / 100f));
                    break;

                case Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent:
                    if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPriceCalculated || context.merchantOffer == null)
                        continue;

                    int discountPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (discountPercent <= 0)
                        continue;

                    context.merchantDiscountPercent += discountPercent;
                    break;

                case Relic_SO.RelicBuiltInEffectType.HealAllAlliesOnEnemyDeath:
                    if (context.trigger != Relic_SO.RelicTrigger.OnEnemyDeath || context.allies == null || context.allies.Length == 0)
                        continue;

                    int healAllAmount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (healAllAmount <= 0)
                        continue;

                    int healedCount = 0;
                    for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
                    {
                        BattleAllyController allyTarget = context.allies[allyIndex];
                        if (allyTarget == null || allyTarget.IsDead)
                            continue;

                        if (allyTarget.Heal(healAllAmount) > 0)
                            healedCount += 1;
                    }

                    if (healedCount > 0 && context.feedbackMessages != null)
                        context.feedbackMessages.Add($"{relicData.DisplayName} 生效：敌人倒下后，{healedCount} 名友方各回复 {healAllAmount} 点生命。");
                    break;

                case Relic_SO.RelicBuiltInEffectType.ApplyStatusToPlayerWhenSpeedEquals:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerRoundStart || !context.sourceIsPlayer)
                        continue;

                    BattleAllyController roundStartAlly = context.source as BattleAllyController;
                    if (roundStartAlly == null || effect.statusData == null)
                        continue;

                    int speedMin = effect.requiredSpeedMin;
                    int speedMax = effect.requiredSpeedMax;
                    if (speedMax < speedMin || (speedMin == 1 && speedMax == 1 && effect.requiredSpeed != 1))
                    {
                        int fallbackSpeed = Mathf.Max(0, effect.requiredSpeed);
                        speedMin = fallbackSpeed;
                        speedMax = fallbackSpeed;
                    }

                    if (context.currentSpeed < Mathf.Max(0, speedMin) || context.currentSpeed > Mathf.Max(speedMin, speedMax))
                        continue;

                    int stacksToApply = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (stacksToApply <= 0)
                        continue;

                    bool applied;
                    if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
                    {
                        int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
                        applied = roundStartAlly.QueueStatusForRoundStart(
                            effect.statusData,
                            effect.statusData.StatusId,
                            effect.statusData.DisplayName,
                            stacksToApply,
                            delayRounds,
                            1);

                        if (applied && context.feedbackMessages != null)
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{roundStartAlly.GetDebugDisplayName()} 将于下 {delayRounds} 个回合开始时获得 {stacksToApply} 层{effect.statusData.DisplayName}。");
                    }
                    else
                    {
                        applied = roundStartAlly.AddStatus(
                            effect.statusData,
                            effect.statusData.StatusId,
                            effect.statusData.DisplayName,
                            stacksToApply);

                        if (applied && context.feedbackMessages != null)
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{roundStartAlly.GetDebugDisplayName()} 获得 {stacksToApply} 层{effect.statusData.DisplayName}。");
                    }

                    break;


                case Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllEnemiesOnSpecificRound:
                    if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
                        continue;
                    if (effect.statusData == null || context.enemies == null || context.enemies.Length == 0)
                        continue;

                    int requiredRoundMin = Mathf.Max(1, effect.requiredRoundMin);
                    int requiredRoundMax = Mathf.Max(requiredRoundMin, effect.requiredRoundMax);
                    if (requiredRoundMin == 1 && requiredRoundMax == 1 && effect.requiredRound != 1)
                    {
                        requiredRoundMin = Mathf.Max(1, effect.requiredRound);
                        requiredRoundMax = requiredRoundMin;
                    }

                    if (context.currentRound < requiredRoundMin || context.currentRound > requiredRoundMax)
                        continue;

                    int enemyStacksToApply = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (enemyStacksToApply <= 0)
                        continue;

                    int appliedEnemyCount = 0;
                    for (int enemyIndex = 0; enemyIndex < context.enemies.Length; enemyIndex++)
                    {
                        BattleEnemyController enemy = context.enemies[enemyIndex];
                        if (enemy == null || enemy.IsDead)
                            continue;

                        bool enemyApplied = false;
                        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
                        {
                            enemyApplied = enemy.QueueStatusForRoundStart(
                                effect.statusData,
                                effect.statusData.StatusId,
                                effect.statusData.DisplayName,
                                enemyStacksToApply,
                                Mathf.Max(1, effect.applyAfterRoundStarts),
                                1);
                        }
                        else
                        {
                            enemyApplied = enemy.AddStatus(
                                effect.statusData,
                                effect.statusData.StatusId,
                                effect.statusData.DisplayName,
                                enemyStacksToApply);
                        }

                        if (enemyApplied)
                            appliedEnemyCount += 1;
                    }

                    if (appliedEnemyCount > 0 && context.feedbackMessages != null)
                    {
                        string roundRangeText = requiredRoundMin == requiredRoundMax
                            ? $"第 {requiredRoundMin} 回合"
                            : $"第 {requiredRoundMin}-{requiredRoundMax} 回合";

                        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
                        {
                            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{roundRangeText}将使 {appliedEnemyCount} 名敌人于下 {delayRounds} 个回合开始时获得 {enemyStacksToApply} 层{effect.statusData.DisplayName}。");
                        }
                        else
                        {
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{roundRangeText}使 {appliedEnemyCount} 名敌人获得 {enemyStacksToApply} 层{effect.statusData.DisplayName}。");
                        }
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesOnSpecificRound:
                    if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
                        continue;
                    if (effect.statusData == null || context.allies == null || context.allies.Length == 0)
                        continue;

                    int allyRequiredRoundMin = Mathf.Max(1, effect.requiredRoundMin);
                    int allyRequiredRoundMax = Mathf.Max(allyRequiredRoundMin, effect.requiredRoundMax);
                    if (allyRequiredRoundMin == 1 && allyRequiredRoundMax == 1 && effect.requiredRound != 1)
                    {
                        allyRequiredRoundMin = Mathf.Max(1, effect.requiredRound);
                        allyRequiredRoundMax = allyRequiredRoundMin;
                    }

                    if (context.currentRound < allyRequiredRoundMin || context.currentRound > allyRequiredRoundMax)
                        continue;

                    int allyStacksToApply = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (allyStacksToApply <= 0)
                        continue;

                    int appliedAllyCount = 0;
                    for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
                    {
                        BattleAllyController allyTarget = context.allies[allyIndex];
                        if (allyTarget == null || allyTarget.IsDead)
                            continue;

                        bool allyApplied = false;
                        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
                        {
                            allyApplied = allyTarget.QueueStatusForRoundStart(
                                effect.statusData,
                                effect.statusData.StatusId,
                                effect.statusData.DisplayName,
                                allyStacksToApply,
                                Mathf.Max(1, effect.applyAfterRoundStarts),
                                1);
                        }
                        else
                        {
                            allyApplied = allyTarget.AddStatus(
                                effect.statusData,
                                effect.statusData.StatusId,
                                effect.statusData.DisplayName,
                                allyStacksToApply);
                        }

                        if (allyApplied)
                            appliedAllyCount += 1;
                    }

                    if (appliedAllyCount > 0 && context.feedbackMessages != null)
                    {
                        string allyRoundRangeText = allyRequiredRoundMin == allyRequiredRoundMax
                            ? $"第 {allyRequiredRoundMin} 回合"
                            : $"第 {allyRequiredRoundMin}-{allyRequiredRoundMax} 回合";

                        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
                        {
                            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{allyRoundRangeText}将使 {appliedAllyCount} 名友方于下 {delayRounds} 个回合开始时获得 {allyStacksToApply} 层{effect.statusData.DisplayName}。");
                        }
                        else
                        {
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{allyRoundRangeText}使 {appliedAllyCount} 名友方获得 {allyStacksToApply} 层{effect.statusData.DisplayName}。");
                        }
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.ApplyStatusToRandomAllyOnSpecificRound:
                    if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
                        continue;
                    if (effect.statusData == null || context.allies == null || context.allies.Length == 0)
                        continue;

                    int randomAllyRequiredRoundMin = Mathf.Max(1, effect.requiredRoundMin);
                    int randomAllyRequiredRoundMax = Mathf.Max(randomAllyRequiredRoundMin, effect.requiredRoundMax);
                    if (randomAllyRequiredRoundMin == 1 && randomAllyRequiredRoundMax == 1 && effect.requiredRound != 1)
                    {
                        randomAllyRequiredRoundMin = Mathf.Max(1, effect.requiredRound);
                        randomAllyRequiredRoundMax = randomAllyRequiredRoundMin;
                    }

                    if (context.currentRound < randomAllyRequiredRoundMin || context.currentRound > randomAllyRequiredRoundMax)
                        continue;

                    int randomAllyStacksToApply = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (randomAllyStacksToApply <= 0)
                        continue;

                    List<BattleAllyController> aliveAllies = new List<BattleAllyController>();
                    for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
                    {
                        BattleAllyController candidate = context.allies[allyIndex];
                        if (candidate != null && !candidate.IsDead)
                            aliveAllies.Add(candidate);
                    }

                    if (aliveAllies.Count <= 0)
                        continue;

                    BattleAllyController randomAlly = aliveAllies[Random.Range(0, aliveAllies.Count)];
                    bool randomAllyApplied = effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart
                        ? randomAlly.QueueStatusForRoundStart(
                            effect.statusData,
                            effect.statusData.StatusId,
                            effect.statusData.DisplayName,
                            randomAllyStacksToApply,
                            Mathf.Max(1, effect.applyAfterRoundStarts),
                            1)
                        : randomAlly.AddStatus(
                            effect.statusData,
                            effect.statusData.StatusId,
                            effect.statusData.DisplayName,
                            randomAllyStacksToApply);

                    if (randomAllyApplied && context.feedbackMessages != null)
                    {
                        string roundLabel = BuildRoundRangeLabel(randomAllyRequiredRoundMin, randomAllyRequiredRoundMax);
                        context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{roundLabel}时，{randomAlly.GetDebugDisplayName()} 获得 {randomAllyStacksToApply} 层{effect.statusData.DisplayName}。");
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.ApplyStatusToRandomEnemyOnSpecificRound:
                    if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
                        continue;
                    if (effect.statusData == null || context.enemies == null || context.enemies.Length == 0)
                        continue;

                    int randomEnemyRequiredRoundMin = Mathf.Max(1, effect.requiredRoundMin);
                    int randomEnemyRequiredRoundMax = Mathf.Max(randomEnemyRequiredRoundMin, effect.requiredRoundMax);
                    if (randomEnemyRequiredRoundMin == 1 && randomEnemyRequiredRoundMax == 1 && effect.requiredRound != 1)
                    {
                        randomEnemyRequiredRoundMin = Mathf.Max(1, effect.requiredRound);
                        randomEnemyRequiredRoundMax = randomEnemyRequiredRoundMin;
                    }

                    if (context.currentRound < randomEnemyRequiredRoundMin || context.currentRound > randomEnemyRequiredRoundMax)
                        continue;

                    int randomEnemyStacksToApply = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (randomEnemyStacksToApply <= 0)
                        continue;

                    List<BattleEnemyController> aliveEnemies = new List<BattleEnemyController>();
                    for (int enemyIndex = 0; enemyIndex < context.enemies.Length; enemyIndex++)
                    {
                        BattleEnemyController candidate = context.enemies[enemyIndex];
                        if (candidate != null && !candidate.IsDead)
                            aliveEnemies.Add(candidate);
                    }

                    if (aliveEnemies.Count <= 0)
                        continue;

                    BattleEnemyController randomEnemy = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
                    bool randomEnemyApplied = effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart
                        ? randomEnemy.QueueStatusForRoundStart(
                            effect.statusData,
                            effect.statusData.StatusId,
                            effect.statusData.DisplayName,
                            randomEnemyStacksToApply,
                            Mathf.Max(1, effect.applyAfterRoundStarts),
                            1)
                        : randomEnemy.AddStatus(
                            effect.statusData,
                            effect.statusData.StatusId,
                            effect.statusData.DisplayName,
                            randomEnemyStacksToApply);

                    if (randomEnemyApplied && context.feedbackMessages != null)
                    {
                        string roundLabel = BuildRoundRangeLabel(randomEnemyRequiredRoundMin, randomEnemyRequiredRoundMax);
                        context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{roundLabel}时，{randomEnemy.GetDebugDisplayName()} 获得 {randomEnemyStacksToApply} 层{effect.statusData.DisplayName}。");
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.ApplyStatusToRemainingAlliesOnPlayerDeath:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDeath)
                        continue;
                    if (effect.statusData == null || context.allies == null || context.allies.Length == 0)
                        continue;

                    BattleAllyController defeatedAlly = context.target as BattleAllyController;
                    int allyDeathStacksToApply = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (allyDeathStacksToApply <= 0)
                        continue;

                    int affectedAllyCount = 0;
                    for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
                    {
                        BattleAllyController allyTarget = context.allies[allyIndex];
                        if (allyTarget == null || allyTarget.IsDead || allyTarget == defeatedAlly)
                            continue;

                        bool allyApplied = false;
                        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
                        {
                            allyApplied = allyTarget.QueueStatusForRoundStart(
                                effect.statusData,
                                effect.statusData.StatusId,
                                effect.statusData.DisplayName,
                                allyDeathStacksToApply,
                                Mathf.Max(1, effect.applyAfterRoundStarts),
                                1);
                        }
                        else
                        {
                            allyApplied = allyTarget.AddStatus(
                                effect.statusData,
                                effect.statusData.StatusId,
                                effect.statusData.DisplayName,
                                allyDeathStacksToApply);
                        }

                        if (allyApplied)
                            affectedAllyCount += 1;
                    }

                    if (affectedAllyCount > 0 && context.feedbackMessages != null)
                    {
                        string defeatedName = defeatedAlly != null ? defeatedAlly.GetDebugDisplayName() : "友方角色";
                        if (effect.statusApplyTiming == Relic_SO.RelicStatusApplyTiming.NextRoundStart)
                        {
                            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{defeatedName} 死亡后，其余 {affectedAllyCount} 名友方将于下 {delayRounds} 个回合开始时获得 {allyDeathStacksToApply} 层{effect.statusData.DisplayName}。");
                        }
                        else
                        {
                            context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{defeatedName} 死亡后，其余 {affectedAllyCount} 名友方获得 {allyDeathStacksToApply} 层{effect.statusData.DisplayName}。");
                        }
                    }
                    break;

                case Relic_SO.RelicBuiltInEffectType.HealRandomDamagedAllyOnBattleRoundEnd:
                    if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundEnd)
                        continue;
                    if (context.allies == null || context.allies.Length == 0)
                        continue;

                    int healAmount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (healAmount <= 0)
                        continue;

                    List<BattleAllyController> damagedAllies = new List<BattleAllyController>();
                    for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
                    {
                        BattleAllyController allyTarget = context.allies[allyIndex];
                        if (allyTarget == null || allyTarget.IsDead || allyTarget.CurrentHP >= allyTarget.MaxHP)
                            continue;

                        damagedAllies.Add(allyTarget);
                    }

                    if (damagedAllies.Count <= 0)
                        continue;

                    BattleAllyController selectedAlly = damagedAllies[Random.Range(0, damagedAllies.Count)];
                    int actualHeal = selectedAlly.Heal(healAmount);
                    if (actualHeal <= 0)
                        continue;

                    if (context.feedbackMessages != null)
                        context.feedbackMessages.Add($"{relicData.DisplayName} 生效：回合结束时，{selectedAlly.GetDebugDisplayName()} 恢复 {actualHeal} 点生命。");
                    break;

                case Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentWhenTargetHasDistinctStatuses:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage)
                        continue;

                    BattleEnemyController outgoingPercentTarget = context.target as BattleEnemyController;
                    if (outgoingPercentTarget == null)
                        continue;

                    if (CountDistinctStatuses(outgoingPercentTarget.StatusEntries) < Mathf.Max(1, effect.requiredDistinctStatusCount))
                        continue;

                    int outgoingPercentBonus = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (outgoingPercentBonus <= 0)
                        continue;

                    context.outgoingDamagePercentModifier += outgoingPercentBonus;
                    break;

                case Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamageFlatWhenTargetHasStatus:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage)
                        continue;

                    BattleEnemyController outgoingFlatTarget = context.target as BattleEnemyController;
                    if (outgoingFlatTarget == null || effect.conditionStatusData == null)
                        continue;

                    if (outgoingFlatTarget.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
                        continue;

                    int outgoingFlatBonus = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (outgoingFlatBonus <= 0)
                        continue;

                    context.outgoingDamageFlatModifier += outgoingFlatBonus;
                    break;

                case Relic_SO.RelicBuiltInEffectType.SurviveLethalDamageOncePerBattleAndApplyStatus:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage)
                        continue;

                    BattleAllyController lethalTarget = context.target as BattleAllyController;
                    if (lethalTarget == null)
                        continue;

                    string relicId = !string.IsNullOrWhiteSpace(relicData.relicId) ? relicData.relicId : relicData.name;
                    if (lethalTarget.HasConsumedLethalProtection(relicId))
                        continue;

                    if (!lethalTarget.TryConsumeLethalProtection(relicId))
                        continue;

                    context.preventLethalDamage = true;
                    context.minimumSurvivingHp = Mathf.Max(1, context.minimumSurvivingHp);

                    int lethalStatusStacks = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (effect.statusData != null && lethalStatusStacks > 0)
                    {
                        lethalTarget.AddStatus(
                            effect.statusData,
                            effect.statusData.StatusId,
                            effect.statusData.DisplayName,
                            lethalStatusStacks);
                    }

                    if (context.feedbackMessages != null)
                        context.feedbackMessages.Add($"{relicData.DisplayName} 生效：{lethalTarget.GetDebugDisplayName()} 以 {context.minimumSurvivingHp} 点生命幸存。");
                    break;

                case Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSkillUsedConsecutively:
                    if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer)
                        continue;

                    BattleAllyController skillChainAlly = context.source as BattleAllyController;
                    if (skillChainAlly == null || context.skillData == null)
                        continue;

                    if (skillChainAlly.GetCompletedConsecutiveSkillUses(context.skillData) < Mathf.Max(1, effect.requiredConsecutiveSkillUses))
                        continue;

                    int skillChainPowerBonus = Mathf.Max(0, effect.powerAmount * Mathf.Max(1, relicState.amount));
                    if (skillChainPowerBonus <= 0)
                        continue;

                    context.additionalPowerBonus += skillChainPowerBonus;
                    skillChainAlly.MarkConsecutiveSkillResetAfterCurrentUse(context.skillData);
                    break;
            }
        }
    }

    private static string BuildRoundRangeLabel(int minRound, int maxRound)
    {
        return minRound == maxRound
            ? $"第 {minRound} 回合"
            : $"第 {minRound}-{maxRound} 回合";
    }

    private static int CountDistinctStatuses(IReadOnlyList<BattleStatusRuntimeEntry> statusEntries)
    {
        if (statusEntries == null || statusEntries.Count == 0)
            return 0;

        int count = 0;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0)
                continue;

            string key = entry.Key;
            if (string.IsNullOrWhiteSpace(key))
                continue;

            count += 1;
        }

        return count;
    }
}
