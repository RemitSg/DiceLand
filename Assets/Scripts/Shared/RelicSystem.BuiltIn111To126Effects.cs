using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static partial class RelicSystem
{
    private const string TransferredLethalProtectionProcPrefix = "transferred-lethal-protection:";

    public static int ResolveEnemyStatusRoundEndStacksAfterDecay(
        BattleEnemyController target,
        BattleStatus_SO statusData,
        string statusId,
        int stacksBeforeDecay,
        int defaultStacksAfterDecay)
    {
        if (target == null || stacksBeforeDecay <= 0)
            return Mathf.Max(0, defaultStacksAfterDecay);

        int resolvedStacks = Mathf.Max(0, defaultStacksAfterDecay);
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return resolvedStacks;

        bool allLivingEnemiesHaveStatus = false;
        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null)
                    continue;
                if (!StatusMatches(effect.conditionStatusData, statusData, statusId))
                    continue;

                if (effect.effectType == Relic_SO.RelicBuiltInEffectType.SetEnemyStatusRoundEndDecayPercent)
                {
                    int decayPercent = Mathf.Clamp(effect.amount, 0, 100);
                    int stacksAfterDecay = Mathf.FloorToInt(stacksBeforeDecay * (1f - (decayPercent / 100f)));
                    resolvedStacks = Mathf.Max(resolvedStacks, Mathf.Max(0, stacksAfterDecay));
                }
                else if (effect.effectType == Relic_SO.RelicBuiltInEffectType.PreserveEnemyStatusWhenAllLivingEnemiesHaveStatus)
                {
                    allLivingEnemiesHaveStatus = allLivingEnemiesHaveStatus
                        || AllLivingEnemiesHaveStatus(Object.FindObjectsOfType<BattleEnemyController>(false), effect.conditionStatusData, Mathf.Max(1, effect.requiredStatusStacks));
                    if (allLivingEnemiesHaveStatus)
                        resolvedStacks = Mathf.Max(resolvedStacks, Mathf.Max(1, effect.amount * Mathf.Max(1, relicState.amount)));
                }
            }
        }

        return resolvedStacks;
    }

    public static bool ShouldPreventEnemyHealing(BattleEnemyController target)
    {
        if (target == null || target.IsDead)
            return false;

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
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.PreventEnemyHealingWhenHasStatus)
                    continue;
                if (effect.conditionStatusData != null && target.GetStatusStacks(effect.conditionStatusData) >= Mathf.Max(1, effect.requiredStatusStacks))
                    return true;
            }
        }

        return false;
    }

    public static bool ShouldPromotePlayerOnHitStatusToSkillUse(BattleStatus_SO statusData, string statusId)
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
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.PromotePlayerOnHitStatusToSkillUse)
                    continue;
                if (StatusMatches(effect.conditionStatusData, statusData, statusId))
                    return true;
            }
        }

        return false;
    }

    private static void HandleDealDamageWhenEnemyStatusReachesStacksOncePerRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnStatusApplied || context.sourceIsPlayer || effect.conditionStatusData == null)
            return;

        BattleEnemyController target = context.target as BattleEnemyController;
        if (target == null || target.IsDead)
            return;
        if (!StatusMatches(effect.conditionStatusData, context.statusData, context.statusId))
            return;

        int projectedStacks = target.GetStatusStacks(effect.conditionStatusData) + context.statusStacks + context.additionalStatusStacks;
        if (projectedStacks < Mathf.Max(1, effect.requiredStatusStacks))
            return;
        if (!TryRegisterRoundScopedProc(relicData, $"status-threshold-damage:{GetObjectKey(target)}"))
            return;

        int damage = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (damage > 0)
            target.TakeDamage(damage, 0, damage);
    }

    private static void HandleTransferStatusToAllEnemiesOnEnemyDeath(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnEnemyDeath || effect.statusData == null || context.enemies == null)
            return;

        BattleEnemyController defeated = context.target as BattleEnemyController;
        if (defeated == null)
            return;

        int stacks = defeated.GetStatusStacks(effect.statusData);
        if (stacks <= 0)
            return;

        int affected = 0;
        for (int i = 0; i < context.enemies.Length; i++)
        {
            BattleEnemyController enemy = context.enemies[i];
            if (enemy == null || enemy.IsDead || enemy == defeated)
                continue;

            if (enemy.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks))
                affected++;
        }

        if (affected > 0)
            AddRelicFeedback(context, relicData, $"{affected} 名敌人获得 {stacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleHealRandomDamagedAllyOnEnemyDeathWithStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnEnemyDeath || effect.conditionStatusData == null || context.allies == null)
            return;

        BattleEnemyController defeated = context.target as BattleEnemyController;
        if (defeated == null || defeated.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        List<BattleAllyController> candidates = new List<BattleAllyController>();
        for (int i = 0; i < context.allies.Length; i++)
        {
            BattleAllyController ally = context.allies[i];
            if (ally != null && !ally.IsDead && ally.CurrentHP < ally.MaxHP)
                candidates.Add(ally);
        }

        if (candidates.Count == 0)
            return;

        BattleAllyController selected = candidates[Random.Range(0, candidates.Count)];
        int healed = selected.Heal(Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount)));
        if (healed > 0)
            AddRelicFeedback(context, relicData, $"{selected.GetDebugDisplayName()} 恢复 {healed} 点生命。");
    }

    private static void HandleReflectEnemyStatusStacksDamageAfterTakingAttackDamage(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerTookSingleAttackDamage || effect.conditionStatusData == null || context.resolvedDamage <= 0)
            return;

        BattleEnemyController attacker = context.source as BattleEnemyController;
        if (attacker == null || attacker.IsDead)
            return;

        int statusStacks = attacker.GetStatusStacks(effect.conditionStatusData);
        if (statusStacks < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int damage = Mathf.Max(0, statusStacks * Mathf.Max(1, effect.amount) * Mathf.Max(1, relicState.amount));
        if (damage > 0)
            attacker.TakeDamage(damage, 0, damage);
    }

    private static void HandleApplyStatusToEnemyOnEnemyClashWinWithStatusOncePerRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerClashLost || effect.statusData == null || effect.conditionStatusData == null)
            return;

        BattleEnemyController target = context.target as BattleEnemyController;
        if (target == null || target.IsDead)
            return;
        if (target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;
        if (!TryRegisterRoundScopedProc(relicData, $"enemy-clash-win-status:{GetObjectKey(target)}:{context.currentRound}"))
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks > 0)
            TryApplyStatus(target, effect, stacks);
    }

    private static void HandlePreventLethalDamageByTransferringHighestHpAllyHealth(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage)
            return;

        BattleAllyController target = context.target as BattleAllyController;
        if (target == null)
            return;

        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        string battleProcKey = BuildRelicProcKey(relicData, TransferredLethalProtectionProcPrefix + "battle");

        BattleAllyController donor = ResolveHighestHpRatioLivingAlly(Object.FindObjectsOfType<BattleAllyController>(false), target);
        if (donor == null || donor.CurrentHP <= 1)
            return;
        if (encounter != null && !encounter.TryRegisterBattleRelicProc(battleProcKey))
            return;

        string relicId = relicData != null && !string.IsNullOrWhiteSpace(relicData.relicId) ? relicData.relicId : relicData.name;
        if (target.HasConsumedLethalProtection(relicId) || !target.TryConsumeLethalProtection(relicId))
            return;

        int transferredHp = Mathf.Max(1, donor.CurrentHP / 2);
        donor.TakeDamage(transferredHp, 0, transferredHp);
        context.preventLethalDamage = true;
        context.minimumSurvivingHp = Mathf.Max(1, context.minimumSurvivingHp);

        if (encounter != null)
            encounter.TryRegisterBattleRelicProc(BuildRelicProcKey(relicData, TransferredLethalProtectionProcPrefix + GetObjectKey(target)));

        if (effect.statusData != null
            && effect.conditionStatusData != null
            && AnyEnemyHasStatusStacksAtLeast(Object.FindObjectsOfType<BattleEnemyController>(false), effect.conditionStatusData, Mathf.Max(1, effect.requiredStatusStacks)))
        {
            int stacks = CalculateScaledStatusStacks(relicState, effect);
            if (stacks > 0)
                target.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
        }

        AddRelicFeedback(context, relicData, $"{target.GetDebugDisplayName()} 借由 {donor.GetDebugDisplayName()} 的生命幸存。");
    }

    private static void HandleApplyStatusOnTransferredLethalProtectionWhenEnemyStatusAtLeast(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage || effect.statusData == null || effect.conditionStatusData == null)
            return;

        BattleAllyController target = context.target as BattleAllyController;
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (target == null || encounter == null)
            return;
        if (encounter.TryRegisterBattleRelicProc(BuildRelicProcKey(relicData, TransferredLethalProtectionProcPrefix + GetObjectKey(target))))
            return;
        if (!AnyEnemyHasStatusStacksAtLeast(Object.FindObjectsOfType<BattleEnemyController>(false), effect.conditionStatusData, Mathf.Max(1, effect.requiredStatusStacks)))
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks > 0)
            target.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
    }

    private static void HandleModifyPlayerClashDiceWhenTargetHasStatusOncePerRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer || !context.isClashPreparation)
            return;

        BattleAllyController source = context.source as BattleAllyController;
        BattleEnemyController target = context.target as BattleEnemyController;
        if (source == null || target == null || effect.conditionStatusData == null)
            return;
        if (target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;
        if (!TryRegisterRoundScopedProc(relicData, $"clash-dice-status-once:{GetObjectKey(source)}"))
            return;

        int relicAmount = Mathf.Max(1, relicState.amount);
        context.additionalPowerBonus += effect.powerAmount * relicAmount;
        context.additionalMinPowerBonus += effect.minPowerAmount * relicAmount;
        context.additionalMaxPowerBonus += effect.maxPowerAmount * relicAmount;
    }

    private static void HandleModifyOutgoingDamagePercentWhenTargetHasStatus(
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
        if (target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int percentBonus = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (percentBonus <= 0)
            return;

        context.outgoingDamagePercentModifier += percentBonus;
        context.AddOutgoingDamagePercentModifierSource(percentBonus, GetRelicDisplayName(relicData));
    }

    private static BattleAllyController ResolveHighestHpRatioLivingAlly(BattleAllyController[] allies, BattleAllyController excluded)
    {
        if (allies == null)
            return null;

        BattleAllyController best = null;
        float bestRatio = -1f;
        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally == excluded || ally.IsDead || ally.MaxHP <= 0)
                continue;

            float ratio = ally.CurrentHP / (float)ally.MaxHP;
            if (best == null || ratio > bestRatio || (Mathf.Approximately(ratio, bestRatio) && ally.CurrentHP > best.CurrentHP))
            {
                best = ally;
                bestRatio = ratio;
            }
        }

        return best;
    }

    private static bool AllLivingEnemiesHaveStatus(BattleEnemyController[] enemies, BattleStatus_SO statusData, int requiredStacks)
    {
        if (enemies == null || statusData == null)
            return false;

        bool hasLivingEnemy = false;
        int safeRequiredStacks = Mathf.Max(1, requiredStacks);
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            hasLivingEnemy = true;
            if (enemy.GetStatusStacks(statusData) < safeRequiredStacks)
                return false;
        }

        return hasLivingEnemy;
    }
}
