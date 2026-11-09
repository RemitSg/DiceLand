using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public enum EncyclopediaCategory
{
    Enemies,
    Weapons,
    Relics,
    Loot,
    Calamities
}

public enum EncyclopediaEnemyGroup
{
    OuterStandard,
    OuterElite,
    MidStandard,
    MidElite,
    InnerStandard,
    InnerElite,
    CoreStandard,
    CoreElite,
    Boss,
    Special
}

public sealed class EncyclopediaCatalog : ScriptableObject
{
    public const string ResourcePath = "MainMenu/EncyclopediaCatalog";
    public const string IdentityPath = "MainMenu/EncyclopediaIdentities";

    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private string id;
        [SerializeField] private ScriptableObject asset;
        [SerializeField] private EncyclopediaEnemyGroup enemyGroup = EncyclopediaEnemyGroup.Special;

        public Entry(string id, ScriptableObject asset, EncyclopediaEnemyGroup enemyGroup = EncyclopediaEnemyGroup.Special)
        {
            this.id = id;
            this.asset = asset;
            this.enemyGroup = enemyGroup;
        }

        public string Id => id;
        public ScriptableObject Asset => asset;
        public EncyclopediaEnemyGroup EnemyGroup => enemyGroup;
        public EncyclopediaCategory Category => asset switch
        {
            EnemyData_SO _ => EncyclopediaCategory.Enemies,
            Weapon_SO _ => EncyclopediaCategory.Weapons,
            Relic_SO _ => EncyclopediaCategory.Relics,
            LootItem_SO _ => EncyclopediaCategory.Loot,
            _ => EncyclopediaCategory.Calamities
        };

        public string DisplayName => asset switch
        {
            EnemyData_SO enemy => enemy.LocalizedName,
            Weapon_SO weapon => weapon.LocalizedName,
            Relic_SO relic => relic.LocalizedName,
            LootItem_SO loot => loot.LocalizedName,
            Calamity_SO calamity => calamity.LocalizedName,
            _ => string.Empty
        };

        public LootRarity? Rarity => asset switch
        {
            Weapon_SO weapon => (LootRarity)weapon.rarity,
            Relic_SO relic => relic.rarity,
            LootItem_SO loot => loot.rarity,
            Calamity_SO calamity => calamity.rarity,
            _ => null
        };

        public Sprite Icon => asset switch
        {
            EnemyData_SO enemy => enemy.GetStageSprite(0),
            Weapon_SO weapon => weapon.icon,
            Relic_SO relic => relic.icon != null ? relic.icon : MapForestArt.Get("Treasure"),
            LootItem_SO loot => loot.icon,
            _ => null
        };
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    [SerializeField] private TMP_FontAsset headingFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    public IReadOnlyList<Entry> Entries => entries;
    public TMP_FontAsset HeadingFont => headingFont;
    public TMP_FontAsset BodyFont => bodyFont;

    public static string AssetKey(ScriptableObject asset)
    {
        switch (asset)
        {
            case EnemyData_SO enemy:
                string enemyId = string.IsNullOrWhiteSpace(enemy.localizationKey)
                    ? enemy.enemyID.ToString(CultureInfo.InvariantCulture) : enemy.localizationKey;
                return "enemy:" + enemyId;
            case Weapon_SO weapon: return "weapon:" + MainMenuRunSaveSystem.GetWeaponId(weapon);
            case Relic_SO relic: return "relic:" + MainMenuRunSaveSystem.GetRelicId(relic);
            case LootItem_SO loot: return "loot:" + MainMenuRunSaveSystem.GetLootId(loot);
            case Calamity_SO calamity: return "calamity:" + MainMenuRunSaveSystem.GetCalamityId(calamity);
            default: return null;
        }
    }

#if UNITY_EDITOR
    public void SetContents(Entry[] newEntries, TMP_FontAsset title, TMP_FontAsset body)
    {
        entries = newEntries;
        headingFont = title;
        bodyFont = body;
    }
#endif
}
