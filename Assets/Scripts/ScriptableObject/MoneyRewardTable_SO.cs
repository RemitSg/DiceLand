using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoneyRewardTable", menuName = "Economy/Money Reward Table")]
public class MoneyRewardTable_SO : ScriptableObject
{
    [Serializable]
    public sealed class MoneyRewardEntry
    {
        public MapTileType tileType;
        public MapDangerBand dangerBand;
        [Min(0)] public int minMoney = 1;
        [Min(0)] public int maxMoney = 3;
    }

    [SerializeField] private List<MoneyRewardEntry> rewardEntries = new List<MoneyRewardEntry>();

    public int RollReward(MapTileType tileType, MapDangerBand dangerBand, int seed)
    {
        MoneyRewardEntry entry = GetEntry(tileType, dangerBand);
        if (entry == null)
            return 0;

        int min = Mathf.Max(0, entry.minMoney);
        int max = Mathf.Max(min, entry.maxMoney);
        if (max <= min)
            return min;

        System.Random rng = new System.Random(seed ^ ((int)tileType * 397) ^ ((int)dangerBand * 977));
        return rng.Next(min, max + 1);
    }

    public MoneyRewardEntry GetEntry(MapTileType tileType, MapDangerBand dangerBand)
    {
        if (rewardEntries == null)
            return null;

        for (int i = 0; i < rewardEntries.Count; i++)
        {
            MoneyRewardEntry entry = rewardEntries[i];
            if (entry == null)
                continue;

            if (entry.tileType == tileType && entry.dangerBand == dangerBand)
                return entry;
        }

        return null;
    }

    private void OnValidate()
    {
        if (rewardEntries == null)
            rewardEntries = new List<MoneyRewardEntry>();
    }
}
