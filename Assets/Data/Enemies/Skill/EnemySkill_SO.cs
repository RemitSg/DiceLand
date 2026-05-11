using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New EnemySkill", menuName = "Enemies/EnemySkill")]
public class EnemySkill_SO : ScriptableObject
{
    public enum DiceEffectTrigger
    {
        OnHit = 0,
        OnMiss = 1,
        OnClash = 2,
        OnSkillUse = 3,
        OnClashWin = 4,
        OnSingleAttack = 5,
        OnAllDiceHit = 6
    }

    public enum DiceStatusType
    {
        Burn = 0,
        Electro = 1
    }

    public enum StatusApplyTiming
    {
        Immediate = 0,
        NextRoundStart = 1
    }

    public enum DamageTargetMode
    {
        CurrentTarget = 0,
        RandomOpponents = 1,
        AllOpponents = 2,
        Self = 3,
        AllFriendlies = 4,
        LowestHpFriendly = 5
    }

    public enum HealAmountMode
    {
        FixedAmount = 0,
        DamageDealt = 1,
        StatusStacks = 2
    }

    public enum BuiltInEffectType
    {
        Heal = 0,
        Damage = 1,
        ApplyStatus = 2,
        TransferRandomNegativeStatus = 3,
        QueueNextDicePowerBonus = 4,
        DamageByTargetStatusStacksDivided = 5
    }

    public enum BuiltInConditionScope
    {
        CurrentTarget = 0,
        AnyOpponent = 1,
        AnyFriendly = 2,
        Self = 3
    }

    public enum StatusStackOperationType
    {
        ReduceStacks = 0,
        ClearStacks = 1
    }

    public enum EnemyStageTransitionTargetMode
    {
        Self = 0
    }

    public enum EnemyStageTransitionApplyTiming
    {
        NextRoundStart = 0,
        Immediate = 1
    }

    public enum PowerModifierCondition
    {
        SelfSpeedHigherThanTarget = 0,
        SelfSpeedLowerThanTarget = 1,
        SelfSpeedEqualToTarget = 2,
        TargetHasStatus = 3,
        TargetStatusStacksAtLeast = 4,
        TargetStatusStacksPerThreshold = 5,
        SelfHasStatus = 6,
        SelfStatusStacksGreaterThan = 7,
        SelfStatusStacksGreaterOrEqual = 8,
        SelfStatusStacksLessThan = 9,
        SelfStatusStacksLessOrEqual = 10,
        TargetSkillIsSpecific = 11,
        SelfSpeedHigherThanValue = 12,
        SelfSpeedLowerThanValue = 13,
        TargetNegativeStatusTypesAtLeast = 14,
        SelfHpRatioBelowOrEqual = 15
    }

    public enum SkillCategory
    {
        Attack = 0,
        Defense = 1
    }

    public enum TargetPriorityMode
    {
        None = 0,
        PreferTargetsWithStatusAtLeast = 1,
        PreferLowestSpeed = 2
    }

    public enum TransformOffsetSpace
    {
        Local = 0,
        Global = 1
    }

    [System.Serializable]
    public struct DicePowerRange
    {
        [Tooltip("Minimum power for this die, inclusive.")]
        public int minPower;

        [Tooltip("Maximum power for this die, inclusive.")]
        public int maxPower;

        public int SafeMin => Mathf.Max(0, minPower);
        public int SafeMax => Mathf.Max(SafeMin, maxPower);
    }

    [System.Serializable]
    public struct DicePresentationSettings
    {
        [Header("Optional Custom Prefabs")]
        [Tooltip("Optional clash slash prefab. Falls back to the built-in clash effect when empty.")]
        public GameObject clashEffectPrefab;

        [Tooltip("Optional hit prefab. Falls back to the built-in hit effect when empty.")]
        public GameObject hitEffectPrefab;

        [Header("Clash Transform")]
        [Tooltip("How clash position and rotation offsets should be interpreted. Local keeps the legacy behavior.")]
        public TransformOffsetSpace clashOffsetSpace;

        [Tooltip("Local position offset for the clash effect.")]
        public Vector3 clashPositionOffset;

        [Tooltip("Additional local rotation offset for the clash effect.")]
        public Vector3 clashRotationOffset;

        [Tooltip("Scale for the clash effect. Zero means 1,1,1.")]
        public Vector3 clashScale;

        [Tooltip("Sorting order offset for the clash effect.")]
        public int clashSortingOrderOffset;

        [Header("Clash Timing")]
        [Tooltip("Override the clash effect duration.")]
        public bool overrideClashDuration;

