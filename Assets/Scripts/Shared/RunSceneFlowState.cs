using System.Collections.Generic;
using UnityEngine;

public sealed class RunSceneFlowState
{
    public Queue<int> PendingCalamityTiers { get; } = new Queue<int>();
    public bool DeferPendingCalamityUntilMapResume { get; set; }
    public bool DeferPendingCalamityUntilOverlayClosed { get; set; }
    public bool SuppressPendingCalamityOnNextRestActionPointRestore { get; set; }
    public BattleEncounterData PendingEventEncounterData { get; set; }

    public void Reset()
    {
        PendingCalamityTiers.Clear();
        DeferPendingCalamityUntilMapResume = false;
        DeferPendingCalamityUntilOverlayClosed = false;
        SuppressPendingCalamityOnNextRestActionPointRestore = false;
        PendingEventEncounterData = null;
    }

    public void RestorePendingCalamityTiers(IEnumerable<int> pendingTiers)
    {
        PendingCalamityTiers.Clear();
        if (pendingTiers == null)
            return;

        foreach (int tier in pendingTiers)
            PendingCalamityTiers.Enqueue(Mathf.Max(1, tier));
    }

    public MapMovementCycleFlags ToMovementCycleFlags()
    {
        return new MapMovementCycleFlags
        {
            deferPendingCalamityUntilMapResume = DeferPendingCalamityUntilMapResume,
            deferPendingCalamityUntilOverlayClosed = DeferPendingCalamityUntilOverlayClosed,
            suppressPendingCalamityOnNextRestActionPointRestore = SuppressPendingCalamityOnNextRestActionPointRestore
        };
    }

    public void ApplyMovementCycleFlags(MapMovementCycleFlags flags)
    {
        if (flags == null)
            return;

        DeferPendingCalamityUntilMapResume = flags.deferPendingCalamityUntilMapResume;
        DeferPendingCalamityUntilOverlayClosed = flags.deferPendingCalamityUntilOverlayClosed;
        SuppressPendingCalamityOnNextRestActionPointRestore = flags.suppressPendingCalamityOnNextRestActionPointRestore;
    }
}
