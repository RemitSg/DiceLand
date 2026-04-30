using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleSpeedButtonListServiceTests
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
    public void GetAllyButtons_ReturnsActiveButtonsBeforeLegacySingleButton()
    {
        BattleAllyController ally = CreateAlly("Ally", buttonCount: 2);

        IReadOnlyList<SpeedButton> buttons = BattleSpeedButtonListService.GetAllyButtons(ally);

        Assert.AreEqual(2, buttons.Count);
        Assert.AreEqual(ally.SpeedButtons[0], buttons[0]);
        Assert.AreEqual(ally.SpeedButtons[1], buttons[1]);
    }

    [Test]
    public void GetEnemyButtons_ReturnsEmptyForNullEnemy()
    {
        IReadOnlyList<SpeedButton> buttons = BattleSpeedButtonListService.GetEnemyButtons(null);

        Assert.IsNotNull(buttons);
        Assert.AreEqual(0, buttons.Count);
    }

    private BattleAllyController CreateAlly(string name, int buttonCount)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        character.minSpeed = 5;
        character.maxSpeed = 5;
        createdObjects.Add(character);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            level = 1,
            maxHP = 10,
            currentHP = 10
        });

        List<SpeedButton> speedButtons = new List<SpeedButton>();
        for (int i = 0; i < buttonCount; i++)
        {
            GameObject buttonObject = new GameObject($"SpeedButton_{i + 1}");
            buttonObject.transform.SetParent(go.transform, false);
            SpeedButton button = buttonObject.AddComponent<SpeedButton>();
            button.SetButtonType(SpeedButton.ButtonType.Ally);
            speedButtons.Add(button);
        }

        ally.RegisterSpeedButtons(speedButtons);
        ally.SetTemporaryExtraSpeedButtons(Mathf.Max(0, buttonCount - 1));
        ally.GenerateRandomSpeed();
        return ally;
    }
}
