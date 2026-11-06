using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static partial class RelicSystem
{
    private static void HandleHealAllAlliesOnEnemyDeath(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnEnemyDeath || context.allies == null || context.allies.Length == 0)
            return;

        int healAllAmount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (healAllAmount <= 0)
            return;

        int healedCount = 0;
        for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
        {
            BattleAllyController allyTarget = context.allies[allyIndex];
            if (allyTarget == null || allyTarget.IsDead)
                continue;

            if (allyTarget.Heal(healAllAmount) > 0)
                healedCount += 1;
        }

        if (healedCount > 0)
            AddRelicFeedback(context, relicData, GameText.Format("relic.feedback.heal_all_allies_on_enemy_death", "敌人倒下后，{healedCount} 名友方各回复 {healAllAmount} 点生命。", ("healedCount", healedCount), ("healAllAmount", healAllAmount)));
        return;
    }

    private static void HandleHealRandomDamagedAllyOnBattleRoundEnd(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundEnd)
            return;
        if (context.allies == null || context.allies.Length == 0)
            return;

        int healAmount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (healAmount <= 0)
            return;

        List<BattleAllyController> damagedAllies = new List<BattleAllyController>();
        for (int allyIndex = 0; allyIndex < context.allies.Length; allyIndex++)
        {
            BattleAllyController allyTarget = context.allies[allyIndex];
            if (allyTarget == null || allyTarget.IsDead || allyTarget.CurrentHP >= allyTarget.MaxHP)
                continue;

            damagedAllies.Add(allyTarget);
        }

        if (damagedAllies.Count <= 0)
            return;

        BattleAllyController selectedAlly = damagedAllies[Random.Range(0, damagedAllies.Count)];
        int actualHeal = selectedAlly.Heal(healAmount);
        if (actualHeal <= 0)
            return;

        AddRelicFeedback(context, relicData, GameText.Format("relic.feedback.heal_random_damaged_ally_on_battle_round_end", "回合结束时，{actor} 恢复 {actualHeal} 点生命。", ("actor", GetActorDisplayName(selectedAlly)), ("actualHeal", actualHeal)));
        return;
    }
}
