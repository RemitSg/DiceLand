using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapEvent_SO))]
public sealed class MapEventSOEditor : Editor
{
    private SerializedProperty optionsProperty;
    private int previousOptionsSize;

    private void OnEnable()
    {
        serializedObject.Update();
        optionsProperty = serializedObject.FindProperty("options");
        previousOptionsSize = optionsProperty != null ? Mathf.Max(0, optionsProperty.arraySize) : 0;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        int optionsSizeBeforeDraw = optionsProperty != null ? Mathf.Max(0, optionsProperty.arraySize) : 0;
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();

        serializedObject.Update();
        int optionsSizeAfterDraw = optionsProperty != null ? Mathf.Max(0, optionsProperty.arraySize) : 0;
        int previousSize = Mathf.Max(previousOptionsSize, optionsSizeBeforeDraw);
        bool initializedNewOptions = MapEventSOEditorDefaults.ApplyNewOptionDefaults(optionsProperty, previousSize);
        if (initializedNewOptions)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        previousOptionsSize = optionsSizeAfterDraw;
    }
}

public static class MapEventSOEditorDefaults
{
    public static bool ApplyNewOptionDefaults(SerializedProperty optionsProperty, int previousOptionsSize)
    {
        if (optionsProperty == null || !optionsProperty.isArray)
            return false;

        int currentSize = Mathf.Max(0, optionsProperty.arraySize);
        int firstNewIndex = Mathf.Clamp(previousOptionsSize, 0, currentSize);
        bool changed = false;
        for (int i = firstNewIndex; i < currentSize; i++)
        {
            SerializedProperty optionProperty = optionsProperty.GetArrayElementAtIndex(i);
            SerializedProperty resolveEventProperty = optionProperty != null
                ? optionProperty.FindPropertyRelative("resolveEvent")
                : null;
            if (resolveEventProperty == null || resolveEventProperty.boolValue)
                continue;

            resolveEventProperty.boolValue = true;
            changed = true;
        }

        return changed;
    }
}
