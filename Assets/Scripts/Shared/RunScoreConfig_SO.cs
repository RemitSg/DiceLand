using UnityEngine;

[CreateAssetMenu(fileName = "RunScoreConfig", menuName = "Game/Run Score Config")]
public class RunScoreConfig_SO : ScriptableObject
{
    public int shortRunThresholdSeconds = 300;
    public float shortRunMultiplier = 0.1f;

    public int normalBattleOuterScore = 20;
    public int normalBattleMidScore = 50;
    public int normalBattleInnerScore = 100;
    public int normalBattleCoreScore = 200;

    public int eliteBattleOuterScore = 50;
    public int eliteBattleMidScore = 150;
    public int eliteBattleInnerScore = 250;
    public int eliteBattleCoreScore = 500;

    public int defaultSpecialDungeonScore = 200;
    public int partyMemberScore = 50;
    public int minimumPartyMembersForScore = 2;
    public float moneyScoreMultiplier = 0.2f;
    public int craftedWeaponScore = 100;
    public int completedEventTileScore = 10;
    public int completedEventBattleScore = 50;
    public int unexploredTileScore = 5;
    public int relicScore = 20;
    public int keyScore = 50;
    public int bossDefeatScore = 1000;
    public int abilityPointDivisor = 1000;

    public int calamityTier1Score = 100;
    public int calamityTier2Score = 200;
    public int calamityTier3Score = 400;
    public int calamityTier4Score = 1000;
    public int calamityTier5Score = 2000;
    public int calamityTier6Score = 4000;
    public int calamityScorePerTierAfterSix = 2000;

    public static RunScoreConfig_SO CreateDefaultRuntimeConfig()
    {
        return CreateInstance<RunScoreConfig_SO>();
    }

    public int GetNormalBattleScore(MapDangerBand band)
    {
        switch (band)
        {
            case MapDangerBand.Outer:
                return normalBattleOuterScore;
            case MapDangerBand.Mid:
                return normalBattleMidScore;
            case MapDangerBand.Inner:
                return normalBattleInnerScore;
            default:
                return normalBattleCoreScore;
        }
    }

    public int GetEliteBattleScore(MapDangerBand band)
    {
        switch (band)
        {
            case MapDangerBand.Outer:
                return eliteBattleOuterScore;
            case MapDangerBand.Mid:
                return eliteBattleMidScore;
            case MapDangerBand.Inner:
                return eliteBattleInnerScore;
            default:
                return eliteBattleCoreScore;
        }
    }

    public int GetCalamityScore(int tier)
    {
        switch (tier)
        {
            case 0:
                return 0;
            case 1:
                return calamityTier1Score;
            case 2:
                return calamityTier2Score;
            case 3:
                return calamityTier3Score;
            case 4:
                return calamityTier4Score;
            case 5:
                return calamityTier5Score;
            case 6:
                return calamityTier6Score;
            default:
                return calamityTier6Score + Mathf.Max(0, tier - 6) * calamityScorePerTierAfterSix;
        }
    }
}
