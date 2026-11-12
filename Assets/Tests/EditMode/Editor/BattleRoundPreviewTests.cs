using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleRoundPreviewTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Sword = "Assets/Data/Weapons/000 WoodenSword/WoodenSword_Data.asset";
    private const string Rat = "Assets/Data/Enemies/Enemies/014 BloodRat/BloodRat.asset";
    private readonly List<EnemySkill_SO> skills = new List<EnemySkill_SO>();
    private GameObject host;
    private BattleEncounterData oldActive, oldPending;
    private bool oldLocked, oldSuppressed;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("RoundPreviewTests");
        oldActive = BattleEncounterRuntime.ActiveEncounter;
        oldPending = BattleEncounterRuntime.CurrentEncounter;
        oldLocked = SpeedButton.CombatLocked;
        oldSuppressed = SpeedButton.InputSuppressed;
        SpeedButton.CombatLocked = SpeedButton.InputSuppressed = false;
        BattleEncounterRuntime.SetEncounter(new BattleEncounterData());
        BattleEncounterRuntime.ConsumeEncounter();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (EnemySkill_SO skill in skills)
            Object.DestroyImmediate(skill);
        skills.Clear();
        BattleEncounterRuntime.SetEncounter(oldActive);
        BattleEncounterRuntime.ConsumeEncounter();
        BattleEncounterRuntime.SetEncounter(oldPending);
        SpeedButton.CombatLocked = oldLocked;
        SpeedButton.InputSuppressed = oldSuppressed;
    }

    [Test]
    public void WinRateKeepsTheOriginalFaceWeightsAfterFinalPowerClamps()
    {
        EnemySkill_SO strike = Skill(Range(1, 4));
        strike.diceEffectSettings = new[]
        {
            new EnemySkill_SO.DiceEffectSettings
            {
                powerModifiers = new[]
                {
                    new EnemySkill_SO.ConditionalPowerModifierEntry { trigger = EnemySkill_SO.DiceEffectTrigger.OnClash, powerBonus = -2 }
                }
            }
        };
        BattleAllyController ally = Ally(strike);
        BattleEnemyController enemy = Enemy(Skill(Range(1, 2)));
        LinkClash(ally, enemy, 0);
        var random = Random.state;

        Assert.That(Rate(ally, enemy), Is.EqualTo(0.125).Within(1e-10));
        Assert.AreEqual(random, Random.state);
    }

    [Test]
    public void MultiDiceCountsWinsOverLossesAndIgnoresTheUnpairedDie()
    {
        BattleAllyController ally = Ally(Skill(Range(1, 2), Range(1, 2), Range(100, 100)));
        BattleEnemyController enemy = Enemy(Skill(Range(1, 2), Range(1, 2)));
        LinkClash(ally, enemy, 0);

        Assert.That(Rate(ally, enemy), Is.EqualTo(0.3125).Within(1e-10));
    }

    [Test]
    public void ForcedRollsChangeTheOddsWithoutRolling()
    {
        BattleAllyController ally = Ally(Skill(Range(1, 5)));
        BattleEnemyController enemy = Enemy(Skill(Range(3, 3)));
        LinkClash(ally, enemy, 0);
        Assert.That(Rate(ally, enemy), Is.EqualTo(0.4).Within(1e-10));
        BattleStatus_SO luck = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Luck/Luck.asset");
        ally.AddStatus(luck, luck.StatusId, luck.DisplayName, 1);
        Assert.That(Rate(ally, enemy), Is.EqualTo(1));
        BattleStatus_SO misfortune = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Misfortune/Misfortune.asset");
        ally.AddStatus(misfortune, misfortune.StatusId, misfortune.DisplayName, 1);
        Assert.That(Rate(ally, enemy), Is.Zero);
    }

    [Test]
    public void ForecastSpendsAOncePerRoundRelicOnTheFirstClashOnlyAndLeavesCombatStateIntact()
    {
        EnemySkill_SO skill = Skill(Range(1, 1));
        BattleAllyController ally = Ally(skill);
        BattleEnemyController enemy = Enemy(Skill(Range(1, 1)));
        LinkClash(ally, enemy, 0);
        LinkClash(ally, enemy, 1);
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        Relic_SO relic = AssetDatabase.LoadAssetAtPath<Relic_SO>("Assets/Data/Relics/118 苦根药袋/苦根药袋.asset");
        encounter.relicInventory.Add(new RunRelicState { relicData = relic, amount = 1 });
        BattleStatus_SO poison = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>("Assets/Data/Battle/Poison/Poison.asset");
        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 1);

        var actions = BattleCombatActionBuilder.Build(new[] { ally }, new[] { enemy });
        Assert.AreEqual(2, actions.Count);
        Assert.AreEqual(new double?[] { 0.5, 0 }, BattleClashWinRateService.Estimate(actions));
        Assert.AreEqual(new double?[] { 0.5, 0 }, BattleClashWinRateService.Estimate(actions));
        Assert.AreEqual(1, RelicTriggerDispatcher.ProcessPlayerDicePrepared(encounter, null, skill, 0, true, ally, enemy).additionalMaxPowerBonus);
    }

    [Test]
    public void ForecastIncludesTheUpcomingUseForConsecutiveSkillRelicsAndTracksTheirReset()
    {
        EnemySkill_SO skill = Skill(Range(1, 1));
        BattleAllyController ally = Ally(skill);
        BattleEnemyController enemy = Enemy(Skill(Range(1, 1)));
        LinkClash(ally, enemy, 0);
        LinkClash(ally, enemy, 1);
        ally.ActivateSelectedSkillForButton(ally.SpeedButtons[0]);
        ally.MarkCurrentRoundSkillUsed();
        Relic_SO relic = AssetDatabase.LoadAssetAtPath<Relic_SO>("Assets/Data/Relics/026 刃石/刃石.asset");
        BattleEncounterRuntime.ActiveEncounter.relicInventory.Add(new RunRelicState { relicData = relic, amount = 1 });

        var actions = BattleCombatActionBuilder.Build(new[] { ally }, new[] { enemy });
        Assert.AreEqual(new double?[] { 1, 0 }, BattleClashWinRateService.Estimate(actions));
        Assert.AreEqual(1, ally.GetCompletedConsecutiveSkillUses(skill));
        ally.MarkCurrentRoundSkillUsed();
        Assert.AreEqual(2, ally.GetCompletedConsecutiveSkillUses(skill));
    }

    [Test]
    public void BattleHudUpdatesWithLinksAndCyclesUninterceptedAttacksThroughTheScrollableList()
    {
        BattleAllyController ally = Ally(Skill(Range(4, 4)));
        BattleEnemyController first = Enemy(Skill(Range(1, 1)));
        BattleEnemyController second = Enemy(Skill(Range(5, 5)));
        BattleEnemyController third = Enemy(Skill(Range(3, 3)));
        foreach (BattleEnemyController enemy in new[] { first, second, third })
        foreach (SpeedButton button in enemy.SpeedButtons)
            button.EnemyDrawLineTo(ally.SpeedButtons[0]);
        LinkClash(ally, first, 0);
        LinkClash(ally, second, 1);

        var canvas = new GameObject("PreviewHud", typeof(RectTransform), typeof(Canvas));
        canvas.transform.SetParent(host.transform, false);
        var hud = (RectTransform)canvas.transform;
        hud.sizeDelta = new Vector2(1920, 1080);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var buttonHost = new GameObject("InactiveBattleButton");
        buttonHost.transform.SetParent(host.transform, false);
        buttonHost.SetActive(false);
        BattleButton battle = buttonHost.AddComponent<BattleButton>();
        SetField(battle, "sharedEnergyRoot", hud);
        SetField(battle, "activeRoundAllies", new[] { ally });
        SetField(battle, "activeRoundEnemies", new[] { first, second, third });
        FieldInfo phase = typeof(BattleButton).GetField("currentPhase", Private);
        phase.SetValue(battle, System.Enum.Parse(phase.FieldType, "Targeting"));
        Refresh(battle);

        Transform preview = hud.Find("BattleRoundPreview");
        Transform panel = preview.Find("RoundPreviewPanel");
        Transform actions = panel.Find("RoundActionList/Viewport/Actions");
        object overlay = typeof(BattleButton).GetField("roundPreview", Private).GetValue(battle);
        overlay.GetType().GetMethod("ApplyViewport").Invoke(overlay, new object[]
        {
            new Rect(40, 20, 1830, 1040), new Vector2(1920, 1080), new Rect(735, 55, 450, 130)
        });
        var panelRect = (RectTransform)panel;
        Assert.That(panelRect.anchoredPosition.x, Is.GreaterThan(1185));
        Assert.That(panelRect.anchoredPosition.x + panelRect.rect.width, Is.LessThanOrEqualTo(1870));
        Assert.That(((RectTransform)actions.parent).rect.height, Is.EqualTo(3 * 44f).Within(0.1f));
        StringAssert.Contains("拼点胜率 <color=#85E89C>100%", Text(actions, "RoundAction_1/ActionKind"));
        StringAssert.Contains("拼点胜率 <color=#FF7E6E>0%", Text(actions, "RoundAction_2/ActionKind"));
        Assert.AreEqual("未拦截 4", Text(panel, "Unintercepted/WarningLabel"));
        Assert.AreEqual("1–3 / 6", Text(panel, "VisibleActions"));
        var warning = panel.Find("Unintercepted").GetComponent<Button>();
        for (int i = 0; i < 4; i++)
            warning.onClick.Invoke();
        Assert.AreEqual("4–6 / 6", Text(panel, "VisibleActions"));
        Assert.That(ally.SpeedButtons[0].GetComponent<LineRenderer>().widthMultiplier, Is.EqualTo(0.03f).Within(1e-5));
        Assert.That(third.SpeedButtons[1].GetComponent<LineRenderer>().widthMultiplier, Is.EqualTo(0.145f).Within(1e-5));

        ally.SpeedButtons[1].DisconnectLine();
        Refresh(battle);
        Assert.AreEqual("未拦截 5", Text(panel, "Unintercepted/WarningLabel"));
        Assert.AreEqual(1, actions.GetComponentsInChildren<TMP_Text>().Count(t => t.name == "ActionKind" && t.text.Contains("拼点胜率")));

        SpeedButton.InputSuppressed = true;
        Refresh(battle);
        Assert.IsFalse(preview.gameObject.activeSelf);
        Assert.That(ally.SpeedButtons[0].GetComponent<LineRenderer>().widthMultiplier, Is.EqualTo(0.1f).Within(1e-5));
        SpeedButton.InputSuppressed = false;
        Refresh(battle);
        Assert.IsTrue(preview.gameObject.activeSelf);
        SetField(battle, "isResolvingCombat", true);
        Refresh(battle);
        Assert.IsFalse(preview.gameObject.activeSelf);
    }

    private BattleAllyController Ally(EnemySkill_SO skill)
    {
        var ally = BattleSkillVisualTools.CreateAlly(host.transform, AssetDatabase.LoadAssetAtPath<Weapon_SO>(Sword));
        ally.gameObject.SetActive(true);
        ally.GrantTemporarySkill(skill, true);
        ally.SetRoundSpeed(6);
        foreach (SpeedButton button in ally.SpeedButtons)
        {
            button.ConfigureHeadlessExecution();
            button.IsInteractable = true;
        }
        return ally;
    }

    private BattleEnemyController Enemy(EnemySkill_SO skill)
    {
        var enemy = BattleSkillVisualTools.CreateEnemy(host.transform, AssetDatabase.LoadAssetAtPath<EnemyData_SO>(Rat), skill);
        enemy.gameObject.SetActive(true);
        foreach (SpeedButton button in enemy.SpeedButtons)
        {
            button.ConfigureHeadlessExecution();
            button.Speed = 3;
        }
        return enemy;
    }

    private static void LinkClash(BattleAllyController ally, BattleEnemyController enemy, int slot)
    {
        Assert.IsTrue(ally.SpeedButtons[slot].TryAssignTarget(enemy.SpeedButtons[slot]));
        Assert.IsTrue(ally.SpeedButtons[slot].IsContestedConnection);
    }

    private EnemySkill_SO Skill(params EnemySkill_SO.DicePowerRange[] ranges)
    {
        var skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skills.Add(skill);
        skill.skillName = "测试技能";
        skill.diceCount = ranges.Length;
        skill.dicePowerRanges = ranges;
        return skill;
    }

    private static double Rate(BattleAllyController ally, BattleEnemyController enemy)
        => BattleClashWinRateService.Estimate(BattleCombatActionBuilder.Build(new[] { ally }, new[] { enemy }))[0].Value;

    private static EnemySkill_SO.DicePowerRange Range(int min, int max)
        => new EnemySkill_SO.DicePowerRange { minPower = min, maxPower = max };

    private static string Text(Transform root, string path) => root.Find(path).GetComponent<TMP_Text>().text;
    private static void SetField(BattleButton battle, string name, object value) => typeof(BattleButton).GetField(name, Private).SetValue(battle, value);
    private static void Refresh(BattleButton battle) => typeof(BattleButton).GetMethod("RefreshRoundPreview", Private).Invoke(battle, null);
}
