using UnityEngine;

public static class BattlePowerCalculationService
{
    public static int ClampFinalPower(int basePower, int bonusPower)
    {
        return Mathf.Max(1, basePower + bonusPower);
    }

    public static string FormatPowerWithBonus(int basePower, int bonusPower)
    {
        if (bonusPower == 0)
            return basePower.ToString();

        int finalPower = ClampFinalPower(basePower, bonusPower);
        return $"{basePower}{(bonusPower > 0 ? "+" : string.Empty)}{bonusPower}={finalPower}";
    }
}
