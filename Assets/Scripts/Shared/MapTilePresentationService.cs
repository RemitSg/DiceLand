public static class MapTilePresentationService
{
    public static string GetTileTypeLabel(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Empty:
                return "空白";
            case MapTileType.Battle:
                return "战斗";
            case MapTileType.Elite:
                return "精英";
            case MapTileType.Event:
                return "事件";
            case MapTileType.Merchant:
                return "商人";
            case MapTileType.Treasure:
                return "\u5b9d\u85cf";
            case MapTileType.Forge:
                return "锻造";
            case MapTileType.Rest:
                return "休息";
            case MapTileType.Key:
                return "钥匙";
            case MapTileType.Boss:
                return "Boss";
            case MapTileType.SpecialDungeonExit:
                return "副本出口";
            default:
                return "未知";
        }
    }

    public static string GetDangerBandLabel(MapDangerBand dangerBand)
    {
        switch (dangerBand)
        {
            case MapDangerBand.Outer:
                return "外围";
            case MapDangerBand.Mid:
                return "中层";
            case MapDangerBand.Inner:
                return "内层";
            case MapDangerBand.Core:
                return "核心";
            default:
                return "未知";
        }
    }

    public static string BuildUnavailableEnemyGroupMessage(MapTileType tileType, MapDangerBand dangerBand)
    {
        return $"{GetTileTypeLabel(tileType)} / {GetDangerBandLabel(dangerBand)} 没有可用敌人组。";
    }
}

public static class MapDangerRatingService
{
    private const int MinRating = 1;
    private const int MaxRating = 8;

    public static int ResolveDefaultRating(MapTileType tileType, MapDangerBand dangerBand)
    {
        switch (tileType)
        {
            case MapTileType.Boss:
                return 7;
            case MapTileType.Elite:
                switch (dangerBand)
                {
                    case MapDangerBand.Outer:
                        return 2;
                    case MapDangerBand.Mid:
                        return 4;
                    case MapDangerBand.Inner:
                        return 5;
                    case MapDangerBand.Core:
                    default:
                        return 6;
                }
            case MapTileType.Battle:
                switch (dangerBand)
                {
                    case MapDangerBand.Outer:
                        return 1;
                    case MapDangerBand.Mid:
                        return 2;
                    case MapDangerBand.Inner:
                        return 3;
                    case MapDangerBand.Core:
                    default:
                        return 4;
                }
            default:
                return 0;
        }
    }

    public static int ResolveRating(
        MapTileType tileType,
        MapDangerBand dangerBand,
        EnemyGroup_SO enemyGroup = null,
        int configuredOverride = 0)
    {
        if (configuredOverride > 0)
            return ClampRating(configuredOverride);

        if (enemyGroup != null && enemyGroup.dangerRatingOverride > 0)
            return ClampRating(enemyGroup.dangerRatingOverride);

        return ResolveDefaultRating(tileType, dangerBand);
    }

    public static string GetRatingPhrase(int rating)
    {
        switch (ClampRating(rating))
        {
            case 1:
                return "轻松";
            case 2:
                return "吃力";
            case 3:
                return "艰难";
            case 4:
                return "十分艰难";
            case 5:
                return "极其艰难";
            case 6:
                return "噩梦";
            case 7:
                return "绝望";
            case 8:
            default:
                return "死亡";
        }
    }

    public static string GetRatingRichText(int rating)
    {
        int safeRating = ClampRating(rating);
        return $"<color=#{GetRatingColorHex(safeRating)}>{GetRatingPhrase(safeRating)}</color>";
    }

    public static string GetRatingColorHex(int rating)
    {
        switch (ClampRating(rating))
        {
            case 1:
                return "FF8A7A";
            case 2:
                return "FF6A5C";
            case 3:
                return "EF4E43";
            case 4:
                return "D93632";
            case 5:
                return "B91F25";
            case 6:
                return "8F1118";
            case 7:
                return "000000";
            case 8:
            default:
                return "2A0000";
        }
    }

    public static int ClampRating(int rating)
    {
        if (rating <= 0)
            return MinRating;

        return rating > MaxRating ? MaxRating : rating;
    }
}
