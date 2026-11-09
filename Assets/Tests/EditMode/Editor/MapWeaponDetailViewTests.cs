using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapWeaponDetailViewTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject host;
    private MapInventoryDetailView view;
    private Weapon_SO sword;
    private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();

    [SetUp] public void SetUp()
    {
        host = new GameObject("WeaponDetailTests", typeof(RectTransform), typeof(Canvas));
        view = MapInventoryDetailView.Create((RectTransform)host.transform, "InventoryDetail");
        MapTreasureArt art = MapTreasureArt.Load(); view.ConfigureFonts(art.headingFont, art.bodyFont);
        view.ConfigureInventoryContext(new RunState());
        sword = AssetDatabase.LoadAssetAtPath<Weapon_SO>("Assets/Data/Weapons/006 曦誓剑/006 曦誓剑.asset");
        view.Show(new RunWeaponState { weaponData = sword, amount = 7 }); Layout();
    }

    [TearDown] public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (ScriptableObject asset in temporary) Object.DestroyImmediate(asset);
        temporary.Clear();
    }

    [Test] public void EveryRealWeaponUsesItsOwnSkillsCostsCategoriesAndIndividualRanges()
    {
        Weapon_SO[] weapons = AssetDatabase.FindAssets("t:Weapon_SO", new[] { "Assets/Data" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<Weapon_SO>(AssetDatabase.GUIDToAssetPath(guid))).Where(w => w != null).ToArray();
        Assert.IsNotEmpty(weapons);
        foreach (Weapon_SO weapon in weapons)
        {
            view.Show(new RunWeaponState { weaponData = weapon, amount = 7 }); Layout();
            EnemySkill_SO[] expected = weapon.skillPool?.Where(skill => skill != null).Distinct().ToArray() ?? new EnemySkill_SO[0];
            object[] cards = Cards(); Assert.AreEqual(Mathf.Max(1, expected.Length), cards.Length, weapon.name);
            Assert.AreEqual(weapon.LocalizedName, Field<TMP_Text>(view, "titleText").text);
            StringAssert.Contains("7", Field<TMP_Text>(view, "quantityText").text);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].LocalizedName, Field<TMP_Text>(cards[i], "name").text);
                StringAssert.Contains(Mathf.Max(0, expected[i].energyCost).ToString(), Field<TMP_Text>(cards[i], "energy").text);
                Assert.AreEqual(Mathf.Max(0, expected[i].diceCount), Field<int>(cards[i], "diceCount"));
                IList dice = Field<IList>(cards[i], "dice");
                for (int die = 0; die < expected[i].diceCount; die++)
                {
                    EnemySkill_SO.DicePowerRange range = expected[i].GetDicePowerRange(die);
                    Assert.AreEqual(range.minPower + " ~ " + range.maxPower, Field<TMP_Text>(dice[die], "range").text);
                    Assert.AreEqual(expected[i].GetDiceSkillCategory(die) == EnemySkill_SO.SkillCategory.Defense,
                        Field<BattleSkillFrameGraphic>(dice[die], "icon").defense);
                }
            }
        }
    }

    [Test] public void MultiDiceRulesStayWithTheirOwnDieAndUseConditionsHaveNoInventedBattleState()
    {
        object card = Cards().Single(value => Field<TMP_Text>(value, "name").text == "圣裁连斩");
        IList dice = Field<IList>(card, "dice"); Assert.AreEqual(2, Field<int>(card, "diceCount"));
        string first = Plain(Field<TMP_Text>(dice[0], "rules").text), second = Plain(Field<TMP_Text>(dice[1], "rules").text);
        StringAssert.Contains("至少2种负面状态", first); StringAssert.Contains("额外造成2点伤害", first);
        StringAssert.DoesNotContain("全部攻击段命中", first);
        StringAssert.Contains("全部攻击段命中", second); StringAssert.Contains("2层护盾", second);
        Assert.AreEqual("武器详情", Field<TMP_Text>(view, "headingText").text);
        Assert.AreEqual("返回背包", Field<TMP_Text>(view, "closeLabel").text);
    }

    [TestCase(2340,1080,0,0)]
    [TestCase(1920,1080,0,0)]
    [TestCase(1600,1200,0,0)]
    [TestCase(2400,1080,140,50)]
    [TestCase(1080,1920,30,24)]
    public void SafeAreaHeaderProfileSkillsAndEquipmentActionsStayInsideTheirZones(int width, int height, int left, int right)
    {
        view.ConfigureActionButton("装备", true, () => { }, "将装备给：Chara");
        Layout(width, height, new Rect(left,24,width-left-right,height-48));
        Contains(Bounds(view.ModalRoot.Find("ItemDetailSafeArea")), Bounds(view.transform));
        Contains(Bounds(view.ModalRoot.Find("ItemDetailSafeArea")), Bounds(view.transform.Find("TitlePlaque")));
        Rect skills = Bounds(Field<RectTransform>(view, "weaponScrollRoot"));
        Assert.IsFalse(skills.Overlaps(Bounds(view.transform.Find("ActionButton"))));
        Assert.IsFalse(skills.Overlaps(Bounds(view.transform.Find("CloseButton"))));
        Assert.IsFalse(Bounds(view.transform.Find("CloseButton")).Overlaps(Bounds(view.transform.Find("ActionButton"))));
        Assert.IsFalse(Bounds(Field<RectTransform>(view, "descriptionWell")).Overlaps(skills));
        TMP_Text title = Field<TMP_Text>(view, "headingText"); title.ForceMeshUpdate(true,true);
        Assert.IsFalse(title.isTextOverflowing);
        if (Field<RectTransform>(view,"weaponViewport").rect.width >= 1000)
        {
            object[] cards=Cards();
            Assert.That(Bounds(Field<RectTransform>(cards[0],"rect")).yMax,
                Is.EqualTo(Bounds(Field<RectTransform>(cards[1],"rect")).yMax).Within(.1f));
            Assert.IsFalse(Bounds(Field<RectTransform>(cards[0],"rect")).Overlaps(Bounds(Field<RectTransform>(cards[2],"rect"))));
        }
    }

    [Test] public void LongRulesMixedDiceAndRepeatedEquipmentRefreshPreserveReadingPosition()
    {
        Weapon_SO weapon = Object.Instantiate(sword); temporary.Add(weapon);
        EnemySkill_SO skill = Object.Instantiate(sword.skillPool[0]); temporary.Add(skill);
        skill.localizationKey = "weapon-details-long-test";
        skill.skillName = "很长的技能名称也应完整展示并自然换行";
        skill.diceCount = 3;
        skill.dicePowerRanges = new[] { Range(1,2), Range(3,7), Range(8,11) };
        skill.diceSkillCategories = new[] { EnemySkill_SO.SkillCategory.Attack, EnemySkill_SO.SkillCategory.Defense, EnemySkill_SO.SkillCategory.Attack };
        skill.diceEffectSettings = new EnemySkill_SO.DiceEffectSettings[3];
        skill.diceEffectSettings[1].damageEffects = Enumerable.Range(100,90).Select(value => new EnemySkill_SO.DamageEffectEntry { damageAmount = value }).ToArray();
        skill.description = string.Join("\n", Enumerable.Repeat("保留完整的技能说明，滚动后也能查看全部内容。",16));
        weapon.skillPool = new[] { skill };
        var state = new RunWeaponState { weaponData = weapon, amount = 2 };
        view.Show(state); Layout();
        object card = Cards()[0]; IList dice = Field<IList>(card,"dice");
        Assert.AreEqual("3 ~ 7",Field<TMP_Text>(dice[1],"range").text);
        Assert.IsTrue(Field<BattleSkillFrameGraphic>(dice[1],"icon").defense);
        StringAssert.Contains("189",Plain(Field<TMP_Text>(dice[1],"rules").text));
        Field<RectTransform>(card,"rect").GetComponent<Button>().onClick.Invoke(); Layout();
        Assert.IsTrue(Field<RectTransform>(card,"descriptionClip").gameObject.activeSelf);
        ScrollRect scroll = Field<ScrollRect>(view,"weaponScroll");
        Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height*3);
        scroll.verticalNormalizedPosition=.3f; Canvas.ForceUpdateCanvases();
        float before=scroll.verticalNormalizedPosition;
        view.Show(state); view.ConfigureActionButton("当前装备",false,null,"当前装备者：Chara"); Layout();
        Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(before).Within(.015f));
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>(); temporary.Add(loot); loot.lootName="切换后的材料";
        view.Show(new RunLootState { lootItem=loot,amount=1 }); Layout();
        Assert.IsFalse(Field<RectTransform>(view,"weaponRoot").gameObject.activeSelf);
        Assert.IsTrue(Field<ScrollRect>(view,"descriptionScroll").vertical);
        Assert.IsFalse(Field<Button>(view,"actionButton").gameObject.activeSelf);
    }

    [Test] public void EmptyPoolsNullEntriesAndZeroDiceAreReadableWithoutStaleRows()
    {
        Weapon_SO weapon = Object.Instantiate(sword); temporary.Add(weapon);
        weapon.skillPool = new EnemySkill_SO[] { null, sword.skillPool[1], null, sword.skillPool[1] };
        view.Show(new RunWeaponState { weaponData=weapon,amount=1 }); Layout();
        Assert.AreEqual(1,Cards().Length);
        Assert.IsFalse(Field<RectTransform>(Cards()[0],"defaultBadge").gameObject.activeSelf);
        weapon.skillPool=null; weapon.minPower=5; weapon.maxPower=9;
        view.Show(new RunWeaponState { weaponData=weapon,amount=1 }); Layout();
        object fallback=Cards()[0]; Assert.AreEqual(1,Field<int>(fallback,"diceCount"));
        Assert.AreEqual("5 ~ 9",Field<TMP_Text>(Field<IList>(fallback,"dice")[0],"range").text);
        EnemySkill_SO zero = ScriptableObject.CreateInstance<EnemySkill_SO>(); temporary.Add(zero); zero.skillName="无骰技能"; zero.diceCount=0;
        weapon.skillPool=new[]{zero}; view.Show(new RunWeaponState { weaponData=weapon,amount=1 }); Layout();
        Assert.AreEqual(0,Field<int>(Cards()[0],"diceCount"));
        foreach(object row in Field<IList>(Cards()[0],"dice")) Assert.IsFalse(Field<RectTransform>(row,"header").gameObject.activeSelf);
    }

    [Test] public void RequiredStatusIsReadFromConfigurationWithoutAnInventedCurrentStackCount()
    {
        Weapon_SO weapon=Object.Instantiate(sword); temporary.Add(weapon);
        EnemySkill_SO skill=Object.Instantiate(sword.skillPool[0]); temporary.Add(skill);
        skill.requiredSelfStatusData=sword.skillPool[0].GetDiceEffectSettings(0).powerModifiers[0].targetStatusData;
        Assert.IsNotNull(skill.requiredSelfStatusData);
        skill.requiredSelfStatusStacks=7; skill.keepRequiredSelfStatusAfterUse=false; weapon.skillPool=new[]{skill};
        view.Show(new RunWeaponState {weaponData=weapon,amount=1}); Layout();
        string requirements=Plain(Field<TMP_Text>(Cards()[0],"requirements").text);
        StringAssert.Contains("至少拥有7层",requirements); StringAssert.Contains("施放后消耗",requirements);
        StringAssert.DoesNotContain("当前",requirements);
        skill.keepRequiredSelfStatusAfterUse=true;
        view.Show(new RunWeaponState {weaponData=weapon,amount=1}); Layout();
        StringAssert.Contains("施放后不消耗",Plain(Field<TMP_Text>(Cards()[0],"requirements").text));
    }

    private object[] Cards() => Field<IList>(view,"weaponCards").Cast<object>().Where(card => Field<RectTransform>(card,"rect").gameObject.activeSelf).ToArray();
    private static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,Members).GetValue(target);
    private static string Plain(string text) => Regex.Replace(Regex.Replace(text,"<[^>]+>",""),@"\s+","");
    private static EnemySkill_SO.DicePowerRange Range(int min,int max) => new EnemySkill_SO.DicePowerRange { minPower=min,maxPower=max };
    private void Layout(int width=2340,int height=1080,Rect? safe=null)
    {
        RectTransform canvas=(RectTransform)host.transform; canvas.sizeDelta=new Vector2(1080f*width/height,1080);
        view.ApplyViewport(new Vector2(width,height),safe??new Rect(0,0,width,height)); Canvas.ForceUpdateCanvases();
    }
    private static Rect Bounds(Transform target)
    {
        var corners=new Vector3[4]; ((RectTransform)target).GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }
    private static void Contains(Rect outer,Rect inner)
    {
        Assert.GreaterOrEqual(inner.xMin,outer.xMin-.5f); Assert.GreaterOrEqual(inner.yMin,outer.yMin-.5f);
        Assert.LessOrEqual(inner.xMax,outer.xMax+.5f); Assert.LessOrEqual(inner.yMax,outer.yMax+.5f);
    }
}
