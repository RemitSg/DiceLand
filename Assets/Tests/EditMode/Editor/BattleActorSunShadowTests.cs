using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleActorSunShadowTests
{
    private GameObject root;
    private Transform allies;
    private Transform enemies;
    private Texture2D texture;
    private Sprite sprite;
    private Material material;
    private Light sun;
    private BattleActorSunShadowController shadows;
    private float previousLighting;

    [SetUp]
    public void SetUp()
    {
        previousLighting = Shader.GetGlobalFloat("_DiceLandForestLighting");
        Shader.SetGlobalFloat("_DiceLandForestLighting", 1f);
        root = new GameObject("Sun shadow fixture");
        allies = Child("Allies", root.transform); enemies = Child("Enemies", root.transform);
        texture = new Texture2D(8, 16);
        sprite = Sprite.Create(texture, new Rect(0, 0, 8, 16), new Vector2(0.3f,0.2f), 2f);
        material = new Material(Shader.Find("DiceLand/Battle Actor Sun Shadow"));
        sun = Child("Sun", root.transform).gameObject.AddComponent<Light>();
        sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(52f,218f,0f);
        shadows = root.AddComponent<BattleActorSunShadowController>();
        shadows.Configure(allies, enemies, material, null, sun);
    }

    [TearDown]
    public void TearDown()
    {
        shadows.ReleaseShadows(); Object.DestroyImmediate(root); Object.DestroyImmediate(material);
        Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
        Shader.SetGlobalFloat("_DiceLandForestLighting", previousLighting);
    }

    [Test]
    public void SpawnedUnitsShareGeometryAndKeepTheirSourceMaterialsAndRandomState()
    {
        SpriteRenderer first = Actor("First", Child("SpawnedAllies", allies));
        SpriteRenderer second = Actor("Second", first.transform.parent.parent);
        Actor("Enemy", Child("SpawnedEnemies", enemies), true);
        Material sourceMaterial = first.sharedMaterial;
        MaterialPropertyBlock witness = new MaterialPropertyBlock(); witness.SetFloat("_Witness", 17f);
        first.SetPropertyBlock(witness);
        string random = JsonUtility.ToJson(Random.state);
        shadows.UpdateShadows(); shadows.UpdateShadows();
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        Assert.AreEqual(3, renderers.Length);
        Mesh[] meshes = renderers.Select(r => r.GetComponent<MeshFilter>().sharedMesh).ToArray();
        Assert.IsTrue(meshes.All(mesh => mesh == meshes[0]), "Identical sprites share one owned mesh.");
        Assert.That(meshes[0].vertexCount, Is.EqualTo(4));
        Assert.AreSame(sourceMaterial, first.sharedMaterial);
        first.GetPropertyBlock(witness); Assert.AreEqual(17f, witness.GetFloat("_Witness"));
        Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
        Assert.IsEmpty(root.GetComponentsInChildren<Collider2D>());
        shadows.ReleaseShadows(); shadows.ReleaseShadows();
        Assert.IsTrue(meshes[0] == null);
        Assert.IsEmpty(root.GetComponentsInChildren<MeshRenderer>());
        Assert.AreEqual(random, JsonUtility.ToJson(Random.state));
    }

    [Test]
    public void VisibilityMirroringAndSpriteReplacementTrackTheActualBody()
    {
        SpriteRenderer body = Actor("Actor", allies);
        shadows.UpdateShadows();
        MeshRenderer shadow = root.GetComponentsInChildren<MeshRenderer>().Single();
        body.forceRenderingOff = true; shadows.UpdateShadows(); Assert.IsFalse(shadow.enabled);
        body.forceRenderingOff = false; body.flipX = true; body.flipY = true;
        body.color = new Color(1f,1f,1f,0.4f); shadows.UpdateShadows();
        Assert.IsTrue(shadow.enabled);
        MaterialPropertyBlock properties = new MaterialPropertyBlock(); shadow.GetPropertyBlock(properties);
        Assert.AreEqual(new Vector4(-1f,-1f,0.4f,0f), properties.GetVector("_Flip"));
        Assert.AreEqual(body.transform.localToWorldMatrix, properties.GetMatrix("_CasterToWorld"));
        Mesh previous = shadow.GetComponent<MeshFilter>().sharedMesh;
        Sprite replacement = Sprite.Create(texture, new Rect(0,0,8,16), new Vector2(0.8f,0.5f), 2f);
        try
        {
            body.sprite = replacement; shadows.UpdateShadows();
            Assert.AreNotSame(previous, shadow.GetComponent<MeshFilter>().sharedMesh);
            body.enabled = false; shadows.UpdateShadows(); Assert.IsFalse(shadow.enabled);
            body.enabled = true; body.gameObject.SetActive(false); shadows.UpdateShadows(); Assert.IsFalse(shadow.enabled);
        }
        finally { Object.DestroyImmediate(replacement); }
    }

    [Test]
    public void ProjectedBoundsFollowTheCasterAndTheSunRatherThanTheStandingSpriteBounds()
    {
        SpriteRenderer body = Actor("Moving", allies);
        body.transform.parent.position = new Vector3(4f,0f,2f);
        body.transform.localPosition = new Vector3(0f,2f,0f);
        body.transform.localScale = new Vector3(-1.3f,0.7f,1f);
        body.flipX = true;
        shadows.UpdateShadows();
        MeshRenderer shadow = root.GetComponentsInChildren<MeshRenderer>().Single();
        Bounds oldBounds = shadow.bounds;
        body.transform.parent.position += Vector3.right * 3f;
        shadows.UpdateShadows();
        Assert.That(shadow.bounds.center.x - oldBounds.center.x, Is.EqualTo(3f).Within(0.001f));
        Assert.That(shadow.bounds.center.y, Is.EqualTo(0.06f).Within(0.001f));
        Assert.That(shadow.bounds.size.y, Is.LessThan(0.1f));
        Bounds beforeSunTurn = shadow.bounds;
        sun.transform.rotation = Quaternion.Euler(52f,140f,0f);
        shadows.UpdateShadows();
        Assert.That(Vector3.Distance(beforeSunTurn.center,shadow.bounds.center), Is.GreaterThan(0.5f));
        Shader.SetGlobalFloat("_DiceLandForestLighting",0f); shadows.UpdateShadows(); Assert.IsFalse(shadow.enabled);
    }

    [Test]
    public void LegacyNormalLibrariesDoNotBecomeOpaqueRectangularShadowMasks()
    {
        Actor("Masked", allies);
        BattleActorFormLibrary library = ScriptableObject.CreateInstance<BattleActorFormLibrary>();
        typeof(BattleActorFormLibrary).GetField("entries",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(library,new[]{new BattleActorFormLibrary.Entry{sprite=sprite,formMap=texture}});
        try
        {
            Assert.IsNull(library.FindShadowMask(sprite));
            typeof(BattleActorFormLibrary).GetField("silhouetteAlpha",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(library,true);
            Assert.AreSame(texture,library.FindShadowMask(sprite));
            shadows.Configure(allies,enemies,material,library,sun); shadows.UpdateShadows();
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            root.GetComponentsInChildren<MeshRenderer>().Single().GetPropertyBlock(properties);
            Assert.AreSame(texture,properties.GetTexture("_ShadowMask"));
            Assert.AreEqual(1f,properties.GetVector("_MaskMetrics").w);
        }
        finally { Object.DestroyImmediate(library); }
    }

    private SpriteRenderer Actor(string name, Transform parent, bool enemy = false)
    {
        Transform actor = Child(name,parent);
        if (enemy) actor.gameObject.AddComponent<BattleEnemyController>();
        else actor.gameObject.AddComponent<BattleAllyController>();
        SpriteRenderer body = Child("Body",actor).gameObject.AddComponent<SpriteRenderer>();
        body.sprite = sprite;
        return body;
    }

    private static Transform Child(string name,Transform parent)
    {
        Transform child = new GameObject(name).transform; child.SetParent(parent,false); return child;
    }
}
