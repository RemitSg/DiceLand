using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class EncyclopediaCatalogBuilder : IPreprocessBuildWithReport
{
    public const string AssetPath = "Assets/Resources/MainMenu/EncyclopediaCatalog.asset";
    public const string IdentityAssetPath = "Assets/Resources/MainMenu/EncyclopediaIdentities.txt";
    private static readonly string[] ContentTypes = { "EnemyData_SO", "Weapon_SO", "Relic_SO", "LootItem_SO", "Calamity_SO" };
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) => Rebuild();

    [MenuItem("Tools/DiceLand/Rebuild Encyclopedia Catalog")]
    public static void Rebuild()
    {
        EncyclopediaCatalog.Entry[] entries = FindEntries();
        string identities = BuildIdentityIndex(entries);
        if (!File.Exists(IdentityAssetPath) || File.ReadAllText(IdentityAssetPath) != identities)
        {
            File.WriteAllText(IdentityAssetPath, identities);
            AssetDatabase.ImportAsset(IdentityAssetPath);
        }

        var heading = LoadFont("c6e7143cd2df45b45a784ef8c61a210f");
        var body = LoadFont("3285407dd9827054e9f4e40c9f7b9009");
        var catalog = AssetDatabase.LoadAssetAtPath<EncyclopediaCatalog>(AssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<EncyclopediaCatalog>();
            catalog.SetContents(entries, heading, body);
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }
        else if (catalog.HeadingFont != heading || catalog.BodyFont != body ||
                 !catalog.Entries.Select(entry => entry.Id).SequenceEqual(entries.Select(entry => entry.Id)))
        {
            catalog.SetContents(entries, heading, body);
            EditorUtility.SetDirty(catalog);
        }
        AssetDatabase.SaveAssetIfDirty(catalog);
    }

    public static string BuildIdentityIndex(IEnumerable<EncyclopediaCatalog.Entry> entries)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return string.Join("\n", entries.Select(entry =>
        {
            string key = EncyclopediaCatalog.AssetKey(entry.Asset);
            if (string.IsNullOrEmpty(key) || key.IndexOfAny(new[] { '\t', '\r', '\n' }) >= 0 || !keys.Add(key))
                throw new BuildFailedException("Invalid or duplicate encyclopedia identity at " + AssetDatabase.GetAssetPath(entry.Asset));
            return key + "\t" + entry.Id;
        })) + "\n";
    }

    public static EncyclopediaCatalog.Entry[] FindEntries()
    {
        // Only include content linked from the shipped scenes or Resources. The index itself is not a root.
        IEnumerable<string> roots = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path)
            .Concat(AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/Resources/", StringComparison.Ordinal) &&
                path != AssetPath && !AssetDatabase.IsValidFolder(path)));
        var included = new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(), true), StringComparer.Ordinal);
        return ContentTypes
            .SelectMany(type => AssetDatabase.FindAssets("t:" + type, new[] { "Assets/Data" }))
            .Distinct()
            .Select(id => (id, path: AssetDatabase.GUIDToAssetPath(id)))
            .Where(entry => included.Contains(entry.path))
            .OrderBy(entry => entry.path, StringComparer.Ordinal)
            .Select(entry => new EncyclopediaCatalog.Entry(entry.id, AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.path)))
            .ToArray();
    }

    private static TMP_FontAsset LoadFont(string guid) =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
}
