using System.Collections.Generic;
using NUnit.Framework;

public class RunSceneFlowStateTests
{
    [Test]
    public void Reset_ClearsPendingCalamitiesAndTransientFlags()
    {
        RunSceneFlowState state = new RunSceneFlowState();
        state.PendingCalamityTiers.Enqueue(2);
        state.DeferPendingCalamityUntilMapResume = true;
        state.DeferPendingCalamityUntilOverlayClosed = true;
        state.SuppressPendingCalamityOnNextRestActionPointRestore = true;
        state.PendingEventEncounterData = new BattleEncounterData();

        state.Reset();

        Assert.AreEqual(0, state.PendingCalamityTiers.Count);
        Assert.IsFalse(state.DeferPendingCalamityUntilMapResume);
        Assert.IsFalse(state.DeferPendingCalamityUntilOverlayClosed);
        Assert.IsFalse(state.SuppressPendingCalamityOnNextRestActionPointRestore);
        Assert.IsNull(state.PendingEventEncounterData);
    }

    [Test]
    public void RestorePendingCalamityTiers_NormalizesInvalidTiers()
    {
        RunSceneFlowState state = new RunSceneFlowState();

        state.RestorePendingCalamityTiers(new List<int> { 0, 3, -2 });

        CollectionAssert.AreEqual(new[] { 1, 3, 1 }, state.PendingCalamityTiers.ToArray());
    }

    [Test]
    public void MovementCycleFlags_RoundTripsCurrentFlags()
    {
        RunSceneFlowState state = new RunSceneFlowState
        {
            DeferPendingCalamityUntilMapResume = true,
            DeferPendingCalamityUntilOverlayClosed = false,
            SuppressPendingCalamityOnNextRestActionPointRestore = true
        };

        MapMovementCycleFlags flags = state.ToMovementCycleFlags();
        state.DeferPendingCalamityUntilMapResume = false;
        state.SuppressPendingCalamityOnNextRestActionPointRestore = false;
        state.ApplyMovementCycleFlags(flags);

        Assert.IsTrue(state.DeferPendingCalamityUntilMapResume);
        Assert.IsFalse(state.DeferPendingCalamityUntilOverlayClosed);
        Assert.IsTrue(state.SuppressPendingCalamityOnNextRestActionPointRestore);
    }
}
