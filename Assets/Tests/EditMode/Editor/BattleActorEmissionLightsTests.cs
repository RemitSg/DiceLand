using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BattleActorEmissionLightsTests
{
    private GameObject root;
    private SpriteRenderer source;
    private Sprite sprite, replacement;
    private Texture2D texture;
    private Material material, external;
    private BattleActorFormLibrary library;
    private float previousForest;
    private readonly BattleActorEmissionLights emission = new BattleActorEmissionLights();

    [SetUp]
    public void SetUp()
    {
        previousForest=Shader.GetGlobalFloat("_DiceLandForestLighting");Shader.SetGlobalFloat("_DiceLandForestLighting",1);
        root=new GameObject("Emission fixture");texture=new Texture2D(4,8);
        sprite=Sprite.Create(texture,new Rect(0,0,4,8),new Vector2(.25f,.75f),2,0,SpriteMeshType.FullRect);
        source=new GameObject("Emissive actor").AddComponent<SpriteRenderer>();source.transform.SetParent(root.transform);
        material=new Material(Shader.Find("Assets/Shaders/Lit"));source.sharedMaterial=material;source.sprite=sprite;
        library=ScriptableObject.CreateInstance<BattleActorFormLibrary>();
        var serialized=new SerializedObject(library);var entries=serialized.FindProperty("entries");entries.arraySize=1;
        var entry=entries.GetArrayElementAtIndex(0);entry.FindPropertyRelative("sprite").objectReferenceValue=sprite;
        entry.FindPropertyRelative("materialMap").objectReferenceValue=texture;entry.FindPropertyRelative("emissiveAlpha").boolValue=true;
        BattleActorFormBakeTools.WriteEmissionLights(entry,new[]{Profile(.35f,.65f),Profile(.6f,.1f)});
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static BattleActorFormLibrary.EmissionLight Profile(float x,float y) =>
        new BattleActorFormLibrary.EmissionLight { uv=new Vector2(x,y),color=new Color(1,.1f,.4f),range=.6f,intensity=.1f };

    [TearDown]
    public void TearDown()
    {
        emission.Release();if(root!=null)Object.DestroyImmediate(root);
        Object.DestroyImmediate(library);Object.DestroyImmediate(sprite);if(replacement!=null)Object.DestroyImmediate(replacement);
        Object.DestroyImmediate(texture);Object.DestroyImmediate(material);if(external!=null)Object.DestroyImmediate(external);
        Shader.SetGlobalFloat("_DiceLandForestLighting",previousForest);
    }

    [Test]
    public void AuthoredUvTracksPivotFlipsRotationAndMirroredScaleWithoutChangingTheSource()
    {
        source.transform.SetPositionAndRotation(new Vector3(3,4,7),Quaternion.Euler(0,180,0));
        source.transform.localScale=new Vector3(-1.5f,2,.75f);source.flipX=true;source.flipY=true;
        source.color=new Color(.5f,.75f,1,.6f);Vector2[] vertices=sprite.vertices;
        emission.Update(source,library,root.transform,.5f,.8f);Light[] lights=root.GetComponentsInChildren<Light>();
        Assert.AreEqual(2,lights.Length);float height=source.bounds.size.y;
        Vector2 pixel=Vector2.Scale(sprite.rect.size,new Vector2(.35f,.65f))-sprite.pivot;
        Vector3 local=pixel/sprite.pixelsPerUnit;local.x=-local.x;local.y=-local.y;
        Vector3 expected=source.transform.TransformPoint(local);expected.z-=height*.08f;
        Assert.That(Vector3.Distance(expected,lights[0].transform.position),Is.LessThan(.00001f));
        Assert.That(lights[0].range,Is.EqualTo(height*.6f).Within(.00001f));
        Assert.That(lights[0].intensity,Is.EqualTo(.1f*height*height*.5f*.6f*.8f).Within(.00001f));
        Assert.AreEqual(new Color(.5f,.075f,.4f,1),lights[0].color);
        Assert.AreEqual(LightType.Point,lights[0].type);Assert.AreEqual(LightShadows.None,lights[0].shadows);
        Assert.AreEqual(LightRenderMode.ForcePixel,lights[0].renderMode);
        Assert.AreSame(sprite,source.sprite);Assert.AreSame(material,source.sharedMaterial);CollectionAssert.AreEqual(vertices,sprite.vertices);
    }

    [Test]
    public void HiddenFadedAndInactiveSourcesStopIlluminatingAndReuseTheirOwnedLights()
    {
        emission.Update(source,library,root.transform,1,1);Light first=root.GetComponentsInChildren<Light>()[0];
        source.forceRenderingOff=true;emission.Update(source,library,root.transform,1,1);Assert.IsFalse(first.enabled);
        source.forceRenderingOff=false;source.enabled=false;emission.Update(source,library,root.transform,1,1);Assert.IsFalse(first.enabled);
        source.enabled=true;source.color=Color.clear;emission.Update(source,library,root.transform,1,1);Assert.IsFalse(first.enabled);
        source.color=Color.white;source.gameObject.SetActive(false);emission.Update(source,library,root.transform,1,1);Assert.IsFalse(first.enabled);
        source.gameObject.SetActive(true);emission.Update(source,library,root.transform,0,1);Assert.IsFalse(first.enabled);
        source.transform.position+=Vector3.right*3;emission.Update(source,library,root.transform,1,1);
        Assert.IsTrue(first.enabled);Assert.AreSame(first,root.GetComponentsInChildren<Light>()[0]);Assert.AreEqual(2,root.GetComponentsInChildren<Light>().Length);
    }

    [Test]
    public void UnknownSpritesAndMissingEmissionOptInRemoveThePreviousLightProxies()
    {
        emission.Update(source,library,root.transform,1,1);
        replacement=Sprite.Create(texture,new Rect(0,0,4,4),Vector2.one*.5f,2);source.sprite=replacement;
        emission.Update(source,library,root.transform,1,1);Assert.AreEqual(0,root.GetComponentsInChildren<Light>().Length);
        source.sprite=sprite;var serialized=new SerializedObject(library);
        serialized.FindProperty("entries").GetArrayElementAtIndex(0).FindPropertyRelative("emissiveAlpha").boolValue=false;
        serialized.ApplyModifiedPropertiesWithoutUndo();emission.Update(source,library,root.transform,1,1);
        Assert.AreEqual(0,root.GetComponentsInChildren<Light>().Length);Assert.AreEqual(2,library.FindEmissionLights(sprite).Length);
    }

    [Test]
    public void MaterialOverridesAndRepeatedReleaseLeaveNoLightOrMaterialOwnershipBehind()
    {
        BattleActorLightingController lighting=root.AddComponent<BattleActorLightingController>();lighting.Configure(root.transform,null,library);
        lighting.UpdateLighting();Assert.AreEqual(2,root.GetComponentsInChildren<Light>().Length);
        external=new Material(material);source.sharedMaterial=external;lighting.UpdateLighting();
        foreach(Light light in root.GetComponentsInChildren<Light>())Assert.IsFalse(light.enabled);
        lighting.ReleaseLighting();lighting.ReleaseLighting();Assert.AreSame(external,source.sharedMaterial);
        Assert.AreEqual(0,root.GetComponentsInChildren<Light>().Length);
        var serialized=new SerializedObject(library);var entry=serialized.FindProperty("entries").GetArrayElementAtIndex(0);
        BattleActorFormBakeTools.WriteEmissionLights(entry,new[]{Profile(.2f,.3f),Profile(.4f,.5f),Profile(.6f,.7f)});
        serialized.ApplyModifiedPropertiesWithoutUndo();emission.Update(source,library,root.transform,1,1);
        Assert.AreEqual(2,root.GetComponentsInChildren<Light>().Length);Assert.AreEqual(3,library.FindEmissionLights(sprite).Length);
        emission.Release();emission.Release();Assert.AreEqual(0,root.GetComponentsInChildren<Light>().Length);
    }
}
