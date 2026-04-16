using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRelic", menuName = "Relics/Relic")]
public class Relic_SO : ScriptableObject
{
    public enum RelicStatusApplyTiming
    {
        Immediate = 0,
        NextRoundStart = 1
    }

    public enum RelicTrigger
    {
        OnMoveCostAboutToSpend = 0,
        OnStatusApplied = 1,
        OnPlayerDicePrepared = 2,
        OnPlayerAboutToTakeSingleAttackDamage = 3,
        OnMerchantPriceCalculated = 4,
        OnPlayerRoundStart = 5,
        OnBattleRoundStart = 6
    }

    public enum RelicBuiltInEffectType
    {
        None = 0,
        ChanceToIgnoreMoveCost = 1,
        AddExtraStatusStacksOnApply = 2,
        ModifyPlayerDiceWhenSelfHasStatus = 3,
        ReduceIncomingSingleAttackDamageFlat = 4,
        ModifyPlayerDiceBasedOnLastRoundSkillMatch = 5,
        ReduceMerchantPricePercent = 6,
        ApplyStatusToPlayerWhenSpeedEquals = 7,
        ApplyStatusToAllEnemiesOnSpecificRound = 8
    }

    [Serializable]
    public class RelicBuiltInEffectEntry
    {
        public string effectName;
        public RelicTrigger trigger = RelicTrigger.OnMoveCostAboutToSpend;
        public RelicBuiltInEffectType effectType = RelicBuiltInEffectType.None;
        [Range(0f, 1f)] public float chance = 1f;
        public bool requireUnvisitedTile = true;
        public BattleStatus_SO statusData;
        [Min(0)] public int amount = 1;
        public BattleStatus_SO conditionStatusData;
        [Min(1)] public int requiredStatusStacks = 1;
        public int requiredSpeed = 1;
        public int requiredSpeedMin = 1;
        public int requiredSpeedMax = 1;
        public RelicStatusApplyTiming statusApplyTiming = RelicStatusApplyTiming.Immediate;
        [Min(1)] public int applyAfterRoundStarts = 1;
        [Min(1)] public int requiredRound = 1;
        public int powerAmount;
        public int minPowerAmount;
        public int maxPowerAmount;
    }

    [Header("Identity")]
    public string relicId;
    public string relicName;

    [TextArea(2, 5)]
    public string description;

    [TextArea(2, 5)]
    public string additionalDescription;

    [Header("Presentation")]
    public LootRarity rarity = LootRarity.Common;
    public Sprite icon;

    [Header("Effects")]
    public List<RelicBuiltInEffectEntry> builtInEffects = new List<RelicBuiltInEffectEntry>();
    public RelicEffectHandler_SO customEffectHandler;

    public string DisplayName => !string.IsNullOrWhiteSpace(relicName) ? relicName : name;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(relicId))
            relicId = name;
        if (builtInEffects == null)
            builtInEffects = new List<RelicBuiltInEffectEntry>();

        for (int i = 0; i < builtInEffects.Count; i++)
        {
            RelicBuiltInEffectEntry effect = builtInEffects[i];
            if (effect == null)
                continue;

            effect.requiredSpeed = Mathf.Max(0, effect.requiredSpeed);
            effect.requiredSpeedMin = Mathf.Max(0, effect.requiredSpeedMin);
            effect.requiredSpeedMax = Mathf.Max(0, effect.requiredSpeedMax);
            effect.applyAfterRoundStarts = Mathf.Max(1, effect.applyAfterRoundStarts);

            if (effect.requiredSpeed != 1 && effect.requiredSpeedMin == 1 && effect.requiredSpeedMax == 1)
            {
                effect.requiredSpeedMin = effect.requiredSpeed;
                effect.requiredSpeedMax = effect.requiredSpeed;
            }
        }
    }
}
