using System.Collections.Generic;

public static class BattleSettlementRelicChoiceBuilder
{
    public static BattleSettlementRelicChoiceData Build(IReadOnlyList<Relic_SO> relics)
    {
        if (relics == null || relics.Count == 0)
            return null;

        BattleSettlementRelicChoiceData choiceData = new BattleSettlementRelicChoiceData
        {
            title = "精英奖励：选择 1 个遗物",
            requiredMessage = "返回前请选择一个精英遗物奖励。"
        };

        for (int i = 0; i < relics.Count; i++)
        {
            Relic_SO relic = relics[i];
            if (relic == null)
                continue;

            choiceData.choices.Add(new BattleSettlementLootEntry
            {
                relicData = relic,
                amount = 1,
                displayName = relic.DisplayName,
                description = relic.description ?? string.Empty,
                rarity = relic.rarity,
                attribute = null,
                icon = relic.icon,
                isRelicChoiceOption = true
            });
        }

        return choiceData.HasChoices ? choiceData : null;
    }

    public static BattleSettlementRelicChoiceData Build(
        IReadOnlyList<Relic_SO> relics,
        RelicTable_SO sourceTable,
        int choiceCount)
    {
        BattleSettlementRelicChoiceData choiceData = Build(relics);
        if (choiceData == null)
            return null;

        choiceData.sourceTable = sourceTable;
        choiceData.choiceCount = choiceCount;
        return choiceData;
    }
}
