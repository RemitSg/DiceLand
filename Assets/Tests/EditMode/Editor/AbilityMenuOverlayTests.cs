using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilityMenuOverlayTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private GameObject host;
    private AbilityMenuOverlay overlay;
    private AbilityTreeConfig_SO config;
    private AbilityPointWallet wallet;
    private string folder;
    private string progressionPath;

    [SetUp]
    public void SetUp()
    {
        folder = Path.Combine(Path.GetTempPath(), "DiceLandAbilityUI", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        wallet = new AbilityPointWallet(Path.Combine(folder, "wallet.json"));
        progressionPath = Path.Combine(folder, "progression.json");
        config = ScriptableObject.CreateInstance<AbilityTreeConfig_SO>();
        config.nodes = new List<AbilityNodeDefinition>
        {
            new AbilityNodeDefinition { nodeId = "root", displayName = "Root", cost = 0, position = new Vector2(-600f, 180f) },
            new AbilityNodeDefinition { nodeId = "money", displayName = "Money", cost = 2, position = new Vector2(-360f, 180f), prerequisiteNodeIds = new List<string> { "root" }, effects = new List<AbilityNodeEffect> { new AbilityNodeEffect { effectType = AbilityEffectType.StartingMoneyFlat, intValue = 10 } } },
            new AbilityNodeDefinition { nodeId = "toggle", displayName = "Companion", cost = 1, position = new Vector2(3200f, -200f), isToggleable = true, prerequisiteNodeIds = new List<string> { "root" } }
        };
        host = new GameObject("AbilityUiTestHost");
        overlay = host.AddComponent<AbilityMenuOverlay>();
    }

    [TearDown]
    public void TearDown()
    {
        // EditMode does not send OnDestroy to a MonoBehaviour that never entered play.
        if (overlay != null)
            typeof(AbilityMenuOverlay).GetMethod("OnDestroy", Private).Invoke(overlay, null);
        UnityEngine.Object.DestroyImmediate(host);
        UnityEngine.Object.DestroyImmediate(config);
        Directory.Delete(folder, true);
    }

    [Test]
    public void Show_BindsEveryNodeAndResourcesWithoutChangingProfile()
    {
        overlay.Show(config, wallet, null, progressionPath);
        Assert.AreEqual(3, Get<Dictionary<string, Button>>("nodeButtons").Count);
        Assert.AreEqual("root", Get<string>("selectedNodeId"));
        Assert.IsTrue(Get<Button>("unlockButton").interactable, "The free root remains actionable with zero points.");
        Assert.IsNotNull(Get<RectTransform>("treeRoot").GetComponent<RectMask2D>());
        Assert.IsNotNull(Resources.Load<Sprite>("AbilityMenu/GildedNodeFrame"));
        Assert.IsNotNull(Resources.Load<Sprite>("AbilityMenu/GildedPanelFrame"));
        Assert.IsNotNull(Resources.Load<Texture2D>("AbilityMenu/ForestSanctuary"));
        Assert.IsFalse(File.Exists(progressionPath));
        Assert.IsFalse(File.Exists(wallet.FilePath));
    }

    [Test]
    public void Unlock_UsesRealCostAndPersistsStateAcrossReopen()
    {
        wallet.SetPoints(2);
        overlay.Show(config, wallet, null, progressionPath);
        Get<Button>("unlockButton").onClick.Invoke();
        Select("money");
        Assert.IsTrue(Get<Button>("unlockButton").interactable);
        Get<Button>("unlockButton").onClick.Invoke();
        Assert.AreEqual(0, wallet.GetCurrentPoints());
        Assert.AreEqual(AbilityNodeState.Unlocked, new AbilityProgressionService(config, wallet, progressionPath).GetNodeState("money"));
        Assert.IsFalse(Get<Button>("unlockButton").interactable);
        Assert.AreEqual("能力解锁成功。", Get<Text>("statusText").text, "A refresh must not overwrite the operation result.");
        overlay.Hide();
        overlay.Show(config, wallet, null, progressionPath);
        Select("money");
        Assert.AreEqual("已解锁", Get<Text>("unlockButtonText").text);
        Assert.AreEqual(3, Get<RectTransform>("treeContent").GetComponentsInChildren<Button>().Length);
    }

    [Test]
    public void InsufficientPoints_DisablesPurchaseAndExplainsShortfall()
    {
        new AbilityProgressionService(config, wallet, progressionPath).TryUnlock("root");
        overlay.Show(config, wallet, null, progressionPath);
        Select("money");
        Assert.IsFalse(Get<Button>("unlockButton").interactable);
        StringAssert.Contains("还需要 2 点", Get<Text>("statusText").text);
        Assert.AreEqual(AbilityNodeState.Available, new AbilityProgressionService(config, wallet, progressionPath).GetNodeState("money"));
    }

    [Test]
    public void Toggle_UsesExistingServiceAndDoesNotSpendAgain()
    {
        wallet.SetPoints(1);
        var service = new AbilityProgressionService(config, wallet, progressionPath);
        service.TryUnlock("root");
        service.TryUnlock("toggle");
        overlay.Show(config, wallet, null, progressionPath);
        Select("toggle");
        Assert.AreEqual("关闭", Get<Text>("unlockButtonText").text);
        Get<Button>("unlockButton").onClick.Invoke();
        Assert.IsFalse(service.IsNodeEnabled("toggle"));
        Assert.AreEqual("启用", Get<Text>("unlockButtonText").text);
        Assert.AreEqual(0, wallet.GetCurrentPoints());
    }

    [Test]
    public void PanAndZoom_KeepFarNodesReachableAndClampAtBothEnds()
    {
        overlay.Show(config, wallet, null, progressionPath);
        Call("SetTreeZoom", 0.55f);
        Call("PanTree", new Vector2(-1000000f, 0f));
        RectTransform content = Get<RectTransform>("treeContent");
        RectTransform viewport = Get<RectTransform>("treeRoot");
        Button far = Get<Dictionary<string, Button>>("nodeButtons")["toggle"];
        float farCenter = ((RectTransform)far.transform).anchoredPosition.x * 0.55f + content.anchoredPosition.x;
        Assert.LessOrEqual(farCenter + 126f * 0.55f, viewport.rect.width * 0.5f);
        Assert.Greater(farCenter, -viewport.rect.width * 0.5f);
        Call("PanTree", new Vector2(1000000f, 0f));
        Button root = Get<Dictionary<string, Button>>("nodeButtons")["root"];
        float rootCenter = ((RectTransform)root.transform).anchoredPosition.x * 0.55f + content.anchoredPosition.x;
        Assert.GreaterOrEqual(rootCenter - 126f * 0.55f, -viewport.rect.width * 0.5f);
        overlay.Hide();
        overlay.Show(config, wallet, null, progressionPath);
        Assert.AreEqual(Vector3.one, Get<RectTransform>("treeContent").localScale);
    }

    [Test]
    public void LongDetails_AreScrollableAndResetWhenSelectionChanges()
    {
        config.nodes[0].description = new string('x', 1000);
        overlay.Show(config, wallet, null, progressionPath);
        Text description = Get<Text>("detailDescriptionText");
        Assert.IsNotNull(description.GetComponent<ContentSizeFitter>());
        Assert.IsNotNull(description.GetComponentInParent<ScrollRect>());
        description.rectTransform.anchoredPosition = new Vector2(0, 250);
        Select("money");
        Assert.AreEqual(Vector2.zero, description.rectTransform.anchoredPosition);
    }

    [Test]
    public void Hide_NotifiesOnceAndCleanupDestroysIndependentCanvas()
    {
        int calls = 0;
        overlay.Show(config, wallet, () => calls++, progressionPath);
        GameObject canvasObject = Get<Canvas>("canvas").gameObject;
        overlay.Hide();
        overlay.Hide();
        Assert.AreEqual(1, calls);
        typeof(AbilityMenuOverlay).GetMethod("OnDestroy", Private).Invoke(overlay, null);
        UnityEngine.Object.DestroyImmediate(host);
        host = null;
        Assert.IsTrue(canvasObject == null);
    }

    private void Select(string id) => Get<Dictionary<string, Button>>("nodeButtons")[id].onClick.Invoke();
    private T Get<T>(string name) => (T)typeof(AbilityMenuOverlay).GetField(name, Private).GetValue(overlay);
    private void Call(string name, object arg) => typeof(AbilityMenuOverlay).GetMethod(name, Private).Invoke(overlay, new[] { arg });
}
