using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattlePuppetPassiveServiceTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ApplyPuppetMasterDamageRedirect_DamagesLivingSoullessPuppetInsteadOfMaster()
    {
        BattleEnemyController master = CreateEnemy("牵丝傀儡师", BattlePuppetPassiveService.PuppetMasterEnemyId, 35);
        BattleEnemyController puppet = CreateEnemy("无神傀儡", BattlePuppetPassiveService.SoullessPuppetEnemyId, 20);

        bool redirected = BattlePuppetPassiveService.TryApplyPuppetMasterDamageRedirect(
            master,
            7,
            new[] { master, puppet },
            _ => 0,
            out int dealtDamage,
            null);

        Assert.IsTrue(redirected);
        Assert.AreEqual(7, dealtDamage);
        Assert.AreEqual(35, master.CurrentHP);
        Assert.AreEqual(13, puppet.CurrentHP);
    }

    [Test]
    public void ResolveSoullessPuppetUnmarkedTargetPowerPenalty_ReturnsMinusOneOnlyWhenTargetIsUnmarked()
    {
        BattleEnemyController puppet = CreateEnemy("无神傀儡", BattlePuppetPassiveService.SoullessPuppetEnemyId, 20);
        BattleAllyController ally = CreateAlly("目标", 20);
        BattleStatus_SO markStatus = CreateStatus("target", "标记");

        Assert.AreEqual(-1, BattlePuppetPassiveService.ResolveSoullessPuppetUnmarkedTargetPowerPenalty(puppet, ally, markStatus));

        ally.AddStatus(markStatus, markStatus.StatusId, markStatus.DisplayName, 1);

        Assert.AreEqual(0, BattlePuppetPassiveService.ResolveSoullessPuppetUnmarkedTargetPowerPenalty(puppet, ally, markStatus));
    }

    private BattleEnemyController CreateEnemy(string name, int id, int hp)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.enemyID = id;
        enemyData.baseHP = hp;
        createdObjects.Add(enemyData);
        enemy.ApplyEnemyData(enemyData);
        return enemy;
    }

    private BattleAllyController CreateAlly(string name, int hp)
    {
        GameObject go = CreateGameObject(name);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = hp;
        createdObjects.Add(character);
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            level = 1,
            maxHP = hp,
            currentHP = hp
        });
        return ally;
    }

    private BattleStatus_SO CreateStatus(string statusId, string displayName)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        createdObjects.Add(status);
        return status;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
