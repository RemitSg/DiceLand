using UnityEngine;
using Object = UnityEngine.Object;

public static partial class RelicSystem
{
    public static int DispatchPlayerStatusBurst(
        BattleAllyController attacker,
        BattleEnemyController target,
        BattleStatus_SO statusData,
        string statusId,
        string statusName,
        int damagePerStack,
        bool clearStacksAfterBurst,
        System.Func<int, int> modifyDamage = null,
        System.Action<int, int> logApplied = null)
    {
        if (target == null)
            return 0;

        int stacks = target.GetStatusStacks(statusData);
        if (stacks <= 0)
            stacks = target.GetStatusStacks(statusId);

        int damage = BattleStatusBurstDispatcher.DispatchBurst(
            stacks,
            damagePerStack,
            modifyDamage,
            (modifiedDamage, originalDamage) => target.TakeDamage(modifiedDamage, 0, originalDamage),
            () => target.ClearStatus(statusData, statusId, statusName),
            amount => ConsumeStatusBurstStacks(target, statusData, statusId, statusName, amount),
            clearStacksAfterBurst,
            logApplied);

        if (damage > 0)
            ProcessPlayerStatusBurstRelics(attacker, target, statusData, statusId, damage);

        return damage;
    }

    public static int ResolveAllyRelicRoundStartSpeedBonus(BattleAllyController ally)
    {
        if (ally == null || ally.IsDead)
            return 0;

        int bonus = 0;
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return bonus;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.TreatAllyWithStatusAsSpeedBonusAtRoundStart)
                    continue;
                if (effect.conditionStatusData == null || ally.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
                    continue;

                bonus += Mathf.Max(0, effect.amount) * Mathf.Max(1, relicState.amount);
            }
        }

        return bonus;
    }

    public static void ApplyRelicStatusOnPlayerDamage(BattleAllyController attacker, BattleEnemyController target, int dealtDamage)
    {
        if (attacker == null || target == null || dealtDamage <= 0)
            return;

        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.ApplyStatusToTargetOnPlayerDamageWhenSourceHasStatus)
                    continue;
                if (effect.statusData == null || effect.conditionStatusData == null)
                    continue;
                if (attacker.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
                    continue;

                int stacks = CalculateScaledStatusStacks(relicState, effect);
                if (stacks > 0)
                    TryApplyStatus(target, effect, stacks);
            }
        }
    }

    private static int ConsumeStatusBurstStacks(
        BattleEnemyController target,
        BattleStatus_SO statusData,
        string statusId,
        string statusName,
        int stacksBeforeBurst)
    {
        if (target == null || stacksBeforeBurst <= 0)
            return 0;

        int consumePercent = ResolveStatusBurstConsumptionPercent(statusData, statusId, 100);
        int stacksToRemove = Mathf.Clamp(Mathf.FloorToInt(stacksBeforeBurst * (consumePercent / 100f)), 0, stacksBeforeBurst);
        if (stacksToRemove <= 0)
            return 0;

        return target.RemoveStatusStacks(statusData, statusId, stacksToRemove, statusName);
    }

    private static int ResolveStatusBurstConsumptionPercent(BattleStatus_SO statusData, string statusId, int defaultPercent)
    {
        int resolvedPercent = Mathf.Clamp(defaultPercent, 0, 100);
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return resolvedPercent;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null || effect.effectType != Relic_SO.RelicBuiltInEffectType.SetStatusBurstConsumptionPercent)
                    continue;
                if (!StatusMatches(effect.conditionStatusData, statusData, statusId))
                    continue;

                resolvedPercent = Mathf.Min(resolvedPercent, Mathf.Clamp(effect.amount, 0, 100));
            }
        }

        return resolvedPercent;
    }

    private static void ProcessPlayerStatusBurstRelics(
        BattleAllyController attacker,
        BattleEnemyController target,
        BattleStatus_SO statusData,
        string statusId,
        int damage)
    {
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null || encounter.relicInventory == null)
            return;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0 || relicState.relicData.builtInEffects == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicState.relicData.builtInEffects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relicState.relicData.builtInEffects[effectIndex];
                if (effect == null || !StatusMatches(effect.conditionStatusData, statusData, statusId))
                    continue;

                ProcessPlayerStatusBurstEffect(relicState.relicData, relicState, effect, attacker, target, damage);
            }
        }
    }

    private static void ProcessPlayerStatusBurstEffect(
        Relic_SO relicData,
        RunRelicState relicState,
        Relic_SO.RelicBuiltInEffectEntry effect,
        BattleAllyController attacker,
        BattleEnemyController target,
        int damage)
    {
        switch (effect.effectType)
        {
            case Relic_SO.RelicBuiltInEffectType.SpreadPlayerStatusBurstDamageToOtherEnemies:
                SpreadStatusBurstDamage(relicData, relicState, effect, target, damage);
                break;
            case Relic_SO.RelicBuiltInEffectType.QueueNextRoundStatusOnSourceWhenPlayerStatusBurstDamageAtLeast:
                QueueThresholdStatus(relicState, effect, attacker, damage);
                break;
            case Relic_SO.RelicBuiltInEffectType.ApplyStatusToSourceOnPlayerStatusBurstDamage:
                ApplyStatusToBurstSource(relicState, effect, attacker, damage);
                break;
            case Relic_SO.RelicBuiltInEffectType.DealDamageToPlayerStatusBurstTargetWithConditionStatus:
                DealExtraDamageToBurstTarget(effect, target);
                break;
        }
    }

    private static void SpreadStatusBurstDamage(
        Relic_SO relicData,
        RunRelicState relicState,
        Relic_SO.RelicBuiltInEffectEntry effect,
        BattleEnemyController target,
        int damage)
    {
        if (target == null || damage <= 0)
            return;

        int spreadDamage = Mathf.FloorToInt(damage * Mathf.Max(0, effect.amount) * Mathf.Max(1, relicState.amount) / 100f);
        if (spreadDamage <= 0)
            return;

        BattleEnemyController[] enemies = Object.FindObjectsOfType<BattleEnemyController>(false);
        int affected = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy == target || enemy.IsDead)
                continue;

            enemy.TakeDamage(spreadDamage, 0, spreadDamage);
            affected++;
        }

        if (affected == 0)
            target.TakeDamage(spreadDamage, 0, spreadDamage);
    }

    private static void QueueThresholdStatus(
        RunRelicState relicState,
        Relic_SO.RelicBuiltInEffectEntry effect,
        BattleAllyController attacker,
        int damage)
    {
        if (attacker == null || damage < Mathf.Max(1, effect.requiredStatusStacks))
            return;
        if (effect.statusData == null)
            return;

        int stacks = Mathf.Max(0, effect.amount) * Mathf.Max(1, relicState.amount);
        if (stacks > 0)
            attacker.QueueStatusForRoundStart(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks, 1, 1);
    }

    private static void ApplyStatusToBurstSource(
        RunRelicState relicState,
        Relic_SO.RelicBuiltInEffectEntry effect,
        BattleAllyController attacker,
        int damage)
    {
        if (attacker == null || effect.statusData == null || damage <= 0)
            return;

        int stacks = Mathf.Max(0, damage * Mathf.Max(1, effect.amount) * Mathf.Max(1, relicState.amount));
        if (stacks > 0)
            attacker.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
    }

    private static void DealExtraDamageToBurstTarget(Relic_SO.RelicBuiltInEffectEntry effect, BattleEnemyController target)
    {
        if (target == null || effect.statusData == null)
            return;
        if (target.GetStatusStacks(effect.statusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int damage = Mathf.Max(0, effect.amount);
        if (damage > 0)
            target.TakeDamage(damage, 0, damage);
    }

    private static void HandleChanceTriggerNonConsumingStatusBurstWhenDamagingStatusTarget(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerHitEnemy || context.resolvedDamage <= 0 || effect.conditionStatusData == null)
            return;

        BattleAllyController attacker = context.source as BattleAllyController;
        BattleEnemyController target = context.target as BattleEnemyController;
        if (target == null || target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int damagePerStack = Mathf.Max(1, effect.amount);
        DispatchPlayerStatusBurst(
            attacker,
            target,
            effect.conditionStatusData,
            effect.conditionStatusData.StatusId,
            effect.conditionStatusData.DisplayName,
            damagePerStack,
            clearStacksAfterBurst: false);
    }

    private static void HandleDealDamageToEnemiesWithStatusStacksAtLeastOnBattleRoundEnd(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundEnd || effect.conditionStatusData == null || context.enemies == null)
            return;

        int requiredStacks = Mathf.Max(1, effect.requiredStatusStacks);
        int damage = Mathf.Max(0, effect.amount) * Mathf.Max(1, relicState.amount);
        if (damage <= 0)
            return;

        for (int i = 0; i < context.enemies.Length; i++)
        {
            BattleEnemyController enemy = context.enemies[i];
            if (enemy != null && !enemy.IsDead && enemy.GetStatusStacks(effect.conditionStatusData) >= requiredStacks)
                enemy.TakeDamage(damage, 0, damage);
        }
    }

    private static void HandleApplyStatusToAllAlliesAtBattleSetup(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleSetup || context.currentRound != 1 || effect.statusData == null || context.allies == null)
            return;

        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        string procKey = BuildRelicProcKey(relicData, $"battle-setup-all-ally-status:{GetStatusKey(effect.statusData, effect.statusData.StatusId)}");
        if (encounter != null && !encounter.TryRegisterBattleRelicProc(procKey))
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        for (int i = 0; i < context.allies.Length; i++)
        {
            BattleAllyController ally = context.allies[i];
            if (ally != null && !ally.IsDead)
                ally.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
        }
    }

    private static void HandleApplyStatusToAllAlliesWhenAllLivingAlliesHaveStatusOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart || effect.statusData == null || effect.conditionStatusData == null || context.allies == null)
            return;
        if (!AllLivingAlliesHaveStatus(context.allies, effect.conditionStatusData, Mathf.Max(1, effect.requiredStatusStacks)))
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        for (int i = 0; i < context.allies.Length; i++)
        {
            BattleAllyController ally = context.allies[i];
            if (ally != null && !ally.IsDead)
                ally.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
        }
    }

    private static void HandlePreventLethalDamageByConsumingAllyStatusOncePerBattle(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage || effect.conditionStatusData == null)
            return;

        BattleAllyController target = context.target as BattleAllyController;
        if (target == null || target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        string relicId = relicData != null && !string.IsNullOrWhiteSpace(relicData.relicId) ? relicData.relicId : relicData.name;
        if (target.HasConsumedLethalProtection(relicId) || !target.TryConsumeLethalProtection(relicId))
            return;

        int consumed = target.RemoveStatusStacks(
            effect.conditionStatusData,
            effect.conditionStatusData.StatusId,
            Mathf.Max(1, effect.amount),
            effect.conditionStatusData.DisplayName);
        if (consumed < Mathf.Max(1, effect.amount))
            return;

        context.preventLethalDamage = true;
        context.minimumSurvivingHp = Mathf.Max(1, context.minimumSurvivingHp);

        if (effect.statusData != null)
        {
            int stacks = Mathf.Max(1, effect.maxStacks) * Mathf.Max(1, relicState.amount);
            target.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, stacks);
        }
    }

    private static bool AllLivingAlliesHaveStatus(BattleAllyController[] allies, BattleStatus_SO statusData, int requiredStacks)
    {
        if (allies == null || statusData == null)
            return false;

        bool hasLivingAlly = false;
        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;

            hasLivingAlly = true;
            if (ally.GetStatusStacks(statusData) < requiredStacks)
                return false;
        }

        return hasLivingAlly;
    }
}
