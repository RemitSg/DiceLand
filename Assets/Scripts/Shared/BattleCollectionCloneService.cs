using System.Collections.Generic;

public static class BattleCollectionCloneService
{
    public static List<T> CloneNonNullReferences<T>(IReadOnlyList<T> source) where T : class
    {
        List<T> clone = new List<T>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                clone.Add(source[i]);
        }

        return clone;
    }
}
