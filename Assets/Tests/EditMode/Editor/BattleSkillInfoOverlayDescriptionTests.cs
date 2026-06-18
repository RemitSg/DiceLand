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
    public void CompactSkillDetail_LabelsClashWinAsClashVictory()
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "测试技能";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 2 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                damageEffects = new[]
                {
                    new EnemySkill_SO.DamageEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnClashWin,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        damageAmount = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);

        Assert.That(detail, Does.Contain("拼点胜利时"));
        Assert.That(detail, Does.Not.Contain("造成伤害时"));
    }

    [Test]
    public void CompactSkillDetail_LabelsClashWinPowerModifierAsClashVictory()
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "测试技能";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 2 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                powerModifiers = new[]
                {
                    new EnemySkill_SO.ConditionalPowerModifierEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnClashWin,
                        powerBonus = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);

        Assert.That(detail, Does.Contain("拼点胜利时"));
        Assert.That(detail, Does.Not.Contain("造成伤害时"));
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
    public void CompactSkillDetail_MergesMarkedDelayedStatusBranches()
    {
        BattleStatus_SO mark = CreateStatus("target", "\u6807\u8bb0");
        BattleStatus_SO darkness = CreateStatus("darkness", "\u9ed1\u6697");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u68fa\u9489";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 5 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                statusEffects = new[]
                {
                    new EnemySkill_SO.StatusEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = mark,
                        applyTiming = EnemySkill_SO.StatusApplyTiming.NextRoundStart,
                        applyAfterRoundStarts = 1,
                        applyRepeatCount = 1,
                        stackAmount = 1,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksLessOrEqual,
                        conditionStatusData = mark,
                        requiredConditionStatusStacks = 0
                    },
                    new EnemySkill_SO.StatusEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = darkness,
                        applyTiming = EnemySkill_SO.StatusApplyTiming.NextRoundStart,
                        applyAfterRoundStarts = 1,
                        applyRepeatCount = 1,
                        stackAmount = 1,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast,
                        conditionStatusData = mark,
                        requiredConditionStatusStacks = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);

        Assert.That(detail, Does.Contain("\u547d\u4e2d\u65f6"));
        Assert.That(detail, Does.Contain("\u4e0b 1 \u4e2a\u56de\u5408\u5f00\u59cb\u65f6"));
        Assert.That(detail, Does.Contain("\u65bd\u52a0"));
        Assert.That(detail, Does.Contain("\u6807\u8bb0"));
        Assert.That(detail, Does.Contain("\u82e5"));
        Assert.That(detail, Does.Contain("\u5df2\u6709"));
        Assert.That(detail, Does.Contain("\u6539\u4e3a"));
        Assert.That(detail, Does.Contain("\u9ed1\u6697"));
        Assert.That(detail, Does.Not.Contain("\u81f3\u591a\u4e3a"));
        Assert.That(detail, Does.Not.Contain("\u62e5\u6709\u81f3\u5c11"));
    }

    [Test]
    public void CompactSkillDetail_StatusEffectShowsNegativeStatusTypeCondition()
    {
        BattleStatus_SO stun = CreateStatus("stun", "\u7729\u6655");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u8000\u5149\u7a81\u8fdb";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 4 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                statusEffects = new[]
                {
                    new EnemySkill_SO.StatusEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = stun,
                        applyTiming = EnemySkill_SO.StatusApplyTiming.NextRoundStart,
                        applyAfterRoundStarts = 1,
                        applyRepeatCount = 1,
                        stackAmount = 1,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast,
                        requiredConditionStatusStacks = 2
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);

        Assert.That(detail, Does.Contain("\u82e5"));
        Assert.That(detail, Does.Contain("\u76ee\u6807\u62e5\u6709\u81f3\u5c11"));
        Assert.That(detail, Does.Contain("2"));
        Assert.That(detail, Does.Contain("\u79cd\u8d1f\u9762\u72b6\u6001"));
        Assert.That(detail, Does.Contain("\u7729\u6655"));
    }

    [Test]
    public void CompactSkillDetail_MergesConsumedStatusMultipleGains()
    {
        BattleStatus_SO bleed = CreateStatus("bleed", "\u6d41\u8840");
        BattleStatus_SO temporaryStrength = CreateStatus("strength_temp", "\u4e34\u65f6\u5f3a\u58ee");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u56de\u8a93";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 3 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                healingEffects = new[]
                {
                    new EnemySkill_SO.HealingEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        targetMode = EnemySkill_SO.DamageTargetMode.Self,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
                        conditionStatusData = bleed,
                        requiredConditionStatusStacks = 1,
                        amountMode = EnemySkill_SO.HealAmountMode.FixedAmount,
                        healAmount = 2,
                        randomTargetCount = 1
                    }
                },
                builtInEffects = new[]
                {
                    new EnemySkill_SO.BuiltInEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        effectType = EnemySkill_SO.BuiltInEffectType.ApplyStatus,
                        targetMode = EnemySkill_SO.DamageTargetMode.Self,
                        useStatusCondition = true,
                        conditionScope = EnemySkill_SO.BuiltInConditionScope.CurrentTarget,
                        conditionStatusData = bleed,
                        requiredStatusStacks = 1,
                        statusData = temporaryStrength,
                        applyTiming = EnemySkill_SO.StatusApplyTiming.NextRoundStart,
                        applyAfterRoundStarts = 1,
                        applyRepeatCount = 1,
                        statusStackAmount = 1,
                        randomTargetCount = 1
                    }
                },
                statusStackOperationEffects = new[]
                {
                    new EnemySkill_SO.StatusStackOperationEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = bleed,
                        operationType = EnemySkill_SO.StatusStackOperationType.ReduceStacks,
                        stackAmount = 1,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
                        conditionStatusData = bleed,
                        requiredConditionStatusStacks = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);

        Assert.That(detail, Does.Contain("\u6d88\u8017"));
        Assert.That(detail, Does.Contain("\u5f53\u524d\u76ee\u6807"));
        Assert.That(detail, Does.Contain("1"));
        Assert.That(detail, Does.Contain("\u6d41\u8840"));
        Assert.That(detail, Does.Contain("\u6765\u4f7f"));
        Assert.That(detail, Does.Contain("\u81ea\u5df1"));
        Assert.That(detail, Does.Contain("\u6062\u590d"));
        Assert.That(detail, Does.Contain("2"));
        Assert.That(detail, Does.Contain("\u5e76"));
        Assert.That(detail, Does.Contain("\u4e0b\u56de\u5408"));
        Assert.That(detail, Does.Contain("\u4e34\u65f6\u5f3a\u58ee"));
        Assert.That(detail, Does.Not.Contain("\u51cf\u5c11"));
        Assert.That(detail, Does.Not.Contain("\u82e5 \u003ccolor"));
    }

    [Test]
    public void CompactSkillDetail_MergesConditionedConsumedStatusFlatPowerModifier()
    {
        BattleStatus_SO refractedMark = CreateStatus("refracted_mark", "\u6298\u5149\u5370");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u7167\u9762\u5207\u843d";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 4, maxPower = 6 }
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
                        condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
                        targetStatusData = refractedMark,
                        requiredStatusStacks = 1,
                        powerBonus = 1
                    }
                },
                statusStackOperationEffects = new[]
                {
                    new EnemySkill_SO.StatusStackOperationEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = refractedMark,
                        operationType = EnemySkill_SO.StatusStackOperationType.ReduceStacks,
                        stackAmount = 1,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
                        conditionStatusData = refractedMark,
                        requiredConditionStatusStacks = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);
        string consumeLine = FindLineContaining(detail, "\u6d88\u8017");

        Assert.That(consumeLine, Does.Contain("\u4f7f\u7528\u65f6"));
        Assert.That(consumeLine, Does.Contain("1"));
        Assert.That(consumeLine, Does.Contain("\u6298\u5149\u5370"));
        Assert.That(consumeLine, Does.Contain("\u5a01\u529b"));
        Assert.That(consumeLine, Does.Contain("+1"));
        Assert.That(detail, Does.Not.Contain("\u51cf\u5c11"));
        Assert.That(detail, Does.Not.Contain("\u82e5 \u003ccolor"));
    }

    [Test]
    public void CompactSkillDetail_MergesConditionedConsumedStatusBuiltInDamage()
    {
        BattleStatus_SO refractedMark = CreateStatus("refracted_mark", "\u6298\u5149\u5370");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u7167\u9762\u5207\u843d";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 4 }
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
                        effectType = EnemySkill_SO.BuiltInEffectType.Damage,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        amount = 2,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
                        targetStatusData = refractedMark,
                        requiredConditionStatusStacks = 1
                    }
                },
                statusStackOperationEffects = new[]
                {
                    new EnemySkill_SO.StatusStackOperationEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = refractedMark,
                        operationType = EnemySkill_SO.StatusStackOperationType.ReduceStacks,
                        stackAmount = 1,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
                        conditionStatusData = refractedMark,
                        requiredConditionStatusStacks = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);
        string consumeLine = FindLineContaining(detail, "\u6d88\u8017");

        Assert.That(consumeLine, Does.Contain("\u547d\u4e2d\u65f6"));
        Assert.That(consumeLine, Does.Contain("1"));
        Assert.That(consumeLine, Does.Contain("\u6298\u5149\u5370"));
        Assert.That(consumeLine, Does.Contain("\u989d\u5916\u9020\u6210"));
        Assert.That(consumeLine, Does.Contain("2"));
        Assert.That(detail, Does.Not.Contain("\u51cf\u5c11"));
        Assert.That(detail, Does.Not.Contain("\u82e5 \u003ccolor"));
    }

    [Test]
    public void CompactSkillDetail_MergesConsumedStatusRepeatCurrentDie()
    {
        BattleStatus_SO refractedMark = CreateStatus("refracted_mark", "\u6298\u5149\u5370");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u6666\u9762\u6298\u8fd4";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 3 }
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
                        effectType = EnemySkill_SO.BuiltInEffectType.RepeatCurrentDie,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
                        targetStatusData = refractedMark,
                        requiredConditionStatusStacks = 1
                    }
                },
                statusStackOperationEffects = new[]
                {
                    new EnemySkill_SO.StatusStackOperationEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = refractedMark,
                        operationType = EnemySkill_SO.StatusStackOperationType.ReduceStacks,
                        stackAmount = 1,
                        randomTargetCount = 1,
                        useCondition = true,
                        condition = EnemySkill_SO.PowerModifierCondition.TargetHasStatus,
                        conditionStatusData = refractedMark,
                        requiredConditionStatusStacks = 1
                    }
                }
            }
        };

        string detail = InvokeCompactSkillHoverDetail(skill);
        string consumeLine = FindLineContaining(detail, "\u6d88\u8017");

        Assert.That(consumeLine, Does.Contain("\u547d\u4e2d\u65f6"));
        Assert.That(consumeLine, Does.Contain("1"));
        Assert.That(consumeLine, Does.Contain("\u6298\u5149\u5370"));
        Assert.That(consumeLine, Does.Contain("\u91cd\u590d\u4f7f\u7528\u8be5\u9ab0\u5b50"));
        Assert.That(detail, Does.Not.Contain("\u51cf\u5c11"));
        Assert.That(detail, Does.Not.Contain("\u82e5 \u003ccolor"));
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
        Assert.That(detail, Does.Contain("\u5a01\u529b\u8303\u56f4\u6700\u5c0f\u503c"));
        Assert.That(detail, Does.Contain("+1"));
        Assert.That(detail, Does.Contain("\u6700\u591a"));
        Assert.That(detail, Does.Contain("+5"));
    }

    [Test]
    public void DiceRangeLabel_StrikesOnlyChangedPowerNumbersAndAppendsDisplayedValues()
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        skill.diceCount = 1;

        string minReduced = InvokeDiceRangeLabel(
            skill,
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 5 },
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 5 });
        string maxReduced = InvokeDiceRangeLabel(
            skill,
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 5 },
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 3 });

        Assert.That(minReduced, Does.Contain("<color=#8C8C8C><s>2</s></color><color="));
        Assert.That(minReduced, Does.Contain(" ~ <color="));
        Assert.That(minReduced, Does.Not.Contain("<s>2 ~ 5</s>"));
        Assert.That(maxReduced, Does.Contain("<color="));
        Assert.That(maxReduced, Does.Contain(" ~ <color=#8C8C8C><s>5</s></color><color="));
        Assert.That(maxReduced, Does.Not.Contain("<s>2 ~ 5</s>"));
    }

    [Test]
    public void CompactSkillHoverSections_SeparatesDiceSummaryAndClauseText()
    {
        BattleStatus_SO frail = CreateStatus("frail", "\u8106\u5f31");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u538b\u5236";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 4 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                statusEffects = new[]
                {
                    new EnemySkill_SO.StatusEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
                        targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
                        statusData = frail,
                        stackAmount = 1,
                        randomTargetCount = 1
                    }
                }
            }
        };

        string[] sections = InvokeCompactSkillHoverSections(skill);

        Assert.That(sections, Has.Length.EqualTo(2));
        Assert.That(sections[0], Does.Contain("\u7b2c 1 \u653b\u51fb"));
        Assert.That(sections[0], Does.Not.Contain("\u8bcd\u6761"));
        Assert.That(sections[1], Does.StartWith("\u8bcd\u6761\uff1a"));
        Assert.That(sections[1], Does.Contain("\u8106\u5f31"));
    }

    [Test]
    public void SkillDetail_IncludesSummonEffects()
    {
        EnemyData_SO summonedEnemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdObjects.Add(summonedEnemy);
        summonedEnemy.enemyName = "\u8840\u8718\u86cb";

        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        createdObjects.Add(skill);
        skill.skillName = "\u7e41\u6b96";
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 2 }
        };
        skill.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                summonEffects = new[]
                {
                    new EnemySkill_SO.SummonEnemyEffectEntry
                    {
                        trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                        enemyData = summonedEnemy,
                        summonCount = 2
                    }
                }
            }
        };

        string detail = InvokeSkillDetail(skill);

        Assert.That(detail, Does.Contain("\u4f7f\u7528\u65f6"));
        Assert.That(detail, Does.Contain("\u53ec\u5524"));
        Assert.That(detail, Does.Contain("\u8840\u8718\u86cb"));
        Assert.That(detail, Does.Contain("2"));
    }

    private string InvokeCompactSkillHoverDetail(EnemySkill_SO skill)
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

    private string InvokeSkillDetail(EnemySkill_SO skill)
    {
        MethodInfo method = typeof(BattleSkillInfoOverlay).GetMethod(
            "BuildSkillDetail",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(EnemySkill_SO), typeof(string), typeof(bool), typeof(System.Func<int, EnemySkill_SO.DicePowerRange>), typeof(BattleAllyController) },
            null);

        Assert.IsNotNull(method);
        return (string)method.Invoke(overlay, new object[] { skill, "\u5f53\u524d\u6280\u80fd", false, null, null });
    }

    private string[] InvokeCompactSkillHoverSections(EnemySkill_SO skill)
    {
        MethodInfo method = typeof(BattleSkillInfoOverlay).GetMethod(
            "BuildCompactSkillHoverSections",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(EnemySkill_SO), typeof(bool), typeof(System.Func<int, EnemySkill_SO.DicePowerRange>) },
            null);

        Assert.IsNotNull(method);
        return (string[])method.Invoke(overlay, new object[] { skill, true, null });
    }

    private string InvokeDiceRangeLabel(
        EnemySkill_SO skill,
        EnemySkill_SO.DicePowerRange baseRange,
        EnemySkill_SO.DicePowerRange displayedRange)
    {
        MethodInfo method = typeof(BattleSkillInfoOverlay).GetMethod(
            "BuildDiceRangeLabel",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        return (string)method.Invoke(overlay, new object[] { skill, 0, baseRange, displayedRange });
    }

    private static string FindLineContaining(string text, string expectedSubstring)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(expectedSubstring))
                return lines[i];
        }

        Assert.Fail($"Could not find line containing {expectedSubstring} in:\n{text}");
        return string.Empty;
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
