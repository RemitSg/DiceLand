using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

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

    [Test]
    public void SeparatedSolesHaveTheirOwnShadowsAndMirroringKeepsThemUnderTheFeet()
    {
        SpriteRenderer body = CreateActor("Standing", allies, Vector3.zero, false);
        body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BattleActorSurfaceBakeTools.ManPath);
        Transform actor = body.transform.parent;
        actor.position = new Vector3(-2f, -body.sprite.bounds.min.y, 3f);
        actor.gameObject.AddComponent<Weapon>(); // Equipment data on the unit root is not a held-weapon renderer.
        shadows = root.AddComponent<BattleContactShadowController>();
        shadows.Configure(allies, enemies, uiMaterial);
        string random = JsonUtility.ToJson(Random.state);
        shadows.UpdateShadows();
        MeshRenderer first = root.transform.Find("Runtime contact shadows/Contact shadow Standing").GetComponent<MeshRenderer>();
        MeshRenderer second = root.transform.Find("Runtime contact shadows/Contact shadow Standing foot 2").GetComponent<MeshRenderer>();
        Assert.Less(first.transform.position.x, body.bounds.center.x - .5f);
        Assert.Greater(second.transform.position.x, body.bounds.center.x + .5f);
        Assert.AreSame(first.GetComponent<MeshFilter>().sharedMesh, second.GetComponent<MeshFilter>().sharedMesh);
        Assert.AreSame(uiMaterial, first.sharedMaterial); Assert.AreSame(uiMaterial, second.sharedMaterial);
        Vector3 beforeFirst = first.transform.position, beforeSecond = second.transform.position;
        body.flipX = true; shadows.UpdateShadows();
        Assert.That(first.transform.position.x, Is.EqualTo(2f*actor.position.x-beforeFirst.x).Within(.001f));
        Assert.That(second.transform.position.x, Is.EqualTo(2f*actor.position.x-beforeSecond.x).Within(.001f));
        actor.localScale = new Vector3(-1, 1, 1); shadows.UpdateShadows();
        Assert.That(first.transform.position.x, Is.EqualTo(beforeFirst.x).Within(.001f));
        Assert.That(second.transform.position.x, Is.EqualTo(beforeSecond.x).Within(.001f));
        Assert.AreEqual(random, JsonUtility.ToJson(Random.state));
    }

    [Test]
    public void RaisedFeetBroadenAndFadeWithoutChangingTheSharedMaterial()
    {
        SpriteRenderer body = CreateActor("Raised", allies, Vector3.zero, false);
        body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BattleActorSurfaceBakeTools.ManPath);
        body.transform.parent.position = Vector3.up * -body.sprite.bounds.min.y;
        shadows = root.AddComponent<BattleContactShadowController>();
        shadows.Configure(allies, enemies, uiMaterial); shadows.UpdateShadows();
        MeshRenderer contact = root.GetComponentInChildren<MeshRenderer>();
        var block = new MaterialPropertyBlock(); contact.GetPropertyBlock(block);
        float grounded = block.GetFloat("_ContactStrength"), width = contact.transform.localScale.x;
        body.transform.parent.position += Vector3.up;
        shadows.UpdateShadows(); contact.GetPropertyBlock(block);
        Assert.Less(block.GetFloat("_ContactStrength"), grounded * .7f);
        Assert.Greater(contact.transform.localScale.x, width);
        Assert.That(contact.transform.position.y, Is.EqualTo(.055f).Within(.001f));
        Assert.AreSame(uiMaterial, contact.sharedMaterial);
        Assert.IsFalse(uiMaterial.HasProperty("_ContactStrength"), "Only the shadow property block is changed.");
    }

    [Test]
    public void FootShadowsHandleReplacementInvisibilityAndDestroyedActorsWithoutOrphans()
    {
        SpriteRenderer body = CreateActor("Lifecycle", allies, Vector3.zero, false);
        Sprite man = AssetDatabase.LoadAssetAtPath<Sprite>(BattleActorSurfaceBakeTools.ManPath);
        body.sprite = man;
        shadows = root.AddComponent<BattleContactShadowController>(); shadows.Configure(allies, enemies, uiMaterial);
        shadows.UpdateShadows();
        Assert.AreEqual(2, System.Array.FindAll(root.GetComponentsInChildren<MeshRenderer>(), r=>r.enabled).Length);
        body.sprite = sprite; shadows.UpdateShadows();
        Assert.AreEqual(1, System.Array.FindAll(root.GetComponentsInChildren<MeshRenderer>(), r=>r.enabled).Length);
        body.sprite = man; body.flipY = true; shadows.UpdateShadows();
        Assert.AreEqual(1, System.Array.FindAll(root.GetComponentsInChildren<MeshRenderer>(), r=>r.enabled).Length);
        body.flipY = false; body.color = new Color(1,1,1,0); shadows.UpdateShadows();
        Assert.AreEqual(0, System.Array.FindAll(root.GetComponentsInChildren<MeshRenderer>(), r=>r.enabled).Length);
        body.color = Color.white; shadows.UpdateShadows();
        Assert.AreEqual(2, System.Array.FindAll(root.GetComponentsInChildren<MeshRenderer>(), r=>r.enabled).Length);
        Object.DestroyImmediate(body.transform.parent.gameObject); shadows.UpdateShadows();
        Assert.AreEqual(0, root.GetComponentsInChildren<MeshRenderer>().Length);
        shadows.ReleaseShadows(); shadows.ReleaseShadows();
        Assert.IsNull(root.transform.Find("Runtime contact shadows"));
    }

    [Test]
    public void InactiveEquipmentAndSpeedUiCannotReplaceBodyContactContours()
    {
        SpriteRenderer body = CreateActor("Filtering", allies, Vector3.zero, false);
        body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BattleActorSurfaceBakeTools.ManPath);
        Transform actor = body.transform.parent;
        Transform held = Child("Held weapon", actor); held.gameObject.SetActive(false); held.gameObject.AddComponent<Weapon>();
        CreateSprite("Weapon art", held, Vector3.zero, material).transform.localScale = Vector3.one*100;
        Transform badge = Child("Speed UI", actor); badge.gameObject.SetActive(false); badge.gameObject.AddComponent<SpeedButton>();
        CreateSprite("Badge art", badge, Vector3.zero, material).transform.localScale = Vector3.one*100;
        shadows = root.AddComponent<BattleContactShadowController>(); shadows.Configure(allies, enemies, uiMaterial);
        shadows.UpdateShadows();
        held.gameObject.SetActive(true); // Initially inactive renderers must never enter the cached body list.
        badge.gameObject.SetActive(true); shadows.UpdateShadows();
        MeshRenderer[] contacts = root.GetComponentsInChildren<MeshRenderer>();
        Assert.AreEqual(2, contacts.Length);
        Assert.IsTrue(System.Array.TrueForAll(contacts, r=>r.enabled && r.transform.localScale.x<2));
    }

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
            .SetValue(library, new[] { new BattleActorFormLibrary.Entry { sprite = sprite, formMap = map, materialMap = map, emissiveAlpha = true, fabricSheen = true } });
        lighting.Configure(allies, enemies, library);
        lighting.UpdateLighting();
        Material runtime = body.sharedMaterial;
        Assert.AreSame(map, runtime.GetTexture("_ForestFormMap"));
        Assert.AreSame(map, runtime.GetTexture("_ForestMaterialMap"));
        Assert.Greater(runtime.GetFloat("_ForestMaterialWeight"), 0f);
        Assert.AreEqual(1f,runtime.GetFloat("_ForestEmissionWeight"));
        Assert.AreEqual(0f,material.GetFloat("_ForestEmissionWeight"));
        Assert.AreEqual(1f,runtime.GetFloat("_ForestSheenStrength"));
        Assert.AreEqual(0f,material.GetFloat("_ForestSheenStrength"));
        Assert.AreEqual(1f, runtime.GetFloat("_ForestSkinScatterStrength"));
        Assert.AreEqual(0f, material.GetFloat("_ForestSkinScatterStrength"));
        Assert.AreEqual(1f, runtime.GetFloat("_ForestMetalReflectionStrength"));
        Assert.AreEqual(0f, material.GetFloat("_ForestMetalReflectionStrength"));
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
        Assert.AreEqual(0f,runtime.GetFloat("_ForestSheenStrength"));
        Assert.AreEqual(0f, runtime.GetFloat("_ForestSkinScatterStrength"));
        Assert.AreEqual(0f, runtime.GetFloat("_ForestMetalReflectionStrength"));
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
        Assert.IsFalse(library.UsesFabricSheen(sprite));
        Assert.AreEqual(0f,body.sharedMaterial.GetFloat("_ForestSheenStrength"));
        Assert.IsFalse(library.UsesHairAlpha(sprite));
        Assert.AreEqual(0f,body.sharedMaterial.GetVector("_ForestHairParams").z);
    }

    [Test]
    public void AuthoredHairBindsItsFlowAndClearsOnSpriteReplacementWithoutChangingSharedMaterials()
    {
        SpriteRenderer body=CreateActor("Hair binding",allies,Vector3.up*2f,false);
        var library=ScriptableObject.CreateInstance<BattleActorFormLibrary>();
        var form=new Texture2D(4,8,TextureFormat.RGBA32,false,true);
        var hairMap=new Texture2D(4,8,TextureFormat.RGBA32,false,true);
        resources.Add(library);resources.Add(form);resources.Add(hairMap);
        typeof(BattleActorFormLibrary).GetField("entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .SetValue(library,new[]{new BattleActorFormLibrary.Entry{sprite=sprite,formMap=form,materialMap=hairMap,hairAlpha=true,hairDirection=new Vector2(-.96f,.28f)}});
        typeof(BattleActorFormLibrary).GetField("silhouetteAlpha",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(library,true);
        lighting.Configure(allies,enemies,library);lighting.UpdateLighting();
        Material runtime=body.sharedMaterial;
        Assert.That(Vector4.Distance(new Vector4(-.96f,.28f,1,0),runtime.GetVector("_ForestHairParams")),Is.LessThan(.0001f));
        Assert.AreEqual(0,material.GetVector("_ForestHairParams").z);
        Assert.AreEqual(0,runtime.GetFloat("_ForestEmissionWeight"));
        Assert.AreSame(form,library.FindShadowMask(sprite),"Hair coverage must never replace the independent shadow silhouette.");
        body.flipX=true;body.flipY=true;lighting.UpdateLighting();
        Assert.AreEqual(-1,runtime.GetVector("_ForestFormParams").y);
        Assert.AreEqual(-1,runtime.GetVector("_ForestFormParams").z);
        Assert.That(Vector4.Distance(new Vector4(-.96f,.28f,1,0),runtime.GetVector("_ForestHairParams")),Is.LessThan(.0001f));
        Sprite replacement=Sprite.Create(texture,new Rect(0,0,4,8),Vector2.one*.5f,2f);resources.Add(replacement);
        body.sprite=replacement;lighting.UpdateLighting();
        Assert.AreEqual(0,runtime.GetVector("_ForestHairParams").z);
        lighting.ReleaseLighting();Assert.AreSame(material,body.sharedMaterial);Assert.IsTrue(runtime==null);
    }

    [Test]
    public void HairCoverageRequiresAnExplicitNonEmissiveMaterialMap()
    {
        var library=ScriptableObject.CreateInstance<BattleActorFormLibrary>();var map=new Texture2D(4,8);
        resources.Add(library);resources.Add(map);
        var field=typeof(BattleActorFormLibrary).GetField("entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        field.SetValue(library,new[]{new BattleActorFormLibrary.Entry{sprite=sprite,materialMap=map,hairAlpha=true,emissiveAlpha=true}});
        Assert.IsFalse(library.UsesHairAlpha(sprite));Assert.IsTrue(library.UsesEmissiveAlpha(sprite));
        var body=CreateActor("Conflicting alpha tags",allies,Vector3.up*2f,false);lighting.Configure(allies,enemies,library);lighting.UpdateLighting();
        Assert.AreEqual(0,body.sharedMaterial.GetVector("_ForestHairParams").z);Assert.AreEqual(1,body.sharedMaterial.GetFloat("_ForestEmissionWeight"));
        field.SetValue(library,new[]{new BattleActorFormLibrary.Entry{sprite=sprite,hairAlpha=true}});
        Assert.IsFalse(library.UsesHairAlpha(sprite));Assert.IsFalse(library.UsesHairAlpha(null));
    }

    [Test]
    public void HairFlowNormalizesAuthoredDirectionsAndFallsBackForInvalidData()
    {
        var library=ScriptableObject.CreateInstance<BattleActorFormLibrary>();resources.Add(library);
        var field=typeof(BattleActorFormLibrary).GetField("entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        foreach(var direction in new[]{Vector2.zero,new Vector2(float.NaN,1),new Vector2(float.PositiveInfinity,0)})
        {
            field.SetValue(library,new[]{new BattleActorFormLibrary.Entry{sprite=sprite,hairDirection=direction}});
            Assert.AreEqual(Vector2.right,library.FindHairDirection(sprite));
        }
        field.SetValue(library,new[]{new BattleActorFormLibrary.Entry{sprite=sprite,hairDirection=new Vector2(3,4)}});
        Assert.That(Vector2.Distance(new Vector2(.6f,.8f),library.FindHairDirection(sprite)),Is.LessThan(.0001f));
        Assert.AreEqual(Vector2.right,library.FindHairDirection(null));
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
    public void SunEdgeKeywordsStayOnOwnedMaterialsAndFollowSurfaceTreatment()
    {
        SpriteRenderer body = CreateActor("Sunlit actor", allies, Vector3.up * 2, false);
        Material unrelated = new Material(material);
        resources.Add(unrelated);
        Assert.AreEqual(0f, material.GetFloat("_ForestSunEdgeStrength"));

        lighting.UpdateLighting();
        Material runtime = body.sharedMaterial;
        Assert.AreEqual(1f, runtime.GetFloat("_ForestSunEdgeStrength"));
        Assert.IsTrue(runtime.IsKeywordEnabled("_FOREST_SUN_EDGE"));
        Assert.IsFalse(material.IsKeywordEnabled("_FOREST_SUN_EDGE"));
        Assert.IsFalse(unrelated.IsKeywordEnabled("_FOREST_SUN_EDGE"));
        Assert.IsFalse(Shader.IsKeywordEnabled("_FOREST_SUN_EDGE"));

        typeof(BattleActorLightingController).GetField("surfaceWeight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(lighting, 0f);
        lighting.UpdateLighting();
        Assert.AreEqual(0f, runtime.GetFloat("_ForestSunEdgeStrength"));
        Assert.IsFalse(runtime.IsKeywordEnabled("_FOREST_SUN_EDGE"));

        lighting.ReleaseLighting();
        Assert.AreSame(material, body.sharedMaterial);
        Assert.IsTrue(runtime == null);
        Assert.AreEqual(0f, material.GetFloat("_ForestSunEdgeStrength"));
        Assert.IsFalse(material.IsKeywordEnabled("_FOREST_SUN_EDGE"));
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
