using UnityEngine;

public static class BattleActorVisualUtility
{
    public static Vector3 GetActorCenterWorld(Transform actor)
    {
        if (TryGetActorSpriteBounds(actor, out Bounds bounds))
            return bounds.center;

        return actor != null ? actor.position : Vector3.zero;
    }

    public static bool TryGetActorSpriteBounds(Transform actor, out Bounds bounds)
    {
        bounds = default;
        if (actor == null)
            return false;

        SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (ShouldIgnoreRenderer(actor, renderer))
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds.min);
                bounds.Encapsulate(renderer.bounds.max);
            }
        }

        return hasBounds;
    }

    private static bool ShouldIgnoreRenderer(Transform actor, SpriteRenderer renderer)
    {
        if (actor == null || renderer == null)
            return true;

        if (renderer.GetComponentInParent<SpeedButton>() != null)
            return true;

        Weapon weaponAncestor = renderer.GetComponentInParent<Weapon>();
        if (weaponAncestor != null && weaponAncestor.transform != actor)
            return true;

        return false;
    }
}
