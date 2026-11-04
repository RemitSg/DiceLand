using System;
using UnityEngine;

/// <summary>Optional form normals and material data; original color sprites remain the source art.</summary>
public sealed class BattleActorFormLibrary : ScriptableObject
{
    [Serializable]
    public struct EmissionLight
    {
        public Vector2 uv;
        public Color color;
        // Range and intensity are normalized to the displayed sprite's height.
        public float range;
        public float intensity;
    }

    [Serializable]
    public struct Entry
    {
        public Sprite sprite;
        public Texture2D formMap;
        public Texture2D materialMap;
        // Legacy material maps have silhouette A; emission must be explicitly opted in.
        public bool emissiveAlpha;
        public bool fabricSheen;
        // Material A is hair coverage only on tagged, non-emissive maps.
        public bool hairAlpha;
        public Vector2 hairDirection;
        public EmissionLight[] emissionLights;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    [SerializeField] private bool silhouetteAlpha;

    public Texture2D FindShadowMask(Sprite sprite) => silhouetteAlpha ? Find(sprite) : null;

    public EmissionLight[] FindEmissionLights(Sprite sprite)
    {
        if (sprite == null || sprite.packed) return Array.Empty<EmissionLight>();
        foreach (Entry entry in entries)
            if (entry.sprite == sprite) return entry.emissionLights ?? Array.Empty<EmissionLight>();
        return Array.Empty<EmissionLight>();
    }

    public Texture2D FindMaterialMap(Sprite sprite)
    {
        if (sprite == null || sprite.packed) return null;
        foreach (Entry entry in entries)
            if (entry.sprite == sprite) return entry.materialMap;
        return null;
    }

    public bool UsesEmissiveAlpha(Sprite sprite)
    {
        if (sprite == null || sprite.packed) return false;
        foreach (Entry entry in entries)
            if (entry.sprite == sprite) return entry.materialMap != null && entry.emissiveAlpha;
        return false;
    }

    public bool UsesFabricSheen(Sprite sprite)
    {
        if (sprite == null || sprite.packed) return false;
        foreach (Entry entry in entries)
            if (entry.sprite == sprite) return entry.materialMap != null && entry.fabricSheen;
        return false;
    }

    public bool UsesHairAlpha(Sprite sprite)
    {
        if (sprite == null || sprite.packed) return false;
        foreach (Entry entry in entries)
            if (entry.sprite == sprite)
                return entry.materialMap != null && entry.hairAlpha && !entry.emissiveAlpha;
        return false;
    }

    public Vector2 FindHairDirection(Sprite sprite)
    {
        if (sprite == null || sprite.packed) return Vector2.right;
        foreach (Entry entry in entries)
            if (entry.sprite == sprite)
            {
                float lengthSquared = entry.hairDirection.sqrMagnitude;
                return !float.IsNaN(lengthSquared) && !float.IsInfinity(lengthSquared) && lengthSquared > 0.0001f
                    ? entry.hairDirection / Mathf.Sqrt(lengthSquared) : Vector2.right;
            }
        return Vector2.right;
    }

    public Texture2D Find(Sprite sprite)
    {
        // These maps use the original texture's UVs. A packed/rotated atlas retains the old shading.
        if (sprite == null || sprite.packed)
            return null;
        foreach (Entry entry in entries)
            if (entry.sprite == sprite)
                return entry.formMap;
        return null;
    }
}
