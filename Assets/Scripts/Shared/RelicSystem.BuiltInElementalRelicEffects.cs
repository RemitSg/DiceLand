using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static partial class RelicSystem
{
    private const string BurnStatusId = "burn";

    private static void HandlePassiveMarker(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
    }

    public static int ResolveEnemyBurnRoundStartStacksAfterTick(int stacksBeforeTick, int defaultStacksAfterTick)
    {
        if (stacksBeforeTick <= 0)
            return 0;

        int resolvedStacks = Mathf.Max(0, defaultStacksAfterTick);
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter != null && encounter.relicInventory != null)
        {
            for (int i = 0; i < encounter.relicInventory.Count; i++)
            {
                RunRelicState relicState = encounter.relicInventory[i];
                if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                    continue;

                for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
                {
                    Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                    if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.SetEnemyBurnRoundStartDecayPercent)
                        continue;

                    int decayPercent = Mathf.Clamp(effect.amount, 0, 100);
                    int stacksAfterDecay = Mathf.FloorToInt(stacksBeforeTick * (1f - (decayPercent / 100f)));
                    resolvedStacks = Mathf.Max(resolvedStacks, Mathf.Max(0, stacksAfterDecay));
                }
            }
        }

        int minimumStacks = ResolveEnemyMinimumStatusStacks(BurnStatusId);
        return resolvedStacks > 0 && minimumStacks > 0 ? Mathf.Max(resolvedStacks, minimumStacks) : resolvedStacks;
    }

    public static int ResolveEnemyMinimumStatusStacks(string statusId)
    {
        if (string.IsNullOrWhiteSpace(statusId))
            return 0;

        int minimumStacks = 0;
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return 0;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.SetEnemyStatusMinimumStacks)
                    continue;
                if (!StatusMatches(effect.conditionStatusData, null, statusId))
                    continue;

                minimumStacks = Mathf.Max(minimumStacks, effect.amount * Mathf.Max(1, relicState.amount));
            }
        }

        return minimumStacks;
    }

    private static void HandleSetEnemyStatusMinimumStacks(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if ((context.trigger != Relic_SO.RelicTrigger.OnBattleSetup && context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            || effect.conditionStatusData == null
            || context.enemies == null)
        {
            return;
        }

        int minimumStacks = CalculateScaledStatusStacks(relicState, effect);
        if (minimumStacks <= 0)
            return;

        int affectedEnemies = 0;
        for (int i = 0; i < context.enemies.Length; i++)
        {
            BattleEnemyController enemy = context.enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            int currentStacks = enemy.GetStatusStacks(effect.conditionStatusData);
            if (currentStacks >= minimumStacks)
                continue;

            int stacksToApply = minimumStacks - Mathf.Max(0, currentStacks);
            if (enemy.AddStatus(effect.conditionStatusData, effect.conditionStatusData.StatusId, effect.conditionStatusData.DisplayName, stacksToApply))
                affectedEnemies++;
        }

        if (affectedEnemies > 0)
            AddRelicFeedback(context, relicData, $"{affectedEnemies} 名敌人的 {GetStatusDisplayName(effect.conditionStatusData)} 被维持到至少 {minimumStacks} 层。");
    }

    public static int ResolveEnemyRoundStartStatusDamage(
        BattleEnemyController target,
        BattleStatus_SO statusData,
        string statusId,
        int stacks,
        int defaultDamage)
    {
        int resolvedDamage = Mathf.Max(0, defaultDamage);
        if (target == null || stacks <= 0)
            return resolvedDamage;

        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return resolvedDamage;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.TriggerAdditionalDamageWhenEnemyStatusTicksWithConditionStatus)
                    continue;
                if (!StatusMatches(effect.statusData, statusData, statusId))
                    continue;
                if (effect.conditionStatusData == null || target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
                    continue;

                int multiplier = Mathf.Max(1, effect.amount) * Mathf.Max(1, relicState.amount);
                resolvedDamage += Mathf.Max(0, defaultDamage) * multiplier;
            }
        }

        return resolvedDamage;
    }

    public static int ResolveEnemyRoundStartLinkedStatusAmount(
        BattleStatus_SO statusData,
        string statusId,
        int defaultStacks)
    {
        int resolvedStacks = Mathf.Max(0, defaultStacks);
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return resolvedStacks;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.SetEnemyLinkedStatusRoundStartStacks)
                    continue;
                if (!StatusMatches(effect.conditionStatusData, statusData, statusId))
                    continue;

                resolvedStacks = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
            }
        }

        return resolvedStacks;
    }

    public static bool ShouldPreventAllyStatusApplication(BattleStatus_SO statusData, string statusId)
    {
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return false;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.PreventAllyStatusApplication)
                    continue;
                if (StatusMatches(effect.conditionStatusData, statusData, statusId))
                    return true;
            }
        }

        return false;
    }

    private static void HandleApplyStatusToLowestHpEnemyOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null)
            return;

        BattleEnemyController target = ResolveLowestHpLivingEnemy(context.enemies, null);
        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (target == null || stacks <= 0)
            return;

        TryApplyStatus(target, effect, stacks);
    }

    private static void HandleApplyStatusToEnemyOnFirstPlayerHitEachRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerHitEnemy || effect.statusData == null)
            return;
        if (!TryRegisterRoundScopedProc(relicData, $"first-hit-status:{context.currentRound}"))
            return;

        BattleEnemyController target = context.target as BattleEnemyController;
        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (target == null || stacks <= 0)
            return;

        TryApplyStatus(target, effect, stacks);
    }

    private static void HandleTriggerStatusDamageWhenEnemyStatusReachesStacksOncePerBattle(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnStatusApplied || !context.sourceIsPlayer)
            return;
        if (!StatusMatches(effect.conditionStatusData, context.statusData, context.statusId))
            return;

        BattleEnemyController target = context.target as BattleEnemyController;
        if (target == null || target.IsDead)
            return;

        int projectedStacks = target.GetStatusStacks(effect.conditionStatusData) + context.statusStacks + context.additionalStatusStacks;
        if (projectedStacks < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        string key = BuildRelicProcKey(relicData, $"status-threshold:{GetObjectKey(target)}:{GetStatusKey(effect.conditionStatusData, context.statusId)}");
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter != null && !encounter.TryRegisterBattleRelicProc(key))
            return;

        int damage = Mathf.Max(0, projectedStacks * Mathf.Max(1, relicState.amount));
        if (damage > 0)
            target.TakeDamage(damage, 0, damage);
    }

    private static void HandleChanceApplyStatusToEnemiesWithStatusOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || effect.conditionStatusData == null)
            return;
        if (context.enemies == null)
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        float perTargetChance = effect.maxStacks > 1
            ? Mathf.Clamp01(effect.maxStacks / 100f)
            : Mathf.Clamp01(effect.chance);
        for (int i = 0; i < context.enemies.Length; i++)
        {
            BattleEnemyController enemy = context.enemies[i];
            if (enemy == null || enemy.IsDead || enemy.GetStatusStacks(effect.conditionStatusData) <= 0)
                continue;
            if (Random.value > perTargetChance)
                continue;

            TryApplyStatus(enemy, effect, stacks);
        }
    }

    private static void HandleApplyStatusToAttackerAfterTakingSingleAttackDamageLimitedPerRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerTookSingleAttackDamage || effect.statusData == null || context.resolvedDamage <= 0)
            return;
        if (!TryRegisterRoundScopedProc(relicData, $"single-attack-limited-status:{context.currentRound}", Mathf.Max(1, effect.maxStacks)))
            return;

        BattleEnemyController attacker = context.source as BattleEnemyController;
        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (attacker == null || stacks <= 0)
            return;

        TryApplyStatus(attacker, effect, stacks);
    }

    private static void HandleModifyPlayerClashDiceWhenTargetHasStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer || !context.isClashPreparation)
            return;

        BattleEnemyController target = context.target as BattleEnemyController;
        if (target == null || effect.conditionStatusData == null)
            return;
        if (target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int relicAmount = Mathf.Max(1, relicState.amount);
        context.additionalPowerBonus += effect.powerAmount * relicAmount;
        context.additionalMinPowerBonus += effect.minPowerAmount * relicAmount;
        context.additionalMaxPowerBonus += effect.maxPowerAmount * relicAmount;
    }

    private static void HandleHealSourceWhenPlayerAppliesStatusToEnemy(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnStatusApplied || !context.sourceIsPlayer)
            return;
        if (!StatusMatches(effect.conditionStatusData, context.statusData, context.statusId))
            return;

        BattleAllyController source = context.source as BattleAllyController;
        BattleEnemyController target = context.target as BattleEnemyController;
        int heal = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (source == null || target == null || heal <= 0)
            return;

        source.Heal(heal);
    }

    private static void HandleApplyStatusToAllAlliesWhenStatusBearerCountAtLeast(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || effect.conditionStatusData == null)
            return;
        if (context.allies == null)
            return;

        int bearerCount = CountLivingStatusBearers(context.allies, context.enemies, effect.conditionStatusData);
        if (bearerCount < Mathf.Max(1, effect.requiredDistinctStatusCount))
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        for (int i = 0; i < context.allies.Length; i++)
        {
            BattleAllyController ally = context.allies[i];
            if (ally != null && !ally.IsDead)
                TryApplyStatus(ally, effect, stacks);
        }
    }

    private static void HandleTransferStatusToLowestHpEnemyOnEnemyDeath(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnEnemyDeath || effect.statusData == null)
            return;

        BattleEnemyController defeated = context.target as BattleEnemyController;
        if (defeated == null)
            return;

        int stacks = defeated.GetStatusStacks(effect.statusData);
        if (stacks <= 0)
            return;

        BattleEnemyController recipient = ResolveLowestHpLivingEnemy(context.enemies, defeated);
        if (recipient == null)
            return;

        recipient.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
    }

    private static void HandleHealSourceWhenDamagingEnemyWithStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage || context.outgoingDamage <= 0)
            return;

        BattleAllyController source = context.source as BattleAllyController;
        BattleEnemyController target = context.target as BattleEnemyController;
        int heal = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (source == null || target == null || effect.conditionStatusData == null || heal <= 0)
            return;
        if (target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        source.Heal(heal);
    }

    private static void HandleModifyOutgoingDamagePercentByTargetStatusStacks(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage || context.outgoingDamage <= 0)
            return;

        BattleEnemyController target = context.target as BattleEnemyController;
        if (target == null || effect.conditionStatusData == null)
            return;

        int conditionStacks = target.GetStatusStacks(effect.conditionStatusData);
        if (conditionStacks < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int percentBonus = conditionStacks * Mathf.Max(0, effect.amount) * Mathf.Max(1, relicState.amount);
        if (percentBonus > 0)
        {
            context.outgoingDamagePercentModifier += percentBonus;
            context.AddOutgoingDamagePercentModifierSource(percentBonus, GetRelicDisplayName(relicData));
        }
    }

    private static void HandlePreventLethalDamageByConsumingEnemyStatusTotal(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage || effect.conditionStatusData == null)
            return;

        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        BattleEnemyController[] enemies = Object.FindObjectsOfType<BattleEnemyController>(false);
        int totalStacks = CountEnemyStatusStacks(enemies, effect.conditionStatusData);
        if (totalStacks <= Mathf.Max(0, effect.requiredStatusStacks))
            return;

        string relicId = relicData != null && !string.IsNullOrWhiteSpace(relicData.relicId) ? relicData.relicId : relicData.name;
        BattleAllyController target = context.target as BattleAllyController;
        if (target != null)
        {
            if (target.HasConsumedLethalProtection(relicId))
                return;
            if (!target.TryConsumeLethalProtection(relicId))
                return;
        }
        else if (encounter != null && !encounter.TryRegisterBattleRelicProc(BuildRelicProcKey(relicData, "lethal-prevent")))
        {
            return;
        }

        ConsumeEnemyStatusStacks(enemies, effect.conditionStatusData, Mathf.Min(totalStacks, Mathf.Max(1, context.incomingDamage)));
        context.preventLethalDamage = true;
        context.minimumSurvivingHp = Mathf.Max(1, context.minimumSurvivingHp);
    }

    private static void HandleChanceCopyAppliedStatusToOtherEnemy(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnStatusApplied || !context.sourceIsPlayer || effect.statusData == null)
            return;
        if (!StatusMatches(effect.statusData, context.statusData, context.statusId))
            return;

        BattleEnemyController originalTarget = context.target as BattleEnemyController;
        BattleEnemyController other = ResolveRandomOtherLivingEnemy(originalTarget);
        int stacks = Mathf.Max(0, (context.statusStacks + context.additionalStatusStacks) * Mathf.Max(1, relicState.amount));
        if (other == null || stacks <= 0)
            return;

        other.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
    }

    private static void HandleChanceRepeatAppliedStatusToSameEnemy(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnStatusApplied || !context.sourceIsPlayer || effect.statusData == null)
            return;
        if (!StatusMatches(effect.statusData, context.statusData, context.statusId))
            return;

        BattleEnemyController target = context.target as BattleEnemyController;
        int stacks = Mathf.Max(0, (context.statusStacks + context.additionalStatusStacks) * Mathf.Max(1, relicState.amount));
        if (target == null || stacks <= 0)
            return;

        target.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
    }

    private static void HandleApplyStatusToEnemiesBasedOnConditionStacksOnRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || effect.conditionStatusData == null || context.enemies == null)
            return;

        for (int i = 0; i < context.enemies.Length; i++)
        {
            BattleEnemyController enemy = context.enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            int conditionStacks = enemy.GetStatusStacks(effect.conditionStatusData);
            if (conditionStacks <= 0)
                continue;

            int stacks = conditionStacks * Mathf.Max(1, effect.amount) * Mathf.Max(1, relicState.amount);
            if (stacks > 0)
                enemy.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
        }
    }

    private static void HandleApplyStatusToEnemiesWhenConditionStacksAtLeastOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || effect.conditionStatusData == null || context.enemies == null)
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        int requiredStacks = Mathf.Max(1, effect.requiredStatusStacks);
        for (int i = 0; i < context.enemies.Length; i++)
        {
            BattleEnemyController enemy = context.enemies[i];
            if (enemy == null || enemy.IsDead || enemy.GetStatusStacks(effect.conditionStatusData) < requiredStacks)
                continue;

            TryApplyStatus(enemy, effect, stacks);
        }
    }

    private static void HandleDealDamageToEnemiesEqualToStatusStacksOnBattleRoundEnd(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundEnd || effect.conditionStatusData == null || context.enemies == null)
            return;

        int multiplier = Mathf.Max(1, effect.amount) * Mathf.Max(1, relicState.amount);
        for (int i = 0; i < context.enemies.Length; i++)
        {
            BattleEnemyController enemy = context.enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            int stacks = enemy.GetStatusStacks(effect.conditionStatusData);
            int damage = Mathf.Max(0, stacks * multiplier);
            if (damage > 0)
                enemy.TakeDamage(damage, 0, damage);
        }
    }

    private static void HandleRestoreSharedEnergyByEnemyStatusStacksOnBattleRoundEnd(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundEnd || effect.conditionStatusData == null)
            return;

        int totalStacks = CountEnemyStatusStacks(context.enemies, effect.conditionStatusData);
        int divisor = Mathf.Max(1, effect.requiredStatusStacks);
        int recovery = Mathf.FloorToInt(totalStacks / (float)divisor) * Mathf.Max(1, effect.amount) * Mathf.Max(1, relicState.amount);
        if (recovery > 0)
            context.additionalSharedEnergyRecovery += recovery;
    }

    private static void HandleApplyStatusesToHighestAndLowestHpRatioEnemiesOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || effect.conditionStatusData == null || context.enemies == null)
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        BattleEnemyController highest = ResolveHighestHpRatioLivingEnemy(context.enemies);
        BattleEnemyController lowest = ResolveLowestHpRatioLivingEnemy(context.enemies);
        if (highest != null)
            highest.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
        if (lowest != null)
            lowest.AddStatus(effect.conditionStatusData, effect.conditionStatusData.StatusId, effect.conditionStatusData.DisplayName, stacks);
    }

    private static void HandleApplyStatusToEnemyWithConditionOnFirstPlayerHitEachRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerHitEnemy || effect.statusData == null || effect.conditionStatusData == null)
            return;

        BattleEnemyController target = context.target as BattleEnemyController;
        if (target == null || target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;
        if (!TryRegisterRoundScopedProc(relicData, $"conditional-first-hit-status:{context.currentRound}"))
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks > 0)
            TryApplyStatus(target, effect, stacks);
    }

    private static void HandleApplyStatusToLowestSpeedAllyWhenAnyEnemyHasStatusOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || effect.conditionStatusData == null)
            return;
        if (!AnyEnemyHasStatusStacksAtLeast(context.enemies, effect.conditionStatusData, Mathf.Max(1, effect.requiredStatusStacks)))
            return;

        BattleAllyController ally = ResolveLowestSpeedLivingAlly(context.allies);
        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (ally != null && stacks > 0)
            TryApplyStatus(ally, effect, stacks);
    }

    private static bool StatusMatches(BattleStatus_SO expectedStatus, BattleStatus_SO actualStatus, string actualStatusId)
    {
        if (expectedStatus == null)
            return false;
        if (actualStatus != null && expectedStatus == actualStatus)
            return true;

        return string.Equals(expectedStatus.StatusId, actualStatusId, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetStatusKey(BattleStatus_SO statusData, string fallbackStatusId)
    {
        return statusData != null && !string.IsNullOrWhiteSpace(statusData.StatusId)
            ? statusData.StatusId
            : (fallbackStatusId ?? string.Empty);
    }

    private static string BuildRelicProcKey(Relic_SO relicData, string suffix)
    {
        string relicIdentity = relicData != null && !string.IsNullOrWhiteSpace(relicData.relicId)
            ? relicData.relicId
            : (relicData != null ? relicData.name : "relic");
        return $"{relicIdentity}:{suffix}";
    }

    private static string GetObjectKey(Object target)
    {
        return target != null ? target.GetInstanceID().ToString() : "none";
    }

    private static bool TryRegisterRoundScopedProc(Relic_SO relicData, string suffix, int maxCount)
    {
        maxCount = Mathf.Max(1, maxCount);
        for (int i = 0; i < maxCount; i++)
        {
            if (TryRegisterRoundScopedProc(relicData, $"{suffix}:{i}"))
                return true;
        }

        return false;
    }

    private static BattleEnemyController ResolveLowestHpLivingEnemy(BattleEnemyController[] enemies, BattleEnemyController excluded)
    {
        if (enemies == null)
            return null;

        BattleEnemyController best = null;
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController candidate = enemies[i];
            if (candidate == null || candidate == excluded || candidate.IsDead)
                continue;

            if (best == null
                || candidate.CurrentHP < best.CurrentHP
                || (candidate.CurrentHP == best.CurrentHP && candidate.MaxHP < best.MaxHP))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static BattleEnemyController ResolveRandomOtherLivingEnemy(BattleEnemyController excluded)
    {
        BattleEnemyController[] enemies = Object.FindObjectsOfType<BattleEnemyController>(false);
        List<BattleEnemyController> candidates = new List<BattleEnemyController>();
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null && enemy != excluded && !enemy.IsDead)
                candidates.Add(enemy);
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private static int CountLivingStatusBearers(BattleAllyController[] allies, BattleEnemyController[] enemies, BattleStatus_SO statusData)
    {
        int count = 0;
        if (allies != null)
        {
            for (int i = 0; i < allies.Length; i++)
            {
                BattleAllyController ally = allies[i];
                if (ally != null && !ally.IsDead && ally.GetStatusStacks(statusData) > 0)
                    count++;
            }
        }

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy != null && !enemy.IsDead && enemy.GetStatusStacks(statusData) > 0)
                    count++;
            }
        }

        return count;
    }

    private static int CountEnemyStatusStacks(BattleEnemyController[] enemies, BattleStatus_SO statusData)
    {
        if (enemies == null || statusData == null)
            return 0;

        int total = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null && !enemy.IsDead)
                total += enemy.GetStatusStacks(statusData);
        }

        return total;
    }

    private static bool AnyEnemyHasStatusStacksAtLeast(BattleEnemyController[] enemies, BattleStatus_SO statusData, int requiredStacks)
    {
        if (enemies == null || statusData == null)
            return false;

        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null && !enemy.IsDead && enemy.GetStatusStacks(statusData) >= requiredStacks)
                return true;
        }

        return false;
    }

    private static BattleEnemyController ResolveHighestHpRatioLivingEnemy(BattleEnemyController[] enemies)
    {
        if (enemies == null)
            return null;

        BattleEnemyController best = null;
        float bestRatio = -1f;
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.MaxHP <= 0)
                continue;

            float ratio = enemy.CurrentHP / (float)enemy.MaxHP;
            if (best == null || ratio > bestRatio || (Mathf.Approximately(ratio, bestRatio) && enemy.CurrentHP > best.CurrentHP))
            {
                best = enemy;
                bestRatio = ratio;
            }
        }

        return best;
    }

    private static BattleEnemyController ResolveLowestHpRatioLivingEnemy(BattleEnemyController[] enemies)
    {
        if (enemies == null)
            return null;

        BattleEnemyController best = null;
        float bestRatio = float.MaxValue;
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.MaxHP <= 0)
                continue;

            float ratio = enemy.CurrentHP / (float)enemy.MaxHP;
            if (best == null || ratio < bestRatio || (Mathf.Approximately(ratio, bestRatio) && enemy.CurrentHP < best.CurrentHP))
            {
                best = enemy;
                bestRatio = ratio;
            }
        }

        return best;
    }

    private static BattleAllyController ResolveLowestSpeedLivingAlly(BattleAllyController[] allies)
    {
        if (allies == null)
            return null;

        BattleAllyController best = null;
        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;

            if (best == null
                || ally.Speed < best.Speed
                || (ally.Speed == best.Speed && ally.CurrentHP < best.CurrentHP))
            {
                best = ally;
            }
        }

        return best;
    }

    private static void ConsumeEnemyStatusStacks(BattleEnemyController[] enemies, BattleStatus_SO statusData, int amount)
    {
        if (enemies == null || statusData == null || amount <= 0)
            return;

        Array.Sort(enemies, (left, right) =>
        {
            int leftStacks = left != null ? left.GetStatusStacks(statusData) : 0;
            int rightStacks = right != null ? right.GetStatusStacks(statusData) : 0;
            return rightStacks.CompareTo(leftStacks);
        });

        int remaining = amount;
        for (int i = 0; i < enemies.Length && remaining > 0; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            int removed = enemy.RemoveStatusStacks(statusData, statusData.StatusId, remaining, statusData.DisplayName);
            remaining -= removed;
        }
    }
}
