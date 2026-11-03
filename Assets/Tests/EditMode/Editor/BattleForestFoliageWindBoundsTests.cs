using NUnit.Framework;
using UnityEngine;

public sealed class BattleForestFoliageWindBoundsTests
{
    private GameObject root;
    private Texture2D texture;
    private Sprite sprite, replacement;
    private Material material;
    private SpriteRenderer renderer;
    private readonly BattleForestFoliageWindBounds wind = new BattleForestFoliageWindBounds();

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Foliage bounds fixture");
        texture = new Texture2D(16, 8);
        sprite = Sprite.Create(texture, new Rect(0, 0, 16, 8), new Vector2(.3f, .7f), 10, 0, SpriteMeshType.FullRect);
        material = new Material(Shader.Find("DiceLand/Battle Forest Foliage"));
        material.SetFloat("_LocalWindStrength", .85f); material.SetFloat("_SwayAmplitude", .045f);
        renderer = new GameObject("Moving canopy").AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(root.transform);renderer.sprite=sprite;renderer.sharedMaterial=material;
    }

    [TearDown]
    public void TearDown()
    {
        wind.Release();Object.DestroyImmediate(root);Object.DestroyImmediate(sprite);
        if(replacement!=null)Object.DestroyImmediate(replacement);
        Object.DestroyImmediate(texture);Object.DestroyImmediate(material);
    }

    [Test]
    public void ExpandedBoundsContainWorldWindAtRotatedMirroredAndUnequalScales()
    {
        renderer.transform.SetPositionAndRotation(new Vector3(5,2,7),Quaternion.Euler(13,28,11));
        renderer.transform.localScale=new Vector3(-2,.5f,1.25f);
        Bounds original=renderer.localBounds;Vector2[] vertices=sprite.vertices;
        wind.Initialize(root.transform);Bounds padded=renderer.localBounds;padded.Expand(.0001f);
        for(int corner=0;corner<8;corner++)for(int shift=0;shift<4;shift++)
        {
            Vector3 local=original.center+Vector3.Scale(original.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
            Vector3 position=renderer.transform.TransformPoint(local)+new Vector3((shift&1)==0?-.37355f:.37355f,(shift&2)==0?-.032028f:.032028f,0);
            Assert.IsTrue(padded.Contains(renderer.transform.InverseTransformPoint(position)));
        }
        Assert.AreSame(sprite,renderer.sprite);Assert.AreSame(material,renderer.sharedMaterial);
        CollectionAssert.AreEqual(vertices,sprite.vertices);
        wind.Release();Assert.AreEqual(original,renderer.localBounds);
        replacement=Sprite.Create(texture,new Rect(0,0,8,4),Vector2.one*.5f,10,0,SpriteMeshType.FullRect);
        renderer.sprite=replacement;Assert.AreEqual(replacement.bounds,renderer.localBounds);
    }

    [Test]
    public void ReinitializingNeverAccumulatesPaddingAndReleaseKeepsAnExternalOverride()
    {
        Bounds original=renderer.localBounds;
        wind.Initialize(root.transform);Bounds once=renderer.localBounds;
        wind.Initialize(root.transform);Assert.AreEqual(once,renderer.localBounds);
        Bounds external=new Bounds(new Vector3(1,2,3),new Vector3(8,9,10));renderer.localBounds=external;
        wind.Release();Assert.AreEqual(external,renderer.localBounds);
        renderer.localBounds=original;material.SetFloat("_LocalWindStrength",0);
        wind.Initialize(root.transform);Assert.AreEqual(original,renderer.localBounds);
        material.SetFloat("_LocalWindStrength",.85f);renderer.localBounds=external;
        wind.Initialize(root.transform);wind.Release();Assert.AreEqual(external,renderer.localBounds);
    }

    [Test]
    public void ReplacedSpritesDoNotKeepThePreviousCustomBounds()
    {
        wind.Initialize(root.transform);
        replacement=Sprite.Create(texture,new Rect(0,0,8,4),Vector2.one*.5f,10,0,SpriteMeshType.FullRect);
        renderer.sprite=replacement;
        wind.Release();Assert.AreSame(replacement,renderer.sprite);
        Assert.AreEqual(replacement.bounds,renderer.localBounds);
    }
}
