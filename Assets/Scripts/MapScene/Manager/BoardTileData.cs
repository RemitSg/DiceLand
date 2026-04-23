using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapTileType
{
    Empty = 0,
    Battle = 1,
    Elite = 2,
    Event = 3,
    Merchant = 4,
    Forge = 5,
    Rest = 6,
    Key = 7,
    Boss = 8
}

public enum MapDangerBand
{
    Outer = 0,
    Mid = 1,
    Inner = 2,
    Core = 3
}

[Serializable]
public class BoardTileData
{
    public Vector2Int coord;
    public MapTileType tileType;
    public MapDangerBand dangerBand;
    public bool isVisited;
    public bool isResolved;
    public bool isSpawnTile;
    public int encounterSeed;
    public int previewEnemyCount;
    public int previewDangerRating;
    public string previewTag;
    public EnemyGroup_SO assignedEnemyGroup;
    public MapEvent_SO assignedEvent;
    public bool merchantStockGenerated;
    public int merchantRefreshCount;
    public int merchantPurchaseDiscountStacks;
    public List<MerchantOfferState> merchantOffers = new List<MerchantOfferState>();

    public bool IsHostileTile =>
        tileType == MapTileType.Battle ||
        tileType == MapTileType.Elite ||
        tileType == MapTileType.Boss;

    public BoardTileData Clone()
    {
        return new BoardTileData
        {
            coord = coord,
            tileType = tileType,
            dangerBand = dangerBand,
            isVisited = isVisited,
            isResolved = isResolved,
            isSpawnTile = isSpawnTile,
            encounterSeed = encounterSeed,
            previewEnemyCount = previewEnemyCount,
            previewDangerRating = previewDangerRating,
            previewTag = previewTag,
            assignedEnemyGroup = assignedEnemyGroup,
            assignedEvent = assignedEvent,
            merchantStockGenerated = merchantStockGenerated,
            merchantRefreshCount = merchantRefreshCount,
            merchantPurchaseDiscountStacks = merchantPurchaseDiscountStacks,
            merchantOffers = CloneMerchantOffers()
        };
    }

    private List<MerchantOfferState> CloneMerchantOffers()
    {
        List<MerchantOfferState> clone = new List<MerchantOfferState>();
        if (merchantOffers == null)
            return clone;

        for (int i = 0; i < merchantOffers.Count; i++)
        {
            MerchantOfferState offer = merchantOffers[i];
            if (offer != null)
                clone.Add(offer.Clone());
        }

        return clone;
    }

    public string GetDangerLabel()
    {
        switch (dangerBand)
        {
            case MapDangerBand.Outer:
                return "Outer";
            case MapDangerBand.Mid:
                return "Mid";
            case MapDangerBand.Inner:
                return "Inner";
            default:
                return "Core";
        }
    }
}
