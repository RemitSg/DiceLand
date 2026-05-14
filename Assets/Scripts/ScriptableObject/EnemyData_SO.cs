using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New EnemyData", menuName = "Enemies/EnemyData")]
public class EnemyData_SO : ScriptableObject
{
    public enum PassiveDisplayColorType
    {
        Red = 0,
        Orange = 1,
        Yellow = 2
    }

    [System.Serializable]
    public struct PassiveDescriptionEntry
    {
        public string passiveName;
        public PassiveDisplayColorType displayColorType;
        [TextArea(2, 5)] public string description;
    }

    [System.Serializable]
    public struct EnemyStageEntry
    {
        public string stageName;
        public Sprite stageSprite;
        [Min(0)] public int minSpeedOverride;
        [Min(0)] public int maxSpeedOverride;
        [Min(0)] public int speedButtonCountOverride;
    }

    public enum ConditionalSkillConditionType
    {
        HealthBelowPercent = 0,
        RoundSequence = 1,
        AliveEnemyCountAtMost = 2
    }

    public enum StageStatusTargetMode
    {
        Self = 0,
        AnyUnit = 1,
        SpecificEnemy = 2,
        SpecificAlly = 3
    }

    public enum StageStatusComparisonType
    {
        GreaterThan = 0,
        GreaterOrEqual = 1,
        LessThan = 2,
        LessOrEqual = 3
    }

    [System.Serializable]
    public struct ConditionalSkillRule
    {
        public string ruleName;
        public EnemySkill_SO skill;
        public ConditionalSkillConditionType conditionType;
        [Range(0f, 1f)] public float healthThresholdNormalized;
        [Min(1)] public int firstTriggerRound;
        [Min(1)] public int repeatEveryNRounds;
        [Min(0)] public int priority;
        [Min(1)] public int maxSlotsPerRound;
        public bool useOncePerBattle;
        [Min(1)] public int requiredAliveEnemyCount;
    }

    [System.Serializable]
    public struct StatusStageTransitionRule
    {
        public string ruleName;
        public BattleStatus_SO statusData;
        public StageStatusTargetMode targetMode;
        public EnemyData_SO specificEnemyData;
        public CharacterData_SO specificCharacterData;
        public StageStatusComparisonType comparisonType;
        [Min(0)] public int stackThreshold;
        [Min(0)] public int targetStageIndex;
        [Min(0)] public int priority;
    }

    [System.Serializable]
    public struct StatusImmunityRule
    {
        public BattleStatus_SO statusData;
        [Tooltip("When enabled, this status can still be applied and stacked, but all of its gameplay effects are ignored.")]
        public bool allowApplicationWhileImmune;
    }

    [System.Serializable]
    public struct BattleStartAllySkillGrantEntry
    {
        public EnemySkill_SO skill;
        [Tooltip("When enabled, granted temporary skills replace the ally weapon skill pool for this battle.")]
        public bool replaceWeaponSkills;
    }

    [System.Serializable]
    public struct OnSpecificEnemyDeathStatusGainEntry
    {
        public EnemyData_SO watchedEnemyData;
        public BattleStatus_SO statusData;
        [Min(1)] public int stackAmount;
        [Min(0)]
        [Tooltip("How many additional future round starts should also grant the same status stacks after the immediate application.")]
        public int additionalRoundStartApplications;
    }

    [System.Serializable]
    public struct StatusGainEntry
    {
        public BattleStatus_SO statusData;
        [Min(1)] public int stackAmount;
    }

    [System.Serializable]
    public struct RoundStartRetreatRule
    {
        [Tooltip("Enable this rule to make the enemy leave combat at the start of a fixed round.")]
        public bool enabled;
        [Min(1)] public int triggerRound;
        [Tooltip("Optional loot tables used when this enemy retreats and immediately settles the battle. When empty, the enemy group loot tables are used.")]
        public List<LootTable_SO> settlementLootTables;
    }

