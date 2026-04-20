using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CalamityPool", menuName = "Calamities/Calamity Pool")]
public class CalamityPool_SO : ScriptableObject
{
    [Serializable]
    public class CalamityTierEntry
    {
        [Min(1)] public int calamityTier = 1;
        public List<Calamity_SO> calamities = new List<Calamity_SO>();
    }

    [SerializeField] private List<CalamityTierEntry> calamityTiers = new List<CalamityTierEntry>();

    public IReadOnlyList<CalamityTierEntry> CalamityTiers => calamityTiers;

    public int GetHighestConfiguredTier()
    {
        int highestTier = 0;
        if (calamityTiers == null)
            return highestTier;

        for (int i = 0; i < calamityTiers.Count; i++)
        {
            CalamityTierEntry entry = calamityTiers[i];
            if (entry == null)
                continue;

            highestTier = Mathf.Max(highestTier, Mathf.Max(1, entry.calamityTier));
        }

        return highestTier;
    }

    public List<Calamity_SO> RollChoices(int requestedTier, ICollection<string> excludedIds, int choiceCount, System.Random rng)
    {
        List<Calamity_SO> choices = new List<Calamity_SO>();
        if (choiceCount <= 0 || calamityTiers == null || calamityTiers.Count == 0)
            return choices;

        int highestTier = GetHighestConfiguredTier();
        if (highestTier <= 0)
            return choices;

        int effectiveTier = Mathf.Clamp(requestedTier, 1, highestTier);
        HashSet<string> localExcluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (excludedIds != null)
        {
            foreach (string id in excludedIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    localExcluded.Add(id);
            }
        }

        for (int tier = effectiveTier; tier >= 1 && choices.Count < choiceCount; tier--)
        {
            List<Calamity_SO> candidates = CollectTierCandidates(tier, localExcluded);
            while (candidates.Count > 0 && choices.Count < choiceCount)
            {
                int index = rng != null ? rng.Next(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
                Calamity_SO calamity = candidates[index];
                candidates.RemoveAt(index);
                if (calamity == null)
                    continue;

                string calamityId = calamity.UniqueId;
                if (!string.IsNullOrWhiteSpace(calamityId))
                    localExcluded.Add(calamityId);
                choices.Add(calamity);
            }
        }

        return choices;
    }

    private List<Calamity_SO> CollectTierCandidates(int tier, ISet<string> excludedIds)
    {
        List<Calamity_SO> candidates = new List<Calamity_SO>();
        if (calamityTiers == null)
            return candidates;

        for (int i = 0; i < calamityTiers.Count; i++)
        {
            CalamityTierEntry entry = calamityTiers[i];
            if (entry == null || Mathf.Max(1, entry.calamityTier) != tier || entry.calamities == null)
                continue;

            for (int j = 0; j < entry.calamities.Count; j++)
            {
                Calamity_SO calamity = entry.calamities[j];
                if (calamity == null)
                    continue;

                string calamityId = calamity.UniqueId;
                if (!string.IsNullOrWhiteSpace(calamityId) && excludedIds != null && excludedIds.Contains(calamityId))
                    continue;

                candidates.Add(calamity);
            }
        }

        return candidates;
    }
}
