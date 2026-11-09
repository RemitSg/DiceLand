using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapInventoryDetailViewTests
{
    private GameObject host;
    private MapInventoryDetailView view;
    private LootItem_SO item;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("ItemDetailTests", typeof(RectTransform), typeof(Canvas));
        view = MapInventoryDetailView.Create((RectTransform)host.transform, "InventoryDetail");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        view.ConfigureFonts(font, font);
        item = AssetDatabase.LoadAssetAtPath<LootItem_SO>("Assets/Data/Loots/LootItems/004 磁电晶/MagtronicOrb.asset");
        view.Show(new RunLootState { lootItem = item, amount = 4 });
    }

    [TearDown] public void TearDown() => Object.DestroyImmediate(host);

    [TestCase(1920, 1080, 0, 0)]
    [TestCase(2340, 1080, 0, 0)]
    [TestCase(1600, 1200, 0, 0)]
    [TestCase(2400, 1080, 140, 50)]
    [TestCase(1080, 1920, 50, 30)]
    public void WindowPlaqueAndActionsFitSafeAreaAndContentZonesRemainSeparate(int width, int height, int left, int right)
    {
        Layout(width, height, new Rect(left, 24, width - left - right, height - 48));
        Contains(Bounds(Find("ItemDetailSafeArea")), Bounds(view.transform));
        Contains(Bounds(Find("ItemDetailSafeArea")), Bounds(Find("TitlePlaque")));
        foreach (string name in new[] { "Icon", "QuantityBadge", "Title", "RarityBadge", "CategoryBadge", "DescriptionWell", "CloseButton", "CloseXButton" })
            Contains(Bounds(view.transform), Bounds(Find(name)));
        Assert.IsFalse(Bounds(Find("Icon")).Overlaps(Bounds(Find("DescriptionWell"))));
        Assert.IsFalse(Bounds(Find("DescriptionWell")).Overlaps(Bounds(Find("CloseButton"))));
        Assert.IsFalse(Bounds(Find("RarityBadge")).Overlaps(Bounds(Find("CategoryBadge"))));
    }

    [Test]
    public void CrystalUsesRealArtAndSeparateAccurateMetadataWithReadableMeshes()
    {
        Layout();
        Assert.AreSame(item.icon, Field<Image>("iconImage").sprite);
        Assert.IsNotNull(Field<Image>("iconImage").GetComponent<MapItemIconFit>());
        Assert.AreEqual("磁电晶", Field<TMP_Text>("titleText").text);
        StringAssert.Contains("普通", Field<TMP_Text>("metaText").text);
        Assert.AreEqual("电", Find("Category").GetComponent<TMP_Text>().text);
        Assert.AreEqual("数量 ×4", Find("Quantity").GetComponent<TMP_Text>().text);
        Assert.AreEqual(item.description, Field<TMP_Text>("descriptionText").text);
        foreach (string name in new[] { "Heading", "Title", "Meta", "Category", "Quantity", "DescriptionHeading", "Description" })
        {
            TMP_Text text = Find(name).GetComponent<TMP_Text>(); text.ForceMeshUpdate(true, true);
            Assert.IsFalse(text.isTextOverflowing, name);
            Assert.IsTrue(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c => c.isVisible), name);
        }
    }

    [Test]
    public void EnemyNarrativeSkipsKeywordsWhileStatsAndPassiveKeepThem()
    {
        var enemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        try
        {
            enemy.enemyDescription = "以自残血礼为信条的癫信者。向目标追索生命。";
            string narrative = enemy.LocalizedDescription;
            string description = "最大生命：20\n" + narrative + "\n被动：恢复生命。";
            typeof(MapInventoryDetailView).GetMethod("PresentItem", Private).Invoke(view,
                new object[] { enemy, null, "裂肤信徒", string.Empty, Color.white, "敌人", 0, description, string.Empty });

            TMP_Text text = Field<TMP_Text>("descriptionText");
            string formatted = text.textPreprocessor.PreprocessText(text.text);
            StringAssert.Contains("<noparse>" + narrative + "</noparse>", formatted);
            Assert.AreEqual(2, formatted.Split(new[] { "<link=\"dl-resource-DL_Health\">" }, StringSplitOptions.None).Length - 1);
            text.ForceMeshUpdate(true, true);
            StringAssert.Contains(narrative, text.GetParsedText());
        }
        finally
        {
            Object.DestroyImmediate(enemy);
        }
    }

    [Test]
    public void ChangingItemClearsOldEquipmentActionAndPreservesAllFourKindsOfContent()
    {
        CraftingBlueprint_SO recipe = AssetDatabase.FindAssets("t:CraftingBlueprint_SO", new[] { "Assets/Data/Scroll" })
            .Select(g => AssetDatabase.LoadAssetAtPath<CraftingBlueprint_SO>(AssetDatabase.GUIDToAssetPath(g))).First(b => b.blueprintID == 202);
        view.Show(new RunWeaponState { weaponData = recipe.targetWeapon, amount = 2 });
        view.ConfigureActionButton("装备", true, () => Assert.Fail("Old weapon action survived."), "将装备给：Chara");
        Assert.IsTrue(Field<Button>("actionButton").gameObject.activeSelf);
        Layout();
        Assert.IsFalse(Bounds(Find("ActionButton")).Overlaps(Bounds(Find("CloseButton"))));
        view.Show(new RunBlueprintState { blueprintData = recipe, amount = 3 });
        Assert.IsFalse(Field<Button>("actionButton").gameObject.activeSelf);
        Assert.AreEqual("", Field<TMP_Text>("actionStatusText").text);
        Assert.AreSame(recipe.DisplayIcon, Field<Image>("iconImage").sprite);
        StringAssert.Contains(recipe.targetWeapon.weaponName, Field<TMP_Text>("descriptionText").text);
        foreach (CraftingMaterialRequirement requirement in recipe.materialRequirements)
            StringAssert.Contains(requirement.lootItem.lootName, Field<TMP_Text>("descriptionText").text);
        view.Show(new RunLootState { lootItem = item, amount = 4 });
        Assert.IsFalse(Field<TMP_Text>("relicAdditionalDescriptionText").gameObject.activeSelf);
    }

    [Test]
    public void LongRelicDescriptionAndSupplementScrollTogetherWithoutCoveringActions()
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        try
        {
            relic.relicName = "用于验证标题自动换行的非常长的森林遗物名称";
            relic.description = string.Join("\n", Enumerable.Repeat("获得金币并恢复行动点，保留完整的多段机制说明。", 32));
            relic.additionalDescription = "补充规则：每场战斗重置。\n" + string.Join("\n", Enumerable.Repeat("特殊条件与联动说明。", 8));
            var state = new RunRelicState { relicData = relic, amount = 2 };
            view.Show(state); Layout();
            Field<TMP_Text>("titleText").ForceMeshUpdate(true, true);
            Assert.IsFalse(Field<TMP_Text>("titleText").isTextOverflowing);
            ScrollRect scroll = Field<ScrollRect>("descriptionScroll");
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height * 2);
            Assert.IsTrue(Field<TMP_Text>("relicAdditionalDescriptionText").gameObject.activeSelf);
            Assert.AreEqual(relic.additionalDescription, Field<TMP_Text>("relicAdditionalDescriptionText").text);
            scroll.verticalNormalizedPosition = 0.25f; Canvas.ForceUpdateCanvases();
            float before = scroll.verticalNormalizedPosition;
            view.Show(state); Layout();
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(before).Within(0.01f));
            Assert.IsFalse(Bounds(Find("DescriptionWell")).Overlaps(Bounds(Find("CloseButton"))));
            view.Show(new RunLootState { lootItem = item, amount = 1 });
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(0).Within(0.1f));
        }
        finally { Object.DestroyImmediate(relic); }
    }

    [TestCase(2340, 1080)]
    [TestCase(1920, 1080)]
    [TestCase(1080, 1920)]
    public void ReadingAreaFitsSeveralLinesAndUsesAvailableWidth(int width, int height)
    {
        Layout(width, height);
        ScrollRect scroll = Field<ScrollRect>("descriptionScroll");
        Assert.GreaterOrEqual(scroll.viewport.rect.height, 300, "Long descriptions need a useful reading area.");
        Assert.Greater(scroll.viewport.rect.width, 450);
        Assert.IsFalse(Bounds(Find("QuantityBadge")).Overlaps(Bounds(Find("DescriptionWell"))));
    }

    [Test]
    public void RelicsKeepOriginalWindowWhileWeaponsAndBlueprintsUseLargerReadingAreas()
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        try
        {
            relic.relicName = "遗物窗口验证";
            view.Show(new RunRelicState { relicData = relic, amount = 1 }); Layout();
            Assert.AreEqual(new Vector2(1000, 560), ((RectTransform)view.transform).rect.size);
            Assert.AreEqual(136, Field<ScrollRect>("descriptionScroll").viewport.rect.height);
            CraftingBlueprint_SO blueprint = AssetDatabase.FindAssets("t:CraftingBlueprint_SO", new[] { "Assets/Data/Scroll" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CraftingBlueprint_SO>(AssetDatabase.GUIDToAssetPath(g))).First(b => b.blueprintID == 202);
            view.Show(new RunWeaponState { weaponData = blueprint.targetWeapon, amount = 1 }); Layout();
            Assert.Greater(((RectTransform)view.transform).rect.width, 1480);
            Assert.IsTrue(view.transform.Find("WeaponPresentation").gameObject.activeSelf);
            view.Show(new RunBlueprintState { blueprintData = blueprint, amount = 1 }); Layout();
            Assert.Greater(((RectTransform)view.transform).rect.width, 1200);
            Assert.Greater(Field<ScrollRect>("descriptionScroll").viewport.rect.height, 400);
        }
        finally { Object.DestroyImmediate(relic); }
    }

    [Test]
    public void EditorPreviewReusesAnEventSystemWithoutDestroyingOrReconfiguringLoadedOnes()
    {
        GameObject first = new GameObject("PreviewEventsA", typeof(UnityEngine.EventSystems.EventSystem));
        GameObject second = new GameObject("PreviewEventsB", typeof(UnityEngine.EventSystems.EventSystem));
        try
        {
            var firstSystem = first.GetComponent<UnityEngine.EventSystems.EventSystem>();
            firstSystem.enabled = false;
            var chosen = EventSystemUtility.EnsureSingleEventSystem();
            Assert.IsNotNull(chosen);
            Assert.IsNotNull(first); Assert.IsNotNull(second);
            Assert.IsFalse(firstSystem.enabled);
        }
        finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
    }

    [Test]
    public void HideIsIdempotentAndBattleFactoryUsesTheSameView()
    {
        int hidden = 0; view.Hidden += () => hidden++;
        view.Hide(); view.Hide(); Assert.AreEqual(1, hidden); Assert.IsFalse(view.IsVisible);
        GameObject battleHost = new GameObject("BattleDetailsIntegration");
        try
        {
            BattleSettlementOverlay overlay = battleHost.AddComponent<BattleSettlementOverlay>();
            TMP_FontAsset font = Field<TMP_Text>("descriptionText").font; overlay.ConfigureFonts(font, font);
            var data = new BattleSettlementData { isVictory = true };
            var entry = new BattleSettlementLootEntry { lootItem = item, amount = 1, displayName = item.lootName };
            data.lootEntries.Add(entry); overlay.ShowResult(data); overlay.AdvancePresentation(4);
            typeof(BattleSettlementOverlay).GetMethod("ShowLootDetailModal", Private).Invoke(overlay, new object[] { entry, null });
            var battleView = (MapInventoryDetailView)typeof(BattleSettlementOverlay).GetField("lootDetailView", Private).GetValue(overlay);
            Assert.IsTrue(battleView.IsVisible);
            Assert.AreEqual("磁电晶", battleView.transform.Find("Title").GetComponent<TMP_Text>().text);
            Assert.IsNotNull(battleView.transform.Find("TitlePlaque"));
            overlay.HideResult(); Assert.IsFalse(battleView.IsVisible);
        }
        finally { Object.DestroyImmediate(battleHost); }
    }

    private T Field<T>(string name) => (T)typeof(MapInventoryDetailView).GetField(name, Private).GetValue(view);
    private Transform Find(string name) => view.ModalRoot.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private void Layout(int width = 1920, int height = 1080, Rect? safe = null)
    {
        RectTransform rect = (RectTransform)host.transform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(1080f * width / height, 1080);
        view.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
        Canvas.ForceUpdateCanvases();
    }
    private static Rect Bounds(Transform target)
    {
        Vector3[] corners = new Vector3[4]; ((RectTransform)target).GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    private static void Contains(Rect outer, Rect inner)
    {
        Assert.GreaterOrEqual(inner.xMin, outer.xMin - 0.2f); Assert.GreaterOrEqual(inner.yMin, outer.yMin - 0.2f);
        Assert.LessOrEqual(inner.xMax, outer.xMax + 0.2f); Assert.LessOrEqual(inner.yMax, outer.yMax + 0.2f);
    }
}
