using NUnit.Framework;
using UnityEngine;

public sealed class BattleActorFormBakeTests
{
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
