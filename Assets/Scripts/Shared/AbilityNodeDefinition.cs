using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AbilityNodeDefinition
{
    public string nodeId;
    public string LocalizedName => GameText.Content("ability." + nodeId, "name", string.IsNullOrWhiteSpace(displayName) ? nodeId : displayName);
    public string LocalizedDescription => GameText.Content("ability." + nodeId, "description", description);
    public string displayName;
    [TextArea(2, 4)] public string description;
    [Min(0)] public int cost = 1;
    public Vector2 position;
    public bool isToggleable;
    public bool enabledByDefault = true;
    public string mutualExclusionGroupId;
    public List<string> prerequisiteNodeIds = new List<string>();
    public List<AbilityNodeEffect> effects = new List<AbilityNodeEffect>();
}
