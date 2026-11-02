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

public sealed class ResourceKeywordTextTests
{
    private GameObject host;
    private TMP_FontAsset font;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("ResourceKeywordTest", typeof(RectTransform), typeof(Canvas));
        host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        Assert.IsNotNull(font);
    }
    [TearDown] public void TearDown() { Object.DestroyImmediate(host); }

    [TestCase("金币", "F0CC6B", "DL_Money")]
    [TestCase("金钱", "F0CC6B", "DL_Money")]
    [TestCase("行动点", "9EDBB8", "DL_ActionPoints")]
    [TestCase("钥匙", "B8D9E6", "DL_Keys")]
    public void TermsUseTheirOwnColourAndPackagedIcon(string word, string color, string icon)
    {
        string result = ResourceKeywordText.Format("获得 3 " + word);
        StringAssert.Contains("<color=#" + color + ">", result);
        StringAssert.Contains("<sprite name=\"" + icon + "\" tint=0>", result);
        StringAssert.Contains(word, result);
        Assert.AreEqual(result, ResourceKeywordText.Format(result));
    }

    [Test]
    public void FormattingPreservesMarkupAttributesNoParseAndDiscountMeaning()
    {
        string result = ResourceKeywordText.Format("<link=\"钥匙\"><color=#FF0000><s>25 金钱</s></color></link>；<noparse>金币 <color=red>钥匙</color></noparse>；行动点 < 3");
        StringAssert.StartsWith("<link=\"钥匙\"><color=#FF0000><s>25 ", result);
        StringAssert.Contains("</s></color></link>", result);
        StringAssert.Contains("<noparse>金币 <color=red>钥匙</color></noparse>", result);
        Assert.AreEqual(2, Count(result, "<sprite "));
        StringAssert.EndsWith(" < 3", result);
    }

    [Test]
    public void OrdinaryTextIsReturnedWithoutAllocatingDecoratedContent()
    {
        string value = "获得遗物，恢复队伍生命。";
        Assert.AreSame(value, ResourceKeywordText.Format(value));
        Assert.IsNull(ResourceKeywordText.Format(null));
    }

    [Test]
    public void RealChineseMeshContainsFourInlineSpritesAndCorrectWordColoursWithoutChangingRawText()
    {
        TextMeshProUGUI label = Label("Coins and keys");
        const string source = "金币 金钱 行动点 钥匙";
        label.text = source;
        ResourceKeywordText.Attach(label);
        label.ForceMeshUpdate(true, true);
        Assert.AreEqual(source, label.text);
        Assert.AreEqual(4, SpriteCount(label));
        foreach (TMP_CharacterInfo character in label.textInfo.characterInfo.Take(label.textInfo.characterCount))
        {
            if (character.character != '金' && character.character != '行' && character.character != '钥') continue;
            string hex = character.character == '金' ? ResourceKeywordText.MoneyColor : character.character == '行' ? ResourceKeywordText.ActionPointColor : ResourceKeywordText.KeyColor;
            ColorUtility.TryParseHtmlString("#" + hex, out Color expected);
            Assert.AreEqual((Color32)expected, character.color);
        }
        Assert.IsFalse(label.isTextTruncated);
        Assert.AreEqual(3, ResourceKeywordText.Sprites.spriteGlyphTable.Count);
        Assert.AreSame(MapForestArt.Get("Coins").texture, ResourceKeywordText.Sprites.spriteSheet);
    }

    [Test]
    public void LayoutMeasurementIncludesIconsAndDoesNotChangeTheMessage()
    {
        TextMeshProUGUI label = Label("Measured");
        label.text = "金币行动点钥匙";
        float original = label.GetPreferredValues(2000f, 0f).x;
        ResourceKeywordText.Attach(label);
        float decorated = label.GetPreferredValues(2000f, 0f).x;
        Assert.Greater(decorated, original + 45f);
        Assert.AreEqual("金币行动点钥匙", label.text);
        label.rectTransform.sizeDelta = new Vector2(180f, 400f);
        label.ForceMeshUpdate(true, true);
        Assert.Greater(label.textInfo.lineCount, 1);
        Assert.AreEqual(3, SpriteCount(label));
    }

    [Test]
    public void ExistingSpriteAssetsAreClonedAndTheirIndexedIconsRemainUsable()
    {
        TMP_SpriteAsset existing = TMP_Settings.defaultSpriteAsset;
        Assert.IsNotNull(existing);
        int fallbackCount = existing.fallbackSpriteAssets?.Count ?? 0;
        TextMeshProUGUI label = Label("Existing");
        label.spriteAsset = existing;
        label.text = "<sprite index=0> 获得金币";
        ResourceKeywordText.Attach(label);
        label.ForceMeshUpdate(true, true);
        Assert.AreNotSame(existing, label.spriteAsset);
        Assert.AreEqual(fallbackCount, existing.fallbackSpriteAssets?.Count ?? 0);
        Assert.AreEqual(2, SpriteCount(label));
        Assert.AreSame(existing.spriteSheet, label.spriteAsset.spriteSheet);
    }

    [Test]
    public void HudTopLeftIsUntouchedAndAlreadyIllustratedRowsOnlyAddColour()
    {
        GameObject panel = new GameObject("TopLeftPanel", typeof(RectTransform));
        panel.transform.SetParent(host.transform, false);
        TextMeshProUGUI excluded = Label("Excluded", panel.transform);
        excluded.text = "金币行动点钥匙";
        ResourceKeywordText.Attach(excluded);
        Assert.IsNull(excluded.textPreprocessor);
        TextMeshProUGUI separateIcon = Label("WithSeparateIcon");
        separateIcon.text = "行动点";
        ResourceKeywordText.Attach(separateIcon, false);
        separateIcon.ForceMeshUpdate(true, true);
        Assert.AreEqual(0, SpriteCount(separateIcon));
        StringAssert.Contains(ResourceKeywordText.ActionPointColor, separateIcon.textPreprocessor.PreprocessText(separateIcon.text));
    }

    [Test]
    public void LegacyTextRetainsItsFontAndApiButDisplaysAndMeasuresInlineIcons()
    {
        GameObject go = new GameObject("Legacy", typeof(RectTransform));
        go.transform.SetParent(host.transform, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(620f, 180f);
        Font face = AssetDatabase.LoadAssetAtPath<Font>("Assets/Assets/Fonts/AlibabaPuHuiTi-3-115-Black.ttf");
        Text label = go.AddComponent<ResourceKeywordLegacyText>();
        label.font = face;
        label.fontSize = 32;
        label.text = "获得 25 金币，恢复 3 行动点，获得 1 钥匙。";
        Canvas.ForceUpdateCanvases();
        TextMeshProUGUI displayed = go.GetComponentInChildren<TextMeshProUGUI>();
        Assert.IsNotNull(displayed);
        displayed.ForceMeshUpdate(true, true);
        Assert.AreEqual(3, SpriteCount(displayed));
        Assert.AreEqual(label.text, displayed.text);
        Assert.AreSame(face, label.font);
        Assert.Greater(label.preferredHeight, 30f);
        Assert.IsFalse(displayed.raycastTarget);
        label.text = "普通说明";
        Assert.IsFalse(displayed.gameObject.activeSelf);
        label.text = "消耗行动点";
        Assert.IsTrue(displayed.gameObject.activeSelf);
    }

    [Test]
    public void BattleSettlementDescriptionGetsIconsEvenWhenItsOldFactoryRequestsPlainText()
    {
        BattleSettlementOverlay overlay = host.AddComponent<BattleSettlementOverlay>();
        typeof(BattleSettlementOverlay).GetField("bodyFont", Private).SetValue(overlay, font);
        TMP_Text label = (TMP_Text)typeof(BattleSettlementOverlay).GetMethod("CreateModalText", Private).Invoke(overlay,
            new object[] { (RectTransform)host.transform, "Description", Vector2.zero, new Vector2(700f, 300f), 28f, Color.white, "获得金币、行动点和钥匙", false });
        label.ForceMeshUpdate(true, true);
        Assert.IsTrue(label.richText);
        Assert.AreEqual(3, SpriteCount(label));
    }

    [Test]
    public void InventoryDetailAlsoFormatsLabelsSuppliedByASceneOrPrefab()
    {
        MapInventoryDetailView view = host.AddComponent<MapInventoryDetailView>();
        TextMeshProUGUI description = Label("Description");
        view.ConfigureRuntime(null, Label("Title"), Label("Meta"), description, null, null, null, null);
        view.ConfigureFonts(font, font);
        LootItem_SO item = ScriptableObject.CreateInstance<LootItem_SO>();
        try
        {
            item.lootName = "补给";
            item.description = "获得金币，恢复行动点，并获得钥匙。";
            view.Show(new RunLootState { lootItem = item, amount = 1 });
            description.ForceMeshUpdate(true, true);
            Assert.AreEqual(item.description, description.text);
            Assert.AreEqual(3, SpriteCount(description));
        }
        finally { Object.DestroyImmediate(item); }
    }

    [Test]
    public void RunSummaryReusesOneResourceIconAndColoursTheResourceValue()
    {
        RunSettlementSceneController overlay = host.AddComponent<RunSettlementSceneController>();
        typeof(RunSettlementSceneController).GetField("bodyFont", Private).SetValue(overlay, font);
        typeof(RunSettlementSceneController).GetMethod("AddStatListRow", Private).Invoke(overlay,
            new object[] { (RectTransform)host.transform, "获得钥匙", 2, null });
        Assert.IsTrue(host.GetComponentsInChildren<Image>().Any(i => i.sprite == MapForestArt.Get("Key")));
        TextMeshProUGUI label = host.GetComponentsInChildren<TextMeshProUGUI>().First(t => t.text == "获得钥匙");
        label.ForceMeshUpdate(true, true);
        Assert.AreEqual(0, SpriteCount(label));
        TextMeshProUGUI counter = host.GetComponentsInChildren<TextMeshProUGUI>().First(t => t != label);
        ColorUtility.TryParseHtmlString("#" + ResourceKeywordText.KeyColor, out Color expected);
        Assert.AreEqual(expected, counter.color);
    }

    private TextMeshProUGUI Label(string name, Transform parent = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent != null ? parent : host.transform, false);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 32f;
        label.color = Color.white;
        label.rectTransform.sizeDelta = new Vector2(1200f, 400f);
        return label;
    }
    private static int SpriteCount(TMP_Text text) { return text.textInfo.characterInfo.Take(text.textInfo.characterCount).Count(c => c.elementType == TMP_TextElementType.Sprite); }
    private static int Count(string value, string token) { return (value.Length - value.Replace(token, string.Empty).Length) / token.Length; }
}
