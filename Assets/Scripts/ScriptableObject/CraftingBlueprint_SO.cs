using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftingMaterialRequirement
{
    public LootItem_SO lootItem;
    [Min(1)] public int amount = 1;
}

[CreateAssetMenu(fileName = "New CraftingBlueprint", menuName = "Crafting/Crafting Blueprint")]
public class CraftingBlueprint_SO : ScriptableObject
{
    [Header("Identity")]
    public int blueprintID;
    public string blueprintName;

    [TextArea(2, 5)]
    public string description;

    [Header("Classification")]
    public LootRarity rarity = LootRarity.Common;

    [Header("Presentation")]
    public Sprite icon;

    [Header("Craft Result")]
    public Weapon_SO targetWeapon;

    [Header("Materials")]
    public List<CraftingMaterialRequirement> materialRequirements = new List<CraftingMaterialRequirement>();

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(blueprintName)
            ? blueprintName
            : (targetWeapon != null && !string.IsNullOrWhiteSpace(targetWeapon.weaponName)
                ? targetWeapon.weaponName
                : "Blueprint");

    public Sprite DisplayIcon =>
        icon != null
            ? icon
            : (targetWeapon != null ? targetWeapon.icon : null);

    private void OnValidate()
    {
        if (materialRequirements == null)
            materialRequirements = new List<CraftingMaterialRequirement>();

        for (int i = 0; i < materialRequirements.Count; i++)
        {
            CraftingMaterialRequirement requirement = materialRequirements[i];
            if (requirement == null)
            {
                materialRequirements[i] = new CraftingMaterialRequirement();
                continue;
            }

            requirement.amount = Mathf.Max(1, requirement.amount);
        }
    }
}
