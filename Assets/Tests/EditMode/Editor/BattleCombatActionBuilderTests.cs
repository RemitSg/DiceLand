using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleCombatActionBuilderTests
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
    public void BuildActions_CreatesOneClashForContestedEnemyButtonAndSuppressesEnemySingleAttack()
    {
        BattleAllyController ally = CreateAlly("Ally", speed: 5, buttonCount: 1);
        BattleEnemyController enemy = CreateEnemy("Enemy", speed: 4, buttonCount: 1);

        SpeedButton allyButton = ally.SpeedButtons[0];
        SpeedButton enemyButton = enemy.SpeedButtons[0];
        ConnectAllyToEnemy(allyButton, enemyButton, contested: true);
        ConnectEnemyToAlly(enemyButton, allyButton);

        List<BattleCombatAction> actions = BattleCombatActionBuilder.Build(
            new[] { ally },
            new[] { enemy },
            _ => { });

        Assert.AreEqual(1, actions.Count);
        Assert.AreEqual(BattleCombatActionType.Clash, actions[0].Type);
        Assert.AreEqual(ally, actions[0].Ally);
        Assert.AreEqual(enemy, actions[0].Enemy);
        Assert.AreEqual(enemyButton, actions[0].EnemyButton);
    }

    [Test]
    public void BuildActions_PreservesBattleButtonOrderingRules()
    {
        BattleAllyController fastAlly = CreateAlly("Fast Ally", speed: 7, buttonCount: 1);
        BattleAllyController tiedAlly = CreateAlly("Tied Ally", speed: 6, buttonCount: 1);
        BattleEnemyController tiedEnemy = CreateEnemy("Tied Enemy", speed: 6, buttonCount: 1);
        BattleEnemyController clashEnemy = CreateEnemy("Clash Enemy", speed: 4, buttonCount: 1);

        ConnectAllyToEnemy(fastAlly.SpeedButtons[0], clashEnemy.SpeedButtons[0], contested: false);
        ConnectAllyToEnemy(tiedAlly.SpeedButtons[0], tiedEnemy.SpeedButtons[0], contested: false);
        ConnectEnemyToAlly(tiedEnemy.SpeedButtons[0], tiedAlly.SpeedButtons[0]);
        ConnectAllyToEnemy(fastAlly.SpeedButtons[0], clashEnemy.SpeedButtons[0], contested: true);
        ConnectEnemyToAlly(clashEnemy.SpeedButtons[0], fastAlly.SpeedButtons[0]);

        List<BattleCombatAction> actions = BattleCombatActionBuilder.Build(
            new[] { fastAlly, tiedAlly },
            new[] { tiedEnemy, clashEnemy },
            _ => { });

        Assert.AreEqual(3, actions.Count);
        Assert.AreEqual(BattleCombatActionType.Clash, actions[0].Type);
        Assert.AreEqual(clashEnemy, actions[0].Enemy);
        Assert.AreEqual(BattleCombatActionType.AllySingleAttack, actions[1].Type);
        Assert.AreEqual(tiedAlly, actions[1].Ally);
        Assert.AreEqual(tiedEnemy, actions[1].Enemy);
        Assert.AreEqual(BattleCombatActionType.EnemySingleAttack, actions[2].Type);
        Assert.AreEqual(tiedEnemy, actions[2].Enemy);
        Assert.AreEqual(tiedAlly, actions[2].TargetAlly);
    }

    [Test]
    public void BuildActions_LogsLivingAlliesWithoutAssignedTargets()
    {
        BattleAllyController idleAlly = CreateAlly("Idle Ally", speed: 3, buttonCount: 1);
        BattleEnemyController enemy = CreateEnemy("Enemy", speed: 3, buttonCount: 1);
        List<string> messages = new List<string>();

        List<BattleCombatAction> actions = BattleCombatActionBuilder.Build(
            new[] { idleAlly },
            new[] { enemy },
            messages.Add);

        Assert.AreEqual(0, actions.Count);
        Assert.AreEqual(1, messages.Count);
        StringAssert.Contains("\u672a\u6307\u5b9a\u76ee\u6807", messages[0]);
    }

    private BattleAllyController CreateAlly(string name, int speed, int buttonCount)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        character.minSpeed = speed;
        character.maxSpeed = speed;
        createdObjects.Add(character);

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            level = 1,
            maxHP = 10,
            currentHP = 10
        });
        ally.RegisterSpeedButtons(CreateSpeedButtons(go.transform, buttonCount, SpeedButton.ButtonType.Ally));
        ally.GenerateRandomSpeed();
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int speed, int buttonCount)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = 10;
        enemyData.minSpeed = speed;
        enemyData.maxSpeed = speed;
        enemyData.baseSpeedButtonCount = buttonCount;
        createdObjects.Add(enemyData);

        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(enemyData);
        enemy.RegisterSpeedButtons(CreateSpeedButtons(go.transform, buttonCount, SpeedButton.ButtonType.Enemy));
        enemy.GenerateRandomSpeed();
        return enemy;
    }

    private List<SpeedButton> CreateSpeedButtons(Transform parent, int count, SpeedButton.ButtonType buttonType)
    {
        List<SpeedButton> buttons = new List<SpeedButton>();
        for (int i = 0; i < count; i++)
        {
            GameObject buttonObject = new GameObject($"SpeedButton_{i + 1}");
            buttonObject.transform.SetParent(parent, false);
            SpeedButton button = buttonObject.AddComponent<SpeedButton>();
            button.SetButtonType(buttonType);
            buttons.Add(button);
        }

        return buttons;
    }

    private static void ConnectAllyToEnemy(SpeedButton allyButton, SpeedButton enemyButton, bool contested)
    {
        SetPrivateField(allyButton, "isConnected", true);
        SetPrivateField(allyButton, "connectedButton", enemyButton);
        SetPrivateField(allyButton, "absorbedEnemyButton", contested ? enemyButton : null);
    }

    private static void ConnectEnemyToAlly(SpeedButton enemyButton, SpeedButton allyButton)
    {
        SetPrivateField(enemyButton, "isConnected", true);
        SetPrivateField(enemyButton, "connectedButton", allyButton);
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
