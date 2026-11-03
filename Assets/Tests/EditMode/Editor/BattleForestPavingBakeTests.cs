using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

public sealed class BattleForestPavingBakeTests
{
    [Test]
    public void FirstBakeRegistersAPersistentTextureBeforeSavingItsMaterialReference()
    {
        string folder = "Assets/PavingBakeTest-" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets",System.IO.Path.GetFileName(folder));
        try
        {
            string path = folder + "/Forest Paving Relief.asset";
            Texture2D map = BattleForestPavingBakeTools.BakeMap(path);
            Assert.IsTrue(EditorUtility.IsPersistent(map));
            Assert.AreEqual(path,AssetDatabase.GetAssetPath(map));
            var material = new Material(Shader.Find("DiceLand/Battle Forest Stone"));
            BattleForestPavingBakeTools.Configure(material,map);
            string materialPath = folder + "/Paving test.mat";
            AssetDatabase.CreateAsset(material,materialPath);
            AssetDatabase.ImportAsset(materialPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            Material reloaded = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.AreEqual(path,AssetDatabase.GetAssetPath(reloaded.GetTexture("_PavingReliefMap")));
            string guid = AssetDatabase.AssetPathToGUID(path);
            BattleForestPavingBakeTools.BakeMap(path);
            AssetDatabase.ImportAsset(materialPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(path));
            Assert.AreEqual(path,AssetDatabase.GetAssetPath(reloaded.GetTexture("_PavingReliefMap")));
        }
        finally { AssetDatabase.DeleteAsset(folder); }
    }

    [Test]
    public void UniformStoneAndSoilStayFlatWithDifferentCavities()
    {
        foreach (bool stone in new[] { false,true })
        {
            var input = new Color32[32*32];
            for (int i=0;i<input.Length;i++) input[i] = stone
                ? new Color32(215,195,150,255) : new Color32(70,90,30,255);
            foreach (Color32 pixel in BattleForestPavingBakeTools.BakePixels(input,32))
            {
                Assert.AreEqual(128,pixel.r);
                Assert.AreEqual(128,pixel.g);
                Assert.AreEqual(stone ? 255 : 0,pixel.a);
                Assert.AreEqual(stone ? 255 : 199,pixel.b);
            }
        }
    }

    [Test]
    public void BevelsFaceOutwardsAtBothTheInternalAndRepeatingEdge()
    {
        const int size = 64;
        var input = new Color32[size*size];
        for (int y=0;y<size;y++)
        for (int x=0;x<size;x++) input[y*size+x] = x<32
            ? new Color32(215,195,150,255) : new Color32(70,90,30,255);
        Color32[] data = BattleForestPavingBakeTools.BakePixels(input,size);
        Assert.Less(data[32*size].r,100,"The tile boundary is a real neighboring joint.");
        Assert.Greater(data[32*size+31].r,155);
        Assert.AreEqual(128,data[32*size+16].g);
        Assert.Greater(data[32*size+16].a,245);
        Assert.AreEqual(0,data[32*size+48].a);
    }

    [Test]
    public void CircularTranslationsCommuteAndDoNotConsumeBattleRandomness()
    {
        const int size = 64, dx = 11, dy = 17;
        var input = new Color32[size*size];
        var shifted = new Color32[input.Length];
        for (int y=0;y<size;y++)
        for (int x=0;x<size;x++) input[y*size+x] = x<23&&y<29
            ? new Color32(215,195,150,255) : new Color32(70,90,30,255);
        for (int y=0;y<size;y++)
        for (int x=0;x<size;x++) shifted[((y+dy)%size)*size+(x+dx)%size] = input[y*size+x];
        string random = JsonUtility.ToJson(Random.state);
        Color32[] original = BattleForestPavingBakeTools.BakePixels(input,size);
        Color32[] translated = BattleForestPavingBakeTools.BakePixels(shifted,size);
        for (int y=0;y<size;y++)
        for (int x=0;x<size;x++)
        {
            Color32 a=original[y*size+x], b=translated[((y+dy)%size)*size+(x+dx)%size];
            Assert.That(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)+Math.Abs(a.a-b.a),Is.LessThanOrEqualTo(1));
        }
        CollectionAssert.AreEqual(original,BattleForestPavingBakeTools.BakePixels(input,size));
        Assert.AreEqual(random,JsonUtility.ToJson(Random.state));
        Assert.Throws<ArgumentException>(()=>BattleForestPavingBakeTools.BakePixels(input,8));
    }
}
