using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleCombatActionCollectorServiceTests
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
    public void CollectAllyActions_AddsClashOnceAndTracksContestedEnemyButton()
    {
        BattleAllyController ally = CreateAlly("Ally", speed: 7, buttonCount: 2);
        BattleEnemyController enemy = CreateEnemy("Enemy", speed: 4, buttonCount: 1);
        HashSet<SpeedButton> clashEnemyButtons = new HashSet<SpeedButton>();
        int sequence = 0;

        ConnectAllyToEnemy(ally.SpeedButtons[0], enemy.SpeedButtons[0], contested: true);
        ConnectAllyToEnemy(ally.SpeedButtons[1], enemy.SpeedButtons[0], contested: true);

        List<BattleCombatAction> actions = BattleCombatActionCollectorService.CollectAllyActions(
            new[] { ally },
            clashEnemyButtons,
            ref sequence,
            _ => { });

        Assert.AreEqual(2, actions.Count);
        Assert.AreEqual(BattleCombatActionType.Clash, actions[0].Type);
        Assert.AreEqual(BattleCombatActionType.AllySingleAttack, actions[1].Type);
        Assert.IsTrue(clashEnemyButtons.Contains(enemy.SpeedButtons[0]));
        Assert.AreEqual(2, sequence);
    }

    [Test]
    public void CollectEnemyActions_SkipsButtonsAlreadyUsedByClash()
    {
        BattleAllyController ally = CreateAlly("Ally", speed: 5, buttonCount: 2);
        BattleEnemyController enemy = CreateEnemy("Enemy", speed: 6, buttonCount: 2);
        HashSet<SpeedButton> clashEnemyButtons = new HashSet<SpeedButton> { enemy.SpeedButtons[0] };
        int sequence = 3;

        ConnectEnemyToAlly(enemy.SpeedButtons[0], ally.SpeedButtons[0]);
        ConnectEnemyToAlly(enemy.SpeedButtons[1], ally.SpeedButtons[1]);

        List<BattleCombatAction> actions = BattleCombatActionCollectorService.CollectEnemyActions(
            new[] { enemy },
            clashEnemyButtons,
            ref sequence);

        Assert.AreEqual(1, actions.Count);
        Assert.AreEqual(BattleCombatActionType.EnemySingleAttack, actions[0].Type);
        Assert.AreEqual(enemy.SpeedButtons[1], actions[0].EnemyButton);
        Assert.AreEqual(ally, actions[0].TargetAlly);
        Assert.AreEqual(4, sequence);
    }

    private BattleAllyController CreateAlly(string name, int speed, int buttonCount)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        character.minSpeed = speed;
        character.maxSpeed = speed;
        createdObjects.Add(character);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            level = 1,
            maxHP = 10,
            currentHP = 10
        });
        ally.RegisterSpeedButtons(CreateSpeedButtons(go.transform, buttonCount, SpeedButton.ButtonType.Ally));
        ally.SetTemporaryExtraSpeedButtons(Mathf.Max(0, buttonCount - 1));
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
