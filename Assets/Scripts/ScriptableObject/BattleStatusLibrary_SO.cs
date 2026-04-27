using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleStatusLibrary", menuName = "Battle/BattleStatusLibrary")]
public class BattleStatusLibrary_SO : ScriptableObject
{
    [Serializable]
    public struct StatusEntry
    {
        public string statusId;
        public BattleStatus_SO statusData;
    }

    [SerializeField] private List<StatusEntry> entries = new List<StatusEntry>();

    public BattleStatus_SO GetStatusData(string statusId)
    {
        if (string.IsNullOrWhiteSpace(statusId) || entries == null)
            return null;

        for (int i = 0; i < entries.Count; i++)
        {
            StatusEntry entry = entries[i];
            if (entry.statusData == null)
                continue;

            string entryStatusId = !string.IsNullOrWhiteSpace(entry.statusId)
                ? entry.statusId
                : entry.statusData.StatusId;
            if (string.Equals(entryStatusId, statusId, StringComparison.OrdinalIgnoreCase))
                return entry.statusData;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].statusData == null)
                continue;

            StatusEntry entry = entries[i];
            entry.statusId = entry.statusData.StatusId;
            entries[i] = entry;
        }
    }
#endif
}
