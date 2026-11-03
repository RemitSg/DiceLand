using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleForestCanopyBakeTests
{
    [Test]
    public void BarkCurvesHaveOpposingSideNormalsAndDoNotRelightTheStoneTerrace()
    {
        const int width=256,height=144;
        Color32[] pixels=BattleForestCanopyBakeTools.BakePixels(Enumerable.Repeat(true,width*height).ToArray(),width,height);
        Color32 left=At(pixels,width,height,435,550),right=At(pixels,width,height,530,550);
        Assert.Less(left.r,128);Assert.Greater(right.r,128);
        Assert.Greater(left.a,200);Assert.Greater(right.a,200);
        Assert.Less(left.b,25);Assert.Less(right.b,25);
        Color32 terrace=At(pixels,width,height,800,900);
        Assert.AreEqual(0,terrace.a);Assert.AreEqual(0,terrace.b);
        foreach(Color32 pixel in pixels)
        {
            Vector2 normal=new Vector2(pixel.r/127.5f-1f,pixel.g/127.5f-1f);
            Assert.Less(normal.sqrMagnitude,1.02f,"Mip filtering must be able to reconstruct a finite front hemisphere.");
        }
    }

    [Test]
    public void ThinLeafCoverageIncreasesNearAHoleButTheHoleItselfIsNeutral()
    {
        const int width=256,height=144;
        bool[] mask=Enumerable.Repeat(true,width*height).ToArray();
        int x=Mathf.FloorToInt(650f/1672f*width),y=Mathf.FloorToInt((1f-120f/941f)*height);
        Color32[] solid=BattleForestCanopyBakeTools.BakePixels(mask,width,height);
        mask[y*width+x+1]=false;
        Color32[] cut=BattleForestCanopyBakeTools.BakePixels(mask,width,height);
        Assert.Greater(cut[y*width+x].a,solid[y*width+x].a+30);
        Assert.Greater(cut[y*width+x].b,200);
        Assert.AreEqual(new Color32(128,128,0,0),cut[y*width+x+1]);
    }

    [Test]
    public void BakePreservesInputAndBattleRandomStateAndIsDeterministic()
    {
        const int width=96,height=54;
        bool[] mask=Enumerable.Range(0,width*height).Select(i=>i%7!=0).ToArray();
        bool[] before=(bool[])mask.Clone();string random=JsonUtility.ToJson(Random.state);
        Color32[] first=BattleForestCanopyBakeTools.BakePixels(mask,width,height);
        CollectionAssert.AreEqual(first,BattleForestCanopyBakeTools.BakePixels(mask,width,height));
        CollectionAssert.AreEqual(before,mask);Assert.AreEqual(random,JsonUtility.ToJson(Random.state));
    }

    private static Color32 At(Color32[] pixels,int width,int height,float x,float topY)
    {
        return pixels[Mathf.FloorToInt((1f-topY/941f)*height)*width+Mathf.FloorToInt(x/1672f*width)];
    }
}
