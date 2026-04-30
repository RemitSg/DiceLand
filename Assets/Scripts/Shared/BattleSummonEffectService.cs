using UnityEngine;

public static class BattleSummonEffectService
{
    public static bool ShouldProcess(
        EnemySkill_SO.SummonEnemyEffectEntry effect,
        EnemySkill_SO.DiceEffectTrigger trigger)
    {
        return effect.trigger == trigger && effect.enemyData != null;
    }

    public static int NormalizeSummonCount(int summonCount)
    {
        return Mathf.Max(1, summonCount);
    }

    public static string BuildLabel(EnemySkill_SO.SummonEnemyEffectEntry effect, int effectIndex)
    {
        if (!string.IsNullOrWhiteSpace(effect.effectName))
            return effect.effectName;
        if (effect.enemyData != null && !string.IsNullOrWhiteSpace(effect.enemyData.enemyName))
            return effect.enemyData.enemyName;
        return $"鍙敜鏁堟灉#{effectIndex + 1}";
    }
}
