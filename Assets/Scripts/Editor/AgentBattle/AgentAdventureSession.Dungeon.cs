using System.Linq;

public sealed partial class AgentAdventureSession
{
    private AgentAdventureDungeon DescribeDungeon()
    {
        if (!map.IsInSpecialDungeon) return null;
        SpecialDungeonRuntimeContext context = map.Snapshot.specialDungeonContext;
        BoardTileData[] exits = map.Snapshot.tiles.Where(t => t.tileType == MapTileType.SpecialDungeonExit).OrderBy(t => t.coord.x).ToArray();
        return new AgentAdventureDungeon
        {
            active = true,
            id = AssetId(context.activeDungeon), name = context.activeDungeon != null ? context.activeDungeon.DisplayName : context.activeDungeonId,
            description = context.activeDungeon != null ? context.activeDungeon.description : "",
            returnX = context.returnEventCoord.x, returnY = context.returnEventCoord.y,
            leftExitX = exits[0].coord.x, leftExitY = exits[0].coord.y, rightExitX = exits[exits.Length - 1].coord.x, rightExitY = exits[exits.Length - 1].coord.y,
            scoreValue = context.activeDungeon != null ? context.activeDungeon.settlementScoreValue : 0,
            entriesTotal = map.Snapshot.tiles.Count(t => t.specialDungeonEntryIndex >= 0),
            entriesResolved = map.Snapshot.tiles.Count(t => t.specialDungeonEntryIndex >= 0 && t.isResolved)
        };
    }
}
