using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum AbilityNodeState
{
    Locked = 0,
    Available = 1,
    Unlocked = 2
}

public sealed class AbilityRunBonus
{
    public int startingMoneyFlat;
    public int startingActionPointLimitFlat;
    public List<AbilityStartingBlueprintBonus> startingBlueprints = new List<AbilityStartingBlueprintBonus>();
    public List<AbilityStartingLootTableRollBonus> startingLootTableRolls = new List<AbilityStartingLootTableRollBonus>();
    public List<AbilityStartingRelicBagBonus> startingRelicBags = new List<AbilityStartingRelicBagBonus>();
    public int merchantFreeRefreshPerShop;
    public bool firstMerchantPurchaseHalfDiscount;
    public int battleStartTemporaryEnergy;
    public int battleStartEnergyMaxFlat;
    public int battleStartMaxHealthFlat;
    public int startingPartySizeFlat;
    public CharacterData_SO extraStartingCompanion;
    public TreasureRewardTable_SO treasureRewardTableOverride;
    public Weapon_SO startingWeaponReplacement;
    public int relicChoiceRefreshCount;
}

public sealed class AbilityStartingBlueprintBonus
{
    public CraftingBlueprint_SO blueprintData;
    public int amount;
    public BlueprintTable_SO blueprintTable;
    public int rollCount;
}

public sealed class AbilityStartingLootTableRollBonus
{
    public LootTable_SO lootTable;
    public int rollCount;
}

public sealed class AbilityStartingRelicBagBonus
{
    public RelicTable_SO relicTable;
    public LootRarityWeightTable_SO rarityWeightTable;
    public int bagCount;
    public int choiceCount;
}

public sealed class AbilityUnlockResult
{
    public bool success;
    public string message;
    public int pointsBefore;
    public int pointsAfter;
    public AbilityNodeState state;
}

public sealed class AbilityTreeValidationResult
{
    public bool isValid = true;
    public List<string> messages = new List<string>();

    public void AddError(string message)
    {
        isValid = false;
        messages.Add(message);
    }
}

public sealed class AbilityProgressionService
{
    private const string FileName = "ability_progression.json";
    private readonly AbilityTreeConfig_SO config;
    private readonly AbilityPointWallet wallet;
    private readonly string path;

    public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

    public AbilityProgressionService(AbilityTreeConfig_SO config)
        : this(config, new AbilityPointWallet(), DefaultPath)
    {
    }

    public AbilityProgressionService(AbilityTreeConfig_SO config, AbilityPointWallet wallet, string path)
    {
        this.config = config;
        this.wallet = wallet ?? new AbilityPointWallet();
        this.path = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
    }

    public string FilePath => path;

    public static bool IsValidSaveJson(string json)
    {
        try { var data = JsonUtility.FromJson<AbilityProgressionData>(json); return data != null && data.version == 1; }
        catch { return false; }
    }

    public AbilityNodeState GetNodeState(string nodeId)
    {
        AbilityNodeDefinition node = config != null ? config.FindNode(nodeId) : null;
        if (node == null)
            return AbilityNodeState.Locked;

        HashSet<string> unlocked = LoadUnlockedNodeIds();
        if (unlocked.Contains(node.nodeId))
            return AbilityNodeState.Unlocked;

        return ArePrerequisitesUnlocked(node, unlocked) ? AbilityNodeState.Available : AbilityNodeState.Locked;
    }

