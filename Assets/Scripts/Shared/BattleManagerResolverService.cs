using System;

public static class BattleManagerResolverService
{
    public static bool TryResolve(
        BattleManager current,
        Func<BattleManager> finder,
        out BattleManager resolved,
        out string warningMessage)
    {
        if (current != null)
        {
            resolved = current;
            warningMessage = null;
            return true;
        }

        resolved = finder != null ? finder() : null;
        if (resolved != null)
        {
            warningMessage = null;
            return true;
        }

        warningMessage = "[BattleButton] BattleManager 未找到。";
        return false;
    }
}
