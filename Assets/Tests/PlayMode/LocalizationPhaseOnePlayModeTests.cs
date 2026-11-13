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
    private const string EncyclopediaSaveKey = "diceland.encyclopedia.v1";
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
    private bool hadEncyclopediaSave;
    private string encyclopediaSaveBefore;

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
        hadEncyclopediaSave = PlayerPrefs.HasKey(EncyclopediaSaveKey);
        encyclopediaSaveBefore = PlayerPrefs.GetString(EncyclopediaSaveKey);
        Set(RuntimeType("BattlePresentationContinueInputService"), "presentationInteractionLockCount", 0);
    }

    [UnityTearDown]
    public IEnumerator RestoreSettings()
    {
        Scene current = SceneManager.GetActiveScene();
        if (current.name == "Menu Selection" || current.name == "MapScene" || current.name == "SampleScene" || current.name == "RunSettlementScene" || current.name == "RestScene")
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
        Call(RuntimeType("RestSceneRuntime"), "Clear");
        Component curtain = Find("SceneCurtainTransitionController");
        if (curtain != null && curtain != curtainBefore) Object.Destroy(curtain.gameObject);
        Call(gameText, "Select", languageBefore, null);
        yield return WaitFor(() => !(bool)Get(gameText, "IsChanging"), "locale reset");
        if (settingsBefore == null) File.Delete(settingsPath);
        else File.WriteAllBytes(settingsPath, settingsBefore);
        Set(settingsStore, "cachedSettings", settingsSnapshot);
        RestoreProfile("menu_continue_save.json", saveBefore);
        RestoreProfile("ability_points.json", walletBefore);
        if (hadEncyclopediaSave) PlayerPrefs.SetString(EncyclopediaSaveKey, encyclopediaSaveBefore);
        else PlayerPrefs.DeleteKey(EncyclopediaSaveKey);
        PlayerPrefs.Save();
        Call(RuntimeType("EncyclopediaProgress"), "Reset");
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
        RawImage logo = (RawImage)Get(forest, "logo");
        Texture titleArt = logo.texture;
        Assert.IsNotNull(titleArt);
        yield return ChangeLanguage(forest, settings, 1, "en");
        Assert.IsTrue(logo.enabled, "The English wordmark artwork is shared by both languages.");
        Assert.AreSame(titleArt, logo.texture);
        Assert.IsFalse(((Text)Get(forest, "logoWordmark")).enabled);
        Assert.AreEqual("en", Get(Call(settingsStore, "Load"), "languageCode"));
        Assert.AreEqual("New Game", MenuButton(forest, "NewGame").transform.Find("Label").GetComponent<Text>().text);
        yield return ChangeLanguage(forest, settings, 0, "zh-Hans");
        Assert.IsTrue(logo.enabled);
        Assert.AreSame(titleArt, logo.texture);
        Assert.AreEqual("新游戏", MenuButton(forest, "NewGame").transform.Find("Label").GetComponent<Text>().text);
        yield return ChangeLanguage(forest, settings, 1, "en");

        AssertEnglishLabels("menu after changing language, including legacy menu items");
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
        Assert.IsFalse(battle.GetComponent<Image>().enabled, "English mode replaces the baked Chinese prepare artwork.");
        Assert.AreEqual("Prepare", Get(battle.transform.Find("LocalizedReadyButton/Label").GetComponent<Graphic>(), "text"));
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
        AssertEnglishReadyRaycast(battle);
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
        Assert.IsFalse(((Image)Get(settlement, "victoryIntroImage")).enabled, "English mode replaces the baked Chinese victory banner.");
        Assert.AreEqual("Victory", Label(settlement, "localizedVictoryIntro"));
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
        yield return VerifyEnglishSkillRules(skills, member);
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

    [UnityTest]
    public IEnumerator EnglishAuthoredContentAndSavedTutorialTagsStayLocalized()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English content tables");
        int checkedAssets = 0;
        foreach (string type in new[] { "CharacterData_SO", "EnemyData_SO", "EnemySkill_SO", "EnemyGroup_SO", "Weapon_SO",
            "LootItem_SO", "LootAttribute_SO", "Relic_SO", "CraftingBlueprint_SO", "BattleStatus_SO", "Calamity_SO", "SpecialDungeon_SO", "MapEvent_SO" })
        {
            string[] paths = AssetPaths(type);
            Assert.IsNotEmpty(paths, type);
            foreach (string path in paths)
            {
                object asset = LoadAsset(path, type);
                foreach (PropertyInfo property in asset.GetType().GetProperties(Members)
                    .Where(property => property.Name.StartsWith("Localized", StringComparison.Ordinal) && property.PropertyType == typeof(string)))
                {
                    string value = (string)property.GetValue(asset);
                    Assert.IsFalse(Regex.IsMatch(value ?? string.Empty, "[\u3400-\u9fff]"), path + "\n" + property.Name + "\n" + value);
                }
                checkedAssets++;
            }
        }
        object tile = Activator.CreateInstance(RuntimeType("BoardTileData"));
        Set(tile, "previewTag", "教程商店");
        Assert.AreEqual("Tutorial Shop", Call(RuntimeType("MapHUD"), "GetPreviewTag", tile));
        Assert.AreEqual("教程商店", Get(tile, "previewTag"));
        Call(gameText, "Select", "zh-Hans", null);
        yield return WaitFor(() => !(bool)Get(gameText, "IsChanging"), "Chinese content tables");
        Assert.AreEqual("教程商店", Call(RuntimeType("MapHUD"), "GetPreviewTag", tile));
        Debug.Log("English content checked for " + checkedAssets + " gameplay assets and legacy tutorial tags.");
    }

    [UnityTest]
    public IEnumerator EnglishDwarfBlacksmithRendersChoicesAndReforgesThroughTheBackpack()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English event table");
        Call(RuntimeType("MainMenuRuntime"), "ClearTutorialRequest");
        Call(RuntimeType("MainMenuRuntime"), "ClearContinueRequest");
        Call(RuntimeType("MapRunRuntime"), "Clear", (object)null);
        yield return SceneManager.LoadSceneAsync("MapScene");
        yield return WaitFor(() => Find("RunGameManager") != null && Get(Find("RunGameManager"), "runState") != null, "map initialization", 30f);
        Component map = Find("RunGameManager");
        object treasure = Get(map, "mapTreasureOverlay");
        if (treasure != null) Call(treasure, "Hide");
        Call(Get(map, "pendingStartingRelicBagChoices"), "Clear");

        object data = LoadAsset("Assets/Data/Events/034 矮人铁匠/034 矮人铁匠.asset", "MapEvent_SO");
        object[] options = ((IEnumerable)Call(data, "GetValidOptions")).Cast<object>().ToArray();
        string[] titles = { "Reforge a relic", "Reforge a blueprint", "Reforge a piece of loot" };
        for (int i = 0; i < options.Length; i++)
        {
            Assert.AreEqual(titles[i], Call(data, "LocalizeOption", options[i], "label", Get(options[i], "optionLabel")));
            StringAssert.Contains("same rarity", (string)Call(data, "LocalizeOption", options[i], "preview", Get(options[i], "consequenceDescription")));
            string result = (string)Call(data, "LocalizeOption", options[i], "result", Get(options[i], "resultContent"));
            Assert.IsNotEmpty(result);
            Assert.IsFalse(Regex.IsMatch(result, "[\u3400-\u9fff]"), titles[i]);
        }

        object run = Get(map, "runState");
        object blueprint = ((IEnumerable)Get(Get(options[1], "blueprintTable"), "Blueprints")).Cast<object>()
            .First(item => item != null && (bool)Call(RuntimeType("MapEventInventoryReforgeService"), "HasReplacement", options[1], item));
        ((IList)Get(run, "blueprintInventory")).Clear();
        Call(run, "AddBlueprint", blueprint, 1);
        object tile = ((IEnumerable)Call(Get(map, "boardManager"), "GetAllTiles")).Cast<object>().First(value => (bool)Get(value, "isSpawnTile"));
        Set(tile, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Event"));
        Set(tile, "assignedEvent", data);
        Set(tile, "isResolved", false);
        Assert.IsTrue((bool)Call(map, "OpenEventPanel", tile));
        object overlay = Get(map, "mapEventOverlay"), hud = Get(map, "mapHUD");
        Call(overlay, "CompleteNarration");
        yield return WaitFor(() => ((Button)Get(overlay, "confirmButton")).interactable, "blacksmith choices");
        Assert.AreEqual("The Dwarf Blacksmith", Label(overlay, "titleText"));
        StringAssert.Contains("Following the sound of a hammer", Label(overlay, "contentText"));

        Call(overlay, "SelectOption", options[1]);
        var confirm = (Button)Get(overlay, "confirmButton");
        CollectionAssert.Contains(ActiveLabels(), "Choose an item");
        confirm.onClick.Invoke();
        Assert.IsTrue((bool)Get(hud, "IsInventoryVisible"));
        var slot = ((IEnumerable)Get(hud, "inventorySlotObjects")).Cast<GameObject>().Single();
        slot.GetComponent<Button>().onClick.Invoke();
        object detail = Get(hud, "runtimeInventoryDetailView");
        var select = (Button)Get(detail, "actionButton");
        CollectionAssert.Contains(ActiveLabels(), "Select");
        select.onClick.Invoke();
        Assert.IsFalse((bool)Get(hud, "IsInventoryVisible"));
        Assert.AreEqual(1, Call(run, "GetBlueprintAmount", blueprint));
        StringAssert.Contains("Selected item: " + Get(blueprint, "LocalizedName"), Label(overlay, "consequenceText"));
        CollectionAssert.Contains(ActiveLabels(), "Confirm reforge");
        confirm.onClick.Invoke();
        Assert.IsTrue((bool)Get(tile, "isResolved"));
        Assert.AreEqual(0, Call(run, "GetBlueprintAmount", blueprint));
        StringAssert.Contains("the lines come to life", Label(overlay, "contentText"));
        Assert.IsFalse(Regex.IsMatch(Label(overlay, "contentText"), "[\u3400-\u9fff]"));
        Call(overlay, "Hide");
    }

    [UnityTest]
    public IEnumerator EnglishMistCollectorShowsTheDiscountAndGrantsBottledMist()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English event table");
        Call(RuntimeType("MainMenuRuntime"), "ClearTutorialRequest");
        Call(RuntimeType("MainMenuRuntime"), "ClearContinueRequest");
        Call(RuntimeType("MapRunRuntime"), "Clear", (object)null);
        yield return SceneManager.LoadSceneAsync("MapScene");
        yield return WaitFor(() => Find("RunGameManager") != null && Get(Find("RunGameManager"), "runState") != null, "map initialization", 30f);

        Component map = Find("RunGameManager");
        Call(Get(map, "mapTreasureOverlay"), "HideImmediately");
        Call(Get(map, "pendingStartingRelicBagChoices"), "Clear");
        object run = Get(map, "runState"), points = Get(map, "actionPointSystem");
        var mist = LoadAsset("Assets/Data/Relics/162 瓶中雾/瓶中雾.asset", "Relic_SO");
        var data = LoadAsset("Assets/Data/Events/035 收雾人/035 收雾人.asset", "MapEvent_SO");
        ((IList)Get(run, "relicInventory")).Clear();
        Call(run, "AddRelic", mist, 1, false);
        Call(points, "RestoreState", 6, 0, 6);
        object tile = ((IEnumerable)Call(Get(map, "boardManager"), "GetAllTiles")).Cast<object>().First(item => (bool)Get(item, "isSpawnTile"));
        Set(tile, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Event"));
        Set(tile, "assignedEvent", data);
        Set(tile, "isResolved", false);
        Assert.IsTrue((bool)Call(map, "OpenEventPanel", tile));
        object overlay = Get(map, "mapEventOverlay");
        Call(overlay, "CompleteNarration");
        var confirm = (Button)Get(overlay, "confirmButton");
        yield return WaitFor(() => confirm.interactable, "mist collector choices");

        Assert.AreEqual("The Mist Collector", Label(overlay, "titleText"));
        CollectionAssert.Contains(ActiveLabels(), "Help clear the fog");
        StringAssert.Contains("holding up a glass bottle", Label(overlay, "contentText"));
        StringAssert.Contains("AP loss for this choice: 2 → 1", Label(overlay, "consequenceText"));
        confirm.onClick.Invoke();
        Assert.AreEqual(5, Get(points, "CurrentPoints"));
        Assert.AreEqual(2, Call(run, "GetRelicAmount", mist));
        Assert.AreEqual("Bottled Mist", Get(mist, "LocalizedName"));
        StringAssert.Contains("the fog soon clears", Label(overlay, "contentText"));
        Assert.IsTrue((bool)Get(tile, "isResolved"));
        Call(overlay, "Hide");
    }

    [UnityTest]
    public IEnumerator EnglishWitheredCourtSkillDetailsIncludeLocalizedRules()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English skill table");
        var host = new GameObject("English Withered Court skill details");
        try
        {
            Component formatter = host.AddComponent(RuntimeType("BattleSkillInfoOverlay"));
            var paths = AssetPaths("EnemySkill_SO").Where(path => path.Contains("/044 WitheredCourt/")).ToArray();
            Assert.AreEqual(7, paths.Length);
            foreach (string path in paths)
            {
                object skill = LoadAsset(path, "EnemySkill_SO");
                string description = (string)Call(formatter, "DescribeSkillRules", skill, false);
                Assert.IsFalse(Regex.IsMatch(description, "[\u3400-\u9fff]"), path + "\n" + description);
                if (path.EndsWith("/Decomposition.asset")) StringAssert.Contains("Attack Die 2: [On Hit]", description);
                if (path.EndsWith("/Flourish.asset")) StringAssert.Contains("Temporary Strength", description);
                if (path.EndsWith("/Growth.asset")) StringAssert.Contains("Up to 4 copies of New Bud", description);
            }
        }
        finally { Object.Destroy(host); }
    }

    [UnityTest]
    public IEnumerator EnglishTutorialDefinitionsAndBlockingPromptsCoverEveryStage()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English tutorial tables");
        int checkedSteps = 0;
        foreach (object stage in Enum.GetValues(RuntimeType("TutorialBattleStageId")))
        {
            if (Convert.ToInt32(stage) == 0) continue;
            object definition = Call(RuntimeType("TutorialBattleContentFactory"), "CreateRuntimeDefinition", stage);
            Assert.IsNotNull(definition, stage.ToString());
            try
            {
                foreach (object step in (IEnumerable)Get(definition, "Steps"))
                {
                    string key = "tutorial." + Get(step, "stepId");
                    foreach (string field in new[] { "title", "body", "blockedMessage" })
                    {
                        string text = (string)Call(gameText, "Content", key, field == "blockedMessage" ? "blocked" : field, Get(step, field));
                        Assert.IsFalse(Regex.IsMatch(text ?? string.Empty, "[\u3400-\u9fff]"), stage + "\n" + key + "\n" + text);
                    }
                    checkedSteps++;
                }
                foreach (object reaction in (IEnumerable)Get(definition, "Reactions"))
                    foreach (string field in new[] { "title", "body" })
                    {
                        string text = (string)Call(gameText, "Content", "tutorial." + Get(reaction, "reactionId"), field, Get(reaction, field));
                        Assert.IsFalse(Regex.IsMatch(text ?? string.Empty, "[\u3400-\u9fff]"), stage + "\n" + text);
                    }
            }
            finally { Object.Destroy((Object)definition); }
        }
        Assert.Greater(checkedSteps, 20);
        Debug.Log("English tutorial text checked for seven stages and " + checkedSteps + " steps.");
    }

    [UnityTest]
    public IEnumerator EnglishMapEventsAndRestStationsRenderTheirAuthoredContent()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English locale");
        Call(RuntimeType("MainMenuRunSaveSystem"), "DeleteSavedRun");
        yield return SceneManager.LoadSceneAsync("Menu Selection");
        object forest = Get(Find("MainMenuSceneController"), "forestMenu");
        MenuButton(forest, "NewGame").onClick.Invoke();
        yield return WaitFor(() => !(bool)Get(forest, "IsPageTransitioning") && ((Button)Get(forest, "startButton")).IsInteractable(), "difficulty page");
        ((Button)Get(forest, "startButton")).onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "MapScene", "new run map", 75f);
        yield return WaitFor(TransitionFinished, "new run transition", 30f);
        Component map = Find("RunGameManager");
        object tile = ((IEnumerable)Call(Get(map, "boardManager"), "GetAllTiles")).Cast<object>().First(data => (bool)Get(data, "isSpawnTile"));
        object events = Get(map, "mapEventOverlay");
        string[] paths = AssetPaths("MapEvent_SO");
        Assert.IsNotEmpty(paths);
        foreach (string path in paths)
        {
            object data = LoadAsset(path, "MapEvent_SO");
            Call(events, "Show", tile, data, null, null);
            foreach (object option in (IEnumerable)Call(data, "GetValidOptions"))
            {
                Call(events, "SelectOption", option);
                AssertEnglishLabels(path);
            }
            if ((string)Get(data, "eventId") == "031_C_02")
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                StringAssert.Contains("second prince", Label(events, "contentText"));
            }
        }
        Call(events, "Hide");

        object dungeonEvent = paths.Select(path => LoadAsset(path, "MapEvent_SO"))
            .Single(data => (string)Get(data, "eventId") == "014");
        Set(tile, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Event"));
        Set(tile, "assignedEvent", dungeonEvent);
        Set(tile, "isResolved", false);
        Assert.IsTrue((bool)Call(map, "OpenEventPanel", tile));
        ((Button)Get(events, "confirmButton")).onClick.Invoke();
        ((Button)Get(events, "confirmButton")).onClick.Invoke();
        yield return WaitFor(() => Get(map, "activeSpecialDungeonContext") != null, "Whispering Well entrance");
        var dungeonTiles = ((IEnumerable)Call(Get(map, "boardManager"), "GetAllTiles")).Cast<object>().ToArray();
        foreach (object dungeonTile in dungeonTiles)
        {
            Call(Get(map, "mapHUD"), "ShowTile", dungeonTile, 0, true);
            AssertEnglishLabels("Whispering Well tile " + Get(dungeonTile, "coord"));
        }
        object exit = dungeonTiles.First(data => Get(data, "tileType").ToString() == "SpecialDungeonExit");
        Set(Get(map, "runState"), "playerGridPosition", Get(exit, "coord"));
        Call(map, "ExitSpecialDungeon");
        Assert.IsNull(Get(map, "activeSpecialDungeonContext"));
        tile = ((IEnumerable)Call(Get(map, "boardManager"), "GetAllTiles")).Cast<object>().First(data => (bool)Get(data, "isSpawnTile"));
        Set(tile, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Rest"));
        Set(tile, "isResolved", false);
        Assert.IsTrue((bool)Call(map, "TryEnterRestScene", tile));
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "RestScene", "rest scene", 75f);
        yield return WaitFor(TransitionFinished, "rest transition", 30f);

        Component rest = Find("RestSceneInteractable");
        Call(rest, "ShowPrompt");
        AssertEnglishLabels("healing prompt");
        ((Button)Get(rest, "cancelButton")).onClick.Invoke();
        Component points = Find("RestSceneActionPointRestoreInteractable");
        Call(points, "ShowPrompt");
        AssertEnglishLabels("AP restoration prompt");
        ((Button)Get(points, "cancelButton")).onClick.Invoke();
        Component recruit = Find("RestSceneRecruitInteractable");
        Call(recruit, "ShowPanel");
        AssertEnglishLabels("recruitment");
        ((Button)Get(recruit, "closeButton")).onClick.Invoke();
        Component reforge = Find("RestSceneReforgeInteractable");
        Call(reforge, "ShowOpenPrompt");
        AssertEnglishLabels("reforge prompt");
        ((Button)Get(reforge, "promptConfirmButton")).onClick.Invoke();
        AssertEnglishLabels("reforge materials");
        Call(reforge, "HideAllPanels");
        Call(rest, "ShowPrompt");
        ((Button)Get(rest, "confirmButton")).onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "MapScene", "return from rest", 75f);
        yield return WaitFor(TransitionFinished, "rest return transition", 30f);
        AssertEnglishLabels("map after rest");
        Debug.Log("Rendered English choices for " + paths.Length + " events and all four rest stations.");
    }

    [UnityTest]
    public IEnumerator EnglishNewRunDefeatsFinalBossAndReturnsFromVictorySummary()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English locale");
        Call(RuntimeType("MainMenuRunSaveSystem"), "DeleteSavedRun");
        yield return SceneManager.LoadSceneAsync("Menu Selection");
        Component menu = Find("MainMenuSceneController");
        object forest = Get(menu, "forestMenu");
        MenuButton(forest, "NewGame").onClick.Invoke();
        yield return WaitFor(() => !(bool)Get(forest, "IsPageTransitioning") && ((Button)Get(forest, "startButton")).IsInteractable(), "difficulty page");
        ((Button)Get(forest, "startButton")).onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "MapScene", "new run map", 75f);
        yield return WaitFor(TransitionFinished, "new run transition", 30f);

        Component map = Find("RunGameManager");
        object state = Get(map, "runState");
        object board = Get(map, "boardManager");
        object boss = ((IEnumerable)Call(board, "GetAllTiles")).Cast<object>().Single(tile => Get(tile, "tileType").ToString() == "Boss");
        Assert.IsFalse((bool)Call(map, "TryEnterBattle", boss), "Boss requires keys.");
        Set(state, "currentKeys", Get(state, "requiredKeys"));
        // Controlled HP and Strength make this a deterministic language/flow test while keeping actual combat and rewards.
        foreach (object member in ((IEnumerable)Get(state, "partyMembers")))
        {
            Set(member, "maxHP", 1000);
            Set(member, "preCalamityMaxHP", 1000);
            Set(member, "currentHP", 1000);
        }
        Assert.IsTrue((bool)Call(map, "TryEnterBattle", boss));
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "SampleScene", "final boss battle", 75f);
        yield return WaitFor(TransitionFinished, "boss transition", 30f);

        Component battle = Find("BattleButton");
        Component enemy = Find("BattleEnemyController");
        Component ally = Find("BattleAllyController");
        object encounter = Get(RuntimeType("BattleEncounterRuntime"), "ActiveEncounter");
        Assert.AreEqual("Boss", Get(encounter, "tileType").ToString());
        Assert.AreEqual(11, Get(enemy, "EnemyID"));
        object skills = Get(battle, "battleSkillInfoOverlay");
        Call(skills, "ShowEnemyDetail", enemy);
        Assert.IsFalse(Regex.IsMatch(Label(skills, "enemyProfileName"), "[\u3400-\u9fff]"));
        Call(skills, "Hide", false);
        yield return WaitFor(() => !(bool)Get(skills, "IsVisible"), "close boss profile");

        object strength = LoadAsset("Assets/Data/Battle/Strength/Strength.asset", "BattleStatus_SO");
        Assert.IsTrue((bool)Call(ally, "AddStatus", strength, null, null, 100));
        // Headless presentation invokes the same damage callbacks and settlement overlay without timed camera waits.
        Set(battle, "headlessExecution", true);
        object settlement = Get(battle, "battleSettlementOverlay");
        for (int round = 0; round < 5 && !(bool)Get(enemy, "IsDead"); round++)
        {
            Call(battle, "OnBattleButtonClicked");
            yield return WaitFor(() => Get(battle, "currentPhase").ToString() == "Targeting", "boss target selection");
            object allySpeed = FirstSpeedButton(ally), enemySpeed = FirstSpeedButton(enemy);
            Set(allySpeed, "isConnected", true);
            Set(allySpeed, "connectedButton", enemySpeed);
            Set(allySpeed, "absorbedEnemyButton", enemySpeed);
            Set(enemySpeed, "isConnected", true);
            Set(enemySpeed, "connectedButton", allySpeed);
            Call(battle, "OnBattleButtonClicked");
            yield return WaitFor(() => !(bool)Get(battle, "isResolvingCombat"), "boss combat resolution", 30f);
        }
        Assert.IsTrue((bool)Get(enemy, "IsDead"));
        Assert.IsTrue((bool)Get(encounter, "wasVictory"));
        yield return WaitFor(() => (bool)Get(settlement, "IsVisible"), "boss victory settlement", 15f);
        yield return WaitFor(() => ((RectTransform)Get(settlement, "panelRoot")).gameObject.activeInHierarchy, "boss reward panel", 15f);
        Assert.AreEqual("Victory", Label(settlement, "titleText"));
        Button finish = Object.FindObjectsOfType<Button>().Single(button => button.name == "ReturnButton" && button.gameObject.activeInHierarchy);
        Assert.AreEqual("Finish Run", Get(finish.transform.Find("Label").GetComponent<Graphic>(), "text"));
        finish.onClick.Invoke();

        yield return WaitFor(() => SceneManager.GetActiveScene().name == "RunSettlementScene", "victory run summary", 75f);
        yield return WaitFor(TransitionFinished, "victory summary transition", 30f);
        Assert.IsTrue((bool)Get(Get(RuntimeType("RunSettlementRuntime"), "Current"), "runCleared"));
        CollectionAssert.Contains(ActiveLabels(), "Final threat defeated");
        CollectionAssert.Contains(ActiveLabels(), "Run Summary");
        CollectionAssert.Contains(ActiveLabels(), "Total Score");
        foreach (string text in ActiveLabels())
            Assert.IsFalse(Regex.IsMatch(text, "[\u3400-\u9fff]"), "Victory summary\n" + text);
        Assert.IsFalse(File.Exists(ProfilePath("menu_continue_save.json")), "Completed run save was cleared.");
        Object.FindObjectsOfType<Button>().Single(button => button.name == "ReturnMainMenuButton").onClick.Invoke();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "Menu Selection", "return from victory summary", 75f);
        menu = Find("MainMenuSceneController");
        forest = Get(menu, "forestMenu");
        Assert.AreEqual("New Game", MenuButton(forest, "NewGame").transform.Find("Label").GetComponent<Text>().text);
        Assert.IsFalse(MenuButton(forest, "Continue").IsInteractable());
    }

    [UnityTest]
    public IEnumerator EnglishFormattedCountsPreserveColoredNumbersAndPluralForms()
    {
        Call(gameText, "Select", "en", null);
        yield return WaitFor(() => (string)Get(gameText, "LanguageCode") == "en" && !(bool)Get(gameText, "IsChanging"), "English plural rules");
        string key = "battle.skill.rules.grant_stacks_of_to";
        foreach (string count in new[] { "1", "2", "0", "1–2" })
        {
            string colored = "<color=#F47E73>" + count + "</color>";
            string text = (string)Call(gameText, "Format", key, string.Empty, new (string, object)[]
            {
                ("diceEffectLabel", "Attack Die 1"), ("triggerLabel", "[On Hit]"), ("conditionPrefix", ""),
                ("stackLabel", colored), ("statusLabel", "Burn"), ("targetLabel", "the enemy")
            });
            StringAssert.Contains(colored + (count == "1" ? " stack of Burn" : " stacks of Burn"), text);
            Assert.IsFalse(text.Contains("{stackLabel"), "Plural formatting must finish before keyword preprocessing.");
        }
        string timing = (string)Call(gameText, "Format", "battle.skill.rules.the_start_of_the_round_in_rounds", string.Empty,
            new (string, object)[] { ("max", 1) });
        Assert.AreEqual("the start of the round in 1 round", timing);
    }

    private static string[] AssetPaths(string type)
    {
        Type assets = RuntimeType("UnityEditor.AssetDatabase");
        string[] guids = (string[])assets.GetMethod("FindAssets", new[] { typeof(string), typeof(string[]) })
            .Invoke(null, new object[] { "t:" + type, new[] { "Assets/Data" } });
        return guids.Select(guid => (string)assets.GetMethod("GUIDToAssetPath", new[] { typeof(string) }).Invoke(null, new object[] { guid })).ToArray();
    }

    private static void AssertEnglishLabels(string context)
    {
        foreach (string text in ActiveLabels())
            Assert.IsFalse(Regex.IsMatch(Regex.Replace(text ?? string.Empty, "<[^>]+>", string.Empty), "[\u3400-\u9fff]"), context + "\n" + text);
    }

    private static IEnumerator VerifyEnglishSkillRules(object overlay, object partyMember)
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
            string compact = (string)Call(overlay, "BuildCompactSkillHoverDetail", skill, false);
            foreach (string description in new[] { detail, compact })
            {
                string plain = Regex.Replace(description, "<[^>]+>", string.Empty);
                Assert.IsFalse(Regex.IsMatch(plain, @"\b(?:enemy|ally|user|target)(?:next round|the start)|\brounds?apply", RegexOptions.IgnoreCase),
                    "Delayed status timing must remain separate from its target and action.\n" + path + "\n" + plain);
            }
        }

        object previewMember = Call(partyMember, "Clone");
        Set(previewMember, "weaponData", LoadAsset("Assets/Data/Weapons/007 烬冠剑/007 烬冠剑.asset", "Weapon_SO"));
        Component ally = Find("BattleAllyController");
        Call(ally, "ApplyRuntimeState", previewMember);
        object speed = FirstSpeedButton(ally);
        Assert.IsTrue((bool)Call(ally, "SetSelectedSkillIndexForButton", speed, 3, -1));
        Call(overlay, "ShowAllySkillSelection", ally, speed);
        yield return WaitFor(() => (float)Get(overlay, "openProgress") >= 1f, "English skill view entrance");
        ((ScrollRect)Get(overlay, "detailScrollRect")).verticalNormalizedPosition = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        Component clause = Object.FindObjectsOfType(RuntimeType("TMPro.TMP_Text")).OfType<Component>()
            .First(label => label.name == "DiceClauses" && (bool)Get(label, "isActiveAndEnabled") && ((string)Get(label, "text")).Contains("Burn"));
        string markup = (string)Call(Get(clause, "textPreprocessor"), "PreprocessText", Get(clause, "text"));
        StringAssert.Contains("<sprite name=\"DL_Status_Burn\"", markup);
        yield return WaitFor(() => !((Graphic)clause).canvasRenderer.cull, "English Burn clause scrolled into view");
        Call(clause, "ForceMeshUpdate", true, true);
        object textInfo = Get(clause, "textInfo");
        int characterCount = (int)Get(textInfo, "characterCount");
        int statusSprites = ((IEnumerable)Get(textInfo, "characterInfo")).Cast<object>().Take(characterCount)
            .Count(character => Get(character, "elementType").ToString() == "Sprite"
                && ((Object)Get(character, "spriteAsset"))?.name == "DiceLandBattleStatusKeywords");
        Assert.Greater(statusSprites, 0, "English Burn glyphs. Characters=" + characterCount + ", richText=" + Get(clause, "richText")
            + ", spriteAsset=" + ((Object)Get(clause, "spriteAsset"))?.name + "\n" + markup);
    }

    private static string[] ActiveLabels()
    {
        Scene scene = SceneManager.GetActiveScene();
        return Object.FindObjectsOfType<Graphic>().Where(label => label.gameObject.scene == scene && label.isActiveAndEnabled)
            .Where(label => label is Text || label.GetType().Name == "TextMeshProUGUI")
            .Select(label => (string)Get(label, "text")).ToArray();
    }

    private static void AssertEnglishReadyRaycast(Component battle)
    {
        var face = (RectTransform)battle.transform.Find("LocalizedReadyButton");
        var canvas = battle.GetComponentInParent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(camera, face.TransformPoint(face.rect.center))
        };
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
        Assert.IsTrue(hits.Any(hit => hit.gameObject.transform == face), "The English button face must remain an input target.");
    }

    [UnityTest]
    public IEnumerator LuckyMazeUsesRoomChoicesWithoutConsequencesOrRepeatedRewards()
    {
        Call(RuntimeType("MainMenuRuntime"), "ClearTutorialRequest");
        Call(RuntimeType("MainMenuRuntime"), "ClearContinueRequest");
        Call(RuntimeType("MapRunRuntime"), "Clear", (object)null);
        yield return SceneManager.LoadSceneAsync("MapScene");
        Component map = Find("RunGameManager");
        yield return WaitFor(() => map != null && Get(map, "runState") != null, "map initialization", 30f);
        Call(map, "StartRun");
        object treasure = Get(map, "mapTreasureOverlay");
        if (treasure != null) Call(treasure, "Hide");
        Call(Get(map, "pendingStartingRelicBagChoices"), "Clear");
        object dungeon = LoadAsset("Assets/Data/Events/033 幸运游戏（其二）/033 幸运迷宫.asset", "SpecialDungeon_SO");
        object flow = Get(map, "runFlowState");
        flow.GetType().GetProperty("PendingSpecialDungeon").SetValue(flow, dungeon);
        object state = Get(map, "runState"); Vector2Int mainCoord = (Vector2Int)Get(state, "playerGridPosition");
        Set(state, "money", 1000);
        object member = ((IEnumerable)Get(state, "partyMembers")).Cast<object>().First();
        Set(member, "maxHP", 100); Set(member, "currentHP", 20);
        Call(map, "EnterPendingSpecialDungeon");
        object board = Get(map, "boardManager"), overlay = Get(map, "mapEventOverlay");
        Func<int, object> room = index => ((IEnumerable)Call(board, "GetAllTiles")).Cast<object>()
            .Single(tile => (int)Get(tile, "specialDungeonEntryIndex") == index);
        Assert.IsFalse((bool)Call(map, "TryExecuteMove", room(1), 0));
        int ap = (int)Get(Get(map, "actionPointSystem"), "CurrentPoints");
        Assert.IsTrue((bool)Call(map, "TryExecuteMove", room(0), 0));
        yield return WaitFor(() => (bool)Get(overlay, "IsVisible"), "first maze room", 15f);
        Assert.AreEqual("033_A_00", Get(Get(overlay, "currentEvent"), "eventId"));
        Assert.IsFalse(((Component)Get(overlay, "consequenceTitleText")).gameObject.activeSelf);
        Action<int> choose = index =>
        {
            object current = Get(overlay, "currentEvent");
            object option = ((IEnumerable)Get(current, "options")).Cast<object>().ElementAt(index);
            Call(overlay, "SelectOption", option);
            Call(overlay, "OnConfirmClicked");
            Assert.IsTrue((bool)Get(overlay, "isShowingResultStep"));
            Assert.IsFalse(((Component)Get(overlay, "consequenceScrollRect")).gameObject.activeSelf);
            Call(overlay, "OnConfirmClicked");
        };
        choose(2); Assert.AreEqual(100, Get(member, "currentHP"));
        Set(member, "currentHP", 45);
        for (int i = 0; i < 18; i++)
        {
            choose(2);
            Assert.AreEqual("033_A_00", Get(Get(overlay, "currentEvent"), "eventId"));
            Assert.AreEqual(45, Get(member, "currentHP"));
        }
        Assert.IsFalse((bool)Call(map, "TryExecuteMove", room(1), 0), "Free movement stays locked after the first event.");
        choose(0); Assert.AreEqual("033_A_01", Get(Get(overlay, "currentEvent"), "eventId"));
        choose(2); Assert.AreEqual("033_B_02", Get(Get(overlay, "currentEvent"), "eventId"));
        choose(0); Assert.AreEqual("033_C_02", Get(Get(overlay, "currentEvent"), "eventId"));
        choose(1); Assert.AreEqual("033_A_00", Get(Get(overlay, "currentEvent"), "eventId"));
        choose(0); choose(2);
        int beforeRepeat = (int)Get(state, "money");
        choose(0);
        Assert.AreEqual(beforeRepeat, Get(state, "money"), "Returning to a room must not award its option twice.");
        StringAssert.Contains("033_B_02::1", File.ReadAllText(ProfilePath("menu_continue_save.json")));
        choose(2); Assert.AreEqual("033_D_02", Get(Get(overlay, "currentEvent"), "eventId"));
        choose(1); Assert.AreEqual("033_E_01", Get(Get(overlay, "currentEvent"), "eventId"));
        Assert.AreEqual(ap, Get(Get(map, "actionPointSystem"), "CurrentPoints"));
        choose(1);
        Assert.IsFalse((bool)Call(map, "IsInSpecialDungeon"));
        Assert.AreEqual(mainCoord, Get(Get(map, "runState"), "playerGridPosition"));
        Assert.IsFalse((bool)Get(overlay, "IsVisible"));
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
