using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Editor-only adapter. The HTML edits the same ScriptableObject used by MerchantStockService.
public static class MerchantCatalogLiveData
{
    public const string CatalogPath = "Assets/Data/Economy/MerchantCatalog.asset";
    public static readonly string[] Categories = { "loot", "weapon", "blueprint", "relic" };
    private static readonly string[] Titles = { "掉落物", "武器", "图纸", "遗物表" };
    private static readonly string[] References = { "lootItem", "weaponData", "blueprintData", "relicTable" };
    private static readonly string[] Types = { "LootItem_SO", "Weapon_SO", "CraftingBlueprint_SO", "RelicTable_SO" };

    [Serializable] public sealed class AssetInfo
    {
        public string guid, path, id, name, description;
        public int rarity = -1;
        public bool valid = true;
    }
    [Serializable] public sealed class Row
    {
        public string category, goodsId;
        public int index, minPrice, maxPrice, weight;
        public bool legacyRelic;
        public AssetInfo asset;
    }
    [Serializable] public sealed class Category
    {
        public string key, title;
        public int minimumCount, randomWeight, availableCount, entryCount, coveredCount;
    }
    [Serializable] public sealed class Choice
    {
        public string category;
        public AssetInfo asset;
    }
    [Serializable] public sealed class Document
    {
        public string revision = "", assetPath;
        public int minTotalGoodsCount, maxTotalGoodsCount;
        public bool unsavedUnityChanges;
        public List<Category> categories = new List<Category>();
        public List<Row> rows = new List<Row>();
        public List<Choice> available = new List<Choice>();
    }
    [Serializable] public sealed class Change
    {
        public string operation, category, assetGuid, goodsId;
        public int index, minPrice, maxPrice, weight;
        public int minTotalGoodsCount, maxTotalGoodsCount, minimumCount, randomWeight;
    }
    [Serializable] public sealed class Patch
    {
        public string baseRevision;
        public List<Change> changes = new List<Change>();
    }
    public sealed class ConflictException : InvalidOperationException
    {
        public ConflictException() : base("配置已在其他位置修改，未覆盖外部更改。") { }
    }

    public static Document Read(MerchantCatalog_SO catalog)
    {
        if (catalog == null) throw new ArgumentException("找不到商店配置资产。");
        var document = new Document
        {
            assetPath = AssetDatabase.GetAssetPath(catalog),
            minTotalGoodsCount = catalog.minTotalGoodsCount,
            maxTotalGoodsCount = catalog.maxTotalGoodsCount,
            unsavedUnityChanges = EditorUtility.IsDirty(catalog)
        };
        var serialized = new SerializedObject(catalog);
        for (int c = 0; c < Categories.Length; c++)
        {
            string key = Categories[c];
            var category = new Category { key = key, title = Titles[c] };
            var minimum = serialized.FindProperty(key + "MinimumCount");
            category.minimumCount = minimum.FindPropertyRelative("minimumCount").intValue;
            category.randomWeight = minimum.FindPropertyRelative("randomWeight").intValue;
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:" + Types[c], new[] { "Assets" }))
            {
                var asset = Load(key, guid);
                if (asset == null) continue;
                var info = Describe(asset);
                known.Add(info.guid);
                document.available.Add(new Choice { category = key, asset = info });
            }
            category.availableCount = known.Count;
            var covered = new HashSet<string>(StringComparer.Ordinal);
            var pool = serialized.FindProperty(key + "Entries");
            category.entryCount = pool.arraySize;
            for (int i = 0; i < pool.arraySize; i++)
            {
                var entry = pool.GetArrayElementAtIndex(i);
                Object asset = entry.FindPropertyRelative(References[c]).objectReferenceValue;
                bool legacy = false;
                if (asset == null && key == "relic")
                {
                    asset = entry.FindPropertyRelative("legacyRelicData").objectReferenceValue;
                    legacy = asset != null;
                }
                var info = Describe(asset);
                if (info.valid && known.Contains(info.guid)) covered.Add(info.guid);
                document.rows.Add(new Row
                {
                    category = key, index = i, asset = info, legacyRelic = legacy,
                    goodsId = entry.FindPropertyRelative("goodsId").stringValue,
                    minPrice = entry.FindPropertyRelative("minPrice").intValue,
                    maxPrice = entry.FindPropertyRelative("maxPrice").intValue,
                    weight = entry.FindPropertyRelative("weight").intValue
                });
            }
            category.coveredCount = covered.Count;
            document.categories.Add(category);
        }
        document.available.Sort((a, b) => string.CompareOrdinal(a.category + "/" + a.asset.path, b.category + "/" + b.asset.path));
        document.revision = Hash(JsonUtility.ToJson(document) + EditorJsonUtility.ToJson(catalog));
        return document;
    }

