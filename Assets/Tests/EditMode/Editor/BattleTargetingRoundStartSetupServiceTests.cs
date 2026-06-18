using System.Collections.Generic;
using System.Reflection;
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
    public void PrepareEnemies_AppliesStatusDrivenStageTransitionBeforeSkillSelection()
    {
        BattleStatus_SO bleed = CreateStatus("bleed", "Bleed");
        EnemySkill_SO allySkill = CreateSkill("Ally Skill");
        EnemySkill_SO stageOneSkill = CreateSkill("Stage One Skill", allowedStage: 1);
        EnemySkill_SO stageTwoSkill = CreateSkill("Stage Two Skill", allowedStage: 2);
        BattleAllyController bleedingAlly = CreateAlly("Bleeding Ally", currentHp: 10, speed: 4, allySkill);
        BattleEnemyController enemy = CreateEnemyWithStatusStageRule(
            "CrimsonBroodmotherLikeEnemy",
            currentHp: 12,
            speed: 5,
            bleed,
            stageOneSkill,
            stageTwoSkill);
        bleedingAlly.AddStatus(bleed, bleed.StatusId, bleed.DisplayName, 3);

        List<string> logs = BattleTargetingRoundStartSetupService.PrepareEnemies(
            new[] { enemy },
            currentRound: 2,
            opposingAllies: new[] { bleedingAlly },
            applyStatusDrivenStageTransition: target =>
                BattleStatusDrivenStageTransitionDispatcher.Dispatch(
                    new[] { target },
                    new[] { bleedingAlly },
                    new[] { target },
                    null));

        Assert.AreEqual(1, enemy.CurrentStageIndex);
        Assert.AreSame(stageTwoSkill, enemy.SelectedSkill);
        Assert.AreEqual(1, logs.Count);
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

    private BattleEnemyController CreateEnemy(string name, int currentHp, int speed, params EnemySkill_SO[] skills)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(CreateEnemyData(name, currentHp > 0 ? currentHp : 12, speed, skills));
        if (currentHp <= 0)
            enemy.TakeDamage(enemy.CurrentHP);
        return enemy;
    }

    private BattleEnemyController CreateEnemyWithStatusStageRule(string name, int currentHp, int speed, BattleStatus_SO statusData, params EnemySkill_SO[] skills)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        EnemyData_SO enemyData = CreateEnemyData(name, currentHp > 0 ? currentHp : 12, speed, skills);
        enemyData.statusStageTransitionRules = new List<EnemyData_SO.StatusStageTransitionRule>
        {
            new EnemyData_SO.StatusStageTransitionRule
            {
                ruleName = "Wake on blood",
                statusData = statusData,
                targetMode = EnemyData_SO.StageStatusTargetMode.AnyUnit,
                comparisonType = EnemyData_SO.StageStatusComparisonType.GreaterOrEqual,
                stackThreshold = 3,
                targetStageIndex = 1,
                priority = 1
            }
        };
        enemy.ApplyEnemyData(enemyData);
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

    private EnemyData_SO CreateEnemyData(string name, int hp, int speed, EnemySkill_SO[] skills)
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = hp;
        enemyData.minSpeed = speed;
        enemyData.maxSpeed = speed;
        enemyData.baseSpeedButtonCount = 1;
        enemyData.stageSprites = new List<EnemyData_SO.EnemyStageEntry>
        {
            new EnemyData_SO.EnemyStageEntry { stageName = "Stage 1", speedButtonCountOverride = 1 },
            new EnemyData_SO.EnemyStageEntry { stageName = "Stage 2", speedButtonCountOverride = 1 }
        };
        enemyData.skillPool = new List<EnemySkill_SO>(skills);
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

    private EnemySkill_SO CreateSkill(string name, int allowedStage = 1)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.diceCount = 1;
        skill.minPower = 1;
        skill.maxPower = 1;
        skill.allowedEnemyStages = new List<int> { allowedStage };
        createdObjects.Add(skill);
        return skill;
    }

    private BattleStatus_SO CreateStatus(string statusId, string displayName)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        createdObjects.Add(status);
        return status;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }
}
