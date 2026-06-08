public sealed class MapTutorialFlowState
{
    public bool merchantGuideShown;
    public bool forgeGuideShown;
    public bool eliteGuideShown;
}

public readonly struct MapTutorialGuideContent
{
    public readonly string title;
    public readonly string body;

    public MapTutorialGuideContent(string title, string body)
    {
        this.title = title;
        this.body = body;
    }
}

public static class MapTutorialFlowService
{
    public static bool TryConsumeArrivalGuide(MapTutorialFlowState state, MapTileType tileType, out MapTutorialGuideContent content)
    {
        content = default;
        if (state == null)
            return false;

        switch (tileType)
        {
            case MapTileType.Merchant:
                if (state.merchantGuideShown)
                    return false;

                state.merchantGuideShown = true;
                content = new MapTutorialGuideContent(
                    "商店格",
                    "这里是商店格。你可以花费金币购买战利品、图纸、武器或遗物。\n\n战利品和图纸可以用于后续锻造，武器能直接提升战斗力，遗物则会提供各种特殊增益。教程商店里的商品都很便宜，先把需要的材料和图纸买下来吧！");
                return true;

            case MapTileType.Forge:
                if (state.forgeGuideShown)
                    return false;

                state.forgeGuideShown = true;
                content = new MapTutorialGuideContent(
                    "锻造格",
                    "这里是锻造格。锻造需要图纸和对应材料，完成后可以在背包的武器栏位装备武器。\n\n武器是角色战力提升的主要来源。先根据图纸锻造一把武器，再把它装备起来吧！");
                return true;

            case MapTileType.Elite:
                if (state.eliteGuideShown)
                    return false;

                state.eliteGuideShown = true;
                content = new MapTutorialGuideContent(
                    "精英终战",
                    "最后一场教程战斗来了。这里会综合考验速度、拼点、单方面攻击、技能切换、能量和状态效果。\n\n先看敌人要做什么，再决定哪条线必须接、哪条线可以放、哪一招值得消耗能量。");
                return true;

            default:
                return false;
        }
    }
}
