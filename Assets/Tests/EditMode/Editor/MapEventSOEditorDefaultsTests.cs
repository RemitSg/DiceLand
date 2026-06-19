using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MapEventSOEditorDefaultsTests
{
    private MapEvent_SO eventData;

    [TearDown]
    public void TearDown()
    {
        if (eventData != null)
            Object.DestroyImmediate(eventData);
    }

    [Test]
    public void ApplyNewOptionDefaults_EnablesResolveEventOnlyForNewOptions()
    {
        eventData = ScriptableObject.CreateInstance<MapEvent_SO>();
        eventData.options.Add(new MapEventOptionData { resolveEvent = false });
        eventData.options.Add(new MapEventOptionData { resolveEvent = false });

        SerializedObject serializedEvent = new SerializedObject(eventData);
        SerializedProperty options = serializedEvent.FindProperty("options");
        bool changed = MapEventSOEditorDefaults.ApplyNewOptionDefaults(options, 1);
        serializedEvent.ApplyModifiedProperties();

        Assert.IsTrue(changed);
        Assert.IsFalse(eventData.options[0].resolveEvent);
        Assert.IsTrue(eventData.options[1].resolveEvent);
    }
}
