using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authored curves and thin-leaf regions for the existing canopy illustration.</summary>
public static class BattleForestCanopyBakeTools
{
    public const string MapPath = BattleLookPreviewTools.ForestAssets + "/Ancient Canopy Response.asset";
    public const string MaterialPath = BattleLookPreviewTools.ForestAssets + "/Ancient Canopy Light.mat";
    public const string MapProperty = "_CanopyResponseMap";
    private const int LongEdge = 512;
    private static readonly Vector2 Reference = new Vector2(1672,941);

    private struct Curve
    {
        public Vector2 start,end,radius;
        public Curve(float x,float y,float endX,float endY,float rx,float ry)
        {
            start=new Vector2(x,y);end=new Vector2(endX,endY);radius=new Vector2(rx,ry);
        }
    }

    private static readonly Curve[] Bark =
    {
        new Curve(582,723,503,611,100,100),new Curve(486,619,480,490,110,105),
        new Curve(480,501,510,390,95,105),new Curve(510,429,630,349,72,68),
        new Curve(620,348,724,267,60,55),new Curve(465,377,300,335,55,48),
        new Curve(462,374,395,241,45,60),new Curve(1360,833,1291,730,80,100),
        new Curve(1290,755,1252,650,90,90),new Curve(1253,647,1240,546,85,95),
        new Curve(1241,553,1254,465,72,90),new Curve(1264,488,1400,430,62,55),
        new Curve(1237,503,1126,455,55,45),new Curve(1235,489,1161,382,50,55)
    };
    private static readonly Curve[] Leaves =
    {
        new Curve(370,185,370,185,350,170),new Curve(650,120,650,120,230,150),
        new Curve(180,320,180,320,200,140),new Curve(810,250,810,250,230,170),
        new Curve(944,345,944,345,210,145),new Curve(1245,210,1245,210,290,170),
        new Curve(1490,392,1490,392,190,160)
    };

