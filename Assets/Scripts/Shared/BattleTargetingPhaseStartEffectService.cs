using System;
using System.Collections.Generic;

public static class BattlePendingBattleStartAllyStatusGrantService
{
    public static bool Apply(
        BattleEncounterData encounter,
        IReadOnlyList<BattleAllyController> allies,
        Action<BattleAllyController, PendingBattleStatusGrant> logApplied)
    {
        if (encounter == null || encounter.battleStartAllyStatusGrantsApplied)
            return false;

        if (encounter.pendingBattleStartAllyStatusGrants == null
            || encounter.pendingBattleStartAllyStatusGrants.Count == 0
            || allies == null
            || allies.Count == 0)
        {
            encounter.battleStartAllyStatusGrantsApplied = true;
            return false;
        }

        bool appliedAny = false;
        BattleStatusGrantDispatcher.DispatchGrants(
            encounter.pendingBattleStartAllyStatusGrants,
            allies,
            ally => ally == null || ally.IsDead,
            (ally, grant) => ally.AddStatus(
                grant.statusData,
                grant.statusData.StatusId,
                grant.statusData.DisplayName,
                grant.stacks),
            (ally, grant) =>
            {
                appliedAny = true;
                logApplied?.Invoke(ally, grant);
            });

        encounter.battleStartAllyStatusGrantsApplied = true;
        return appliedAny;
    }
}

public static class BattleEnemyBattleStartSkillGrantService
{
    public static void Dispatch(
        IReadOnlyList<BattleEnemyController> enemies,
        IReadOnlyList<BattleAllyController> allies,
        Action<BattleEnemyController, EnemySkill_SO> logGrant)
    {
        ClearTemporarySkills(allies);
        if (enemies == null || allies == null)
            return;

        for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
        {
            BattleEnemyController enemy = enemies[enemyIndex];
            if (enemy == null || enemy.IsDead || enemy.enemyData == null)
                continue;

            List<EnemyData_SO.BattleStartAllySkillGrantEntry> grants = enemy.enemyData.battleStartGrantedAllySkills;
            if (grants == null)
                continue;

            for (int grantIndex = 0; grantIndex < grants.Count; grantIndex++)
            {
                EnemyData_SO.BattleStartAllySkillGrantEntry grant = grants[grantIndex];
                if (grant.skill == null)
                    continue;

                for (int allyIndex = 0; allyIndex < allies.Count; allyIndex++)
                {
                    BattleAllyController ally = allies[allyIndex];
                    if (ally == null || ally.IsDead)
                        continue;

                    ally.GrantTemporarySkill(grant.skill, grant.replaceWeaponSkills);
                }

                logGrant?.Invoke(enemy, grant.skill);
            }
        }
    }

    private static void ClearTemporarySkills(IReadOnlyList<BattleAllyController> allies)
    {
        if (allies == null)
            return;

        for (int i = 0; i < allies.Count; i++)
        {
            if (allies[i] != null)
                allies[i].ClearTemporarySkills();
        }
    }
}

public static class BattleEnemyTargetingStatusGainPhaseService
{
    public static void DispatchBattleStartStatusGains(
        IReadOnlyList<BattleEnemyController> enemies,
        Func<BattleEnemyController, bool> applyStatusGains,
        Action<BattleEnemyController> applyStatusDrivenTransition)
    {
        DispatchEnemyStatusGainPhase(enemies, applyStatusGains, null, applyStatusDrivenTransition);
    }

    public static void DispatchRepeatRoundStartEffects(
        int currentRound,
        IReadOnlyList<BattleEnemyController> enemies,
        Func<BattleEnemyController, bool> applyStatusGains,
        Action<BattleEnemyController> applyCustomRepeatRoundStartEffects,
        Action<BattleEnemyController> applyStatusDrivenTransition)
    {
        if (currentRound <= 1)
            return;

        DispatchEnemyStatusGainPhase(
            enemies,
            applyStatusGains,
            applyCustomRepeatRoundStartEffects,
            applyStatusDrivenTransition);
    }

    private static void DispatchEnemyStatusGainPhase(
        IReadOnlyList<BattleEnemyController> enemies,
        Func<BattleEnemyController, bool> applyStatusGains,
        Action<BattleEnemyController> applyAfterStatusGains,
        Action<BattleEnemyController> applyStatusDrivenTransition)
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.enemyData == null)
                continue;

            bool gainedStatuses = applyStatusGains != null && applyStatusGains(enemy);
            applyAfterStatusGains?.Invoke(enemy);
            if (gainedStatuses)
                applyStatusDrivenTransition?.Invoke(enemy);
        }
    }
}
