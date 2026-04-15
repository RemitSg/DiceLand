using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelTable", menuName = "Game/LevelTable")]
public class LevelTable_SO : ScriptableObject
{
    [Serializable]
    public class LevelRequirement
    {
        [Min(1)] public int level = 1;
        [Min(0)] public int requiredExperience = 0;
    }

    [Serializable]
    public class CharacterMaxHPEntry
    {
        [Min(1)] public int level = 1;
        [Min(1)] public int maxHP = 1;
    }

    [Serializable]
    public class CharacterMaxHPTable
    {
        public CharacterData_SO characterData;
        [SerializeField] private List<CharacterMaxHPEntry> maxHPByLevel = new List<CharacterMaxHPEntry>();

        public IReadOnlyList<CharacterMaxHPEntry> MaxHPByLevel => maxHPByLevel;

        public int GetMaxHPForLevel(int level)
        {
            if (level <= 0)
                return 0;

            if (maxHPByLevel == null || maxHPByLevel.Count == 0)
                return characterData != null ? Mathf.Max(1, characterData.baseHP) : 1;

            CharacterMaxHPEntry best = null;
            for (int i = 0; i < maxHPByLevel.Count; i++)
            {
                CharacterMaxHPEntry entry = maxHPByLevel[i];
                if (entry == null)
                    continue;

                if (entry.level > level)
                    break;

                best = entry;
            }

            if (best != null)
                return Mathf.Max(1, best.maxHP);

            return characterData != null ? Mathf.Max(1, characterData.baseHP) : 1;
        }

        public void Validate()
        {
            if (maxHPByLevel == null)
            {
                maxHPByLevel = new List<CharacterMaxHPEntry>();
                return;
            }

            if (maxHPByLevel.Count == 0 && characterData != null)
            {
                maxHPByLevel.Add(new CharacterMaxHPEntry
                {
                    level = 1,
                    maxHP = Mathf.Max(1, characterData.baseHP)
                });
            }

            for (int i = 0; i < maxHPByLevel.Count; i++)
            {
                if (maxHPByLevel[i] == null)
                    maxHPByLevel[i] = new CharacterMaxHPEntry();
            }

            maxHPByLevel.Sort((a, b) => a.level.CompareTo(b.level));

            for (int i = 0; i < maxHPByLevel.Count; i++)
            {
                CharacterMaxHPEntry entry = maxHPByLevel[i];
                entry.level = Mathf.Max(1, entry.level);
                entry.maxHP = Mathf.Max(1, entry.maxHP);

                if (i == 0)
                    continue;

                int expectedLevel = maxHPByLevel[i - 1].level + 1;
                entry.level = Mathf.Max(expectedLevel, entry.level);
            }
        }
    }

    [Header("Level Requirements")]
    [SerializeField] private List<LevelRequirement> levelRequirements = new List<LevelRequirement>
    {
        new LevelRequirement { level = 1, requiredExperience = 0 },
        new LevelRequirement { level = 2, requiredExperience = 10 },
        new LevelRequirement { level = 3, requiredExperience = 20 },
        new LevelRequirement { level = 4, requiredExperience = 35 },
        new LevelRequirement { level = 5, requiredExperience = 55 }
    };

    [Header("Character Max HP Tables")]
    [SerializeField] private List<CharacterMaxHPTable> characterMaxHPTables = new List<CharacterMaxHPTable>();

    public IReadOnlyList<LevelRequirement> LevelRequirements => levelRequirements;
    public IReadOnlyList<CharacterMaxHPTable> CharacterMaxHPTables => characterMaxHPTables;

    public int MaxConfiguredLevel => levelRequirements != null && levelRequirements.Count > 0
        ? levelRequirements[levelRequirements.Count - 1].level
        : 1;

    public int GetRequiredExperienceForLevel(int level)
    {
        if (level <= 1 || levelRequirements == null || levelRequirements.Count == 0)
            return 0;

        for (int i = 0; i < levelRequirements.Count; i++)
        {
            LevelRequirement requirement = levelRequirements[i];
            if (requirement != null && requirement.level == level)
                return Mathf.Max(0, requirement.requiredExperience);
        }

        return 0;
    }

