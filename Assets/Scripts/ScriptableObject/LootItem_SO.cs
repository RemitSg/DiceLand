using UnityEngine;

public enum LootRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Trash = 5
}

[CreateAssetMenu(fileName = "New LootItem", menuName = "Loot/Loot Item")]
public class LootItem_SO : ScriptableObject
{
    [Header("Identity")]
    public int lootID;
    public string saveId;
    public string legacySaveId;
    public string lootName;
    public string LocalizedName => Localize("name", lootName);
    public string LocalizedDescription => Localize("description", description);

    private string Localize(string field, string source) => lootID > 0
        ? GameText.Content("loot", lootID, field, source)
        : GameText.Content(saveId, field, source);

    [TextArea(2, 5)]
    public string description;

    [Header("Classification")]
    public LootAttribute_SO attribute;
    public LootRarity rarity = LootRarity.Common;

    [Header("Presentation")]
    public Sprite icon;
}
