public static class BattleTargetingPhaseLogService
{
    public static string BuildPhaseHeader(int livingAllies, int livingEnemies)
    {
        return $"\u25c6\u25c6\u25c6 目标指定阶段 \u25c6\u25c6\u25c6 友方 {livingAllies} 名 / 敌方 {livingEnemies} 名";
    }

    public static string BuildAllySpeedLog(string displayName, int speed, int currentHp, int maxHp)
    {
        return $"友方 {displayName} 速度={speed} HP={currentHp}/{maxHp}";
    }

    public static string BuildEnemySpeedLog(string displayName, int speed, int currentHp, int maxHp)
    {
        return $"敌方 {displayName} 速度={speed} HP={currentHp}/{maxHp}";
    }

    public static string BuildEnemyAutoTargetCompleteLog()
    {
        return "敌方已完成自动指定目标。请玩家完成画线后再次按下战斗按钮进入结算。";
    }
}
