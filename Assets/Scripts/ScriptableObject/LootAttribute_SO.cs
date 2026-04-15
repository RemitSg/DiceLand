using UnityEngine;

[CreateAssetMenu(fileName = "New LootAttribute", menuName = "Loot/Loot Attribute")]
public class LootAttribute_SO : ScriptableObject
{
    [Header("Attribute Info")]
    public string attributeId;
    public string attributeName;

    [TextArea(2, 4)]
    public string description;

    public Color displayColor = Color.white;
    public Sprite icon;
}
