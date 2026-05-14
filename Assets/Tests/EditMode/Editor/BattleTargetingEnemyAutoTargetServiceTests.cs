using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleTargetingEnemyAutoTargetServiceTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object created = createdObjects[i];
            if (created != null)
                Object.DestroyImmediate(created);
        }

        createdObjects.Clear();
    }

    [Test]
    public void BuildLogEntries_ReportsOnlyConnectedEnemyButtons()
    {
        BattleEnemyController enemy = CreateEnemy("Scout", buttonCount: 2);
        BattleAllyController ally = CreateAlly("Guard", buttonCount: 1);
        Connect(enemy.SpeedButtons[1], ally.SpeedButtons[0]);

        List<string> logs = BattleTargetingEnemyAutoTargetService.BuildLogEntries(new[] { enemy });

        Assert.AreEqual(1, logs.Count);
        StringAssert.Contains("Scout", logs[0]);
        StringAssert.Contains("Guard", logs[0]);
        StringAssert.Contains("2", logs[0]);
    }

    [Test]
    public void BuildLogEntries_SkipsDeadAndNullEnemies()
    {
        BattleEnemyController enemy = CreateEnemy("Scout", buttonCount: 1);
        enemy.TakeDamage(99);

        List<string> logs = BattleTargetingEnemyAutoTargetService.BuildLogEntries(new BattleEnemyController[] { null, enemy });

        Assert.AreEqual(0, logs.Count);
    }

    [Test]
    public void ResolvePreferredTargetButton_PrefersAllyWithEnoughNegativeStatusTypes()
    {
        BattleEnemyController enemy = CreateEnemy("LightKnight", buttonCount: 1);
        BattleAllyController cleanAlly = CreateAlly("Clean", buttonCount: 1);
        BattleAllyController markedAlly = CreateAlly("Marked", buttonCount: 1);
        markedAlly.AddStatus(CreateStatus("bleed", "流血"), "bleed", "流血", 1);
        markedAlly.AddStatus(CreateStatus("slowness", "缓慢"), "slowness", "缓慢", 1);
        SetSelectedSkillForButton(enemy, CreateSkillWithNegativeStatusPriority());

        SpeedButton preferred = InvokeResolvePreferredTargetButton(
            enemy,
            enemy.SpeedButtons[0],
            new List<SpeedButton> { cleanAlly.SpeedButtons[0], markedAlly.SpeedButtons[0] });

        Assert.AreSame(markedAlly.SpeedButtons[0], preferred);
    }

    private BattleAllyController CreateAlly(string name, int buttonCount)
    {
        GameObject go = CreateGameObject(name);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = CreateCharacter(name),
            level = 1,
            maxHP = 10,
            currentHP = 10
        });
        ally.RegisterSpeedButtons(CreateSpeedButtons(go.transform, buttonCount, SpeedButton.ButtonType.Ally));
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int buttonCount)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = 10;
        enemyData.baseSpeedButtonCount = buttonCount;
        createdObjects.Add(enemyData);
        enemy.ApplyEnemyData(enemyData);
        enemy.RegisterSpeedButtons(CreateSpeedButtons(go.transform, buttonCount, SpeedButton.ButtonType.Enemy));
        return enemy;
    }

    private CharacterData_SO CreateCharacter(string name)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        createdObjects.Add(character);
        return character;
    }

    private BattleStatus_SO CreateStatus(string statusId, string displayName)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        createdObjects.Add(status);
        return status;
    }

    private EnemySkill_SO CreateSkillWithNegativeStatusPriority()
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.targetPriorityMode = EnemySkill_SO.TargetPriorityMode.PreferTargetsWithNegativeStatusTypesAtLeast;
        skill.targetPriorityRequiredStacks = 2;
        createdObjects.Add(skill);
        return skill;
    }

    private List<SpeedButton> CreateSpeedButtons(Transform parent, int count, SpeedButton.ButtonType buttonType)
    {
        List<SpeedButton> buttons = new List<SpeedButton>();
        for (int i = 0; i < count; i++)
        {
            GameObject buttonObject = CreateGameObject($"SpeedButton_{i + 1}");
            buttonObject.transform.SetParent(parent, false);
            SpeedButton button = buttonObject.AddComponent<SpeedButton>();
            button.SetButtonType(buttonType);
            buttons.Add(button);
        }

        return buttons;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }

    private static void Connect(SpeedButton source, SpeedButton target)
    {
        SetPrivateField(source, "isConnected", true);
        SetPrivateField(source, "connectedButton", target);
    }

    private static SpeedButton InvokeResolvePreferredTargetButton(BattleEnemyController enemy, SpeedButton source, List<SpeedButton> allyButtons)
    {
        MethodInfo method = typeof(BattleEnemyController).GetMethod("ResolvePreferredTargetButton", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return method.Invoke(enemy, new object[] { source, allyButtons }) as SpeedButton;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static void SetPrivateProperty<TValue>(object target, string propertyName, TValue value)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(property, $"Property {propertyName} was not found on {target.GetType().Name}.");
        property.SetValue(target, value);
    }

    private static void SetSelectedSkillForButton(BattleEnemyController enemy, EnemySkill_SO skill)
    {
        SetPrivateProperty(enemy, "SelectedSkill", skill);

        FieldInfo field = typeof(BattleEnemyController).GetField("selectedSkillsPerButton", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "Field selectedSkillsPerButton was not found on BattleEnemyController.");
        List<EnemySkill_SO> selectedSkills = field.GetValue(enemy) as List<EnemySkill_SO>;
        Assert.IsNotNull(selectedSkills);
        selectedSkills.Clear();
        selectedSkills.Add(skill);
    }
}