    public int GetExperienceToNextLevel(int currentLevel)
    {
        return GetRequiredExperienceForLevel(Mathf.Max(1, currentLevel) + 1);
    }

    public int GetCumulativeExperienceForLevel(int level)
    {
        if (level <= 1 || levelRequirements == null || levelRequirements.Count == 0)
            return 0;

        int total = 0;
        for (int i = 0; i < levelRequirements.Count; i++)
        {
            LevelRequirement requirement = levelRequirements[i];
            if (requirement == null)
                continue;

            if (requirement.level > level)
                break;

            total += Mathf.Max(0, requirement.requiredExperience);
        }

        return total;
    }

    public int GetLevelForExperience(int totalExperience)
    {
        int safeExperience = Mathf.Max(0, totalExperience);
        int level = 1;
        int remaining = safeExperience;

        if (levelRequirements == null || levelRequirements.Count == 0)
            return level;

        for (int i = 0; i < levelRequirements.Count; i++)
        {
            LevelRequirement requirement = levelRequirements[i];
            if (requirement == null || requirement.level <= 1)
                continue;

            int required = Mathf.Max(0, requirement.requiredExperience);
            if (remaining < required)
                break;

            remaining -= required;
            level = Mathf.Max(level, requirement.level);
        }

        return level;
    }

    public int GetMaxHP(CharacterData_SO characterData, int level)
    {
        return GetMaxHP(characterData, level, characterData != null ? characterData.baseHP : 1);
    }

    public int GetMaxHP(CharacterData_SO characterData, int level, int fallbackMaxHP)
    {
        if (TryGetCharacterMaxHPTable(characterData, out CharacterMaxHPTable table))
            return table.GetMaxHPForLevel(level);

        return Mathf.Max(1, fallbackMaxHP);
    }

    public bool TryGetCharacterMaxHPTable(CharacterData_SO characterData, out CharacterMaxHPTable foundTable)
    {
        foundTable = null;
        if (characterData == null || characterMaxHPTables == null)
            return false;

        for (int i = 0; i < characterMaxHPTables.Count; i++)
        {
            CharacterMaxHPTable table = characterMaxHPTables[i];
            if (table == null || table.characterData != characterData)
                continue;

            foundTable = table;
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (levelRequirements == null)
        {
            levelRequirements = new List<LevelRequirement>();
            return;
        }

        if (levelRequirements.Count == 0)
            levelRequirements.Add(new LevelRequirement { level = 1, requiredExperience = 0 });

        for (int i = 0; i < levelRequirements.Count; i++)
        {
            if (levelRequirements[i] == null)
                levelRequirements[i] = new LevelRequirement();
        }

        levelRequirements.Sort((a, b) => a.level.CompareTo(b.level));

        if (levelRequirements[0].level != 1)
            levelRequirements.Insert(0, new LevelRequirement { level = 1, requiredExperience = 0 });

        for (int i = 0; i < levelRequirements.Count; i++)
        {
            LevelRequirement requirement = levelRequirements[i];
            requirement.level = Mathf.Max(1, requirement.level);
            requirement.requiredExperience = Mathf.Max(0, requirement.requiredExperience);

            if (i == 0)
            {
                requirement.level = 1;
                requirement.requiredExperience = 0;
                continue;
            }

            int expectedLevel = levelRequirements[i - 1].level + 1;
            requirement.level = Mathf.Max(expectedLevel, requirement.level);
        }

        if (characterMaxHPTables == null)
        {
            characterMaxHPTables = new List<CharacterMaxHPTable>();
            return;
        }

        for (int i = 0; i < characterMaxHPTables.Count; i++)
        {
            if (characterMaxHPTables[i] == null)
                characterMaxHPTables[i] = new CharacterMaxHPTable();

            characterMaxHPTables[i].Validate();
        }
    }
}
