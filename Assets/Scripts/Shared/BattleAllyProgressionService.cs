using System;
using UnityEngine;

public readonly struct BattleAllyProgressionState
{
    public BattleAllyProgressionState(int finalLevel, int finalExperience, int finalMaxHP, int finalCurrentHP)
    {
        FinalLevel = finalLevel;
        FinalExperience = finalExperience;
        FinalMaxHP = finalMaxHP;
        FinalCurrentHP = finalCurrentHP;
    }

    public int FinalLevel { get; }
    public int FinalExperience { get; }
    public int FinalMaxHP { get; }
    public int FinalCurrentHP { get; }
}

public static class BattleAllyProgressionService
{
    public static BattleAllyProgressionState Evaluate(
        int initialLevel,
        int initialExperience,
        int initialCurrentHP,
        int initialMaxHP,
        int gainedExperience,
        Func<int, int> resolveExperienceToNextLevel,
        Func<int, int, int> resolveMaxHPForLevel)
    {
        int currentLevel = Mathf.Max(1, initialLevel);
        int currentExperience = Mathf.Max(0, initialExperience);
        int currentMaxHP = Mathf.Max(1, initialMaxHP);
        int currentHP = Mathf.Max(0, initialCurrentHP);
        int remaining = Mathf.Max(0, gainedExperience);

        while (remaining > 0)
        {
            int required = resolveExperienceToNextLevel != null ? resolveExperienceToNextLevel(currentLevel) : 100;
            if (required <= 0)
                break;

            currentExperience = Mathf.Clamp(currentExperience, 0, required);
            int needed = Mathf.Max(0, required - currentExperience);
            if (remaining < needed)
            {
                currentExperience += remaining;
                remaining = 0;
                break;
            }

            remaining -= needed;
            currentLevel += 1;
            currentExperience = 0;

            int upgradedMaxHP = resolveMaxHPForLevel != null
                ? resolveMaxHPForLevel(currentLevel, currentMaxHP)
                : currentMaxHP;
            currentMaxHP = Mathf.Max(1, upgradedMaxHP);
            if (currentHP > 0)
            {
                int healAmount = Mathf.CeilToInt(currentMaxHP * 0.5f);
                currentHP = Mathf.Clamp(currentHP + healAmount, 0, currentMaxHP);
            }
        }

        return new BattleAllyProgressionState(
            currentLevel,
            currentExperience,
            currentMaxHP,
            Mathf.Clamp(currentHP, 0, currentMaxHP));
    }
}
