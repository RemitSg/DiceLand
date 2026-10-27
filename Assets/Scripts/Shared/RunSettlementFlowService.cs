using System;

public sealed class RunSettlementFlowRequest
{
    public RunState runState;
    public RunScoreConfig_SO scoreConfig;
    public bool runCleared;
    public long settlementUnixSeconds;
    public string settlementSceneName = "RunSettlementScene";
    public string returnSceneName = "Menu Selection";
    public Func<int> getAbilityPoints;
    public Func<int, int> addAbilityPoints;
    public Action deleteSavedRun;
    public Action clearRuntimeState;
    public Action<string> transitionToScene;
}

public static class RunSettlementFlowService
{
    public const string DefaultSettlementSceneName = "RunSettlementScene";
    public const string DefaultReturnSceneName = "Menu Selection";

    public static RunSettlementData ExecuteSettlement(RunSettlementFlowRequest request)
    {
        return ExecuteSettlement(request, RunSettlementRuntime.Context, true);
    }

    // Explicit contexts never fall back to the player's global settlement or wallet.
    public static RunSettlementData ExecuteSettlement(RunSettlementFlowRequest request, RunSettlementContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        return ExecuteSettlement(request, context, false);
    }

    private static RunSettlementData ExecuteSettlement(RunSettlementFlowRequest request, RunSettlementContext context, bool usePlayerWallet)
    {
        if (request == null)
            return null;

        if (context.HasData)
        {
            request.transitionToScene?.Invoke(ResolveSettlementSceneName(request.settlementSceneName));
            return context.Current;
        }

        if (!usePlayerWallet && (request.getAbilityPoints == null || request.addAbilityPoints == null))
            throw new ArgumentException("An explicit settlement context requires both isolated wallet callbacks.", nameof(request));

        RunSettlementScoreResult scoreResult = RunScoreService.Calculate(
            request.runState,
            request.scoreConfig,
            request.runCleared,
            request.settlementUnixSeconds > 0 ? request.settlementUnixSeconds : DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        RunScoreService.LogDebugLines(scoreResult);

        int before = request.getAbilityPoints != null ? Math.Max(0, request.getAbilityPoints()) : new AbilityPointWallet().GetCurrentPoints();
        int after = before;
        if (scoreResult.gainedAbilityPoints > 0)
        {
            after = request.addAbilityPoints != null
                ? Math.Max(0, request.addAbilityPoints(scoreResult.gainedAbilityPoints))
                : new AbilityPointWallet().AddPoints(scoreResult.gainedAbilityPoints);
        }

        RunSettlementData data = new RunSettlementData
        {
            runCleared = request.runCleared,
            scoreResult = scoreResult,
            displayStats = scoreResult.displayStats,
            abilityPointsBefore = before,
            gainedAbilityPoints = scoreResult.gainedAbilityPoints,
            abilityPointsAfter = after,
            returnSceneName = ResolveReturnSceneName(request.returnSceneName)
        };

        context.Set(data);
        request.deleteSavedRun?.Invoke();
        request.clearRuntimeState?.Invoke();
        request.transitionToScene?.Invoke(ResolveSettlementSceneName(request.settlementSceneName));
        return data;
    }

    private static string ResolveSettlementSceneName(string sceneName)
    {
        return string.IsNullOrWhiteSpace(sceneName) ? DefaultSettlementSceneName : sceneName;
    }

    private static string ResolveReturnSceneName(string sceneName)
    {
        return string.IsNullOrWhiteSpace(sceneName) ? DefaultReturnSceneName : sceneName;
    }
}
