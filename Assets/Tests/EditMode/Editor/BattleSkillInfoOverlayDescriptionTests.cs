using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleSkillInfoOverlayDescriptionTests
{
    private GameObject overlayObject;
    private BattleSkillInfoOverlay overlay;
    private readonly System.Collections.Generic.List<Object> createdObjects = new System.Collections.Generic.List<Object>();

    [SetUp]
    public void SetUp()
    {
        overlayObject = new GameObject("BattleSkillInfoOverlayDescriptionTests");
        overlayObject.SetActive(false);
        overlay = overlayObject.AddComponent<BattleSkillInfoOverlay>();
    }

    [TearDown]
    public void TearDown()
    {
        if (overlayObject != null)
            Object.DestroyImmediate(overlayObject);

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object created = createdObjects[i];
            if (created != null)
                Object.DestroyImmediate(created);
        }

        createdObjects.Clear();
    }

    [Test]
    public void CompactSkillDetail_MergesConsumedStatusPowerModifierAndShowsCap()
    {
        BattleStatus_SO darkness = CreateStatus("darkness", "黑暗");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "暗流";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 5 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                powerModifiers = new[]
                {
                    new EnemySkill_SO.ConditionalPowerModifierEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold,
                        targetStatusData = darkness,
                        requiredStatusStacks = 1,
                        powerBonusPerStep = 1,
                        maxPowerBonus = 2
                    }
                },
                statusStackOperationEffects = new[]
                {
                    new EnemySkill_SO.StatusStackOperationEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = darkness,
                        operationType = EnemySkill_SO.StatusStackOperationType.ReduceStacks,
                        stackAmount = 1,
                        randomTargetCount = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);

        Assert.That(detail, Does.Contain("每消耗"));
        Assert.That(detail, Does.Contain("敌人"));
        Assert.That(detail, Does.Contain("黑暗"));
        Assert.That(detail, Does.Contain("骰子威力"));
        Assert.That(detail, Does.Contain("+1"));
        Assert.That(detail, Does.Contain("最多"));
        Assert.That(detail, Does.Contain("+2"));
        Assert.That(detail, Does.Not.Contain("每达成一次条件"));
        Assert.That(detail, Does.Not.Contain("减少"));
    }

    [Test]
    public void CompactSkillDetail_MergesConsumedStatusDelayedStatusEffect()
    {
        BattleStatus_SO darkness = CreateStatus("darkness", "\u9ed1\u6697");
        BattleStatus_SO doom = CreateStatus("doom", "\u5384\u8fd0");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u566c\u6697";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 6 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                statusEffects = new[]
                {
                    new EnemySkill_SO.StatusEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = doom,
                        applyTiming = EnemySkill_SO.StatusApplyTiming.NextRoundStart,
                        applyAfterRoundStarts = 1,
                        applyRepeatCount = 1,
                        stackAmount = 1,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast,
                        conditionStatusData = darkness,
                        requiredConditionStatusStacks = 3
                    }
                },
                statusStackOperationEffects = new[]
                {
                    new EnemySkill_SO.StatusStackOperationEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = darkness,
                        operationType = EnemySkill_SO.StatusStackOperationType.ReduceStacks,
                        stackAmount = 3,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast,
                        conditionStatusData = darkness,
                        requiredConditionStatusStacks = 3
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);

        Assert.That(detail, Does.Contain("\u6d88\u8017"));
        Assert.That(detail, Does.Contain("\u654c\u4eba"));
        Assert.That(detail, Does.Contain("3"));
        Assert.That(detail, Does.Contain("\u9ed1\u6697"));
        Assert.That(detail, Does.Contain("\u4e0b\u56de\u5408"));
        Assert.That(detail, Does.Contain("1"));
        Assert.That(detail, Does.Contain("\u5384\u8fd0"));
        Assert.That(detail, Does.Not.Contain("\u51cf\u5c11"));
        Assert.That(detail, Does.Not.Contain("\u82e5"));
    }

    [Test]
    public void CompactSkillDetail_ShowsScalingRangeModifierCap()
    {
        BattleStatus_SO current = CreateStatus("current", "\u7535\u6d41");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u8d85\u5bfc";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 6 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                rangeModifiers = new[]
                {
                    new EnemySkill_SO.ConditionalDiceRangeModifierEntry
                    {
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold,
                        targetStatusData = current,
                        requiredStatusStacks = 1,
                        minPowerBonus = 1,
                        minPowerBonusCap = 5
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);

        Assert.That(detail, Does.Contain("\u76ee\u6807\u6bcf\u62e5\u6709"));
        Assert.That(detail, Does.Contain("1"));
        Assert.That(detail, Does.Contain("\u7535\u6d41"));
        Assert.That(detail, Does.Contain("\u6bcf\u8fbe\u6210\u4e00\u6b21\u6761\u4ef6"));
        Assert.That(detail, Does.Contain("\u6700\u5c0f\u5a01\u529b"));
        Assert.That(detail, Does.Contain("+1"));
        Assert.That(detail, Does.Contain("\u6700\u591a"));
        Assert.That(detail, Does.Contain("+5"));
    }

    private string InvokeCompactSkillHoverDetail(EnemySkill_SO skill)
    {
        MethodInfo method = typeof(BattleSkillInfoOverlay).GetMethod(
            "BuildCompactSkillHoverDetail",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        return (string)method.Invoke(overlay, new object[] { skill, true });
    }

    private BattleStatus_SO CreateStatus(string id, string displayName)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        createdObjects.Add(status);
        SetPrivateSerializedField(status, "statusId", id);
        SetPrivateSerializedField(status, "displayName", displayName);
        return status;
    }

    private static void SetPrivateSerializedField(object target, string fieldName, string value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(target, value);
    }
}