        [Min(0.01f)]
        [Tooltip("Manual clash effect duration when override is enabled.")]
        public float clashDuration;

        [Header("Hit Transform")]
        [Tooltip("How hit position and rotation offsets should be interpreted. Local keeps the legacy behavior.")]
        public TransformOffsetSpace hitOffsetSpace;

        [Tooltip("Local position offset for the hit effect.")]
        public Vector3 hitPositionOffset;

        [Tooltip("Additional local rotation offset for the hit effect.")]
        public Vector3 hitRotationOffset;

        [Tooltip("Scale for the hit effect. Zero means 1,1,1.")]
        public Vector3 hitScale;

        [Tooltip("Sorting order offset for the hit effect.")]
        public int hitSortingOrderOffset;

        [Header("Damage Timing")]
        [Tooltip("Override the damage timing for this die.")]
        public bool overrideHitDamageDelay;

        [Min(0f)]
        [Tooltip("Delay before damage is applied after the hit effect starts.")]
        public float hitDamageDelay;

        [Header("Hit Reaction Timing")]
        [Tooltip("Override the hit reaction timing for this die.")]
        public bool overrideHitReactionDelay;

        [Min(0f)]
        [Tooltip("Delay before flash, hit particles, and knockback start.")]
        public float hitReactionDelay;

        [Header("Skill Sprite Swap")]
        [Tooltip("Temporary sprite to show on the attacker while this die is being used.")]
        public Sprite skillSpriteOverride;

        [Tooltip("Local position offset applied while the temporary skill sprite is active.")]
        public Vector3 skillSpritePositionOffset;

        [Tooltip("Additional local rotation offset for the temporary skill sprite.")]
        public Vector3 skillSpriteRotationOffset;

        [Tooltip("Scale applied while the temporary skill sprite is active. Zero means 1,1,1.")]
        public Vector3 skillSpriteScale;

        [Tooltip("Sorting order offset applied while the temporary skill sprite is active.")]
        public int skillSpriteSortingOrderOffset;

        [Tooltip("Override the temporary skill sprite duration.")]
        public bool overrideSkillSpriteDuration;

        [Min(0.01f)]
        [Tooltip("How long the temporary skill sprite stays active.")]
        public float skillSpriteDuration;

        public Vector3 SafeClashScale => IsZeroScale(clashScale) ? Vector3.one : clashScale;
        public Vector3 SafeHitScale => IsZeroScale(hitScale) ? Vector3.one : hitScale;
        public Vector3 SafeSkillSpriteScale => IsZeroScale(skillSpriteScale) ? Vector3.one : skillSpriteScale;

        private static bool IsZeroScale(Vector3 scale)
        {
            return Mathf.Approximately(scale.x, 0f)
                && Mathf.Approximately(scale.y, 0f)
                && Mathf.Approximately(scale.z, 0f);
        }
    }

