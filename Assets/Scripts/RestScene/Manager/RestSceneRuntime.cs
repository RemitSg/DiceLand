using UnityEngine;

public static class RestSceneRuntime
{
    private static string returnSceneName;
    private static Vector2Int sourceCoord;
    private static CharacterRoster_SO recruitRoster;
    private static DifficultyConfig_SO difficultyConfig;
    private static readonly System.Collections.Generic.List<CharacterData_SO> recruitCandidates = new System.Collections.Generic.List<CharacterData_SO>();
    private static bool recruitCandidatesGenerated;

    public static bool HasPendingVisit => !string.IsNullOrWhiteSpace(returnSceneName);
    public static string ReturnSceneName => returnSceneName;
    public static Vector2Int SourceCoord => sourceCoord;
    public static CharacterRoster_SO RecruitRoster => recruitRoster;
    public static DifficultyConfig_SO DifficultyConfig => difficultyConfig;

    public static void BeginVisit(string sourceScene, Vector2Int tileCoord, CharacterRoster_SO recruitRosterData = null, DifficultyConfig_SO difficultyData = null)
    {
        returnSceneName = sourceScene;
        sourceCoord = tileCoord;
        recruitRoster = recruitRosterData;
        difficultyConfig = difficultyData;
        recruitCandidates.Clear();
        recruitCandidatesGenerated = false;
    }

    public static bool TryConfirmRestAndReturn()
    {
        if (!HasPendingVisit)
            return false;

        MapRunRuntime.ApplyRestResult(returnSceneName, sourceCoord);
        string targetScene = returnSceneName;
        Clear();
        SceneCurtainTransitionController.Instance.TransitionToScene(targetScene);
        return true;
    }

    public static bool TryConfirmRestoreActionPointsAndReturn()
    {
        if (!HasPendingVisit)
            return false;

        MapRunRuntime.ApplyRestoreActionPointsResult(returnSceneName, sourceCoord);
        string targetScene = returnSceneName;
        Clear();
        SceneCurtainTransitionController.Instance.TransitionToScene(targetScene);
        return true;
    }

    public static bool TryGetCurrentSnapshot(out MapRunSnapshot snapshot)
    {
        if (!HasPendingVisit)
        {
            snapshot = null;
            return false;
        }

        return MapRunRuntime.TryGetSnapshot(returnSceneName, out snapshot);
    }

    public static System.Collections.Generic.IReadOnlyList<CharacterData_SO> GetRecruitCandidates()
    {
        if (recruitCandidatesGenerated)
            return recruitCandidates;

        recruitCandidates.Clear();
        recruitCandidatesGenerated = true;

        if (!HasPendingVisit || recruitRoster == null || difficultyConfig == null)
            return recruitCandidates;

        if (!TryGetCurrentSnapshot(out MapRunSnapshot snapshot) || snapshot?.runState == null)
            return recruitCandidates;
        if (snapshot.runState.recruitmentDisabled)
            return recruitCandidates;

        recruitCandidates.AddRange(RunRestService.GetRecruitCandidates(snapshot.runState, sourceCoord, returnSceneName, recruitRoster, difficultyConfig));
        return recruitCandidates;
    }

    public static int GetRecruitCost()
    {
        if (difficultyConfig == null)
            return 0;
        if (!TryGetCurrentSnapshot(out MapRunSnapshot snapshot) || snapshot?.runState == null)
            return 0;

        return RunRestService.RecruitCost(snapshot.runState, difficultyConfig);
    }

    public static bool TryRecruitAndReturn(CharacterData_SO characterData, out string errorMessage)
    {
        if (!HasPendingVisit)
        {
            errorMessage = "\u5f53\u524d\u4e0d\u5728\u4f11\u606f\u573a\u666f\u8bbf\u95ee\u6d41\u7a0b\u4e2d\u3002";
            return false;
        }

        if (TryGetCurrentSnapshot(out MapRunSnapshot snapshot) && snapshot?.runState != null && snapshot.runState.recruitmentDisabled)
        {
            errorMessage = "当前遗物效果禁止招募。";
            return false;
        }

        int maxRecruitCount = difficultyConfig != null ? Mathf.Max(1, difficultyConfig.maxRecruitCount) : 4;
        int recruitCost = GetRecruitCost();
        if (!MapRunRuntime.TryApplyRecruitResult(returnSceneName, sourceCoord, characterData, recruitCost, maxRecruitCount, out errorMessage))
            return false;

        string targetScene = returnSceneName;
        Clear();
        SceneCurtainTransitionController.Instance.TransitionToScene(targetScene);
        return true;
    }

    public static bool TryApplyLootReforge(
        LootItem_SO firstLoot,
        LootItem_SO secondLoot,
        LootItem_SO resultLoot,
        System.Collections.Generic.IReadOnlyList<LootItem_SO> lootReforgePool,
        out string errorMessage)
    {
        if (!HasPendingVisit)
        {
            errorMessage = "当前不在休息场景访问流程中。";
            return false;
        }

        return MapRunRuntime.TryApplyLootReforgeResult(returnSceneName, firstLoot, secondLoot, resultLoot, lootReforgePool, out errorMessage);
    }

    public static bool TryApplyRelicReforge(Relic_SO firstRelic, Relic_SO secondRelic, Relic_SO resultRelic, out string errorMessage)
    {
        if (!HasPendingVisit)
        {
            errorMessage = "当前不在休息场景访问流程中。";
            return false;
        }

        return MapRunRuntime.TryApplyRelicReforgeResult(returnSceneName, firstRelic, secondRelic, resultRelic, out errorMessage);
    }

    public static bool TryConfirmReforgeExitAndReturn()
    {
        if (!HasPendingVisit)
            return false;

        MapRunRuntime.ApplyRestVisitConsumedResult(returnSceneName, sourceCoord);
        string targetScene = returnSceneName;
        Clear();
        SceneCurtainTransitionController.Instance.TransitionToScene(targetScene);
        return true;
    }

    public static void Clear()
    {
        returnSceneName = string.Empty;
        sourceCoord = new Vector2Int(int.MinValue, int.MinValue);
        recruitRoster = null;
        difficultyConfig = null;
        recruitCandidates.Clear();
        recruitCandidatesGenerated = false;
    }
}
