using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleTargetingPhaseStartEffectServiceTests
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
    public void ApplyPendingAllyStatusGrants_AppliesOnceAndMarksEncounter()
    {
        BattleStatus_SO status = CreateStatus();
        BattleEncounterData encounter = new BattleEncounterData
        {
            pendingBattleStartAllyStatusGrants = new List<PendingBattleStatusGrant>
            {
                new PendingBattleStatusGrant { statusData = status, stacks = 2 }
            }
        };
        BattleAllyController livingAlly = CreateAlly("LivingAlly", 10);
        BattleAllyController deadAlly = CreateAlly("DeadAlly", 0);
        List<string> logs = new List<string>();

        bool applied = BattlePendingBattleStartAllyStatusGrantService.Apply(
            encounter,
            new[] { livingAlly, deadAlly },
            (ally, grant) => logs.Add($"{ally.gameObject.name}:{grant.stacks}"));

        Assert.IsTrue(applied);
        Assert.IsTrue(encounter.battleStartAllyStatusGrantsApplied);
        Assert.AreEqual(2, livingAlly.GetStatusStacks(status));
        Assert.AreEqual(0, deadAlly.GetStatusStacks(status));
        CollectionAssert.AreEqual(new[] { "LivingAlly:2" }, logs);

        bool appliedAgain = BattlePendingBattleStartAllyStatusGrantService.Apply(
            encounter,
            new[] { livingAlly },
            (ally, grant) => logs.Add("unexpected"));

        Assert.IsFalse(appliedAgain);
        Assert.AreEqual(2, livingAlly.GetStatusStacks(status));
        CollectionAssert.AreEqual(new[] { "LivingAlly:2" }, logs);
    }

    [Test]
    public void DispatchBattleStartSkillGrants_ClearsAllAlliesAndGrantsOnlyFromLivingEnemiesToLivingAllies()
    {
        EnemySkill_SO oldSkill = CreateSkill("Old");
        EnemySkill_SO grantedSkill = CreateSkill("Granted");
        BattleAllyController livingAlly = CreateAlly("LivingAlly", 10);
        BattleAllyController deadAlly = CreateAlly("DeadAlly", 0);
        livingAlly.GrantTemporarySkill(oldSkill, true);
        BattleEnemyController livingEnemy = CreateEnemy("LivingEnemy", 10, grantedSkill);
        BattleEnemyController deadEnemy = CreateEnemy("DeadEnemy", 0, CreateSkill("DeadEnemySkill"));
        List<string> logs = new List<string>();

        BattleEnemyBattleStartSkillGrantService.Dispatch(
            new[] { livingEnemy, deadEnemy },
            new[] { livingAlly, deadAlly },
            (enemy, skill) => logs.Add($"{enemy.gameObject.name}:{skill.skillName}"));

        livingAlly.SelectDefaultSkill();
        deadAlly.SelectDefaultSkill();

        Assert.AreSame(grantedSkill, livingAlly.SelectedSkill);
        Assert.IsNull(deadAlly.SelectedSkill);
        CollectionAssert.AreEqual(new[] { "LivingEnemy:Granted" }, logs);
    }

    [Test]
    public void DispatchEnemyBattleStartStatusGains_FiltersInvalidEnemiesAndTransitionsOnlyWhenStatusApplied()
    {
        BattleEnemyController validEnemy = CreateEnemy("ValidEnemy", 10, CreateSkill("Skill"));
        BattleEnemyController deadEnemy = CreateEnemy("DeadEnemy", 0, CreateSkill("Dead"));
        List<string> events = new List<string>();

        BattleEnemyTargetingStatusGainPhaseService.DispatchBattleStartStatusGains(
            new[] { null, validEnemy, deadEnemy },
            enemy =>
            {
                events.Add($"gain:{enemy.gameObject.name}");
                return true;
            },
            enemy => events.Add($"transition:{enemy.gameObject.name}"));

        CollectionAssert.AreEqual(new[] { "gain:ValidEnemy", "transition:ValidEnemy" }, events);
    }

    [Test]
    public void DispatchEnemyRepeatRoundStartEffects_KeepsStatusCustomTransitionOrderAndSkipsRoundOne()
    {
        BattleEnemyController enemy = CreateEnemy("Enemy", 10, CreateSkill("Skill"));
        List<string> events = new List<string>();

        BattleEnemyTargetingStatusGainPhaseService.DispatchRepeatRoundStartEffects(
            1,
            new[] { enemy },
            _ => true,
            _ => events.Add("custom"),
            _ => events.Add("transition"));

        Assert.AreEqual(0, events.Count);

        BattleEnemyTargetingStatusGainPhaseService.DispatchRepeatRoundStartEffects(
            2,
            new[] { enemy },
            target =>
            {
                events.Add($"gain:{target.gameObject.name}");
                return true;
            },
            target => events.Add($"custom:{target.gameObject.name}"),
            target => events.Add($"transition:{target.gameObject.name}"));

        CollectionAssert.AreEqual(new[] { "gain:Enemy", "custom:Enemy", "transition:Enemy" }, events);
    }

    [Test]
    public void DispatchBattleSetupRelics_ReturnsOnlyFirstRoundExtraButtonsAndFlushesFeedback()
    {
        BattleEncounterData encounter = CreateEncounterWithRelic();
        RelicExecutionContext context = new RelicExecutionContext { additionalTemporaryAllySpeedButtons = 2 };
        List<string> events = new List<string>();

        int firstRoundExtra = BattleTargetingPhaseRelicService.DispatchBattleSetup(
            encounter,
            currentRound: 1,
            null,
            null,
            (activeEncounter, round, allies, enemies) =>
            {
                events.Add($"setup:{round}");
                return context;
            },
            dispatchedContext => events.Add($"flush:{dispatchedContext.additionalTemporaryAllySpeedButtons}"));

        int laterRoundExtra = BattleTargetingPhaseRelicService.DispatchBattleSetup(
            encounter,
            currentRound: 2,
            null,
            null,
            (activeEncounter, round, allies, enemies) =>
            {
                events.Add($"setup:{round}");
                return context;
            },
            dispatchedContext => events.Add($"flush:{dispatchedContext.additionalTemporaryAllySpeedButtons}"));

        Assert.AreEqual(2, firstRoundExtra);
        Assert.AreEqual(0, laterRoundExtra);
        CollectionAssert.AreEqual(new[] { "setup:1", "flush:2", "setup:2", "flush:2" }, events);
    }

    [Test]
    public void DispatchRoundStartRelics_RestoresSharedEnergyBeforeFlushingFeedback()
    {
        BattleEncounterData encounter = CreateEncounterWithRelic();
        RelicExecutionContext context = new RelicExecutionContext { additionalSharedEnergyRecovery = 3 };
        List<string> events = new List<string>();

        BattleTargetingPhaseRelicService.DispatchRoundStart(
            encounter,
            currentRound: 5,
            null,
            null,
            (activeEncounter, round, allies, enemies) =>
            {
                events.Add($"round:{round}");
                return context;
            },
            amount => events.Add($"restore:{amount}"),
            dispatchedContext => events.Add($"flush:{dispatchedContext.additionalSharedEnergyRecovery}"));

        CollectionAssert.AreEqual(new[] { "round:5", "restore:3", "flush:3" }, events);
    }

    private BattleAllyController CreateAlly(string name, int currentHp)
    {
        GameObject go = CreateGameObject(name);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = CreateCharacter(name),
            weaponData = CreateWeapon($"{name} Weapon", null),
            level = 1,
            maxHP = 10,
            currentHP = currentHp
        });
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int currentHp, EnemySkill_SO grantSkill)
    {
        GameObject go = CreateGameObject(name);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        EnemyData_SO data = ScriptableObject.CreateInstance<EnemyData_SO>();
        data.enemyName = name;
        data.baseHP = 10;
        data.minSpeed = 1;
        data.maxSpeed = 1;
        data.baseSpeedButtonCount = 1;
        data.skillPool = new List<EnemySkill_SO> { grantSkill };
        data.battleStartGrantedAllySkills = new List<EnemyData_SO.BattleStartAllySkillGrantEntry>
        {
            new EnemyData_SO.BattleStartAllySkillGrantEntry
            {
                skill = grantSkill,
                replaceWeaponSkills = true
            }
        };
        createdObjects.Add(data);
        enemy.ApplyEnemyData(data);
        if (currentHp <= 0)
            enemy.TakeDamage(enemy.CurrentHP);
        return enemy;
    }

    private BattleStatus_SO CreateStatus()
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        createdObjects.Add(status);
        return status;
    }

    private static BattleEncounterData CreateEncounterWithRelic()
    {
        return new BattleEncounterData
        {
            relicInventory = new List<RunRelicState>
            {
                new RunRelicState { amount = 1 }
            }
        };
    }

    private CharacterData_SO CreateCharacter(string name)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        createdObjects.Add(character);
        return character;
    }

    private Weapon_SO CreateWeapon(string name, EnemySkill_SO skill)
    {
        Weapon_SO weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        weapon.weaponName = name;
        weapon.skillPool = skill != null ? new[] { skill } : null;
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
