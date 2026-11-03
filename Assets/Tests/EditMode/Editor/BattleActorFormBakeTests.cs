using NUnit.Framework;
using UnityEngine;

public sealed class BattleActorFormBakeTests
{
    [Test]
    public void MollyLeatherDoesNotReflectAsSteelAndSeparateArmorBandsRemainMetal()
    {
        const int width=338,height=512;
        Color32[] pixels=BattleActorSurfaceBakeTools.BakePixels(BattleActorSurfaceBakeTools.MollyPath,width,height);
        System.Func<float,float,Color32> sample=(x,y)=>pixels[Mathf.FloorToInt((1-y/1941f)*height)*width+Mathf.FloorToInt(x/1280f*width)];
        Color32 leather=sample(820,640),steel=sample(685,588),gold=sample(861,588),buckle=sample(831,837);
        Assert.Less(leather.r,10);Assert.Less(leather.g,15);Assert.That(leather.b,Is.InRange(190,210));
        Assert.Greater(steel.g,240);Assert.Less(steel.b,100);
        Assert.Greater(gold.g,150);Assert.Greater(buckle.g,240);
        Assert.Less(sample(980,1060).g,10,"The broad trouser fold must remain matte cloth.");
    }

    [Test]
    public void MollyFoldFacesKeepTheExactInputSilhouetteAndUseDifferentNormalDirections()
    {
        const int width=338,height=512;
        Color32[] before=new Color32[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            before[y*width+x]=new Color32(128,128,255,(byte)((x+y)%5==0?0:255));
        Color32[] after=(Color32[])before.Clone();string random=JsonUtility.ToJson(Random.state);
        BattleActorSurfaceBakeTools.RefineFormNormals(BattleActorSurfaceBakeTools.MollyPath,after,width,height);
        for(int i=0;i<after.Length;i++)
        {
            Assert.AreEqual(before[i].a,after[i].a);
            if(before[i].a==0)Assert.AreEqual(before[i],after[i]);
            Vector3 normal=new Vector3(after[i].r/127.5f-1,after[i].g/127.5f-1,after[i].b/127.5f-1);
            Assert.Greater(normal.z,0);Assert.That(normal.magnitude,Is.EqualTo(1).Within(.015f));
        }
        System.Func<float,float,float> vertical=(x,y)=>after[Mathf.FloorToInt((1-y/1941f)*height)*width+Mathf.FloorToInt(x/1280f*width)].g/127.5f-1;
        Assert.Greater(vertical(990,1062),.25f);Assert.Less(vertical(1112,1282),-.15f);
        Assert.AreEqual(random,JsonUtility.ToJson(Random.state));
    }

    [Test]
    public void CoffinMaterialKeepsDarkPanelsSeparateFromMetalAndEmissiveGems()
    {
        foreach(int size in new[]{256,512})
        {
            Color32[] pixels=BattleActorSurfaceBakeTools.BakePixels(BattleActorSurfaceBakeTools.CoffinPath,size,size);
            System.Func<float,float,Color32> at=(x,y)=>pixels[Mathf.FloorToInt((1-y)*size)*size+Mathf.FloorToInt(x*size)];
            Assert.Greater(at(.38f,.104f).g,230);
            Assert.Less(at(.38f,.22f).g,15);
            Assert.AreEqual(0,at(.38f,.22f).a);
            Assert.Greater(at(.411f,.344f).a,240);
            Assert.Greater(at(.742f,.070f).a,220);
            Assert.AreEqual(0,at(.38f,.65f).a);
            Assert.Less(at(.80f,.65f).a,80,"Purple air stays weaker than the central gemstone.");
        }
    }

    [Test]
    public void CoffinPlanesKeepTheExactSilhouetteAndBattleRandomState()
    {
        const int size=96;bool[] mask=new bool[size*size];
        for(int i=0;i<mask.Length;i++)mask[i]=i%9!=0;
        var before=BattleActorFormBakeTools.BakeFormPixels(mask,size,size);
        var after=(Color32[])before.Clone();string random=JsonUtility.ToJson(Random.state);
        BattleActorSurfaceBakeTools.RefineFormNormals(BattleActorSurfaceBakeTools.CoffinPath,after,size,size);
        int changed=0;
        for(int i=0;i<after.Length;i++)
        {
            Assert.AreEqual(before[i].a,after[i].a);
            if(before[i].a==0)Assert.AreEqual(before[i],after[i]);
            if(!before[i].Equals(after[i]))changed++;
            Assert.Greater(after[i].b,127);
        }
        Assert.Greater(changed,1000);Assert.AreEqual(random,JsonUtility.ToJson(Random.state));
    }

    [Test]
    public void AuthoredMaterialsSeparateFacialSkinArmorAndClothAtEitherBakeResolution()
    {
        foreach (int size in new[] { 128, 256 })
        {
            Color32[] man = BattleActorSurfaceBakeTools.BakePixels(BattleActorSurfaceBakeTools.ManPath, size, size);
            Color32[] molly = BattleActorSurfaceBakeTools.BakePixels(BattleActorSurfaceBakeTools.MollyPath, size, size);
            AssertMaterial(man, size, 745f / 1230f, 210f / 1478f, true, false);
            AssertMaterial(man, size, 850f / 1230f, 270f / 1478f, false, true);
            AssertMaterial(man, size, 330f / 1230f, 400f / 1478f, true, false);
            AssertMaterial(man, size, 500f / 1230f, 1000f / 1478f, false, false);
            AssertMaterial(molly, size, 795f / 1280f, 300f / 1941f, true, false);
            AssertMaterial(molly, size, 610f / 1280f, 500f / 1941f, false, true);
            AssertMaterial(molly, size, 1000f / 1280f, 1100f / 1941f, false, false);
        }
    }

    [Test]
    public void MaterialDataIsDeterministicAndUnknownSpritesRequireTheirOwnAnnotations()
    {
        string random = JsonUtility.ToJson(Random.state);
        Color32[] first = BattleActorSurfaceBakeTools.BakePixels(BattleActorSurfaceBakeTools.MollyPath, 80, 120);
        CollectionAssert.AreEqual(first,
            BattleActorSurfaceBakeTools.BakePixels(BattleActorSurfaceBakeTools.MollyPath, 80, 120));
        Assert.Throws<System.ArgumentException>(() => BattleActorSurfaceBakeTools.BakePixels("Unknown.png", 80, 120));
        Assert.AreEqual(random, JsonUtility.ToJson(Random.state));
    }

    private static void AssertMaterial(Color32[] pixels, int size, float x, float topY, bool skin, bool metal)
    {
        Color32 value = pixels[Mathf.FloorToInt((1f - topY) * size) * size + Mathf.FloorToInt(x * size)];
        Assert.That(value.r, skin ? Is.GreaterThan(240) : Is.LessThan(15));
        Assert.That(value.g, metal ? Is.GreaterThan(240) : Is.LessThan(15));
        Assert.That(value.b, Is.InRange(50, 255));
    }

    [Test]
    public void LocalCurvesKeepSilhouetteAlphaAndUnknownFormsIntact()
    {
        const int size = 128;
        bool[] mask = new bool[size * size];
        for (int y = 4; y < size - 4; y++)
        for (int x = 4; x < size - 4; x++) mask[y * size + x] = true;
        Color32[] before = BattleActorFormBakeTools.BakeFormPixels(mask,size,size);
        Color32[] after = (Color32[])before.Clone();
        BattleActorSurfaceBakeTools.RefineFormNormals("Unknown.png",after,size,size);
        CollectionAssert.AreEqual(before,after);
        BattleActorSurfaceBakeTools.RefineFormNormals(BattleActorSurfaceBakeTools.ManPath,after,size,size);
        int changed = 0;
        for (int i = 0; i < after.Length; i++)
        {
            Assert.AreEqual(before[i].a,after[i].a);
            if (after[i].a == 0) Assert.AreEqual(before[i],after[i]);
            if (!after[i].Equals(before[i])) changed++;
            Assert.Greater(after[i].b,127,"Authored local normals stay on the front hemisphere.");
        }
        Assert.Greater(changed,1000,"Body volumes must affect internal painted surfaces, not just the alpha edge.");
    }

    [Test]
    public void ShadowSilhouetteUsesAlphaWithoutChangingAnyNormalChannel()
    {
        const int width = 32, height = 48;
        bool[] mask = new bool[width * height];
        for (int y = 4; y < 44; y++)
        for (int x = 3; x < 29; x++)
            mask[y * width + x] = !(x >= 12 && x < 20 && y >= 16 && y < 32);
        Color32[] normals = BattleActorFormBakeTools.BakeNormalPixels(mask, width, height);
        Color32[] forms = BattleActorFormBakeTools.BakeFormPixels(mask, width, height);
        for (int index = 0; index < forms.Length; index++)
        {
            Assert.AreEqual(normals[index].r, forms[index].r);
            Assert.AreEqual(normals[index].g, forms[index].g);
            Assert.AreEqual(normals[index].b, forms[index].b);
            Assert.AreEqual(mask[index] ? 255 : 0, forms[index].a);
        }
        CollectionAssert.AreEqual(forms, BattleActorFormBakeTools.BakeFormPixels(mask, width, height));
    }

    [Test]
    public void SeparateLimbsReceiveTheirOwnOutwardNormalsAndTheGapStaysFlat()
    {
        const int width = 80, height = 64;
        var mask = new bool[width * height];
        for (int y = 8; y < 56; y++)
        for (int x = 0; x < width; x++)
            mask[y * width + x] = (x >= 10 && x < 30) || (x >= 44 && x < 68);
        Color32[] pixels = BattleActorFormBakeTools.BakeNormalPixels(mask, width, height);
        Assert.Less(pixels[32 * width + 12].r, 115);
        Assert.Greater(pixels[32 * width + 27].r, 140);
        Assert.Less(pixels[32 * width + 46].r, 115);
        Assert.Greater(pixels[32 * width + 65].r, 140);
        Assert.That(pixels[32 * width + 37].r, Is.InRange(126, 129));
        Assert.That(pixels[32 * width + 37].b, Is.GreaterThan(250));
        foreach (Color32 pixel in pixels)
        {
            Vector3 normal = new Vector3(pixel.r / 255f, pixel.g / 255f, pixel.b / 255f) * 2f - Vector3.one;
            Assert.That(normal.magnitude, Is.EqualTo(1f).Within(0.012f));
            Assert.Greater(normal.z, 0f);
            Assert.AreEqual(255, pixel.a);
        }
        CollectionAssert.AreEqual(pixels, BattleActorFormBakeTools.BakeNormalPixels(mask, width, height));
    }

    [Test]
    public void EmptyAndSolidMasksRemainFiniteIncludingImageBorders()
    {
        foreach (bool solid in new[] { false, true })
        {
            var mask = new bool[16 * 16];
            if (solid) for (int i = 0; i < mask.Length; i++) mask[i] = true;
            foreach (Color32 pixel in BattleActorFormBakeTools.BakeNormalPixels(mask, 16, 16))
                Assert.Greater(pixel.b, 180);
        }
    }
}
