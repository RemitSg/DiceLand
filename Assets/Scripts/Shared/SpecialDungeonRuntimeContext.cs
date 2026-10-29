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

    public static SpecialDungeonRuntimeContext Create(SpecialDungeon_SO dungeon, MapRunSnapshot mainMap, Vector2Int returnCoord)
    {
        if (dungeon == null || mainMap?.runState == null || mainMap.specialDungeonContext?.isActive == true)
            return null;
        MapRunSnapshot saved = mainMap.CloneWithoutSpecialDungeonContext();
        var context = new SpecialDungeonRuntimeContext
        {
            isActive = true, activeDungeon = dungeon,
            activeDungeonId = !string.IsNullOrWhiteSpace(dungeon.dungeonId) ? dungeon.dungeonId : dungeon.name,
            returnEventCoord = returnCoord, mainMapSnapshot = saved,
            deferredCalamityTiers = new List<int>(saved.pendingCalamityTiers)
        };
        // The deferred queue has one owner, so it is not restored a second time on exit.
        saved.pendingCalamityTiers.Clear();
        return context;
    }

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
