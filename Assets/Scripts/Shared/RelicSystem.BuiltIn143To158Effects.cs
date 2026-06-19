using System.Collections.Generic;
using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleRefundEnergyOnClashLostOncePerSkillPerRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context == null || context.skillData == null || context.skillData.energyCost <= 0)
            return;

        int energy = Mathf.Max(0, effect.amount);
        if (energy <= 0)
            return;

        string sourceKey = GetObjectKey(context.source);
        string skillKey = context.skillData.GetInstanceID().ToString();
        string procKey = BuildRelicProcKey(relicData, $"energy-refund:{context.currentRound}:{sourceKey}:{skillKey}");
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter != null && !activeEncounter.TryRegisterBattleRelicProc(procKey))
            return;

        context.additionalSharedEnergyRecovery += energy;
        AddRelicFeedback(context, relicData, $"拼点失败，返还 {energy} 点能量。");
    }

    private static void HandleMatchLowestAllySpeedToHighestOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        BattleAllyController lowest = null;
        BattleAllyController highest = null;
        BattleAllyController[] allies = context != null ? context.allies : null;
        if (allies == null || allies.Length == 0)
            return;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;

            if (lowest == null || ally.Speed < lowest.Speed)
                lowest = ally;
            if (highest == null || ally.Speed > highest.Speed)
                highest = ally;
        }

        if (lowest == null || highest == null || lowest == highest || lowest.Speed >= highest.Speed)
            return;

        int targetSpeed = Mathf.Max(1, highest.Speed);
        lowest.SetRoundSpeed(targetSpeed);
        AddRelicFeedback(context, relicData, $"{lowest.GetDebugDisplayName()} 的速度提升至 {targetSpeed}。");
    }

    private static void HandleApplyStatusToLowestHpAllyOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context == null || effect == null || effect.statusData == null)
            return;

        BattleAllyController target = ResolveLowestHpLivingAlly(context.allies, null);
        if (target == null)
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        if (TryApplyStatus(target, effect, stacks))
            AddRelicFeedback(context, relicData, $"{target.GetDebugDisplayName()} 获得 {stacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleDamageRandomEnemyWhenAllyLosesShieldStacks(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context == null || context.statusStacks <= 0)
            return;

        List<BattleEnemyController> livingEnemies = new List<BattleEnemyController>();
        BattleEnemyController[] enemies = context.enemies;
        if (enemies == null || enemies.Length == 0)
            enemies = Object.FindObjectsOfType<BattleEnemyController>(false);

        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null && !enemy.IsDead)
                livingEnemies.Add(enemy);
        }

        if (livingEnemies.Count == 0)
            return;

        int damage = Mathf.Max(0, context.statusStacks * Mathf.Max(1, effect.amount));
        if (damage <= 0)
            return;

        BattleEnemyController target = livingEnemies[Random.Range(0, livingEnemies.Count)];
        int dealt = target.TakeDamage(damage, 0, damage);
        if (dealt > 0)
            AddRelicFeedback(context, relicData, $"{target.GetDebugDisplayName()} 因护盾反击失去 {dealt} 点生命。");
    }

    private static void HandleModifyDefenseDiceMinPowerWhenSelfHasStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context == null || effect == null || effect.conditionStatusData == null)
            return;
        if (!IsDefenseSkill(context.skillData))
            return;
        if (!(context.source is BattleAllyController ally) ||
            ally.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int bonus = effect.minPowerAmount != 0 ? effect.minPowerAmount : effect.amount;
        if (bonus == 0)
            return;

        context.additionalMinPowerBonus += bonus;
    }

    private static void HandleGrantRandomRelicsAndBlueprintsOnAcquire(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context))
            return;

        System.Random rng = new System.Random();
        int acquisitionMultiplier = Mathf.Max(1, context.acquiredRelicAmount);
        int relicCount = Mathf.Max(0, effect.amount * acquisitionMultiplier);
        int blueprintCount = Mathf.Max(0, effect.maxStacks * acquisitionMultiplier);

        HashSet<Relic_SO> excludedRelics = BuildOwnedRelicSet(context.runState);
        for (int i = 0; i < relicCount; i++)
        {
            Relic_SO selected = effect.relicTable != null
                ? effect.relicTable.DrawRandomRelic(rng, excludedRelics)
                : null;
            if (selected == null)
                break;

            excludedRelics.Add(selected);
            context.runState.AddRelic(selected, 1);
        }

        HashSet<CraftingBlueprint_SO> excludedBlueprints = BuildOwnedBlueprintSet(context.runState);
        for (int i = 0; i < blueprintCount; i++)
        {
            CraftingBlueprint_SO selected = BlueprintRewardRollService.DrawRandomBlueprint(
                effect.blueprintTable,
                effect.blueprintRarityWeightTable,
                rng,
                excludedBlueprints);
            if (selected == null)
                break;

            excludedBlueprints.Add(selected);
            context.runState.AddBlueprint(selected, 1);
        }
    }

    private static void HandleEnableDiagonalMapMovement(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context))
            return;

        context.runState.allowDiagonalMapMovement = true;
    }

    private static void HandleKeepOnlyInitialPartyMemberAndDisableRecruitOnAcquire(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context))
            return;
        if (context.runState.partyMembers == null || context.runState.partyMembers.Count == 0)
            return;

        RunPartyMemberState initialMember = context.runState.partyMembers[0];
        context.runState.partyMembers.Clear();
        context.runState.partyMembers.Add(initialMember);
        context.runState.recruitmentDisabled = true;

        int hpBonus = Mathf.Max(0, effect.amount * Mathf.Max(1, context.acquiredRelicAmount));
        if (hpBonus > 0)
            RunPartyMemberMaxHpService.AddPreCalamityMaxHp(initialMember, hpBonus, context.runState.activeCalamities);

        context.runState.SyncLegacyPlayerFieldsFromParty();
    }

    private static void HandleChanceApplyStatusToAllAlliesOnBattleRoundStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context == null || context.allies == null || effect == null || effect.statusData == null)
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (stacks <= 0)
            return;

        int affected = 0;
        for (int i = 0; i < context.allies.Length; i++)
        {
            BattleAllyController ally = context.allies[i];
            if (ally == null || ally.IsDead)
                continue;

            if (TryApplyStatus(ally, effect, stacks))
                affected++;
        }

        if (affected > 0)
            AddRelicFeedback(context, relicData, $"{affected} 名友方获得 {stacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleDoubleFirstEnemyDamageEachBattle(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context == null || context.outgoingDamage <= 0 || !(context.target is BattleEnemyController))
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        string procKey = BuildRelicProcKey(relicData, "first-enemy-damage");
        if (activeEncounter != null && !activeEncounter.TryRegisterBattleRelicProc(procKey))
            return;

        int percent = Mathf.Max(100, effect.amount);
        context.outgoingDamagePercentModifier += percent;
        context.AddOutgoingDamagePercentModifierSource(percent, GetRelicDisplayName(relicData));
    }

    private static void HandleResetKeysAndRegenerateMapOnAcquire(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context))
            return;

        context.runState.currentKeys = 0;
        context.runState.pendingMapRegenerationFromRelic = true;
    }

    private static void HandleConfigureFixedRelicReforgePair(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (!IsAcquiredRelicEffectActive(relicData, context) || effect.relicData == null)
            return;

        context.runState.fixedRelicReforgeFrom = relicData;
        context.runState.fixedRelicReforgeTo = effect.relicData;
    }

    private static HashSet<CraftingBlueprint_SO> BuildOwnedBlueprintSet(RunState runState)
    {
        HashSet<CraftingBlueprint_SO> owned = new HashSet<CraftingBlueprint_SO>();
        if (runState == null || runState.blueprintInventory == null)
            return owned;

        for (int i = 0; i < runState.blueprintInventory.Count; i++)
        {
            RunBlueprintState state = runState.blueprintInventory[i];
            if (state != null && state.blueprintData != null && state.amount > 0)
                owned.Add(state.blueprintData);
        }

        return owned;
    }

    private static bool IsDefenseSkill(EnemySkill_SO skillData)
    {
        return skillData != null && skillData.IsDefenseSkill;
    }
}
