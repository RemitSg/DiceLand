using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AbilityTreeConfig_SO))]
public sealed class AbilityTreeConfigEditor : Editor
{
    private SerializedProperty nodesProperty;

    private void OnEnable()
    {
        nodesProperty = serializedObject.FindProperty("nodes");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("能力树节点按 nodeId 建立前置关系。运行时 UI 使用 position 字段摆放节点。", MessageType.Info);

        if (GUILayout.Button("打开节点编辑器", GUILayout.Height(32f)))
            AbilityTreeEditorWindow.Open((AbilityTreeConfig_SO)target);

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(nodesProperty, new GUIContent("能力节点"), true);

        EditorGUILayout.Space(8f);
        DrawToolbar();
        DrawValidation();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("生成示例树"))
                CreateSampleTree();

            if (GUILayout.Button("补齐缺失 ID"))
                GenerateMissingIds();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("清理无效前置"))
                RemoveInvalidPrerequisites();

            if (GUILayout.Button("校验能力树"))
                LogValidation();
        }
    }

    private void DrawValidation()
    {
        AbilityTreeConfig_SO config = (AbilityTreeConfig_SO)target;
        AbilityTreeValidationResult result = AbilityProgressionService.ValidateTree(config);
        EditorGUILayout.HelpBox(result.isValid ? "能力树校验通过。" : string.Join("\n", result.messages), result.isValid ? MessageType.Info : MessageType.Warning);
    }

    private void CreateSampleTree()
    {
        AbilityTreeConfig_SO config = (AbilityTreeConfig_SO)target;
        Undo.RecordObject(config, "Create Ability Sample Tree");
        config.nodes = new List<AbilityNodeDefinition>
        {
            CreateNode("start-money-1", "初始金币 I", "开局初始金币 +10。", 1, new Vector2(-360f, 180f), null, AbilityEffectType.StartingMoneyFlat, 10),
            CreateNode("start-money-2", "初始金币 II", "开局初始金币 +10。", 2, new Vector2(-120f, 180f), new[] { "start-money-1" }, AbilityEffectType.StartingMoneyFlat, 10),
            CreateNode("start-money-3", "初始金币 III", "开局初始金币 +10，并开启后续分支。", 3, new Vector2(120f, 180f), new[] { "start-money-2" }, AbilityEffectType.StartingMoneyFlat, 10),
            CreateNode("action-limit-1", "行动准备 I", "初始行动点上限 +2。", 3, new Vector2(360f, 60f), new[] { "start-money-3" }, AbilityEffectType.StartingActionPointLimitFlat, 2),
            CreateNode("action-limit-2", "行动准备 II", "初始行动点上限 +2。", 4, new Vector2(360f, 280f), new[] { "start-money-3" }, AbilityEffectType.StartingActionPointLimitFlat, 2)
        };
        EditorUtility.SetDirty(config);
    }

    private static AbilityNodeDefinition CreateNode(
        string id,
        string name,
        string description,
        int cost,
        Vector2 position,
        IEnumerable<string> prerequisites,
        AbilityEffectType effectType,
        int value)
    {
        return new AbilityNodeDefinition
        {
            nodeId = id,
            displayName = name,
            description = description,
            cost = cost,
            position = position,
            prerequisiteNodeIds = prerequisites != null ? new List<string>(prerequisites) : new List<string>(),
            effects = new List<AbilityNodeEffect>
            {
                new AbilityNodeEffect { effectType = effectType, intValue = value }
            }
        };
    }

    private void GenerateMissingIds()
    {
        AbilityTreeConfig_SO config = (AbilityTreeConfig_SO)target;
        Undo.RecordObject(config, "Generate Missing Ability IDs");
        AbilityTreeEditorOperations.GenerateMissingIds(config);
        EditorUtility.SetDirty(config);
    }

    private void RemoveInvalidPrerequisites()
    {
        AbilityTreeConfig_SO config = (AbilityTreeConfig_SO)target;
        Undo.RecordObject(config, "Remove Invalid Ability Prerequisites");
        AbilityTreeEditorOperations.RemoveInvalidPrerequisites(config);
        EditorUtility.SetDirty(config);
    }

    private void LogValidation()
    {
        AbilityTreeConfig_SO config = (AbilityTreeConfig_SO)target;
        AbilityTreeValidationResult result = AbilityProgressionService.ValidateTree(config);
        if (result.isValid)
            Debug.Log("[AbilityTreeConfigEditor] 能力树校验通过。", config);
        else
            Debug.LogWarning($"[AbilityTreeConfigEditor] 能力树存在问题：\n{string.Join("\n", result.messages)}", config);
    }
}
