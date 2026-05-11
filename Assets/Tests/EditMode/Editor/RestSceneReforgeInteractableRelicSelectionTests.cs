using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityObject = UnityEngine.Object;

public class RestSceneReforgeInteractableRelicSelectionTests
{
    private GameObject testObject;
    private Relic_SO createdRelic;
    private readonly System.Collections.Generic.List<UnityObject> createdObjects = new System.Collections.Generic.List<UnityObject>();

    [TearDown]
    public void TearDown()
    {
        RestSceneRuntime.Clear();
        MapRunRuntime.Clear();

        if (createdRelic != null)
            UnityObject.DestroyImmediate(createdRelic);
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                UnityObject.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
        if (testObject != null)
            UnityObject.DestroyImmediate(testObject);
    }

    [Test]
    public void HandleRelicSelection_ClickingOnlySelectedRelicAgainClearsSelection()
    {
        createdRelic = ScriptableObject.CreateInstance<Relic_SO>();
        createdRelic.relicName = "Test Common Relic";
        createdRelic.rarity = LootRarity.Common;

        RunState runState = new RunState();
        runState.AddRelic(createdRelic, 1);
        SetMapRunSnapshot(new MapRunSnapshot
        {
            sceneName = "TestRestReturnScene",
            runState = runState
        });
        RestSceneRuntime.BeginVisit("TestRestReturnScene", Vector2Int.zero);

        RestSceneReforgeInteractable interactable = CreateInteractable();
        InvokeHandleRelicSelection(interactable, createdRelic);
        Assert.AreSame(createdRelic, GetSelectedRelicA(interactable));

        InvokeHandleRelicSelection(interactable, createdRelic);

        Assert.IsNull(GetSelectedRelicA(interactable));
        Assert.IsNull(GetSelectedRelicB(interactable));
    }

    [Test]
    public void BuildCurrentLootReforgeCandidates_IncludesSelectedMaterialWithMatchingRarityAndAttribute()
    {
        LootAttribute_SO blood = CreateAttribute("血");
        LootAttribute_SO fire = CreateAttribute("火");
        LootItem_SO clotspore = CreateLoot("凝血孢", LootRarity.Common, blood);
        LootItem_SO fang = CreateLoot("秽蚀尖牙", LootRarity.Common, blood);
        LootItem_SO ember = CreateLoot("余烬", LootRarity.Common, fire);
        LootItem_SO uncommonBlood = CreateLoot("稀有血", LootRarity.Uncommon, blood);
        LootItemTable_SO table = CreateLootTable(clotspore, fang, ember, uncommonBlood);

        RestSceneReforgeInteractable interactable = CreateInteractable();
        SetPrivateField(interactable, "lootReforgeTable", table);
        SetPrivateField(interactable, "selectedLootA", clotspore);
        SetPrivateField(interactable, "selectedLootB", fang);
        SetPrivateField(interactable, "selectedLootAttribute", blood);

        System.Collections.Generic.List<LootItem_SO> candidates = InvokeBuildCurrentLootReforgeCandidates(interactable);

        CollectionAssert.AreEqual(new[] { clotspore, fang }, candidates);
    }

    private RestSceneReforgeInteractable CreateInteractable()
    {
        testObject = new GameObject("RestSceneReforgeInteractableTest");
        testObject.AddComponent<BoxCollider2D>();
        return testObject.AddComponent<RestSceneReforgeInteractable>();
    }

    private static void InvokeHandleRelicSelection(RestSceneReforgeInteractable interactable, Relic_SO relic)
    {
        MethodInfo method = typeof(RestSceneReforgeInteractable).GetMethod(
            "HandleRelicSelection",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(interactable, new object[] { relic });
    }

    private static System.Collections.Generic.List<LootItem_SO> InvokeBuildCurrentLootReforgeCandidates(RestSceneReforgeInteractable interactable)
    {
        MethodInfo method = typeof(RestSceneReforgeInteractable).GetMethod(
            "BuildCurrentLootReforgeCandidates",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (System.Collections.Generic.List<LootItem_SO>)method.Invoke(interactable, null);
    }

    private static Relic_SO GetSelectedRelicA(RestSceneReforgeInteractable interactable)
    {
        return GetPrivateField<Relic_SO>(interactable, "selectedRelicA");
    }

    private static Relic_SO GetSelectedRelicB(RestSceneReforgeInteractable interactable)
    {
        return GetPrivateField<Relic_SO>(interactable, "selectedRelicB");
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(target, value);
    }

    private LootAttribute_SO CreateAttribute(string name)
    {
        LootAttribute_SO attribute = ScriptableObject.CreateInstance<LootAttribute_SO>();
        attribute.attributeName = name;
        createdObjects.Add(attribute);
        return attribute;
    }

    private LootItem_SO CreateLoot(string name, LootRarity rarity, LootAttribute_SO attribute)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        loot.rarity = rarity;
        loot.attribute = attribute;
        createdObjects.Add(loot);
        return loot;
    }

    private LootItemTable_SO CreateLootTable(params LootItem_SO[] loots)
    {
        LootItemTable_SO table = ScriptableObject.CreateInstance<LootItemTable_SO>();
        SetPrivateField(table, "lootItems", new System.Collections.Generic.List<LootItem_SO>(loots));
        createdObjects.Add(table);
        return table;
    }

    private static void SetMapRunSnapshot(MapRunSnapshot snapshot)
    {
        FieldInfo field = typeof(MapRunRuntime).GetField("currentSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(null, snapshot);
    }
}
