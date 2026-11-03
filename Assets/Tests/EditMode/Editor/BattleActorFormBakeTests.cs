using NUnit.Framework;
using UnityEngine;

public sealed class BattleActorFormBakeTests
{
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
