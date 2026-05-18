using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SpecialDungeonRuntimeContext
{
    public bool isActive;
    public string activeDungeonId;
    public SpecialDungeon_SO activeDungeon;
    public Vector2Int returnEventCoord;
    public MapRunSnapshot mainMapSnapshot;
    public List<int> deferredCalamityTiers = new List<int>();

    public SpecialDungeonRuntimeContext Clone()
    {
        return new SpecialDungeonRuntimeContext
        {
            isActive = isActive,
            activeDungeonId = activeDungeonId,
            activeDungeon = activeDungeon,
            returnEventCoord = returnEventCoord,
            mainMapSnapshot = mainMapSnapshot != null ? mainMapSnapshot.CloneWithoutSpecialDungeonContext() : null,
            deferredCalamityTiers = deferredCalamityTiers != null ? new List<int>(deferredCalamityTiers) : new List<int>()
        };
    }
}
