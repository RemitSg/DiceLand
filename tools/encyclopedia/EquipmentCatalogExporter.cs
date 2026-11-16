using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// This file is compiled as an Editor script in the catalog export checkout.
public static class EquipmentCatalogExporter
{
    private const string MainPoolPath = "Assets/Data/Scroll/MainBlueprintTable.asset";
    private const string TreasurePoolPath = "Assets/Resources/TreasureBlueprintTable.asset";
    private const string MerchantPath = "Assets/Data/Economy/MerchantCatalog.asset";

    public static void Export()
    {
        string output = Environment.GetEnvironmentVariable("DICELAND_EQUIPMENT_OUTPUT");
        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException("Set DICELAND_EQUIPMENT_OUTPUT to the equipment JSON path.");

        var mainPool = Load<BlueprintTable_SO>(MainPoolPath).GetValidBlueprints();
        var treasurePool = Load<BlueprintTable_SO>(TreasurePoolPath).GetValidBlueprints();
        var merchant = Load<MerchantCatalog_SO>(MerchantPath);
        var weaponGoods = merchant.GetValidWeaponEntries().ToLookup(entry => entry.weaponData);
        var blueprintGoods = merchant.GetValidBlueprintEntries().ToLookup(entry => entry.blueprintData);
        var shopLoot = new HashSet<LootItem_SO>(merchant.GetValidLootEntries().Select(entry => entry.lootItem));
        var weapons = Find<Weapon_SO>().OrderBy(asset => asset.id)
            .ThenBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal);
        var blueprints = Find<CraftingBlueprint_SO>().OrderBy(asset => asset.blueprintID)
            .ThenBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal);

        using (var reader = new AgentBattleAssetReader())
        {
            var snapshot = new Snapshot
            {
                date = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                weapons = weapons.Select(asset => new WeaponRecord
                {
                    detail = reader.Weapon(asset),
                    assetId = asset.id,
                    saveId = asset.saveId,
                    path = AssetDatabase.GetAssetPath(asset),
                    fusionLimit = asset.FusionLimit,
                    hasDefaultSkill = asset.GetDefaultSkill() != null,
                    icon = ReadIcon(asset.icon),
                    shop = weaponGoods[asset].Select(entry => new ShopEntry(
                        entry.goodsId, entry.minPrice, entry.maxPrice, entry.weight)).ToArray()
                }).ToArray(),
                blueprints = blueprints.Select(asset => new BlueprintRecord
                {
                    detail = AgentBattleAssetReader.Blueprint(asset),
                    assetId = asset.blueprintID,
                    path = AssetDatabase.GetAssetPath(asset),
                    weaponName = asset.targetWeapon != null ? asset.targetWeapon.weaponName : "",
                    mainPool = mainPool.Contains(asset),
                    treasurePool = treasurePool.Contains(asset),
                    icon = ReadIcon(asset.DisplayIcon),
                    shop = blueprintGoods[asset].Select(entry => new ShopEntry(
                        entry.goodsId, entry.minPrice, entry.maxPrice, entry.weight)).ToArray()
                }).ToArray(),
                loot = Find<LootItem_SO>().OrderBy(asset => asset.lootID)
                    .ThenBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal).Select(asset => new LootRecord
                    {
                        detail = new LootDetails
                        {
                            id = AssetGuid(asset), name = asset.lootName, description = asset.description,
                            rarity = asset.rarity.ToString(), attribute = asset.attribute != null ? asset.attribute.attributeName : ""
                        },
                        assetId = asset.lootID,
                        path = AssetDatabase.GetAssetPath(asset),
                        saveId = asset.saveId,
                        inShop = shopLoot.Contains(asset),
                        icon = ReadIcon(asset.icon)
                    }).ToArray(),
                battleLoot = ReadBattleLoot()
            };

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, JsonUtility.ToJson(snapshot, true) + "\n");
            Debug.Log($"Equipment catalog exported {snapshot.weapons.Length} weapons, {snapshot.blueprints.Length} blueprints, " +
                $"{snapshot.loot.Length} loot items and {snapshot.battleLoot.sources.Length} battle loot configurations to {output}.");
        }
    }

    private static BattleLoot ReadBattleLoot()
    {
        EnemyData_SO[] enemies = Find<EnemyData_SO>().OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal).ToArray();
        var tables = new Dictionary<LootTable_SO, DropTable>();
        string[] CaptureTables(IEnumerable<LootTable_SO> candidates)
        {
            var ids = new List<string>();
            foreach (LootTable_SO table in candidates ?? Array.Empty<LootTable_SO>())
            {
                if (table == null) continue;
                if (!tables.TryGetValue(table, out DropTable info))
                {
                    info = new DropTable
                    {
                        id = AssetGuid(table),
                        name = AssetName(table.tableName, table),
                        path = AssetDatabase.GetAssetPath(table),
                        drops = (table.Drops ?? Array.Empty<LootTable_SO.LootDropEntry>())
                            .Where(entry => entry?.lootItem != null && entry.maxAmount > 0)
                            .Select(entry => new DropEntry
                            {
                                id = AssetGuid(entry.lootItem), chance = Mathf.Clamp01(entry.dropChance),
                                min = Mathf.Max(1, entry.minAmount), max = Mathf.Max(1, entry.maxAmount)
                            }).ToArray()
                    };
                    tables.Add(table, info);
                }
                ids.Add(info.id);
            }
            return ids.ToArray();
        }

        var groups = Find<EnemyGroup_SO>().OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal)
            .Select(group => new BattleLootSource
            {
                kind = "group", name = AssetName(group.groupName, group), path = AssetDatabase.GetAssetPath(group),
                enemyIds = group.GetEnemies().Where(enemy => enemy != null).Select(AssetGuid).ToArray(),
                tableIds = CaptureTables(group.GetLootTables())
            });
        var retreats = enemies.Where(enemy => enemy.roundStartRetreatRule.enabled)
            .Select(enemy => new BattleLootSource
            {
                kind = "retreat", name = enemy.enemyName, path = AssetDatabase.GetAssetPath(enemy),
                enemyIds = new[] { AssetGuid(enemy) }, round = Mathf.Max(1, enemy.roundStartRetreatRule.triggerRound),
                tableIds = CaptureTables(enemy.roundStartRetreatRule.settlementLootTables)
            });
        var sources = groups.Concat(retreats).Concat(ReadEventVictoryLoot()).ToArray();
        return new BattleLoot
        {
            enemyIds = enemies.Select(AssetGuid).ToArray(),
            sources = sources,
            tables = tables.Values.OrderBy(table => table.path, StringComparer.Ordinal).ToArray()
        };
    }

    private static IEnumerable<BattleLootSource> ReadEventVictoryLoot()
    {
        foreach (var mapEvent in Find<MapEvent_SO>().OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal))
        foreach (var option in mapEvent.GetValidOptions())
        {
            var results = (option.possibleResults ?? new List<MapEventPossibleResultData>())
                .Where(result => result != null && result.weight > 0).ToArray();
            var branches = results.Length > 0
                ? results.Select(result => (result.resultId, MapEventEffectSequenceBuilder.BuildAllEffects(result)))
                : new[] { ("", MapEventEffectSequenceBuilder.BuildAllEffects(option)) }.AsEnumerable();
            foreach (var (resultId, effects) in branches)
            {
                var encounter = effects.FirstOrDefault(effect => effect.effectType == MapEventOptionBuiltInEffectType.StartEncounter);
                if (encounter == null) continue;
                var enemyIds = EventEnemies(encounter).Where(enemy => enemy != null).Select(AssetGuid).Distinct().ToArray();
                if (enemyIds.Length == 0) continue;
                var rewards = effects.Where(effect => effect.effectType == MapEventOptionBuiltInEffectType.GainLoot ||
                        effect.effectType == MapEventOptionBuiltInEffectType.GainRandomLootFromList)
                    .Select(ReadEventLootPick).Where(pick => pick != null).ToArray();
                if (rewards.Length == 0) continue;
                yield return new BattleLootSource
                {
                    kind = "event", name = mapEvent.DisplayName, path = AssetDatabase.GetAssetPath(mapEvent),
                    label = string.IsNullOrEmpty(resultId) ? option.DisplayLabel : $"{option.DisplayLabel} / {resultId}",
                    enemyIds = enemyIds, eventPicks = rewards
                };
            }
        }
    }

    private static IEnumerable<EnemyData_SO> EventEnemies(MapEventBuiltInEffectData encounter)
    {
        if (encounter.encounterSelectionMode == MapEventEncounterSelectionMode.RandomEnemies)
            return encounter.encounterEnemies ?? Enumerable.Empty<EnemyData_SO>();
        return (encounter.encounterEnemyGroups ?? new List<EnemyGroup_SO>())
            .Where(group => group != null && group.IsValid).SelectMany(group => group.GetEnemies());
    }

    private static EventLootPick ReadEventLootPick(MapEventBuiltInEffectData effect)
    {
        var drops = (effect.lootRewards ?? new List<MapEventLootRewardEntry>()).Where(entry => entry?.lootItem != null)
            .Select(entry => new DropEntry
            {
                id = AssetGuid(entry.lootItem),
                min = Mathf.Max(1, entry.amountMin),
                max = Mathf.Max(Mathf.Max(1, entry.amountMin), entry.amountMax)
            }).ToArray();
        if (drops.Length == 0) return null;
        var min = Mathf.Clamp(effect.lootPickCountMin, 1, drops.Length);
        return new EventLootPick { min = min, max = Mathf.Clamp(effect.lootPickCountMax, min, drops.Length), drops = drops };
    }

    private static string AssetGuid(UnityEngine.Object asset) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
    private static string AssetName(string name, UnityEngine.Object asset) => string.IsNullOrWhiteSpace(name) ? asset.name : name;

    private static T[] Find<T>() where T : ScriptableObject =>
        AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/Data" })
            .Distinct().Select(AssetDatabase.GUIDToAssetPath).Select(Load<T>).ToArray();

    private static T Load<T>(string path) where T : ScriptableObject =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidDataException("Missing catalog asset: " + path);

    private static SpriteRecord ReadIcon(Sprite sprite)
    {
        if (sprite == null) return null;
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long localId))
            throw new InvalidDataException("Missing sprite identity: " + sprite.name);

        Rect rect = sprite.rect;
        return new SpriteRecord
        {
            key = guid + ":" + localId,
            path = AssetDatabase.GetAssetPath(sprite),
            x = Mathf.RoundToInt(rect.x),
            y = Mathf.RoundToInt(rect.y),
            width = Mathf.RoundToInt(rect.width),
            height = Mathf.RoundToInt(rect.height),
            textureWidth = sprite.texture.width,
            textureHeight = sprite.texture.height
        };
    }

    [Serializable]
    private sealed class Snapshot
    {
        public string date;
        public WeaponRecord[] weapons;
        public BlueprintRecord[] blueprints;
        public LootRecord[] loot;
        public BattleLoot battleLoot;
    }

    [Serializable]
    private sealed class WeaponRecord
    {
        public AgentBattleWeaponDetails detail;
        public int assetId, fusionLimit;
        public bool hasDefaultSkill;
        public string path, saveId;
        public SpriteRecord icon;
        public ShopEntry[] shop;
    }

    [Serializable]
    private sealed class BlueprintRecord
    {
        public AgentBattleBlueprintDetails detail;
        public int assetId;
        public string path, weaponName;
        public bool mainPool, treasurePool;
        public SpriteRecord icon;
        public ShopEntry[] shop;
    }

    [Serializable]
    private sealed class LootRecord
    {
        public LootDetails detail;
        public int assetId;
        public string path, saveId;
        public bool inShop;
        public SpriteRecord icon;
    }

    [Serializable]
    private sealed class LootDetails
    {
        public string id, name, description, rarity, attribute;
    }

    [Serializable]
    private sealed class BattleLoot
    {
        public string[] enemyIds;
        public BattleLootSource[] sources;
        public DropTable[] tables;
    }

    [Serializable]
    private sealed class BattleLootSource
    {
        public string kind, name, label, path;
        public string[] enemyIds, tableIds;
        public int round;
        public EventLootPick[] eventPicks;
    }

    [Serializable]
    private sealed class DropTable
    {
        public string id, name, path;
        public DropEntry[] drops;
    }

    [Serializable]
    private sealed class EventLootPick
    {
        public int min, max;
        public DropEntry[] drops;
    }

    [Serializable]
    private sealed class DropEntry
    {
        public string id;
        public int min, max;
        public float chance;
    }

    [Serializable]
    private sealed class SpriteRecord
    {
        public string key, path;
        public int x, y, width, height, textureWidth, textureHeight;
    }

    [Serializable]
    private sealed class ShopEntry
    {
        public string goodsId;
        public int minPrice, maxPrice, weight;

        public ShopEntry(string goodsId, int minPrice, int maxPrice, int weight)
        {
            this.goodsId = goodsId;
            this.minPrice = minPrice;
            this.maxPrice = maxPrice;
            this.weight = weight;
        }
    }
}
