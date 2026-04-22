using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TutorialMenuSection
{
    public string sectionId = "section";
    public string title = "Untitled Section";
    [TextArea(2, 4)] public string summary;
    [TextArea(3, 8)] public string details;
    public string statusLabel = "Available";
    public int sortOrder;
    public bool isAvailable = true;
    public bool isFeatured;
}

[CreateAssetMenu(fileName = "TutorialMenuCatalog", menuName = "Tutorial/Tutorial Menu Catalog")]
public class TutorialMenuCatalog_SO : ScriptableObject
{
    public const string AvailableStatusLabelFallback = "Available";
    public const string ComingSoonStatusLabelFallback = "Coming Soon";

    public string tutorialId = "main_tutorial";
    public string displayName = "Tutorial";
    public string summaryTitle = "Tutorial";
    [TextArea(3, 6)] public string summaryBody;
    public string startButtonLabel = "Start Tutorial";
    public string backButtonLabel = "Back";
    [SerializeField] private List<TutorialMenuSection> sections = new List<TutorialMenuSection>();

    public List<TutorialMenuSection> Sections
    {
        get => sections;
        set => sections = value ?? new List<TutorialMenuSection>();
    }

    public IReadOnlyList<TutorialMenuSection> GetOrderedSections()
    {
        List<KeyValuePair<int, TutorialMenuSection>> indexedSections = new List<KeyValuePair<int, TutorialMenuSection>>();
        if (sections != null)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i] != null)
                    indexedSections.Add(new KeyValuePair<int, TutorialMenuSection>(i, sections[i]));
            }
        }

        indexedSections.Sort((leftItem, rightItem) =>
        {
            TutorialMenuSection left = leftItem.Value;
            TutorialMenuSection right = rightItem.Value;

            int sortOrderCompare = left.sortOrder.CompareTo(right.sortOrder);
            if (sortOrderCompare != 0)
                return sortOrderCompare;

            int sectionIdCompare = string.CompareOrdinal(left.sectionId ?? string.Empty, right.sectionId ?? string.Empty);
            if (sectionIdCompare != 0)
                return sectionIdCompare;

            return leftItem.Key.CompareTo(rightItem.Key);
        });

        List<TutorialMenuSection> ordered = new List<TutorialMenuSection>(indexedSections.Count);
        for (int i = 0; i < indexedSections.Count; i++)
            ordered.Add(indexedSections[i].Value);

        return ordered;
    }

    public void ForceValidateForTests()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            tutorialId = "main_tutorial";
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Tutorial";
        if (string.IsNullOrWhiteSpace(summaryTitle))
            summaryTitle = "Tutorial";
        if (string.IsNullOrWhiteSpace(startButtonLabel))
            startButtonLabel = "Start Tutorial";
        if (string.IsNullOrWhiteSpace(backButtonLabel))
            backButtonLabel = "Back";
        if (sections == null)
            sections = new List<TutorialMenuSection>();

        for (int i = 0; i < sections.Count; i++)
        {
            TutorialMenuSection section = sections[i];
            if (section == null)
                continue;

            if (string.IsNullOrWhiteSpace(section.sectionId))
                section.sectionId = $"section_{i + 1}";
            if (string.IsNullOrWhiteSpace(section.title))
                section.title = "Untitled Section";
            if (string.IsNullOrWhiteSpace(section.statusLabel))
                section.statusLabel = section.isAvailable ? AvailableStatusLabelFallback : ComingSoonStatusLabelFallback;
        }
    }
}
