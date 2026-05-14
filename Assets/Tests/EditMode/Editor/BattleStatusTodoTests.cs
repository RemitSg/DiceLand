using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleStatusTodoTests
{
    private const BattleStatus_SO.StatusBehaviorType PoisonBehaviorType = (BattleStatus_SO.StatusBehaviorType)12;
    private const BattleStatus_SO.StatusBehaviorType LuckBehaviorType = (BattleStatus_SO.StatusBehaviorType)13;
    private const BattleStatus_SO.StatusBehaviorType MisfortuneBehaviorType = (BattleStatus_SO.StatusBehaviorType)14;
    private const BattleStatus_SO.StatusBehaviorType SpeedButtonPenaltyBehaviorType = (BattleStatus_SO.StatusBehaviorType)15;
    private const BattleStatus_SO.StatusBehaviorType ShieldBehaviorType = (BattleStatus_SO.StatusBehaviorType)16;
    private const BattleStatus_SO.StackDecayMode LoseOneStackDecayMode = (BattleStatus_SO.StackDecayMode)3;

    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        BattleEncounterRuntime.Clear();

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object created = createdObjects[i];
            if (created != null)
                Object.DestroyImmediate(created);
        }

        createdObjects.Clear();
    }

    [Test]
    public void PoisonStatus_ReducesOnlyMaxPowerAndExpiresAtRoundEnd_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Poison Test", 2, 5);
        BattleStatus_SO poison = CreateStatus(
            "poison",
            "中毒",
            PoisonBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.LoseAll);

        BattleAllyController ally = CreateAlly("Poisoned Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Poisoned Enemy", 12, skill);

        ally.AddStatus(poison, poison.StatusId, poison.DisplayName, 2);
        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 2);

        EnemySkill_SO.DicePowerRange allyRange = ally.GetDicePowerRange(0);
        EnemySkill_SO.DicePowerRange enemyRange = enemy.GetDicePowerRange(0);

        Assert.AreEqual(2, allyRange.minPower);
        Assert.AreEqual(3, allyRange.maxPower);
        Assert.AreEqual(2, enemyRange.minPower);
        Assert.AreEqual(3, enemyRange.maxPower);

        Assert.IsTrue(ally.ConsumeRoundEndStatusEffects());
        Assert.IsTrue(enemy.ConsumeRoundEndStatusEffects());
        Assert.AreEqual(0, ally.GetStatusStacks(poison));
        Assert.AreEqual(0, enemy.GetStatusStacks(poison));
    }

    [Test]
    public void DisplayedDicePowerRange_IncludesColdAndPoisonPenalties_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Displayed Status Test", 2, 5);
        BattleStatus_SO cold = CreateStatus(
            "cold",
            "寒冷",
            BattleStatus_SO.StatusBehaviorType.MinPowerPenalty,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.LoseAll);
        BattleStatus_SO poison = CreateStatus(
            "poison",
            "中毒",
            BattleStatus_SO.StatusBehaviorType.MaxPowerPenalty,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.LoseAll);

        BattleAllyController ally = CreateAlly("Displayed Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Displayed Enemy", 12, skill);

        ally.AddStatus(cold, cold.StatusId, cold.DisplayName, 1);
        ally.AddStatus(poison, poison.StatusId, poison.DisplayName, 2);
        enemy.AddStatus(cold, cold.StatusId, cold.DisplayName, 1);
        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 2);

        EnemySkill_SO.DicePowerRange allyRange = ally.GetDisplayedDicePowerRange(skill, 0);
        EnemySkill_SO.DicePowerRange enemyRange = enemy.GetEffectiveDisplayedDicePowerRange(skill, 0);

        Assert.AreEqual(1, allyRange.minPower);
        Assert.AreEqual(3, allyRange.maxPower);
        Assert.AreEqual(1, enemyRange.minPower);
        Assert.AreEqual(3, enemyRange.maxPower);
    }

    [Test]
    public void LuckStatus_ForcesMaxRoll_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Lucky Test", 2, 5);
        BattleStatus_SO luck = CreateStatus(
            "luck",
            "幸运",
            LuckBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.LoseAll);

        BattleAllyController ally = CreateAlly("Lucky Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Lucky Enemy", 12, skill);

        ally.AddStatus(luck, luck.StatusId, luck.DisplayName, 1);
        enemy.AddStatus(luck, luck.StatusId, luck.DisplayName, 1);

        for (int i = 0; i < 12; i++)
        {
            Assert.AreEqual(5, ally.RollDice(0));
            Assert.AreEqual(5, enemy.RollDice(0));
        }
    }

    [Test]
    public void MisfortuneStatus_ForcesMinRoll_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Misfortune Test", 2, 5);
        BattleStatus_SO misfortune = CreateStatus(
            "misfortune",
            "厄运",
            MisfortuneBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.LoseAll);

        BattleAllyController ally = CreateAlly("Unlucky Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Unlucky Enemy", 12, skill);

        ally.AddStatus(misfortune, misfortune.StatusId, misfortune.DisplayName, 1);
        enemy.AddStatus(misfortune, misfortune.StatusId, misfortune.DisplayName, 1);

        for (int i = 0; i < 12; i++)
        {
            Assert.AreEqual(2, ally.RollDice(0));
            Assert.AreEqual(2, enemy.RollDice(0));
        }
    }

    [Test]
    public void LuckAndMisfortuneStatuses_LoseOneStackAtRoundEnd_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Fate Decay Test", 2, 5);
        BattleStatus_SO luck = CreateStatus(
            "luck",
            "骞歌繍",
            LuckBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: LoseOneStackDecayMode);
        BattleStatus_SO misfortune = CreateStatus(
            "misfortune",
            "鍘勮繍",
            MisfortuneBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: LoseOneStackDecayMode);

        BattleAllyController ally = CreateAlly("Fated Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Fated Enemy", 12, skill);

        ally.AddStatus(luck, luck.StatusId, luck.DisplayName, 2);
        ally.AddStatus(misfortune, misfortune.StatusId, misfortune.DisplayName, 2);
        enemy.AddStatus(luck, luck.StatusId, luck.DisplayName, 2);
        enemy.AddStatus(misfortune, misfortune.StatusId, misfortune.DisplayName, 2);

        Assert.IsTrue(ally.ConsumeRoundEndStatusEffects());
        Assert.IsTrue(enemy.ConsumeRoundEndStatusEffects());
        Assert.AreEqual(1, ally.GetStatusStacks(luck));
        Assert.AreEqual(1, ally.GetStatusStacks(misfortune));
        Assert.AreEqual(1, enemy.GetStatusStacks(luck));
        Assert.AreEqual(1, enemy.GetStatusStacks(misfortune));

        Assert.IsTrue(ally.ConsumeRoundEndStatusEffects());
        Assert.IsTrue(enemy.ConsumeRoundEndStatusEffects());
        Assert.AreEqual(0, ally.GetStatusStacks(luck));
        Assert.AreEqual(0, ally.GetStatusStacks(misfortune));
        Assert.AreEqual(0, enemy.GetStatusStacks(luck));
        Assert.AreEqual(0, enemy.GetStatusStacks(misfortune));
    }

    [Test]
    public void StunStatus_ReducesSpeedButtonsByStacksAndExpiresAtRoundEnd_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Stun Test", 2, 5);
        BattleStatus_SO stun = CreateStatus(
            "stun",
            "眩晕",
            SpeedButtonPenaltyBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.LoseAll);

        BattleAllyController ally = CreateAlly("Stunned Ally", 12, skill);
        ally.RegisterSpeedButtons(new[]
        {
            CreateSpeedButton(ally.transform, "Ally SpeedButton 1"),
            CreateSpeedButton(ally.transform, "Ally SpeedButton 2"),
            CreateSpeedButton(ally.transform, "Ally SpeedButton 3")
        });
        ally.SetInheritedExtraSpeedButtons(2);

        BattleEnemyController enemy = CreateEnemy("Stunned Enemy", 12, skill);
        enemy.enemyData.baseSpeedButtonCount = 3;
        enemy.RegisterSpeedButtons(new[]
        {
            CreateSpeedButton(enemy.transform, "Enemy SpeedButton 1"),
            CreateSpeedButton(enemy.transform, "Enemy SpeedButton 2"),
            CreateSpeedButton(enemy.transform, "Enemy SpeedButton 3")
        });

        Assert.AreEqual(3, ally.SpeedButtons.Count);
        Assert.AreEqual(3, enemy.SpeedButtons.Count);

        ally.AddStatus(stun, stun.StatusId, stun.DisplayName, 2);
        enemy.AddStatus(stun, stun.StatusId, stun.DisplayName, 2);

        Assert.AreEqual(1, ally.SpeedButtons.Count);
        Assert.AreEqual(1, enemy.SpeedButtons.Count);

        ally.AddStatus(stun, stun.StatusId, stun.DisplayName, 2);
        enemy.AddStatus(stun, stun.StatusId, stun.DisplayName, 2);

        Assert.AreEqual(0, ally.SpeedButtons.Count);
        Assert.AreEqual(0, enemy.SpeedButtons.Count);

        Assert.IsTrue(ally.ConsumeRoundEndStatusEffects());
        Assert.IsTrue(enemy.ConsumeRoundEndStatusEffects());
        Assert.AreEqual(0, ally.GetStatusStacks(stun));
        Assert.AreEqual(0, enemy.GetStatusStacks(stun));
        Assert.AreEqual(3, ally.SpeedButtons.Count);
        Assert.AreEqual(3, enemy.SpeedButtons.Count);
    }

    [Test]
    public void ShieldStatus_ConsumesStacksToAbsorbDamage_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Shield Test", 2, 5);
        BattleStatus_SO shield = CreateStatus(
            "shield",
            "护盾",
            ShieldBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.HalveRoundedDown);

        BattleAllyController ally = CreateAlly("Shielded Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Shielded Enemy", 12, skill);

        ally.AddStatus(shield, shield.StatusId, shield.DisplayName, 3);
        enemy.AddStatus(shield, shield.StatusId, shield.DisplayName, 3);

        Assert.AreEqual(2, ally.TakeDamage(5));
        Assert.AreEqual(10, ally.CurrentHP);
        Assert.AreEqual(0, ally.GetStatusStacks(shield));

        Assert.AreEqual(2, enemy.TakeDamage(5));
        Assert.AreEqual(10, enemy.CurrentHP);
        Assert.AreEqual(0, enemy.GetStatusStacks(shield));
    }

    [Test]
    public void ShieldStatus_LosesHalfStacksAtRoundEndRoundedDown()
    {
        EnemySkill_SO skill = CreateSkill("Shield Decay Test", 2, 5);
        BattleStatus_SO shield = CreateStatus(
            "shield",
            "护盾",
            ShieldBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.HalveRoundedDown);

        BattleAllyController ally = CreateAlly("Shielded Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Shielded Enemy", 12, skill);

        ally.AddStatus(shield, shield.StatusId, shield.DisplayName, 5);
        enemy.AddStatus(shield, shield.StatusId, shield.DisplayName, 5);

        Assert.IsTrue(ally.ConsumeRoundEndStatusEffects());
        Assert.IsTrue(enemy.ConsumeRoundEndStatusEffects());
        Assert.AreEqual(2, ally.GetStatusStacks(shield));
        Assert.AreEqual(2, enemy.GetStatusStacks(shield));
    }

    [Test]
    public void ResistanceStatus_BlocksNextNegativeStatusAndLosesOneStack_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Resistance Test", 2, 5);
        BattleStatus_SO resistance = CreateStatus(
            BattleStatusResistanceService.ResistanceStatusId,
            "抵抗",
            BattleStatus_SO.StatusBehaviorType.None,
            powerPerStack: 0,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.None);
        BattleStatus_SO burn = CreateStatus(
            "burn",
            "燃烧",
            BattleStatus_SO.StatusBehaviorType.DamageAtRoundStart,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.None);

        BattleAllyController ally = CreateAlly("Resistant Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Resistant Enemy", 12, skill);

        ally.AddStatus(resistance, resistance.StatusId, resistance.DisplayName, 2);
        enemy.AddStatus(resistance, resistance.StatusId, resistance.DisplayName, 2);

        Assert.IsFalse(ally.AddStatus(burn, burn.StatusId, burn.DisplayName, 3));
        Assert.IsFalse(enemy.AddStatus(burn, burn.StatusId, burn.DisplayName, 3));
        Assert.AreEqual(1, ally.GetStatusStacks(resistance));
        Assert.AreEqual(1, enemy.GetStatusStacks(resistance));
        Assert.AreEqual(0, ally.GetStatusStacks(burn));
        Assert.AreEqual(0, enemy.GetStatusStacks(burn));

        Assert.IsFalse(ally.AddStatus(burn, burn.StatusId, burn.DisplayName, 1));
        Assert.IsFalse(enemy.AddStatus(burn, burn.StatusId, burn.DisplayName, 1));
        Assert.AreEqual(0, ally.GetStatusStacks(resistance));
        Assert.AreEqual(0, enemy.GetStatusStacks(resistance));

        Assert.IsTrue(ally.AddStatus(burn, burn.StatusId, burn.DisplayName, 1));
        Assert.IsTrue(enemy.AddStatus(burn, burn.StatusId, burn.DisplayName, 1));
        Assert.AreEqual(1, ally.GetStatusStacks(burn));
        Assert.AreEqual(1, enemy.GetStatusStacks(burn));
    }

    [Test]
    public void ResistanceStatus_DoesNotBlockPositiveStatus_ForAlliesAndEnemies()
    {
        EnemySkill_SO skill = CreateSkill("Resistance Positive Test", 2, 5);
        BattleStatus_SO resistance = CreateStatus(
            BattleStatusResistanceService.ResistanceStatusId,
            "抵抗",
            BattleStatus_SO.StatusBehaviorType.None,
            powerPerStack: 0,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.None);
        BattleStatus_SO shield = CreateStatus(
            "shield",
            "护盾",
            ShieldBehaviorType,
            powerPerStack: 1,
            roundEndDecayMode: BattleStatus_SO.StackDecayMode.None);

        BattleAllyController ally = CreateAlly("Positive Ally", 12, skill);
        BattleEnemyController enemy = CreateEnemy("Positive Enemy", 12, skill);

        ally.AddStatus(resistance, resistance.StatusId, resistance.DisplayName, 1);
        enemy.AddStatus(resistance, resistance.StatusId, resistance.DisplayName, 1);

        Assert.IsTrue(ally.AddStatus(shield, shield.StatusId, shield.DisplayName, 2));
        Assert.IsTrue(enemy.AddStatus(shield, shield.StatusId, shield.DisplayName, 2));
        Assert.AreEqual(1, ally.GetStatusStacks(resistance));
        Assert.AreEqual(1, enemy.GetStatusStacks(resistance));
        Assert.AreEqual(2, ally.GetStatusStacks(shield));
        Assert.AreEqual(2, enemy.GetStatusStacks(shield));
    }

    private BattleAllyController CreateAlly(string name, int maxHp, EnemySkill_SO skill)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        RunPartyMemberState runtimeState = new RunPartyMemberState
        {
            characterData = CreateCharacter(name, maxHp),
            level = 1,
            experience = 0,
            maxHP = maxHp,
            currentHP = maxHp
        };
        ally.ApplyRuntimeState(runtimeState);
        ally.GrantTemporarySkill(skill, true);
        Assert.IsTrue(ally.SetSelectedSkillIndex(0));
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int hp, EnemySkill_SO skill)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = hp;
        enemyData.skillPool = new List<EnemySkill_SO> { skill };
        createdObjects.Add(enemyData);

        enemy.ApplyEnemyData(enemyData);
        enemy.SelectRandomSkill(1);
        return enemy;
    }

    private CharacterData_SO CreateCharacter(string name, int baseHp)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = baseHp;
        character.minSpeed = 1;
        character.maxSpeed = 3;
        createdObjects.Add(character);
        return character;
    }

    private EnemySkill_SO CreateSkill(string name, int minPower, int maxPower)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange
            {
                minPower = minPower,
                maxPower = maxPower
            }
        };
        createdObjects.Add(skill);
        return skill;
    }

    private SpeedButton CreateSpeedButton(Transform parent, string name)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        createdObjects.Add(buttonObject);
        return buttonObject.AddComponent<SpeedButton>();
    }

    private BattleStatus_SO CreateStatus(
        string statusId,
        string displayName,
        BattleStatus_SO.StatusBehaviorType behaviorType,
        int powerPerStack,
        BattleStatus_SO.StackDecayMode roundEndDecayMode)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        SetPrivateField(status, "behaviorType", behaviorType);
        SetPrivateField(status, "powerPerStack", powerPerStack);
        SetPrivateField(status, "roundEndDecayMode", roundEndDecayMode);
        createdObjects.Add(status);
        return status;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
