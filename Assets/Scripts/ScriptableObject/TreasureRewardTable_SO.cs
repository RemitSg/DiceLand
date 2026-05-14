using UnityEngine;

[CreateAssetMenu(fileName = "New TreasureRewardTable", menuName = "Map/Treasure Reward Table")]
public class TreasureRewardTable_SO : ScriptableObject
{
    [Header("Money")]
    [SerializeField, Min(0)] private int minMoney = 30;
    [SerializeField, Min(0)] private int maxMoney = 50;

    [Header("Blueprint Reward")]
    [SerializeField] private BlueprintTable_SO blueprintTable;
    [SerializeField] private LootRarityWeightTable_SO blueprintRarityWeightTable;

    [Header("Relic Bag Reward")]
    [SerializeField] private RelicTable_SO relicTable;
    [SerializeField] private LootRarityWeightTable_SO relicBagRarityWeightTable;

    public int MinMoney => Mathf.Max(0, minMoney);
    public int MaxMoney => Mathf.Max(MinMoney, maxMoney);
    public BlueprintTable_SO BlueprintTable => blueprintTable;
    public LootRarityWeightTable_SO BlueprintRarityWeightTable => blueprintRarityWeightTable;
    public RelicTable_SO RelicTable => relicTable;
    public LootRarityWeightTable_SO RelicBagRarityWeightTable => relicBagRarityWeightTable;

    public void ConfigureRuntimeDefaults(
        BlueprintTable_SO runtimeBlueprintTable,
        LootRarityWeightTable_SO runtimeBlueprintRarityWeightTable,
        RelicTable_SO runtimeRelicTable,
        LootRarityWeightTable_SO runtimeRelicBagRarityWeightTable)
    {
        if (blueprintTable == null)
            blueprintTable = runtimeBlueprintTable;
        if (blueprintRarityWeightTable == null)
            blueprintRarityWeightTable = runtimeBlueprintRarityWeightTable;
        if (relicTable == null)
            relicTable = runtimeRelicTable;
        if (relicBagRarityWeightTable == null)
            relicBagRarityWeightTable = runtimeRelicBagRarityWeightTable;
    }

    private void OnValidate()
    {
        minMoney = Mathf.Max(0, minMoney);
        maxMoney = Mathf.Max(minMoney, maxMoney);
    }
}
