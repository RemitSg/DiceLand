using System.Collections.Generic;

public sealed class BattleSettlementPartyProgression
{
    public CharacterData_SO CharacterData;
    public Weapon_SO WeaponData;
    public string DisplayName;
    public string FallbackName;
    public int InitialLevel;
    public int InitialExperience;
    public int InitialMaxHP;
    public int InitialCurrentHP;
    public int MaxExperience;
    public int FinalLevel;
    public int FinalExperience;
    public int FinalMaxHP;
    public int FinalCurrentHP;
    public bool IsAlive;
}

public static class BattleSettlementPartyMemberFactory
{
    public static RunPartyMemberState BuildRunState(
        string displayName,
        string fallbackName,
        CharacterData_SO characterData,
        Weapon_SO weaponData,
        int level,
        int experience,
        int maxHP,
        int currentHP)
    {
        return new RunPartyMemberState
        {
            memberId = !string.IsNullOrWhiteSpace(displayName) ? displayName : fallbackName,
            characterData = characterData,
            weaponData = weaponData,
            level = level,
            experience = experience,
            maxHP = maxHP,
            currentHP = currentHP
        };
    }

    public static BattleSettlementEntry BuildSettlementEntry(
        string displayName,
        int initialCurrentHP,
        int initialMaxHP,
        int initialLevel,
        bool isAlive,
        int initialExperience,
        int maxExperience,
        int gainedExperience,
        int finalCurrentHP,
        int finalMaxHP,
        int finalLevel,
        int finalExperience)
    {
        return new BattleSettlementEntry
        {
            displayName = displayName,
            currentHP = initialCurrentHP,
            maxHP = initialMaxHP,
            level = initialLevel,
            isAlive = isAlive,
            currentExperience = initialExperience,
            maxExperience = maxExperience,
            experienceGained = gainedExperience,
            resultingHP = finalCurrentHP,
            resultingMaxHP = finalMaxHP,
            resultingLevel = finalLevel,
            resultingExperience = finalExperience
        };
    }

    public static List<RunPartyMemberState> BuildRunStates(IEnumerable<BattleSettlementPartyProgression> progressions)
    {
        List<RunPartyMemberState> result = new List<RunPartyMemberState>();
        if (progressions == null)
            return result;

        foreach (BattleSettlementPartyProgression progression in progressions)
        {
            if (progression == null)
                continue;

            result.Add(BuildRunState(
                progression.DisplayName,
                progression.FallbackName,
                progression.CharacterData,
                progression.WeaponData,
                progression.FinalLevel,
                progression.FinalExperience,
                progression.FinalMaxHP,
                progression.FinalCurrentHP));
        }

        return result;
    }

    public static List<BattleSettlementEntry> BuildSettlementEntries(IEnumerable<BattleSettlementPartyProgression> progressions, int gainedExperience)
    {
        List<BattleSettlementEntry> result = new List<BattleSettlementEntry>();
        if (progressions == null)
            return result;

        foreach (BattleSettlementPartyProgression progression in progressions)
        {
            if (progression == null)
                continue;

            result.Add(BuildSettlementEntry(
                progression.DisplayName,
                progression.InitialCurrentHP,
                progression.InitialMaxHP,
                progression.InitialLevel,
                progression.IsAlive,
                progression.InitialExperience,
                progression.MaxExperience,
                gainedExperience,
                progression.FinalCurrentHP,
                progression.FinalMaxHP,
                progression.FinalLevel,
                progression.FinalExperience));
        }

        return result;
    }
}
