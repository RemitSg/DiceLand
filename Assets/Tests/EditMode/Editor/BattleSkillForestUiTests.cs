using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleSkillForestUiTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject host;
    private BattleSkillInfoOverlay overlay;
    private bool oldInput, oldLock;
    private readonly List<Object> transient = new List<Object>();

    [SetUp]
    public void Setup()
    {
        oldInput = SpeedButton.InputSuppressed; oldLock = SpeedButton.CombatLocked;
        SpeedButton.InputSuppressed = false; SpeedButton.CombatLocked = false;
        host = new GameObject("SkillForestTests"); overlay = host.AddComponent<BattleSkillInfoOverlay>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        overlay.ConfigureFonts(font, font);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (Object item in transient) if (item != null) Object.DestroyImmediate(item);
        transient.Clear(); SpeedButton.InputSuppressed = oldInput; SpeedButton.CombatLocked = oldLock;
    }

    [Test]
    public void FormatterDoesNotConstructUiOrChangeInputOwnership()
    {
        string rules = overlay.DescribeSkillRules(Asset<EnemySkill_SO>("Assets/Data/Weapons/000 WoodenSword/WoodenSword_Slash.asset"), true);
        StringAssert.Contains("所有目标", rules); StringAssert.Contains("点伤害", rules);
        Assert.AreEqual(0, host.transform.childCount);
        Assert.IsFalse(SpeedButton.InputSuppressed);
    }

    [Test]
    public void ShortContentHidesScrollbarsBeforeItsFirstRenderedFrame()
    {
        BattleAllyController ally = Ally("Assets/Data/Weapons/000 WoodenSword/WoodenSword_Data.asset");
        overlay.ShowAllySkillSelection(ally, ally.SpeedButtons[0]); Layout(); overlay.RefreshCurrentView();
        Assert.IsFalse(Get<ScrollRect>("detailScrollRect").verticalScrollbar.gameObject.activeSelf);
        Assert.IsFalse(Get<ScrollRect>("skillListScroll").verticalScrollbar.gameObject.activeSelf);
        Assert.AreEqual(Vector2.zero, Get<RectTransform>("skillListRoot").anchoredPosition);
        StringAssert.Contains("<noparse>无</noparse>", Cards()[0].Find("DiceClauses").GetComponent<TMP_Text>().text);
    }

    [Test]
    public void RealMultiDiceUsesDistinctRangesAndKeepsAllCrossDiceConditions()
    {
        BattleAllyController ally = Ally("Assets/Data/Weapons/007 烬冠剑/007 烬冠剑.asset");
        ally.SetSelectedSkillIndexForButton(ally.SpeedButtons[0], 3);
        overlay.ShowAllySkillSelection(ally, ally.SpeedButtons[0]); Layout();
        Assert.AreEqual(4, host.GetComponentsInChildren<Button>(true).Count(b => b.name.StartsWith("Skill_")));
        Assert.AreEqual(2, Cards().Count);
        Assert.AreEqual("3 ~ 6", Plain(Cards()[0].Find("DiceRange").GetComponent<TMP_Text>().text));
        Assert.AreEqual("2 ~ 4", Plain(Cards()[1].Find("DiceRange").GetComponent<TMP_Text>().text));
        StringAssert.Contains("下一个骰子", Cards()[0].Find("DiceClauses").GetComponent<TMP_Text>().text);
        string second = Plain(Cards()[1].Find("DiceClauses").GetComponent<TMP_Text>().text);
        StringAssert.Contains("燃烧", second); StringAssert.Contains("全部攻击段命中时", second); StringAssert.Contains("额外造成 4", second);
        StringAssert.DoesNotContain("下一个骰子", second);
        Assert.AreEqual(2, Get<RectTransform>("summaryRoot").GetComponentsInChildren<Button>().Length);
    }

    [Test]
    public void EnergyFailureCannotBeBypassedAndSelectionKeepsOtherSpeedSlots()
    {
        BattleAllyController ally = Ally("Assets/Data/Weapons/000 WoodenSword/WoodenSword_Data.asset");
        GameObject gateHost = new GameObject("InactiveEnergyGate"); gateHost.transform.SetParent(host.transform); gateHost.SetActive(false);
        BattleButton gate = gateHost.AddComponent<BattleButton>(); Set(overlay, "cachedBattleButton", gate);
        Set(gate, "currentSharedEnergy", 0);
        overlay.ShowAllySkillSelection(ally, ally.SpeedButtons[0]); Layout();
        Button costly = host.GetComponentsInChildren<Button>(true).Single(b => b.name == "Skill_1");
        Assert.IsFalse(costly.interactable);
        StringAssert.Contains("能量不足", costly.transform.Find("Reason").GetComponent<TMP_Text>().text);
        EnemySkill_SO initial = ally.GetSelectedSkillForButton(ally.SpeedButtons[0]);
        costly.onClick.Invoke(); Assert.AreSame(initial, ally.GetSelectedSkillForButton(ally.SpeedButtons[0]));
        Set(gate, "currentSharedEnergy", 10); overlay.RefreshCurrentView();
        Assert.IsTrue(costly.interactable); costly.onClick.Invoke();
        Assert.AreEqual("横砍", ally.GetSelectedSkillForButton(ally.SpeedButtons[0]).skillName);
        Assert.AreSame(initial, ally.GetSelectedSkillForButton(ally.SpeedButtons[1]));
    }

    [Test]
    public void EnemyUsesFullWidthReadOnlyLayoutAndRefreshRetainsSourceSlot()
    {
        EnemyData_SO data = Asset<EnemyData_SO>("Assets/Data/Enemies/Enemies/014 BloodRat/BloodRat.asset");
        EnemySkill_SO bite = Asset<EnemySkill_SO>("Assets/Data/Enemies/Enemies/014 BloodRat/RotfangBite.asset");
        EnemySkill_SO second = Asset<EnemySkill_SO>("Assets/Data/Enemies/Enemies/027 UtilityPole/HighVoltage.asset");
        BattleEnemyController enemy = BattleSkillVisualTools.CreateEnemy(host.transform, data, bite);
        enemy.SetRuntimeCopiedSkills(new[] { bite, second });
        overlay.ShowEnemySkillInfo(enemy, enemy.SpeedButtons[1]); Layout(); overlay.RefreshCurrentView();
        Assert.AreEqual("高压电", Get<TMP_Text>("skillNameText").text);
        Assert.AreEqual(3, Cards().Count);
        Assert.IsFalse(Get<RectTransform>("listArea").gameObject.activeSelf);
        Assert.AreEqual(0, Get<RectTransform>("skillListRoot").childCount);
        Assert.Greater(Get<RectTransform>("detailRegion").rect.width, Get<RectTransform>("panelRoot").rect.width * 0.85f);
        Assert.IsFalse(Get<TMP_Text>("footerHint").gameObject.activeSelf);
        overlay.ShowEnemyDetail(enemy); Layout();
        Assert.IsTrue(Get<RectTransform>("enemyProfileRoot").gameObject.activeSelf);
        StringAssert.Contains("最大生命：4", Plain(Get<TMP_Text>("enemyProfileHealth").text));
        Assert.IsFalse(Get<RectTransform>("detailRegion").gameObject.activeSelf);
        overlay.ShowEnemySkillInfo(enemy, enemy.SpeedButtons[1]); Layout();
        Assert.IsFalse(Get<RectTransform>("enemyProfileRoot").gameObject.activeSelf);
        Assert.IsTrue(Get<RectTransform>("detailRegion").gameObject.activeSelf);
        Assert.AreEqual(3, Cards().Count);
    }

    [Test]
    public void MixedDiceAndLongRulesScrollWithoutMovingOverviewOrFooter()
    {
        BattleAllyController ally = Ally("Assets/Data/Weapons/000 WoodenSword/WoodenSword_Data.asset");
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>(); transient.Add(skill);
        skill.skillName = "混合多骰验证"; skill.diceCount = 3;
        skill.dicePowerRanges = new[] { Range(2, 5), Range(1, 4), Range(4, 7) };
        skill.diceSkillCategories = new[] { EnemySkill_SO.SkillCategory.Attack, EnemySkill_SO.SkillCategory.Defense, EnemySkill_SO.SkillCategory.Attack };
        skill.diceEffectSettings = new[] { new EnemySkill_SO.DiceEffectSettings { damageEffects = Enumerable.Range(1, 24).Select(i => new EnemySkill_SO.DamageEffectEntry { damageAmount = i, trigger = EnemySkill_SO.DiceEffectTrigger.OnHit, targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget }).ToArray() } };
        skill.description = string.Join("\n", Enumerable.Repeat("长说明仍须全部可达。", 24));
        ally.GrantTemporarySkill(skill, true); overlay.ShowAllySkillSelection(ally, ally.SpeedButtons[0]); Layout();
        Assert.AreEqual(3, Cards().Count); StringAssert.Contains("防御骰", Cards()[1].Find("DiceHeading").GetComponent<TMP_Text>().text);
        Assert.AreEqual("1 ~ 4", Plain(Cards()[1].Find("DiceRange").GetComponent<TMP_Text>().text));
        StringAssert.Contains("24", Plain(Cards()[0].Find("DiceClauses").GetComponent<TMP_Text>().text));
        ScrollRect scroll = Get<ScrollRect>("detailScrollRect");
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height * 3);
        Assert.IsTrue(scroll.verticalScrollbar.gameObject.activeSelf);
        RectTransform footer = (RectTransform)Get<Button>("footerClose").transform;
        Vector3 position = footer.position; scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
        Assert.AreEqual(position, footer.position);
        Assert.LessOrEqual(Get<TMP_Text>("descriptionBody").rectTransform.anchoredPosition.y * -1 + Get<TMP_Text>("descriptionBody").rectTransform.rect.height, scroll.content.rect.height + 1);
        Assert.IsFalse(Get<RectTransform>("summaryRoot").IsChildOf(scroll.content));
    }

    [TestCase(1920, 1080, 100, 30, 1700, 1000)]
    [TestCase(2400, 1080, 120, 0, 2180, 1080)]
    [TestCase(1600, 1200, 0, 40, 1600, 1120)]
    public void WindowIncludingTitleFitsSafeArea(float w, float h, float x, float y, float sw, float sh)
    {
        BattleAllyController ally = Ally("Assets/Data/Weapons/000 WoodenSword/WoodenSword_Data.asset"); overlay.ShowAllySkillSelection(ally, ally.SpeedButtons[0]);
        Layout(new Vector2(w, h), new Rect(x, y, sw, sh));
        RectTransform safe = Get<RectTransform>("safeRoot"), panel = Get<RectTransform>("panelRoot");
        foreach (RectTransform rect in new[] { panel, Get<RectTransform>("titlePlaque"), (RectTransform)Get<Button>("footerClose").transform })
        {
            Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 point = safe.InverseTransformPoint(corner);
                Assert.That(point.x, Is.InRange(safe.rect.xMin - 1, safe.rect.xMax + 1));
                Assert.That(point.y, Is.InRange(safe.rect.yMin - 1, safe.rect.yMax + 1));
            }
        }
    }

    [Test]
    public void HoverShowsEveryDieWithoutDimmingOrBlockingAndIgnoresStaleExit()
    {
        EnemyData_SO data = Asset<EnemyData_SO>("Assets/Data/Enemies/Enemies/014 BloodRat/BloodRat.asset");
        EnemySkill_SO skill = Asset<EnemySkill_SO>("Assets/Data/Enemies/Enemies/027 UtilityPole/HighVoltage.asset");
        BattleEnemyController enemy = BattleSkillVisualTools.CreateEnemy(host.transform, data, skill);
        overlay.ShowEnemyHoverSkillInfo(enemy, enemy.SpeedButtons[0]); Layout();
        string ranges = Plain(Get<TMP_Text>("hoverDetailText").text);
        StringAssert.Contains("第 1 攻击骰", ranges); StringAssert.Contains("第 2 攻击骰", ranges); StringAssert.Contains("第 3 攻击骰", ranges);
        StringAssert.Contains("1 ~ 1", ranges);
        Assert.IsFalse(overlay.IsVisible); Assert.IsFalse(Get<Button>("backdropButton").gameObject.activeSelf);
        Assert.IsFalse(Get<CanvasGroup>("hoverGroup").blocksRaycasts); Assert.IsFalse(SpeedButton.InputSuppressed);
        Assert.AreNotEqual(TextOverflowModes.Ellipsis, Get<TMP_Text>("hoverDetailText").overflowMode);
        overlay.ShowEnemyHoverSkillInfo(enemy, enemy.SpeedButtons[1]); overlay.HideEnemyHoverSkillInfo(enemy.SpeedButtons[0]);
        Assert.IsTrue(Get<RectTransform>("hoverPanelRoot").gameObject.activeSelf);
        overlay.ShowEnemySkillInfo(enemy, enemy.SpeedButtons[1]);
        Assert.IsFalse(Get<RectTransform>("hoverPanelRoot").gameObject.activeSelf);
        Assert.AreNotEqual(HideFlags.None, Get<Canvas>("canvas").gameObject.hideFlags & HideFlags.DontSaveInEditor);
    }

    private BattleAllyController Ally(string path) => BattleSkillVisualTools.CreateAlly(host.transform, Asset<Weapon_SO>(path));

    [Test]
    public void EnemyProfileUsesActualPortraitIdentityMaximumAndCompletePassiveWithoutDuplicateFields()
    {
        BattleEnemyController enemy = ProfileEnemy();
        overlay.ShowEnemyDetail(enemy); Layout();
        Assert.AreEqual("敌人详情", Get<TMP_Text>("titleText").text);
        Assert.AreEqual(enemy.enemyData.enemyName, Get<TMP_Text>("enemyProfileName").text);
        Assert.AreEqual("编号 14", Get<TMP_Text>("enemyProfileId").text);
        Assert.AreEqual("最大生命：4", Plain(Get<TMP_Text>("enemyProfileHealth").text));
        Assert.AreEqual(enemy.enemyData.enemyDescription, Get<TMP_Text>("enemyProfileDescription").text);
        Assert.AreSame(enemy.GetCurrentStageSprite(), Get<Image>("enemyPortrait").sprite);
        Assert.IsNotNull(Get<Image>("enemyProfileBackdrop").sprite);
        Assert.AreEqual(1, PassiveCards().Count);
        Assert.AreEqual("啮齿群", PassiveCards()[0].Find("PassiveName").GetComponent<TMP_Text>().text);
        Assert.AreEqual(enemy.enemyData.passiveDescriptions[0].description, Plain(PassiveCards()[0].Find("PassiveDescription").GetComponent<TMP_Text>().text));
        Assert.IsFalse(Get<ScrollRect>("enemyProfileScroll").verticalScrollbar.gameObject.activeSelf, "A single short passive must fit without a decorative scrollbar.");
        Assert.IsFalse(Get<TMP_Text>("enemyScrollHint").gameObject.activeSelf);
        Assert.IsFalse(Get<TMP_Text>("subtitleText").gameObject.activeInHierarchy);
        Assert.IsFalse(Get<RectTransform>("detailRegion").gameObject.activeInHierarchy);
        Assert.IsFalse(Get<RectTransform>("listArea").gameObject.activeInHierarchy);
        Assert.IsTrue(SpeedButton.InputSuppressed);
    }

    [Test]
    public void EnemyProfileRefreshUsesCurrentStageArtAndRuntimeMaximum()
    {
        BattleEnemyController enemy = ProfileEnemy();
        EnemyData_SO data = Object.Instantiate(enemy.enemyData); transient.Add(data); enemy.enemyData = data;
        Sprite alternate = Asset<EnemyData_SO>("Assets/Data/Enemies/Enemies/027 UtilityPole/UtilityPole_Data.asset").enemySprite;
        data.stageSprites = new List<EnemyData_SO.EnemyStageEntry>
        {
            new EnemyData_SO.EnemyStageEntry { stageSprite = data.enemySprite },
            new EnemyData_SO.EnemyStageEntry { stageSprite = alternate }
        };
        overlay.ShowEnemyDetail(enemy); Layout();
        Set(enemy, "<CurrentStageIndex>k__BackingField", 1); Set(enemy, "<MaxHP>k__BackingField", 17);
        overlay.RefreshCurrentView(); Layout();
        Assert.AreSame(alternate, Get<Image>("enemyPortrait").sprite);
        Assert.AreEqual("最大生命：17", Plain(Get<TMP_Text>("enemyProfileHealth").text));
        Assert.AreEqual(4, data.baseHP);
    }

    [Test]
    public void LongProfileCardsRemainCompleteAndScrollWithoutMovingPortraitIdentityOrFooter()
    {
        BattleEnemyController enemy = ProfileEnemy(); EnemyData_SO data = Object.Instantiate(enemy.enemyData); transient.Add(data); enemy.enemyData = data;
        data.enemyDescription = string.Join("\n", Enumerable.Repeat(data.enemyDescription, 8));
        data.passiveDescriptions = Enumerable.Range(0, 6).Select(i => new EnemyData_SO.PassiveDescriptionEntry
        {
            passiveName = "完整展示较长的特殊被动名称及其阶段条件 " + i,
            displayColorType = (EnemyData_SO.PassiveDisplayColorType)(i % 3),
            description = string.Join("\n", Enumerable.Repeat("当自身以外的 【嗜血啮鼠】 死亡时，给予自身 1 层虚弱，保留所有条件与延迟说明。", 4))
        }).ToList();
        overlay.ShowEnemyDetail(enemy); Layout();
        ScrollRect scroll = Get<ScrollRect>("enemyProfileScroll");
        Assert.AreEqual(6, PassiveCards().Count); Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height * 5);
        var fixedRects = new[] { Get<RectTransform>("enemyPortraitWell"), Get<RectTransform>("enemyIdBadge"), Get<TMP_Text>("enemyProfileName").rectTransform, Get<RectTransform>("enemyHealthBadge"), (RectTransform)Get<Button>("footerClose").transform };
        Vector3[] positions = fixedRects.Select(r => r.position).ToArray();
        for (int i = 0; i < 6; i++)
        {
            TMP_Text title = PassiveCards()[i].Find("PassiveName").GetComponent<TMP_Text>();
            TMP_Text body = PassiveCards()[i].Find("PassiveDescription").GetComponent<TMP_Text>();
            title.ForceMeshUpdate(); body.ForceMeshUpdate();
            Assert.IsFalse(title.isTextTruncated); Assert.IsFalse(body.isTextTruncated);
            Assert.AreEqual(data.passiveDescriptions[i].description, Plain(body.text));
            Assert.GreaterOrEqual(-body.rectTransform.anchoredPosition.y, -title.rectTransform.anchoredPosition.y + title.rectTransform.rect.height);
        }
        Transform lastCard = PassiveCards().Last();
        scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases(); overlay.RefreshCurrentView(); Layout();
        Assert.Less(scroll.verticalNormalizedPosition, .001f, "A refresh must not jump back to the first passive.");
        Assert.AreSame(lastCard, PassiveCards().Last(), "Refresh must reuse cards under an active pointer.");
        CollectionAssert.AreEqual(positions, fixedRects.Select(r => r.position).ToArray());
        Vector3 bottom = scroll.viewport.InverseTransformPoint(((RectTransform)lastCard).TransformPoint(new Vector3(0, -((RectTransform)lastCard).rect.height, 0)));
        Assert.That(bottom.y, Is.InRange(scroll.viewport.rect.yMin - 1, scroll.viewport.rect.yMin + 1));
        overlay.ShowEnemyDetail(enemy); Layout(); Assert.AreEqual(1, scroll.verticalNormalizedPosition, .0001f);
    }

    [Test]
    public void MissingPortraitAndEmptyPassivesUseReadableFallbacksAndNoBlankCards()
    {
        BattleEnemyController enemy = ProfileEnemy(); EnemyData_SO data = Object.Instantiate(enemy.enemyData); transient.Add(data); enemy.enemyData = data;
        data.enemySprite = null; data.stageSprites = null; data.enemyDescription = " ";
        data.passiveDescriptions = new List<EnemyData_SO.PassiveDescriptionEntry> { new EnemyData_SO.PassiveDescriptionEntry() };
        overlay.ShowEnemyDetail(enemy); Layout();
        Assert.IsFalse(Get<Image>("enemyPortrait").enabled); Assert.IsTrue(Get<TMP_Text>("enemyPortraitPlaceholder").gameObject.activeSelf);
        Assert.AreEqual("暂无描述。", Get<TMP_Text>("enemyProfileDescription").text);
        Assert.IsTrue(Get<TMP_Text>("enemyNoPassives").gameObject.activeSelf); Assert.AreEqual(0, PassiveCards().Count);
        data.passiveDescriptions.Add(new EnemyData_SO.PassiveDescriptionEntry { description = "没有标题的完整说明。" });
        data.passiveDescriptions.Add(new EnemyData_SO.PassiveDescriptionEntry { passiveName = "只有名称的被动" });
        overlay.RefreshCurrentView(); Layout(); Assert.AreEqual(2, PassiveCards().Count);
        Assert.IsFalse(PassiveCards()[0].Find("PassiveName").gameObject.activeSelf);
        Assert.IsFalse(PassiveCards()[1].Find("PassiveDescription").gameObject.activeSelf);
    }

    [Test]
    public void ProfilePresentationPreservesAuthoredRichTextAndDoesNotMutateSource()
    {
        const string raw = "<color=#33AAFF>12 层虚弱</color>与<noparse>【原样】 5</noparse>；【目标】获得 2 层虚弱。";
        MethodInfo formatter = typeof(BattleSkillInfoOverlay).GetMethod("FormatEnemyPassive", BindingFlags.NonPublic | BindingFlags.Static);
        string formatted = (string)formatter.Invoke(null, new object[] { raw });
        Assert.AreEqual(Plain(raw), Plain(formatted));
        StringAssert.Contains("<color=#33AAFF>12 层虚弱</color>", formatted);
        StringAssert.Contains("<noparse>【原样】 5</noparse>", formatted);
        StringAssert.Contains("<color=#63DADD>【目标】</color>", formatted);
    }

    [TestCase(1920, 1080, 100, 30, 1700, 1000)]
    [TestCase(2400, 1080, 120, 0, 2180, 1080)]
    [TestCase(1600, 1200, 0, 40, 1600, 1120)]
    public void EnemyProfileArtworkAndControlsFitSafeArea(float w, float h, float x, float y, float sw, float sh)
    {
        overlay.ShowEnemyDetail(ProfileEnemy()); Layout(new Vector2(w, h), new Rect(x, y, sw, sh));
        RectTransform safe = Get<RectTransform>("safeRoot");
        foreach (RectTransform rect in new[] { Get<Image>("enemyProfileBackdrop").rectTransform, Get<RectTransform>("titlePlaque"), (RectTransform)Get<Button>("footerClose").transform, (RectTransform)Get<Button>("closeButton").transform })
        {
            Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 point = safe.InverseTransformPoint(corner);
                Assert.That(point.x, Is.InRange(safe.rect.xMin - 1, safe.rect.xMax + 1));
                Assert.That(point.y, Is.InRange(safe.rect.yMin - 1, safe.rect.yMax + 1));
            }
        }
    }

    private BattleEnemyController ProfileEnemy() => BattleSkillVisualTools.CreateEnemy(host.transform,
        Asset<EnemyData_SO>("Assets/Data/Enemies/Enemies/014 BloodRat/BloodRat.asset"), Asset<EnemySkill_SO>("Assets/Data/Enemies/Enemies/014 BloodRat/RotfangBite.asset"));
    private List<Transform> PassiveCards() => Get<RectTransform>("enemyProfileContent").Cast<Transform>().Where(t => t.name.StartsWith("EnemyPassive_") && t.gameObject.activeSelf).ToList();
    private static T Asset<T>(string path) where T : Object { T asset = AssetDatabase.LoadAssetAtPath<T>(path); Assert.IsNotNull(asset, path); return asset; }
    private T Get<T>(string field) => (T)typeof(BattleSkillInfoOverlay).GetField(field, Private).GetValue(overlay);
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);
    private List<Transform> Cards() => Get<RectTransform>("detailContent").Cast<Transform>().Where(t => t.name.StartsWith("DiceCard_") && t.gameObject.activeSelf).ToList();
    private static string Plain(string text) => Regex.Replace(text, "<[^>]+>", string.Empty);
    private static EnemySkill_SO.DicePowerRange Range(int min, int max) => new EnemySkill_SO.DicePowerRange { minPower = min, maxPower = max };
    private void Layout(Vector2? pixels = null, Rect? safe = null)
    {
        Canvas canvas = Get<Canvas>("canvas"); canvas.GetComponent<CanvasScaler>().enabled = false; canvas.renderMode = RenderMode.WorldSpace;
        RectTransform root = (RectTransform)canvas.transform; root.anchorMin = root.anchorMax = root.pivot = Vector2.one * 0.5f;
        root.sizeDelta = pixels ?? new Vector2(1920, 1080); root.localScale = Vector3.one;
        Canvas.ForceUpdateCanvases(); Vector2 size = pixels ?? new Vector2(1920, 1080);
        overlay.ApplyViewport(size, safe ?? new Rect(0, 0, size.x, size.y)); overlay.AdvancePresentation(2); Canvas.ForceUpdateCanvases();
    }
}
