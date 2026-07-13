using UnityEngine;

public static class MapBattleTilePreviewMotion
{
    public static float Evaluate(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
