using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>A repeatable light cookie: red leaves bend in wind; green branches stay rooted.</summary>
public static class BattleForestCanopyShadowBakeTools
{
    public const string CookiePath = BattleLookPreviewTools.ForestAssets + "/Forest Canopy Cookie.asset";

    [MenuItem("Tools/Battle Visuals/Apply Leaf Shadow Cookie")]
    public static void ApplyToSavedBattleScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode
            ||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).path==BattleLookPreviewTools.ScenePath))
            throw new InvalidOperationException("Exit play mode and close the battle scene before saving its leaf shadow cookie.");
        Texture2D cookie=BakeCookie();
        Scene original=SceneManager.GetActiveScene();
        Scene scene=EditorSceneManager.OpenScene(BattleLookPreviewTools.ScenePath,OpenSceneMode.Additive);
        try
        {
            BattleForestStageController stage=scene.GetRootGameObjects()
                .SelectMany(g=>g.GetComponentsInChildren<BattleForestStageController>(true)).Single();
            SerializedObject data=new SerializedObject(stage);
            data.FindProperty("canopyCookie").objectReferenceValue=cookie;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);
        }
    }

    public static Texture2D BakeCookie()
    {
        Texture2D map=new Texture2D(512,512,TextureFormat.RGBA32,true,true)
        {name="Forest leaf and branch light cookie",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Repeat,anisoLevel=1};
        try
        {
            map.SetPixels32(BakePixels(512));map.Apply(true,true);
            Texture2D existing=AssetDatabase.LoadAssetAtPath<Texture2D>(CookiePath);
            if(existing==null){AssetDatabase.CreateAsset(map,CookiePath);existing=map;map=null;}
            else{EditorUtility.CopySerialized(map,existing);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);}
            return existing;
        }
        finally{if(map!=null)UnityEngine.Object.DestroyImmediate(map);}
    }

    public static Color32[] BakePixels(int size)
    {
        if(size<16||size>2048)throw new ArgumentOutOfRangeException(nameof(size));
        float[] leaves=new float[size*size],branches=new float[size*size];
        var random=new System.Random(57319);
        Func<float,float,float> range=(lo,hi)=>Mathf.Lerp(lo,hi,(float)random.NextDouble());
        for(int b=0;b<24;b++)
        {
            Vector2 root=new Vector2(range(0,1),range(0,1));
            float angle=range(0,Mathf.PI*2),length=range(0.18f,0.36f),curve=range(-0.05f,0.05f);
            Vector2 axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),side=new Vector2(-axis.y,axis.x),previous=root;
            int nodes=random.Next(12,19);
            for(int n=1;n<=nodes;n++)
            {
                float t=(float)n/nodes;
                Vector2 point=root+axis*(length*t)+side*(Mathf.Sin(t*Mathf.PI)*curve);
                Segment(branches,size,previous,point,Mathf.Lerp(0.005f,0.0008f,t));previous=point;
                if(random.NextDouble()<0.15)continue;
                for(int sign=-1;sign<=1;sign+=2)
                {
                    Vector2 twig=(axis*range(0.1f,0.65f)+side*sign).normalized;
                    Vector2 end=point+twig*range(0.016f,0.046f);
                    Segment(branches,size,point,end,Mathf.Lerp(0.0016f,0.00065f,t));
                    for(int fan=-1;fan<=1;fan++)
                    {
                        float leafAngle=Mathf.Atan2(twig.y,twig.x)+fan*range(0.35f,0.7f);
                        Vector2 direction=new Vector2(Mathf.Cos(leafAngle),Mathf.Sin(leafAngle));
                        Vector2 stem=Vector2.Lerp(point,end,range(0.45f,1f));
                        Leaf(leaves,size,stem,direction,range(0.018f,0.044f),range(0.003f,0.007f));
                    }
                }
            }
        }
        var pixels=new Color32[size*size];
        for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(leaves[i],branches[i],0,1);
        return pixels;
    }

    private static void Leaf(float[] pixels,int size,Vector2 stem,Vector2 axis,float length,float width)
    {
        Vector2 centre=stem+axis*(length*0.5f),side=new Vector2(-axis.y,axis.x);
        Raster(pixels,size,centre,length*0.5f+width,(point,aa)=>
        {
            Vector2 local=point-centre;
            float u=Vector2.Dot(local,axis)/(length*0.5f),v=Mathf.Abs(Vector2.Dot(local,side));
            float profile=width*Mathf.Max(0,1-u*u);
            float ends=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1-aa/(length*0.5f),1+aa/(length*0.5f),Mathf.Abs(u)));
            return ends*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(profile-aa,profile+aa,v)));
        });
    }

    private static void Segment(float[] pixels,int size,Vector2 start,Vector2 end,float width)
    {
        Vector2 axis=end-start;
        Raster(pixels,size,(start+end)*0.5f,axis.magnitude*0.5f+width,(point,aa)=>
        {
            float along=Mathf.Clamp01(Vector2.Dot(point-start,axis)/Mathf.Max(0.000001f,axis.sqrMagnitude));
            float distance=(point-Vector2.Lerp(start,end,along)).magnitude;
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width-aa,width+aa,distance));
        });
    }

    private static void Raster(float[] pixels,int size,Vector2 centre,float radius,Func<Vector2,float,float> coverage)
    {
        float aa=0.65f/size;radius+=aa;
        int x0=Mathf.FloorToInt((centre.x-radius)*size),x1=Mathf.CeilToInt((centre.x+radius)*size);
        int y0=Mathf.FloorToInt((centre.y-radius)*size),y1=Mathf.CeilToInt((centre.y+radius)*size);
        for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
        {
            // Rasterize across the boundary before wrapping; both sides share the same shape.
            int index=((y%size+size)%size)*size+(x%size+size)%size;
            float value=coverage(new Vector2((x+0.5f)/size,(y+0.5f)/size),aa);
            pixels[index]=Mathf.Max(pixels[index],value);
        }
    }
}
