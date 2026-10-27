using System;

// JSON DTOs deliberately contain no Unity object references or instance IDs.
[Serializable] public sealed class AgentBattleRequest
{
    public string op;
    public string sessionId;
    public string requestId;
    public int revision = -1;
    public int seed = 1;
    public int maxRounds = 100;
    public AgentBattlePartyMember[] party;
    public string[] enemies;
    public string[] relics;
    public string actorId;
    public int buttonIndex;
    public int skillIndex;
    public string targetId;
    public int targetButtonIndex;
    public int choiceIndex;
}
[Serializable] public sealed class AgentBattlePartyMember
{
    public string character;
    public string weapon;
}
[Serializable] public sealed class AgentBattleResponse
{
    public string apiVersion = "1";
    public bool success;
    public string error;
    public string message;
    public AgentBattleObservation state;
    public AgentBattleCatalogue catalogue;
    public AgentBattleReplay replay;
}
[Serializable] public sealed class AgentBattleObservation
{
    public string sessionId;
    public int revision;
    public int seed;
    public int round;
    public string phase;
    public string outcome;
    public int energy;
    public int temporaryEnergy;
    public int reservedEnergy;
    public AgentBattleActor[] actors;
    public AgentBattleAsset[] relics;
    public AgentBattleEvent[] events;
    public AgentBattleLegalAction[] legalActions;
    public AgentBattleRewards rewards;
}
[Serializable] public sealed class AgentBattleActor
{
    public string id;
    public string side;
    public string name;
    public int hp;
    public int maxHp;
    public int stage;
    public bool dead;
    public AgentBattleStatus[] statuses;
    public AgentBattleSkill[] skills;
    public AgentBattleSlot[] slots;
}
[Serializable] public sealed class AgentBattleStatus { public string id; public string name; public int stacks; }
[Serializable] public sealed class AgentBattleSkill
{
    public int index;
    public string id;
    public string name;
    public string description;
    public int energyCost;
    public AgentBattleDie[] dice;
}
[Serializable] public sealed class AgentBattleDie { public int min; public int max; public string category; }
[Serializable] public sealed class AgentBattleSlot
{
    public int index;
    public int speed;
    public int skillIndex;
    public AgentBattleSkill selectedSkill;
    public string targetId;
    public int targetButtonIndex;
    public bool contested;
}
[Serializable] public sealed class AgentBattleLegalAction
{
    public string op;
    public string actorId;
    public int buttonIndex;
    public int skillIndex;
    public string targetId;
    public int targetButtonIndex;
    public int choiceIndex;
}
[Serializable] public sealed class AgentBattleRewards
{
    public int experience;
    public int money;
    public int refreshesRemaining;
    public AgentBattleRewardItem[] loot;
    public AgentBattleRewardItem[] relics;
    public AgentBattleRewardItem[] choices;
}
[Serializable] public sealed class AgentBattleRewardItem { public string id; public string name; public string description; public int amount; public int index; }
[Serializable] public sealed class AgentBattleEvent { public int index; public int round; public string kind; public string actorId; public int amount; public string message; }
[Serializable] public sealed class AgentBattleCatalogue { public AgentBattleAsset[] characters; public AgentBattleAsset[] weapons; public AgentBattleAsset[] enemies; public AgentBattleAsset[] relics; }
[Serializable] public sealed class AgentBattleAsset { public string id; public string name; public string description; public string limitation; }
[Serializable] public sealed class AgentBattleReplay { public string engineVersion; public string contentHash; public AgentBattleRequest create; public AgentBattleRequest[] commands; }
