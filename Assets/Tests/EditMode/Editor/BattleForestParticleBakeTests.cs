using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Random=UnityEngine.Random;

public sealed class BattleForestParticleBakeTests
{
    [Test]
    public void CurvedLeafKeepsReadableByteColorsAndStableGeometryWithoutBattleRandomness()
    {
        string random=JsonUtility.ToJson(Random.state);
        Mesh a=BattleForestParticleBakeTools.CreateLeafMesh(),b=BattleForestParticleBakeTools.CreateLeafMesh();
        try
        {
            Assert.AreEqual(45,a.vertexCount); Assert.AreEqual(192,a.triangles.Length);
            Assert.IsTrue(a.isReadable);
            Assert.AreEqual(VertexAttributeFormat.UNorm8,a.GetVertexAttributeFormat(VertexAttribute.Color));
            Assert.IsTrue(a.colors32.All(c=>c.Equals(new Color32(255,255,255,255))));
            Assert.Greater(a.vertices.Max(v=>v.z)-a.vertices.Min(v=>v.z),.1f);
            Assert.IsTrue(a.normals.All(n=>Mathf.Abs(n.magnitude-1)<.001f&&n.z>.5f));
            Assert.IsTrue(a.uv.All(v=>v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1));
            CollectionAssert.AreEqual(a.vertices,b.vertices);CollectionAssert.AreEqual(a.normals,b.normals);
            CollectionAssert.AreEqual(a.triangles,b.triangles);CollectionAssert.AreEqual(a.colors32,b.colors32);
            Assert.AreEqual(random,JsonUtility.ToJson(Random.state));
        }
        finally {UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
    }

    [Test]
    public void ParticleMeshPreservesLifetimeColorAndDoesNotTurnWithTheCamera()
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)
            Assert.Ignore("Particle render buffers require a graphics device; run this case in the graphics validation project.");
        GameObject root=new GameObject("Leaf particle test");
        Mesh blade=BattleForestParticleBakeTools.CreateLeafMesh(),a=new Mesh(),b=new Mesh();
        Material material=null;RenderTexture target=null;
        try
        {
            var particles=root.AddComponent<ParticleSystem>();
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed=false;particles.randomSeed=12345;
            var main=particles.main;main.startSpeed=0;main.startSize=.3f;main.startLifetime=10;
            main.playOnAwake=false;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=particles.emission;emission.enabled=false;
            BattleForestParticleBakeTools.ConfigureLeaves(particles,blade);
            Assert.IsTrue(main.startRotation3D);Assert.IsTrue(particles.rotationOverLifetime.separateAxes);
            var renderer=particles.GetComponent<ParticleSystemRenderer>();
            material=new Material(Shader.Find("DiceLand/Battle Forest Atmosphere"));
            material.SetFloat("_Mode",3);material.SetFloat("_LeafVolume",1);renderer.sharedMaterial=material;
            root.layer=31;
            Assert.AreEqual(ParticleSystemRenderSpace.World,renderer.alignment);
            Assert.IsFalse(renderer.enableGPUInstancing);
            var c1=new GameObject("Camera A");c1.transform.SetParent(root.transform);var cameraA=c1.AddComponent<Camera>();cameraA.enabled=false;c1.transform.position=new Vector3(0,0,-5);c1.transform.LookAt(Vector3.zero);
            var c2=new GameObject("Camera B");c2.transform.SetParent(root.transform);var cameraB=c2.AddComponent<Camera>();cameraB.enabled=false;c2.transform.position=new Vector3(4,2,-3);c2.transform.LookAt(Vector3.zero);
            particles.Play(false);
            particles.Emit(new ParticleSystem.EmitParams {position=Vector3.zero,startColor=new Color32(255,255,255,200),startSize=.3f,startLifetime=10,rotation3D=new Vector3(25,40,30)},1);
            particles.Simulate(.01f,false,false,false);
            particles.Pause(false);
            Assert.AreEqual(1,particles.particleCount,"The render fixture must contain its emitted leaf.");
            target=new RenderTexture(64,64,24);target.Create();cameraA.targetTexture=target;cameraA.cullingMask=1<<31;
            cameraA.Render();
            renderer.BakeMesh(a,cameraA,ParticleSystemBakeMeshOptions.BakeRotationAndScale);
            renderer.BakeMesh(b,cameraB,ParticleSystemBakeMeshOptions.BakeRotationAndScale);
            Assert.AreEqual(45,a.vertexCount);
            CollectionAssert.AreEqual(a.vertices,b.vertices);
            Assert.AreEqual(a.vertexCount,a.colors.Length);
            Assert.IsTrue(a.colors.All(c=>c.r>.98f&&c.g>.98f&&c.b>.98f&&Mathf.Abs(c.a-200f/255)<.01f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(blade);
            UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);
            if(material!=null)UnityEngine.Object.DestroyImmediate(material);
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
    }

    [Test]
    public void ScenePatchKeepsUnrelatedRecordsAndOriginalLineEndings()
    {
        string source="%YAML 1.1\r\n--- !u!1 &10\r\nUnrelated: keep  \r\n--- !u!198 &20\r\nParticle: before\r\n--- !u!199 &30\r\nRenderer: before\r\n--- !u!1 &40\r\nOther: keep\r\n";
        string generated="%YAML 1.1\n--- !u!1 &10\nUnrelated: changed\n--- !u!198 &20\nParticle: after\n--- !u!199 &30\nRenderer: after\n--- !u!1 &40\nOther: changed\n";
        string actual=BattleForestParticleBakeTools.MergeParticleRecords(source,generated,new[]{"20","30"});
        Assert.AreEqual(source.Replace("Particle: before","Particle: after").Replace("Renderer: before","Renderer: after"),actual);
        Assert.Throws<InvalidOperationException>(()=>BattleForestParticleBakeTools.MergeParticleRecords(source,generated,new[]{"99"}));
        Assert.Throws<InvalidOperationException>(()=>BattleForestParticleBakeTools.MergeParticleRecords(source,generated+"--- !u!198 &20\nDuplicate\n",new[]{"20"}));
    }
}
