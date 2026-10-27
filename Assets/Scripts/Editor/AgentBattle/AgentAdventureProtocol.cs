using System;

[Serializable] public sealed class AgentAdventureRequest
{
    public string op, sessionId, requestId, assetId;
    public int revision = -1, seed = 1, maxRounds = 1000, x, y, choiceIndex;
    public AgentBattleLegalAction battleAction;
}
[Serializable] public sealed class AgentAdventureAction
{
    public string op, assetId;
    public int x, y, choiceIndex;
    public AgentBattleLegalAction battleAction;
}
[Serializable] public sealed class AgentAdventureResponse
{
    public string apiVersion = "1";
    public bool success;
    public string error, message;
    public AgentAdventureObservation state;
}
[Serializable] public sealed class AgentAdventureObservation
{
    public string sessionId, phase, outcome, message, difficulty;
    public int revision, seed, x, y, boardSize, actionPoints, maxActionPoints, cycle, calamityTier, keys, requiredKeys, money;
    public string[] configurationIssues;
    public AgentAdventureTile[] tiles;
    public AgentAdventurePartyMember[] party;
    public AgentBattleRewardItem[] loot, relics, weapons, blueprints;
    public AgentBattleAsset[] calamityChoices, activeCalamities;
    public AgentBattleObservation battle;
    public AgentAdventureAction[] legalActions;
    public AgentAdventureMerchantOffer[] merchantOffers;
    public int merchantRefreshCost;
    public AgentAdventureRecipe[] recipes;
    public AgentAdventureRest rest;
    public AgentAdventureReforge reforge;
    public AgentBattleRewardItem lastReforgeResult;
}
[Serializable] public sealed class AgentAdventureReforge
{
    public string mode, stage, first, second, attribute;
    public int completed, limit, remaining;
    public bool committed, canConfirm;
    public AgentAdventureReforgeMaterial[] materials;
    public AgentBattleAsset[] attributes;
    public AgentBattleRewardItem[] possibleResults;
    public AgentBattleRewardItem resultPreview;
}
[Serializable] public sealed class AgentAdventureReforgeMaterial
{
    public string id, name, description, rarity, attribute;
    public int amount;
}
[Serializable] public sealed class AgentAdventureRest
{
    public int healPercent, actionPointRestorePercent, recruitCost, recruitedCount, maxRecruitCount;
    public AgentAdventureRecruit[] candidates;
}
[Serializable] public sealed class AgentAdventureRecruit
{
    public int index, hp;
    public string id, name, description, weapon;
    public bool canRecruit;
}
[Serializable] public sealed class AgentAdventureMerchantOffer
{
    public int index, price;
    public string id, name, description, kind;
    public bool soldOut, canPurchase;
}
[Serializable] public sealed class AgentAdventureRecipe
{
    public string id, name, description, weapon;
    public int cost;
    public bool canCraft;
    public AgentAdventureMaterial[] materials;
}
[Serializable] public sealed class AgentAdventureMaterial { public string id, name; public int required, available; }
[Serializable] public sealed class AgentAdventureTile
{
    public int x, y, danger, moveCost;
    public string type, dangerBand;
    public bool visited, resolved, encounterAvailable;
}
[Serializable] public sealed class AgentAdventurePartyMember
{
    public string id, name, character, weapon;
    public int hp, maxHp, level, experience;
}
