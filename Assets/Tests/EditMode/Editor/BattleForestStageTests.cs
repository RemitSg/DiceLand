using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleForestStageTests
{
    [TestCase(true, 0.65f, 0.8f, true)]
    [TestCase(false, 0.65f, 0.8f, false)]
    [TestCase(true, 0f, 0f, true)]
    [TestCase(false, 1f, 1f, true)]
    public void DecorativeStagePreservesCombatAndRandomStateAndReleasesEveryMesh(bool groundWasEnabled, float canopyOcclusion, float sunlightMotion, bool useCookie)
    {
        GameObject root = new GameObject("Forest stage lifecycle regression");
        GameObject groundObject = new GameObject("Original ground");
        groundObject.transform.SetParent(root.transform);
        MeshRenderer ground = groundObject.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("DiceLand/Battle Forest Stone"));
        ground.sharedMaterial = material;
        ground.enabled = groundWasEnabled;
        GameObject actor = new GameObject("Combat position witness");
        actor.transform.SetParent(root.transform);
        actor.transform.position = new Vector3(-6f, 0.12f, 3f);
        Vector3 actorPosition = actor.transform.position;
        float originalLighting = Shader.GetGlobalFloat("_DiceLandForestLighting");
        Vector4 originalOpenings = Shader.GetGlobalVector("_DiceLandForestOpenings");
        Vector4 originalSunDirection = Shader.GetGlobalVector("_DiceLandForestSunDirection");
        Vector4 originalSunColor = Shader.GetGlobalVector("_DiceLandForestSunColor");
        Texture originalCookie = Shader.GetGlobalTexture("_DiceLandForestCanopyCookie");
        Vector4 originalCookieParams = Shader.GetGlobalVector("_DiceLandForestCanopyCookieParams");
        Texture2D cookie = useCookie ? new Texture2D(2,2) : null;
        Texture2D cookieWitness = new Texture2D(4,4);
        Vector4 cookieParamsWitness = new Vector4(2f,3f,4f,5f);
        Shader.SetGlobalTexture("_DiceLandForestCanopyCookie",cookieWitness);
        Shader.SetGlobalVector("_DiceLandForestCanopyCookieParams",cookieParamsWitness);
        Light originalSun = RenderSettings.sun;
        GameObject sunObject = new GameObject("Authored forest sun", typeof(Light));
        sunObject.transform.SetParent(root.transform);
        sunObject.transform.rotation = Quaternion.Euler(52f, 218f, 0f);
        Light sun = sunObject.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.8f, 0.7f, 0.6f);
        sun.intensity = 1.6f;
        RenderSettings.sun = sun;
        Vector4 sunDirectionWitness = new Vector4(1f, 2f, 3f, 4f);
        Vector4 sunColorWitness = new Vector4(5f, 6f, 7f, 8f);
        Vector4 openingWitness = new Vector4(-123f, 456f, 7f, 8f);
        Shader.SetGlobalVector("_DiceLandForestOpenings", openingWitness);
        Shader.SetGlobalVector("_DiceLandForestSunDirection", sunDirectionWitness);
        Shader.SetGlobalVector("_DiceLandForestSunColor", sunColorWitness);
        string randomState = JsonUtility.ToJson(Random.state);
        BattleForestStageController stage = root.AddComponent<BattleForestStageController>();
        stage.Configure(ground, material, canopyOcclusion, sunlightMotion, cookie);
        try
        {
            for (int cycle = 0; cycle < 2; cycle++)
            {
                stage.InitializeStage();
                stage.InitializeStage();
                MeshFilter[] parts = root.GetComponentsInChildren<MeshFilter>();
                Assert.AreEqual(2, parts.Length, "Repeated initialization must not duplicate terrain.");
                Mesh[] meshes = parts.Select(part => part.sharedMesh).ToArray();
                Assert.IsFalse(ground.enabled);
                Assert.AreEqual(new Vector4(BattleForestStageController.CanopyOpenings.x,
                    BattleForestStageController.CanopyOpenings.y, canopyOcclusion, sunlightMotion), Shader.GetGlobalVector("_DiceLandForestOpenings"));
                Vector3 sunDirection = -sun.transform.forward;
                Assert.AreEqual(new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 1f),
                    Shader.GetGlobalVector("_DiceLandForestSunDirection"));
                Color sunColor = (QualitySettings.activeColorSpace == ColorSpace.Linear ? sun.color.linear : sun.color) * sun.intensity;
                Assert.AreEqual((Vector4)sunColor, Shader.GetGlobalVector("_DiceLandForestSunColor"));
                Assert.AreSame(cookie,Shader.GetGlobalTexture("_DiceLandForestCanopyCookie"));
                Assert.AreEqual(new Vector4(0.05f,useCookie?2f:1f,18f,useCookie?1f:0f),
                    Shader.GetGlobalVector("_DiceLandForestCanopyCookieParams"));
                Assert.AreSame(material, ground.sharedMaterial);
                Assert.AreEqual(actorPosition, actor.transform.position);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
                Assert.AreEqual(randomState, JsonUtility.ToJson(Random.state));
                stage.ReleaseStage();
                stage.ReleaseStage();
                Assert.AreEqual(groundWasEnabled, ground.enabled);
                Assert.AreEqual(originalLighting, Shader.GetGlobalFloat("_DiceLandForestLighting"));
                Assert.AreEqual(openingWitness, Shader.GetGlobalVector("_DiceLandForestOpenings"));
                Assert.AreEqual(sunDirectionWitness, Shader.GetGlobalVector("_DiceLandForestSunDirection"));
                Assert.AreEqual(sunColorWitness, Shader.GetGlobalVector("_DiceLandForestSunColor"));
                Assert.AreSame(cookieWitness,Shader.GetGlobalTexture("_DiceLandForestCanopyCookie"));
                Assert.AreEqual(cookieParamsWitness,Shader.GetGlobalVector("_DiceLandForestCanopyCookieParams"));
                Assert.IsEmpty(root.GetComponentsInChildren<MeshFilter>());
                Assert.IsTrue(meshes.All(mesh => mesh == null), "Preview and scene unload must free generated meshes.");
            }
        }
        finally
        {
            stage.ReleaseStage();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(material);
            if(cookie!=null)Object.DestroyImmediate(cookie);
            Object.DestroyImmediate(cookieWitness);
            Shader.SetGlobalFloat("_DiceLandForestLighting", originalLighting);
            Shader.SetGlobalVector("_DiceLandForestOpenings", originalOpenings);
            Shader.SetGlobalVector("_DiceLandForestSunDirection", originalSunDirection);
            Shader.SetGlobalVector("_DiceLandForestSunColor", originalSunColor);
            Shader.SetGlobalTexture("_DiceLandForestCanopyCookie",originalCookie);
            Shader.SetGlobalVector("_DiceLandForestCanopyCookieParams",originalCookieParams);
            RenderSettings.sun = originalSun;
        }
    }

    [Test]
    public void WoodlandMeetsBattleTerraceWithoutAHeightGap()
    {
        GameObject root = new GameObject("Forest terrain join regression");
        MeshRenderer ground = root.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("DiceLand/Battle Forest Stone"));
        ground.sharedMaterial = material;
        BattleForestStageController stage = root.AddComponent<BattleForestStageController>();
        stage.Configure(ground, material);
        try
        {
            stage.InitializeStage();
            MeshFilter[] parts = root.GetComponentsInChildren<MeshFilter>();
            Mesh terrace = parts.Single(part => part.name == "Weathered battle terrace and steps").sharedMesh;
            Mesh woodland = parts.Single(part => part.name == "Lower forest floor").sharedMesh;
            Vector3[] terraceJoin = terrace.vertices.Where(v => Mathf.Approximately(v.z, 8.5f)).ToArray();
            Vector3[] woodlandJoin = woodland.vertices.Where(v => Mathf.Approximately(v.z, 8.5f) && v.y > -0.2f).ToArray();
            Assert.IsNotEmpty(terraceJoin);
            Assert.IsNotEmpty(woodlandJoin);
            Assert.IsTrue(terraceJoin.All(v => Mathf.Approximately(v.y, 0.045f)));
            Assert.IsTrue(woodlandJoin.All(v => Mathf.Approximately(v.y, 0.045f)));
            Assert.IsTrue(woodland.vertices.Any(v => v.z >= 15f && Mathf.Approximately(v.y, -0.08f)));
        }
        finally
        {
            stage.ReleaseStage();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(material);
        }
    }
}
