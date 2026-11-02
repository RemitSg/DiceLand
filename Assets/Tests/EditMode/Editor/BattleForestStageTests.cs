using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleForestStageTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void DecorativeStagePreservesCombatAndRandomStateAndReleasesEveryMesh(bool groundWasEnabled)
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
        Vector4 openingWitness = new Vector4(-123f, 456f, 7f, 8f);
        Shader.SetGlobalVector("_DiceLandForestOpenings", openingWitness);
        string randomState = JsonUtility.ToJson(Random.state);
        BattleForestStageController stage = root.AddComponent<BattleForestStageController>();
        stage.Configure(ground, material);
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
                    BattleForestStageController.CanopyOpenings.y, 0f, 0f), Shader.GetGlobalVector("_DiceLandForestOpenings"));
                Assert.AreSame(material, ground.sharedMaterial);
                Assert.AreEqual(actorPosition, actor.transform.position);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
                Assert.AreEqual(randomState, JsonUtility.ToJson(Random.state));
                stage.ReleaseStage();
                stage.ReleaseStage();
                Assert.AreEqual(groundWasEnabled, ground.enabled);
                Assert.AreEqual(originalLighting, Shader.GetGlobalFloat("_DiceLandForestLighting"));
                Assert.AreEqual(openingWitness, Shader.GetGlobalVector("_DiceLandForestOpenings"));
                Assert.IsEmpty(root.GetComponentsInChildren<MeshFilter>());
                Assert.IsTrue(meshes.All(mesh => mesh == null), "Preview and scene unload must free generated meshes.");
            }
        }
        finally
        {
            stage.ReleaseStage();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(material);
            Shader.SetGlobalFloat("_DiceLandForestLighting", originalLighting);
            Shader.SetGlobalVector("_DiceLandForestOpenings", originalOpenings);
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
