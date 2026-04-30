using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleTargetingSpeedButtonStateServiceTests
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
    public void PrepareForTargeting_ShowsLivingButtonsAndHidesDeadAllyButtons()
    {
        BattleAllyController livingAlly = CreateAlly("LivingAlly", currentHp: 10, out SpeedButton livingAllyButton);
        BattleAllyController deadAlly = CreateAlly("DeadAlly", currentHp: 0, out SpeedButton deadAllyButton);
        BattleEnemyController livingEnemy = CreateEnemy("LivingEnemy", currentHp: 10, out SpeedButton livingEnemyButton);

        BattleTargetingSpeedButtonStateService.PrepareForTargeting(
            new[] { livingAlly, deadAlly },
            new[] { livingEnemy });

        AssertButtonShown(livingAllyButton);
        AssertButtonHidden(deadAllyButton);
        Assert.IsFalse(deadAllyButton.IsInteractable);
        AssertButtonShown(livingEnemyButton);
    }

    [Test]
    public void EnableLivingAllyButtons_ReturnsOnlyLivingAllyButtonsAndEnablesThem()
    {
        BattleAllyController livingAlly = CreateAlly("LivingAlly", currentHp: 10, out SpeedButton livingAllyButton);
        BattleAllyController deadAlly = CreateAlly("DeadAlly", currentHp: 0, out SpeedButton deadAllyButton);

        List<SpeedButton> interactableButtons = BattleTargetingSpeedButtonStateService.EnableLivingAllyButtons(
            new[] { livingAlly, deadAlly });

        Assert.AreEqual(1, interactableButtons.Count);
        Assert.AreEqual(livingAllyButton, interactableButtons[0]);
        Assert.IsTrue(livingAllyButton.IsInteractable);
        Assert.IsFalse(deadAllyButton.IsInteractable);
    }

    [Test]
    public void PrepareForTargeting_SkipsNullCombatantsAndButtons()
    {
        Assert.DoesNotThrow(() => BattleTargetingSpeedButtonStateService.PrepareForTargeting(
            new BattleAllyController[] { null, CreateAllyWithoutButton("NoButtonAlly") },
            new BattleEnemyController[] { null, CreateEnemyWithoutButton("NoButtonEnemy") }));
    }

    private BattleAllyController CreateAlly(string name, int currentHp, out SpeedButton speedButton)
    {
        BattleAllyController ally = CreateAllyWithoutButton(name);
        speedButton = CreateSpeedButton($"{name}_Button", SpeedButton.ButtonType.Ally);
        ally.RegisterSpeedButtons(new[] { speedButton });
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = CreateCharacter(name),
            level = 1,
            maxHP = 10,
            currentHP = currentHp
        });
        return ally;
    }

    private BattleAllyController CreateAllyWithoutButton(string name)
    {
        GameObject go = CreateGameObject(name);
        return go.AddComponent<BattleAllyController>();
    }

    private BattleEnemyController CreateEnemy(string name, int currentHp, out SpeedButton speedButton)
    {
        BattleEnemyController enemy = CreateEnemyWithoutButton(name);
        enemy.ApplyEnemyData(CreateEnemyData(name));
        speedButton = CreateSpeedButton($"{name}_Button", SpeedButton.ButtonType.Enemy);
        enemy.RegisterSpeedButtons(new[] { speedButton });
        if (currentHp <= 0)
            enemy.TakeDamage(enemy.CurrentHP);
        return enemy;
    }

    private BattleEnemyController CreateEnemyWithoutButton(string name)
    {
        GameObject go = CreateGameObject(name);
        return go.AddComponent<BattleEnemyController>();
    }

    private CharacterData_SO CreateCharacter(string name)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        createdObjects.Add(character);
        return character;
    }

    private EnemyData_SO CreateEnemyData(string name)
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = 10;
        enemyData.baseSpeedButtonCount = 1;
        createdObjects.Add(enemyData);
        return enemyData;
    }

    private SpeedButton CreateSpeedButton(string name, SpeedButton.ButtonType buttonType)
    {
        GameObject go = CreateGameObject(name);
        SpeedButton button = go.AddComponent<SpeedButton>();
        button.SetButtonType(buttonType);
        button.unpressedButton = CreateGameObject($"{name}_Unpressed");
        button.pressedButton = CreateGameObject($"{name}_Pressed");
        button.speedText = CreateGameObject($"{name}_SpeedText");
        button.IsInteractable = true;
        return button;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }

    private static void AssertButtonShown(SpeedButton button)
    {
        Assert.IsTrue(button.unpressedButton.activeSelf);
        Assert.IsFalse(button.pressedButton.activeSelf);
        Assert.IsTrue(button.speedText.activeSelf);
    }

    private static void AssertButtonHidden(SpeedButton button)
    {
        Assert.IsFalse(button.unpressedButton.activeSelf);
        Assert.IsFalse(button.pressedButton.activeSelf);
        Assert.IsFalse(button.speedText.activeSelf);
    }
}
