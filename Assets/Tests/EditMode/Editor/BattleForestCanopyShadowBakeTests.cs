using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleForestCanopyShadowBakeTests
{
    [Test]
    public void CookiePreservesBattleRandomAndContainsLeavesBranchesAndOpenSky()
    {
        string random=JsonUtility.ToJson(Random.state);
        Color32[] pixels=BattleForestCanopyShadowBakeTools.BakePixels(128);
        Assert.AreEqual(random,JsonUtility.ToJson(Random.state));
        CollectionAssert.AreEqual(pixels,BattleForestCanopyShadowBakeTools.BakePixels(128));
        Assert.IsTrue(pixels.All(p=>p.b==0&&p.a==255));
        Assert.Greater(pixels.Count(p=>p.r>220),pixels.Length/10);
        Assert.Greater(pixels.Count(p=>p.r<20&&p.g<20),pixels.Length/5);
        Assert.Greater(pixels.Count(p=>p.g>200),pixels.Length/100);
    }

    [Test]
    public void CookieCoverageIsStableAtDifferentBakeResolutions()
    {
        Color32[] small=BattleForestCanopyShadowBakeTools.BakePixels(128);
        Color32[] large=BattleForestCanopyShadowBakeTools.BakePixels(512);
        Assert.Less(System.Math.Abs(small.Average(p=>(double)p.r)-large.Average(p=>(double)p.r)),12);
        Assert.Less(System.Math.Abs(small.Average(p=>(double)p.g)-large.Average(p=>(double)p.g)),12);
    }
}
