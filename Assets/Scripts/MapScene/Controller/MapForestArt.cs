using System.Collections.Generic;
using UnityEngine;

/// <summary>Packaged map artwork shared by world tiles and native UGUI. Contains no gameplay rules.</summary>
public static class MapForestArt
{
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private static bool loaded;

    public static bool Available => Get("TileFace") != null;
    public static Sprite Pawn => Resources.Load<Sprite>("MapUI/ChibiPawn");

    public static Sprite Get(string name)
    {
        if (!loaded)
        {
            loaded = true;
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("MapUI/MapAtlas"))
                sprites[sprite.name] = sprite;
        }
        return sprites.TryGetValue(name, out Sprite value) ? value : null;
    }

    public static Sprite Background(MapBackgroundPhase phase)
    {
        return Resources.Load<Sprite>("MapUI/Forest" + phase);
    }

    public static Sprite Icon(MapTileType type)
    {
        switch (type)
        {
            case MapTileType.Battle: return Get("Sword");
            case MapTileType.Elite: return Get("Elite");
            case MapTileType.Event: return Get("Event");
            case MapTileType.Merchant: return Get("Merchant");
            case MapTileType.Treasure: return Get("Treasure");
            case MapTileType.Forge: return Get("Forge");
            case MapTileType.Rest: return Get("Rest");
            case MapTileType.Key: return Get("Key");
            case MapTileType.Boss: return Get("Boss");
            case MapTileType.Empty: return Get("Footprint");
            default: return null;
        }
    }

    public static Color TileColor(BoardTileData tile, bool collapsed)
    {
        if (collapsed) return new Color(0.31f, 0.36f, 0.34f);
        switch (tile.tileType)
        {
            case MapTileType.Battle: return new Color(0.62f, 0.60f, 0.38f);
            case MapTileType.Elite: return new Color(0.66f, 0.43f, 0.32f);
            case MapTileType.Event: return new Color(0.43f, 0.58f, 0.60f);
            case MapTileType.Rest: return new Color(0.43f, 0.60f, 0.49f);
            case MapTileType.Merchant: return new Color(0.65f, 0.59f, 0.42f);
            case MapTileType.Treasure: return new Color(0.78f, 0.65f, 0.35f);
            case MapTileType.Forge: return new Color(0.66f, 0.57f, 0.41f);
            case MapTileType.Key: return new Color(0.66f, 0.65f, 0.38f);
            case MapTileType.Boss: return new Color(0.60f, 0.32f, 0.34f);
            case MapTileType.SpecialDungeonExit: return new Color(0.40f, 0.66f, 0.62f);
            default: return new Color(0.35f, 0.41f, 0.38f);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reload()
    {
        loaded = false;
        sprites.Clear();
    }
}
