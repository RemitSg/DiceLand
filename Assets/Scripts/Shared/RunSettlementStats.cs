using System;
using UnityEngine;

[Serializable]
public class RunSettlementStats
{
    public long runStartedUnixSeconds;
    public int normalBattleOuterCleared;
    public int normalBattleMidCleared;
    public int normalBattleInnerCleared;
    public int normalBattleCoreCleared;
    public int eliteBattleOuterCleared;
    public int eliteBattleMidCleared;
    public int eliteBattleInnerCleared;
    public int eliteBattleCoreCleared;
    public int specialDungeonCleared;
    public int specialDungeonScoreTotal;
    public int totalMoneyGained;
    public int craftedWeaponCount;
    public int eventTilesCompleted;
    public int eventBattlesCompleted;
    public int unexploredTilesVisited;
    public int keysCollected;
    public int bossesDefeated;

    public RunSettlementStats Clone()
    {
        return new RunSettlementStats
        {
            runStartedUnixSeconds = runStartedUnixSeconds,
            normalBattleOuterCleared = normalBattleOuterCleared,
            normalBattleMidCleared = normalBattleMidCleared,
            normalBattleInnerCleared = normalBattleInnerCleared,
            normalBattleCoreCleared = normalBattleCoreCleared,
            eliteBattleOuterCleared = eliteBattleOuterCleared,
            eliteBattleMidCleared = eliteBattleMidCleared,
            eliteBattleInnerCleared = eliteBattleInnerCleared,
            eliteBattleCoreCleared = eliteBattleCoreCleared,
            specialDungeonCleared = specialDungeonCleared,
            specialDungeonScoreTotal = specialDungeonScoreTotal,
            totalMoneyGained = totalMoneyGained,
            craftedWeaponCount = craftedWeaponCount,
            eventTilesCompleted = eventTilesCompleted,
            eventBattlesCompleted = eventBattlesCompleted,
            unexploredTilesVisited = unexploredTilesVisited,
            keysCollected = keysCollected,
            bossesDefeated = bossesDefeated
        };
    }

    public void IncrementBattle(MapTileType tileType, MapDangerBand dangerBand)
    {
        switch (tileType)
        {
            case MapTileType.Battle:
                IncrementNormalBattle(dangerBand);
                break;
            case MapTileType.Elite:
                IncrementEliteBattle(dangerBand);
                break;
            case MapTileType.Boss:
                bossesDefeated++;
                break;
        }
    }

    private void IncrementNormalBattle(MapDangerBand dangerBand)
    {
        switch (dangerBand)
        {
            case MapDangerBand.Outer:
                normalBattleOuterCleared++;
                break;
            case MapDangerBand.Mid:
                normalBattleMidCleared++;
                break;
            case MapDangerBand.Inner:
                normalBattleInnerCleared++;
                break;
            default:
                normalBattleCoreCleared++;
                break;
        }
    }

    private void IncrementEliteBattle(MapDangerBand dangerBand)
    {
        switch (dangerBand)
        {
            case MapDangerBand.Outer:
                eliteBattleOuterCleared++;
                break;
            case MapDangerBand.Mid:
                eliteBattleMidCleared++;
                break;
            case MapDangerBand.Inner:
                eliteBattleInnerCleared++;
                break;
            default:
                eliteBattleCoreCleared++;
                break;
        }
    }

    public void AddMoneyGained(int amount)
    {
        if (amount > 0)
            totalMoneyGained += amount;
    }

    public void AddSpecialDungeonScore(int scoreValue)
    {
        specialDungeonCleared++;
        specialDungeonScoreTotal += Mathf.Max(0, scoreValue);
    }
}
