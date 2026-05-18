using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SpecialDungeon_SO))]
public sealed class SpecialDungeonSOEditor : Editor
{
    private const float LineHeight = 18f;
    private const float VerticalSpacing = 4f;
    private const float BoxPadding = 6f;
    private const float HelpBoxHeight = 40f;

    private readonly List<bool> entryFoldouts = new List<bool>();

    private SerializedProperty dungeonIdProperty;
    private SerializedProperty dungeonNameProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty entriesProperty;
    private ReorderableList entriesList;
    private int pendingRemoveIndex = -1;

    private void OnEnable()
    {
        dungeonIdProperty = serializedObject.FindProperty("dungeonId");
        dungeonNameProperty = serializedObject.FindProperty("dungeonName");
        descriptionProperty = serializedObject.FindProperty("description");
        entriesProperty = serializedObject.FindProperty("entries");
        BuildEntriesList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDungeonHeader();
        DrawEntryToolbar();
        DrawEntries();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDungeonHeader()
    {
        EditorGUILayout.LabelField("特殊副本", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dungeonIdProperty, new GUIContent("副本 ID"));
        EditorGUILayout.PropertyField(dungeonNameProperty, new GUIContent("显示名称"));
        EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("描述"));
        EditorGUILayout.Space(8f);
    }

    private void DrawEntryToolbar()
    {
        EditorGUILayout.LabelField("格子列表", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("可拖动每个格子左侧的手柄调整生成顺序。左右出口和出生点会自动生成；这里从出生点右侧开始依次生成。", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加战斗格"))
            AddEntry(MapTileType.Battle);
        if (GUILayout.Button("添加精英格"))
            AddEntry(MapTileType.Elite);
        if (GUILayout.Button("添加商店格"))
            AddEntry(MapTileType.Merchant);
        if (GUILayout.Button("添加宝藏格"))
            AddEntry(MapTileType.Treasure);
        if (GUILayout.Button("添加事件格"))
            AddEntry(MapTileType.Event);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全部展开"))
            SetAllFoldouts(true);
        if (GUILayout.Button("全部折叠"))
            SetAllFoldouts(false);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6f);
    }

    private void DrawEntries()
    {
        EnsureFoldoutCount();
        if (entriesProperty == null || entriesList == null)
            return;

        pendingRemoveIndex = -1;
        entriesList.DoLayoutList();

        if (pendingRemoveIndex >= 0)
            RemoveEntry(pendingRemoveIndex);
    }

    private void BuildEntriesList()
    {
        if (entriesProperty == null)
            return;

        entriesList = new ReorderableList(serializedObject, entriesProperty, true, false, false, false)
        {
            drawElementCallback = DrawEntryElement,
            elementHeightCallback = GetEntryHeight,
            onReorderCallback = _ => EnsureFoldoutCount()
        };
    }

    private void DrawEntryElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        EnsureFoldoutCount();
        if (index < 0 || index >= entriesProperty.arraySize)
            return;

        SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(index);
        if (entry == null)
            return;

        SerializedProperty tileType = entry.FindPropertyRelative("tileType");
        MapTileType type = (MapTileType)tileType.intValue;

        Rect contentRect = new Rect(rect.x + BoxPadding, rect.y + 2f, rect.width - BoxPadding, rect.height - 4f);
        Rect lineRect = new Rect(contentRect.x, contentRect.y, contentRect.width, LineHeight);
        Rect foldoutRect = new Rect(lineRect.x, lineRect.y, lineRect.width - 54f, LineHeight);
        Rect deleteRect = new Rect(lineRect.xMax - 48f, lineRect.y, 48f, LineHeight);

        entryFoldouts[index] = EditorGUI.Foldout(foldoutRect, entryFoldouts[index], $"#{index + 1} {GetTileTypeLabel(type)}", true);
        if (GUI.Button(deleteRect, "删除"))
        {
            pendingRemoveIndex = index;
            return;
        }

        if (!entryFoldouts[index])
            return;

        EditorGUI.indentLevel++;
        lineRect.y += LineHeight + VerticalSpacing;
        DrawProperty(ref lineRect, tileType, "格子类型");
        type = (MapTileType)tileType.intValue;
        DrawProperty(ref lineRect, entry.FindPropertyRelative("previewTag"), "预览标签");
        DrawTypeSpecificFields(ref lineRect, entry, type);
        DrawValidation(ref lineRect, entry, type);
        EditorGUI.indentLevel--;
    }

