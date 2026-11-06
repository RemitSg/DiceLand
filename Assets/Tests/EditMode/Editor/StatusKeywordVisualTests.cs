using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class StatusKeywordVisualTests
{
    private GameObject host;
    private TMP_FontAsset font;
    private bool oldSuppressed,oldLocked;

    [SetUp]
    public void SetUp()
    {
        oldSuppressed=SpeedButton.InputSuppressed; oldLocked=SpeedButton.CombatLocked;
        host=new GameObject("Status keyword fixture",typeof(RectTransform),typeof(Canvas));
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
    }

    [TearDown]
    public void TearDown() { Object.DestroyImmediate(host); SpeedButton.InputSuppressed=oldSuppressed; SpeedButton.CombatLocked=oldLocked; }

    [Test]
    public void EveryStatusAssetUsesTheSameSpriteAsItsCompleteKeywordIncludingVariants()
    {
        BattleStatus_SO[] statuses=AssetDatabase.FindAssets("t:BattleStatus_SO",new[]{"Assets"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<BattleStatus_SO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Assert.AreEqual(35,statuses.Length);
        foreach(BattleStatus_SO status in statuses)
        {
            Assert.IsNotNull(status.IconSprite,status.DisplayName);
            Assert.AreEqual(StatusIconBuildTools.AtlasPath,AssetDatabase.GetAssetPath(status.IconSprite));
            Assert.AreSame(status.IconSprite,ResourceKeywordText.IconFor(status.DisplayName),status.DisplayName);
            string markup=ResourceKeywordText.Format(status.DisplayName);
            Assert.AreEqual(1,Regex.Matches(markup,"<sprite ").Count,status.DisplayName);
            StringAssert.Contains("DL_Status_"+status.IconSprite.name,markup);
            StringAssert.Contains(">"+status.DisplayName+"</color>",markup);
        }
        Assert.AreEqual(27,statuses.Select(s=>s.IconSprite).Distinct().Count());
        Assert.AreEqual(27,ResourceKeywordText.Sprites.fallbackSpriteAssets[2].spriteGlyphTable.Count);
        Assert.AreNotSame(ResourceKeywordText.IconFor("燃烧"),ResourceKeywordText.IconFor("炎属性"));
        Assert.AreNotSame(ResourceKeywordText.IconFor("电流"),ResourceKeywordText.IconFor("电属性"));
    }

    [Test]
    public void EveryEnglishStatusNameUsesTheSameKeywordAndSpriteAsChinese()
    {
        var english = AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.StringTable>("Assets/Localization/Tables/DiceLand_en.asset");
        foreach (string guid in AssetDatabase.FindAssets("t:BattleStatus_SO", new[] { "Assets/Data" }))
        {
            var status = AssetDatabase.LoadAssetAtPath<BattleStatus_SO>(AssetDatabase.GUIDToAssetPath(guid));
            string key = new SerializedObject(status).FindProperty("localizationKey").stringValue;
            if (string.IsNullOrWhiteSpace(key)) key = "status." + status.StatusId;
            string name = english.GetEntry(key + ".name")?.Value;
            Assert.IsNotEmpty(name, key);
            Assert.AreSame(status.IconSprite, ResourceKeywordText.IconFor(name), name);
            Assert.AreSame(status.IconSprite, ResourceKeywordText.IconFor(name.ToLowerInvariant()), name);
            string markup = ResourceKeywordText.Format(name);
            Assert.AreEqual(1, Regex.Matches(markup, "<sprite ").Count, name);
            StringAssert.Contains("DL_Status_" + status.IconSprite.name, markup);
            Assert.AreEqual(markup, ResourceKeywordText.Format(markup), name);
        }
    }

    [Test]
    public void CompleteStatusNamesWinOverShorterNamesAndRichTextIsNotDuplicated()
    {
        const string raw="<b>临时强壮</b>、临时重防、临时标记、流血；<noparse>燃烧</noparse>。";
        string markup=ResourceKeywordText.Format(raw);
        Assert.AreEqual(4,Regex.Matches(markup,"<sprite ").Count);
        StringAssert.Contains(">临时强壮</color>",markup);
        StringAssert.Contains("<noparse>燃烧</noparse>",markup);
        Assert.AreEqual(markup,ResourceKeywordText.Format(markup));
        Assert.AreEqual(0,Regex.Matches(ResourceKeywordText.Format(raw,false),"<sprite ").Count);
    }

    [Test]
    public void OneMeshCanRenderStatusAttributeHealthAndResourceIconsTogether()
    {
        var go=new GameObject("Combined keywords",typeof(RectTransform)); go.transform.SetParent(host.transform,false);
        var text=go.AddComponent<TextMeshProUGUI>(); text.font=font; text.fontSize=32;
        text.rectTransform.sizeDelta=new Vector2(880,360);
        const string raw="施加 2 层燃烧和寒冷，获得临时强壮。恢复生命值，消耗金币，造成电属性伤害。";
        text.text=raw; ResourceKeywordText.Attach(text); text.ForceMeshUpdate(true,true);
        Assert.AreEqual(raw,text.text);
        Assert.AreEqual(6,text.textInfo.characterInfo.Take(text.textInfo.characterCount).Count(c=>c.elementType==TMP_TextElementType.Sprite));
        Assert.AreEqual(3,StatusGlyphCount(text)); Assert.IsFalse(text.isTextTruncated);
    }

    [Test]
    public void ActualSkillDiceDescriptionRendersBurnStatusSprite()
    {
        SpeedButton.InputSuppressed=false; SpeedButton.CombatLocked=false;
        var overlay=host.AddComponent<BattleSkillInfoOverlay>(); overlay.ConfigureFonts(font,font);
        var weapon=AssetDatabase.LoadAssetAtPath<Weapon_SO>("Assets/Data/Weapons/007 烬冠剑/007 烬冠剑.asset");
        BattleAllyController ally=BattleSkillVisualTools.CreateAlly(host.transform,weapon);
        ally.SetSelectedSkillIndexForButton(ally.SpeedButtons[0],3);
        overlay.ShowAllySkillSelection(ally,ally.SpeedButtons[0]); Canvas.ForceUpdateCanvases();
        TMP_Text clause=host.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="DiceClauses"&&t.text.Contains("燃烧"));
        clause.ForceMeshUpdate(true,true);
        StringAssert.Contains("DL_Status_Burn",clause.textPreprocessor.PreprocessText(clause.text));
        Assert.Greater(StatusGlyphCount(clause),0);
    }

    [Test]
    public void ActualRelicDetailRendersStatusSpritesAndRetainsItsDescription()
    {
        Relic_SO relic=AssetDatabase.FindAssets("t:Relic_SO",new[]{"Assets/Data/Relics"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<Relic_SO>(AssetDatabase.GUIDToAssetPath(g)))
            .First(r=>r.description!=null&&r.description.Contains("燃烧"));
        var view=MapInventoryDetailView.Create((RectTransform)host.transform,"StatusRelicDetail");
        view.ConfigureFonts(font,font); view.Show(new RunRelicState{relicData=relic,amount=1}); Canvas.ForceUpdateCanvases();
        TMP_Text description=view.ModalRoot.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="Description");
        description.ForceMeshUpdate(true,true);
        Assert.AreEqual(relic.description,description.text);
        Assert.Greater(StatusGlyphCount(description),0);
    }

    private static int StatusGlyphCount(TMP_Text label) => label.textInfo.characterInfo.Take(label.textInfo.characterCount)
        .Count(c=>c.elementType==TMP_TextElementType.Sprite&&c.spriteAsset!=null&&c.spriteAsset.name=="DiceLandBattleStatusKeywords");
}
