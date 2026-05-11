using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleStatusPolarityList", menuName = "Battle/Status Polarity List")]
public class BattleStatusPolarityList_SO : ScriptableObject
{
    [Serializable]
    public struct StatusPolarityEntry
    {
        public string statusId;
        public BattleStatus_SO statusData;

        public string ResolvedStatusId
        {
            get
            {
                if (statusData != null && !string.IsNullOrWhiteSpace(statusData.StatusId))
                    return statusData.StatusId;
                return statusId;
            }
        }
    }

    [SerializeField] private List<StatusPolarityEntry> negativeStatuses = new List<StatusPolarityEntry>();
    [SerializeField] private List<StatusPolarityEntry> positiveStatuses = new List<StatusPolarityEntry>();

    public IReadOnlyList<StatusPolarityEntry> NegativeStatuses => negativeStatuses;
    public IReadOnlyList<StatusPolarityEntry> PositiveStatuses => positiveStatuses;

    public bool ContainsNegativeStatusId(string statusId)
    {
        return ContainsStatusId(negativeStatuses, statusId);
    }

    public bool ContainsPositiveStatusId(string statusId)
    {
        return ContainsStatusId(positiveStatuses, statusId);
    }

    public int CountNegativeStatusTypes(Func<string, int> getStatusStacksById)
    {
        return CountStatusTypes(negativeStatuses, getStatusStacksById);
    }

    private static bool ContainsStatusId(IReadOnlyList<StatusPolarityEntry> entries, string statusId)
    {
        if (string.IsNullOrWhiteSpace(statusId) || entries == null)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            string entryStatusId = entries[i].ResolvedStatusId;
            if (string.Equals(entryStatusId, statusId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static int CountStatusTypes(IReadOnlyList<StatusPolarityEntry> entries, Func<string, int> getStatusStacksById)
    {
        if (entries == null || getStatusStacksById == null)
            return 0;

        int count = 0;
        HashSet<string> countedStatusIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < entries.Count; i++)
        {
            string statusId = entries[i].ResolvedStatusId;
            if (string.IsNullOrWhiteSpace(statusId) || !countedStatusIds.Add(statusId))
                continue;

            if (getStatusStacksById(statusId) > 0)
                count++;
        }

        return count;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncStatusIds(negativeStatuses);
        SyncStatusIds(positiveStatuses);
    }

    private static void SyncStatusIds(List<StatusPolarityEntry> entries)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            StatusPolarityEntry entry = entries[i];
            if (entry.statusData != null)
                entry.statusId = entry.statusData.StatusId;
            entries[i] = entry;
        }
    }
#endif
}