    private float GetEntryHeight(int index)
    {
        EnsureFoldoutCount();
        if (index < 0 || index >= entriesProperty.arraySize)
            return LineHeight + VerticalSpacing;

        SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(index);
        if (entry == null)
            return LineHeight + VerticalSpacing;

        float height = LineHeight + VerticalSpacing + 4f;
        if (index >= entryFoldouts.Count || !entryFoldouts[index])
            return height;

        MapTileType type = (MapTileType)entry.FindPropertyRelative("tileType").intValue;
        height += GetPropertyHeight(entry.FindPropertyRelative("tileType"));
        height += GetPropertyHeight(entry.FindPropertyRelative("previewTag"));
        height += GetTypeSpecificHeight(entry, type);
        height += GetValidationHeight(entry, type);
        return height + BoxPadding;
    }

    private void DrawTypeSpecificFields(ref Rect lineRect, SerializedProperty entry, MapTileType type)
    {
        switch (type)
        {
            case MapTileType.Battle:
            case MapTileType.Elite:
                DrawProperty(ref lineRect, entry.FindPropertyRelative("enemyGroup"), "遭遇敌人组");
                DrawProperty(ref lineRect, entry.FindPropertyRelative("previewDangerRating"), "危险度");
                break;
            case MapTileType.Merchant:
                DrawProperty(ref lineRect, entry.FindPropertyRelative("merchantOffers"), "商店商品", true);
                break;
            case MapTileType.Treasure:
                DrawProperty(ref lineRect, entry.FindPropertyRelative("treasureOptions"), "宝藏选项", true);
                break;
            case MapTileType.Event:
                DrawProperty(ref lineRect, entry.FindPropertyRelative("eventData"), "指定事件");
                break;
            default:
                DrawHelpBox(ref lineRect, "特殊副本目前只支持战斗、精英、商店、宝藏和事件格。", MessageType.Warning);
                break;
        }
    }

    private void DrawValidation(ref Rect lineRect, SerializedProperty entry, MapTileType type)
    {
        switch (type)
        {
            case MapTileType.Battle:
            case MapTileType.Elite:
                if (entry.FindPropertyRelative("enemyGroup").objectReferenceValue == null)
                    DrawHelpBox(ref lineRect, "战斗/精英格建议配置敌人组，否则进入时会缺少遭遇内容。", MessageType.Warning);
                break;
            case MapTileType.Merchant:
                if (entry.FindPropertyRelative("merchantOffers").arraySize == 0)
                    DrawHelpBox(ref lineRect, "商店格当前没有商品。", MessageType.Info);
                break;
            case MapTileType.Treasure:
                if (entry.FindPropertyRelative("treasureOptions").arraySize == 0)
                    DrawHelpBox(ref lineRect, "宝藏格当前没有奖励选项。", MessageType.Warning);
                break;
            case MapTileType.Event:
                if (entry.FindPropertyRelative("eventData").objectReferenceValue == null)
                    DrawHelpBox(ref lineRect, "事件格需要指定一个事件。", MessageType.Warning);
                break;
        }
    }

    private static void DrawProperty(ref Rect lineRect, SerializedProperty property, string label, bool includeChildren = false)
    {
        float height = EditorGUI.GetPropertyHeight(property, includeChildren);
        lineRect.height = height;
        EditorGUI.PropertyField(lineRect, property, new GUIContent(label), includeChildren);
        lineRect.y += height + VerticalSpacing;
    }

    private static void DrawHelpBox(ref Rect lineRect, string message, MessageType messageType)
    {
        lineRect.height = HelpBoxHeight;
        EditorGUI.HelpBox(lineRect, message, messageType);
        lineRect.y += HelpBoxHeight + VerticalSpacing;
    }

