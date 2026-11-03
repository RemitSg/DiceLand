using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns conservative bounds for shader-driven foliage without changing source sprites.</summary>
public sealed class BattleForestFoliageWindBounds
{
    private sealed class Entry
    {
        public SpriteRenderer renderer;
        public Sprite sprite;
        public Bounds original, expanded;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private static readonly int WindId = Shader.PropertyToID("_LocalWindStrength");

    public void Initialize(Transform root)
    {
        Release();
        if (root == null) return;
        foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Material material = renderer.sharedMaterial;
            if (renderer.sprite == null || material == null || !material.HasProperty(WindId)
                || material.GetFloat(WindId) <= 0.001f) continue;
            float strength = Mathf.Clamp01(material.GetFloat(WindId));
            // Exact upper bounds of the shared wind harmonics, plus the existing card sway.
            float horizontal = 0.368f * strength + 1.35f * Mathf.Max(0f, material.GetFloat("_SwayAmplitude"));
            float vertical = 0.03768f * strength;
            Vector3 x = renderer.transform.InverseTransformVector(Vector3.right * horizontal);
            Vector3 y = renderer.transform.InverseTransformVector(Vector3.up * vertical);
            Bounds original = renderer.localBounds, expanded = original;
            expanded.extents += new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y), Mathf.Abs(x.z) + Mathf.Abs(y.z));
            renderer.localBounds = expanded;
            entries.Add(new Entry { renderer = renderer, sprite = renderer.sprite,
                original = original, expanded = expanded });
        }
    }

    public void Release()
    {
        foreach (Entry entry in entries)
        {
            SpriteRenderer renderer = entry.renderer;
            if (renderer == null || renderer.localBounds != entry.expanded) continue;
            if (renderer.sprite != entry.sprite || entry.sprite == null || entry.original == entry.sprite.bounds)
                renderer.ResetLocalBounds();
            else renderer.localBounds = entry.original;
        }
        entries.Clear();
    }
}
