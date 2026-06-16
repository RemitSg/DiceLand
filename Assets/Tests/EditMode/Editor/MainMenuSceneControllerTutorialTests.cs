using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

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
    public void ResolveMenuAction_RecognizesLocalizedTutorialIdentity()
    {
        MethodInfo method = typeof(MainMenuSceneController).GetMethod("ResolveMenuAction", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);

        object resolved = method.Invoke(null, new object[] { "教程" });

        Assert.IsNotNull(resolved);
        Assert.AreEqual("Tutorial", resolved.ToString());
    }

    [Test]
    public void ResolveMenuAction_RecognizesAbilityIdentity()
    {
        MethodInfo method = typeof(MainMenuSceneController).GetMethod("ResolveMenuAction", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);

        Assert.AreEqual("Ability", method.Invoke(null, new object[] { "Ability" }).ToString());
        Assert.AreEqual("Ability", method.Invoke(null, new object[] { "ability" }).ToString());
        Assert.AreEqual("Ability", method.Invoke(null, new object[] { "能力" }).ToString());
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
        SetPrivateField(controller, "newGameLabel", "NewGame");
        SetPrivateField(controller, "continueLabel", "Continue");
        SetPrivateField(controller, "tutorialLabel", "Tutorial");
        SetPrivateField(controller, "abilityLabel", "Ability");
        SetPrivateField(controller, "demoTestLabel", "DemoTest");
        InvokePrivate(controller, "NormalizeLegacyDefaultLabels");
        SetPrivateField(controller, "tutorialLabel", "教程");
        InvokePrivate(controller, "ConfigureMainMenuItems");

        Assert.AreEqual(5, gallery.items.Length);
        Assert.AreEqual("新游戏", gallery.items[0].levelName);
        Assert.AreEqual("继续游戏", gallery.items[1].levelName);
        Assert.AreEqual("教程", gallery.items[2].levelName);
        Assert.AreEqual("测试战斗", gallery.items[3].levelName);
        Assert.AreNotSame(demoItem.gameObject, gallery.items[2].gameObject);
        Assert.AreEqual("Ability", gallery.items[4].levelName);
    }

    [Test]
    public void ConfigureMainMenuItems_AppliesConfiguredMainMenuItemFontAndSize()
    {
        GameObject root = new GameObject("MainMenuRoot");
        MainMenuSceneController controller = root.AddComponent<MainMenuSceneController>();
        GalleryLevelSelectionManager gallery = root.AddComponent<GalleryLevelSelectionManager>();
        Font itemFont = new Font("CustomMainMenuItemFont");

        gallery.items = new GalleryLevelView[]
        {
            CreateMenuItem("Solo Play", root.transform),
            CreateMenuItem("Load", root.transform),
            CreateMenuItem("Demo", root.transform)
        };

        SetPrivateField(controller, "mainMenuGallery", gallery);
        SetPrivateField(controller, "mainMenuItemFont", itemFont);
        SetPrivateField(controller, "mainMenuItemFontSize", 64);

        InvokePrivate(controller, "ConfigureMainMenuItems");

        for (int i = 0; i < gallery.items.Length; i++)
        {
            Assert.IsNotNull(gallery.items[i].text);
            Assert.AreSame(itemFont, gallery.items[i].text.font);
            Assert.AreEqual(64, gallery.items[i].text.fontSize);
        }

        Object.DestroyImmediate(itemFont);
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
        item.text = itemObject.AddComponent<Text>();
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
