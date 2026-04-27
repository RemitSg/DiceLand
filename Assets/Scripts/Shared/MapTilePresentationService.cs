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
            case MapTileType.Forge:
                return "锻造";
            case MapTileType.Rest:
                return "休息";
            case MapTileType.Key:
                return "钥匙";
            case MapTileType.Boss:
                return "Boss";
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
