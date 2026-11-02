using UnityEngine;

public static class MapBattleTilePreviewMotion
{
    public static float Evaluate(float t)
    {
        t = Mathf.Clamp01(t);
        // Zero velocity and acceleration at both ends avoid a visible kick or hard stop.
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}
