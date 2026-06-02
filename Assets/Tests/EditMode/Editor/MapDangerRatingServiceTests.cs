using NUnit.Framework;
using UnityEngine;

public class MapDangerRatingServiceTests
{
    [Test]
    public void ResolveDefaultRating_UsesBandAndTileTypeTable()
    {
        Assert.AreEqual(1, MapDangerRatingService.ResolveDefaultRating(MapTileType.Battle, MapDangerBand.Outer));
        Assert.AreEqual(2, MapDangerRatingService.ResolveDefaultRating(MapTileType.Elite, MapDangerBand.Outer));
        Assert.AreEqual(2, MapDangerRatingService.ResolveDefaultRating(MapTileType.Battle, MapDangerBand.Mid));
        Assert.AreEqual(4, MapDangerRatingService.ResolveDefaultRating(MapTileType.Elite, MapDangerBand.Mid));
        Assert.AreEqual(3, MapDangerRatingService.ResolveDefaultRating(MapTileType.Battle, MapDangerBand.Inner));
        Assert.AreEqual(5, MapDangerRatingService.ResolveDefaultRating(MapTileType.Elite, MapDangerBand.Inner));
        Assert.AreEqual(4, MapDangerRatingService.ResolveDefaultRating(MapTileType.Battle, MapDangerBand.Core));
        Assert.AreEqual(6, MapDangerRatingService.ResolveDefaultRating(MapTileType.Elite, MapDangerBand.Core));
        Assert.AreEqual(7, MapDangerRatingService.ResolveDefaultRating(MapTileType.Boss, MapDangerBand.Core));
    }

    [Test]
    public void ResolveRating_UsesConfiguredOverrideBeforeEnemyGroupAndDefault()
    {
        EnemyGroup_SO enemyGroup = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        try
        {
            enemyGroup.dangerRatingOverride = 6;

            Assert.AreEqual(8, MapDangerRatingService.ResolveRating(
                MapTileType.Battle,
                MapDangerBand.Outer,
                enemyGroup,
                configuredOverride: 8));
            Assert.AreEqual(6, MapDangerRatingService.ResolveRating(
                MapTileType.Battle,
                MapDangerBand.Outer,
                enemyGroup,
                configuredOverride: 0));

            enemyGroup.dangerRatingOverride = 0;
            Assert.AreEqual(1, MapDangerRatingService.ResolveRating(
                MapTileType.Battle,
                MapDangerBand.Outer,
                enemyGroup,
                configuredOverride: 0));
        }
        finally
        {
            Object.DestroyImmediate(enemyGroup);
        }
    }

    [Test]
    public void GetRatingPhrase_MapsOneToEightAndClampsOutOfRange()
    {
        Assert.AreEqual("轻松", MapDangerRatingService.GetRatingPhrase(1));
        Assert.AreEqual("吃力", MapDangerRatingService.GetRatingPhrase(2));
        Assert.AreEqual("艰难", MapDangerRatingService.GetRatingPhrase(3));
        Assert.AreEqual("十分艰难", MapDangerRatingService.GetRatingPhrase(4));
        Assert.AreEqual("极其艰难", MapDangerRatingService.GetRatingPhrase(5));
        Assert.AreEqual("噩梦", MapDangerRatingService.GetRatingPhrase(6));
        Assert.AreEqual("绝望", MapDangerRatingService.GetRatingPhrase(7));
        Assert.AreEqual("死亡", MapDangerRatingService.GetRatingPhrase(8));
        Assert.AreEqual("轻松", MapDangerRatingService.GetRatingPhrase(0));
        Assert.AreEqual("死亡", MapDangerRatingService.GetRatingPhrase(99));
    }

    [Test]
    public void GetRatingRichText_UsesIncreasingRedDepthAndBlackForDespair()
    {
        string easy = MapDangerRatingService.GetRatingRichText(1);
        string nightmare = MapDangerRatingService.GetRatingRichText(6);
        string despair = MapDangerRatingService.GetRatingRichText(7);
        string death = MapDangerRatingService.GetRatingRichText(8);

        Assert.That(easy, Does.Contain("轻松"));
        Assert.That(nightmare, Does.Contain("噩梦"));
        Assert.That(despair, Does.Contain("<color=#000000>绝望</color>"));
        Assert.That(death, Does.Contain("死亡"));
        Assert.AreNotEqual(easy, nightmare);
    }
}