    public int enemyID;
    public string enemyName;
    public int baseHP;
    [Range(0f, 100f)]
    [Tooltip("Flat damage reduction percentage applied to all incoming damage after other damage calculations are finished.")]
    public float damageReductionPercent;
    public int minSpeed;
    public int maxSpeed;
    public int baseSpeedButtonCount;
    [Tooltip("Sprite used by this enemy in battle.")]
    public Sprite enemySprite;
    [Tooltip("Optional stage sprites. When empty, the enemy uses the single enemySprite field.")]
    public List<EnemyStageEntry> stageSprites;

    [Header("Health-Based Stage Transition")]
    [Tooltip("Enable automatic stage transition when HP falls below the threshold.")]
    public bool enableHealthBasedStageTransition;
    [Range(0f, 1f)]
    [Tooltip("Trigger threshold based on remaining HP ratio.")]
    public float healthBasedTransitionThresholdNormalized = 0.2f;
    [Tooltip("Which stage index should be entered after the threshold is reached.")]
    public int healthBasedTransitionTargetStageIndex = 1;
    [Tooltip("When enabled, damage that crosses the threshold will stop at the threshold HP until the next-round transition happens.")]
    public bool lockHPAtHealthTransitionThreshold;

    [Header("Status-Based Stage Transition")]
    [Tooltip("Optional stage transition rules evaluated from battlefield status stacks.")]
    public List<StatusStageTransitionRule> statusStageTransitionRules;

    [Header("Status Immunity")]
    [Tooltip("Statuses listed here are immune on this enemy. They can optionally still be applied without taking effect.")]
    public List<StatusImmunityRule> statusImmunityRules;

    [Header("Battle Start Effects")]
    [Tooltip("Temporary skills granted to all allied characters when this enemy enters battle.")]
    public List<BattleStartAllySkillGrantEntry> battleStartGrantedAllySkills;
    [Tooltip("Statuses granted to this enemy once when the battle begins.")]
    public List<StatusGainEntry> battleStartStatusGains;

    [Header("On Enemy Death Effects")]
    [Tooltip("When the specified enemy dies during this battle, this enemy gains the configured status stacks.")]
    public List<OnSpecificEnemyDeathStatusGainEntry> onSpecificEnemyDeathStatusGains;

    [Header("Round Start Status Gains")]
    [Tooltip("Statuses granted to this enemy at the start of each round after round 1.")]
    public List<StatusGainEntry> repeatRoundStartStatusGains;

    [Header("Round Start Retreat")]
    [Tooltip("Optional retreat rule. When triggered, the encounter settles immediately with the configured loot tables.")]
    public RoundStartRetreatRule roundStartRetreatRule;

    [Header("Battlefield Aura")]
    [Tooltip("When enabled, allied characters keep their bleed stacks after being hit while this enemy remains alive on the battlefield.")]
    public bool preventAllyBleedDecayOnHitWhileAlive;

    [Header("Last Stand")]
    [Tooltip("When enabled, lethal damage leaves this enemy at 1 HP until every other living ally is also at 1 HP or lower.")]
    public bool surviveAtOneHpUntilAllLivingAlliesAtOrBelowOneHp;

    [Header("Round Start Transform")]
    [Tooltip("Optional enemy data this enemy turns into at round start when the required status is missing.")]
    public EnemyData_SO transformToEnemyDataWhenStatusMissing;
    [Tooltip("Status that must be present to prevent the round start transform.")]
    public BattleStatus_SO transformMissingStatusData;