    private static float GetPropertyHeight(SerializedProperty property, bool includeChildren = false)
    {
        return EditorGUI.GetPropertyHeight(property, includeChildren) + VerticalSpacing;
    }

    private static float GetTypeSpecificHeight(SerializedProperty entry, MapTileType type)
    {
        switch (type)
        {
            case MapTileType.Battle:
            case MapTileType.Elite:
                return GetPropertyHeight(entry.FindPropertyRelative("enemyGroup"))
                    + GetPropertyHeight(entry.FindPropertyRelative("previewDangerRating"));
            case MapTileType.Merchant:
                return GetPropertyHeight(entry.FindPropertyRelative("merchantOffers"), true);
            case MapTileType.Treasure:
                return GetPropertyHeight(entry.FindPropertyRelative("treasureOptions"), true);
            case MapTileType.Event:
                return GetPropertyHeight(entry.FindPropertyRelative("eventData"));
            default:
                return HelpBoxHeight + VerticalSpacing;
        }
    }

    private static float GetValidationHeight(SerializedProperty entry, MapTileType type)
    {
        switch (type)
        {
            case MapTileType.Battle:
            case MapTileType.Elite:
                return entry.FindPropertyRelative("enemyGroup").objectReferenceValue == null ? HelpBoxHeight + VerticalSpacing : 0f;
            case MapTileType.Merchant:
                return entry.FindPropertyRelative("merchantOffers").arraySize == 0 ? HelpBoxHeight + VerticalSpacing : 0f;
            case MapTileType.Treasure:
                return entry.FindPropertyRelative("treasureOptions").arraySize == 0 ? HelpBoxHeight + VerticalSpacing : 0f;
            case MapTileType.Event:
                return entry.FindPropertyRelative("eventData").objectReferenceValue == null ? HelpBoxHeight + VerticalSpacing : 0f;
            default:
                return 0f;
        }
    }

    private void AddEntry(MapTileType type)
    {
        int index = entriesProperty.arraySize;
        entriesProperty.InsertArrayElementAtIndex(index);
        SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(index);
        ConfigureEntry(entry, type);
        entryFoldouts.Add(true);
    }

    private static void ConfigureEntry(SerializedProperty entry, MapTileType type)
    {
        entry.FindPropertyRelative("tileType").intValue = (int)type;
        entry.FindPropertyRelative("previewTag").stringValue = string.Empty;
        entry.FindPropertyRelative("previewDangerRating").intValue = 0;
        entry.FindPropertyRelative("enemyGroup").objectReferenceValue = null;
        entry.FindPropertyRelative("eventData").objectReferenceValue = null;
        entry.FindPropertyRelative("merchantOffers").ClearArray();
        entry.FindPropertyRelative("treasureOptions").ClearArray();
    }

    private void RemoveEntry(int index)
    {
        entriesProperty.DeleteArrayElementAtIndex(index);
        if (index >= 0 && index < entryFoldouts.Count)
            entryFoldouts.RemoveAt(index);
    }

    private void SetAllFoldouts(bool value)
    {
        EnsureFoldoutCount();
        for (int i = 0; i < entryFoldouts.Count; i++)
            entryFoldouts[i] = value;
    }

    private void EnsureFoldoutCount()
    {
        if (entriesProperty == null)
            return;

        while (entryFoldouts.Count < entriesProperty.arraySize)
            entryFoldouts.Add(true);
        while (entryFoldouts.Count > entriesProperty.arraySize)
            entryFoldouts.RemoveAt(entryFoldouts.Count - 1);
    }

    private static string GetTileTypeLabel(MapTileType type)
    {
        switch (type)
        {
            case MapTileType.Battle:
                return "战斗格";
            case MapTileType.Elite:
                return "精英战斗格";
            case MapTileType.Merchant:
                return "商店格";
            case MapTileType.Treasure:
                return "宝藏格";
            case MapTileType.Event:
                return "事件格";
            default:
                return type.ToString();
        }
    }
}
