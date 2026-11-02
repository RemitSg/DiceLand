using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Small shared-material ground shadows keep illustrated combatants grounded.</summary>
[DisallowMultipleComponent]
public sealed class BattleContactShadowController : MonoBehaviour
{
    [SerializeField] private Transform allies;
    [SerializeField] private Transform enemies;
    [SerializeField] private Material shadowMaterial;
    [SerializeField] private float groundHeight = 0.055f;

    private sealed class Entry
    {
        public Transform actor;
        public SpriteRenderer[] sprites;
        public MeshRenderer shadow;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private readonly List<Transform> scratchActors = new List<Transform>();
    private Transform shadowRoot;
    private Mesh quad;
    private float nextRefresh;

    public void Configure(Transform allyRoot, Transform enemyRoot, Material material)
    {
        allies = allyRoot;
        enemies = enemyRoot;
        shadowMaterial = material;
    }

    private void LateUpdate()
    {
        UpdateShadows();
    }

    public void UpdateShadows()
    {
        if (shadowMaterial == null)
            return;
        if (shadowRoot == null)
        {
            shadowRoot = new GameObject("Runtime contact shadows").transform;
            shadowRoot.SetParent(transform, false);
            quad = new Mesh { name = "Ground shadow quad" };
            quad.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f) };
            quad.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.RecalculateBounds();
        }
        if (Time.unscaledTime >= nextRefresh || entries.Count == 0)
        {
            Collect(allies);
            Collect(enemies);
            nextRefresh = Time.unscaledTime + 0.5f;
        }
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            if (entry.actor == null)
            {
                if (entry.shadow != null)
                    Release(entry.shadow.gameObject);
                entries.RemoveAt(i);
                continue;
            }
            Bounds bounds = default;
            bool visible = false;
            foreach (SpriteRenderer sprite in entry.sprites)
            {
                if (sprite == null || !sprite.enabled || sprite.forceRenderingOff || !sprite.gameObject.activeInHierarchy)
                    continue;
                if (!visible)
                    bounds = sprite.bounds;
                else
                    bounds.Encapsulate(sprite.bounds);
                visible = true;
            }
            entry.shadow.enabled = visible;
            if (!visible)
                continue;
            float width = Mathf.Clamp(bounds.size.x * 0.52f, 0.55f, 4.5f);
            entry.shadow.transform.position = new Vector3(bounds.center.x, groundHeight, bounds.center.z + 0.12f);
            entry.shadow.transform.localScale = new Vector3(width, 1f, Mathf.Max(0.32f, width * 0.30f));
        }
    }

    private void Collect(Transform root)
    {
        if (root == null)
            return;
        scratchActors.Clear();
        BattleActorVisualUtility.CollectActorRoots(root, scratchActors);
        foreach (Transform actor in scratchActors)
        {
            if (entries.Exists(item => item.actor == actor))
                continue;
            List<SpriteRenderer> sprites = new List<SpriteRenderer>();
            foreach (SpriteRenderer sprite in actor.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.GetComponentInParent<SpeedButton>() == null && sprite.GetComponentInParent<Weapon>() == null)
                    sprites.Add(sprite);
            if (sprites.Count == 0)
                continue;
            GameObject item = new GameObject("Contact shadow " + actor.name);
            item.transform.SetParent(shadowRoot, false);
            item.AddComponent<MeshFilter>().sharedMesh = quad;
            MeshRenderer renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = shadowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            entries.Add(new Entry { actor = actor, sprites = sprites.ToArray(), shadow = renderer });
        }
    }

    private void OnDisable()
    {
        ReleaseShadows();
    }

    public void ReleaseShadows()
    {
        if (shadowRoot != null)
            Release(shadowRoot.gameObject);
        if (quad != null)
            Release(quad);
        shadowRoot = null;
        quad = null;
        entries.Clear();
        nextRefresh = 0f;
    }

    private static void Release(Object value)
    {
        if (Application.isPlaying)
            Destroy(value);
        else
            DestroyImmediate(value);
    }
}
