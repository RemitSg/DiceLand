using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal sealed class BattleFlowPlayModeFixture
{
    private const string RuntimeAssemblyName = "Assembly-CSharp";

    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

    public object BattleManager { get; private set; }
    public object BattleButton { get; private set; }
    public object Ally { get; private set; }
    public object Enemy { get; private set; }
    public object AllyButton { get; private set; }
    public object EnemyButton { get; private set; }

    public void BuildOneOnOneBattle(int allyPower, int enemyPower, int enemyHp, int allyHp = 30)
    {
        ClearStaticRuntimeState();

        GameObject managerObject = CreateGameObject("PlayMode BattleManager");
        BattleManager = AddRuntimeComponent(managerObject, "BattleManager");

        GameObject alliesParent = CreateGameObject("PlayMode Allies");
        GameObject enemiesParent = CreateGameObject("PlayMode Enemies");
        SetField(BattleManager, "alliesParent", alliesParent);
        SetField(BattleManager, "enemiesParent", enemiesParent);
        Invoke(BattleManager, "Initialize");

        object allySkill = CreateSkill("PlayMode Ally Strike", allyPower, allyPower);
        object enemySkill = CreateSkill("PlayMode Enemy Strike", enemyPower, enemyPower);
        Ally = CreateAlly(alliesParent.transform, "PlayMode Ally", 5, allyHp, allySkill);
        Enemy = CreateEnemy(enemiesParent.transform, "PlayMode Enemy", 3, enemyHp, enemySkill);

        GameObject buttonObject = CreateGameObject("PlayMode BattleButton");
        BattleButton = AddRuntimeComponent(buttonObject, "BattleButton");
        SetField(BattleButton, "battleManager", BattleManager);
        SetField(BattleButton, "speedMin", 5);
        SetField(BattleButton, "speedMax", 5);
    }

    public void BuildEliteBattleWithRelicChoices(int allyPower, int enemyPower, int enemyHp)
    {
        BuildOneOnOneBattle(allyPower, enemyPower, enemyHp);
        object encounter = Activator.CreateInstance(RuntimeType("BattleEncounterData"));
        SetField(encounter, "sourceSceneName", "PlayModeMap");
        SetField(encounter, "targetSceneName", "PlayModeBattle");
        SetField(encounter, "encounterLabel", "PlayMode Elite");
        SetField(encounter, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Elite"));
        SetField(encounter, "enemyGroup", CreateEnemyGroupForEncounter());
        SetField(encounter, "eliteRelicChoiceTable", CreateRelicChoiceTable());
        SetField(encounter, "seed", 12345);
        SetField(encounter, "moneyGained", 0);

        InvokeStatic("BattleEncounterRuntime", "SetEncounter", encounter);
        InvokeStatic("BattleEncounterRuntime", "ConsumeEncounter");
    }

    public void BuildBattleWithRetryEncounter(int allyPower, int enemyPower, int enemyHp, int allyHp)
    {
        BuildOneOnOneBattle(allyPower, enemyPower, enemyHp, allyHp);
        object encounter = Activator.CreateInstance(RuntimeType("BattleEncounterData"));
        string activeSceneName = SceneManager.GetActiveScene().name;
        SetField(encounter, "sourceSceneName", activeSceneName);
        SetField(encounter, "targetSceneName", activeSceneName);
        SetField(encounter, "encounterLabel", "PlayMode Retry Battle");
        SetField(encounter, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Battle"));
        SetField(encounter, "enemyGroup", CreateEnemyGroupForEncounter());
        SetField(encounter, "seed", 23456);
        SetField(encounter, "moneyGained", 0);

        InvokeStatic("BattleEncounterRuntime", "SetEncounter", encounter);
        InvokeStatic("BattleEncounterRuntime", "ConsumeEncounter");
    }

    public void BuildBattleWithEnemyRoundStartBurn(int allyPower, int enemyPower, int enemyHp, int burnStacks)
    {
        BuildOneOnOneBattle(allyPower, enemyPower, enemyHp);
        object burnStatus = CreateDamageStatus("playmode_burn", "PlayMode Burn", damagePerStack: 1);
        Invoke(Enemy, "AddStatus", burnStatus, "burn", "PlayMode Burn", burnStacks);
    }

    public void BuildBattleWithEnemyRoundStartRetreat(int allyPower, int enemyPower, int enemyHp)
    {
        BuildOneOnOneBattle(allyPower, enemyPower, enemyHp);

        object enemyData = GetField(Enemy, "enemyData");
        Type retreatRuleType = RuntimeType("EnemyData_SO+RoundStartRetreatRule");
        object retreatRule = Activator.CreateInstance(retreatRuleType);
        SetField(retreatRule, "enabled", true);
        SetField(retreatRule, "triggerRound", 1);
        SetField(retreatRule, "settlementLootTables", CreateRuntimeList("LootTable_SO"));
        SetField(enemyData, "roundStartRetreatRule", retreatRule);
    }

    public void BuildBattleWithAllyStatusEffectSkill(int allyPower, int enemyPower, int enemyHp)
    {
        BuildOneOnOneBattle(allyPower, enemyPower, enemyHp);
        object burnStatus = CreateDamageStatus("burn", "PlayMode Burn", damagePerStack: 1);
        object statusSkill = CreateSkill("PlayMode Status Strike", allyPower, allyPower);
        ConfigureSkillStatusEffects(statusSkill, burnStatus);
        ReplaceAllyWeaponSkill(statusSkill);
        Invoke(Ally, "SelectDefaultSkill");
    }


    public void TearDown()
    {
        ClearStaticRuntimeState();

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            UnityEngine.Object created = createdObjects[i];
            if (created != null)
                UnityEngine.Object.Destroy(created);
        }

        createdObjects.Clear();
        DestroySceneCurtainTransitionController();
    }

    public void ClickBattleButton()
    {
        Invoke(BattleButton, "OnBattleButtonClicked");
    }

    public void ClickRelicChoiceEntrance()
    {
        object view = FindLootView(viewEntry => (bool)GetField(viewEntry, "isRelicChoiceEntrance"));
        Assert.IsNotNull(view, "Relic choice entrance loot view was not found.");
        ClickViewButton(view);
    }

    public void ClickRelicChoiceOption(int optionIndex)
    {
        object overlay = GetSettlementOverlay();
        IList views = (IList)GetField(overlay, "relicChoiceViews");
        Assert.IsNotNull(views, "Relic choice views were not created.");
        Assert.Greater(views.Count, optionIndex, $"Relic choice option {optionIndex} was not created.");

        ClickViewButton(views[optionIndex]);
    }

    public string RelicChoiceOptionName(int optionIndex)
    {
        object overlay = GetSettlementOverlay();
        IList views = (IList)GetField(overlay, "relicChoiceViews");
        Assert.IsNotNull(views, "Relic choice views were not created.");
        Assert.Greater(views.Count, optionIndex, $"Relic choice option {optionIndex} was not created.");

        object entry = GetField(views[optionIndex], "entry");
        return entry != null ? (string)GetField(entry, "displayName") : null;
    }

    public void ClickRelicChoiceConfirmButton()
    {
        object overlay = GetSettlementOverlay();
        RectTransform root = (RectTransform)GetField(overlay, "relicChoiceModalButtonRoot");
        Assert.IsNotNull(root, "Relic choice modal button root was not created.");

        Button button = root.GetComponentInChildren<Button>(true);
        Assert.IsNotNull(button, "Relic choice confirm button was not created.");
        button.onClick.Invoke();
    }

    public void ClickSettlementReturnButton()
    {
        object overlay = GetSettlementOverlay();
        IList buttons = (IList)GetField(overlay, "spawnedButtons");
        Assert.IsNotNull(buttons, "Settlement action buttons were not created.");
        Assert.Greater(buttons.Count, 0, "Settlement return button was not created.");

        Button button = buttons[0] as Button;
        Assert.IsNotNull(button, "Settlement return action is not a Button.");
        button.onClick.Invoke();
    }

    public void ClickSettlementRetryButton()
    {
        object overlay = GetSettlementOverlay();
        IList buttons = (IList)GetField(overlay, "spawnedButtons");
        Assert.IsNotNull(buttons, "Settlement action buttons were not created.");
        Assert.Greater(buttons.Count, 1, "Settlement retry button was not created.");

        Button button = buttons[1] as Button;
        Assert.IsNotNull(button, "Settlement retry action is not a Button.");
        button.onClick.Invoke();
    }

    public void ForceContestedConnection()
    {
        SetField(AllyButton, "isConnected", true);
        SetField(AllyButton, "connectedButton", EnemyButton);
        SetField(AllyButton, "absorbedEnemyButton", EnemyButton);

        SetField(EnemyButton, "isConnected", true);
        SetField(EnemyButton, "connectedButton", AllyButton);
    }

    public void ForceAllySingleAttackConnection()
    {
        SetField(AllyButton, "isConnected", true);
        SetField(AllyButton, "connectedButton", EnemyButton);
        SetField(AllyButton, "absorbedEnemyButton", null);
    }

    public void ForceEnemySingleAttackConnection()
    {
        SetField(EnemyButton, "isConnected", true);
        SetField(EnemyButton, "connectedButton", AllyButton);
    }

    public void BeginBlockingTutorialStep()
    {
        object definition = CreateScriptableObject("TutorialBattleDefinition_SO");
        SetField(definition, "tutorialBattleId", "playmode_blocking_tutorial");
        SetField(definition, "displayName", "PlayMode Blocking Tutorial");

        object step = Activator.CreateInstance(RuntimeType("TutorialBattleStepDefinition"));
        SetField(step, "stepId", "block_before_combat");
        SetField(step, "title", "Block Before Combat");
        SetField(step, "body", "PlayMode blocking step.");
        SetField(step, "sortOrder", 0);
        SetField(step, "completionEvent", Enum.Parse(RuntimeType("TutorialBattleEventType"), "ContinuePressed"));
        SetField(step, "inputPolicy", Enum.Parse(RuntimeType("TutorialBattleInputPolicy"), "RequireBeforeCombat"));
        SetField(step, "autoShow", false);
        SetField(step, "blockedMessage", "PlayMode combat blocked.");

        SetField(definition, "steps", CreateRuntimeList("TutorialBattleStepDefinition", step));
        InvokeStatic("TutorialBattleRuntime", "BeginBattle", definition);
    }

    public bool IsResolvingCombat => (bool)GetField(BattleButton, "isResolvingCombat");

    public bool EnemyIsDead => (bool)GetProperty(Enemy, "IsDead");

    public bool AllyIsDead => (bool)GetProperty(Ally, "IsDead");

    public int EnemyCurrentHp => (int)GetProperty(Enemy, "CurrentHP");

    public int EnemyBurnStacks => (int)GetProperty(Enemy, "BurnStacks");

    public string AllySelectedSkillName
    {
        get
        {
            object skill = GetProperty(Ally, "SelectedSkill");
            return skill != null ? (string)GetField(skill, "skillName") : null;
        }
    }

    public int AllySelectedSkillStatusEffectCount
    {
        get
        {
            object skill = GetProperty(Ally, "SelectedSkill");
            if (skill == null)
                return 0;

            Array diceEffectSettings = GetField(skill, "diceEffectSettings") as Array;
            if (diceEffectSettings == null || diceEffectSettings.Length == 0)
                return 0;

            object firstSettings = diceEffectSettings.GetValue(0);
            Array statusEffects = GetField(firstSettings, "statusEffects") as Array;
            return statusEffects != null ? statusEffects.Length : 0;
        }
    }

    public string CurrentBattlePhase => GetField(BattleButton, "currentPhase").ToString();

    public bool AllyButtonInteractable => (bool)GetProperty(AllyButton, "IsInteractable");

    public object EnemyConnectedTarget => GetProperty(EnemyButton, "ConnectedTarget");

    public bool SettlementOverlayVisible
    {
        get
        {
            object overlay = GetField(BattleButton, "battleSettlementOverlay");
            return overlay != null && (bool)GetProperty(overlay, "IsVisible");
        }
    }

    public bool LastSettlementHasRelicChoice
    {
        get
        {
            object overlay = GetField(BattleButton, "battleSettlementOverlay");
            if (overlay == null)
                return false;

            object choiceData = GetField(overlay, "activeRelicChoiceData");
            return choiceData != null && (bool)GetProperty(choiceData, "HasChoices");
        }
    }

    public bool RelicChoiceModalVisible
    {
        get
        {
            object overlay = GetSettlementOverlay();
            RectTransform root = (RectTransform)GetField(overlay, "relicChoiceModalRoot");
            return root != null && root.gameObject.activeSelf;
        }
    }

    public string SelectedRelicChoiceName
    {
        get
        {
            object selectedChoice = GetSelectedRelicChoice();
            return selectedChoice != null ? (string)GetField(selectedChoice, "displayName") : null;
        }
    }

    public string PresentedRelicChoiceEntranceName
    {
        get
        {
            object overlay = GetSettlementOverlay();
            object view = FindLootView(viewEntry => (bool)GetField(viewEntry, "isRelicChoiceEntrance"));
            Assert.IsNotNull(view, "Relic choice entrance loot view was not found.");

            object entry = GetField(view, "entry");
            object presentedEntry = Invoke(overlay, "ResolvePresentedLootEntry", entry);
            return presentedEntry != null ? (string)GetField(presentedEntry, "displayName") : null;
        }
    }

    public string RelicChoiceModalTitle
    {
        get
        {
            object overlay = GetSettlementOverlay();
            object titleText = GetField(overlay, "relicChoiceModalTitleText");
            return titleText != null ? (string)GetProperty(titleText, "text") : null;
        }
    }

    public bool CurrentEncounterQueued => GetStaticProperty("BattleEncounterRuntime", "CurrentEncounter") != null;

    public bool SpeedButtonCombatLocked
    {
        get
        {
            Type speedButtonType = RuntimeType("SpeedButton");
            return (bool)speedButtonType.GetField("CombatLocked", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }
    }

    public IEnumerator WaitUntilCombatSettles(float timeoutSeconds = 8f)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (!IsResolvingCombat)
                yield break;

            yield return null;
        }

        Assert.Fail("BattleButton did not finish combat resolution within the time budget.");
    }

    private object CreateAlly(Transform parent, string name, int speed, int maxHp, object skill)
    {
        GameObject allyObject = CreateGameObject(name);
        allyObject.transform.SetParent(parent, false);
        allyObject.transform.position = new Vector3(-1.5f, 0f, 0f);

        object allyController = AddRuntimeComponent(allyObject, "BattleAllyController");
        AllyButton = CreateSpeedButton(allyObject.transform, "Ally Speed Button", "Ally");

        object character = CreateScriptableObject("CharacterData_SO");
        SetField(character, "characterName", name);
        SetField(character, "baseHP", maxHp);
        SetField(character, "minSpeed", speed);
        SetField(character, "maxSpeed", speed);

        object weapon = CreateScriptableObject("Weapon_SO");
        SetField(weapon, "weaponName", "PlayMode Test Weapon");
        SetField(weapon, "skillPool", CreateRuntimeArray("EnemySkill_SO", skill));

        object state = Activator.CreateInstance(RuntimeType("RunPartyMemberState"));
        SetField(state, "memberId", name);
        SetField(state, "characterData", character);
        SetField(state, "weaponData", weapon);
        SetField(state, "level", 1);
        SetField(state, "experience", 0);
        SetField(state, "maxHP", maxHp);
        SetField(state, "currentHP", maxHp);

        Invoke(allyController, "ApplyRuntimeState", state);
        RegisterSpeedButton(allyController, AllyButton);
        return allyController;
    }

    private object CreateEnemy(Transform parent, string name, int speed, int hp, object skill)
    {
        GameObject enemyObject = CreateGameObject(name);
        enemyObject.transform.SetParent(parent, false);
        enemyObject.transform.position = new Vector3(1.5f, 0f, 0f);

        object enemyController = AddRuntimeComponent(enemyObject, "BattleEnemyController");
        EnemyButton = CreateSpeedButton(enemyObject.transform, "Enemy Speed Button", "Enemy");

        object enemyData = CreateScriptableObject("EnemyData_SO");
        SetField(enemyData, "enemyName", name);
        SetField(enemyData, "baseHP", hp);
        SetField(enemyData, "minSpeed", speed);
        SetField(enemyData, "maxSpeed", speed);
        SetField(enemyData, "baseSpeedButtonCount", 1);
        SetField(enemyData, "skillPool", CreateRuntimeList("EnemySkill_SO", skill));

        Invoke(enemyController, "ApplyEnemyData", enemyData);
        RegisterSpeedButton(enemyController, EnemyButton);
        return enemyController;
    }

    private object CreateEnemyGroupForEncounter()
    {
        object enemyData = GetField(Enemy, "enemyData");
        object group = CreateScriptableObject("EnemyGroup_SO");
        SetField(group, "groupName", "PlayMode Elite Group");
        SetField(group, "enemies", CreateRuntimeList("EnemyData_SO", enemyData));
        return group;
    }

    private object CreateRelicChoiceTable()
    {
        object table = CreateScriptableObject("RelicTable_SO");
        SetField(table, "tableName", "PlayMode Elite Relic Choice");
        SetField(table, "relics", CreateRuntimeList(
            "Relic_SO",
            CreateRelic("playmode_relic_1", "PlayMode Relic 1"),
            CreateRelic("playmode_relic_2", "PlayMode Relic 2"),
            CreateRelic("playmode_relic_3", "PlayMode Relic 3")));
        return table;
    }

    private object CreateRelic(string relicId, string relicName)
    {
        object relic = CreateScriptableObject("Relic_SO");
        SetField(relic, "relicId", relicId);
        SetField(relic, "relicName", relicName);
        SetField(relic, "description", "PlayMode relic choice.");
        SetField(relic, "rarity", Enum.Parse(RuntimeType("LootRarity"), "Rare"));
        return relic;
    }

    private object CreateSkill(string name, int minPower, int maxPower)
    {
        object skill = CreateScriptableObject("EnemySkill_SO");
        SetField(skill, "skillName", name);
        SetField(skill, "energyCost", 0);
        SetField(skill, "diceCount", 1);

        Type rangeType = RuntimeType("EnemySkill_SO+DicePowerRange");
        object range = Activator.CreateInstance(rangeType);
        SetField(range, "minPower", minPower);
        SetField(range, "maxPower", maxPower);

        Array ranges = Array.CreateInstance(rangeType, 1);
        ranges.SetValue(range, 0);
        SetField(skill, "dicePowerRanges", ranges);
        return skill;
    }

    private object CreateDamageStatus(string statusId, string displayName, int damagePerStack)
    {
        object status = CreateScriptableObject("BattleStatus_SO");
        SetField(status, "statusId", statusId);
        SetField(status, "displayName", displayName);
        SetField(status, "behaviorType", Enum.Parse(RuntimeType("BattleStatus_SO+StatusBehaviorType"), "DamageAtRoundStart"));
        SetField(status, "powerPerStack", damagePerStack);
        SetField(status, "roundStartDecayMode", Enum.Parse(RuntimeType("BattleStatus_SO+StackDecayMode"), "None"));
        return status;
    }

    private void ConfigureSkillStatusEffects(object skill, object status)
    {
        Type diceSettingsType = RuntimeType("EnemySkill_SO+DiceEffectSettings");
        Type statusEffectType = RuntimeType("EnemySkill_SO+StatusEffectEntry");

        object onUse = CreateStatusEffectEntry(statusEffectType, status, "OnSkillUse");
        object onHit = CreateStatusEffectEntry(statusEffectType, status, "OnHit");

        Array statusEffects = Array.CreateInstance(statusEffectType, 2);
        statusEffects.SetValue(onUse, 0);
        statusEffects.SetValue(onHit, 1);

        object diceSettings = Activator.CreateInstance(diceSettingsType);
        SetField(diceSettings, "statusEffects", statusEffects);

        Array diceEffectSettings = Array.CreateInstance(diceSettingsType, 1);
        diceEffectSettings.SetValue(diceSettings, 0);
        SetField(skill, "diceEffectSettings", diceEffectSettings);
    }

    private void ReplaceAllyWeaponSkill(object skill)
    {
        object allyController = GetField(Ally, "allyController");
        object weapon = GetField(allyController, "weapon");
        object weaponData = GetField(weapon, "weaponData");
        SetField(weaponData, "skillPool", CreateRuntimeArray("EnemySkill_SO", skill));
    }

    private object CreateStatusEffectEntry(Type statusEffectType, object status, string triggerName)
    {
        object entry = Activator.CreateInstance(statusEffectType);
        SetField(entry, "trigger", Enum.Parse(RuntimeType("EnemySkill_SO+DiceEffectTrigger"), triggerName));
        SetField(entry, "targetMode", Enum.Parse(RuntimeType("EnemySkill_SO+DamageTargetMode"), "CurrentTarget"));
        SetField(entry, "statusData", status);
        SetField(entry, "stackAmount", 1);
        SetField(entry, "randomTargetCount", 1);
        SetField(entry, "applyAfterRoundStarts", 1);
        SetField(entry, "applyRepeatCount", 1);
        return entry;
    }

    private object GetSettlementOverlay()
    {
        object overlay = GetField(BattleButton, "battleSettlementOverlay");
        Assert.IsNotNull(overlay, "Battle settlement overlay was not resolved.");
        return overlay;
    }

    private object FindLootView(Func<object, bool> entryPredicate)
    {
        object overlay = GetSettlementOverlay();
        IList views = (IList)GetField(overlay, "lootViews");
        Assert.IsNotNull(views, "Loot views were not created.");

        for (int i = 0; i < views.Count; i++)
        {
            object view = views[i];
            if (view == null)
                continue;

            object entry = GetField(view, "entry");
            if (entry != null && entryPredicate(entry))
                return view;
        }

        return null;
    }

    private object GetSelectedRelicChoice()
    {
        object overlay = GetSettlementOverlay();
        object choiceData = GetField(overlay, "activeRelicChoiceData");
        return choiceData != null ? GetField(choiceData, "selectedChoice") : null;
    }

    private static void ClickViewButton(object view)
    {
        Button button = GetField(view, "button") as Button;
        Assert.IsNotNull(button, "View button was not created.");
        button.onClick.Invoke();
    }

    private object CreateSpeedButton(Transform parent, string name, string buttonTypeName)
    {
        GameObject buttonObject = CreateGameObject(name);
        buttonObject.transform.SetParent(parent, false);
        buttonObject.transform.localPosition = buttonTypeName == "Ally"
            ? new Vector3(-0.2f, 0.8f, 0f)
            : new Vector3(0.2f, 0.8f, 0f);

        object button = AddRuntimeComponent(buttonObject, "SpeedButton");
        Type enumType = RuntimeType("SpeedButton+ButtonType");
        object buttonType = Enum.Parse(enumType, buttonTypeName);
        Invoke(button, "SetButtonType", buttonType);
        return button;
    }

    private void RegisterSpeedButton(object controller, object button)
    {
        Array buttons = CreateRuntimeArray("SpeedButton", button);
        Invoke(controller, "RegisterSpeedButtons", buttons);
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }

    private object AddRuntimeComponent(GameObject target, string typeName)
    {
        Component component = target.AddComponent(RuntimeType(typeName));
        return component;
    }

    private object CreateScriptableObject(string typeName)
    {
        ScriptableObject asset = ScriptableObject.CreateInstance(RuntimeType(typeName));
        createdObjects.Add(asset);
        return asset;
    }

    private Array CreateRuntimeArray(string elementTypeName, params object[] values)
    {
        Type elementType = RuntimeType(elementTypeName);
        Array array = Array.CreateInstance(elementType, values.Length);
        for (int i = 0; i < values.Length; i++)
            array.SetValue(values[i], i);
        return array;
    }

    private object CreateRuntimeList(string elementTypeName, params object[] values)
    {
        Type listType = typeof(List<>).MakeGenericType(RuntimeType(elementTypeName));
        IList list = (IList)Activator.CreateInstance(listType);
        for (int i = 0; i < values.Length; i++)
            list.Add(values[i]);
        return list;
    }

    private static void ClearStaticRuntimeState()
    {
        InvokeStaticIfExists("BattleEncounterRuntime", "Clear");
        InvokeStaticIfExists("TutorialBattleRuntime", "Clear");

        Type speedButtonType = RuntimeType("SpeedButton");
        speedButtonType.GetField("CombatLocked", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, false);
        speedButtonType.GetField("InputSuppressed", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, false);
    }

    private static void DestroySceneCurtainTransitionController()
    {
        Type transitionType = RuntimeType("SceneCurtainTransitionController");
        UnityEngine.Object controller = UnityEngine.Object.FindObjectOfType(transitionType, true);
        if (controller != null)
            UnityEngine.Object.Destroy(controller);

        FieldInfo instanceField = transitionType.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
        instanceField?.SetValue(null, null);
    }

    private static void InvokeStaticIfExists(string typeName, string methodName)
    {
        MethodInfo method = RuntimeType(typeName).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        method?.Invoke(null, null);
    }

    private static object InvokeStatic(string typeName, string methodName, params object[] args)
    {
        Type type = RuntimeType(typeName);
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, $"Static method {methodName} was not found on {typeName}.");
        return method.Invoke(null, CompleteOptionalArguments(method, args));
    }

    private static object GetStaticProperty(string typeName, string propertyName)
    {
        Type type = RuntimeType(typeName);
        PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(property, $"Static property {propertyName} was not found on {typeName}.");
        return property.GetValue(null);
    }

    private static object Invoke(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, $"Method {methodName} was not found on {target.GetType().Name}.");
        return method.Invoke(target, CompleteOptionalArguments(method, args));
    }

    private static object[] CompleteOptionalArguments(MethodInfo method, object[] args)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (args.Length == parameters.Length)
            return args;

        Assert.LessOrEqual(args.Length, parameters.Length, $"Too many arguments were supplied for {method.Name}.");
        object[] completed = new object[parameters.Length];
        for (int i = 0; i < completed.Length; i++)
        {
            if (i < args.Length)
            {
                completed[i] = args[i];
                continue;
            }

            Assert.IsTrue(parameters[i].IsOptional, $"Missing non-optional argument {parameters[i].Name} for {method.Name}.");
            completed[i] = parameters[i].DefaultValue;
        }

        return completed;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static object GetField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        return field.GetValue(target);
    }

    private static object GetProperty(object target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(property, $"Property {propertyName} was not found on {target.GetType().Name}.");
        return property.GetValue(target);
    }

    private static Type RuntimeType(string typeName)
    {
        Type type = Type.GetType($"{typeName}, {RuntimeAssemblyName}");
        Assert.IsNotNull(type, $"Runtime type {typeName} was not found in {RuntimeAssemblyName}.");
        return type;
    }
}
