using System.Collections.Generic;

public static class BattleSettlementRelicChoiceBuilder
{
    public static BattleSettlementRelicChoiceData Build(IReadOnlyList<Relic_SO> relics)
    {
        if (relics == null || relics.Count == 0)
            return null;

        BattleSettlementRelicChoiceData choiceData = new BattleSettlementRelicChoiceData
        {
            title = "Elite Reward: Choose 1 Relic",
            requiredMessage = "Choose one elite relic reward before returning."
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
}
