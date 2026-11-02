using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleHudLayoutTests
{
    private GameObject ownerObject;
    private GameObject canvasObject;
    private BattleButton owner;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [TearDown]
    public void TearDown()
    {
        if (canvasObject != null) Object.DestroyImmediate(canvasObject);
        if (ownerObject != null) Object.DestroyImmediate(ownerObject);
    }

    [TestCase(1920, 1080, 0, 0)]
    [TestCase(2400, 1080, 110, 36)]
    [TestCase(1600, 1200, 64, 24)]
    [TestCase(600, 1080, 32, 20)]
    public void HeaderPanelsFitTheSafeAreaAndNeverOverlap(int width, int height, int notch, int top)
    {
        BattleHudLook look = CreateHud();
        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(width, height);
        Rect safe = new Rect(notch, 0f, width - notch, height - top);
        look.ApplyLayout(safe, new Vector2(width, height));
        Bounds energy = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find("EnergyPanel"));
        Bounds round = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find("RoundPanel"));
        foreach (Bounds panel in new[] { energy, round })
        {
            Assert.GreaterOrEqual(panel.min.x, root.rect.xMin + safe.xMin - 0.01f);
            Assert.LessOrEqual(panel.max.x, root.rect.xMin + safe.xMax + 0.01f);
            Assert.LessOrEqual(panel.max.y, root.rect.yMin + safe.yMax + 0.01f);
        }
        Assert.IsFalse(new Rect(energy.min, energy.size).Overlaps(new Rect(round.min, round.size)));
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            Assert.IsFalse(graphic.raycastTarget, graphic.name + " must not block battle input.");
    }

    [Test]
    public void PaintedFramesUseTheirSliceBordersAndLeaveDynamicContentAboveThem()
    {
        CreateHud();
        RectTransform root = canvasObject.GetComponent<RectTransform>();
        foreach (string panelName in new[] { "EnergyPanel", "RoundPanel" })
        {
            RectTransform panel = (RectTransform)root.Find(panelName);
            Image frame = panel.Find("Painted forest plate").GetComponent<Image>();
            Assert.IsNotNull(frame.sprite, panelName + " production frame must load from Resources.");
            Assert.AreEqual(Image.Type.Sliced, frame.type);
            Assert.AreEqual(0, frame.transform.GetSiblingIndex());
            Assert.IsFalse(frame.raycastTarget);
            Assert.Greater(frame.sprite.border.x, 0);
            Assert.Greater(frame.sprite.border.y, 0);
            using (VertexHelper vertices = new VertexHelper())
            {
                typeof(Image).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(frame, new object[] { vertices });
                var stream = new System.Collections.Generic.List<UIVertex>();
                vertices.GetUIVertexStream(stream);
                var rows = new System.Collections.Generic.SortedSet<float>();
                foreach (UIVertex vertex in stream) rows.Add(vertex.position.y);
                Assert.AreEqual(4, rows.Count, "The actual sliced mesh must have two edge bands and a center band.");
                float[] y = new float[4];
                rows.CopyTo(y);
                Assert.That(y[1] - y[0], Is.EqualTo(panel.rect.height * 0.2f).Within(0.6f));
                Assert.That(y[3] - y[2], Is.EqualTo(panel.rect.height * 0.2f).Within(0.6f));
            }
            foreach (TextMeshProUGUI label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.Greater(label.transform.GetSiblingIndex(), frame.transform.GetSiblingIndex());
        }
        int roundJewels = 0;
        foreach (BattleHudOrnament ornament in root.Find("RoundPanel").GetComponentsInChildren<BattleHudOrnament>(true))
            if (ornament.kind == BattleHudOrnament.Shape.Jewel) roundJewels++;
        Assert.AreEqual(1, roundJewels, "Only the top jewel is separate; side jewels are painted into the frame.");
    }

    [Test]
    public void DecoratedHudRetainsReservedAndOverflowingTemporaryEnergySegments()
    {
        CreateOwner();
        typeof(BattleButton).GetMethod("EnsureSharedEnergyUi", Private).Invoke(owner, null);
        canvasObject = ((Canvas)typeof(BattleButton).GetField("sharedEnergyCanvas", Private).GetValue(owner)).gameObject;
        Set("currentSharedEnergyMax", 10);
        Set("currentSharedEnergy", 8);
        Set("temporarySharedEnergy", 5);
        Set("reservedSharedEnergy", 0);
        Refresh();
        Image normal = (Image)typeof(BattleButton).GetField("sharedEnergyBarFill", Private).GetValue(owner);
        Image temporary = (Image)typeof(BattleButton).GetField("temporarySharedEnergyBarFill", Private).GetValue(owner);
        Assert.That(normal.rectTransform.anchorMax.x, Is.EqualTo(8f / 13f).Within(0.0001f));
        Assert.That(temporary.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.0001f));
        Assert.IsTrue(temporary.enabled);
        Set("reservedSharedEnergy", 5);
        Refresh();
        Assert.That(normal.rectTransform.anchorMax.x, Is.EqualTo(0.8f).Within(0.0001f));
        Assert.That(temporary.rectTransform.anchorMax.x, Is.EqualTo(0.8f).Within(0.0001f));
        Assert.IsFalse(temporary.enabled);
        TextMeshProUGUI label = (TextMeshProUGUI)typeof(BattleButton).GetField("sharedEnergyText", Private).GetValue(owner);
        Assert.That(label.text, Does.Contain("8"));
    }

    [Test]
    public void IndependentHudCanvasIsRemovedAfterItsBattleOwnerIsDestroyed()
    {
        BattleHudLook look = CreateHud();
        Object.DestroyImmediate(ownerObject);
        typeof(BattleHudLook).GetMethod("LateUpdate", Private).Invoke(look, null);
        Assert.IsTrue(canvasObject == null);
    }

    private BattleHudLook CreateHud()
    {
        CreateOwner();
        canvasObject = new GameObject("Battle HUD layout witness", typeof(RectTransform));
        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(1920f, 1080f);
        RectTransform energy = Child("EnergyPanel", root);
        energy.gameObject.AddComponent<Image>();
        RectTransform round = Child("RoundPanel", root);
        round.gameObject.AddComponent<Image>();
        TextMeshProUGUI energyText = Child("EnergyLabel", energy).gameObject.AddComponent<TextMeshProUGUI>();
        TextMeshProUGUI roundText = Child("RoundLabel", round).gameObject.AddComponent<TextMeshProUGUI>();
        RectTransform track = Child("EnergyBarBg", energy);
        track.gameObject.AddComponent<Image>();
        Image normal = Child("Normal", track).gameObject.AddComponent<Image>();
        Image temporary = Child("Temporary", track).gameObject.AddComponent<Image>();
        // Match BattleButton's segment contract: the service changes only the horizontal anchors.
        normal.rectTransform.anchorMin = Vector2.zero;
        normal.rectTransform.anchorMax = Vector2.one;
        normal.rectTransform.offsetMin = normal.rectTransform.offsetMax = Vector2.zero;
        temporary.rectTransform.anchorMin = Vector2.zero;
        temporary.rectTransform.anchorMax = new Vector2(0f, 1f);
        temporary.rectTransform.offsetMin = temporary.rectTransform.offsetMax = Vector2.zero;
        BattleHudLook look = canvasObject.AddComponent<BattleHudLook>();
        look.Configure(owner, energy, round, energyText, roundText, normal, temporary,
            new Vector2(20f, 20f), new Vector2(260f, 72f), new Vector2(220f, 56f));
        return look;
    }

    private void CreateOwner()
    {
        ownerObject = new GameObject("HUD battle owner");
        ownerObject.SetActive(false);
        owner = ownerObject.AddComponent<BattleButton>();
    }

    private static RectTransform Child(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private void Set(string field, int value) => typeof(BattleButton).GetField(field, Private).SetValue(owner, value);
    private void Refresh() => typeof(BattleButton).GetMethod("RefreshSharedEnergyUi", Private).Invoke(owner, null);
}
