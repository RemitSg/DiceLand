using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TutorialMenuCatalogTests
{
    [Test]
    public void GetOrderedSections_FiltersNullsAndSortsBySortOrder()
    {
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        catalog.Sections = new List<TutorialMenuSection>
        {
            new TutorialMenuSection { sectionId = "world", title = "世界系统", sortOrder = 20 },
            null,
            new TutorialMenuSection { sectionId = "battle", title = "战斗基础", sortOrder = 10 },
            new TutorialMenuSection { sectionId = "map", title = "初识地图", sortOrder = 0 }
        };

        IReadOnlyList<TutorialMenuSection> ordered = catalog.GetOrderedSections();

        Assert.AreEqual(3, ordered.Count);
        Assert.AreEqual("map", ordered[0].sectionId);
        Assert.AreEqual("battle", ordered[1].sectionId);
        Assert.AreEqual("world", ordered[2].sectionId);
    }

    [Test]
    public void GetOrderedSections_WhenSortOrderEqual_UsesSectionIdAsTieBreaker()
    {
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        catalog.Sections = new List<TutorialMenuSection>
        {
            new TutorialMenuSection { sectionId = "zeta", title = "z", sortOrder = 10 },
            new TutorialMenuSection { sectionId = "alpha", title = "a", sortOrder = 10 },
            new TutorialMenuSection { sectionId = "beta", title = "b", sortOrder = 10 }
        };

        IReadOnlyList<TutorialMenuSection> ordered = catalog.GetOrderedSections();

        Assert.AreEqual("alpha", ordered[0].sectionId);
        Assert.AreEqual("beta", ordered[1].sectionId);
        Assert.AreEqual("zeta", ordered[2].sectionId);
    }

    [Test]
    public void GetOrderedSections_WhenSortOrderAndSectionIdEqual_PreservesSourceOrder()
    {
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        TutorialMenuSection first = new TutorialMenuSection { sectionId = "dup", title = "first", sortOrder = 5 };
        TutorialMenuSection second = new TutorialMenuSection { sectionId = "dup", title = "second", sortOrder = 5 };
        TutorialMenuSection third = new TutorialMenuSection { sectionId = "dup", title = "third", sortOrder = 5 };

        catalog.Sections = new List<TutorialMenuSection>
        {
            new TutorialMenuSection { sectionId = "lead", title = "lead", sortOrder = 0 },
            second,
            third,
            first
        };

        IReadOnlyList<TutorialMenuSection> ordered = catalog.GetOrderedSections();

        Assert.AreEqual("lead", ordered[0].sectionId);
        Assert.AreSame(second, ordered[1]);
        Assert.AreSame(third, ordered[2]);
        Assert.AreSame(first, ordered[3]);
    }

    [Test]
    public void OnValidate_FillsMissingDisplayFieldsFromFallbacks()
    {
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        TutorialMenuCatalog_SO defaults = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();

        catalog.displayName = "";
        catalog.summaryTitle = "";
        catalog.startButtonLabel = "";
        catalog.backButtonLabel = "";

        catalog.ForceValidateForTests();

        Assert.AreEqual("Tutorial", catalog.displayName);
        Assert.AreEqual(defaults.summaryTitle, catalog.summaryTitle);
        Assert.AreEqual(defaults.startButtonLabel, catalog.startButtonLabel);
        Assert.AreEqual(defaults.backButtonLabel, catalog.backButtonLabel);
    }

    [Test]
    public void OnValidate_FillsMissingSectionFieldsFromFallbacks()
    {
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        TutorialMenuSection sectionDefaults = new TutorialMenuSection();

        catalog.Sections = new List<TutorialMenuSection>
        {
            new TutorialMenuSection
            {
                sectionId = "",
                title = "",
                statusLabel = "",
                isAvailable = true,
                sortOrder = 3
            },
            new TutorialMenuSection
            {
                sectionId = "fixed_id",
                title = "fixed_title",
                statusLabel = "",
                isAvailable = false,
                sortOrder = 5
            }
        };

        catalog.ForceValidateForTests();

        Assert.AreEqual("section_1", catalog.Sections[0].sectionId);
        Assert.AreEqual(sectionDefaults.title, catalog.Sections[0].title);
        Assert.AreEqual(TutorialMenuCatalog_SO.AvailableStatusLabelFallback, catalog.Sections[0].statusLabel);
        Assert.AreEqual("fixed_id", catalog.Sections[1].sectionId);
        Assert.AreEqual("fixed_title", catalog.Sections[1].title);
        Assert.AreEqual(TutorialMenuCatalog_SO.ComingSoonStatusLabelFallback, catalog.Sections[1].statusLabel);
    }
}
