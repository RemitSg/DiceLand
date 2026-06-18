using System;

[Serializable]
public sealed class RunSettlementDisplayStats
{
    public int durationSeconds;
    public int normalBattleOuterCleared;
    public int normalBattleMidCleared;
    public int normalBattleInnerCleared;
    public int normalBattleCoreCleared;
    public int eliteBattleOuterCleared;
    public int eliteBattleMidCleared;
    public int eliteBattleInnerCleared;
    public int eliteBattleCoreCleared;
    public int specialDungeonCleared;
    public int partyMemberCount;
    public int totalMoneyGained;
    public int craftedWeaponCount;
    public int eventTilesCompleted;
    public int eventBattlesCompleted;
    public int unexploredTilesVisited;
    public int relicCount;
    public int keysCollected;
    public int calamityTier;
    public int bossesDefeated;
    public bool calamityScoreCounted;
}
