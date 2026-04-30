using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleSpeedButtonCombatVisibilityServiceTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ApplyCombatVisibility_WhenRequested_DisablesAndHidesAllyButtons()
    {
        BattleAllyController ally = CreateAlly("Ally", CreateSpeedButton("AllyButton"));

        BattleSpeedButtonCombatVisibilityService.ApplyCombatVisibility(
            new[] { ally },
            Array.Empty<BattleEnemyController>(),
            disableAllyButtons: true,
            hideAllyButtons: true,
            hideEnemyButtons: false);

        SpeedButton button = ally.speedButton;
        Assert.IsFalse(button.IsInteractable);
        Assert.IsFalse(button.unpressedButton.activeSelf);
        Assert.IsFalse(button.pressedButton.activeSelf);
        Assert.IsFalse(button.speedText.activeSelf);
    }

    [Test]
    public void ApplyCombatVisibility_WhenRequested_HidesEnemyButtonsWithoutChangingInteractableState()
    {
        SpeedButton button = CreateSpeedButton("EnemyButton");
        button.IsInteractable = true;
        BattleEnemyController enemy = CreateEnemy("Enemy", button);

        BattleSpeedButtonCombatVisibilityService.ApplyCombatVisibility(
            Array.Empty<BattleAllyController>(),
            new[] { enemy },
            disableAllyButtons: false,
            hideAllyButtons: false,
            hideEnemyButtons: true);

        Assert.IsTrue(button.IsInteractable);
        Assert.IsFalse(button.unpressedButton.activeSelf);
        Assert.IsFalse(button.pressedButton.activeSelf);
        Assert.IsFalse(button.speedText.activeSelf);
    }

    [Test]
    public void ApplyCombatVisibility_SkipsNullControllersAndButtons()
    {
        BattleAllyController ally = CreateAlly("Ally", null);
        BattleEnemyController enemy = CreateEnemy("Enemy", null);

        Assert.DoesNotThrow(() => BattleSpeedButtonCombatVisibilityService.ApplyCombatVisibility(
            new BattleAllyController[] { null, ally },
            new BattleEnemyController[] { null, enemy },
            disableAllyButtons: true,
            hideAllyButtons: true,
            hideEnemyButtons: true));
    }

    private BattleAllyController CreateAlly(string name, SpeedButton button)
    {
        GameObject go = CreateGameObject(name);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        if (button != null)
            ally.RegisterSpeedButtons(new[] { button });
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, SpeedButton button)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        if (button != null)
            enemy.RegisterSpeedButtons(new[] { button });
        return enemy;
    }

    private SpeedButton CreateSpeedButton(string name)
    {
        GameObject go = CreateGameObject(name);
        SpeedButton button = go.AddComponent<SpeedButton>();
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
}
