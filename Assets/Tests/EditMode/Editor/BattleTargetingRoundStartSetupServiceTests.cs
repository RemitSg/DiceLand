using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleTargetingRoundStartSetupServiceTests
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
    public void PrepareAllies_SkipsDeadAlliesAndPreparesLivingAllies()
    {
        EnemySkill_SO expectedSkill = CreateSkill("Ally Skill");
        BattleAllyController livingAlly = CreateAlly("LivingAlly", currentHp: 10, speed: 4, expectedSkill);
        BattleAllyController deadAlly = CreateAlly("DeadAlly", currentHp: 0, speed: 4, expectedSkill);
        int roundStartRelicCalls = 0;

        List<string> logs = BattleTargetingRoundStartSetupService.PrepareAllies(
            new[] { null, livingAlly, deadAlly },
            speedMin: 1,
            speedMax: 9,
            currentRound: 3,
            applyRoundStartRelics: ally =>
            {
                roundStartRelicCalls++;
                Assert.AreSame(livingAlly, ally);
            });

        Assert.AreEqual(1, roundStartRelicCalls);
        Assert.AreEqual(4, livingAlly.Speed);
        Assert.AreSame(expectedSkill, livingAlly.SelectedSkill);
        Assert.IsNull(deadAlly.SelectedSkill);
        Assert.AreEqual(1, logs.Count);
        StringAssert.Contains("LivingAlly", logs[0]);
        StringAssert.Contains("4", logs[0]);
        StringAssert.Contains("10/10", logs[0]);
    }

    [Test]
    public void PrepareEnemies_SkipsDeadEnemiesAndPreparesLivingEnemies()
    {
        EnemySkill_SO expectedSkill = CreateSkill("Enemy Skill");
        BattleEnemyController livingEnemy = CreateEnemy("LivingEnemy", currentHp: 12, speed: 5, expectedSkill);
        BattleEnemyController deadEnemy = CreateEnemy("DeadEnemy", currentHp: 0, speed: 5, expectedSkill);

        List<string> logs = BattleTargetingRoundStartSetupService.PrepareEnemies(
            new[] { null, livingEnemy, deadEnemy },
            currentRound: 4);

        Assert.AreEqual(5, livingEnemy.Speed);
        Assert.AreSame(expectedSkill, livingEnemy.SelectedSkill);
        Assert.IsNull(deadEnemy.SelectedSkill);
        Assert.AreEqual(1, logs.Count);
        StringAssert.Contains("LivingEnemy", logs[0]);
        StringAssert.Contains("5", logs[0]);
        StringAssert.Contains("12/12", logs[0]);
    }

    [Test]
    public void PrepareMethods_HandleNullCollections()
    {
        Assert.AreEqual(0, BattleTargetingRoundStartSetupService.PrepareAllies(null, 1, 2, 1, null).Count);
        Assert.AreEqual(0, BattleTargetingRoundStartSetupService.PrepareEnemies(null, 1).Count);
    }

    private BattleAllyController CreateAlly(string name, int currentHp, int speed, EnemySkill_SO skill)
    {
        GameObject go = CreateGameObject(name);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        Weapon_SO weapon = CreateWeapon($"{name} Weapon", skill);
        CharacterData_SO character = CreateCharacter(name, speed);
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            weaponData = weapon,
            level = 1,
            maxHP = 10,
            currentHP = currentHp
        });
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int currentHp, int speed, EnemySkill_SO skill)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(CreateEnemyData(name, currentHp > 0 ? currentHp : 12, speed, skill));
        if (currentHp <= 0)
            enemy.TakeDamage(enemy.CurrentHP);
        return enemy;
    }

    private CharacterData_SO CreateCharacter(string name, int speed)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        character.minSpeed = speed;
        character.maxSpeed = speed;
        createdObjects.Add(character);
        return character;
    }

    private EnemyData_SO CreateEnemyData(string name, int hp, int speed, EnemySkill_SO skill)
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = hp;
        enemyData.minSpeed = speed;
        enemyData.maxSpeed = speed;
        enemyData.baseSpeedButtonCount = 1;
        enemyData.skillPool = new List<EnemySkill_SO> { skill };
        createdObjects.Add(enemyData);
        return enemyData;
    }

    private Weapon_SO CreateWeapon(string name, EnemySkill_SO skill)
    {
        Weapon_SO weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        weapon.weaponName = name;
        weapon.skillPool = new[] { skill };
        createdObjects.Add(weapon);
        return weapon;
    }

    private EnemySkill_SO CreateSkill(string name)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.diceCount = 1;
        skill.minPower = 1;
        skill.maxPower = 1;
        skill.allowedEnemyStages = new List<int> { 1 };
        createdObjects.Add(skill);
        return skill;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }
}
