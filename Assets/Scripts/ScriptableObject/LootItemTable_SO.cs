using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New LootItemTable", menuName = "Loot/Loot Item Table")]
public class LootItemTable_SO : ScriptableObject
{
    [Header("Identity")]
    public int tableID;
    public string tableName;

    [TextArea(2, 4)]
    public string description;

    [Header("Loot Items")]
    [SerializeField] private List<LootItem_SO> lootItems = new List<LootItem_SO>();

    public IReadOnlyList<LootItem_SO> LootItems => lootItems;

    public bool IsValid
    {
        get
        {
            if (lootItems == null)
                return false;

            for (int i = 0; i < lootItems.Count; i++)
            {
                if (lootItems[i] != null)
                    return true;
            }

            return false;
        }
    }

    private void OnValidate()
    {
        if (lootItems == null)
            lootItems = new List<LootItem_SO>();

        HashSet<LootItem_SO> seen = new HashSet<LootItem_SO>();
        for (int i = lootItems.Count - 1; i >= 0; i--)
        {
            LootItem_SO item = lootItems[i];
            if (item == null)
                continue;

            if (!seen.Add(item))
                lootItems.RemoveAt(i);
        }
    }
}
