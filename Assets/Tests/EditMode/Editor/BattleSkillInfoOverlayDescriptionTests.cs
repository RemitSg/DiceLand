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
