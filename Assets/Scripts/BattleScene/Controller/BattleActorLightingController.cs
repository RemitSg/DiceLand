using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns temporary forest surface materials; shared assets and battle rules remain untouched.</summary>
[DisallowMultipleComponent]
public sealed class BattleActorLightingController : MonoBehaviour
{
    [SerializeField] private Transform allies;
    [SerializeField] private Transform enemies;
    [SerializeField, Range(0f, 1f)] private float surfaceWeight = 0.8f;
    private static readonly int BoundsId = Shader.PropertyToID("_ForestSpriteBounds");
    private static readonly int WeightId = Shader.PropertyToID("_ForestSurfaceWeight");
    private readonly List<Transform> actors = new List<Transform>();
    private readonly List<Entry> entries = new List<Entry>();
    private float nextRefresh;

    private sealed class Entry
    {
        public SpriteRenderer renderer;
        public Material originalMaterial;
        public Material runtimeMaterial;
        public Bounds lastBounds;
        public float lastWeight = -1f;
    }

    public void Configure(Transform allyRoot, Transform enemyRoot)
    {
        ReleaseLighting();
        allies = allyRoot;
        enemies = enemyRoot;
    }

    private void LateUpdate() => UpdateLighting();

    public void UpdateLighting()
    {
        if (Time.unscaledTime >= nextRefresh || entries.Count == 0)
        {
            actors.Clear();
            BattleActorVisualUtility.CollectActorRoots(allies, actors);
            BattleActorVisualUtility.CollectActorRoots(enemies, actors);
            foreach (Transform actor in actors)
            foreach (SpriteRenderer renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.GetComponentInParent<SpeedButton>() != null || renderer.sharedMaterial == null
                    || !renderer.sharedMaterial.HasProperty(WeightId) || entries.Exists(e => e.renderer == renderer))
                    continue;
                Material original = renderer.sharedMaterial;
                Material runtime = new Material(original) { name = "Forest surface (runtime) " + renderer.name,
                    hideFlags = HideFlags.HideAndDontSave };
                renderer.sharedMaterial = runtime;
                entries.Add(new Entry { renderer = renderer, originalMaterial = original, runtimeMaterial = runtime });
            }
            nextRefresh = Time.unscaledTime + 0.5f;
        }
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            SpriteRenderer renderer = entry.renderer;
            if (renderer == null)
            {
                Release(entry.runtimeMaterial);
                entries.RemoveAt(i);
                continue;
            }
            // Temporary flash/override materials retain ownership until their original material returns.
            if (renderer.sharedMaterial != entry.runtimeMaterial) continue;
            Bounds bounds = renderer.bounds;
            if (bounds.size.x < 0.001f || bounds.size.y < 0.001f) continue;
            Vector4 packedBounds = new Vector4(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
            if (entry.lastBounds == bounds && Mathf.Approximately(entry.lastWeight, surfaceWeight)) continue;
            entry.runtimeMaterial.SetVector(BoundsId, packedBounds);
            entry.runtimeMaterial.SetFloat(WeightId, surfaceWeight);
            entry.lastBounds = bounds;
            entry.lastWeight = surfaceWeight;
        }
    }

    private void OnDisable() => ReleaseLighting();

    public void ReleaseLighting()
    {
        foreach (Entry entry in entries)
        {
            if (entry.renderer != null && entry.renderer.sharedMaterial == entry.runtimeMaterial)
                entry.renderer.sharedMaterial = entry.originalMaterial;
            Release(entry.runtimeMaterial);
        }
        entries.Clear();
        actors.Clear();
        nextRefresh = 0f;
    }

    private static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
