using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AbilityTreeEditorOperationsTests
{
    [Test]
    public void AddNode_AppendsUniqueNodeAtRequestedPosition()
    {
        AbilityTreeConfig_SO config = CreateConfig(Node("ability-1", new Vector2(10f, 20f)));

        AbilityNodeDefinition created = AbilityTreeEditorOperations.AddNode(config, new Vector2(100f, 200f));

        Assert.IsNotNull(created);
        Assert.AreEqual(2, config.nodes.Count);
        Assert.AreEqual("ability-2", created.nodeId);
        Assert.AreEqual(new Vector2(100f, 200f), created.position);
        Assert.AreEqual(1, created.cost);
        Assert.IsNotNull(created.effects);
    }

    [Test]
    public void DeleteNode_RemovesNodeAndReferencesFromPrerequisites()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("root", Vector2.zero),
            Node("child", Vector2.right, "root"),
            Node("branch", Vector2.up, "root", "child"));

        bool removed = AbilityTreeEditorOperations.DeleteNode(config, "root");

        Assert.IsTrue(removed);
        Assert.IsNull(config.FindNode("root"));
        Assert.AreEqual(2, config.nodes.Count);
        Assert.IsFalse(config.FindNode("child").prerequisiteNodeIds.Contains("root"));
        Assert.IsFalse(config.FindNode("branch").prerequisiteNodeIds.Contains("root"));
        Assert.IsTrue(config.FindNode("branch").prerequisiteNodeIds.Contains("child"));
    }

    [Test]
    public void DuplicateNode_CopiesEditableFieldsWithUniqueIdOffsetAndNoPrerequisites()
    {
        AbilityNodeDefinition source = Node("source", new Vector2(10f, 20f), "root");
        source.displayName = "Original";
        source.description = "Original description";
        source.cost = 4;
        source.isToggleable = true;
        source.enabledByDefault = false;
        source.mutualExclusionGroupId = "group-a";
        source.effects = new List<AbilityNodeEffect>
        {
            new AbilityNodeEffect { effectType = AbilityEffectType.StartingMoneyFlat, intValue = 10 },
            new AbilityNodeEffect
            {
                effectType = AbilityEffectType.StartingBlueprint,
                intValue = 2,
                blueprintTable = ScriptableObject.CreateInstance<BlueprintTable_SO>()
            }
        };
        AbilityTreeConfig_SO config = CreateConfig(
            Node("root", Vector2.zero),
            source);

        AbilityNodeDefinition duplicate = AbilityTreeEditorOperations.DuplicateNode(config, source, new Vector2(32f, 48f));

        Assert.IsNotNull(duplicate);
        Assert.AreEqual(3, config.nodes.Count);
        Assert.AreNotEqual(source.nodeId, duplicate.nodeId);
        Assert.AreEqual("source-1", duplicate.nodeId);
        Assert.AreEqual("Original Copy", duplicate.displayName);
        Assert.AreEqual(source.description, duplicate.description);
        Assert.AreEqual(source.cost, duplicate.cost);
        Assert.AreEqual(source.position + new Vector2(32f, 48f), duplicate.position);
        Assert.AreEqual(0, duplicate.prerequisiteNodeIds.Count);
        Assert.AreEqual(source.isToggleable, duplicate.isToggleable);
        Assert.AreEqual(source.enabledByDefault, duplicate.enabledByDefault);
        Assert.AreEqual(source.mutualExclusionGroupId, duplicate.mutualExclusionGroupId);
        Assert.AreEqual(source.effects.Count, duplicate.effects.Count);
        Assert.AreEqual(AbilityEffectType.StartingMoneyFlat, duplicate.effects[0].effectType);
        Assert.AreEqual(10, duplicate.effects[0].intValue);
        Assert.AreEqual(AbilityEffectType.StartingBlueprint, duplicate.effects[1].effectType);
        Assert.AreSame(source.effects[1].blueprintTable, duplicate.effects[1].blueprintTable);

        duplicate.effects[0].intValue = 99;
        Assert.AreEqual(10, source.effects[0].intValue);
        Object.DestroyImmediate(source.effects[1].blueprintTable);
    }

    [Test]
    public void GenerateMissingIds_FillsEmptyIdsWithoutColliding()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("ability-1", Vector2.zero),
            Node(string.Empty, Vector2.right),
            Node(null, Vector2.up));

        int changed = AbilityTreeEditorOperations.GenerateMissingIds(config);

        Assert.AreEqual(2, changed);
        Assert.AreEqual("ability-2", config.nodes[1].nodeId);
        Assert.AreEqual("ability-3", config.nodes[2].nodeId);
    }

    [Test]
    public void RemoveInvalidPrerequisites_RemovesMissingEmptyAndSelfReferences()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("root", Vector2.zero),
            Node("child", Vector2.right, "root", "missing", "", "child"));

        int removed = AbilityTreeEditorOperations.RemoveInvalidPrerequisites(config);

        Assert.AreEqual(3, removed);
        CollectionAssert.AreEqual(new[] { "root" }, config.FindNode("child").prerequisiteNodeIds);
    }

    [Test]
    public void ApplyAxisSnap_SnapsEachAxisIndependentlyWhenCloseToOtherNodes()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("dragging", new Vector2(0f, 0f)),
            Node("x-anchor", new Vector2(100f, 260f)),
            Node("y-anchor", new Vector2(300f, 400f)));
        AbilityNodeDefinition dragging = config.FindNode("dragging");

        Vector2 snapped = AbilityTreeEditorOperations.ApplyAxisSnap(config, dragging, new Vector2(112f, 386f), 18f);

        Assert.AreEqual(new Vector2(100f, 400f), snapped);
    }

    [Test]
    public void ApplyAxisSnap_LeavesPositionUnchangedWhenOutsideThreshold()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("dragging", new Vector2(0f, 0f)),
            Node("anchor", new Vector2(100f, 400f)));
        AbilityNodeDefinition dragging = config.FindNode("dragging");

        Vector2 snapped = AbilityTreeEditorOperations.ApplyAxisSnap(config, dragging, new Vector2(130f, 370f), 18f);

        Assert.AreEqual(new Vector2(130f, 370f), snapped);
    }

    private static AbilityTreeConfig_SO CreateConfig(params AbilityNodeDefinition[] nodes)
    {
        AbilityTreeConfig_SO config = ScriptableObject.CreateInstance<AbilityTreeConfig_SO>();
        config.nodes = new List<AbilityNodeDefinition>(nodes);
        return config;
    }

    private static AbilityNodeDefinition Node(string nodeId, Vector2 position, params string[] prerequisites)
    {
        return new AbilityNodeDefinition
        {
            nodeId = nodeId,
            displayName = string.IsNullOrWhiteSpace(nodeId) ? "Node" : nodeId,
            description = "desc",
            cost = 1,
            position = position,
            prerequisiteNodeIds = new List<string>(prerequisites ?? new string[0]),
            effects = new List<AbilityNodeEffect>()
        };
    }
}
