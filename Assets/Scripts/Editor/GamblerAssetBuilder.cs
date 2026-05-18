#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GamblerAssetBuilder
{
    private const string EnemyRoot = "Assets/Data/Enemies/Enemies/036 Gambler";
    private const string DebtPath = "Assets/Data/Battle/Debt/Debt.asset";
    private const string EnemyPath = EnemyRoot + "/Gambler_Data.asset";
    private const string GroupPath = "Assets/Data/Enemies/EnemyGroup/Events/Gambler.asset";

    [MenuItem("Tools/Data/Build 036 Gambler Assets")]
    public static void Build()
    {
        EnsureFolder("Assets/Data/Battle/Debt");
        EnsureFolder(EnemyRoot);
        EnsureFolder("Assets/Data/Enemies/EnemyGroup/Events");

        BattleStatus_SO debt = CreateOrLoad<BattleStatus_SO>(DebtPath);
        ConfigureDebtStatus(debt);

        BattleStatus_SO weaknessTemp = LoadRequired<BattleStatus_SO>("Assets/Data/Battle/Weakness/Weakness_Temp.asset");
        LootAttribute_SO dark = LoadRequired<LootAttribute_SO>("Assets/Data/Loots/Attributes/Dark_Attribute.asset");
        Sprite fallbackSprite = Load<Sprite>("Assets/Data/Enemies/Enemies/031 Reflection/Reflection.png")
            ?? Load<Sprite>("Assets/Data/Enemies/Enemies/009 LoomingDark/LoomingDark.png");

        EnemySkill_SO enemyGamble = CreateOrLoad<EnemySkill_SO>(EnemyRoot + "/Gamble.asset");
        ConfigureGambleSkill(enemyGamble, "Gamble", "赌博", "骰子在黑暗里滚动，停下前谁也不知道自己押上的是什么。", debt, true);

        EnemySkill_SO allyGamble = CreateOrLoad<EnemySkill_SO>(EnemyRoot + "/GrantedGamble.asset");
        ConfigureGambleSkill(allyGamble, "GrantedGamble", "赌博", "桌面很安静，只有一枚骰子替你说话。", debt, false);

        EnemySkill_SO cheat = CreateOrLoad<EnemySkill_SO>(EnemyRoot + "/Cheat.asset");
        ConfigureCheatSkill(cheat, debt, weaknessTemp);

        EnemyData_SO gambler = CreateOrLoad<EnemyData_SO>(EnemyPath);
        ConfigureGamblerEnemy(gambler, fallbackSprite, enemyGamble, allyGamble, cheat);

        EnemyGroup_SO group = CreateOrLoad<EnemyGroup_SO>(GroupPath);
        ConfigureGamblerGroup(group, gambler, dark);

        EditorUtility.SetDirty(debt);
        EditorUtility.SetDirty(enemyGamble);
        EditorUtility.SetDirty(allyGamble);
        EditorUtility.SetDirty(cheat);
        EditorUtility.SetDirty(gambler);
        EditorUtility.SetDirty(group);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureDebtStatus(BattleStatus_SO debt)
    {
        debt.name = "Debt";
        SetPrivate(debt, "statusId", "debt");
        SetPrivate(debt, "displayName", "负债");
        SetPrivate(debt, "description", "筹码欠下的影子。达到 3 层时，偿还会立刻到来。");
        SetPrivate(debt, "accentColor", new Color(0.95f, 0.76f, 0.28f, 1f));
        SetPrivate(debt, "behaviorType", BattleStatus_SO.StatusBehaviorType.None);
        SetPrivate(debt, "powerPerStack", 0);
        SetPrivate<BattleStatus_SO>(debt, "linkedStatusData", null);
        SetPrivate(debt, "onHitDecayMode", BattleStatus_SO.StackDecayMode.None);
        SetPrivate(debt, "roundStartDecayMode", BattleStatus_SO.StackDecayMode.None);
        SetPrivate(debt, "roundEndDecayMode", BattleStatus_SO.StackDecayMode.None);
    }

    private static void ConfigureGambleSkill(
        EnemySkill_SO skill,
        string assetName,
        string displayName,
        string description,
        BattleStatus_SO debt,
        bool appliesDebtOnClashWin)
    {
        skill.name = assetName;
        skill.skillName = displayName;
        skill.description = description;
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        skill.energyCost = 0;
        skill.minPower = 1;
        skill.maxPower = 6;
        skill.diceCount = 1;
        skill.allowedEnemyStages = new List<int> { 1 };
        skill.dicePowerRanges = new[] { Range(1, 6) };
        skill.diceSkillCategories = new[] { EnemySkill_SO.SkillCategory.Attack };
        EnemySkill_SO.DiceEffectSettings settings = EmptySettings();
        if (appliesDebtOnClashWin)
        {
            settings.statusEffects = new[]
            {
                StatusEffect("落下负债", EnemySkill_SO.DiceEffectTrigger.OnClashWin, EnemySkill_SO.DamageTargetMode.CurrentTarget, debt, 1)
            };
        }

        skill.diceEffectSettings = new[] { settings };
    }

    private static void ConfigureCheatSkill(EnemySkill_SO skill, BattleStatus_SO debt, BattleStatus_SO weaknessTemp)
    {
        skill.name = "Cheat";
        skill.skillName = "千术";
        skill.description = "指尖只轻轻一翻，账本上的名字便少了一笔，多了一道阴影。";
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        skill.energyCost = 5;
        skill.minPower = 7;
        skill.maxPower = 7;
        skill.diceCount = 1;
        skill.allowedEnemyStages = new List<int> { 1 };
        skill.dicePowerRanges = new[] { Range(7, 7) };
        skill.diceSkillCategories = new[] { EnemySkill_SO.SkillCategory.Attack };

        EnemySkill_SO.DiceEffectSettings settings = EmptySettings();
        settings.statusStackOperationEffects = new[]
        {
            StackOperation("洗去负债", EnemySkill_SO.DiceEffectTrigger.OnSkillUse, EnemySkill_SO.DamageTargetMode.Self, debt, 1)
        };
        settings.statusEffects = new[]
        {
            StatusEffect("留下破绽", EnemySkill_SO.DiceEffectTrigger.OnHit, EnemySkill_SO.DamageTargetMode.CurrentTarget, weaknessTemp, 1, EnemySkill_SO.StatusApplyTiming.NextRoundStart)
        };
        skill.diceEffectSettings = new[] { settings };
    }

    private static void ConfigureGamblerEnemy(EnemyData_SO gambler, Sprite sprite, EnemySkill_SO enemyGamble, EnemySkill_SO allyGamble, EnemySkill_SO cheat)
    {
        gambler.name = "Gambler_Data";
        gambler.enemyID = 36;
        gambler.enemyName = "贝者";
        gambler.baseHP = 6;
        gambler.damageReductionPercent = 0f;
        gambler.minSpeed = 1;
        gambler.maxSpeed = 1;
        gambler.baseSpeedButtonCount = 1;
        gambler.enemySprite = sprite;
        gambler.stageSprites = new List<EnemyData_SO.EnemyStageEntry>();
        gambler.statusStageTransitionRules = new List<EnemyData_SO.StatusStageTransitionRule>();
        gambler.statusImmunityRules = new List<EnemyData_SO.StatusImmunityRule>();
        gambler.battleStartGrantedAllySkills = new List<EnemyData_SO.BattleStartAllySkillGrantEntry>
        {
            new EnemyData_SO.BattleStartAllySkillGrantEntry { skill = allyGamble, replaceWeaponSkills = false },
            new EnemyData_SO.BattleStartAllySkillGrantEntry { skill = cheat, replaceWeaponSkills = false }
        };
        gambler.battleStartStatusGains = new List<EnemyData_SO.StatusGainEntry>();
        gambler.onSpecificEnemyDeathStatusGains = new List<EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry>();
        gambler.repeatRoundStartStatusGains = new List<EnemyData_SO.StatusGainEntry>();
        gambler.roundStartRetreatRule = new EnemyData_SO.RoundStartRetreatRule { enabled = false, triggerRound = 1, settlementLootTables = new List<LootTable_SO>() };
        gambler.preventAllyBleedDecayOnHitWhileAlive = false;
        gambler.surviveAtOneHpUntilAllLivingAlliesAtOrBelowOneHp = false;
        gambler.transformToEnemyDataWhenStatusMissing = null;
        gambler.transformMissingStatusData = null;
        gambler.copyOpponentLastRoundSpeedDiceCount = false;
        gambler.copyOpponentLastRoundSkills = false;
        gambler.reflectionSkillTemplate = null;
        gambler.enemyDescription = "一枚贝壳，一张赌桌，一个不会离席的债主。它不追杀任何人，只等骰子停下。";
        gambler.passiveDescriptions = new List<EnemyData_SO.PassiveDescriptionEntry>
        {
            new EnemyData_SO.PassiveDescriptionEntry { passiveName = "偿还", displayColorType = EnemyData_SO.PassiveDisplayColorType.Red, description = "负债到达 3 的角色立即死亡" },
            new EnemyData_SO.PassiveDescriptionEntry { passiveName = "执念成型", displayColorType = EnemyData_SO.PassiveDisplayColorType.Red, description = "免疫除自身被动【愿赌服输】外的所有伤害" },
            new EnemyData_SO.PassiveDescriptionEntry { passiveName = "愿赌服输", displayColorType = EnemyData_SO.PassiveDisplayColorType.Orange, description = "与技能【赌博】拼点失败时，对自身造成 1 点伤害" },
            new EnemyData_SO.PassiveDescriptionEntry { passiveName = "上桌", displayColorType = EnemyData_SO.PassiveDisplayColorType.Yellow, description = "战斗开始时，为所有目标添加技能【赌博】与【千术】" }
        };
        gambler.enemyVisualScale = 1f;
        gambler.enemyVisualOffset = Vector3.zero;
        gambler.speedButtonOffset = Vector2.zero;
        gambler.skillPool = new List<EnemySkill_SO> { enemyGamble };
        gambler.conditionalSkillRules = new List<EnemyData_SO.ConditionalSkillRule>();
    }

    private static void ConfigureGamblerGroup(EnemyGroup_SO group, EnemyData_SO gambler, LootAttribute_SO dark)
    {
        group.name = "Gambler";
        group.groupID = 36;
        group.groupName = "Gambler";
        group.description = "事件敌人：贝者";
        group.enemies = new List<EnemyData_SO> { gambler };
        group.attributes = new List<LootAttribute_SO> { dark };
        group.lootTables = new List<LootTable_SO>();
    }

    private static EnemySkill_SO.DicePowerRange Range(int min, int max)
    {
        return new EnemySkill_SO.DicePowerRange { minPower = min, maxPower = max };
    }

    private static EnemySkill_SO.DiceEffectSettings EmptySettings()
    {
        return new EnemySkill_SO.DiceEffectSettings
        {
            damageEffects = Array.Empty<EnemySkill_SO.DamageEffectEntry>(),
            statusEffects = Array.Empty<EnemySkill_SO.StatusEffectEntry>(),
            powerModifiers = Array.Empty<EnemySkill_SO.ConditionalPowerModifierEntry>(),
            statusBurstEffects = Array.Empty<EnemySkill_SO.StatusBurstDamageEffectEntry>(),
            healingEffects = Array.Empty<EnemySkill_SO.HealingEffectEntry>(),
            builtInEffects = Array.Empty<EnemySkill_SO.BuiltInEffectEntry>(),
            rangeModifiers = Array.Empty<EnemySkill_SO.ConditionalDiceRangeModifierEntry>(),
            statusStackOperationEffects = Array.Empty<EnemySkill_SO.StatusStackOperationEffectEntry>(),
            enemyStageTransitionEffects = Array.Empty<EnemySkill_SO.EnemyStageTransitionEffectEntry>(),
            summonEffects = Array.Empty<EnemySkill_SO.SummonEnemyEffectEntry>()
        };
    }

    private static EnemySkill_SO.StatusEffectEntry StatusEffect(
        string name,
        EnemySkill_SO.DiceEffectTrigger trigger,
        EnemySkill_SO.DamageTargetMode targetMode,
        BattleStatus_SO status,
        int stacks,
        EnemySkill_SO.StatusApplyTiming timing = EnemySkill_SO.StatusApplyTiming.Immediate)
    {
        return new EnemySkill_SO.StatusEffectEntry
        {
            effectName = name,
            trigger = trigger,
            targetMode = targetMode,
            statusData = status,
            statusType = EnemySkill_SO.DiceStatusType.Burn,
            applyTiming = timing,
            applyAfterRoundStarts = 1,
            applyRepeatCount = 1,
            stackAmount = stacks,
            randomTargetCount = 1,
            requiredConditionStatusStacks = 1
        };
    }

    private static EnemySkill_SO.StatusStackOperationEffectEntry StackOperation(
        string name,
        EnemySkill_SO.DiceEffectTrigger trigger,
        EnemySkill_SO.DamageTargetMode targetMode,
        BattleStatus_SO status,
        int stacks)
    {
        return new EnemySkill_SO.StatusStackOperationEffectEntry
        {
            effectName = name,
            trigger = trigger,
            targetMode = targetMode,
            statusData = status,
            statusType = EnemySkill_SO.DiceStatusType.Burn,
            operationType = EnemySkill_SO.StatusStackOperationType.ReduceStacks,
            stackAmount = stacks,
            randomTargetCount = 1,
            requiredConditionStatusStacks = 1
        };
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = Load<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"Missing required asset: {path}");
        return asset;
    }

    private static T CreateOrLoad<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void SetPrivate<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException($"Field {fieldName} was not found on {target.GetType().Name}.");

        field.SetValue(target, value);
    }
}
#endif
