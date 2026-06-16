using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class AbilityTreeEditorWindow : EditorWindow
{
    private const float ToolbarHeight = 34f;
    private const float InspectorWidth = 360f;
    private const float NodeWidth = 180f;
    private const float NodeHeight = 76f;
    private const float AxisSnapThreshold = 18f;
    private static readonly Vector2 DuplicateOffset = new Vector2(48f, 48f);

    private AbilityTreeConfig_SO config;
    private Vector2 graphPan = new Vector2(520f, 260f);
    private Vector2 graphDragStart;
    private Vector2 inspectorScroll;
    private AbilityNodeDefinition selectedNode;
    private AbilityNodeDefinition copiedNode;
    private bool isDraggingCanvas;
    private bool isDraggingNode;
    private bool axisSnapEnabled = true;
    private AbilityNodeDefinition draggingNode;
    private Vector2 nodeDragOffset;
    private GUIStyle normalNodeStyle;
    private GUIStyle selectedNodeStyle;

    [MenuItem("Tools/Ability Tree Editor")]
    public static void Open()
    {
        GetWindow<AbilityTreeEditorWindow>("Ability Tree");
    }

    public static void Open(AbilityTreeConfig_SO targetConfig)
    {
        AbilityTreeEditorWindow window = GetWindow<AbilityTreeEditorWindow>("Ability Tree");
        window.SetConfig(targetConfig);
        window.Focus();
    }

    private void OnEnable()
    {
        if (config == null && Selection.activeObject is AbilityTreeConfig_SO selectedConfig)
            SetConfig(selectedConfig);
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (config == null)
        {
            DrawEmptyState();
            return;
        }

        Rect graphRect = new Rect(0f, ToolbarHeight, position.width - InspectorWidth, position.height - ToolbarHeight);
        Rect inspectorRect = new Rect(position.width - InspectorWidth, ToolbarHeight, InspectorWidth, position.height - ToolbarHeight);

        DrawGraph(graphRect);
        DrawInspector(inspectorRect);
        HandleGraphEvents(graphRect);

        if (GUI.changed)
            Repaint();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight)))
        {
            EditorGUI.BeginChangeCheck();
            AbilityTreeConfig_SO nextConfig = (AbilityTreeConfig_SO)EditorGUILayout.ObjectField(config, typeof(AbilityTreeConfig_SO), false, GUILayout.Width(320f));
            if (EditorGUI.EndChangeCheck())
                SetConfig(nextConfig);

            using (new EditorGUI.DisabledScope(config == null))
            {
                if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    AddNode();
                using (new EditorGUI.DisabledScope(selectedNode == null))
                {
                    if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(54f)))
                        CopySelectedNode();
                }
                using (new EditorGUI.DisabledScope(copiedNode == null))
                {
                    if (GUILayout.Button("Paste", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                        PasteCopiedNode();
                }
                if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    DeleteSelectedNode();
                if (GUILayout.Button("Cleanup", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    CleanupPrerequisites();
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    LogValidation();
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(54f)))
                    SaveConfig();
                if (GUILayout.Button("Frame", EditorStyles.toolbarButton, GUILayout.Width(54f)))
                    FrameAll();
                axisSnapEnabled = GUILayout.Toggle(axisSnapEnabled, "Snap", EditorStyles.toolbarButton, GUILayout.Width(54f));
            }
        }
    }

    private void DrawEmptyState()
    {
        GUILayout.Space(80f);
        EditorGUILayout.HelpBox("Select an AbilityTreeConfig_SO asset, or choose one from the toolbar object field.", MessageType.Info);
    }

    private void DrawGraph(Rect graphRect)
    {
        GUI.Box(graphRect, GUIContent.none);
        DrawGrid(graphRect, 24f, new Color(1f, 1f, 1f, 0.06f));
        DrawGrid(graphRect, 120f, new Color(1f, 1f, 1f, 0.11f));
        DrawConnections();

        if (config.nodes == null)
            return;

        BeginWindows();
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null)
                continue;

            Rect nodeRect = NodeToScreenRect(node);
            GUI.Window(i + 1, nodeRect, _ => DrawNodeWindow(node), GUIContent.none, GetNodeStyle(ReferenceEquals(node, selectedNode)));
        }
        EndWindows();
    }

    private void DrawGrid(Rect rect, float spacing, Color color)
    {
        Handles.BeginGUI();
        Handles.color = color;
        float offsetX = graphPan.x % spacing;
        float offsetY = graphPan.y % spacing;

        for (float x = rect.xMin + offsetX; x < rect.xMax; x += spacing)
            Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));
        for (float y = rect.yMin + offsetY; y < rect.yMax; y += spacing)
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));

        Handles.EndGUI();
    }

    private void DrawConnections()
    {
        if (config == null || config.nodes == null)
            return;

        Dictionary<string, AbilityNodeDefinition> lookup = BuildLookup();
        Handles.BeginGUI();
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || node.prerequisiteNodeIds == null)
                continue;

            Rect toRect = NodeToScreenRect(node);
            Vector3 to = new Vector3(toRect.xMin, toRect.center.y);
            for (int p = 0; p < node.prerequisiteNodeIds.Count; p++)
            {
                if (!lookup.TryGetValue(node.prerequisiteNodeIds[p], out AbilityNodeDefinition prerequisite))
                    continue;

                Rect fromRect = NodeToScreenRect(prerequisite);
                Vector3 from = new Vector3(fromRect.xMax, fromRect.center.y);
                Handles.DrawBezier(from, to, from + Vector3.right * 80f, to + Vector3.left * 80f, new Color(0.92f, 0.58f, 0.15f, 0.95f), null, 3f);
            }
        }
        Handles.EndGUI();
    }

    private void DrawNodeWindow(AbilityNodeDefinition node)
    {
        string title = string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName;
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{Mathf.Max(0, node.cost)} points");
        if (node.effects != null && node.effects.Count > 0)
            EditorGUILayout.LabelField(DescribeFirstEffect(node.effects[0]));
    }

    private void DrawInspector(Rect inspectorRect)
    {
        if (selectedNode != null && (config.nodes == null || !config.nodes.Contains(selectedNode)))
            selectedNode = null;

        GUI.Box(inspectorRect, GUIContent.none);
        GUILayout.BeginArea(inspectorRect);
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);

        EditorGUILayout.LabelField("Node Detail", EditorStyles.boldLabel);
        AbilityNodeDefinition node = selectedNode;
        if (node == null)
        {
            EditorGUILayout.HelpBox("Select a node in the graph.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        EditorGUI.BeginChangeCheck();
        string controlKey = GetNodeControlKey(node);
        GUI.SetNextControlName($"{controlKey}_NodeId");
        node.nodeId = EditorGUILayout.TextField("Node ID", node.nodeId);
        GUI.SetNextControlName($"{controlKey}_DisplayName");
        node.displayName = EditorGUILayout.TextField("Display Name", node.displayName);
        GUI.SetNextControlName($"{controlKey}_Description");
        node.description = EditorGUILayout.TextField("Description", node.description, GUILayout.MinHeight(48f));
        node.cost = Mathf.Max(0, EditorGUILayout.IntField("Cost", node.cost));
        node.position = EditorGUILayout.Vector2Field("Position", node.position);
        node.isToggleable = EditorGUILayout.Toggle("Toggleable", node.isToggleable);
        if (node.isToggleable)
        {
            node.enabledByDefault = EditorGUILayout.Toggle("Enabled By Default", node.enabledByDefault);
            node.mutualExclusionGroupId = EditorGUILayout.TextField("Mutual Exclusion Group", node.mutualExclusionGroupId);
        }
        DrawPrerequisites(node);
        DrawEffects(node);
        if (EditorGUI.EndChangeCheck())
            MarkDirty();

        EditorGUILayout.Space(10f);
        AbilityTreeValidationResult validation = AbilityProgressionService.ValidateTree(config);
        EditorGUILayout.HelpBox(validation.isValid ? "Ability tree validation passed." : string.Join("\n", validation.messages), validation.isValid ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawPrerequisites(AbilityNodeDefinition node)
    {
        if (node.prerequisiteNodeIds == null)
            node.prerequisiteNodeIds = new List<string>();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Prerequisites", EditorStyles.boldLabel);
        for (int i = node.prerequisiteNodeIds.Count - 1; i >= 0; i--)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.SetNextControlName($"{GetNodeControlKey(node)}_Prerequisite_{i}");
                node.prerequisiteNodeIds[i] = EditorGUILayout.TextField(node.prerequisiteNodeIds[i]);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                    node.prerequisiteNodeIds.RemoveAt(i);
            }
        }

        List<string> candidates = BuildPrerequisiteCandidates(node);
        if (candidates.Count <= 0)
            return;

        int selectedIndex = EditorGUILayout.Popup("Candidate", 0, candidates.ToArray());
        if (GUILayout.Button("Add Prerequisite") && !node.prerequisiteNodeIds.Contains(candidates[selectedIndex]))
            node.prerequisiteNodeIds.Add(candidates[selectedIndex]);
    }

    private void DrawEffects(AbilityNodeDefinition node)
    {
        if (node.effects == null)
            node.effects = new List<AbilityNodeEffect>();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
        for (int i = node.effects.Count - 1; i >= 0; i--)
        {
            AbilityNodeEffect effect = node.effects[i];
            if (effect == null)
            {
                node.effects.RemoveAt(i);
                continue;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                effect.effectType = (AbilityEffectType)EditorGUILayout.EnumPopup(effect.effectType);
                effect.intValue = EditorGUILayout.IntField(effect.intValue, GUILayout.Width(64f));
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                    node.effects.RemoveAt(i);
            }

            DrawEffectReferenceFields(effect);
        }

        if (GUILayout.Button("Add Effect"))
            node.effects.Add(new AbilityNodeEffect { effectType = AbilityEffectType.StartingMoneyFlat, intValue = 10 });
    }

    private void DrawEffectReferenceFields(AbilityNodeEffect effect)
    {
        switch (effect.effectType)
        {
            case AbilityEffectType.StartingBlueprint:
                effect.blueprintTable = (BlueprintTable_SO)EditorGUILayout.ObjectField("Blueprint Table", effect.blueprintTable, typeof(BlueprintTable_SO), false);
                break;
            case AbilityEffectType.StartingLootTableRoll:
                effect.lootTable = (LootTable_SO)EditorGUILayout.ObjectField("Loot Table", effect.lootTable, typeof(LootTable_SO), false);
                effect.relicTable = (RelicTable_SO)EditorGUILayout.ObjectField("Relic Bag Table", effect.relicTable, typeof(RelicTable_SO), false);
                effect.relicRarityWeightTable = (LootRarityWeightTable_SO)EditorGUILayout.ObjectField("Relic Bag Rarity Weights", effect.relicRarityWeightTable, typeof(LootRarityWeightTable_SO), false);
                break;
            case AbilityEffectType.StartingPartySizeFlat:
            case AbilityEffectType.ExtraStartingCompanion:
                effect.characterData = (CharacterData_SO)EditorGUILayout.ObjectField("Extra Companion", effect.characterData, typeof(CharacterData_SO), false);
                break;
            case AbilityEffectType.TreasureRewardTableOverride:
                effect.treasureRewardTable = (TreasureRewardTable_SO)EditorGUILayout.ObjectField("Treasure Table", effect.treasureRewardTable, typeof(TreasureRewardTable_SO), false);
                break;
            case AbilityEffectType.StartingWeaponReplacement:
                effect.weaponData = (Weapon_SO)EditorGUILayout.ObjectField("Weapon", effect.weaponData, typeof(Weapon_SO), false);
                break;
        }
    }

    private void HandleGraphEvents(Rect graphRect)
    {
        Event current = Event.current;
        HandleCopyPasteShortcuts(current);
        if (!graphRect.Contains(current.mousePosition))
            return;

        if (current.type == EventType.MouseDown && current.button == 0)
        {
            GUI.FocusControl(null);
            AbilityNodeDefinition node = FindNodeAtScreenPosition(current.mousePosition);
            if (node != null)
            {
                selectedNode = node;
                draggingNode = node;
                isDraggingNode = true;
                nodeDragOffset = current.mousePosition - GraphToScreen(node.position);
                current.Use();
            }
            else
            {
                isDraggingCanvas = true;
                graphDragStart = current.mousePosition;
                current.Use();
            }
        }
        else if (current.type == EventType.MouseDrag && current.button == 0)
        {
            if (isDraggingNode && draggingNode != null)
            {
                Undo.RecordObject(config, "Move Ability Node");
                Vector2 desiredPosition = ScreenToGraph(current.mousePosition - nodeDragOffset);
                draggingNode.position = axisSnapEnabled
                    ? AbilityTreeEditorOperations.ApplyAxisSnap(config, draggingNode, desiredPosition, AxisSnapThreshold)
                    : desiredPosition;
                EditorUtility.SetDirty(config);
                current.Use();
            }
            else if (isDraggingCanvas)
            {
                Vector2 delta = current.mousePosition - graphDragStart;
                graphPan += delta;
                graphDragStart = current.mousePosition;
                current.Use();
            }
        }
        else if (current.type == EventType.MouseUp)
        {
            isDraggingCanvas = false;
            isDraggingNode = false;
            draggingNode = null;
        }
    }

    private void HandleCopyPasteShortcuts(Event current)
    {
        if (config == null || current == null || current.type != EventType.KeyDown || !current.control)
            return;
        if (IsEditingTextField())
            return;

        if (current.keyCode == KeyCode.C)
        {
            CopySelectedNode();
            current.Use();
        }
        else if (current.keyCode == KeyCode.V)
        {
            PasteCopiedNode();
            current.Use();
        }
    }

    private static bool IsEditingTextField()
    {
        string focusedControl = GUI.GetNameOfFocusedControl();
        return !string.IsNullOrEmpty(focusedControl);
    }

    private void AddNode()
    {
        if (config == null)
            return;

        Undo.RecordObject(config, "Add Ability Node");
        AbilityNodeDefinition node = AbilityTreeEditorOperations.AddNode(config, ScreenToGraph(new Vector2((position.width - InspectorWidth) * 0.5f, position.height * 0.5f)));
        selectedNode = node != null ? node : selectedNode;
        MarkDirty();
    }

    private void CopySelectedNode()
    {
        if (selectedNode == null)
            return;

        copiedNode = selectedNode;
        ShowNotification(new GUIContent($"Copied {GetNodeLabel(selectedNode)}"));
    }

    private void PasteCopiedNode()
    {
        if (config == null || copiedNode == null)
            return;

        if (config.nodes == null || !config.nodes.Contains(copiedNode))
        {
            copiedNode = null;
            return;
        }

        Undo.RecordObject(config, "Paste Ability Node");
        AbilityNodeDefinition duplicate = AbilityTreeEditorOperations.DuplicateNode(config, copiedNode, DuplicateOffset);
        if (duplicate != null)
        {
            selectedNode = duplicate;
            copiedNode = duplicate;
            MarkDirty();
            ShowNotification(new GUIContent($"Pasted {GetNodeLabel(duplicate)}"));
        }
    }

    private void DeleteSelectedNode()
    {
        if (config == null || selectedNode == null)
            return;

        Undo.RecordObject(config, "Delete Ability Node");
        if (AbilityTreeEditorOperations.DeleteNode(config, selectedNode.nodeId))
        {
            if (ReferenceEquals(copiedNode, selectedNode))
                copiedNode = null;
            selectedNode = null;
            MarkDirty();
        }
    }

    private void CleanupPrerequisites()
    {
        if (config == null)
            return;

        Undo.RecordObject(config, "Cleanup Ability Prerequisites");
        AbilityTreeEditorOperations.GenerateMissingIds(config);
        AbilityTreeEditorOperations.RemoveInvalidPrerequisites(config);
        MarkDirty();
    }

    private void LogValidation()
    {
        AbilityTreeValidationResult result = AbilityProgressionService.ValidateTree(config);
        if (result.isValid)
            Debug.Log("[AbilityTreeEditorWindow] Ability tree validation passed.", config);
        else
            Debug.LogWarning($"[AbilityTreeEditorWindow] Ability tree validation failed:\n{string.Join("\n", result.messages)}", config);
    }

    private void SaveConfig()
    {
        if (config == null)
            return;

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private void FrameAll()
    {
        if (config == null || config.nodes == null || config.nodes.Count == 0)
            return;

        Vector2 center = Vector2.zero;
        int count = 0;
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null)
                continue;
            center += node.position;
            count++;
        }

        if (count > 0)
            graphPan = new Vector2((position.width - InspectorWidth) * 0.5f, (position.height + ToolbarHeight) * 0.5f) - center / count;
    }

    private static string GetNodeLabel(AbilityNodeDefinition node)
    {
        if (node == null)
            return "Node";
        if (!string.IsNullOrWhiteSpace(node.displayName))
            return node.displayName;
        if (!string.IsNullOrWhiteSpace(node.nodeId))
            return node.nodeId;
        return "Node";
    }

    private void SetConfig(AbilityTreeConfig_SO targetConfig)
    {
        GUI.FocusControl(null);
        config = targetConfig;
        selectedNode = config != null && config.nodes != null && config.nodes.Count > 0 ? config.nodes.Find(node => node != null) : null;
        Repaint();
    }

    private void MarkDirty()
    {
        if (config == null)
            return;

        EditorUtility.SetDirty(config);
        GUI.changed = true;
    }

    private Rect NodeToScreenRect(AbilityNodeDefinition node)
    {
        Vector2 center = GraphToScreen(node.position);
        return new Rect(center.x - NodeWidth * 0.5f, center.y - NodeHeight * 0.5f, NodeWidth, NodeHeight);
    }

    private Vector2 GraphToScreen(Vector2 graphPosition)
    {
        return graphPosition + graphPan;
    }

    private Vector2 ScreenToGraph(Vector2 screenPosition)
    {
        return screenPosition - graphPan;
    }

    private AbilityNodeDefinition FindNodeAtScreenPosition(Vector2 screenPosition)
    {
        if (config == null || config.nodes == null)
            return null;

        for (int i = config.nodes.Count - 1; i >= 0; i--)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node != null && NodeToScreenRect(node).Contains(screenPosition))
                return node;
        }

        return null;
    }

    private Dictionary<string, AbilityNodeDefinition> BuildLookup()
    {
        Dictionary<string, AbilityNodeDefinition> lookup = new Dictionary<string, AbilityNodeDefinition>();
        if (config == null || config.nodes == null)
            return lookup;

        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node != null && !string.IsNullOrWhiteSpace(node.nodeId) && !lookup.ContainsKey(node.nodeId))
                lookup.Add(node.nodeId, node);
        }

        return lookup;
    }

    private List<string> BuildPrerequisiteCandidates(AbilityNodeDefinition node)
    {
        List<string> candidates = new List<string>();
        if (config == null || config.nodes == null || node == null)
            return candidates;

        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition candidate = config.nodes[i];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.nodeId) || candidate.nodeId == node.nodeId)
                continue;
            candidates.Add(candidate.nodeId);
        }

        return candidates;
    }

    private string GetNodeControlKey(AbilityNodeDefinition node)
    {
        int index = config != null && config.nodes != null ? config.nodes.IndexOf(node) : -1;
        return $"AbilityNode_{Mathf.Max(0, index)}";
    }

    private static string DescribeFirstEffect(AbilityNodeEffect effect)
    {
        if (effect == null)
            return string.Empty;

        switch (effect.effectType)
        {
            case AbilityEffectType.StartingMoneyFlat:
                return $"Money +{effect.intValue}";
            case AbilityEffectType.StartingActionPointLimitFlat:
                return $"Action points +{effect.intValue}";
            case AbilityEffectType.StartingBlueprint:
                return $"Random Blueprint x{Mathf.Max(1, effect.intValue)}";
            case AbilityEffectType.StartingLootTableRoll:
                return $"Loot table rolls x{Mathf.Max(1, effect.intValue)}";
            case AbilityEffectType.MerchantFreeRefreshPerShop:
                return $"Shop free refresh +{effect.intValue}";
            case AbilityEffectType.FirstMerchantPurchaseHalfDiscount:
                return "First shop purchase half";
            case AbilityEffectType.BattleStartTemporaryEnergy:
                return $"Temp energy +{effect.intValue}";
            case AbilityEffectType.BattleStartEnergyMaxFlat:
                return $"Energy max +{effect.intValue}";
            case AbilityEffectType.BattleStartMaxHealthFlat:
                return $"Max health +{effect.intValue}";
            case AbilityEffectType.StartingPartySizeFlat:
                return $"Party size +{effect.intValue}";
            case AbilityEffectType.ExtraStartingCompanion:
                return "Extra companion choice";
            case AbilityEffectType.TreasureRewardTableOverride:
                return "Treasure table override";
            case AbilityEffectType.StartingWeaponReplacement:
                return "Starting weapon replacement";
            case AbilityEffectType.RelicChoiceRefreshCount:
                return $"Relic choice refresh +{effect.intValue}";
            default:
                return effect.effectType.ToString();
        }
    }

    private GUIStyle GetNodeStyle(bool selected)
    {
        if (normalNodeStyle == null)
        {
            normalNodeStyle = new GUIStyle(GUI.skin.window)
            {
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 10, 8),
                normal =
                {
                    textColor = new Color(0.9f, 0.86f, 0.76f, 1f),
                    background = MakeTexture(new Color(0.18f, 0.14f, 0.08f, 0.96f))
                }
            };
        }

        if (selectedNodeStyle == null)
        {
            selectedNodeStyle = new GUIStyle(normalNodeStyle)
            {
                normal =
                {
                    textColor = Color.white,
                    background = MakeTexture(new Color(0.35f, 0.22f, 0.06f, 1f))
                }
            };
        }

        return selected ? selectedNodeStyle : normalNodeStyle;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
