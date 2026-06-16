using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityTreeConfig", menuName = "Game/Ability Tree Config")]
public sealed class AbilityTreeConfig_SO : ScriptableObject
{
    public List<AbilityNodeDefinition> nodes = new List<AbilityNodeDefinition>();

    public AbilityNodeDefinition FindNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
            return null;

        for (int i = 0; i < nodes.Count; i++)
        {
            AbilityNodeDefinition node = nodes[i];
            if (node != null && string.Equals(node.nodeId, nodeId, StringComparison.Ordinal))
                return node;
        }

        return null;
    }
}
