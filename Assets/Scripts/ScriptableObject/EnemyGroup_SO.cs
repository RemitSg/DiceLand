using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New EnemyGroup", menuName = "Enemies/EnemyGroup")]
public class EnemyGroup_SO : ScriptableObject
{
    [Header("Group Info")]
    public int groupID;
    public string groupName;

    [TextArea(2, 4)]
    public string description;

    [Header("Members")]
    public List<EnemyData_SO> enemies = new List<EnemyData_SO>();

    [Header("Attributes")]
    public List<LootAttribute_SO> attributes = new List<LootAttribute_SO>();

    [Header("Danger")]
    [Min(0)]
    [Tooltip("0 uses the map area's default danger rating. Values above 0 override that rating.")]
    public int dangerRatingOverride;

    [Header("Audio")]
    public AudioClip battleMusicOverride;

    [Header("Loot Tables")]
    public List<LootTable_SO> lootTables = new List<LootTable_SO>();

    public int Count => enemies != null ? enemies.Count : 0;

    public bool IsValid => enemies != null && enemies.Count > 0;

    public IReadOnlyList<EnemyData_SO> GetEnemies()
    {
        return enemies ?? (IReadOnlyList<EnemyData_SO>)Array.Empty<EnemyData_SO>();
    }

    public IReadOnlyList<LootTable_SO> GetLootTables()
    {
        return lootTables ?? (IReadOnlyList<LootTable_SO>)Array.Empty<LootTable_SO>();
    }

    public IReadOnlyList<LootAttribute_SO> GetAttributes()
    {
        return attributes ?? (IReadOnlyList<LootAttribute_SO>)Array.Empty<LootAttribute_SO>();
    }

    private void OnValidate()
    {
        if (enemies == null)
            enemies = new List<EnemyData_SO>();

        if (attributes == null)
            attributes = new List<LootAttribute_SO>();

        if (lootTables == null)
            lootTables = new List<LootTable_SO>();

        dangerRatingOverride = Mathf.Max(0, dangerRatingOverride);
    }
}