    public AbilityUnlockResult TryUnlock(string nodeId)
    {
        int pointsBefore = wallet.GetCurrentPoints();
        AbilityNodeDefinition node = config != null ? config.FindNode(nodeId) : null;
        if (node == null)
            return Result(false, "能力节点不存在。", pointsBefore, pointsBefore, AbilityNodeState.Locked);

        HashSet<string> unlocked = LoadUnlockedNodeIds();
        if (unlocked.Contains(node.nodeId))
            return Result(false, "能力节点已经解锁。", pointsBefore, pointsBefore, AbilityNodeState.Unlocked);

        if (!ArePrerequisitesUnlocked(node, unlocked))
            return Result(false, "前置能力尚未解锁。", pointsBefore, pointsBefore, AbilityNodeState.Locked);

        int cost = Mathf.Max(0, node.cost);
        if (!wallet.TrySpendPoints(cost, out int pointsAfter))
            return Result(false, "能力点不足。", pointsBefore, pointsAfter, AbilityNodeState.Available);

        unlocked.Add(node.nodeId);
        AbilityProgressionData data = LoadProgressionData();
        if (!data.unlockedNodeIds.Contains(node.nodeId))
            data.unlockedNodeIds.Add(node.nodeId);
        if (node.isToggleable && node.enabledByDefault)
            EnableNodeInData(data, node);
        SaveProgressionData(data);
        return Result(true, "能力解锁成功。", pointsBefore, pointsAfter, AbilityNodeState.Unlocked);
    }

    public AbilityUnlockResult SetNodeEnabled(string nodeId, bool enabled)
    {
        int points = wallet.GetCurrentPoints();
        AbilityNodeDefinition node = config != null ? config.FindNode(nodeId) : null;
        if (node == null)
            return Result(false, "Ability node does not exist.", points, points, AbilityNodeState.Locked);
        if (!node.isToggleable)
            return Result(false, "Ability node is not toggleable.", points, points, GetNodeState(nodeId));
        if (GetNodeState(nodeId) != AbilityNodeState.Unlocked)
            return Result(false, "Ability node is not unlocked.", points, points, GetNodeState(nodeId));

        AbilityProgressionData data = LoadProgressionData();
        if (enabled)
            EnableNodeInData(data, node);
        else
            SetNodeEnabledInData(data, node.nodeId, false);
        SaveProgressionData(data);
        return Result(true, enabled ? "Ability enabled." : "Ability disabled.", points, points, AbilityNodeState.Unlocked);
    }

    public bool IsNodeEnabled(string nodeId)
    {
        AbilityNodeDefinition node = config != null ? config.FindNode(nodeId) : null;
        if (node == null)
            return false;
        if (!node.isToggleable)
            return GetNodeState(nodeId) == AbilityNodeState.Unlocked;

        AbilityProgressionData data = LoadProgressionData();
        for (int i = 0; i < data.nodeEnabledStates.Count; i++)
        {
            AbilityNodeEnabledState state = data.nodeEnabledStates[i];
            if (state != null && string.Equals(state.nodeId, node.nodeId, StringComparison.Ordinal))
                return state.enabled;
        }

        return node.enabledByDefault;
    }