    [MenuItem("Tools/Battle Visuals/Apply Canopy Light Response")]
    public static void ApplyToSavedBattleScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit play mode before saving the canopy response.");
        if (Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).path==BattleLookPreviewTools.ScenePath))
            throw new InvalidOperationException("Close the battle scene before applying its canopy response; unsaved changes are never replaced.");
        Scene original=SceneManager.GetActiveScene();
        Material source=AssetDatabase.LoadAssetAtPath<Material>(BattleLookPreviewTools.ForestAssets+"/Forest Foliage.mat");
        Material material=ConfigureMaterial(source);
        Scene scene=EditorSceneManager.OpenScene(BattleLookPreviewTools.ScenePath,OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            SpriteRenderer tree=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true))
                .Single(r=>r.name=="Ancient tree and ruined terrace");
            if (AssetDatabase.GetAssetPath(tree.sprite)!=BattleLookPreviewTools.MidgroundPath)
                throw new InvalidOperationException("The canopy response belongs to the authored ancient-tree sprite.");
            tree.sharedMaterial=material;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);
        }
    }

    public static Material ConfigureMaterial(Material source)
    {
        if (source==null) throw new ArgumentNullException(nameof(source));
        Texture2D map=BakeMap();
        Material material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material==null)
        {
            material=new Material(source){name="Ancient Canopy Light"};AssetDatabase.CreateAsset(material,MaterialPath);
        }
        material.CopyPropertiesFromMaterial(source);
        material.name="Ancient Canopy Light";
        material.SetTexture(MapProperty,map);material.SetFloat("_CanopyResponse",1f);
        material.SetFloat("_ResponseWeight",1f);material.SetFloat("_BarkLightGain",1.2f);material.SetFloat("_LeafTransmission",0.7f);
        material.SetFloat("_LocalWindStrength",0.85f);
        material.EnableKeyword("_CANOPY_LIGHT_RESPONSE");
        EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    public static Texture2D BakeMap()
    {
        // This dedicated material uses the original full-sprite UVs. Atlas UVs need a paired atlas.
        Sprite sprite=AssetDatabase.LoadAssetAtPath<Sprite>(BattleLookPreviewTools.MidgroundPath);
        if(sprite==null||sprite.packed||sprite.rect.x!=0f||sprite.rect.y!=0f
            ||sprite.rect.width!=sprite.texture.width||sprite.rect.height!=sprite.texture.height)
            throw new InvalidOperationException("The canopy response requires the unpacked full ancient-tree sprite.");
        Texture2D source=new Texture2D(2,2,TextureFormat.RGBA32,false,true),map=null;
        try
        {
            if (!ImageConversion.LoadImage(source,File.ReadAllBytes(BattleLookPreviewTools.MidgroundPath),false))
                throw new InvalidOperationException("Cannot read the authored canopy silhouette.");
            float scale=(float)LongEdge/Mathf.Max(source.width,source.height);
            int width=Mathf.RoundToInt(source.width*scale),height=Mathf.RoundToInt(source.height*scale);
            bool[] inside=new bool[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                inside[y*width+x]=source.GetPixelBilinear((x+0.5f)/width,(y+0.5f)/height).a>=0.5f;
            map=new Texture2D(width,height,TextureFormat.RGBA32,true,true)
            {name="Ancient canopy curve and leaf response",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp,anisoLevel=1};
            map.SetPixels32(BakePixels(inside,width,height));map.Apply(true,true);
            Texture2D existing=AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
            if(existing==null){AssetDatabase.CreateAsset(map,MapPath);existing=map;map=null;}
            else{EditorUtility.CopySerialized(map,existing);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);}
            return existing;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);if(map!=null)UnityEngine.Object.DestroyImmediate(map);
        }
    }

    public static Color32[] BakePixels(bool[] inside,int width,int height)
    {
        if(inside==null||width<2||height<2||inside.Length!=width*height)
            throw new ArgumentException("A complete canopy mask of at least 2x2 is required.");
        float[] distance=new float[inside.Length];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            distance[y*width+x]=inside[y*width+x]?Mathf.Min(x+1,y+1,width-x,height-y):0f;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int i=y*width+x;if(x>0)distance[i]=Mathf.Min(distance[i],distance[i-1]+1);
            if(y>0)distance[i]=Mathf.Min(distance[i],distance[i-width]+1);
        }
        for(int y=height-1;y>=0;y--)for(int x=width-1;x>=0;x--)
        {
            int i=y*width+x;if(x<width-1)distance[i]=Mathf.Min(distance[i],distance[i+1]+1);
            if(y<height-1)distance[i]=Mathf.Min(distance[i],distance[i+width]+1);
        }
        Color32[] pixels=new Color32[inside.Length];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int i=y*width+x;
            if(!inside[i]){pixels[i]=new Color32(128,128,0,0);continue;}
            Vector2 point=new Vector2((x+0.5f)/width*Reference.x,(1f-(y+0.5f)/height)*Reference.y);
            Vector3 bark=Vector3.zero,leaf=Vector3.zero;float barkSum=0f,leafSum=0f,barkWeight=0f,leafWeight=0f;
            Accumulate(point,Bark,ref bark,ref barkSum,ref barkWeight);
            Accumulate(point,Leaves,ref leaf,ref leafSum,ref leafWeight);
            float baseFade=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(point.x<900f?700f:850f,point.x<900f?750f:900f,point.y));
            bark*=baseFade;barkWeight*=baseFade;
            float leafRegion=leafWeight*(1f-barkWeight);
            Vector3 sum=bark+(leafSum>0f?leaf/leafSum*leafRegion:Vector3.zero);
            Vector3 normal=sum.sqrMagnitude>0.00001f?sum.normalized:Vector3.forward;
            float edge=Mathf.Exp(-distance[i]*512f/width/4f);
            float coverage=Mathf.Max(barkWeight,leafRegion*(0.35f+0.65f*edge));
            pixels[i]=new Color(normal.x*0.5f+0.5f,normal.y*0.5f+0.5f,leafRegion,coverage);
        }
        return pixels;
    }

    private static void Accumulate(Vector2 point,Curve[] curves,ref Vector3 sum,ref float total,ref float strongest)
    {
        foreach(Curve curve in curves)
        {
            Vector2 scale=new Vector2(1f/curve.radius.x,1f/curve.radius.y);
            Vector2 start=Vector2.Scale(curve.start,scale),end=Vector2.Scale(curve.end,scale);
            Vector2 local=Vector2.Scale(point,scale),axis=end-start;
            float along=Mathf.Clamp01(Vector2.Dot(local-start,axis)/Mathf.Max(0.00001f,axis.sqrMagnitude));
            Vector2 offset=local-Vector2.Lerp(start,end,along);float length=offset.magnitude;
            float weight=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(0.72f,1.16f,length));
            if(weight<=0f)continue;
            Vector3 normal=new Vector3(offset.x*0.82f,-offset.y*0.82f,Mathf.Sqrt(Mathf.Max(0.08f,1f-length*length*0.65f))).normalized;
            sum+=normal*weight;total+=weight;strongest=Mathf.Max(strongest,weight);
        }
    }
}
