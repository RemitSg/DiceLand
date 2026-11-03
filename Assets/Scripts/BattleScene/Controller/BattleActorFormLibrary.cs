using System;
using UnityEngine;

/// <summary>Optional form normals and material data; original color sprites remain the source art.</summary>
public sealed class BattleActorFormLibrary : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public Sprite sprite;
        public Texture2D formMap;
        public Texture2D materialMap;
        // Legacy material maps have silhouette A; emission must be explicitly opted in.
        public bool emissiveAlpha;
        public bool fabricSheen;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();
    [SerializeField] private bool silhouetteAlpha;

    public Texture2D FindShadowMask(Sprite sprite) => silhouetteAlpha ? Find(sprite) : null;

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
