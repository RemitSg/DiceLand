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
                 !catalog.Entries.Select(entry => (entry.Id, entry.Asset, entry.EnemyGroup))
                     .SequenceEqual(entries.Select(entry => (entry.Id, entry.Asset, entry.EnemyGroup))))
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
        var enemyGroups = BuildEnemyGroups(AssetDatabase.FindAssets("t:EnemyRoster_SO", new[] { "Assets/Data" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(included.Contains)
            .Select(AssetDatabase.LoadAssetAtPath<EnemyRoster_SO>));
        return ContentTypes
            .SelectMany(type => AssetDatabase.FindAssets("t:" + type, new[] { "Assets/Data" }))
            .Distinct()
            .Select(id => (id, path: AssetDatabase.GUIDToAssetPath(id)))
            .Where(entry => included.Contains(entry.path))
            .OrderBy(entry => entry.path, StringComparer.Ordinal)
            .Select(entry =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.path);
                var group = asset is EnemyData_SO enemy && enemyGroups.TryGetValue(enemy, out var placement)
                    ? placement : EncyclopediaEnemyGroup.Special;
                return new EncyclopediaCatalog.Entry(entry.id, asset, group);
            })
            .ToArray();
    }

    public static Dictionary<EnemyData_SO, EncyclopediaEnemyGroup> BuildEnemyGroups(IEnumerable<EnemyRoster_SO> rosters)
    {
        var all = (rosters ?? Array.Empty<EnemyRoster_SO>()).Where(roster => roster != null).ToArray();
        var result = new Dictionary<EnemyData_SO, EncyclopediaEnemyGroup>();
        // One fixed slot per enemy: boss first, then earliest area and standard before elite.
        foreach (var roster in all)
            AddMembers(roster.GetBossGroups(), EncyclopediaEnemyGroup.Boss);
        foreach (MapDangerBand band in Enum.GetValues(typeof(MapDangerBand)))
            foreach (EnemyPoolKind kind in Enum.GetValues(typeof(EnemyPoolKind)))
                foreach (var roster in all)
                    AddMembers(roster.GetGroups(band, kind), Placement(band, kind));
        return result;

        void AddMembers(IEnumerable<EnemyGroup_SO> groups, EncyclopediaEnemyGroup placement)
        {
            foreach (var group in groups)
                if (group != null)
                    foreach (var enemy in group.GetEnemies())
                        if (enemy != null && !result.ContainsKey(enemy)) result.Add(enemy, placement);
        }
    }

    private static EncyclopediaEnemyGroup Placement(MapDangerBand band, EnemyPoolKind kind)
    {
        bool elite = kind == EnemyPoolKind.Elite;
        switch (band)
        {
            case MapDangerBand.Outer: return elite ? EncyclopediaEnemyGroup.OuterElite : EncyclopediaEnemyGroup.OuterStandard;
            case MapDangerBand.Mid: return elite ? EncyclopediaEnemyGroup.MidElite : EncyclopediaEnemyGroup.MidStandard;
            case MapDangerBand.Inner: return elite ? EncyclopediaEnemyGroup.InnerElite : EncyclopediaEnemyGroup.InnerStandard;
            case MapDangerBand.Core: return elite ? EncyclopediaEnemyGroup.CoreElite : EncyclopediaEnemyGroup.CoreStandard;
            default: return EncyclopediaEnemyGroup.Special;
        }
    }

    private static TMP_FontAsset LoadFont(string guid) =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
}
