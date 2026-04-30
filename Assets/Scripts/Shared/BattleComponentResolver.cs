using UnityEngine;

public static class BattleComponentResolver
{
    public static T ResolveSceneOrHost<T>(
        T current,
        GameObject host,
        bool includeInactive,
        bool addIfMissing)
        where T : Component
    {
        if (current != null)
            return current;

        T resolved = Object.FindObjectOfType<T>(includeInactive);
        if (resolved != null)
            return resolved;

        if (host == null)
            return null;

        resolved = host.GetComponent<T>();
        if (resolved != null || !addIfMissing)
            return resolved;

        return host.AddComponent<T>();
    }
}
