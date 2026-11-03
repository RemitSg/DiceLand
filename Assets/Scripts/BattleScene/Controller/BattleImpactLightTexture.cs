using UnityEngine;

/// <summary>Owner-created light cores for built-in flashes and small spark billboards.</summary>
public static class BattleImpactLightTexture
{
    public static Texture2D Create(string name)
    {
        const int size = 64;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + 0.5f) / size * 2f - 1f,
                (y + 0.5f) / size * 2f - 1f).magnitude;
            float core = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(radius / 0.32f));
            float halo = 0.55f * Mathf.Pow(Mathf.Max(0f, 1f - radius), 2f);
            pixels[y * size + x] = new Color32(255, 255, 255,
                (byte)Mathf.RoundToInt(Mathf.Clamp01(core + halo) * 255f));
        }

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
