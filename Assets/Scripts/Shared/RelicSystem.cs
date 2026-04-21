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

                case Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent:
                    if (context.trigger != Relic_SO.RelicTrigger.OnMerchantPriceCalculated || context.merchantOffer == null)
                        continue;

                    int discountPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
                    if (discountPercent <= 0)
                        continue;

                    context.merchantDiscountPercent += discountPercent;
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
            }
        }
    }
}
