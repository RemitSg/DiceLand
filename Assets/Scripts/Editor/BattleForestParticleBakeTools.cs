using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>World-oriented leaf surfaces and shared sun response for the forest's existing particles.</summary>
public static class BattleForestParticleBakeTools
{
    public const string MeshPath = BattleLookPreviewTools.ForestAssets + "/Forest Falling Leaf.asset";

    public static Mesh CreateLeafMesh()
    {
        const int columns=4, rows=8;
        Vector3[] vertices=new Vector3[(columns+1)*(rows+1)];
        Vector2[] uv=new Vector2[vertices.Length];
        int[] triangles=new int[columns*rows*6];
        for(int y=0;y<=rows;y++)
        for(int x=0;x<=columns;x++)
        {
            float u=(float)x/columns, v=(float)y/rows;
            float arc=Mathf.Sin(v*Mathf.PI), spine=.5f+.08f*arc;
            int i=y*(columns+1)+x;
            vertices[i]=new Vector3(u-.5f,v-.5f,.12f*arc*(1-4*(u-spine)*(u-spine)));
            uv[i]=new Vector2(u,v);
        }
        int index=0;
        for(int y=0;y<rows;y++)
        for(int x=0;x<columns;x++)
        {
            int a=y*(columns+1)+x, b=a+1, c=a+columns+1, d=c+1;
            triangles[index++]=a; triangles[index++]=b; triangles[index++]=c;
            triangles[index++]=b; triangles[index++]=d; triangles[index++]=c;
        }
        Mesh mesh=new Mesh {name="Forest Falling Leaf",vertices=vertices,uv=uv,triangles=triangles};
        // The particle mesh path in this editor reads byte colors; Float32 white became dark blue.
        mesh.colors32=Enumerable.Repeat(new Color32(255,255,255,255),vertices.Length).ToArray();
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        // CPU particle batching needs the mesh's readable vertex data.
        return mesh;
    }

