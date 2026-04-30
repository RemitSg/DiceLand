public static class BattleRelicEventKeyService
{
    public static string BuildAllySkillUseKey(BattleAllyController attacker, int currentRound)
    {
        if (attacker == null)
            return $"ally:none:{currentRound}";

        EnemySkill_SO skill = attacker.SelectedSkill;
        int skillId = skill != null ? skill.GetInstanceID() : 0;
        return $"{attacker.GetInstanceID()}:{skillId}:{currentRound}";
    }

    public static string BuildSingleAttackTakenKey(
        BattleEnemyController attacker,
        BattleAllyController target,
        int currentRound,
        int diceIndex)
    {
        string attackerId = attacker != null ? attacker.GetInstanceID().ToString() : "none";
        string targetId = target != null ? target.GetInstanceID().ToString() : "none";
        return $"enemy:{attackerId}:ally:{targetId}:{currentRound}:{diceIndex}";
    }
}
