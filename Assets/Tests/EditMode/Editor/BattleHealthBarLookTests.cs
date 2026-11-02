using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleHealthBarLookTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private GameObject canvas;
    private BattleFeedbackSystem feedback;
    private BattleAllyController actor;
    private TMP_FontAsset font;
    private float originalOutline;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Health presentation fixture");
        root.SetActive(false);
        feedback = root.AddComponent<BattleFeedbackSystem>();
        var owner = new GameObject("Health witness");
        owner.transform.SetParent(root.transform,false);
        actor = owner.AddComponent<BattleAllyController>();
        canvas = new GameObject("Health fixture canvas",typeof(RectTransform));
        canvas.SetActive(false);
        typeof(BattleFeedbackSystem).GetField("overlayRoot",Private).SetValue(feedback,canvas.GetComponent<RectTransform>());
        font = TMP_Settings.defaultFontAsset;
        Assert.IsNotNull(font);
        originalOutline = font.material.GetFloat(ShaderUtilities.ID_OutlineWidth);
        typeof(BattleFeedbackSystem).GetField("healthValueFont",Private).SetValue(feedback,font);
    }

    [TearDown]
    public void TearDown()
    {
        if (canvas != null) Object.DestroyImmediate(canvas);
        if (root != null) Object.DestroyImmediate(root);
    }

    [Test]
    public void HealthUpdatesKeepTheRealFrontBufferAndTextAlignedIncludingZero()
    {
        object ui = CreateUi();
        float width = Field<float>(ui,"fillWidth");
        SetHealth(ui,50,100);
        Assert.That(Field<RectTransform>(ui,"fillRect").rect.width,Is.EqualTo(width * 0.5f).Within(0.001f));
        Assert.That(Field<RectTransform>(ui,"bufferRect").rect.width,Is.EqualTo(width * 0.5f).Within(0.001f));
        Assert.AreEqual("50 / 100",Field<TextMeshProUGUI>(ui,"healthValueText").text);
        foreach (string name in new[]{"fillRect","bufferRect"})
        {
            RectTransform rect = Field<RectTransform>(ui,name);
            var glass = rect.GetComponentInChildren<BattleHudOrnament>(true);
            Assert.IsNotNull(glass);
            Assert.That(glass.rectTransform.rect.width,Is.EqualTo(rect.rect.width).Within(0.001f));
        }
        SetHealth(ui,0,100);
        foreach (string name in new[]{"fillRect","bufferRect"})
        {
            var glass = Field<RectTransform>(ui,name).GetComponentInChildren<BattleHudOrnament>(true);
            using (var vertices = new VertexHelper())
            {
                typeof(BattleHudOrnament).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null)
                    .Invoke(glass,new object[]{vertices});
                Assert.AreEqual(0,vertices.currentVertCount,"Empty health must not retain decorative glints.");
            }
        }
        SetHealth(ui,100,100);
        Assert.That(Field<RectTransform>(ui,"fillRect").rect.width,Is.EqualTo(width).Within(0.001f));
        Assert.AreEqual("100 / 100",Field<TextMeshProUGUI>(ui,"healthValueText").text);
    }

    [Test]
    public void HealthOutlinesLeaveTheFontAssetIntactAndStatusIconsKeepPointerEvents()
    {
        object ui = CreateUi();
        var label = Field<TextMeshProUGUI>(ui,"healthValueText");
        Assert.AreNotSame(font.material,label.fontSharedMaterial);
        Assert.AreEqual(originalOutline,font.material.GetFloat(ShaderUtilities.ID_OutlineWidth));
        foreach (Graphic graphic in Field<RectTransform>(ui,"root").GetComponentsInChildren<Graphic>(true))
            Assert.IsFalse(graphic.raycastTarget,graphic.name);
        object icon = typeof(BattleFeedbackSystem).GetMethod("GetOrCreateStatusIcon",Private)
            .Invoke(feedback,new[]{ui,(object)0});
        Assert.IsTrue(Field<Image>(icon,"iconImage").raycastTarget,"Status inspection must remain interactive.");
        EventTrigger trigger = Field<EventTrigger>(icon,"trigger");
        Assert.AreEqual(3,trigger.triggers.Count);
        SetHealth(ui,25,100);
        Assert.IsTrue(Field<Image>(icon,"iconImage").raycastTarget);
    }

    private object CreateUi() => typeof(BattleFeedbackSystem).GetMethod("CreateTrackedUnitUI",Private)
        .Invoke(feedback,new object[]{actor,false});
    private void SetHealth(object ui,int current,int maximum) => typeof(BattleFeedbackSystem)
        .GetMethod("SetHealthBarImmediate",Private).Invoke(feedback,new object[]{ui,current,maximum});
    private static T Field<T>(object instance,string name) => (T)instance.GetType().GetField(name).GetValue(instance);
}
