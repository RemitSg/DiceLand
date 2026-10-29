using System;
using NUnit.Framework;
using UnityEngine;

public sealed class RunSettlementContextTests
{
    private RunSettlementData outside;
    [SetUp] public void SetUp() { outside = RunSettlementRuntime.Current; }
    [TearDown] public void TearDown() { RunSettlementRuntime.Set(outside); }

    private static RunSettlementFlowRequest Request(Action<int> award, Func<int> wallet)
    {
        var state = new RunState();
        state.settlementStats.bossesDefeated = 1;
        return new RunSettlementFlowRequest
        {
            runState = state, runCleared = true, settlementUnixSeconds = 3600,
            getAbilityPoints = wallet, addAbilityPoints = amount => { award(amount); return wallet(); }
        };
    }

    [Test] public void IsolatedSettlementDoesNotReadOrReplaceThePlayerSettlement()
    {
        var player = new RunSettlementData { abilityPointsAfter = 999 };
        RunSettlementRuntime.Set(player);
        int points = 4;
        var context = new RunSettlementContext();
        var request = Request(amount => points += amount, () => points);
        RunSettlementData result = RunSettlementFlowService.ExecuteSettlement(request, context);
        Assert.AreSame(player, RunSettlementRuntime.Current);
        Assert.AreNotSame(player, result);
        Assert.AreSame(result, context.Current);
        Assert.AreEqual(4, result.abilityPointsBefore);
        Assert.AreEqual(5, points);
    }

    [Test] public void SeparateContextsAwardSeparatelyAndRetryDoesNotRepeatSideEffects()
    {
        int points = 0, deleted = 0, cleared = 0, transitioned = 0;
        var request = Request(amount => points += amount, () => points);
        request.deleteSavedRun = () => deleted++;
        request.clearRuntimeState = () => cleared++;
        request.transitionToScene = _ => transitioned++;
        var first = new RunSettlementContext();
        var result = RunSettlementFlowService.ExecuteSettlement(request, first);
        Assert.AreSame(result, RunSettlementFlowService.ExecuteSettlement(request, first));
        Assert.AreEqual(1, points); Assert.AreEqual(1, deleted); Assert.AreEqual(1, cleared); Assert.AreEqual(2, transitioned);
        RunSettlementFlowService.ExecuteSettlement(request, new RunSettlementContext());
        Assert.AreEqual(2, points); Assert.AreEqual(2, deleted); Assert.AreEqual(2, cleared);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public void ExplicitContextRequiresBothWalletCallbacksBeforeAnySideEffect(bool read, bool write)
    {
        bool touched = false;
        var context = new RunSettlementContext();
        var request = new RunSettlementFlowRequest
        {
            getAbilityPoints = read ? (Func<int>)(() => { touched = true; return 0; }) : null,
            addAbilityPoints = write ? (Func<int, int>)(_ => { touched = true; return 0; }) : null,
            clearRuntimeState = () => touched = true
        };
        Assert.Throws<ArgumentException>(() => RunSettlementFlowService.ExecuteSettlement(request, context));
        Assert.IsFalse(touched); Assert.IsFalse(context.HasData);
    }

    [Test] public void ExplicitContextUsesExactlyTheFormalScoreAndSerializesTheResult()
    {
        int points = 0;
        var request = Request(amount => points += amount, () => points);
        request.runState.settlementStats.specialDungeonScoreTotal = 200;
        request.runState.settlementStats.totalMoneyGained = 123;
        RunSettlementScoreResult expected = RunScoreService.Calculate(request.runState, request.scoreConfig, true, request.settlementUnixSeconds);
        RunSettlementData result = RunSettlementFlowService.ExecuteSettlement(request, new RunSettlementContext());
        Assert.AreEqual(JsonUtility.ToJson(expected), JsonUtility.ToJson(result.scoreResult));
        var roundtrip = JsonUtility.FromJson<RunSettlementData>(JsonUtility.ToJson(result));
        Assert.AreEqual(result.scoreResult.finalScore, roundtrip.scoreResult.finalScore);
        Assert.AreEqual(result.gainedAbilityPoints, roundtrip.gainedAbilityPoints);
        Assert.AreEqual(result.displayStats.totalMoneyGained, roundtrip.displayStats.totalMoneyGained);
    }

    [Test] public void ConsumeAndClearOnlyAffectTheirOwningContext()
    {
        var player = new RunSettlementData(); RunSettlementRuntime.Set(player);
        var context = new RunSettlementContext(); var local = new RunSettlementData(); context.Set(local);
        Assert.AreSame(local, context.Consume()); Assert.IsFalse(context.HasData); Assert.IsNull(context.Consume());
        context.Set(local); context.Clear(); Assert.IsFalse(context.HasData);
        Assert.AreSame(player, RunSettlementRuntime.Current);
    }
}
