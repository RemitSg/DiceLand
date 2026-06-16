using System.Collections.Generic;
using UnityEngine;

public static class AbilityTreeEditorOperations
{
    public static AbilityNodeDefinition AddNode(AbilityTreeConfig_SO config, Vector2 position)
    {
        if (config == null)
            return null;

        if (config.nodes == null)
            config.nodes = new List<AbilityNodeDefinition>();

        string nodeId = GenerateUniqueId(config, "ability");
        AbilityNodeDefinition node = new AbilityNodeDefinition
        {
            nodeId = nodeId,
            displayName = nodeId,
            description = string.Empty,
            cost = 1,
            position = position,
            prerequisiteNodeIds = new List<string>(),
            effects = new List<AbilityNodeEffect>()
        };
        config.nodes.Add(node);
        return node;
    }

    public static bool DeleteNode(AbilityTreeConfig_SO config, string nodeId)
    {
        if (config == null || config.nodes == null || string.IsNullOrWhiteSpace(nodeId))
            return false;

        int removeIndex = config.nodes.FindIndex(node => node != null && node.nodeId == nodeId);
        if (removeIndex < 0)
            return false;

        config.nodes.RemoveAt(removeIndex);
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || node.prerequisiteNodeIds == null)
                continue;

            node.prerequisiteNodeIds.RemoveAll(id => id == nodeId);
        }

        return true;
    }

    public static AbilityNodeDefinition DuplicateNode(AbilityTreeConfig_SO config, AbilityNodeDefinition source, Vector2 offset)
    {
        if (config == null || source == null)
            return null;

        if (config.nodes == null)
            config.nodes = new List<AbilityNodeDefinition>();

        string sourceId = string.IsNullOrWhiteSpace(source.nodeId) ? "ability" : source.nodeId.Trim();
        AbilityNodeDefinition duplicate = new AbilityNodeDefinition
        {
            nodeId = GenerateUniqueId(config, sourceId),
            displayName = string.IsNullOrWhiteSpace(source.displayName) ? $"{sourceId} Copy" : $"{source.displayName} Copy",
            description = source.description,
            cost = source.cost,
            position = source.position + offset,
            prerequisiteNodeIds = new List<string>(),
            effects = CopyEffects(source.effects),
            isToggleable = source.isToggleable,
            enabledByDefault = source.enabledByDefault,
            mutualExclusionGroupId = source.mutualExclusionGroupId
        };

        config.nodes.Add(duplicate);
        return duplicate;
    }

    public static int GenerateMissingIds(AbilityTreeConfig_SO config)
    {
        if (config == null || config.nodes == null)
            return 0;

        int changed = 0;
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || !string.IsNullOrWhiteSpace(node.nodeId))
                continue;

            node.nodeId = GenerateUniqueId(config, "ability");
            changed++;
        }

        return changed;
    }

    public static int RemoveInvalidPrerequisites(AbilityTreeConfig_SO config)
    {
        if (config == null || config.nodes == null)
            return 0;

        HashSet<string> validIds = new HashSet<string>();
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node != null && !string.IsNullOrWhiteSpace(node.nodeId))
                validIds.Add(node.nodeId);
        }

        int removed = 0;
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || node.prerequisiteNodeIds == null)
                continue;

            int before = node.prerequisiteNodeIds.Count;
            node.prerequisiteNodeIds.RemoveAll(id => string.IsNullOrWhiteSpace(id) || !validIds.Contains(id) || id == node.nodeId);
            removed += before - node.prerequisiteNodeIds.Count;
        }

        return removed;
    }

    public static Vector2 ApplyAxisSnap(
        AbilityTreeConfig_SO config,
        AbilityNodeDefinition movingNode,
        Vector2 desiredPosition,
        float threshold)
    {
        if (config == null || config.nodes == null || movingNode == null || threshold <= 0f)
            return desiredPosition;

        Vector2 snapped = desiredPosition;
        float bestXDistance = threshold;
        float bestYDistance = threshold;

        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition other = config.nodes[i];
            if (other == null || ReferenceEquals(other, movingNode))
                continue;

            float xDistance = Mathf.Abs(desiredPosition.x - other.position.x);
            if (xDistance <= bestXDistance)
            {
                bestXDistance = xDistance;
                snapped.x = other.position.x;
            }

            float yDistance = Mathf.Abs(desiredPosition.y - other.position.y);
            if (yDistance <= bestYDistance)
            {
                bestYDistance = yDistance;
                snapped.y = other.position.y;
            }
        }

        return snapped;
    }

    private static List<AbilityNodeEffect> CopyEffects(IReadOnlyList<AbilityNodeEffect> sourceEffects)
    {
        List<AbilityNodeEffect> copied = new List<AbilityNodeEffect>();
        if (sourceEffects == null)
            return copied;

        for (int i = 0; i < sourceEffects.Count; i++)
        {
            AbilityNodeEffect source = sourceEffects[i];
            if (source == null)
                continue;

            copied.Add(new AbilityNodeEffect
            {
                effectType = source.effectType,
                intValue = source.intValue,
                blueprintData = source.blueprintData,
                blueprintTable = source.blueprintTable,
                lootTable = source.lootTable,
                relicTable = source.relicTable,
                relicRarityWeightTable = source.relicRarityWeightTable,
                treasureRewardTable = source.treasureRewardTable,
                characterData = source.characterData,
                weaponData = source.weaponData
            });
        }

        return copied;
    }

    public static string GenerateUniqueId(AbilityTreeConfig_SO config, string prefix)
    {
        string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "ability" : prefix.Trim();
        HashSet<string> used = new HashSet<string>();
        if (config != null && config.nodes != null)
        {
            for (int i = 0; i < config.nodes.Count; i++)
            {
                AbilityNodeDefinition node = config.nodes[i];
                if (node != null && !string.IsNullOrWhiteSpace(node.nodeId))
                    used.Add(node.nodeId);
            }
        }

        int index = 1;
        string candidate;
        do
        {
            candidate = $"{safePrefix}-{index}";
            index++;
        }
        while (used.Contains(candidate));

        return candidate;
    }
}
