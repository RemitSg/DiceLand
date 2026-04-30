using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class BattleEditorAssetFallbackService
{
    public static List<RelicTable_SO> FindRelicTablesByName(string tableName)
    {
        List<RelicTable_SO> result = new List<RelicTable_SO>();
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(tableName))
            return result;

        AddRelicTables(result, AssetDatabase.FindAssets($"{tableName} t:RelicTable_SO"));
        AddRelicTables(result, AssetDatabase.FindAssets(tableName));
#endif
        return result;
    }

    public static List<Relic_SO> FindAllRelics()
    {
        List<Relic_SO> result = new List<Relic_SO>();
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Relic_SO");
        for (int i = 0; guids != null && i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Relic_SO relic = AssetDatabase.LoadAssetAtPath<Relic_SO>(path);
            if (relic != null)
                result.Add(relic);
        }
#endif
        return result;
    }

#if UNITY_EDITOR
    private static void AddRelicTables(List<RelicTable_SO> result, string[] guids)
    {
        for (int i = 0; guids != null && i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            RelicTable_SO table = AssetDatabase.LoadAssetAtPath<RelicTable_SO>(path);
            if (table != null)
                result.Add(table);
        }
    }
#endif
}
