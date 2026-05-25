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

        SpeedButton speedButtonAncestor = renderer.GetComponentInParent<SpeedButton>();
        if (speedButtonAncestor != null && speedButtonAncestor.transform != actor)
            return true;

        Weapon weaponAncestor = renderer.GetComponentInParent<Weapon>();
        if (weaponAncestor != null && weaponAncestor.transform != actor)
            return true;

        return false;
    }
}

public static class BattleEnemyDataAllyVisualService
{
    public static void ApplyMirroredEnemyDataSprites(Transform root)
    {
        if (root == null)
            return;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (ShouldSkipMirroringRenderer(root, renderer))
                continue;

            renderer.flipX = !renderer.flipX;
        }
    }

    private static bool ShouldSkipMirroringRenderer(Transform root, SpriteRenderer renderer)
    {
        if (root == null || renderer == null)
            return true;

        SpeedButton speedButtonAncestor = renderer.GetComponentInParent<SpeedButton>();
        return speedButtonAncestor != null && speedButtonAncestor.transform != root;
    }

    public static bool ShouldMirrorEnemyDataSkillSprite(Transform source)
    {
        if (source == null)
            return false;

        BattleAllyController ally = source.GetComponent<BattleAllyController>();
        if (ally == null)
            ally = source.GetComponentInParent<BattleAllyController>();

        return ally != null && ally.MirrorsEnemyDataSprites;
    }
}
