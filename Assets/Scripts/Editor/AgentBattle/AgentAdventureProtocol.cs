using System;

[Serializable] public sealed class AgentAdventureRequest
{
    public string op, sessionId, requestId;
    public int revision = -1, seed = 1, maxRounds = 1000, x, y, choiceIndex;
    public AgentBattleLegalAction battleAction;
}
[Serializable] public sealed class AgentAdventureAction
{
    public string op;
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
}
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
