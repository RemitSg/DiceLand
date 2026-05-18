using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleGamblerPassiveServiceTests
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
    public void ShouldBlockIncomingDamage_BlocksNonGambleLossDamageForGambler()
    {
        BattleEnemyController gambler = CreateEnemy("\u8D1D\u8005", BattleGamblerPassiveService.GamblerEnemyId, 6);

        Assert.IsTrue(BattleGamblerPassiveService.ShouldBlockIncomingDamage(gambler, BattleGamblerDamageSource.Normal));
        Assert.IsFalse(BattleGamblerPassiveService.ShouldBlockIncomingDamage(gambler, BattleGamblerDamageSource.GambleLoss));
    }

    [Test]
    public void TryApplyDebtInstantDeath_KillsAllyAtThreeDebtStacks()
    {
        BattleAllyController ally = CreateAlly("\u8D4C\u5BA2", 20);
        BattleStatus_SO debt = CreateStatus("debt", "\u8D1F\u503A");

        ally.AddStatus(debt, debt.StatusId, debt.DisplayName, 2);

        bool killed = BattleGamblerPassiveService.TryApplyDebtInstantDeath(
            ally,
            debt,
            log: null);

        Assert.IsFalse(killed);
        Assert.IsFalse(ally.IsDead);

        ally.AddStatus(debt, debt.StatusId, debt.DisplayName, 1);

        killed = BattleGamblerPassiveService.TryApplyDebtInstantDeath(
            ally,
            debt,
            log: null);

        Assert.IsTrue(killed);
        Assert.IsTrue(ally.IsDead);
        Assert.AreEqual(0, ally.CurrentHP);
    }

    [Test]
    public void TryApplyPlayerGambleClashWinDamage_DamagesGamblerOnlyWhenAllyUsesGamble()
    {
        BattleAllyController ally = CreateAlly("\u8D4C\u5BA2", 20);
        BattleEnemyController gambler = CreateEnemy("\u8D1D\u8005", BattleGamblerPassiveService.GamblerEnemyId, 6);
        BattleEnemyController other = CreateEnemy("\u65C1\u89C2\u8005", 37, 6);

        GrantAndSelectTemporarySkills(ally, CreateSkill("\u8D4C\u535A"), 0);

        Assert.IsTrue(BattleGamblerPassiveService.TryApplyPlayerGambleClashWinDamage(ally, gambler, out int dealtDamage, null));
        Assert.AreEqual(1, dealtDamage);
        Assert.AreEqual(5, gambler.CurrentHP);

        Assert.IsFalse(BattleGamblerPassiveService.TryApplyPlayerGambleClashWinDamage(ally, other, out dealtDamage, null));
        Assert.AreEqual(0, dealtDamage);
        Assert.AreEqual(6, other.CurrentHP);
    }

    [Test]
    public void TryApplyPlayerGambleClashWinDamage_IgnoresCheatAndOtherSkills()
    {
        BattleAllyController ally = CreateAlly("\u8D4C\u5BA2", 20);
        BattleEnemyController gambler = CreateEnemy("\u8D1D\u8005", BattleGamblerPassiveService.GamblerEnemyId, 6);

        GrantAndSelectTemporarySkills(ally, CreateSkill("\u5343\u672F"), 0);

        Assert.IsFalse(BattleGamblerPassiveService.TryApplyPlayerGambleClashWinDamage(ally, gambler, out int dealtDamage, null));
        Assert.AreEqual(0, dealtDamage);
        Assert.AreEqual(6, gambler.CurrentHP);

        ally.ClearTemporarySkills();
        GrantAndSelectTemporarySkills(ally, CreateSkill("\u65A9\u51FB"), 0);

        Assert.IsFalse(BattleGamblerPassiveService.TryApplyPlayerGambleClashWinDamage(ally, gambler, out dealtDamage, null));
        Assert.AreEqual(0, dealtDamage);
        Assert.AreEqual(6, gambler.CurrentHP);
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

    private EnemySkill_SO CreateSkill(string skillName)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = skillName;
        createdObjects.Add(skill);
        return skill;
    }

    private static void GrantAndSelectTemporarySkills(BattleAllyController ally, EnemySkill_SO skill, int skillIndex)
    {
        ally.GrantTemporarySkill(skill);
        Assert.IsTrue(ally.SetSelectedSkillIndex(skillIndex));
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
