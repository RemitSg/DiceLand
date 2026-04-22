using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MainMenuSceneControllerTutorialTests
{
    [TearDown]
    public void TearDown()
    {
        DestroyIfExists(GameObject.Find("MainMenuRoot"));
        MainMenuRuntime.ClearContinueRequest();
        MainMenuRuntime.ClearTutorialRequest();
        MainMenuRuntime.ClearSelectedDifficulty();
    }

    [Test]
    public void ResolveMenuAction_RecognizesTutorialIdentity()
    {
        MethodInfo method = typeof(MainMenuSceneController).GetMethod("ResolveMenuAction", BindingFlags.Static | BindingFlags.NonPublic);

        object resolved = method.Invoke(null, new object[] { "Tutorial" });

        Assert.IsNotNull(resolved);
        Assert.AreEqual("Tutorial", resolved.ToString());
    }

    [Test]
    public void ConfigureMainMenuItems_WhenTutorialMissing_InjectsTutorialEntryAfterContinue()
    {
        GameObject root = new GameObject("MainMenuRoot");
        MainMenuSceneController controller = root.AddComponent<MainMenuSceneController>();
        GalleryLevelSelectionManager gallery = root.AddComponent<GalleryLevelSelectionManager>();

        DemoGalleryMenuItem newGameItem = CreateMenuItem("Solo Play", root.transform);
        DemoGalleryMenuItem continueItem = CreateMenuItem("Load", root.transform);
        DemoGalleryMenuItem demoItem = CreateMenuItem("Demo", root.transform);

        gallery.items = new GalleryLevelView[]
        {
            newGameItem,
            continueItem,
            demoItem
        };

        SetPrivateField(controller, "mainMenuGallery", gallery);
        InvokePrivate(controller, "ConfigureMainMenuItems");

        Assert.AreEqual(4, gallery.items.Length);
        Assert.AreEqual("NewGame", gallery.items[0].levelName);
        Assert.AreEqual("Continue", gallery.items[1].levelName);
        Assert.AreEqual("Tutorial", gallery.items[2].levelName);
        Assert.AreEqual("DemoTest", gallery.items[3].levelName);
        Assert.AreNotSame(demoItem.gameObject, gallery.items[2].gameObject);
    }

    [Test]
    public void PrepareTutorialRun_ClearsNormalStateAndRequestsTutorial()
    {
        GameObject root = new GameObject("MainMenuRoot");
        MainMenuSceneController controller = root.AddComponent<MainMenuSceneController>();
        DifficultyConfig_SO difficulty = ScriptableObject.CreateInstance<DifficultyConfig_SO>();

        MainMenuRuntime.SetSelectedDifficulty(difficulty);
        MainMenuRuntime.RequestContinueFromSave();
        MainMenuRuntime.ClearTutorialRequest();

        InvokePrivate(controller, "PrepareTutorialRun");

        Assert.IsNull(MainMenuRuntime.SelectedDifficultyConfig);
        Assert.IsFalse(MainMenuRuntime.ContinueRequested);
        Assert.IsTrue(MainMenuRuntime.TutorialRequested);

        ScriptableObject.DestroyImmediate(difficulty);
    }

    private static DemoGalleryMenuItem CreateMenuItem(string name, Transform parent)
    {
        GameObject itemObject = new GameObject(name, typeof(RectTransform), typeof(DemoGalleryMenuItem));
        itemObject.transform.SetParent(parent, false);

        DemoGalleryMenuItem item = itemObject.GetComponent<DemoGalleryMenuItem>();
        item.levelName = name;
        return item;
    }

    private static void DestroyIfExists(Object target)
    {
        if (target != null)
            Object.DestroyImmediate(target);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, methodName);
        method.Invoke(target, null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        field.SetValue(target, value);
    }
}
