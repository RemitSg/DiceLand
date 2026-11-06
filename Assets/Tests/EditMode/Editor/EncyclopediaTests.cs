using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class EncyclopediaTests
{
    private EncyclopediaCatalog catalog;
    private TextAsset identities;
    private EncyclopediaProgress progress;
    private string saveKey;
    private GameObject host;

    [SetUp]
    public void SetUp()
    {
        catalog = Resources.Load<EncyclopediaCatalog>(EncyclopediaCatalog.ResourcePath);
        identities = Resources.Load<TextAsset>(EncyclopediaCatalog.IdentityPath);
        Assert.IsNotNull(catalog);
        Assert.IsNotNull(identities);
        saveKey = "test.encyclopedia." + Guid.NewGuid().ToString("N");
        progress = new EncyclopediaProgress(identities.text, saveKey);
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null)
            Object.DestroyImmediate(host);
        if (saveKey != null)
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void ShippedIndexUsesUniqueAssetIdentitiesAndCoversEachCategory()
    {
        EncyclopediaCatalog.Entry[] expected = EncyclopediaCatalogBuilder.FindEntries();
        CollectionAssert.AreEqual(expected.Select(entry => entry.Id), catalog.Entries.Select(entry => entry.Id));
        Assert.AreEqual(EncyclopediaCatalogBuilder.BuildIdentityIndex(expected), identities.text.Replace("\r\n", "\n"));
        Assert.AreEqual(catalog.Entries.Count, catalog.Entries.Select(entry => entry.Id).Distinct().Count());
        Assert.AreEqual(5, catalog.Entries.Select(entry => entry.Category).Distinct().Count());
        Assert.IsTrue(catalog.Entries.All(entry => (entry.Icon == null) == (entry.Category == EncyclopediaCategory.Calamities)));
        Assert.IsNotNull(catalog.HeadingFont);
        Assert.IsNotNull(catalog.BodyFont);
    }

    [Test]
    public void RelicChoiceUnlocksOnlyTheItemActuallyGrantedAndSurvivesAProfileReload()
    {
        EncyclopediaCatalog.Entry[] entries = Entries(EncyclopediaCategory.Relics)
            .Where(entry => entry.Rarity == LootRarity.Legendary).Take(3).ToArray();
        var run = new RunState();
        run.TrackEncyclopedia(progress);
        var choice = new BattleSettlementRelicChoiceData
        {
            choices = entries.Select(entry => new BattleSettlementLootEntry
            {
                relicData = (Relic_SO)entry.Asset,
                amount = 1,
                rarity = LootRarity.Legendary
            }).ToList()
        };

        Assert.IsFalse(TreasureRewardService.ApplyRelicChoice(run, choice).success);
        Assert.IsFalse(entries.Any(progress.IsUnlocked));
        choice.selectedChoice = choice.choices[1];
        Assert.IsTrue(TreasureRewardService.ApplyRelicChoice(run, choice).success);
        Assert.IsTrue(run.TryConsumeRelic(choice.selectedChoice.relicData, 1));
        var reloaded = new EncyclopediaProgress(identities.text, saveKey);
        CollectionAssert.AreEqual(new[] { false, true, false }, entries.Select(reloaded.IsUnlocked));
    }

    [Test]
    public void ActualRunOwnershipCountsEquippedWeaponsGrantedLootAndAppliedCalamities()
    {
        EncyclopediaCatalog.Entry weapon = Entries(EncyclopediaCategory.Weapons).First();
        EncyclopediaCatalog.Entry loot = Entries(EncyclopediaCategory.Loot).First();
        EncyclopediaCatalog.Entry[] calamities = Entries(EncyclopediaCategory.Calamities).Take(2).ToArray();
        var run = new RunState();
        run.AddPartyMember(new RunPartyMemberState { weaponData = (Weapon_SO)weapon.Asset });
        Assert.IsFalse(progress.IsUnlocked(weapon));
        run.TrackEncyclopedia(progress);
        Assert.IsTrue(progress.IsUnlocked(weapon));

        RunState snapshot = run.Clone();
        snapshot.AddLoot((LootItem_SO)loot.Asset, 0);
        Assert.IsFalse(progress.IsUnlocked(loot));
        snapshot.AddLoot((LootItem_SO)loot.Asset, 1);
        Assert.IsTrue(progress.IsUnlocked(loot));

        Calamity_SO[] options = calamities.Select(entry => (Calamity_SO)entry.Asset).ToArray();
        CalamitySelectionService.MarkChoicesDrawn(snapshot, options);
        Assert.IsFalse(calamities.Any(progress.IsUnlocked));
        CalamitySelectionService.ApplyChosenCalamity(snapshot, options[1], 1);
        CollectionAssert.AreEqual(new[] { false, true }, calamities.Select(progress.IsUnlocked));
    }

    [Test]
    public void BrowserKeepsLockedSlotsInOrderWrapsAtEightAndUsesReadOnlyModal()
    {
        EncyclopediaCatalog.Entry[] legendary = Entries(EncyclopediaCategory.Relics)
            .Where(entry => entry.Rarity == LootRarity.Legendary).Take(9).ToArray();
        progress.Record(legendary[1].Asset);
        EncyclopediaCatalog.Entry calamity = Entries(EncyclopediaCategory.Calamities).First();
        progress.Record(calamity.Asset);

        host = new GameObject("Encyclopedia test");
        var overlay = host.AddComponent<MainMenuEncyclopediaOverlay>();
        overlay.Show(catalog, progress, null);
        Canvas canvas = overlay.PresentationCanvas;
        canvas.renderMode = RenderMode.WorldSpace;
        Resize(overlay, 2340, 1080);
        Transform root = canvas.transform;
        Find(root, "RelicsTab").GetComponent<Button>().onClick.Invoke();
        StringAssert.Contains("%", Find(root, "ProgressCount").GetComponent<TMP_Text>().text);
        string percentage = (100f / Entries(EncyclopediaCategory.Relics).Count(entry => entry.Rarity == LootRarity.Legendary))
            .ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
        StringAssert.Contains(percentage, Find(root, "Section_Legendary").GetComponentsInChildren<TMP_Text>()
            .Single(text => text.name == "Count").text);
        Assert.AreEqual("战利品", Find(root, "LootTab").GetComponentInChildren<TMP_Text>().text);
        var cards = legendary.Select(entry => Find(root, "Entry_" + entry.Id)).ToArray();

        Assert.IsNull(cards[0].GetComponent<Button>());
        Assert.IsNull(cards[0].Find("Icon"));
        Assert.AreEqual(legendary[1].DisplayName, cards[1].Find("EntryName").GetComponent<TMP_Text>().text);
        Assert.AreEqual(cards[0].anchoredPosition.y, cards[7].anchoredPosition.y);
        Assert.Less(cards[8].anchoredPosition.y, cards[0].anchoredPosition.y);
        Assert.Greater(cards[1].anchoredPosition.x, cards[0].anchoredPosition.x);

        ScrollRect scroll = Find(root, "EncyclopediaScroll").GetComponent<ScrollRect>();
        scroll.verticalNormalizedPosition = .82f;
        float scrollBefore = scroll.verticalNormalizedPosition;
        cards[1].GetComponent<Button>().onClick.Invoke();
        MapInventoryDetailView modal = root.GetComponentInChildren<MapInventoryDetailView>(true);
        Assert.IsTrue(modal.IsVisible);
        Assert.Greater(((RectTransform)modal.transform).rect.width, 1200, "Encyclopedia relics retain the enlarged reading window.");
        Assert.AreEqual(legendary[1].DisplayName, Find(modal.ModalRoot, "Title").GetComponent<TMP_Text>().text);
        Assert.IsFalse(Find(modal.ModalRoot, "QuantityBadge").gameObject.activeSelf);
        Assert.IsFalse(Find(root, "EncyclopediaPanel").GetComponent<CanvasGroup>().interactable);
        overlay.Back();
        Assert.IsFalse(modal.IsVisible);
        Assert.AreEqual(scrollBefore, scroll.verticalNormalizedPosition, .001f);

        Resize(overlay, 800, 1080);
        Assert.Less(cards[4].anchoredPosition.y, cards[0].anchoredPosition.y);
        Find(root, "CalamitiesTab").GetComponent<Button>().onClick.Invoke();
        RectTransform calamityCard = Find(root, "Entry_" + calamity.Id);
        Assert.IsNull(calamityCard.Find("Icon"));
        calamityCard.GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(modal.IsVisible);
        Assert.IsFalse(Find(modal.ModalRoot, "Icon").GetComponent<Image>().enabled);
        Assert.IsFalse(Find(modal.ModalRoot, "DisplayEngraving").gameObject.activeSelf);
    }

    private EncyclopediaCatalog.Entry[] Entries(EncyclopediaCategory category) =>
        catalog.Entries.Where(entry => entry.Category == category).ToArray();

    private static RectTransform Find(Transform parent, string name) =>
        parent.GetComponentsInChildren<RectTransform>(true).Single(rect => rect.name == name);

    private static void Resize(MainMenuEncyclopediaOverlay overlay, float width, float height)
    {
        ((RectTransform)overlay.PresentationCanvas.transform).sizeDelta = new Vector2(width, height);
        overlay.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
    }
}