    public static Document Apply(MerchantCatalog_SO catalog, Patch patch, bool persist = true)
    {
        if (patch == null || string.IsNullOrEmpty(patch.baseRevision)) throw new ArgumentException("缺少配置版本。");
        if (!string.Equals(Read(catalog).revision, patch.baseRevision, StringComparison.Ordinal)) throw new ConflictException();
        if (patch.changes == null || patch.changes.Count == 0 || patch.changes.Count > 128)
            throw new ArgumentException("每次需要提交 1–128 项修改。");

        // Validate the complete request on a disposable copy, so invalid batches cannot partly write the asset.
        var draft = Object.Instantiate(catalog);
        draft.name = catalog.name;
        try
        {
            foreach (var change in patch.changes) ApplyToDraft(draft, change);
            Validate(draft);
            string before = EditorJsonUtility.ToJson(catalog);
            if (persist) Undo.RegisterCompleteObjectUndo(catalog, "编辑商店商品池");
            try
            {
                EditorUtility.CopySerialized(draft, catalog);
                EditorUtility.SetDirty(catalog);
                if (persist) AssetDatabase.SaveAssetIfDirty(catalog);
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(before, catalog);
                EditorUtility.SetDirty(catalog);
                throw;
            }
            return Read(catalog);
        }
        finally { Object.DestroyImmediate(draft); }
    }