    [TextArea(2, 5)]
    public string enemyDescription;
    public List<PassiveDescriptionEntry> passiveDescriptions;
    [Min(0.01f)]
    [Tooltip("Visual scale multiplier applied to this enemy sprite in battle.")]
    public float enemyVisualScale = 1f;
    [Tooltip("Additional local offset applied to this enemy sprite in battle.")]
    public Vector3 enemyVisualOffset = Vector3.zero;
    [Tooltip("Additional local offset applied to this enemy's speed buttons in battle. Useful for large or floating enemies.")]
    public Vector2 speedButtonOffset = Vector2.zero;
    [Tooltip("Normal random skill pool used when no conditional rule is selected.")]
    public List<EnemySkill_SO> skillPool;
    [Tooltip("Special skill rules checked before the normal random pool.")]
    public List<ConditionalSkillRule> conditionalSkillRules;

    public int StageCount => stageSprites != null && stageSprites.Count > 0 ? stageSprites.Count : 1;
    public int MaxStageSpeedButtonCount
    {
        get
        {
            int maxCount = Mathf.Max(1, baseSpeedButtonCount);
            if (stageSprites != null)
            {
                for (int i = 0; i < stageSprites.Count; i++)
                    maxCount = Mathf.Max(maxCount, ResolveStageSpeedButtonCount(stageSprites[i]));
            }

            return maxCount;
        }
    }

    public Sprite GetStageSprite(int stageIndex)
    {
        if (stageSprites != null && stageSprites.Count > 0)
        {
            int safeIndex = Mathf.Clamp(stageIndex, 0, stageSprites.Count - 1);
            Sprite sprite = stageSprites[safeIndex].stageSprite;
            if (sprite != null)
                return sprite;
        }

        return enemySprite;
    }

    public int GetStageMinSpeed(int stageIndex)
    {
        if (stageSprites != null && stageSprites.Count > 0)
        {
            int safeIndex = Mathf.Clamp(stageIndex, 0, stageSprites.Count - 1);
            EnemyStageEntry stage = stageSprites[safeIndex];
            if (stage.minSpeedOverride > 0)
                return stage.minSpeedOverride;
        }

        return Mathf.Max(0, minSpeed);
    }

    public int GetStageMaxSpeed(int stageIndex)
    {
        int resolvedMin = GetStageMinSpeed(stageIndex);
        if (stageSprites != null && stageSprites.Count > 0)
        {
            int safeIndex = Mathf.Clamp(stageIndex, 0, stageSprites.Count - 1);
            EnemyStageEntry stage = stageSprites[safeIndex];
            if (stage.maxSpeedOverride > 0)
                return Mathf.Max(resolvedMin, stage.maxSpeedOverride);
        }

        return Mathf.Max(resolvedMin, maxSpeed);
    }

    public int GetStageSpeedButtonCount(int stageIndex)
    {
        if (stageSprites != null && stageSprites.Count > 0)
        {
            int safeIndex = Mathf.Clamp(stageIndex, 0, stageSprites.Count - 1);
            return ResolveStageSpeedButtonCount(stageSprites[safeIndex]);
        }

        return Mathf.Max(1, baseSpeedButtonCount);
    }

