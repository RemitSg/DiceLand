using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuTutorialOverlayTests
{
    [TearDown]
    public void TearDown()
    {
        DestroyIfExists(GameObject.Find("MainMenuTutorialOverlay"));
        DestroyIfExists(GameObject.Find("Host"));
    }

    [Test]
    public void Show_BindsCatalogTextsAndCreatesOrderedSectionViews()
    {
        GameObject host = new GameObject("Host");
        MainMenuTutorialOverlay overlay = host.AddComponent<MainMenuTutorialOverlay>();
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        catalog.summaryTitle = "Tutorial";
        catalog.summaryBody = "Learn the map, combat, and progression basics.";
        catalog.startButtonLabel = "Start tutorial";
        catalog.backButtonLabel = "Back";
        catalog.Sections.Add(new TutorialMenuSection { sectionId = "combat", title = "Combat Basics", summary = "Practice strikes and dodges.", sortOrder = 20 });
        catalog.Sections.Add(new TutorialMenuSection { sectionId = "map", title = "Map Basics", summary = "Find key paths and landmarks.", sortOrder = 10 });
        catalog.Sections.Add(new TutorialMenuSection { sectionId = "gear", title = "Gear Upgrade", summary = "Review equipment and upgrades.", sortOrder = 30 });

        overlay.Show(catalog, null, null);

        Assert.IsTrue(overlay.IsVisible);
        Assert.AreEqual("Tutorial", overlay.DebugSummaryTitleText.text);
        Assert.AreEqual("Learn the map, combat, and progression basics.", overlay.DebugSummaryBodyText.text);
        Assert.AreEqual("Start tutorial", overlay.DebugStartButtonText.text);
        Assert.AreEqual("Back", overlay.DebugBackButtonText.text);

        IReadOnlyList<TutorialMenuSection> orderedSections = catalog.GetOrderedSections();
        Assert.AreEqual(orderedSections.Count, overlay.DebugSectionCount);

        List<Transform> rowTransforms = GetSectionRows(overlay);
        Assert.AreEqual(orderedSections.Count, rowTransforms.Count);

        ScrollRect sectionsScrollRect = GetSectionsScrollRect(overlay);
        Assert.IsNotNull(sectionsScrollRect);
        Assert.IsFalse(sectionsScrollRect.horizontal);
        Assert.IsTrue(sectionsScrollRect.vertical);
        Assert.IsNotNull(sectionsScrollRect.viewport);
        Assert.IsNotNull(sectionsScrollRect.content);

        Mask viewportMask = sectionsScrollRect.viewport.GetComponent<Mask>();
        Assert.IsNotNull(viewportMask);
        Assert.IsFalse(viewportMask.showMaskGraphic);
        Assert.IsNotNull(sectionsScrollRect.viewport.GetComponent<Image>());
        Assert.AreEqual("Viewport", sectionsScrollRect.viewport.name);
        Assert.AreEqual("Content", sectionsScrollRect.content.name);
        Assert.IsNotNull(sectionsScrollRect.content.GetComponent<VerticalLayoutGroup>());
        Assert.IsNotNull(sectionsScrollRect.content.GetComponent<ContentSizeFitter>());

        for (int i = 0; i < orderedSections.Count; i++)
        {
            TutorialMenuSection expectedSection = orderedSections[i];
            Assert.AreEqual($"Section_{i}", rowTransforms[i].name);
            Assert.IsNotNull(rowTransforms[i].GetComponent<LayoutElement>());

            Text titleText = rowTransforms[i].Find("Title").GetComponent<Text>();
            Text summaryText = rowTransforms[i].Find("Summary").GetComponent<Text>();

            Assert.AreEqual(expectedSection.title, titleText.text);
            Assert.AreEqual(expectedSection.summary, summaryText.text);
        }
    }

    [Test]
    public void BackButtonClick_InvokesCallbackAndHidesOverlay()
    {
        GameObject host = new GameObject("Host");
        MainMenuTutorialOverlay overlay = host.AddComponent<MainMenuTutorialOverlay>();

        bool backInvoked = false;

        overlay.Show(null, null, () => backInvoked = true);

        Assert.IsTrue(overlay.IsVisible);

        Button backButton = overlay.DebugBackButtonText.GetComponentInParent<Button>();
        Assert.IsNotNull(backButton);

        backButton.onClick.Invoke();

        Assert.IsTrue(backInvoked);
        Assert.IsFalse(overlay.IsVisible);
    }

    private static List<Transform> GetSectionRows(MainMenuTutorialOverlay overlay)
    {
        Transform sectionsRoot = GetSectionsScrollRect(overlay).content;
        Assert.IsNotNull(sectionsRoot);

        List<Transform> rows = new List<Transform>();
        for (int i = 0; i < sectionsRoot.childCount; i++)
            rows.Add(sectionsRoot.GetChild(i));

        return rows;
    }

    private static void DestroyIfExists(Object target)
    {
        if (target != null)
            Object.DestroyImmediate(target);
    }

    private static ScrollRect GetSectionsScrollRect(MainMenuTutorialOverlay overlay)
    {
        Transform sectionsRoot = overlay.DebugSummaryTitleText.canvas.transform.Find("Panel/Sections");
        Assert.IsNotNull(sectionsRoot);
        return sectionsRoot.GetComponent<ScrollRect>();
    }
}
