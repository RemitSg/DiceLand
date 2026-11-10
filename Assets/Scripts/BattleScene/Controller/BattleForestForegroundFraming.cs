using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Keeps the outer and lower edges of soft foreground cards outside the view without resizing them.</summary>
public sealed class BattleForestForegroundFraming
{
    private sealed class Card
    {
        public SpriteRenderer renderer;
        public Vector3 original, applied;
        public bool left;
    }

    private readonly List<Card> cards = new List<Card>();
    private static readonly int SoftId = Shader.PropertyToID("_SoftSilhouette");
    private const float Overscan = .06f;

    public void Initialize(Transform scenery)
    {
        Release();
        if (scenery == null) return;
        foreach (SpriteRenderer renderer in scenery.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Material material = renderer.sharedMaterial;
            if (renderer.sprite == null || material == null || material.renderQueue < (int)RenderQueue.Transparent
                || !material.HasProperty(SoftId) || material.GetFloat(SoftId) <= .5f) continue;
            cards.Add(new Card { renderer = renderer, original = renderer.transform.localPosition,
                applied = renderer.transform.localPosition,
                left = scenery.InverseTransformPoint(renderer.bounds.center).x < 0f });
        }
    }

    public void PrepareCamera(Camera camera)
    {
        if (camera == null) return;
        foreach (Card card in cards)
        {
            SpriteRenderer renderer = card.renderer;
            if (renderer == null) continue;
            Transform transform = renderer.transform;
            transform.localPosition = card.original;
            card.applied = card.original;
            if (renderer.sprite == null || Mathf.Abs(transform.lossyScale.x) < .0001f
                || Mathf.Abs(transform.lossyScale.y) < .0001f) continue;
            Plane plane = new Plane(transform.forward, transform.position);
            Vector2 min = Vector2.one * float.PositiveInfinity, max = Vector2.one * float.NegativeInfinity;
            bool valid = true;
            for (int corner = 0; corner < 4; corner++)
            {
                Ray ray = camera.ViewportPointToRay(new Vector3((corner & 1) == 0 ? -Overscan : 1f + Overscan,
                    (corner & 2) == 0 ? -Overscan : 1f + Overscan, 0f));
                if (!plane.Raycast(ray, out float distance) || distance > camera.farClipPlane
                    || float.IsNaN(distance) || float.IsInfinity(distance)) { valid = false; break; }
                Vector2 point = transform.InverseTransformPoint(ray.GetPoint(distance));
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            if (!valid) continue;
            Bounds bounds = renderer.sprite.bounds;
            Vector3 center = bounds.center;
            if (renderer.flipX) center.x = -center.x;
            if (renderer.flipY) center.y = -center.y;
            bounds.center = center;
            Vector3 worldCenter = transform.TransformPoint(center);
            Vector3 viewCenter = camera.WorldToViewportPoint(worldCenter);
            bool axisRight = camera.WorldToViewportPoint(worldCenter + transform.TransformVector(Vector3.right)).x > viewCenter.x;
            bool axisUp = camera.WorldToViewportPoint(worldCenter + transform.TransformVector(Vector3.up)).y > viewCenter.y;
            Vector3 offset = new Vector3(card.left == axisRight ? Mathf.Min(0f, min.x - bounds.min.x) : Mathf.Max(0f, max.x - bounds.max.x),
                axisUp ? Mathf.Min(0f, min.y - bounds.min.y) : Mathf.Max(0f, max.y - bounds.max.y), 0f);
            // Translation stays in the card's plane, so the ray intersections remain valid.
            transform.position += transform.TransformVector(offset);
            card.applied = transform.localPosition;
        }
    }

    public void Release()
    {
        foreach (Card card in cards)
            if (card.renderer != null && card.renderer.transform.localPosition == card.applied)
                card.renderer.transform.localPosition = card.original;
        cards.Clear();
    }
}
