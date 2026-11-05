using UnityEngine;

[CreateAssetMenu(fileName = "New LootAttribute", menuName = "Loot/Loot Attribute")]
public class LootAttribute_SO : ScriptableObject
{
    [Header("Attribute Info")]
    public const string ElectricityId = "5";
    public string attributeId;
    public string LocalizedName => GameText.Content("attribute." + attributeId, "name", attributeName);
    public string LocalizedDescription => GameText.Content("attribute." + attributeId, "description", description);
    public string attributeName;

    [TextArea(2, 4)]
    public string description;

    public Color displayColor = Color.white;
    public Sprite icon;
}
