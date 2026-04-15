using UnityEngine;

public static class RestSceneModalState
{
    private static Object currentOwner;

    public static bool IsLocked => currentOwner != null;

    public static bool IsLockedBy(Object owner)
    {
        return owner != null && currentOwner == owner;
    }

    public static bool TryAcquire(Object owner)
    {
        if (owner == null)
            return false;

        if (currentOwner == null || currentOwner == owner)
        {
            currentOwner = owner;
            return true;
        }

        return false;
    }

    public static void Release(Object owner)
    {
        if (owner != null && currentOwner == owner)
            currentOwner = null;
    }
}
