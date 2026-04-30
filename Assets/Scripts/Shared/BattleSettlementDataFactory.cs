using System.Collections.Generic;

public static class BattleSettlementDataFactory
{
    public static BattleSettlementData BuildForcedRetreatData(
        BattleEncounterData encounter,
        string subtitle,
        int experienceGained,
        string retrySceneName,
        List<BattleSettlementEntry> partyEntries,
        List<BattleSettlementLootEntry> lootEntries,
        BattleSettlementRelicChoiceData relicChoice)
    {
        return new BattleSettlementData
        {
            isVictory = true,
            title = "Victory",
            subtitle = subtitle,
            experienceGained = experienceGained,
            returnSceneName = encounter != null ? encounter.sourceSceneName : "MapScene",
            retrySceneName = retrySceneName,
            partyEntries = partyEntries,
            lootEntries = lootEntries,
            relicChoice = relicChoice
        };
    }

    public static BattleSettlementData BuildCompletionData(
        bool victory,
        BattleEncounterData encounter,
        int livingAllies,
        int experienceGained,
        string retrySceneName,
        List<BattleSettlementEntry> partyEntries,
        List<BattleSettlementLootEntry> lootEntries,
        BattleSettlementRelicChoiceData relicChoice)
    {
        return new BattleSettlementData
        {
            isVictory = victory,
            title = BuildTitle(victory, encounter),
            subtitle = BuildSubtitle(victory, encounter, livingAllies),
            experienceGained = experienceGained,
            returnSceneName = BuildReturnSceneName(victory, encounter),
            retrySceneName = retrySceneName,
            partyEntries = partyEntries,
            lootEntries = lootEntries,
            relicChoice = relicChoice
        };
    }

    private static string BuildTitle(bool victory, BattleEncounterData encounter)
    {
        if (victory)
            return "Victory";

        return encounter != null && encounter.isTutorialEncounter ? "Tutorial Failed" : "Defeat";
    }

    private static string BuildSubtitle(bool victory, BattleEncounterData encounter, int livingAllies)
    {
        if (victory)
        {
            if (!string.IsNullOrWhiteSpace(encounter?.resolutionOverride?.subtitleOverride))
                return encounter.resolutionOverride.subtitleOverride;

            return $"战斗胜利，剩余可行动角色：{livingAllies}";
        }

        return encounter != null && encounter.isTutorialEncounter
            ? "本次教学战斗失败。可以直接重试当前战斗，不会写入正常存档。"
            : "队伍已在战斗中全灭。";
    }

    private static string BuildReturnSceneName(bool victory, BattleEncounterData encounter)
    {
        if (victory && encounter != null && encounter.returnToMainMenuOnVictory)
        {
            return !string.IsNullOrWhiteSpace(encounter.tutorialCompletionSceneName)
                ? encounter.tutorialCompletionSceneName
                : "Menu Selection";
        }

        return encounter != null ? encounter.sourceSceneName : "MapScene";
    }
}