    private void OnValidate()
    {
        enemyVisualScale = Mathf.Max(0.01f, enemyVisualScale);
        damageReductionPercent = Mathf.Clamp(damageReductionPercent, 0f, 100f);
        healthBasedTransitionThresholdNormalized = Mathf.Clamp01(healthBasedTransitionThresholdNormalized);
        healthBasedTransitionTargetStageIndex = Mathf.Max(0, healthBasedTransitionTargetStageIndex);

        if (skillPool == null)
            skillPool = new List<EnemySkill_SO>();
        if (stageSprites == null)
            stageSprites = new List<EnemyStageEntry>();
        for (int i = 0; i < stageSprites.Count; i++)
        {
            EnemyStageEntry stage = stageSprites[i];
            stage.minSpeedOverride = Mathf.Max(0, stage.minSpeedOverride);
            stage.maxSpeedOverride = Mathf.Max(0, stage.maxSpeedOverride);
            stage.speedButtonCountOverride = Mathf.Max(0, stage.speedButtonCountOverride);
            if (stage.maxSpeedOverride > 0 && stage.minSpeedOverride > stage.maxSpeedOverride)
                stage.maxSpeedOverride = stage.minSpeedOverride;
            stageSprites[i] = stage;
        }
        if (passiveDescriptions == null)
            passiveDescriptions = new List<PassiveDescriptionEntry>();
        if (conditionalSkillRules == null)
            conditionalSkillRules = new List<ConditionalSkillRule>();
        if (statusStageTransitionRules == null)
            statusStageTransitionRules = new List<StatusStageTransitionRule>();
        if (statusImmunityRules == null)
            statusImmunityRules = new List<StatusImmunityRule>();
        if (battleStartGrantedAllySkills == null)
            battleStartGrantedAllySkills = new List<BattleStartAllySkillGrantEntry>();
        if (battleStartStatusGains == null)
            battleStartStatusGains = new List<StatusGainEntry>();
        if (onSpecificEnemyDeathStatusGains == null)
            onSpecificEnemyDeathStatusGains = new List<OnSpecificEnemyDeathStatusGainEntry>();
        if (repeatRoundStartStatusGains == null)
            repeatRoundStartStatusGains = new List<StatusGainEntry>();
        if (roundStartRetreatRule.settlementLootTables == null)
            roundStartRetreatRule.settlementLootTables = new List<LootTable_SO>();
        roundStartRetreatRule.triggerRound = Mathf.Max(1, roundStartRetreatRule.triggerRound);

        for (int i = 0; i < conditionalSkillRules.Count; i++)
        {
            ConditionalSkillRule rule = conditionalSkillRules[i];
            rule.healthThresholdNormalized = Mathf.Clamp01(rule.healthThresholdNormalized);
            rule.firstTriggerRound = Mathf.Max(1, rule.firstTriggerRound);
            rule.repeatEveryNRounds = Mathf.Max(1, rule.repeatEveryNRounds);
            rule.priority = Mathf.Max(0, rule.priority);
            rule.maxSlotsPerRound = Mathf.Max(1, rule.maxSlotsPerRound);
            rule.requiredAliveEnemyCount = Mathf.Max(1, rule.requiredAliveEnemyCount);
            conditionalSkillRules[i] = rule;
        }

        for (int i = 0; i < statusStageTransitionRules.Count; i++)
        {
            StatusStageTransitionRule rule = statusStageTransitionRules[i];
            rule.stackThreshold = Mathf.Max(0, rule.stackThreshold);
            rule.targetStageIndex = Mathf.Max(0, rule.targetStageIndex);
            rule.priority = Mathf.Max(0, rule.priority);
            statusStageTransitionRules[i] = rule;
        }

        for (int i = 0; i < battleStartStatusGains.Count; i++)
        {
            StatusGainEntry entry = battleStartStatusGains[i];
            entry.stackAmount = Mathf.Max(1, entry.stackAmount);
            battleStartStatusGains[i] = entry;
        }

        for (int i = 0; i < onSpecificEnemyDeathStatusGains.Count; i++)
        {
            OnSpecificEnemyDeathStatusGainEntry entry = onSpecificEnemyDeathStatusGains[i];
            entry.stackAmount = Mathf.Max(1, entry.stackAmount);
            entry.additionalRoundStartApplications = Mathf.Max(0, entry.additionalRoundStartApplications);
            onSpecificEnemyDeathStatusGains[i] = entry;
        }

        for (int i = 0; i < repeatRoundStartStatusGains.Count; i++)
        {
            StatusGainEntry entry = repeatRoundStartStatusGains[i];
            entry.stackAmount = Mathf.Max(1, entry.stackAmount);
            repeatRoundStartStatusGains[i] = entry;
        }
    }

    private int ResolveStageSpeedButtonCount(EnemyStageEntry stage)
    {
        return stage.speedButtonCountOverride > 0
            ? Mathf.Max(1, stage.speedButtonCountOverride)
            : Mathf.Max(1, baseSpeedButtonCount);
    }
}
