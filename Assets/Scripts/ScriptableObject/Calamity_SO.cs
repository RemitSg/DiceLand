using System;
using System.Collections.Generic;
using UnityEngine;

public enum CalamityBuiltInEffectType
{
    None = 0,
    EnemyMaxHpPercentModifier = 1,
    EnemyDamagePercentModifier = 2,
    BattleExperiencePercentModifier = 3,
    BattleMoneyPercentModifier = 4,
    MerchantPricePercentModifier = 5,
    BattleStartSharedEnergyFlatModifier = 6,
    RestHealPercentOverride = 7,
    RestActionPointRestorePercentOverride = 8,
    EnemySpeedFlatModifier = 9,
    EnemyBattleStartStatus = 10,
    AllyDamagePercentModifier = 11,
    AllyMaxHpPercentModifier = 12,
    ForgeCostFlatModifier = 13,
    EnemyIncomingDamagePercentModifier = 14,
    EnemyRoundEndHealFlat = 15,
    AllyBattleStartHpLossFlat = 16,
    AllyBattleStartStatus = 17,
    EnemyDeathStatusToRemainingEnemies = 18,
    EnemyDelayDeathUntilNextRoundStart = 19,
    TeleportToRandomOuterUnvisitedTile = 20,
    LoseRandomRelics = 21,
    ConvertUnvisitedRestTilesToBattles = 22,
    VisitedEmptyTileAmbushChance = 23
}

[Serializable]
public class CalamityBuiltInEffectEntry
{
    public string effectName = "CalamityEffect";
    public CalamityBuiltInEffectType effectType = CalamityBuiltInEffectType.None;
    public int effectAmount;
    public BattleStatus_SO statusData;
}

[CreateAssetMenu(fileName = "Calamity", menuName = "Calamities/Calamity")]
public class Calamity_SO : ScriptableObject
{
    public string calamityId = "calamity";
    public string calamityName = "灾厄";
    [Min(1)] public int calamityTier = 1;
    [TextArea(3, 8)] public string description;
    public LootRarity rarity = LootRarity.Common;
    public Sprite icon;
    public List<CalamityBuiltInEffectEntry> builtInEffects = new List<CalamityBuiltInEffectEntry>();
    public CalamityEffectHandler_SO customEffectHandler;

    public string DisplayName => string.IsNullOrWhiteSpace(calamityName) ? name : calamityName;
    public string UniqueId => !string.IsNullOrWhiteSpace(calamityId) ? calamityId : name;

    public IReadOnlyList<CalamityBuiltInEffectEntry> GetValidEffects()
    {
        if (builtInEffects == null || builtInEffects.Count == 0)
            return Array.Empty<CalamityBuiltInEffectEntry>();

        List<CalamityBuiltInEffectEntry> validEffects = new List<CalamityBuiltInEffectEntry>(builtInEffects.Count);
        for (int i = 0; i < builtInEffects.Count; i++)
        {
            CalamityBuiltInEffectEntry effect = builtInEffects[i];
            if (effect != null && effect.effectType != CalamityBuiltInEffectType.None)
                validEffects.Add(effect);
        }

        return validEffects;
    }
}
