using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Packs generated, unmodified source PNGs and updates only the status icon reference fields.</summary>
public static class StatusIconBuildTools
{
    public const string AtlasPath="Assets/Resources/StatusUI/StatusIcons.png";
    public const string ManifestPath="docs/visuals/status-icons-20261105.json";
    private const int Cell=256,Columns=6,Rows=5,ArtSize=216;
    [Serializable] public sealed class Manifest { public string date; public Icon[] icons; }
    [Serializable] public sealed class Icon { public string key,name,id,color,source,prompt; public string[] statusAssets; }

    public static string Build()
    {
        Manifest manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
        if(manifest?.icons==null || manifest.icons.Length!=27) throw new InvalidDataException("Expected all 27 status icon families.");
        Directory.CreateDirectory(Path.GetDirectoryName(AtlasPath));
        Texture2D atlas=new Texture2D(Columns*Cell,Rows*Cell,TextureFormat.RGBA32,false);
        try
        {
            atlas.SetPixels32(new Color32[atlas.width*atlas.height]);
            for(int i=0;i<manifest.icons.Length;i++)
            {
                Icon icon=manifest.icons[i]; Texture2D source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                try
                {
                    if(!source.LoadImage(File.ReadAllBytes(icon.source))) throw new InvalidDataException("Invalid PNG: "+icon.source);
                    Color32[] pixels=source.GetPixels32(); int minX=source.width,maxX=-1,minY=source.height,maxY=-1;
                    for(int y=0;y<source.height;y++) for(int x=0;x<source.width;x++)
                        if(pixels[y*source.width+x].a>=20) { minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y); }
                    if(maxX<=minX || maxY<=minY) throw new InvalidDataException("Empty status icon: "+icon.key);
                    int sourceWidth=maxX-minX+1,sourceHeight=maxY-minY+1;
                    float scale=(float)ArtSize/Math.Max(sourceWidth,sourceHeight);
                    int w=Mathf.Max(1,Mathf.RoundToInt(sourceWidth*scale)),h=Mathf.Max(1,Mathf.RoundToInt(sourceHeight*scale));
                    Color32[] tile=new Color32[Cell*Cell]; int dx=(Cell-w)/2,dy=(Cell-h)/2;
                    for(int y=0;y<h;y++) for(int x=0;x<w;x++)
                        tile[(y+dy)*Cell+x+dx]=source.GetPixelBilinear((minX+(x+.5f)*sourceWidth/w)/source.width,(minY+(y+.5f)*sourceHeight/h)/source.height);
                    atlas.SetPixels32(i%Columns*Cell,(Rows-1-i/Columns)*Cell,Cell,Cell,tile);
                }
                finally { UnityEngine.Object.DestroyImmediate(source); }
            }
            atlas.Apply(); File.WriteAllBytes(AtlasPath,atlas.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(atlas); }
        AssetDatabase.ImportAsset(AtlasPath,ImportAssetOptions.ForceSynchronousImport);
        ConfigureImports(manifest);
        Dictionary<string,Sprite> sprites=AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(s=>s.name);
        var changed=new List<string>();
        foreach(Icon icon in manifest.icons)
        {
            Sprite sprite=sprites[icon.key]; string guid; long fileId;
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out guid,out fileId)) throw new InvalidDataException("Missing sprite identity: "+icon.key);
            foreach(string path in icon.statusAssets)
            {
                string before=File.ReadAllText(path);
                var field=new Regex(@"(?m)^  iconSprite:[^\r\n]*");
                if(field.Matches(before).Count!=1) throw new InvalidDataException("Expected one status icon field: "+path);
                string after=field.Replace(before,"  iconSprite: {fileID: "+fileId+", guid: "+guid+", type: 3}");
                if(before==after) continue;
                File.WriteAllText(path,after,new System.Text.UTF8Encoding(false)); changed.Add(path);
            }
        }
        foreach(string path in changed) AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        typeof(ResourceKeywordText).GetMethod("ResetOwnedAssets",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        return "Packed "+sprites.Count+" status icons; updated "+changed.Count+" icon fields.";
    }

    private static void ConfigureImports(Manifest manifest)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=100; importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
        importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.filterMode=FilterMode.Bilinear; importer.wrapMode=TextureWrapMode.Clamp;
        var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        var factories=new SpriteDataProviderFactories(); factories.Init();
        ISpriteEditorDataProvider provider=factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        Dictionary<string,SpriteRect> previous=provider.GetSpriteRects().ToDictionary(r=>r.name);
        SpriteRect[] rects=manifest.icons.Select((icon,index)=>new SpriteRect
        {
            name=icon.key,rect=new Rect(index%Columns*Cell,(Rows-1-index/Columns)*Cell,Cell,Cell),
            alignment=SpriteAlignment.Center,pivot=Vector2.one*.5f,border=Vector4.zero,
            spriteID=previous.TryGetValue(icon.key,out SpriteRect old)?old.spriteID:GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        ISpriteNameFileIdDataProvider ids=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if(ids!=null)
        {
            var oldIds=ids.GetNameFileIdPairs().ToDictionary(pair=>pair.name);
            ids.SetNameFileIdPairs(rects.Select(r=>oldIds.TryGetValue(r.name,out SpriteNameFileIdPair old)?old:new SpriteNameFileIdPair(r.name,r.spriteID)));
        }
        provider.Apply(); importer.SaveAndReimport();
    }
}
