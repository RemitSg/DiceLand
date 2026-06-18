using System.Collections.Generic;
using System;
using UnityEngine;

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
    public int PreCalamityMaxHP;
    public int MaxHPFlatBonus;
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
        int currentHP,
        int preCalamityMaxHP = 0,
        int maxHPFlatBonus = 0)
    {
        return new RunPartyMemberState
        {
            memberId = !string.IsNullOrWhiteSpace(displayName) ? displayName : fallbackName,
            characterData = characterData,
            weaponData = weaponData,
            level = level,
            experience = experience,
            maxHPFlatBonus = maxHPFlatBonus,
            preCalamityMaxHP = preCalamityMaxHP > 0 ? preCalamityMaxHP : maxHP,
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
                progression.FinalCurrentHP,
                progression.PreCalamityMaxHP,
                progression.MaxHPFlatBonus));
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

    public static List<BattleSettlementPartyProgression> BuildProgressions(
        IEnumerable<BattleAllyController> allies,
        int experienceReward,
        Func<int, int> resolveExperienceToNextLevel,
        Func<CharacterData_SO, int, int, int> resolveMaxHPForLevel)
    {
        List<BattleSettlementPartyProgression> progressions = new List<BattleSettlementPartyProgression>();
        if (allies == null)
            return progressions;

        foreach (BattleAllyController ally in allies)
        {
            if (ally == null)
                continue;

            progressions.Add(EvaluateAllyProgression(
                ally,
                Math.Max(0, experienceReward),
                resolveExperienceToNextLevel,
                resolveMaxHPForLevel));
        }

        return progressions;
    }

    private static BattleSettlementPartyProgression EvaluateAllyProgression(
        BattleAllyController ally,
        int experienceReward,
        Func<int, int> resolveExperienceToNextLevel,
        Func<CharacterData_SO, int, int, int> resolveMaxHPForLevel)
    {
        AllyController allyController = ally != null ? ally.GetComponent<AllyController>() : null;
        if (allyController == null && ally != null)
            allyController = ally.GetComponentInParent<AllyController>();

        CharacterData_SO characterData = allyController != null ? allyController.characterData : null;
        Weapon_SO weaponData = allyController != null && allyController.weapon != null
            ? allyController.weapon.weaponData
            : null;

        int initialLevel = ally != null ? Mathf.Max(1, ally.Level) : 1;
        int initialExperience = ally != null ? Mathf.Max(0, ally.Experience) : 0;
        int persistentMaxHP = ally != null ? Mathf.Max(1, ally.PersistentMaxHP) : 1;
        int persistentMaxHPFlatBonus = ally != null ? ally.PersistentMaxHPFlatBonus : 0;
        int initialMaxHP = ally != null ? Mathf.Max(1, ally.MaxHP) : 1;
        int initialCurrentHP = ally != null ? Mathf.Max(0, ally.CurrentHP) : 0;
        BattleSettlementPartyProgression result = new BattleSettlementPartyProgression
        {
            CharacterData = characterData,
            WeaponData = weaponData,
            DisplayName = ally != null ? ally.GetSettlementDisplayName() : string.Empty,
            FallbackName = ally != null ? ally.gameObject.name : string.Empty,
            InitialLevel = initialLevel,
            InitialExperience = initialExperience,
            InitialMaxHP = initialMaxHP,
            InitialCurrentHP = initialCurrentHP,
            PreCalamityMaxHP = persistentMaxHP,
            MaxHPFlatBonus = persistentMaxHPFlatBonus,
            MaxExperience = resolveExperienceToNextLevel != null ? resolveExperienceToNextLevel(initialLevel) : 100,
            IsAlive = ally != null && !ally.IsDead
        };

        BattleAllyProgressionState progression = BattleAllyProgressionService.Evaluate(
            result.InitialLevel,
            result.InitialExperience,
            result.InitialCurrentHP,
            persistentMaxHP,
            experienceReward,
            resolveExperienceToNextLevel ?? (_ => 100),
            (level, fallbackMaxHP) =>
            {
                int currentBaseMaxHP = Mathf.Max(1, fallbackMaxHP - persistentMaxHPFlatBonus);
                int upgradedBaseMaxHP = resolveMaxHPForLevel != null
                    ? resolveMaxHPForLevel(characterData, level, currentBaseMaxHP)
                    : currentBaseMaxHP;
                return Mathf.Max(1, upgradedBaseMaxHP + persistentMaxHPFlatBonus);
            });

        result.FinalLevel = progression.FinalLevel;
        result.FinalExperience = progression.FinalExperience;
        result.PreCalamityMaxHP = progression.FinalMaxHP;
        result.MaxHPFlatBonus = persistentMaxHPFlatBonus;
        IReadOnlyList<RunCalamityState> activeCalamities = BattleEncounterRuntime.ActiveEncounter != null
            ? BattleEncounterRuntime.ActiveEncounter.activeCalamities
            : null;
        result.FinalMaxHP = CalamitySystem.ApplyAllyMaxHpModifiers(progression.FinalMaxHP, activeCalamities);
        result.FinalCurrentHP = Mathf.Min(progression.FinalCurrentHP, result.FinalMaxHP);
        if (result.FinalCurrentHP > result.FinalMaxHP)
            result.FinalCurrentHP = result.FinalMaxHP;
        return result;
    }
}