    public static Mesh BakeLeafMesh()
    {
        Mesh mesh=CreateLeafMesh();
        try
        {
            Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if(existing==null)
            {
                AssetDatabase.CreateAsset(mesh,MeshPath); mesh=null;
                AssetDatabase.ImportAsset(MeshPath,ImportAssetOptions.ForceSynchronousImport);
                existing=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
                if(existing==null)throw new InvalidOperationException("Cannot register the leaf mesh.");
            }
            else
            {
                EditorUtility.CopySerialized(mesh,existing);
                EditorUtility.SetDirty(existing); AssetDatabase.SaveAssetIfDirty(existing);
            }
            return existing;
        }
        finally {if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);}
    }

    public static void ConfigureMaterial(Material material,bool leaf)
    {
        material.SetFloat("_ParticleLighting",1);
        material.SetFloat("_LeafVolume",leaf?1:0);
    }

    public static void ConfigureLeaves(ParticleSystem particles,Mesh mesh)
    {
        var main=particles.main;
        main.startRotation3D=true;
        main.startRotationX=new ParticleSystem.MinMaxCurve(-.9f,.9f);
        main.startRotationY=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
        main.startRotationZ=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
        var spin=particles.rotationOverLifetime;
        spin.enabled=true; spin.separateAxes=true;
        spin.x=new ParticleSystem.MinMaxCurve(.35f,.75f);
        spin.y=new ParticleSystem.MinMaxCurve(-1.1f,1.1f);
        spin.z=new ParticleSystem.MinMaxCurve(-.9f,.9f);
        var renderer=particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode=ParticleSystemRenderMode.Mesh;
        renderer.alignment=ParticleSystemRenderSpace.World;
        renderer.mesh=mesh;
        renderer.enableGPUInstancing=false;
        var streams=new List<ParticleSystemVertexStream> {ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV};
        var current=new List<ParticleSystemVertexStream>(); renderer.GetActiveVertexStreams(current);
        if(!current.SequenceEqual(streams))renderer.SetActiveVertexStreams(streams);
    }

    [MenuItem("Tools/Battle Visuals/Apply Particle Sunlight")]
    public static void ApplyToSavedBattleScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode
            ||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).path==BattleLookPreviewTools.ScenePath))
            throw new InvalidOperationException("Exit play mode and close the battle scene before applying its particle look.");
        Mesh mesh=BakeLeafMesh();
        string path=BattleLookPreviewTools.ScenePath, before=File.ReadAllText(path);
        string copy=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/__ParticleLookCopy.scene");
        Scene original=SceneManager.GetActiveScene();
        Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        string merged=null;
        try
        {
            var particles=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParticleSystem>(true))
                .Single(p=>p.name=="Sunlit drifting leaves");
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ConfigureLeaves(particles,mesh);
            string[] ids={GlobalObjectId.GetGlobalObjectIdSlow(particles).targetObjectId.ToString(),
                GlobalObjectId.GetGlobalObjectIdSlow(particles.GetComponent<ParticleSystemRenderer>()).targetObjectId.ToString()};
            if(!EditorSceneManager.SaveScene(scene,copy,true))throw new InvalidOperationException("Cannot stage the particle scene changes.");
            merged=MergeParticleRecords(before,File.ReadAllText(copy),ids);
        }
        finally
        {
            SceneManager.SetActiveScene(original); EditorSceneManager.CloseScene(scene,true);
            AssetDatabase.DeleteAsset(copy);
        }
        if(File.ReadAllText(path)!=before)throw new InvalidOperationException("The saved battle scene changed while preparing its particle look.");
        bool bom=File.ReadAllBytes(path).Take(3).SequenceEqual(new byte[]{239,187,191});
        File.WriteAllText(path,merged,new UTF8Encoding(bom));
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        foreach(bool leaf in new[]{false,true})
        {
            string materialPath=BattleLookPreviewTools.ForestAssets+(leaf?"/Sunlit Falling Leaves.mat":"/Sunlit Dust.mat");
            Material material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null)throw new InvalidOperationException("Missing particle material: "+materialPath);
            ConfigureMaterial(material,leaf); EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            AssetDatabase.ImportAsset(materialPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material.GetFloat("_ParticleLighting")!=1||material.GetFloat("_LeafVolume")!=(leaf?1:0))
                throw new InvalidOperationException("Particle material settings did not persist: "+materialPath);
        }
    }

    // Unity may migrate unrelated serialized fields when saving. Only the two particle records are replaced.
    public static string MergeParticleRecords(string original,string generated,IEnumerable<string> ids)
    {
        var header=new Regex(@"(?m)^--- !u!\d+ &(-?\d+)[^\n]*");
        MatchCollection a=header.Matches(original), b=header.Matches(generated);
        string newline=original.Contains("\r\n")?"\r\n":"\n";
        var replacements=new Dictionary<string,string>();
        foreach(string id in ids)
        {
            var source=b.Cast<Match>().Where(m=>m.Groups[1].Value==id).ToArray();
            if(source.Length!=1||a.Cast<Match>().Count(m=>m.Groups[1].Value==id)!=1)
                throw new InvalidOperationException("A particle scene record is missing or duplicated: "+id);
            int i=b.Cast<Match>().ToList().IndexOf(source[0]);
            int end=i+1<b.Count?b[i+1].Index:generated.Length;
            replacements.Add(id,generated.Substring(source[0].Index,end-source[0].Index).Replace("\r\n","\n").Replace("\n",newline));
        }
        var result=new StringBuilder(); int start=0;
        for(int i=0;i<a.Count;i++)
        {
            string replacement;
            if(!replacements.TryGetValue(a[i].Groups[1].Value,out replacement))continue;
            int end=i+1<a.Count?a[i+1].Index:original.Length;
            result.Append(original,start,a[i].Index-start); result.Append(replacement); start=end;
        }
        result.Append(original,start,original.Length-start); return result.ToString();
    }
}
