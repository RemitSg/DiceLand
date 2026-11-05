#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class LocalizationPhaseOnePlayModeTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private readonly Type gameText = RuntimeType("GameText");
    private readonly Type settingsStore = RuntimeType("MainMenuSettingsPersistence");
    private string settingsPath;
    private byte[] settingsBefore;
    private byte[] saveBefore;
    private byte[] walletBefore;
    private object settingsSnapshot;
    private float timeScaleBefore;
    private string languageBefore;
    private Component curtainBefore;

    [SetUp]
    public void RememberSettings()
    {
        settingsPath = Path.Combine(Application.persistentDataPath, "main_menu_settings.json");
        settingsBefore = File.Exists(settingsPath) ? File.ReadAllBytes(settingsPath) : null;
        settingsSnapshot = Get(settingsStore, "Current");
        saveBefore = ReadProfile("menu_continue_save.json");
        walletBefore = ReadProfile("ability_points.json");
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 1f;
        languageBefore = (string)Get(gameText, "LanguageCode");
        curtainBefore = Find("SceneCurtainTransitionController");
        Set(RuntimeType("BattlePresentationContinueInputService"), "presentationInteractionLockCount", 0);
    }

    [UnityTearDown]
    public IEnumerator RestoreSettings()
    {
        Scene current = SceneManager.GetActiveScene();
        if (current.name == "Menu Selection" || current.name == "MapScene" || current.name == "SampleScene" || current.name == "RunSettlementScene")
        {
            Scene empty = SceneManager.CreateScene("Localization Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(current);
        }
        Call(RuntimeType("TutorialBattleRuntime"), "Clear");
        Call(RuntimeType("BattleEncounterRuntime"), "Clear");
        Call(RuntimeType("MapRunRuntime"), "Clear", (object)null);
        Call(RuntimeType("MainMenuRuntime"), "ClearTutorialRequest");
        Call(RuntimeType("MainMenuRuntime"), "ClearContinueRequest");
        Call(RuntimeType("RunSettlementRuntime"), "Clear");
        Component curtain = Find("SceneCurtainTransitionController");
        if (curtain != null && curtain != curtainBefore) Object.Destroy(curtain.gameObject);
        Call(gameText, "Select", languageBefore, null);
        yield return WaitFor(() => !(bool)Get(gameText, "IsChanging"), "locale reset");
        if (settingsBefore == null) File.Delete(settingsPath);
        else File.WriteAllBytes(settingsPath, settingsBefore);
        Set(settingsStore, "cachedSettings", settingsSnapshot);
        RestoreProfile("menu_continue_save.json", saveBefore);
        RestoreProfile("ability_points.json", walletBefore);
        Time.timeScale = timeScaleBefore;
    }

    [UnityTest]
    public IEnumerator EnglishSettingsReachTutorialMudPitAndFirstBattleVictory()
    {
        Call(gameText, "Select", "zh-Hans", null);
        yield return WaitFor(() => !(bool)Get(gameText, "IsChanging"), "Chinese locale");
        yield return SceneManager.LoadSceneAsync("Menu Selection");
        Component menu = Find("MainMenuSceneController");
        object forest = Get(menu, "forestMenu");
        object settings = Get(menu, "settingsOverlay");
        yield return ChangeLanguage(forest, settings, 1, "en");
        Assert.AreEqual("en", Get(Call(settingsStore, "Load"), "languageCode"));
        Assert.AreEqual("New Game", MenuButton(forest, "NewGame").transform.Find("Label").GetComponent<Text>().text);
        yield return ChangeLanguage(forest, settings, 0, "zh-Hans");
        Assert.AreEqual("新游戏", MenuButton(forest, "NewGame").transform.Find("Label").GetComponent<Text>().text);
        yield return ChangeLanguage(forest, settings, 1, "en");

        yield return WaitFor(() => !(bool)Get(forest, "IsPageTransitioning"), "menu return");
        MenuButton(forest, "Tutorial").onClick.Invoke();
        object tutorial = Get(menu, "tutorialOverlay");
        yield return WaitFor(() => (bool)Get(tutorial, "IsVisible"), "tutorial launcher");
        Assert.AreEqual("Start the Tutorial?", ((Text)Get(tutorial, "summaryTitleText")).text);
        Button start = (Button)Get(tutorial, "startButton");
        yield return WaitFor(start.IsInteractable, "tutorial start button");
        start.onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "MapScene", "tutorial map", 75f);
        yield return WaitFor(TransitionFinished, "map transition", 20f);

        Component map = Find("RunGameManager");
        object mapGuide = Get(map, "mapTutorialGuideOverlay");
        yield return WaitFor(() => (bool)Get(mapGuide, "IsModalVisible"), "map introduction");
        Assert.AreEqual("Welcome to DiceLand", ((Text)Get(mapGuide, "titleText")).text);
        Call(mapGuide, "Hide");
        yield return WaitFor(() => !(bool)Get(mapGuide, "IsVisible"), "closing map introduction");

        var tiles = ((IEnumerable)Call(Get(map, "boardManager"), "GetAllTiles")).Cast<object>().ToArray();
        object eventTile = tiles.First(tile => ((Vector2Int)Get(tile, "coord")).x == 0);
        object firstBattle = tiles.First(tile => ((Vector2Int)Get(tile, "coord")).x == 1);
        object mud = LoadAsset("Assets/Data/Events/001 泥潭/泥潭.asset", "MapEvent_SO");
        Set(eventTile, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Event"));
        Set(eventTile, "assignedEvent", mud);
        Set(eventTile, "isResolved", false);
        object points = Get(map, "actionPointSystem");
        int startingPoints = (int)Get(points, "CurrentPoints");
        Assert.IsTrue((bool)Call(map, "OpenEventPanel", eventTile));
        object eventPanel = Get(map, "mapEventOverlay");
        Assert.AreEqual("Mud Pit", Label(eventPanel, "titleText"));
        StringAssert.Contains("Lose 2", Label(eventPanel, "consequenceText"));
        Button confirm = (Button)Get(eventPanel, "confirmButton");
        confirm.onClick.Invoke();
        StringAssert.Contains("You struggle", Label(eventPanel, "contentText"));
        StringAssert.Contains("Lost 2", Label(eventPanel, "consequenceText"));
        Assert.AreEqual(startingPoints - 2, Get(points, "CurrentPoints"));
        confirm.onClick.Invoke();

        Set(eventTile, "isResolved", false);
        Set(eventTile, "assignedEvent", mud);
        Assert.IsTrue((bool)Call(map, "OpenEventPanel", eventTile));
        Assert.AreEqual("Mud Pit", Label(eventPanel, "titleText"));
        object secondChoice = ((IEnumerable)Get(mud, "options")).Cast<object>().ElementAt(1);
        Call(eventPanel, "SelectOption", secondChoice);
        StringAssert.Contains("15%", Label(eventPanel, "consequenceText"));
        confirm.onClick.Invoke();
        StringAssert.Contains("You let yourself sink", Label(eventPanel, "contentText"));
        StringAssert.Contains("Recovered 1", Label(eventPanel, "consequenceText"));
        Assert.AreEqual(startingPoints - 1, Get(points, "CurrentPoints"));
        confirm.onClick.Invoke();

        Assert.IsTrue((bool)Call(map, "TryEnterBattle", firstBattle));
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "SampleScene", "first tutorial battle", 75f);
        yield return WaitFor(TransitionFinished, "battle transition", 20f);
        Component battle = Find("BattleButton");
        object guide = Get(battle, "tutorialBattleGuideOverlay");
        Call(battle, "TryShowPendingTutorialStep");
        yield return WaitFor(() => (bool)Get(guide, "IsModalVisible"), "battle introduction");
        Assert.AreEqual("Your First Battle", Label(Get(guide, "Presentation"), "Title"));
        Call(guide, "Hide", false);
        yield return WaitFor(() => !(bool)Get(guide, "IsVisible"), "closing battle guide");

        Component enemy = Find("BattleEnemyController");
        Component ally = Find("BattleAllyController");
        object skills = Get(battle, "battleSkillInfoOverlay");
        Call(skills, "ShowEnemyDetail", enemy);
        Assert.AreEqual("Training Rat", Label(skills, "enemyProfileName"));
        Call(skills, "Hide", false);
        // Combat below uses the real tutorial encounter and attack data, with click waits bypassed for batch input.
        Call(RuntimeType("TutorialBattleRuntime"), "Clear");
        Call(guide, "Hide", false);
        yield return WaitFor(() => !(bool)Get(skills, "IsVisible") && !(bool)Get(guide, "IsVisible"), "battle controls");

        // The scripted combat starts after the tutorial modal and its input gate have been cleared.
        Time.timeScale = 1f;
        Assert.IsFalse((bool)Get(RuntimeType("TutorialBattleRuntime"), "IsActive"));
        Assert.IsFalse((bool)Get(RuntimeType("BattlePauseMenuOverlay"), "IsOpen"));
        Assert.AreEqual(2, Convert.ToInt32(Get(Get(settingsStore, "Current"), "battleAdvanceMode")));
        object settlement = Get(battle, "battleSettlementOverlay");
        for (int round = 0; round < 5 && !(bool)Get(enemy, "IsDead"); round++)
        {
            Call(battle, "OnBattleButtonClicked");
            yield return WaitFor(() => Get(battle, "currentPhase").ToString() == "Targeting", "target selection");
            object allySpeed = FirstSpeedButton(ally), enemySpeed = FirstSpeedButton(enemy);
            if (round == 0)
            {
                Call(skills, "ShowAllySkillSelection", ally, allySpeed);
                Assert.AreEqual("Heavy Strike", Label(skills, "skillNameText"));
                Call(skills, "Hide", false);
                yield return WaitFor(() => !(bool)Get(skills, "IsVisible"), "closing skill details");
            }
            Set(allySpeed, "isConnected", true);
            Set(allySpeed, "connectedButton", enemySpeed);
            Set(allySpeed, "absorbedEnemyButton", enemySpeed);
            Set(enemySpeed, "isConnected", true);
            Set(enemySpeed, "connectedButton", allySpeed);
            Call(battle, "OnBattleButtonClicked");
            yield return WaitFor(() => !(bool)Get(battle, "isResolvingCombat"), "combat resolution", 40f);
        }

        Assert.IsTrue((bool)Get(enemy, "IsDead"));
        yield return WaitFor(() => (bool)Get(settlement, "IsVisible"), "victory settlement", 12f);
        Assert.AreEqual("Victory", Label(settlement, "titleText"));
        StringAssert.Contains("Gold", Label(settlement, "moneyRewardText"));
        StringAssert.Contains("XP", Label(settlement, "expText"));
    }

    [UnityTest]
    public IEnumerator EnglishNewRunContinuesLegacyIdsAndReachesRunSummary()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English locale");
        Call(RuntimeType("MainMenuRunSaveSystem"), "DeleteSavedRun");
        yield return SceneManager.LoadSceneAsync("Menu Selection");
        Component menu = Find("MainMenuSceneController");
        object forest = Get(menu, "forestMenu");
        MenuButton(forest, "NewGame").onClick.Invoke();
        yield return WaitFor(() => !((bool)Get(forest, "IsPageTransitioning")) && ((Button)Get(forest, "startButton")).IsInteractable(), "difficulty page");
        CollectionAssert.Contains(ActiveLabels(), "Start Adventure");
        ((Button)Get(forest, "startButton")).onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "MapScene", "new run map", 75f);
        yield return WaitFor(TransitionFinished, "new run transition", 30f);

        Component map = Find("RunGameManager");
        object state = Get(map, "runState");
        object board = Get(map, "boardManager");
        Vector2Int start = (Vector2Int)Get(board, "SpawnCoord");
        var tiles = ((IEnumerable)Call(board, "GetAllTiles")).Cast<object>().ToArray();
        object next = tiles.First(tile => ((Vector2Int)Get(tile, "coord") - start).sqrMagnitude == 1);
        object loot = LoadAsset("Assets/Data/Loots/LootItems/000 史莱姆粘液/SlimeMucus.asset", "LootItem_SO");
        object group = LoadAsset("Assets/Data/Enemies/EnemyGroup/Outer/Danger1/GreenSlime x2.asset", "EnemyGroup_SO");
        Call(state, "AddLoot", loot, 2);
        Set(next, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Battle"));
        Set(next, "isResolved", false);
        Set(next, "assignedEnemyGroup", group);
        Call(map, "PersistCurrentRunSave");

        string runPath = ProfilePath("menu_continue_save.json");
        string saved = File.ReadAllText(runPath);
        StringAssert.Contains("\"character.chara\"", saved);
        StringAssert.Contains("\"weapon.wooden_sword\"", saved);
        StringAssert.Contains("\"loot.slime_mucus\"", saved);
        StringAssert.Contains("\"enemy_group.green_slime_pair\"", saved);
        File.WriteAllText(runPath, saved.Replace("\"character.chara\"", "\"Chara\"")
            .Replace("\"weapon.wooden_sword\"", "\"新手木剑\"")
            .Replace("\"loot.slime_mucus\"", "\"史莱姆粘液\"")
            .Replace("\"enemy_group.green_slime_pair\"", "\"GreenSlime x2\""));
        Vector2Int nextCoord = (Vector2Int)Get(next, "coord");

        Call(RuntimeType("MapRunRuntime"), "Clear", "MapScene");
        yield return SceneManager.LoadSceneAsync("Menu Selection");
        menu = Find("MainMenuSceneController");
        forest = Get(menu, "forestMenu");
        Button continueGame = MenuButton(forest, "Continue");
        Assert.IsTrue(continueGame.IsInteractable());
        continueGame.onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "MapScene", "continued map", 75f);
        yield return WaitFor(TransitionFinished, "continue transition", 30f);

        map = Find("RunGameManager");
        state = Get(map, "runState");
        board = Get(map, "boardManager");
        var member = ((IEnumerable)Get(state, "partyMembers")).Cast<object>().First();
        Assert.AreEqual("Wooden Sword", Get(Get(member, "weaponData"), "LocalizedName"));
        Assert.AreEqual(2, Call(state, "GetLootAmount", loot));
        next = ((IEnumerable)Call(board, "GetAllTiles")).Cast<object>().First(tile => (Vector2Int)Get(tile, "coord") == nextCoord);
        Assert.AreSame(group, Get(next, "assignedEnemyGroup"));

        Assert.IsTrue((bool)Call(map, "OpenMerchantPanel", next));
        object merchant = Get(map, "mapMerchantOverlay");
        Assert.IsTrue((bool)Get(merchant, "IsVisible"));
        CollectionAssert.Contains(ActiveLabels(), "Stock");
        Call(merchant, "Hide");
        yield return WaitFor(() => !(bool)Get(merchant, "IsVisible"), "leave shop");
        Call(map, "OpenForgePanel", next);
        object forge = Get(map, "mapCraftingOverlay");
        Assert.IsTrue((bool)Get(forge, "IsVisible"));
        CollectionAssert.Contains(ActiveLabels(), "Weapon Preview");
        Call(forge, "Hide");
        yield return WaitFor(() => !(bool)Get(forge, "IsVisible"), "leave forge");

        Assert.IsTrue((bool)Call(map, "TryEnterBattle", next));
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "SampleScene", "normal battle", 75f);
        yield return WaitFor(TransitionFinished, "normal battle transition", 30f);
        Component battle = Find("BattleButton");
        object skills = Get(battle, "battleSkillInfoOverlay");
        Call(skills, "ShowEnemyDetail", Find("BattleEnemyController"));
        Assert.AreEqual("Green Slime", Label(skills, "enemyProfileName"));
        VerifyEnglishSkillRules(skills);
        Call(skills, "Hide", false);
        yield return WaitFor(() => !(bool)Get(skills, "IsVisible"), "close enemy detail");

        Component pause = Find("BattlePauseMenuOverlay");
        Call(pause, "Show");
        Canvas canvas = (Canvas)Get(pause, "canvas");
        Button abandon = canvas.GetComponentsInChildren<Button>(true).Single(button => button.name == "AbandonButton");
        Assert.AreEqual("Abandon Battle", abandon.GetComponentInChildren<Text>().text);
        abandon.onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "RunSettlementScene", "run summary", 75f);
        yield return WaitFor(TransitionFinished, "run summary transition", 30f);
        CollectionAssert.Contains(ActiveLabels(), "Run Summary");
        CollectionAssert.Contains(ActiveLabels(), "Total Score");
        CollectionAssert.Contains(ActiveLabels(), "The final threat remains");
    }

    private static void VerifyEnglishSkillRules(object overlay)
    {
        Type assets = RuntimeType("UnityEditor.AssetDatabase");
        var find = assets.GetMethod("FindAssets", new[] { typeof(string), typeof(string[]) });
        var getPath = assets.GetMethod("GUIDToAssetPath", new[] { typeof(string) });
        var guids = (string[])find.Invoke(null, new object[] { "t:EnemySkill_SO", new[] { "Assets/Data" } });
        Assert.IsNotEmpty(guids);
        foreach (string guid in guids)
        {
            string path = (string)getPath.Invoke(null, new object[] { guid });
            object skill = LoadAsset(path, "EnemySkill_SO");
            string detail = (string)Call(overlay, "DescribeSkillRules", skill, false);
            Assert.IsFalse(Regex.IsMatch(detail, "[\u3400-\u9fff]"), path + "\n" + detail);
        }
    }

    private static string[] ActiveLabels()
    {
        Scene scene = SceneManager.GetActiveScene();
        return Object.FindObjectsOfType<Graphic>().Where(label => label.gameObject.scene == scene && label.isActiveAndEnabled)
            .Where(label => label is Text || label.GetType().Name == "TextMeshProUGUI")
            .Select(label => (string)Get(label, "text")).ToArray();
    }

    private static string ProfilePath(string name) => Path.Combine(Application.persistentDataPath, name);
    private static byte[] ReadProfile(string name) => File.Exists(ProfilePath(name)) ? File.ReadAllBytes(ProfilePath(name)) : null;
    private static void RestoreProfile(string name, byte[] contents)
    {
        if (contents == null) File.Delete(ProfilePath(name));
        else File.WriteAllBytes(ProfilePath(name), contents);
    }

    private IEnumerator ChangeLanguage(object forest, object settings, int value, string code)
    {
        MenuButton(forest, "Settings").onClick.Invoke();
        yield return WaitFor(() => (bool)Get(settings, "IsVisible"), "settings panel");
        yield return WaitFor(() => !Moving(settings), "settings entrance");
        ((Dropdown)Get(settings, "languageDropdown")).value = value;
        ((Dropdown)Get(settings, "battleAdvanceDropdown")).value = 2;
        ((Button)Get(settings, "saveButton")).onClick.Invoke();
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == code
            && !(bool)Get(settings, "IsVisible"), "saved language " + code);
        yield return null;
    }

    private static bool Moving(object settings)
    {
        var canvas = (Canvas)Get(settings, "PresentationCanvas");
        Component motion = canvas.GetComponent(RuntimeType("MainMenuPageMotion"));
        return motion != null && (bool)Get(motion, "IsTransitioning");
    }

    private static bool TransitionFinished()
    {
        Component curtain = Find("SceneCurtainTransitionController");
        return curtain == null || !(bool)Get(curtain, "IsTransitioning");
    }

    private static object FirstSpeedButton(object actor) => ((IEnumerable)Get(actor, "SpeedButtons")).Cast<object>().First();
    private static Button MenuButton(object forest, string action) => (Button)Call(forest, "GetButton", Enum.Parse(RuntimeType("MainMenuSceneController+MenuAction"), action));
    private static string Label(object owner, string field) => (string)Get(Get(owner, field), "text");
    private static Component Find(string type)
    {
        Type componentType = RuntimeType(type);
        if (type == "SceneCurtainTransitionController") return (Component)Object.FindObjectOfType(componentType);
        Scene scene = SceneManager.GetActiveScene();
        return Object.FindObjectsOfType(componentType).OfType<Component>()
            .FirstOrDefault(component => component.gameObject.scene == scene);
    }
    private static object LoadAsset(string path, string type) => RuntimeType("UnityEditor.AssetDatabase")
        .GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { path, RuntimeType(type) });
    private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).First(type => type != null);

    private static object Get(object target, string name)
    {
        Type type = target as Type ?? target.GetType();
        object instance = target is Type ? null : target;
        FieldInfo field = type.GetField(name, Members);
        return field != null ? field.GetValue(instance) : type.GetProperty(name, Members).GetValue(instance);
    }

    private static void Set(object target, string name, object value)
    {
        Type type = target as Type ?? target.GetType();
        type.GetField(name, Members).SetValue(target is Type ? null : target, value);
    }

    private static object Call(object target, string name, params object[] arguments)
    {
        Type type = target as Type ?? target.GetType();
        MethodInfo method = type.GetMethods(Members).Single(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        return method.Invoke(target is Type ? null : target, arguments);
    }

    private static IEnumerator WaitFor(Func<bool> predicate, string step, float seconds = 6f)
    {
        float timeout = Time.realtimeSinceStartup + seconds;
        while (!predicate() && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.IsTrue(predicate(), step);
    }
}
#endif
