using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New BlueprintTable", menuName = "Crafting/Blueprint Table")]
public class BlueprintTable_SO : ScriptableObject
{
    [Header("Table Info")]
    public int tableID;
    public string tableName;

    [TextArea(2, 4)]
    public string description;

    [Header("Blueprints")]
    [SerializeField] private List<CraftingBlueprint_SO> blueprints = new List<CraftingBlueprint_SO>();

    public IReadOnlyList<CraftingBlueprint_SO> Blueprints => blueprints;

    public bool IsValid
    {
        get
        {
            if (blueprints == null)
                return false;

            for (int i = 0; i < blueprints.Count; i++)
            {
                if (blueprints[i] != null)
                    return true;
            }

            return false;
        }
    }

    public IReadOnlyList<CraftingBlueprint_SO> GetValidBlueprints()
    {
        if (blueprints == null || blueprints.Count == 0)
            return Array.Empty<CraftingBlueprint_SO>();

        List<CraftingBlueprint_SO> valid = new List<CraftingBlueprint_SO>(blueprints.Count);
        for (int i = 0; i < blueprints.Count; i++)
        {
            CraftingBlueprint_SO blueprint = blueprints[i];
            if (blueprint != null)
                valid.Add(blueprint);
        }

        return valid;
    }

    private void OnValidate()
    {
        if (blueprints == null)
            blueprints = new List<CraftingBlueprint_SO>();
    }
}
