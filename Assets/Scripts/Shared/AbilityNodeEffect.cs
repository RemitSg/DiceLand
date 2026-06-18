using System;

[Serializable]
public sealed class AbilityNodeEffect
{
    public AbilityEffectType effectType = AbilityEffectType.None;
    public int intValue;
    public CraftingBlueprint_SO blueprintData;
    public BlueprintTable_SO blueprintTable;
    public LootTable_SO lootTable;
    public RelicTable_SO relicTable;
    public LootRarityWeightTable_SO relicRarityWeightTable;
    public TreasureRewardTable_SO treasureRewardTable;
    public CharacterData_SO characterData;
    public Weapon_SO weaponData;
}