    [System.Serializable]
    public struct DamageEffectEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this effect should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("Who should receive the damage when this effect triggers.")]
        public DamageTargetMode targetMode;

        [Min(0)]
        [Tooltip("Fixed damage dealt by this effect.")]
        public int damageAmount;

        [Min(1)]
        [Tooltip("How many random opponents should be selected when using RandomOpponents.")]
        public int randomTargetCount;

    }

    [System.Serializable]
    public struct ConditionalPowerModifierEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this power modifier should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("Enable a condition check before applying this modifier.")]
        public bool useCondition;

        [Tooltip("Condition required for this power modifier to become active.")]
        public PowerModifierCondition condition;

        [Tooltip("Preferred target status asset used by status-based conditions.")]
        public BattleStatus_SO targetStatusData;

        [Tooltip("Specific opposing skill required when using the 'TargetSkillIsSpecific' condition.")]
        public EnemySkill_SO targetSkillData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType targetStatusType;

        [Min(1)]
        [Tooltip("Required stack count when using TargetStatusStacksAtLeast, or the threshold size when using TargetStatusStacksPerThreshold.")]
        public int requiredStatusStacks;

        [Tooltip("Required self speed value when using fixed-speed conditions.")]
        public int requiredSpeedValue;

        [Min(0)]
        [Tooltip("Required speed difference when comparing against the target. Values are inclusive, and 0 falls back to 1 for backwards compatibility.")]
        public int requiredSpeedDifference;

        [Tooltip("How much power should be added when the condition is met.")]
        public int powerBonus;

        [Tooltip("Additional power gained for each threshold when using TargetStatusStacksPerThreshold.")]
        public int powerBonusPerStep;

        [Min(0)]
        [Tooltip("Optional cap for total power gained by this modifier. 0 means uncapped.")]
        public int maxPowerBonus;
    }

    [System.Serializable]
    public struct StatusBurstDamageEffectEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this effect should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("Who should receive the burst damage when this effect triggers.")]
        public DamageTargetMode targetMode;

        [Tooltip("The status checked by this burst effect.")]
        public BattleStatus_SO statusData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType statusType;

        [Min(0)]
        [Tooltip("Burst damage multiplier per target status stack.")]
        public int damagePerStack;

        [Min(1)]
        [Tooltip("How many random opponents should be selected when using RandomOpponents.")]
        public int randomTargetCount;

        [Tooltip("Clear all stacks of the checked status after the burst triggers.")]
        public bool clearStacksAfterBurst;
    }

    [System.Serializable]
    public struct HealingEffectEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this healing effect should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("Who should receive the healing when this effect triggers.")]
        public DamageTargetMode targetMode;

        [Tooltip("Enable a condition check before applying this healing effect.")]
        public bool useCondition;

        [Tooltip("Condition required for this healing effect to become active.")]
        public PowerModifierCondition condition;

        [Tooltip("How the healing amount should be calculated.")]
        public HealAmountMode amountMode;

        [Min(0)]
        [Tooltip("Fixed heal amount, or per-stack heal amount when using status-stack healing.")]
        public int healAmount;

        [Tooltip("Preferred status asset used by status-stack healing.")]
        public BattleStatus_SO statusData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType statusType;

        [Tooltip("Preferred target status asset used by status-based conditions.")]
        public BattleStatus_SO conditionStatusData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType conditionStatusType;

        [Min(1)]
        [Tooltip("Required stack count when using TargetStatusStacksAtLeast, or the threshold size when using TargetStatusStacksPerThreshold.")]
        public int requiredConditionStatusStacks;

        [Tooltip("Required self speed value when using fixed-speed conditions.")]
        public int requiredConditionSpeedValue;

        [Min(0)]
        [Tooltip("Required speed difference when comparing against the target. Values are inclusive, and 0 falls back to 1 for backwards compatibility.")]
        public int requiredConditionSpeedDifference;

        [Tooltip("Whose status stacks should be sampled when using status-stack healing.")]
        public DamageTargetMode statusStackSourceMode;

        [Min(1)]
        [Tooltip("How many random opponents should be selected when using RandomOpponents.")]
        public int randomTargetCount;

    }

    [System.Serializable]
    public struct BuiltInEffectEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this built-in effect should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("What kind of built-in effect should be executed.")]
        public BuiltInEffectType effectType;

        [Tooltip("Who should receive the heal or damage when this effect triggers.")]
        public DamageTargetMode targetMode;

        [Tooltip("Enable a status check before applying this built-in effect.")]
        public bool useStatusCondition;

        [Tooltip("Where the status condition should be checked.")]
        public BuiltInConditionScope conditionScope;

        [Tooltip("Preferred status asset used by the status condition.")]
        public BattleStatus_SO conditionStatusData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType conditionStatusType;

        [Min(1)]
        [Tooltip("Required stack count for the configured status condition.")]
        public int requiredStatusStacks;

        [Min(0)]
        [Tooltip("How much healing or damage this built-in effect should apply.")]
        public int amount;

        [Min(1)]
        [Tooltip("How many random opponents should be selected when using RandomOpponents.")]
        public int randomTargetCount;

        [Tooltip("Enable an additional general condition check before applying this built-in effect.")]
        public bool useCondition;

        [Tooltip("Additional condition required for this built-in effect to become active.")]
        public PowerModifierCondition condition;

        [Tooltip("Preferred status asset used by the additional condition.")]
        public BattleStatus_SO targetStatusData;

        [Tooltip("Specific opposing skill required when using the 'TargetSkillIsSpecific' condition.")]
        public EnemySkill_SO targetSkillData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType targetStatusType;

        [Min(1)]
        [Tooltip("Required stack count when using status-stack conditions.")]
        public int requiredConditionStatusStacks;

        [Tooltip("Required self speed value when using fixed-speed conditions.")]
        public int requiredConditionSpeedValue;

        [Min(0)]
        [Tooltip("Required speed difference when comparing against the target. Values are inclusive, and 0 falls back to 1 for backwards compatibility.")]
        public int requiredConditionSpeedDifference;

        [Tooltip("Status applied by this built-in effect, or the fallback status used by transfer effects when no transferable status exists.")]
        public BattleStatus_SO statusData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType statusType;

        [Tooltip("When the built-in status should actually be applied.")]
        public StatusApplyTiming applyTiming;

        [Min(1)]
        [Tooltip("How many round starts later the built-in status should be applied when using delayed timing.")]
        public int applyAfterRoundStarts;

        [Min(1)]
        [Tooltip("How many times the delayed built-in status application should repeat on round start.")]
        public int applyRepeatCount;

        [Min(0)]
        [Tooltip("How many stacks should be applied when using built-in status effects.")]
        public int statusStackAmount;

        [Min(0)]
        [Tooltip("How many fallback stacks should be applied when a transfer effect cannot move any negative status.")]
        public int fallbackAmount;
    }

    [System.Serializable]
    public struct ConditionalDiceRangeModifierEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("Enable a condition check before applying this range modifier.")]
        public bool useCondition;

        [Tooltip("Condition required for this range modifier to become active.")]
        public PowerModifierCondition condition;

        [Tooltip("Preferred target status asset used by status-based conditions.")]
        public BattleStatus_SO targetStatusData;

        [Tooltip("Specific opposing skill required when using the 'TargetSkillIsSpecific' condition.")]
        public EnemySkill_SO targetSkillData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType targetStatusType;

        [Min(1)]
        [Tooltip("Required stack count when using TargetStatusStacksAtLeast, or the threshold size when using TargetStatusStacksPerThreshold.")]
        public int requiredStatusStacks;

        [Tooltip("Required self speed value when using fixed-speed conditions.")]
        public int requiredSpeedValue;

        [Min(0)]
        [Tooltip("Required speed difference when comparing against the target. Values are inclusive, and 0 falls back to 1 for backwards compatibility.")]
        public int requiredSpeedDifference;

        [Tooltip("How much the minimum power should change when the condition is met.")]
        public int minPowerBonus;

        [Min(0)]
        [Tooltip("Optional cap for total minimum power gained by this range modifier. 0 means uncapped.")]
        public int minPowerBonusCap;

        [Tooltip("How much the maximum power should change when the condition is met.")]
        public int maxPowerBonus;

        [Min(0)]
        [Tooltip("Optional cap for total maximum power gained by this range modifier. 0 means uncapped.")]
        public int maxPowerBonusCap;
    }

    [System.Serializable]
    public struct StatusStackOperationEffectEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this status stack operation should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("Who should receive the stack change when this effect triggers.")]
        public DamageTargetMode targetMode;

        [Tooltip("Preferred status asset to operate on.")]
        public BattleStatus_SO statusData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType statusType;

        [Tooltip("Whether this effect should reduce stacks or clear them entirely.")]
        public StatusStackOperationType operationType;

        [Min(1)]
        [Tooltip("How many stacks should be removed when using ReduceStacks.")]
        public int stackAmount;

        [Min(1)]
        [Tooltip("How many random opponents should be selected when using RandomOpponents.")]
        public int randomTargetCount;

        [Tooltip("Enable a condition check before applying this status stack operation.")]
        public bool useCondition;

        [Tooltip("Condition required for this status stack operation to apply.")]
        public PowerModifierCondition condition;

        [Tooltip("Preferred status asset used by the condition. This can differ from the status being operated on.")]
        public BattleStatus_SO conditionStatusData;

        [Tooltip("Specific opposing skill required when using the TargetSkillIsSpecific condition.")]
        public EnemySkill_SO conditionTargetSkillData;

        [Tooltip("Legacy fallback status type for the condition. Only Burn is still supported here.")]
        public DiceStatusType conditionStatusType;

        [Min(1)]
        [Tooltip("Required stack count when using status-stack conditions.")]
        public int requiredConditionStatusStacks;

        [Tooltip("Required self speed value when using fixed-speed conditions.")]
        public int requiredConditionSpeedValue;

        [Min(0)]
        [Tooltip("Required speed difference when comparing against the target. Values are inclusive, and 0 falls back to 1 for backwards compatibility.")]
        public int requiredConditionSpeedDifference;
    }

    [System.Serializable]
    public struct EnemyStageTransitionEffectEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this stage transition should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("Who should transition stage when this effect triggers.")]
        public EnemyStageTransitionTargetMode targetMode;

        [Tooltip("When the stage transition should actually happen.")]
        public EnemyStageTransitionApplyTiming applyTiming;

        [Min(0)]
        [Tooltip("Target zero-based stage index.")]
        public int targetStageIndex;
    }

    [System.Serializable]
    public struct SummonEnemyEffectEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this summon effect should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("Enemy data to spawn.")]
        public EnemyData_SO enemyData;

        [Min(1)]
        [Tooltip("How many copies should be summoned.")]
        public int summonCount;
    }

    [System.Serializable]
    public struct DiceEffectSettings
    {
        [Tooltip("Effects triggered by this die.")]
        public DamageEffectEntry[] damageEffects;

        [Tooltip("Status effects triggered by this die.")]
        public StatusEffectEntry[] statusEffects;

        [Tooltip("Conditional power modifiers applied by this die.")]
        public ConditionalPowerModifierEntry[] powerModifiers;

        [Tooltip("Status burst damage effects triggered by this die.")]
        public StatusBurstDamageEffectEntry[] statusBurstEffects;

        [Tooltip("Healing effects triggered by this die.")]
        public HealingEffectEntry[] healingEffects;

        [Tooltip("Reusable built-in effects triggered by this die.")]
        public BuiltInEffectEntry[] builtInEffects;

        [Tooltip("Conditional range modifiers applied by this die before rolling and previewing power.")]
        public ConditionalDiceRangeModifierEntry[] rangeModifiers;

        [Tooltip("Status stack operations triggered by this die.")]
        public StatusStackOperationEffectEntry[] statusStackOperationEffects;

        [Tooltip("Enemy stage transitions triggered by this die.")]
        public EnemyStageTransitionEffectEntry[] enemyStageTransitionEffects;

        [Tooltip("Enemy summons triggered by this die.")]
        public SummonEnemyEffectEntry[] summonEffects;
    }

    [System.Serializable]
    public struct StatusEffectEntry
    {
        [Tooltip("Optional effect label for debugging and UI.")]
        public string effectName;

        [Tooltip("When this effect should trigger.")]
        public DiceEffectTrigger trigger;

        [Tooltip("Who should receive the status when this effect triggers.")]
        public DamageTargetMode targetMode;

        [Tooltip("Preferred status asset. When empty, the legacy enum below is used as a fallback.")]
        public BattleStatus_SO statusData;

        [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
        public DiceStatusType statusType;

        [Tooltip("When the status should actually be applied to the target.")]
        public StatusApplyTiming applyTiming;

        [Min(1)]
        [Tooltip("How many round starts later the status should be applied when using delayed timing.")]
        public int applyAfterRoundStarts;

        [Min(1)]
        [Tooltip("How many times the delayed status application should repeat on round start.")]
        public int applyRepeatCount;

        [Min(1)]
        [Tooltip("How many stacks should be applied.")]
        public int stackAmount;

        [Min(1)]
        [Tooltip("How many random opponents should be selected when using RandomOpponents.")]
        public int randomTargetCount;

        [Tooltip("Enable a condition check before applying this status effect.")]
        public bool useCondition;

        [Tooltip("Condition required for this status effect to apply.")]
        public PowerModifierCondition condition;

        [Tooltip("Preferred status asset used by the condition. This can differ from the status being applied.")]
        public BattleStatus_SO conditionStatusData;

        [Tooltip("Specific opposing skill required when using the TargetSkillIsSpecific condition.")]
        public EnemySkill_SO conditionTargetSkillData;

        [Tooltip("Legacy fallback status type for the condition. Only Burn is still supported here.")]
        public DiceStatusType conditionStatusType;

        [Min(1)]
        [Tooltip("Required stack count when using status-stack conditions.")]
        public int requiredConditionStatusStacks;

        [Tooltip("Required self speed value when using fixed-speed conditions.")]
        public int requiredConditionSpeedValue;

        [Min(0)]
        [Tooltip("Required speed difference when comparing against the target. Values are inclusive, and 0 falls back to 1 for backwards compatibility.")]
        public int requiredConditionSpeedDifference;
    }

    [Tooltip("Skill display name.")]
    public string skillName = "Basic Attack";

    [TextArea(2, 4)]
    [Tooltip("Optional skill description.")]
    public string description;

    [Header("Skill Category")]
    [Tooltip("Attack skills deal damage normally. Defense skills reduce incoming clash damage instead of dealing direct clash damage.")]
    public SkillCategory skillCategory = SkillCategory.Attack;

    [Header("Ally Energy")]
    [Min(0)]
    [Tooltip("Shared energy cost consumed by allies when this skill is used. Enemies ignore this value.")]
    public int energyCost;

    [Header("Ally Status Requirement")]
    [Tooltip("Optional status required for player-controlled allies to cast this skill. Enemies ignore this requirement.")]
    public BattleStatus_SO requiredSelfStatusData;

    [Min(0)]
    [Tooltip("Minimum required stacks of the status above for player-controlled allies to cast this skill. 0 disables the requirement.")]
    public int requiredSelfStatusStacks;

    [Tooltip("When enabled, allies keep the required status after using the skill. Disabled by default, so the required stacks are consumed on use.")]
    public bool keepRequiredSelfStatusAfterUse;

    [Header("Enemy Stage Transition")]
    [Tooltip("When set to 0 or above, an enemy using this skill will immediately switch to the target stage index.")]
    public int transformEnemyToStageIndex = -1;

    [Header("Stage Restriction")]
    [Tooltip("Allowed enemy stages for this skill. Stage numbers start from 1. When empty, the skill is only available in stage 1.")]
    public List<int> allowedEnemyStages;

    [Header("Target Priority")]
    [Tooltip("Optional targeting preference used by enemy auto-target selection.")]
    public TargetPriorityMode targetPriorityMode = TargetPriorityMode.None;
    [Tooltip("Preferred target status asset checked by the targeting preference.")]
    public BattleStatus_SO targetPriorityStatusData;
    [Tooltip("Legacy fallback status type kept for backwards compatibility. Only Burn is still supported here.")]
    public DiceStatusType targetPriorityStatusType;
    [Min(1)]
    [Tooltip("Minimum required stacks when using status-based target priority.")]
    public int targetPriorityRequiredStacks = 1;

    [Tooltip("Fallback minimum power used when per-die ranges are not configured.")]
    public int minPower = 1;

    [Tooltip("Fallback maximum power used when per-die ranges are not configured.")]
    public int maxPower = 3;

    [Tooltip("Number of dice used by this skill.")]
    [Min(1)]
    public int diceCount = 1;

    [Tooltip("Per-die power ranges. Falls back to the unified range when empty.")]
    public DicePowerRange[] dicePowerRanges;

    [Tooltip("Per-die presentation settings. Falls back to built-in presentation when empty.")]
    public DicePresentationSettings[] dicePresentationSettings;

    [Tooltip("Per-die gameplay effects. Falls back to no extra effects when empty.")]
    public DiceEffectSettings[] diceEffectSettings;

    [Tooltip("Optional per-die categories. Falls back to the unified skill category when empty.")]
    public SkillCategory[] diceSkillCategories;

    public DicePowerRange GetDicePowerRange(int diceIndex)
    {
        if (dicePowerRanges != null && dicePowerRanges.Length > 0)
        {
            int safeIndex = Mathf.Clamp(diceIndex, 0, dicePowerRanges.Length - 1);
            DicePowerRange configuredRange = dicePowerRanges[safeIndex];
            configuredRange.minPower = configuredRange.SafeMin;
            configuredRange.maxPower = configuredRange.SafeMax;
            return configuredRange;
        }

        int safeMin = Mathf.Max(0, minPower);
        return new DicePowerRange
        {
            minPower = safeMin,
            maxPower = Mathf.Max(safeMin, maxPower)
        };
    }

    public DicePresentationSettings GetDicePresentationSettings(int diceIndex)
    {
        if (dicePresentationSettings != null && dicePresentationSettings.Length > 0)
        {
            int safeIndex = Mathf.Clamp(diceIndex, 0, dicePresentationSettings.Length - 1);
            DicePresentationSettings settings = dicePresentationSettings[safeIndex];
            if (settings.overrideClashDuration && settings.clashDuration < 0.01f)
                settings.clashDuration = 0.01f;
            if (settings.overrideHitDamageDelay && settings.hitDamageDelay < 0f)
                settings.hitDamageDelay = 0f;
            if (settings.overrideHitReactionDelay && settings.hitReactionDelay < 0f)
                settings.hitReactionDelay = 0f;
            if (settings.overrideSkillSpriteDuration && settings.skillSpriteDuration < 0.01f)
                settings.skillSpriteDuration = 0.01f;
            return settings;
        }

        return default;
    }

    public DiceEffectSettings GetDiceEffectSettings(int diceIndex)
    {
        if (diceEffectSettings != null && diceEffectSettings.Length > 0)
        {
            int safeIndex = Mathf.Clamp(diceIndex, 0, diceEffectSettings.Length - 1);
            DiceEffectSettings settings = diceEffectSettings[safeIndex];
            SanitizeDamageEffects(ref settings);
            return settings;
        }

        return default;
    }

    public SkillCategory GetDiceSkillCategory(int diceIndex)
    {
        if (diceSkillCategories != null && diceSkillCategories.Length > 0)
        {
            int safeIndex = Mathf.Clamp(diceIndex, 0, diceSkillCategories.Length - 1);
            return diceSkillCategories[safeIndex];
        }

        return skillCategory;
    }

    public bool IsUsableInEnemyStage(int zeroBasedStageIndex)
    {
        int oneBasedStage = Mathf.Max(1, zeroBasedStageIndex + 1);
        if (allowedEnemyStages == null || allowedEnemyStages.Count == 0)
            return oneBasedStage == 1;

        for (int i = 0; i < allowedEnemyStages.Count; i++)
        {
            if (allowedEnemyStages[i] == oneBasedStage)
                return true;
        }

        return false;
    }

    public bool IsDefenseSkill => skillCategory == SkillCategory.Defense;
    public bool RequiresSelfStatusToUse => requiredSelfStatusData != null && requiredSelfStatusStacks > 0;

    private void OnValidate()
    {
        if (energyCost < 0)
            energyCost = 0;

        if (requiredSelfStatusStacks < 0)
            requiredSelfStatusStacks = 0;

        if (transformEnemyToStageIndex < -1)
            transformEnemyToStageIndex = -1;

        if (allowedEnemyStages == null)
            allowedEnemyStages = new List<int>();
        for (int i = allowedEnemyStages.Count - 1; i >= 0; i--)
        {
            if (allowedEnemyStages[i] < 1)
                allowedEnemyStages[i] = 1;
        }
        for (int i = allowedEnemyStages.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < i; j++)
            {
                if (allowedEnemyStages[i] == allowedEnemyStages[j])
                {
                    allowedEnemyStages.RemoveAt(i);
                    break;
                }
            }
        }

        if (targetPriorityRequiredStacks < 1)
            targetPriorityRequiredStacks = 1;

        if (diceCount < 1)
            diceCount = 1;

        if (minPower < 0)
            minPower = 0;
        if (maxPower < minPower)
            maxPower = minPower;

        if (dicePowerRanges != null)
        {
            for (int i = 0; i < dicePowerRanges.Length; i++)
            {
                DicePowerRange range = dicePowerRanges[i];
                if (range.minPower < 0)
                    range.minPower = 0;
                if (range.maxPower < range.minPower)
                    range.maxPower = range.minPower;
                dicePowerRanges[i] = range;
            }
        }

        if (dicePresentationSettings != null)
        {
            for (int i = 0; i < dicePresentationSettings.Length; i++)
            {
                DicePresentationSettings settings = dicePresentationSettings[i];
                if (settings.overrideClashDuration && settings.clashDuration < 0.01f)
                    settings.clashDuration = 0.01f;
                if (settings.overrideHitDamageDelay && settings.hitDamageDelay < 0f)
                    settings.hitDamageDelay = 0f;
                if (settings.overrideHitReactionDelay && settings.hitReactionDelay < 0f)
                    settings.hitReactionDelay = 0f;
                if (settings.overrideSkillSpriteDuration && settings.skillSpriteDuration < 0.01f)
                    settings.skillSpriteDuration = 0.01f;
                dicePresentationSettings[i] = settings;
            }
        }

        if (diceEffectSettings != null)
        {
            for (int i = 0; i < diceEffectSettings.Length; i++)
            {
                DiceEffectSettings settings = diceEffectSettings[i];
                SanitizeDamageEffects(ref settings);
                diceEffectSettings[i] = settings;
            }
        }
    }

    private static void SanitizeDamageEffects(ref DiceEffectSettings settings)
    {
        if (settings.damageEffects != null)
        {
            for (int i = 0; i < settings.damageEffects.Length; i++)
            {
                DamageEffectEntry entry = settings.damageEffects[i];
                if (entry.damageAmount < 0)
                    entry.damageAmount = 0;
                if (entry.randomTargetCount < 1)
                    entry.randomTargetCount = 1;
                settings.damageEffects[i] = entry;
            }
        }

        if (settings.statusEffects != null)
        {
            for (int i = 0; i < settings.statusEffects.Length; i++)
            {
                StatusEffectEntry entry = settings.statusEffects[i];
                if (entry.stackAmount < 1)
                    entry.stackAmount = 1;
                if (entry.randomTargetCount < 1)
                    entry.randomTargetCount = 1;
                if (entry.requiredConditionStatusStacks < 1)
                    entry.requiredConditionStatusStacks = 1;
                if (entry.requiredConditionSpeedDifference < 0)
                    entry.requiredConditionSpeedDifference = 0;
                settings.statusEffects[i] = entry;
            }
        }

        if (settings.powerModifiers != null)
        {
            for (int i = 0; i < settings.powerModifiers.Length; i++)
            {
                ConditionalPowerModifierEntry entry = settings.powerModifiers[i];
                if (entry.requiredStatusStacks < 1)
                    entry.requiredStatusStacks = 1;
                if (entry.requiredSpeedDifference < 0)
                    entry.requiredSpeedDifference = 0;
                if (entry.maxPowerBonus < 0)
                    entry.maxPowerBonus = 0;
                settings.powerModifiers[i] = entry;
            }
        }

        if (settings.statusBurstEffects != null)
        {
            for (int i = 0; i < settings.statusBurstEffects.Length; i++)
            {
                StatusBurstDamageEffectEntry entry = settings.statusBurstEffects[i];
                if (entry.damagePerStack < 0)
                    entry.damagePerStack = 0;
                if (entry.randomTargetCount < 1)
                    entry.randomTargetCount = 1;
                settings.statusBurstEffects[i] = entry;
            }
        }

        if (settings.healingEffects != null)
        {
            for (int i = 0; i < settings.healingEffects.Length; i++)
            {
                HealingEffectEntry entry = settings.healingEffects[i];
                if (entry.healAmount < 0)
                    entry.healAmount = 0;
                if (entry.requiredConditionStatusStacks < 1)
                    entry.requiredConditionStatusStacks = 1;
                if (entry.requiredConditionSpeedDifference < 0)
                    entry.requiredConditionSpeedDifference = 0;
                if (entry.randomTargetCount < 1)
                    entry.randomTargetCount = 1;
                settings.healingEffects[i] = entry;
            }
        }

        if (settings.builtInEffects != null)
        {
            for (int i = 0; i < settings.builtInEffects.Length; i++)
            {
                BuiltInEffectEntry entry = settings.builtInEffects[i];
                if (entry.requiredStatusStacks < 1)
                    entry.requiredStatusStacks = 1;
                if (entry.requiredConditionStatusStacks < 1)
                    entry.requiredConditionStatusStacks = 1;
                if (entry.requiredConditionSpeedDifference < 0)
                    entry.requiredConditionSpeedDifference = 0;
                if (entry.amount < 0)
                    entry.amount = 0;
                if (entry.statusStackAmount < 0)
                    entry.statusStackAmount = 0;
                if (entry.fallbackAmount < 0)
                    entry.fallbackAmount = 0;
                if (entry.applyAfterRoundStarts < 1)
                    entry.applyAfterRoundStarts = 1;
                if (entry.applyRepeatCount < 1)
                    entry.applyRepeatCount = 1;
                if (entry.randomTargetCount < 1)
                    entry.randomTargetCount = 1;
                settings.builtInEffects[i] = entry;
            }
        }

        if (settings.rangeModifiers != null)
        {
            for (int i = 0; i < settings.rangeModifiers.Length; i++)
            {
                ConditionalDiceRangeModifierEntry entry = settings.rangeModifiers[i];
                if (entry.requiredStatusStacks < 1)
                    entry.requiredStatusStacks = 1;
                if (entry.requiredSpeedDifference < 0)
                    entry.requiredSpeedDifference = 0;
                if (entry.minPowerBonusCap < 0)
                    entry.minPowerBonusCap = 0;
                if (entry.maxPowerBonusCap < 0)
                    entry.maxPowerBonusCap = 0;
                settings.rangeModifiers[i] = entry;
            }
        }

        if (settings.statusStackOperationEffects != null)
        {
            for (int i = 0; i < settings.statusStackOperationEffects.Length; i++)
            {
                StatusStackOperationEffectEntry entry = settings.statusStackOperationEffects[i];
                if (entry.stackAmount < 1)
                    entry.stackAmount = 1;
                if (entry.randomTargetCount < 1)
                    entry.randomTargetCount = 1;
                if (entry.requiredConditionStatusStacks < 1)
                    entry.requiredConditionStatusStacks = 1;
                if (entry.requiredConditionSpeedDifference < 0)
                    entry.requiredConditionSpeedDifference = 0;
                settings.statusStackOperationEffects[i] = entry;
            }
        }

        if (settings.enemyStageTransitionEffects != null)
        {
            for (int i = 0; i < settings.enemyStageTransitionEffects.Length; i++)
            {
                EnemyStageTransitionEffectEntry entry = settings.enemyStageTransitionEffects[i];
                entry.targetStageIndex = Mathf.Max(0, entry.targetStageIndex);
                settings.enemyStageTransitionEffects[i] = entry;
            }
        }

        if (settings.summonEffects != null)
        {
            for (int i = 0; i < settings.summonEffects.Length; i++)
            {
                SummonEnemyEffectEntry entry = settings.summonEffects[i];
                if (entry.summonCount < 1)
                    entry.summonCount = 1;
                settings.summonEffects[i] = entry;
            }
        }
    }
}
