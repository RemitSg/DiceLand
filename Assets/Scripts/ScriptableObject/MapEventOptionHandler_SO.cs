using UnityEngine;

public sealed class MapEventExecutionContext
{
    public RunState runState;
    public ActionPointSystem actionPointSystem;
    public BoardTileData tile;
    public MapEvent_SO eventData;
    public MapEventOptionData optionData;
    public RunGameManager runGameManager;
}

public abstract class MapEventOptionHandler_SO : ScriptableObject
{
    public abstract bool Execute(MapEventExecutionContext context, out string message);
}