    public AbilityRunBonus BuildRunBonus()
    {
        AbilityRunBonus bonus = new AbilityRunBonus();
        if (config == null || config.nodes == null)
            return bonus;

        HashSet<string> unlocked = LoadUnlockedNodeIds();
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId) || !unlocked.Contains(node.nodeId) || node.effects == null)
                continue;
            if (node.isToggleable && !IsNodeEnabled(node.nodeId))
                continue;

            for (int effectIndex = 0; effectIndex < node.effects.Count; effectIndex++)
            {
                AbilityNodeEffect effect = node.effects[effectIndex];
                if (effect == null)
                    continue;

                switch (effect.effectType)
                {
                    case AbilityEffectType.StartingMoneyFlat:
                        bonus.startingMoneyFlat += effect.intValue;
                        break;
                    case AbilityEffectType.StartingActionPointLimitFlat:
                        bonus.startingActionPointLimitFlat += effect.intValue;
                        break;
                    case AbilityEffectType.StartingBlueprint:
                        if (effect.blueprintTable != null)
                        {
                            bonus.startingBlueprints.Add(new AbilityStartingBlueprintBonus
                            {
                                blueprintTable = effect.blueprintTable,
                                rollCount = Mathf.Max(1, effect.intValue)
                            });
                        }
                        else if (effect.blueprintData != null)
                        {
                            bonus.startingBlueprints.Add(new AbilityStartingBlueprintBonus
                            {
                                blueprintData = effect.blueprintData,
                                amount = Mathf.Max(1, effect.intValue)
                            });
                        }
                        break;
                    case AbilityEffectType.StartingLootTableRoll:
                        if (effect.lootTable != null)
                        {
                            bonus.startingLootTableRolls.Add(new AbilityStartingLootTableRollBonus
                            {
                                lootTable = effect.lootTable,
                                rollCount = Mathf.Max(1, effect.intValue)
                            });
                        }
                        if (effect.relicTable != null)
                        {
                            bonus.startingRelicBags.Add(new AbilityStartingRelicBagBonus
                            {
                                relicTable = effect.relicTable,
                                rarityWeightTable = effect.relicRarityWeightTable,
                                bagCount = Mathf.Max(1, effect.intValue),
                                choiceCount = 3
                            });
                        }
                        break;
                    case AbilityEffectType.MerchantFreeRefreshPerShop:
                        bonus.merchantFreeRefreshPerShop += Mathf.Max(0, effect.intValue);
                        break;
                    case AbilityEffectType.FirstMerchantPurchaseHalfDiscount:
                        bonus.firstMerchantPurchaseHalfDiscount = true;
                        break;
                    case AbilityEffectType.BattleStartTemporaryEnergy:
                        bonus.battleStartTemporaryEnergy += Mathf.Max(0, effect.intValue);
                        break;
                    case AbilityEffectType.BattleStartEnergyMaxFlat:
                        bonus.battleStartEnergyMaxFlat += Mathf.Max(0, effect.intValue);
                        break;
                    case AbilityEffectType.BattleStartMaxHealthFlat:
                        bonus.battleStartMaxHealthFlat += Mathf.Max(0, effect.intValue);
                        break;
                    case AbilityEffectType.StartingPartySizeFlat:
                        bonus.startingPartySizeFlat += Mathf.Max(0, effect.intValue);
                        if (bonus.extraStartingCompanion == null && effect.characterData != null)
                            bonus.extraStartingCompanion = effect.characterData;
                        break;
                    case AbilityEffectType.ExtraStartingCompanion:
                        if (effect.characterData != null)
                            bonus.extraStartingCompanion = effect.characterData;
                        break;
                    case AbilityEffectType.TreasureRewardTableOverride:
                        if (effect.treasureRewardTable != null)
                            bonus.treasureRewardTableOverride = effect.treasureRewardTable;
                        break;
                    case AbilityEffectType.StartingWeaponReplacement:
                        if (effect.weaponData != null)
                            bonus.startingWeaponReplacement = effect.weaponData;
                        break;
                    case AbilityEffectType.RelicChoiceRefreshCount:
                        bonus.relicChoiceRefreshCount += Mathf.Max(0, effect.intValue);
                        break;
                }
            }
        }

        bonus.startingMoneyFlat = Mathf.Max(0, bonus.startingMoneyFlat);
        bonus.startingActionPointLimitFlat = Mathf.Max(0, bonus.startingActionPointLimitFlat);
        bonus.merchantFreeRefreshPerShop = Mathf.Max(0, bonus.merchantFreeRefreshPerShop);
        bonus.battleStartTemporaryEnergy = Mathf.Max(0, bonus.battleStartTemporaryEnergy);
        bonus.battleStartEnergyMaxFlat = Mathf.Max(0, bonus.battleStartEnergyMaxFlat);
        bonus.battleStartMaxHealthFlat = Mathf.Max(0, bonus.battleStartMaxHealthFlat);
        bonus.startingPartySizeFlat = Mathf.Max(0, bonus.startingPartySizeFlat);
        bonus.relicChoiceRefreshCount = Mathf.Max(0, bonus.relicChoiceRefreshCount);
        return bonus;
    }

    public HashSet<string> LoadUnlockedNodeIds()
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        AbilityProgressionData data = LoadProgressionData();
        if (data == null || data.unlockedNodeIds == null)
            return result;

        for (int i = 0; i < data.unlockedNodeIds.Count; i++)
        {
            string nodeId = data.unlockedNodeIds[i];
            if (!string.IsNullOrWhiteSpace(nodeId))
                result.Add(nodeId);
        }

        return result;
    }

    public void SaveUnlockedNodeIds(IEnumerable<string> nodeIds)
    {
        AbilityProgressionData data = LoadProgressionData();
        data.unlockedNodeIds.Clear();
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        if (nodeIds != null)
        {
            foreach (string nodeId in nodeIds)
            {
                if (!string.IsNullOrWhiteSpace(nodeId) && unique.Add(nodeId))
                    data.unlockedNodeIds.Add(nodeId);
            }
        }

        SaveProgressionData(data);
    }

    public void ClearProgression()
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AbilityProgressionService] Failed to clear progression at {path}: {ex.Message}");
        }
    }

    private AbilityProgressionData LoadProgressionData()
    {
        if (!File.Exists(path))
            return new AbilityProgressionData();

        try
        {
            AbilityProgressionData data = JsonUtility.FromJson<AbilityProgressionData>(File.ReadAllText(path));
            if (data == null)
                data = new AbilityProgressionData();
            if (data.unlockedNodeIds == null)
                data.unlockedNodeIds = new List<string>();
            if (data.nodeEnabledStates == null)
                data.nodeEnabledStates = new List<AbilityNodeEnabledState>();
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AbilityProgressionService] Failed to read progression at {path}: {ex.Message}");
            return new AbilityProgressionData();
        }
    }

    private void SaveProgressionData(AbilityProgressionData data)
    {
        if (data == null)
            data = new AbilityProgressionData();
        if (data.unlockedNodeIds == null)
            data.unlockedNodeIds = new List<string>();
        if (data.nodeEnabledStates == null)
            data.nodeEnabledStates = new List<AbilityNodeEnabledState>();

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AbilityProgressionService] Failed to save progression at {path}: {ex.Message}");
        }
    }

    private static void SetNodeEnabledInData(AbilityProgressionData data, string nodeId, bool enabled)
    {
        if (data == null || string.IsNullOrWhiteSpace(nodeId))
            return;
        if (data.nodeEnabledStates == null)
            data.nodeEnabledStates = new List<AbilityNodeEnabledState>();

        for (int i = 0; i < data.nodeEnabledStates.Count; i++)
        {
            AbilityNodeEnabledState state = data.nodeEnabledStates[i];
            if (state != null && string.Equals(state.nodeId, nodeId, StringComparison.Ordinal))
            {
                state.enabled = enabled;
                return;
            }
        }

        data.nodeEnabledStates.Add(new AbilityNodeEnabledState
        {
            nodeId = nodeId,
            enabled = enabled
        });
    }

    private void EnableNodeInData(AbilityProgressionData data, AbilityNodeDefinition node)
    {
        if (data == null || node == null || string.IsNullOrWhiteSpace(node.nodeId))
            return;

        if (!string.IsNullOrWhiteSpace(node.mutualExclusionGroupId) && config != null && config.nodes != null)
        {
            for (int i = 0; i < config.nodes.Count; i++)
            {
                AbilityNodeDefinition other = config.nodes[i];
                if (other == null || other == node || !other.isToggleable)
                    continue;
                if (string.Equals(other.mutualExclusionGroupId, node.mutualExclusionGroupId, StringComparison.Ordinal))
                    SetNodeEnabledInData(data, other.nodeId, false);
            }
        }

        SetNodeEnabledInData(data, node.nodeId, true);
    }

    public static AbilityTreeValidationResult ValidateTree(AbilityTreeConfig_SO config)
    {
        AbilityTreeValidationResult result = new AbilityTreeValidationResult();
        if (config == null)
        {
            result.AddError("[EMPTY_CONFIG] 能力树配置为空。");
            return result;
        }

        List<AbilityNodeDefinition> nodes = config.nodes ?? new List<AbilityNodeDefinition>();
        Dictionary<string, AbilityNodeDefinition> lookup = new Dictionary<string, AbilityNodeDefinition>(StringComparer.Ordinal);
        HashSet<string> duplicateIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < nodes.Count; i++)
        {
            AbilityNodeDefinition node = nodes[i];
            if (node == null)
            {
                result.AddError($"[EMPTY_NODE] 第 {i + 1} 个节点为空。");
                continue;
            }

            if (node.cost < 0)
                result.AddError($"[NEGATIVE_COST] 节点 {(string.IsNullOrWhiteSpace(node.nodeId) ? (i + 1).ToString() : node.nodeId)} 的消耗为负数。");

            if (string.IsNullOrWhiteSpace(node.nodeId))
            {
                result.AddError($"[EMPTY_ID] 节点 {i + 1} 的 ID 为空。");
                continue;
            }

            if (lookup.ContainsKey(node.nodeId))
            {
                duplicateIds.Add(node.nodeId);
                result.AddError($"[DUPLICATE_ID] 节点 ID 重复：{node.nodeId}");
                continue;
            }

            lookup[node.nodeId] = node;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            AbilityNodeDefinition node = nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                continue;

            if (node.prerequisiteNodeIds == null)
                continue;

            for (int prerequisiteIndex = 0; prerequisiteIndex < node.prerequisiteNodeIds.Count; prerequisiteIndex++)
            {
                string prerequisiteId = node.prerequisiteNodeIds[prerequisiteIndex];
                if (string.IsNullOrWhiteSpace(prerequisiteId))
                    continue;

                if (!lookup.ContainsKey(prerequisiteId))
                    result.AddError($"[MISSING_PREREQUISITE] 节点 {node.nodeId} 缺少前置节点引用：{prerequisiteId}");
            }
        }

        HashSet<string> visiting = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, AbilityNodeDefinition> pair in lookup)
        {
            if (HasCycle(pair.Key, lookup, visiting, visited))
                result.AddError($"[CYCLE] 能力树存在循环前置依赖，涉及节点：{pair.Key}");
        }

        return result;
    }

    private static bool HasCycle(
        string nodeId,
        Dictionary<string, AbilityNodeDefinition> lookup,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visited.Contains(nodeId))
            return false;

        if (!visiting.Add(nodeId))
            return true;

        AbilityNodeDefinition node;
        if (lookup.TryGetValue(nodeId, out node) && node != null && node.prerequisiteNodeIds != null)
        {
            for (int i = 0; i < node.prerequisiteNodeIds.Count; i++)
            {
                string prerequisiteId = node.prerequisiteNodeIds[i];
                if (!string.IsNullOrWhiteSpace(prerequisiteId)
                    && lookup.ContainsKey(prerequisiteId)
                    && HasCycle(prerequisiteId, lookup, visiting, visited))
                    return true;
            }
        }

        visiting.Remove(nodeId);
        visited.Add(nodeId);
        return false;
    }

    private static AbilityUnlockResult Result(bool success, string message, int pointsBefore, int pointsAfter, AbilityNodeState state)
    {
        return new AbilityUnlockResult
        {
            success = success,
            message = message,
            pointsBefore = pointsBefore,
            pointsAfter = pointsAfter,
            state = state
        };
    }

    private static bool ArePrerequisitesUnlocked(AbilityNodeDefinition node, HashSet<string> unlocked)
    {
        if (node == null || node.prerequisiteNodeIds == null)
            return true;

        for (int i = 0; i < node.prerequisiteNodeIds.Count; i++)
        {
            string prerequisiteId = node.prerequisiteNodeIds[i];
            if (!string.IsNullOrWhiteSpace(prerequisiteId) && !unlocked.Contains(prerequisiteId))
                return false;
        }

        return true;
    }

    [Serializable]
    private sealed class AbilityProgressionData
    {
        public int version = 1;
        public List<string> unlockedNodeIds = new List<string>();
        public List<AbilityNodeEnabledState> nodeEnabledStates = new List<AbilityNodeEnabledState>();
    }

    [Serializable]
    private sealed class AbilityNodeEnabledState
    {
        public string nodeId;
        public bool enabled;
    }
}
