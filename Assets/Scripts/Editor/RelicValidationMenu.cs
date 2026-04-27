using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RelicValidationMenu
{
    [MenuItem("Tools/Relics/Validate Relics")]
    public static void ValidateRelics()
    {
        List<RelicValidationIssue> issues = new List<RelicValidationIssue>();
        Relic_SO[] relics = LoadAssets<Relic_SO>();
        RelicTable_SO[] relicTables = LoadAssets<RelicTable_SO>();

        for (int i = 0; i < relics.Length; i++)
            issues.AddRange(RelicValidationUtility.ValidateRelic(relics[i]));

        for (int i = 0; i < relicTables.Length; i++)
            issues.AddRange(RelicValidationUtility.ValidateRelicTable(relicTables[i]));

        int errorCount = 0;
        int warningCount = 0;
        for (int i = 0; i < issues.Count; i++)
        {
            RelicValidationIssue issue = issues[i];
            if (issue.Severity == RelicValidationSeverity.Error)
            {
                errorCount++;
                Debug.LogError(issue.ToString(), issue.Asset);
            }
            else
            {
                warningCount++;
                Debug.LogWarning(issue.ToString(), issue.Asset);
            }
        }

        Debug.LogFormat(
            "Relic validation completed. Scanned {0} relics and {1} relic tables. Errors: {2}, Warnings: {3}.",
            relics.Length,
            relicTables.Length,
            errorCount,
            warningCount);
    }

    private static T[] LoadAssets<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        List<T> assets = new List<T>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                assets.Add(asset);
        }

        return assets.ToArray();
    }
}
