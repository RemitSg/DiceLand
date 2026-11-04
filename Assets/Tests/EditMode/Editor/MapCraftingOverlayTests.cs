using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapCraftingOverlayTests
{
    private GameObject host;
    private MapCraftingOverlay overlay;
    private RunState state;
    private CraftingBlueprint_SO blueprint;
    private LootItem_SO material;
    private int calls;
    private readonly List<Object> owned = new List<Object>();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("CraftingPanelTest");
        overlay = host.AddComponent<MapCraftingOverlay>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        overlay.ConfigureFonts(font, font);
        state = new RunState { money = 8 };
        material = Own<LootItem_SO>(); material.lootName = "测试矿石";
        blueprint = Recipe("测试卷轴", "测试战斧", material);
        state.AddBlueprint(blueprint);
        calls = 0;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (Object item in owned) Object.DestroyImmediate(item);
        owned.Clear();
    }

    [TestCase(2340, 1080, 0, 0)]
    [TestCase(1920, 1080, 0, 0)]
    [TestCase(1600, 1200, 0, 0)]
    [TestCase(2400, 1080, 140, 50)]
    [TestCase(1080, 1920, 50, 30)]
    public void ThreeColumnsPlaqueAndFooterFitTheSafeArea(int width, int height, int left, int right)
    {
        Show(); Layout(width, height, new Rect(left, 24, width - left - right, height - 48));
        Contains(Bounds(Find("SafeArea")), Bounds(Find("Panel")));
        Rect plaque = Bounds(Find("TitlePlaque")); plaque.yMax += 48 * Get<RectTransform>("panelRect").lossyScale.y;
        Contains(Bounds(Find("SafeArea")), plaque);
        string[] names = { "BlueprintScroll", "DescriptionScroll", "Preview", "MaterialScroll", "Availability", "CraftButton", "CloseButton" };
        foreach (string name in names) Contains(Bounds(Find("Panel")), Bounds(Find(name)));
        Assert.IsFalse(Bounds(Find("BlueprintScroll")).Overlaps(Bounds(Find("Preview"))));
        Assert.IsFalse(Bounds(Find("Preview")).Overlaps(Bounds(Find("MaterialScroll"))));
        Assert.IsFalse(Bounds(Find("MaterialScroll")).Overlaps(Bounds(Find("Availability"))));
        Assert.IsFalse(Bounds(Find("CraftButton")).Overlaps(Bounds(Find("CloseButton"))));
        Contains(Bounds(Find("Preview")), Bounds(Find("WeaponIcon")));
        Transform row = Find("Material_00");
        Contains(Bounds(row), Bounds(row.Find("Name"))); Contains(Bounds(row), Bounds(row.Find("Count")));
        Assert.IsFalse(Bounds(row.Find("Name")).Overlaps(Bounds(row.Find("Count"))));
    }

    [Test]
    public void PreviewUsesWeaponArtAndRecipeKeepsItsOwnScrollArt()
    {
        Texture2D texture = new Texture2D(4, 4); owned.Add(texture);
        Sprite scroll = Sprite.Create(texture, new Rect(0, 0, 2, 4), Vector2.one * 0.5f); owned.Add(scroll);
        Sprite weapon = Sprite.Create(texture, new Rect(2, 0, 2, 4), Vector2.one * 0.5f); owned.Add(weapon);
        blueprint.icon = scroll; blueprint.targetWeapon.icon = weapon;
        Show();
        Assert.AreSame(weapon, Get<Image>("iconImage").sprite);
        Assert.AreSame(scroll, Get<Image>("blueprintImage").sprite);
        Assert.AreEqual("优秀 · 斧", Get<TMP_Text>("metaText").text);
        Assert.IsTrue(Get<RectTransform>("blueprintArtRoot").gameObject.activeSelf);
        blueprint.targetWeapon.icon = null; Show();
        Assert.IsFalse(Get<Image>("iconImage").enabled);
    }

    [Test]
    public void MultipleBlueprintsUseScrollableThumbnailsAndAssetIdentityEvenWithIdenticalNames()
    {
        for (int i = 1; i < 13; i++) state.AddBlueprint(Recipe(blueprint.DisplayName, "成品 " + i, material));
        Show(); Layout();
        ScrollRect list = Get<ScrollRect>("blueprintScroll");
        Assert.AreEqual(13, list.content.childCount);
        Assert.IsTrue(list.verticalScrollbar.gameObject.activeSelf);
        Assert.IsFalse(Get<RectTransform>("blueprintArtRoot").gameObject.activeSelf);
        for (int i = 0; i < 5; i++) Contains(Bounds(list.viewport), Bounds(list.content.GetChild(i)));
        list.content.GetChild(1).GetComponent<Button>().onClick.Invoke();
        Assert.AreSame(state.blueprintInventory[1].blueprintData, Get<CraftingBlueprint_SO>("selectedBlueprint"));
        Assert.AreEqual("成品 1", Get<TMP_Text>("weaponNameText").text);
        Assert.AreSame(blueprint, state.blueprintInventory[0].blueprintData);
        Assert.AreEqual("缺材料", list.content.GetChild(1).Find("Readiness").GetComponent<TMP_Text>().text);
    }

    [Test]
    public void BlueprintReadinessHeadingsAndNamesProduceVisibleUnclippedTextMeshes()
    {
        state.AddBlueprint(Recipe("第二张图纸", "第二件成品", material));
        Show(); Layout();
        foreach (TMP_Text text in host.GetComponentsInChildren<TMP_Text>().Where(t => t.name == "Readiness" || t.name == "Label" || t.name == "Name" || t.name == "WeaponName"))
        {
            text.ForceMeshUpdate(true, true);
            Assert.IsFalse(text.isTextOverflowing, text.transform.parent.name + "/" + text.name + ": " + text.text);
            Assert.IsTrue(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c => c.isVisible), text.text);
        }
    }

    [Test]
    public void ReopeningWithDifferentInventoryClearsStaleSelectionStatusAndIcons()
    {
        Show();
        var other = new RunState(); var second = Recipe("新图纸", "新武器", material); other.AddBlueprint(second);
        overlay.Show(other, Craft);
        Assert.AreSame(second, Get<CraftingBlueprint_SO>("selectedBlueprint"));
        overlay.Show(new RunState(), Craft);
        Assert.IsNull(Get<CraftingBlueprint_SO>("selectedBlueprint"));
        Assert.AreEqual("", Get<TMP_Text>("statusText").text);
        Assert.IsFalse(Get<Button>("craftButton").interactable);
        Assert.IsFalse(Get<Image>("blueprintImage").enabled);
        Assert.IsFalse(Get<Image>("iconImage").enabled);
        Assert.IsTrue(Find("EmptyBlueprints").gameObject.activeSelf);
    }

    [Test]
    public void MaterialsAndRealCalamityFeeUseSharedValidationWithoutCallingInvalidHandler()
    {
        Calamity_SO calamity = Own<Calamity_SO>();
        calamity.builtInEffects.Add(new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.ForgeCostFlatModifier, effectAmount = 3 });
        state.AddCalamity(calamity, 1); state.money = 2; state.AddLoot(material, 2);
        Show();
        Assert.AreEqual("金币", Find("Material_00").Find("Name").GetComponent<TMP_Text>().text);
        Assert.AreEqual("2 / 3", Find("Material_00").Find("Count").GetComponent<TMP_Text>().text);
        Get<Button>("craftButton").onClick.Invoke(); Assert.AreEqual(0, calls);
        state.money = 8; Show(); Assert.IsTrue(Get<Button>("craftButton").interactable);
        Get<Button>("craftButton").onClick.Invoke();
        Assert.AreEqual(1, calls); Assert.AreEqual(5, state.money); Assert.AreEqual(0, state.GetLootAmount(material));
        Assert.AreEqual(1, state.GetWeaponAmount(blueprint.targetWeapon)); Assert.AreEqual(1, state.GetBlueprintAmount(blueprint));
        Assert.AreEqual("0 / 2", Find("Material_01").Find("Count").GetComponent<TMP_Text>().text);
        StringAssert.Contains("成功锻造", Get<TMP_Text>("statusText").text);
        TMP_Text status = Get<TMP_Text>("statusText"); status.ForceMeshUpdate(true, true);
        Assert.IsFalse(status.isTextOverflowing);
        Assert.IsTrue(status.textInfo.characterInfo.Take(status.textInfo.characterCount).Any(c => c.isVisible));
        Get<Button>("craftButton").onClick.Invoke(); Assert.AreEqual(1, calls);
    }

    [Test]
    public void CraftKeepsSelectionScrollAndUpdatesOtherRecipesSharingTheMaterial()
    {
        for (int i = 0; i < 12; i++) state.AddBlueprint(Recipe("卷轴 " + i, "成品 " + i, material));
        state.AddLoot(material, 2); Show(); Layout();
        ScrollRect list = Get<ScrollRect>("blueprintScroll");
        list.verticalNormalizedPosition = 0.4f; Canvas.ForceUpdateCanvases();
        Vector2 before = list.content.anchoredPosition;
        var selected = Get<CraftingBlueprint_SO>("selectedBlueprint");
        Get<Button>("craftButton").onClick.Invoke(); Canvas.ForceUpdateCanvases();
        Assert.AreSame(selected, Get<CraftingBlueprint_SO>("selectedBlueprint"));
        Assert.That(list.content.anchoredPosition.y, Is.EqualTo(before.y).Within(0.1f));
        foreach (Transform card in list.content) Assert.AreEqual("缺材料", card.Find("Readiness").GetComponent<TMP_Text>().text);
    }

    [Test]
    public void DuplicateRequirementsAreAggregatedAndLongMaterialNamesStaySeparateFromCounts()
    {
        material.lootName = "用于验证长名称布局的非常长的森林矿石材料";
        blueprint.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = material, amount = 3 });
        for (int i = 0; i < 10; i++)
        {
            LootItem_SO extra = Own<LootItem_SO>(); extra.lootName = "额外材料 " + i;
            blueprint.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = extra, amount = i + 1 });
        }
        Show(); Layout();
        ScrollRect scroll = Get<ScrollRect>("materialScroll");
        Assert.AreEqual(11, scroll.content.childCount);
        Assert.AreEqual("0 / 5", scroll.content.GetChild(0).Find("Count").GetComponent<TMP_Text>().text);
        Assert.IsTrue(scroll.verticalScrollbar.gameObject.activeSelf);
        foreach (Transform row in scroll.content)
        {
            Assert.IsFalse(Bounds(row.Find("Name")).Overlaps(Bounds(row.Find("Count"))));
            TMP_Text name = row.Find("Name").GetComponent<TMP_Text>();
            Assert.GreaterOrEqual(name.rectTransform.rect.height, name.GetPreferredValues(name.rectTransform.rect.width, 0).y);
        }
    }

    [Test]
    public void WeaponSealAndAnimatedButtonsKeepTheirVisualCenters()
    {
        Show(); Layout();
        Rect weapon = Bounds(Find("WeaponIcon")), seal = Bounds(Find("EngravedSeal"));
        Assert.That(Vector2.Distance(weapon.center, seal.center), Is.LessThan(0.01f));
        foreach (string name in new[] { "CraftButton", "CloseButton", "CloseXButton", "Blueprint_00" })
        {
            Transform button = Find(name); Vector2 before = Bounds(button).center;
            button.localScale = Vector3.one * 0.965f;
            Assert.That(Vector2.Distance(before, Bounds(button).center), Is.LessThan(0.01f), name);
        }
        foreach (string name in new[] { "Title", "WeaponName" })
        {
            TMP_Text text = Find(name).GetComponent<TMP_Text>(); text.ForceMeshUpdate(true, true);
            Assert.That(text.textBounds.center.y, Is.EqualTo(text.rectTransform.rect.center.y).Within(1f), name);
        }
    }

    [Test]
    public void HideIsIdempotentAndShowWithoutHandlerCannotForge()
    {
        int hidden = 0; overlay.Hidden += () => hidden++;
        state.AddLoot(material, 5); overlay.Show(state, null);
        Assert.IsFalse(Get<Button>("craftButton").interactable);
        overlay.Hide(); overlay.Hide(); Assert.AreEqual(1, hidden); Assert.IsFalse(overlay.IsVisible);
        Show(); Assert.IsTrue(overlay.IsVisible); overlay.Hide(); Assert.AreEqual(2, hidden);
    }

    private void Show() => overlay.Show(state, Craft);
    private string Craft(CraftingBlueprint_SO recipe) { calls++; RunCraftingService.TryCraft(state, recipe, out string message); return message; }
    private CraftingBlueprint_SO Recipe(string name, string weaponName, LootItem_SO ingredient)
    {
        var recipe = Own<CraftingBlueprint_SO>(); recipe.blueprintName = name; recipe.rarity = LootRarity.Uncommon;
        recipe.description = "记载着如何打造【" + weaponName + "】的卷轴";
        recipe.targetWeapon = Own<Weapon_SO>(); recipe.targetWeapon.weaponName = weaponName; recipe.targetWeapon.weaponType = WeaponType.Axe;
        recipe.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = ingredient, amount = 2 }); return recipe;
    }
    private T Own<T>() where T : ScriptableObject { T item = ScriptableObject.CreateInstance<T>(); owned.Add(item); return item; }
    private T Get<T>(string name) => (T)typeof(MapCraftingOverlay).GetField(name, Private).GetValue(overlay);
    private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name && t.gameObject.activeInHierarchy);
    private void Layout(int width = 2340, int height = 1080, Rect? safe = null)
    {
        Canvas canvas = host.GetComponentInChildren<Canvas>(); canvas.GetComponent<CanvasScaler>().enabled = false; canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = (RectTransform)canvas.transform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(1080f * width / height, 1080);
        overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
        typeof(MapCraftingOverlay).GetMethod("RefreshLayout", Private).Invoke(overlay, null); Canvas.ForceUpdateCanvases();
    }
    private static Rect Bounds(Transform target) { var corners = new Vector3[4]; ((RectTransform)target).GetWorldCorners(corners); return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y); }
    private static void Contains(Rect outer, Rect inner) { Assert.GreaterOrEqual(inner.xMin, outer.xMin - 0.2f); Assert.GreaterOrEqual(inner.yMin, outer.yMin - 0.2f); Assert.LessOrEqual(inner.xMax, outer.xMax + 0.2f); Assert.LessOrEqual(inner.yMax, outer.yMax + 0.2f); }
}
