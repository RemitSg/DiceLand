using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleJinGuanSwordMechanicsTests
{
    private const EnemySkill_SO.PowerModifierCondition SelfHpRatioBelowOrEqualCondition =
        (EnemySkill_SO.PowerModifierCondition)15;
    private const EnemySkill_SO.BuiltInEffectType QueueNextDicePowerBonusEffect =
        (EnemySkill_SO.BuiltInEffectType)4;
    private const EnemySkill_SO.BuiltInEffectType DamageByTargetStatusStacksDividedEffect =
        (EnemySkill_SO.BuiltInEffectType)5;

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
    public void BuiltInCondition_SelfHpRatioBelowOrEqual_UsesCurrentHealthPercent()
    {
        EnemySkill_SO skill = CreateSkill("Half Hp Condition Test");
        BattleAllyController ally = CreateAlly("Low Hp Ally", 10, 4, skill);
        BattleEnemyController enemy = CreateEnemy("Target Enemy", 10, skill);
        EnemySkill_SO.BuiltInEffectEntry effect = new EnemySkill_SO.BuiltInEffectEntry
        {
            useCondition = true,
            condition = SelfHpRatioBelowOrEqualCondition,
            requiredConditionStatusStacks = 50
        };

        Assert.IsTrue(InvokeAllyBuiltInCondition(ally, enemy, effect));

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = CreateCharacter("High Hp Ally", 10),
            maxHP = 10,
            currentHP = 6
        });

        Assert.IsFalse(InvokeAllyBuiltInCondition(ally, enemy, effect));
    }

    [Test]
    public void CompactSkillDetail_DescribesQueuedNextDicePowerBonus()
    {
        GameObject overlayObject = new GameObject("JinGuan Overlay");
        createdObjects.Add(overlayObject);
        overlayObject.SetActive(false);
        BattleSkillInfoOverlay overlay = overlayObject.AddComponent<BattleSkillInfoOverlay>();

        EnemySkill_SO skill = CreateSkill("Next Dice Bonus Test");
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                builtInEffects = new[]
                {
                    new EnemySkill_SO.BuiltInEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        effectType = QueueNextDicePowerBonusEffect,
                        amount = 2
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(overlay, skill);

        Assert.That(detail, Does.Contain("下一个骰子"));
        Assert.That(detail, Does.Contain("+2"));
    }

    [Test]
    public void QueuedNextDicePowerBonus_IncreasesSingleAttackHitPowerNotOnlyClashPower()
    {
        GameObject buttonObject = new GameObject("BattleButton Probe");
        createdObjects.Add(buttonObject);
        BattleButton battleButton = buttonObject.AddComponent<BattleButton>();
        EnemySkill_SO skill = CreateSkill("Queued Overall Power Test");
        skill.minPower = 3;
        skill.maxPower = 3;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 3 }
        };
        BattleAllyController ally = CreateAlly("Queued Ally", 10, 10, skill);
        BattleEnemyController enemy = CreateEnemy("Queued Enemy", 10, skill);

        FieldInfo pendingField = typeof(BattleButton).GetField(
            "pendingAllyNextDicePowerBonus",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(pendingField);
        pendingField.SetValue(battleButton, 2);

        object diceInput = InvokePrepareAllySingleAttackDice(battleButton, ally, enemy, 0);
        object attackResult = diceInput.GetType().GetField("AttackResult").GetValue(diceInput);
        int finalPower = (int)attackResult.GetType().GetField("FinalPower").GetValue(attackResult);
        int hitPower = (int)attackResult.GetType().GetField("HitPower").GetValue(attackResult);
        int hitPowerModifier = (int)attackResult.GetType().GetField("HitPowerModifier").GetValue(attackResult);

        Assert.AreEqual(5, finalPower);
        Assert.AreEqual(5, hitPower);
        Assert.AreEqual(2, hitPowerModifier);
    }

    [Test]
    public void QueuedNextDicePowerBonus_IncreasesClashHitPowerNotOnlyClashComparison()
    {
        BattleDicePowerPreparation allyPower = BattleDicePowerPreparationService.Resolve(
            basePower: 3,
            statusPowerModifier: 0,
            relicPowerBonus: 0,
            skillUsePowerBonus: 2,
            actionPowerBonus: 0);
        BattleDicePowerPreparation enemyPower = BattleDicePowerPreparationService.Resolve(
            basePower: 1,
            statusPowerModifier: 0,
            relicPowerBonus: 0,
            skillUsePowerBonus: 0,
            actionPowerBonus: 0);

        BattleClashResult result = BattleClashResultService.Resolve(
            allyPower: allyPower.FinalPower,
            enemyPower: enemyPower.FinalPower,
            allyDefenseSkill: false,
            enemyDefenseSkill: false,
            allyFollowUpBasePower: allyPower.FollowUpBasePower,
            enemyFollowUpBasePower: enemyPower.FollowUpBasePower,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        BattleClashImpactPlan impactPlan = BattleClashImpactPlanService.Resolve(
            result,
            allyPower.FinalPower,
            enemyPower.FinalPower,
            allyPower.BonusPower,
            enemyPower.BonusPower,
            allyPower.SkillUseBonus,
            enemyPower.SkillUseBonus,
            allyWinAttackBonus: 0,
            enemyWinAttackBonus: 0);

        Assert.AreEqual(5, allyPower.FinalPower);
        Assert.AreEqual(5, result.AllyHitPower);
        Assert.AreEqual(5, impactPlan.AllyHitPower);
        Assert.AreEqual(2, impactPlan.AllyHitPowerModifier);
    }

    [Test]
    public void AllyOnHitQueuedNextDicePowerEffect_AddsPendingBonusForNextDie()
    {
        GameObject buttonObject = new GameObject("BattleButton OnHit Probe");
        createdObjects.Add(buttonObject);
        BattleButton battleButton = buttonObject.AddComponent<BattleButton>();
        EnemySkill_SO skill = CreateTwoDiceQueueSkill();
        BattleAllyController ally = CreateAlly("Queue Source Ally", 10, 10, skill);
        BattleEnemyController enemy = CreateEnemy("Queue Target Enemy", 10, skill);
        SetActiveRoundCombatants(battleButton, new[] { ally }, new[] { enemy });

        InvokeAllyTriggeredEffects(
            battleButton,
            ally,
            enemy,
            diceIndex: 0,
            EnemySkill_SO.DiceEffectTrigger.OnHit,
            dealtDamage: 3);

        FieldInfo pendingField = typeof(BattleButton).GetField(
            "pendingAllyNextDicePowerBonus",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(pendingField);
        Assert.AreEqual(2, (int)pendingField.GetValue(battleButton));
    }

    [Test]
    public void BuiltInEligibility_AllowsQueuedNextDicePowerBonusWhenAmountIsNonZero()
    {
        EnemySkill_SO.BuiltInEffectEntry effect = new EnemySkill_SO.BuiltInEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            effectType = QueueNextDicePowerBonusEffect,
            amount = 2
        };

        Assert.IsTrue(BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnHit));

        effect.amount = 0;

        Assert.IsFalse(BattleEnemyBuiltInEffectEligibilityService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnHit));
    }

    [Test]
    public void AllyStatusScaledDamageBuiltIn_DealsTargetStatusStacksDividedDamage()
    {
        GameObject buttonObject = new GameObject("BattleButton Status Damage Probe");
        createdObjects.Add(buttonObject);
        BattleButton battleButton = buttonObject.AddComponent<BattleButton>();
        BattleStatus_SO burn = CreateStatus("burn", "燃烧");
        EnemySkill_SO skill = CreateSkill("Status Scaled Damage Test");
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                builtInEffects = new[]
                {
                    new EnemySkill_SO.BuiltInEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
                        effectType = DamageByTargetStatusStacksDividedEffect,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        targetStatusData = burn,
                        requiredConditionStatusStacks = 2,
                        amount = 1
                    }
                }
            }
        };
        BattleAllyController ally = CreateAlly("Status Damage Ally", 10, 10, skill);
        BattleEnemyController enemy = CreateEnemy("Burning Target", 10, skill);
        enemy.AddStatus(burn, burn.StatusId, burn.DisplayName, 5);
        SetActiveRoundCombatants(battleButton, new[] { ally }, new[] { enemy });

        InvokeAllyTriggeredEffects(
            battleButton,
            ally,
            enemy,
            diceIndex: 0,
            EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
            dealtDamage: 0);

        Assert.AreEqual(8, enemy.CurrentHP);
    }

    [Test]
    public void CompactSkillDetail_DescribesStatusScaledDamageBuiltIn()
    {
        GameObject overlayObject = new GameObject("Status Damage Overlay");
        createdObjects.Add(overlayObject);
        overlayObject.SetActive(false);
        BattleSkillInfoOverlay overlay = overlayObject.AddComponent<BattleSkillInfoOverlay>();
        BattleStatus_SO burn = CreateStatus("burn", "燃烧");
        EnemySkill_SO skill = CreateSkill("Status Scaled Damage Detail");
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                builtInEffects = new[]
                {
                    new EnemySkill_SO.BuiltInEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
                        effectType = DamageByTargetStatusStacksDividedEffect,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        targetStatusData = burn,
                        requiredConditionStatusStacks = 2,
                        amount = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(overlay, skill);

        Assert.That(detail, Does.Contain("燃烧"));
        Assert.That(detail, Does.Contain("层数 /"));
    }

    private bool InvokeAllyBuiltInCondition(
        BattleAllyController ally,
        BattleEnemyController enemy,
        EnemySkill_SO.BuiltInEffectEntry effect)
    {
        MethodInfo method = typeof(BattleButton).GetMethod(
            "IsAllyBuiltInEffectConditionMet",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        return (bool)method.Invoke(
            null,
            new object[]
            {
                ally,
                enemy,
                effect,
                new List<BattleEnemyController> { enemy },
                new List<BattleAllyController> { ally }
            });
    }

    private string InvokeCompactSkillHoverDetail(BattleSkillInfoOverlay overlay, EnemySkill_SO skill)
    {
        MethodInfo method = typeof(BattleSkillInfoOverlay).GetMethod(
            "BuildCompactSkillHoverDetail",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(EnemySkill_SO), typeof(bool) },
            null);

        Assert.IsNotNull(method);
        return (string)method.Invoke(overlay, new object[] { skill, true });
    }

    private object InvokePrepareAllySingleAttackDice(
        BattleButton battleButton,
        BattleAllyController ally,
        BattleEnemyController enemy,
        int diceIndex)
    {
        MethodInfo method = typeof(BattleButton).GetMethod(
            "PrepareAllySingleAttackDice",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        return method.Invoke(battleButton, new object[] { ally, enemy, diceIndex });
    }

    private void InvokeAllyTriggeredEffects(
        BattleButton battleButton,
        BattleAllyController ally,
        BattleEnemyController enemy,
        int diceIndex,
        EnemySkill_SO.DiceEffectTrigger trigger,
        int dealtDamage)
    {
        MethodInfo method = typeof(BattleButton).GetMethod(
            "ApplyAllyTriggeredEffects",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        method.Invoke(battleButton, new object[] { ally, enemy, diceIndex, trigger, dealtDamage });
    }

    private void SetActiveRoundCombatants(
        BattleButton battleButton,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        MethodInfo method = typeof(BattleButton).GetMethod(
            "SetActiveRoundCombatants",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        method.Invoke(battleButton, new object[] { allies, enemies });
    }

    private EnemySkill_SO CreateTwoDiceQueueSkill()
    {
        EnemySkill_SO skill = CreateSkill("Two Dice Queue Skill");
        skill.diceCount = 2;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 3 },
            new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 3 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                builtInEffects = new[]
                {
                    new EnemySkill_SO.BuiltInEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        effectType = QueueNextDicePowerBonusEffect,
                        amount = 2
                    }
                }
            },
            new EnemySkill_SO.DiceEffectSettings()
        };
        return skill;
    }

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp, EnemySkill_SO skill)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = CreateCharacter(name, maxHp),
            maxHP = maxHp,
            currentHP = currentHp
        });
        ally.GrantTemporarySkill(skill, true);
        Assert.IsTrue(ally.SetSelectedSkillIndex(0));
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int hp, EnemySkill_SO skill)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdObjects.Add(enemyData);
        enemyData.enemyName = name;
        enemyData.baseHP = hp;
        enemyData.skillPool = new List<EnemySkill_SO> { skill };

        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(enemyData);
        enemy.SelectRandomSkill(1);
        return enemy;
    }

    private CharacterData_SO CreateCharacter(string name, int baseHp)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        createdObjects.Add(character);
        character.characterName = name;
        character.baseHP = baseHp;
        character.minSpeed = 1;
        character.maxSpeed = 3;
        return character;
    }

    private EnemySkill_SO CreateSkill(string name)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = name;
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 4 }
        };
        return skill;
    }

    private BattleStatus_SO CreateStatus(string statusId, string displayName)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        createdObjects.Add(status);
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        return status;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
