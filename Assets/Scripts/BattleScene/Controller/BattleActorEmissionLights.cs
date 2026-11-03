using System.Collections.Generic;
using UnityEngine;

/// <summary>Owned point-light proxies for explicitly authored emissive sprite regions.</summary>
public sealed class BattleActorEmissionLights
{
    private readonly List<Light> lights = new List<Light>();
    public const int MaximumLights = 2;

    public void Update(SpriteRenderer source, BattleActorFormLibrary library, Transform owner, float weight, float pulse)
    {
        bool visible = source != null && source.sprite != null && source.enabled && !source.forceRenderingOff
            && source.gameObject.activeInHierarchy && source.color.a > 0f && weight > 0.001f;
        if (!visible)
        {
            foreach (Light light in lights) if (light != null) light.enabled = false;
            return;
        }
        BattleActorFormLibrary.EmissionLight[] profiles = library != null && library.UsesEmissiveAlpha(source.sprite)
            ? library.FindEmissionLights(source.sprite) : System.Array.Empty<BattleActorFormLibrary.EmissionLight>();
        int count = Mathf.Min(MaximumLights, profiles.Length);
        while (lights.Count > count)
        {
            Release(lights[lights.Count - 1]);lights.RemoveAt(lights.Count - 1);
        }
        float height = source.bounds.size.y;
        for (int i = 0; i < count; i++)
        {
            if (i >= lights.Count)
            {
                GameObject item = new GameObject("Forest emissive point " + source.name + " " + i);
                item.hideFlags = HideFlags.HideAndDontSave;item.transform.SetParent(owner, false);
                Light light = item.AddComponent<Light>();light.type = LightType.Point;
                light.shadows = LightShadows.None;light.renderMode = LightRenderMode.ForcePixel;
                lights.Add(light);
            }
            Light point = lights[i];
            BattleActorFormLibrary.EmissionLight profile = profiles[i];
            Vector2 pixel = Vector2.Scale(source.sprite.rect.size,profile.uv) - source.sprite.pivot;
            Vector3 local = pixel / source.sprite.pixelsPerUnit;
            local.x *= source.flipX ? -1f : 1f;local.y *= source.flipY ? -1f : 1f;
            Vector3 position = source.transform.TransformPoint(local);
            // The illustration has no thickness. Move the light slightly toward the viewing side.
            position.z -= height * 0.08f;
            point.transform.position = position;
            point.range = Mathf.Max(0.001f, profile.range * height);
            point.color = profile.color * new Color(source.color.r,source.color.g,source.color.b,1f);
            point.intensity = Mathf.Max(0f,profile.intensity) * height * height * Mathf.Clamp01(weight)
                * source.color.a * Mathf.Max(0f,pulse);
            point.enabled = profile.range > 0f && point.intensity > 0.001f;
        }
    }

    public void Release()
    {
        foreach (Light light in lights) Release(light);
        lights.Clear();
    }

    private static void Release(Light light)
    {
        if (light == null) return;
        light.enabled = false;
        if (Application.isPlaying) Object.Destroy(light.gameObject);
        else Object.DestroyImmediate(light.gameObject);
    }
}
