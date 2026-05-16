using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpecialDungeon_SO))]
public sealed class SpecialDungeonSOEditor : Editor
{
    private readonly List<bool> entryFoldouts = new List<bool>();

    private SerializedProperty dungeonIdProperty;
    private SerializedProperty dungeonNameProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty entriesProperty;

    private void OnEnable()
    {
        dungeonIdProperty = serializedObject.FindProperty("dungeonId");
        dungeonNameProperty = serializedObject.FindProperty("dungeonName");
        descriptionProperty = serializedObject.FindProperty("description");
        entriesProperty = serializedObject.FindProperty("entries");
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
        if (entriesProperty == null)
            return;

        if (entriesProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("当前副本没有配置格子。左右出口和出生点会自动生成；这里从出生点右侧开始依次生成。", MessageType.Info);
            return;
        }

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
            if (entry == null)
                continue;

            DrawEntry(i, entry);
        }
    }

    private void DrawEntry(int index, SerializedProperty entry)
    {
        SerializedProperty tileType = entry.FindPropertyRelative("tileType");
        MapTileType type = (MapTileType)tileType.intValue;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        entryFoldouts[index] = EditorGUILayout.Foldout(
            entryFoldouts[index],
            $"#{index + 1} {GetTileTypeLabel(type)}",
            true);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("上移", GUILayout.Width(44f)))
            MoveEntry(index, -1);
        if (GUILayout.Button("下移", GUILayout.Width(44f)))
            MoveEntry(index, 1);
        if (GUILayout.Button("删除", GUILayout.Width(44f)))
        {
            RemoveEntry(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (entryFoldouts[index])
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(tileType, new GUIContent("格子类型"));
            type = (MapTileType)tileType.intValue;
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("previewTag"), new GUIContent("预览标签"));
            DrawTypeSpecificFields(entry, type);
            DrawValidation(entry, type);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTypeSpecificFields(SerializedProperty entry, MapTileType type)
    {
        switch (type)
        {
            case MapTileType.Battle:
            case MapTileType.Elite:
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("enemyGroup"), new GUIContent("遭遇敌人组"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("previewDangerRating"), new GUIContent("危险度"));
                break;
            case MapTileType.Merchant:
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("merchantOffers"), new GUIContent("商店商品"), true);
                break;
            case MapTileType.Treasure:
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("treasureOptions"), new GUIContent("宝藏选项"), true);
                break;
            case MapTileType.Event:
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("eventData"), new GUIContent("指定事件"));
                break;
            default:
                EditorGUILayout.HelpBox("特殊副本目前只支持战斗、精英、商店、宝藏和事件格。", MessageType.Warning);
                break;
        }
    }

    private void DrawValidation(SerializedProperty entry, MapTileType type)
    {
        switch (type)
        {
            case MapTileType.Battle:
            case MapTileType.Elite:
                if (entry.FindPropertyRelative("enemyGroup").objectReferenceValue == null)
                    EditorGUILayout.HelpBox("战斗/精英格建议配置敌人组，否则进入时会缺少遭遇内容。", MessageType.Warning);
                break;
            case MapTileType.Merchant:
                if (entry.FindPropertyRelative("merchantOffers").arraySize == 0)
                    EditorGUILayout.HelpBox("商店格当前没有商品。", MessageType.Info);
                break;
            case MapTileType.Treasure:
                if (entry.FindPropertyRelative("treasureOptions").arraySize == 0)
                    EditorGUILayout.HelpBox("宝藏格当前没有奖励选项。", MessageType.Warning);
                break;
            case MapTileType.Event:
                if (entry.FindPropertyRelative("eventData").objectReferenceValue == null)
                    EditorGUILayout.HelpBox("事件格需要指定一个事件。", MessageType.Warning);
                break;
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

    private void MoveEntry(int index, int direction)
    {
        int target = index + direction;
        if (target < 0 || target >= entriesProperty.arraySize)
            return;

        entriesProperty.MoveArrayElement(index, target);
        bool foldout = entryFoldouts[index];
        entryFoldouts[index] = entryFoldouts[target];
        entryFoldouts[target] = foldout;
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
