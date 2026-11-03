using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleActorLightingTests
{
    private GameObject root;
    private Transform allies;
    private Transform enemies;
    private Sprite sprite;
    private Texture2D texture;
    private Material material;
    private Material uiMaterial;
    private BattleActorLightingController lighting;
    private BattleContactShadowController shadows;
    private readonly List<Object> resources = new List<Object>();
    private const string BoundsProperty = "_ForestSpriteBounds";
    private const string WeightProperty = "_ForestSurfaceWeight";

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Actor lighting fixture");
        allies = Child("Allies", root.transform);
        enemies = Child("Enemies", root.transform);
        texture = new Texture2D(4, 8);
        sprite = Sprite.Create(texture, new Rect(0, 0, 4, 8), Vector2.one * 0.5f, 2f);
        material = new Material(Shader.Find("Assets/Shaders/Lit"));
        uiMaterial = new Material(Shader.Find("UI/Default"));
        resources.AddRange(new Object[] { texture, sprite, material, uiMaterial });
        lighting = root.AddComponent<BattleActorLightingController>();
        lighting.Configure(allies, enemies);
    }

    [TearDown]
    public void TearDown()
    {
        if (lighting != null) lighting.ReleaseLighting();
        if (shadows != null) shadows.ReleaseShadows();
        if (root != null) Object.DestroyImmediate(root);
        foreach (Object resource in resources) if (resource != null) Object.DestroyImmediate(resource);
        resources.Clear();
    }

    [Test]
    public void SpawnContainersProduceOneContactShadowForEachRealUnit()
    {
        Transform spawnedAllies = Child("SpawnedAllies", allies);
        Transform spawnedEnemies = Child("SpawnedEnemies", enemies);
        CreateActor("Man", spawnedAllies, new Vector3(-5f, 2f, 0f), false);
        CreateActor("Molly", spawnedAllies, new Vector3(-2f, 2f, 3f), false);
        CreateActor("Coffin", spawnedEnemies, new Vector3(5f, 2f, 0f), true);
        shadows = root.AddComponent<BattleContactShadowController>();
        shadows.Configure(allies, enemies, uiMaterial);
        shadows.UpdateShadows();
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        Assert.AreEqual(3, renderers.Length, "SpawnedAllies must not collapse two units into one combined ellipse.");
        CollectionAssert.AreEquivalent(new[] { "Contact shadow Man", "Contact shadow Molly", "Contact shadow Coffin" },
            System.Array.ConvertAll(renderers, r => r.name));
        foreach (MeshRenderer renderer in renderers)
            Assert.That(renderer.transform.localScale.x, Is.LessThan(2f));
        shadows.UpdateShadows();
        Assert.AreEqual(3, root.GetComponentsInChildren<MeshRenderer>().Length);
    }

    [Test]
    public void AHiddenUnitDoesNotHideItsLivingAllysContactShadow()
    {
        Transform container = Child("SpawnedAllies", allies);
        SpriteRenderer first = CreateActor("First", container, new Vector3(-5f, 2f, 0f), false);
        CreateActor("Second", container, new Vector3(-2f, 2f, 0f), false);
        shadows = root.AddComponent<BattleContactShadowController>();
        shadows.Configure(allies, enemies, uiMaterial);
        shadows.UpdateShadows();
        first.forceRenderingOff = true;
        shadows.UpdateShadows();
        Assert.IsFalse(root.transform.Find("Runtime contact shadows/Contact shadow First").GetComponent<MeshRenderer>().enabled);
        Assert.IsTrue(root.transform.Find("Runtime contact shadows/Contact shadow Second").GetComponent<MeshRenderer>().enabled);
        first.forceRenderingOff = false;
        shadows.UpdateShadows();
        Assert.IsTrue(root.transform.Find("Runtime contact shadows/Contact shadow First").GetComponent<MeshRenderer>().enabled);
        first.enabled = false;
        shadows.UpdateShadows();
        Assert.IsFalse(root.transform.Find("Runtime contact shadows/Contact shadow First").GetComponent<MeshRenderer>().enabled);
        Assert.IsTrue(root.transform.Find("Runtime contact shadows/Contact shadow Second").GetComponent<MeshRenderer>().enabled);
    }

    [Test]
    public void VisualPreviewUnitsRemainSupportedWithoutCombatControllers()
    {
        SpriteRenderer preview = CreateSprite("Preview actor", enemies, new Vector3(3f, 2f, 0f), material);
        List<Transform> actors = new List<Transform>();
        BattleActorVisualUtility.CollectActorRoots(enemies, actors);
        CollectionAssert.AreEqual(new[] { preview.transform }, actors);
    }

    [Test]
    public void AUnitRootDoesNotAlsoRegisterItsBodyAsAnotherUnit()
    {
        SpriteRenderer body = CreateActor("One unit", allies, Vector3.up * 2f, false);
        List<Transform> actors = new List<Transform>();
        BattleActorVisualUtility.CollectActorRoots(body.transform.parent, actors);
        CollectionAssert.AreEqual(new[] { body.transform.parent }, actors);
    }

    [Test]
    public void WorldBoundsFollowMovementAndMirroringWithoutEditingTheSharedMaterial()
    {
        SpriteRenderer body = CreateActor("Moving", Child("SpawnedAllies", allies), new Vector3(-2f, 2f, 0f), false);
        lighting.UpdateLighting();
        Material instance = body.sharedMaterial;
        Vector4 before = instance.GetVector(BoundsProperty);
        Assert.Greater(instance.GetFloat(WeightProperty), 0f);
        body.transform.parent.position += Vector3.right * 3f;
        body.flipX = true;
        lighting.UpdateLighting();
        Vector4 after = body.sharedMaterial.GetVector(BoundsProperty);
        Assert.That(after.x - before.x, Is.EqualTo(3f).Within(0.001f));
        Assert.That(after.z, Is.EqualTo(before.z).Within(0.001f));
        Assert.AreNotSame(material, body.sharedMaterial);
        Assert.AreSame(instance, body.sharedMaterial);
        Assert.AreEqual(0f, material.GetFloat(WeightProperty));
    }

    [Test]
    public void ReleaseRestoresTheSharedMaterialAndRetainsANewerHitFlash()
    {
        SpriteRenderer body = CreateActor("Flash witness", allies, Vector3.up * 2f, false);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_Color", Color.red);
        body.SetPropertyBlock(block);
        lighting.UpdateLighting();
        Material instance = body.sharedMaterial;
        body.GetPropertyBlock(block);
        block.SetColor("_Color", Color.blue);
        body.SetPropertyBlock(block);
        lighting.ReleaseLighting();
        body.GetPropertyBlock(block);
        Assert.AreSame(material, body.sharedMaterial);
        Assert.IsTrue(instance == null, "Owned surface materials must be released immediately in edit mode.");
        Assert.AreEqual(Color.blue, block.GetColor("_Color"));
    }

    [Test]
    public void FormDataTracksSpriteMirroringAndReplacementWithoutTouchingSharedMaterials()
    {
        SpriteRenderer body = CreateActor("Form witness", allies, Vector3.up * 2f, false);
        BattleActorFormLibrary library = ScriptableObject.CreateInstance<BattleActorFormLibrary>();
        Texture2D map = new Texture2D(4, 8, TextureFormat.RGBA32, false, true);
        resources.Add(library);
        resources.Add(map);
        typeof(BattleActorFormLibrary).GetField("entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(library, new[] { new BattleActorFormLibrary.Entry { sprite = sprite, formMap = map, materialMap = map, emissiveAlpha = true } });
        lighting.Configure(allies, enemies, library);
        lighting.UpdateLighting();
        Material runtime = body.sharedMaterial;
        Assert.AreSame(map, runtime.GetTexture("_ForestFormMap"));
        Assert.AreSame(map, runtime.GetTexture("_ForestMaterialMap"));
        Assert.Greater(runtime.GetFloat("_ForestMaterialWeight"), 0f);
        Assert.AreEqual(1f,runtime.GetFloat("_ForestEmissionWeight"));
        Assert.AreEqual(0f,material.GetFloat("_ForestEmissionWeight"));
        Assert.AreEqual(0f, material.GetFloat("_ForestMaterialWeight"));
        Assert.Greater(runtime.GetVector("_ForestFormParams").x, 0f);
        Assert.AreEqual(0f, material.GetVector("_ForestFormParams").x);

        body.flipX = true;
        body.flipY = true;
        lighting.UpdateLighting();
        Assert.AreSame(runtime, body.sharedMaterial);
        Assert.AreEqual(-1f, runtime.GetVector("_ForestFormParams").y);
        Assert.AreEqual(-1f, runtime.GetVector("_ForestFormParams").z);

        Sprite replacement = Sprite.Create(texture, new Rect(0, 0, 4, 8), Vector2.one * 0.5f, 2f);
        resources.Add(replacement);
        body.sprite = replacement; // Identical bounds must still invalidate the old form binding.
        lighting.UpdateLighting();
        Assert.AreEqual(0f, runtime.GetVector("_ForestFormParams").x);
        Assert.IsNull(runtime.GetTexture("_ForestFormMap"));
        Assert.AreEqual(0f, runtime.GetFloat("_ForestMaterialWeight"));
        Assert.AreEqual(0f,runtime.GetFloat("_ForestEmissionWeight"));
        Assert.IsNull(runtime.GetTexture("_ForestMaterialMap"));
        lighting.ReleaseLighting();
        Assert.AreSame(material, body.sharedMaterial);
    }

    [Test]
    public void ExistingMaterialAlphaDoesNotBecomeEmissionWithoutAnExplicitTag()
    {
        SpriteRenderer body=CreateActor("Non emissive material",allies,Vector3.up*2f,false);
        var library=ScriptableObject.CreateInstance<BattleActorFormLibrary>();
        var map=new Texture2D(4,8,TextureFormat.RGBA32,false,true);
        resources.Add(library);resources.Add(map);
        typeof(BattleActorFormLibrary).GetField("entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .SetValue(library,new[]{new BattleActorFormLibrary.Entry{sprite=sprite,materialMap=map}});
        lighting.Configure(allies,enemies,library);lighting.UpdateLighting();
        Assert.AreSame(map,body.sharedMaterial.GetTexture("_ForestMaterialMap"));
        Assert.Greater(body.sharedMaterial.GetFloat("_ForestMaterialWeight"),0f);
        Assert.AreEqual(0f,body.sharedMaterial.GetFloat("_ForestEmissionWeight"));
        Assert.IsFalse(library.UsesEmissiveAlpha(sprite));
    }

    [Test]
    public void AnExternalMaterialOverrideIsNeverReplacedByTheLightingController()
    {
        SpriteRenderer body = CreateActor("Replaced block", allies, Vector3.up * 2f, false);
        lighting.UpdateLighting();
        Material instance = body.sharedMaterial;
        body.sharedMaterial = uiMaterial;
        lighting.UpdateLighting();
        Assert.AreSame(uiMaterial, body.sharedMaterial);
        lighting.ReleaseLighting();
        Assert.AreSame(uiMaterial, body.sharedMaterial);
        Assert.IsTrue(instance == null);
    }

    [Test]
    public void NonActorMaterialsRetainTheirOriginalPropertyBlocks()
    {
        SpriteRenderer body = CreateActor("Actor", allies, Vector3.up * 2f, false);
        SpriteRenderer decoration = CreateSprite("UI decoration", body.transform.parent, Vector3.zero, uiMaterial);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetFloat("_Witness", 17f);
        decoration.SetPropertyBlock(block);
        lighting.UpdateLighting();
        decoration.GetPropertyBlock(block);
        Assert.AreEqual(17f, block.GetFloat("_Witness"));
        Assert.AreEqual(0f, block.GetFloat(WeightProperty));
    }

    private SpriteRenderer CreateActor(string name, Transform parent, Vector3 position, bool enemy)
    {
        Transform actor = Child(name, parent);
        actor.position = position;
        if (enemy) actor.gameObject.AddComponent<BattleEnemyController>();
        else actor.gameObject.AddComponent<BattleAllyController>();
        return CreateSprite("Body", actor, Vector3.zero, material);
    }

    private SpriteRenderer CreateSprite(string name, Transform parent, Vector3 position, Material mat)
    {
        Transform item = Child(name, parent);
        item.localPosition = position;
        SpriteRenderer renderer = item.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = mat;
        return renderer;
    }

    private static Transform Child(string name, Transform parent)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }
}