    private static void ApplyToDraft(MerchantCatalog_SO draft, Change change)
    {
        if (change == null) throw new ArgumentException("修改不能为空。");
        var serialized = new SerializedObject(draft);
        if (change.operation == "settings")
        {
            Range(change.minTotalGoodsCount, 0, 10000, "商品总数下限");
            Range(change.maxTotalGoodsCount, change.minTotalGoodsCount, 10000, "商品总数上限");
            serialized.FindProperty("minTotalGoodsCount").intValue = change.minTotalGoodsCount;
            serialized.FindProperty("maxTotalGoodsCount").intValue = change.maxTotalGoodsCount;
        }
        else
        {
            int c = Array.IndexOf(Categories, change.category);
            if (c < 0) throw new ArgumentException("未知商品类型。");
            if (change.operation == "category")
            {
                Range(change.minimumCount, 0, 10000, "保底数量");
                Range(change.randomWeight, 0, 1000000, "类型权重");
                var minimum = serialized.FindProperty(change.category + "MinimumCount");
                minimum.FindPropertyRelative("minimumCount").intValue = change.minimumCount;
                minimum.FindPropertyRelative("randomWeight").intValue = change.randomWeight;
            }
            else
            {
                var pool = serialized.FindProperty(change.category + "Entries");
                if (change.operation == "add")
                {
                    var asset = Load(change.category, change.assetGuid);
                    if (asset == null || (asset is RelicTable_SO && !((RelicTable_SO)asset).IsValid))
                        throw new ArgumentException("所选资产不存在、类型不符或遗物表为空。");
                    for (int i = 0; i < pool.arraySize; i++)
                        if (pool.GetArrayElementAtIndex(i).FindPropertyRelative(References[c]).objectReferenceValue == asset)
                            throw new ArgumentException("此资产已经在商品池中。");
                    int index = pool.arraySize;
                    pool.InsertArrayElementAtIndex(index);
                    var entry = pool.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative(References[c]).objectReferenceValue = asset;
                    if (change.category == "relic") entry.FindPropertyRelative("legacyRelicData").objectReferenceValue = null;
                    string stem = change.category + "_" + Describe(asset).id;
                    string goodsId = stem;
                    int suffix = 2;
                    while (ContainsGoodsId(pool, goodsId, index)) goodsId = stem + "_" + suffix++;
                    SetEntry(entry, goodsId, change);
                }
                else
                {
                    if (change.index < 0 || change.index >= pool.arraySize) throw new ArgumentException("商品行已失效。");
                    var entry = pool.GetArrayElementAtIndex(change.index);
                    Object asset = entry.FindPropertyRelative(References[c]).objectReferenceValue;
                    if (asset == null && change.category == "relic") asset = entry.FindPropertyRelative("legacyRelicData").objectReferenceValue;
                    if (!string.Equals(Describe(asset).guid, change.assetGuid ?? "", StringComparison.Ordinal))
                        throw new ConflictException();
                    if (change.operation == "entry") SetEntry(entry, change.goodsId, change);
                    else if (change.operation == "remove") pool.DeleteArrayElementAtIndex(change.index);
                    else throw new ArgumentException("未知修改操作。");
                }
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEntry(SerializedProperty entry, string goodsId, Change change)
    {
        if (string.IsNullOrWhiteSpace(goodsId) || goodsId.Length > 120) throw new ArgumentException("商品标识需要 1–120 个字符。");
        Range(change.minPrice, 1, 1000000, "价格下限");
        Range(change.maxPrice, change.minPrice, 1000000, "价格上限");
        Range(change.weight, 1, 1000000, "商品权重");
        entry.FindPropertyRelative("goodsId").stringValue = goodsId;
        entry.FindPropertyRelative("minPrice").intValue = change.minPrice;
        entry.FindPropertyRelative("maxPrice").intValue = change.maxPrice;
        entry.FindPropertyRelative("weight").intValue = change.weight;
    }

    private static void Validate(MerchantCatalog_SO draft)
    {
        var serialized = new SerializedObject(draft);
        foreach (string category in Categories)
        {
            long weight = 0;
            var pool = serialized.FindProperty(category + "Entries");
            for (int i = 0; i < pool.arraySize; i++)
                weight += Math.Max(1, pool.GetArrayElementAtIndex(i).FindPropertyRelative("weight").intValue);
            if (weight > int.MaxValue) throw new ArgumentException("商品池权重总和超出游戏支持范围。");
        }
    }

    private static bool ContainsGoodsId(SerializedProperty pool, string id, int except)
    {
        for (int i = 0; i < pool.arraySize; i++)
            if (i != except && pool.GetArrayElementAtIndex(i).FindPropertyRelative("goodsId").stringValue == id) return true;
        return false;
    }

    private static void Range(int value, int min, int max, string label)
    {
        if (value < min || value > max) throw new ArgumentException(label + "必须为 " + min + "–" + max + " 之间的整数。");
    }

    private static Object Load(string category, string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        switch (category)
        {
            case "loot": return AssetDatabase.LoadAssetAtPath<LootItem_SO>(path);
            case "weapon": return AssetDatabase.LoadAssetAtPath<Weapon_SO>(path);
            case "blueprint": return AssetDatabase.LoadAssetAtPath<CraftingBlueprint_SO>(path);
            case "relic": return AssetDatabase.LoadAssetAtPath<RelicTable_SO>(path);
            default: return null;
        }
    }

    private static AssetInfo Describe(Object asset)
    {
        if (asset == null) return new AssetInfo { guid = "", name = "缺失资产引用", path = "", id = "", description = "", valid = false };
        string path = AssetDatabase.GetAssetPath(asset);
        var info = new AssetInfo { guid = AssetDatabase.AssetPathToGUID(path), path = path, name = asset.name, id = "", description = "" };
        var loot = asset as LootItem_SO;
        var weapon = asset as Weapon_SO;
        var blueprint = asset as CraftingBlueprint_SO;
        var relic = asset as Relic_SO;
        var table = asset as RelicTable_SO;
        if (loot != null) { info.id = loot.lootID.ToString(); info.name = loot.lootName; info.description = loot.description; info.rarity = (int)loot.rarity; }
        if (weapon != null) { info.id = weapon.id.ToString(); info.name = weapon.weaponName; info.description = weapon.description; info.rarity = (int)weapon.rarity; }
        if (blueprint != null) { info.id = blueprint.blueprintID.ToString(); info.name = blueprint.DisplayName; info.description = blueprint.description; info.rarity = (int)blueprint.rarity; }
        if (relic != null) { info.id = relic.relicId; info.name = relic.DisplayName; info.description = relic.description; info.rarity = (int)relic.rarity; }
        if (table != null) { info.id = table.tableID.ToString(); info.name = string.IsNullOrEmpty(table.tableName) ? table.name : table.tableName; info.description = table.description; info.valid = table.IsValid; }
        return info;
    }

    private static string Hash(string value)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
    }
}
