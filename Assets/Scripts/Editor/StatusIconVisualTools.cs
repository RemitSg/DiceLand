using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Actual imported sprites and text meshes, rendered in disposable production UI previews.</summary>
public static class StatusIconVisualTools
{
    public const string Output="outputs/agent-battle/status-icons-20261105";
    private static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
    private static readonly Color Ivory=new Color32(242,232,203,255),Gold=new Color32(228,188,111,255);

    public static string CaptureOverview()
    {
        return Persist(MapVisualTools.Capture("status-icons-overview.png",2340,1080,MapBackgroundPhase.Morning,false,1,null,(host,camera)=>
        {
            Canvas canvas=MakeCanvas(host,camera,"StatusOverview"); RectTransform root=(RectTransform)canvas.transform;
            RectTransform dark=Rect("Backdrop",root,-1170,540,2340,1080); dark.gameObject.AddComponent<Image>().color=new Color(.005f,.013f,.008f,.6f);
            RectTransform panel=Rect("StatusPanel",root,-1060,455,2120,910); Face(panel,MapCraftingFrameGraphic.Style.Window);
            Label(panel,"Title","战斗状态图标",54,Gold,40,28,2040,70,false);
            Label(panel,"Subtitle","27 种状态 · 临时版本沿用同组图标",26,Ivory,40,98,2040,36,false);
            var manifest=JsonUtility.FromJson<StatusIconBuildTools.Manifest>(File.ReadAllText(StatusIconBuildTools.ManifestPath));
            for(int i=0;i<manifest.icons.Length;i++)
            {
                var entry=manifest.icons[i]; RectTransform tile=Rect(entry.key,panel,32+i%9*228,146+i/9*171,220,155);
                Face(tile,MapCraftingFrameGraphic.Style.Material);
                Image icon=Rect("Icon",tile,62,5,96,96).gameObject.AddComponent<Image>(); icon.sprite=ResourceKeywordText.IconFor(entry.name); icon.preserveAspect=true; icon.raycastTarget=false;
                Label(tile,"Name",entry.name,27,ResourceKeywordText.ColorFor(entry.name,Ivory),7,109,206,37,false);
            }
            RectTransform sample=Rect("InlineSamples",panel,38,674,2044,185); Face(sample,MapCraftingFrameGraphic.Style.Material);
            Label(sample,"SkillExample","技能示例 · 命中时施加 2 层燃烧、1 层电流与寒冷。",32,Ivory,28,20,1988,62,true);
            Label(sample,"RelicExample","遗物示例 · 获得临时强壮和护盾；金币、生命值与电属性图标可同列显示。",32,Ivory,28,94,1988,62,true);
            Force(canvas);
        }),"status-icons-overview.png");
    }

    public static string CaptureRelic()
    {
        return Persist(MapVisualTools.Capture("status-keywords-relic.png",2340,1080,MapBackgroundPhase.Morning,false,1,null,(host,camera)=>
        {
            Canvas canvas=MakeCanvas(host,camera,"StatusRelicPreview");
            Relic_SO relic=AssetDatabase.FindAssets("t:Relic_SO",new[]{"Assets/Data/Relics"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<Relic_SO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r=>r.description!=null&&r.description.Contains("燃烧"))
                .OrderBy(r=>r.description.Length).First();
            MapInventoryDetailView view=MapInventoryDetailView.Create((RectTransform)canvas.transform,"StatusRelicDetail");
            view.ConfigureFonts(Font,Font); view.Show(new RunRelicState{relicData=relic,amount=1});
            view.ApplyViewport(new Vector2(2340,1080),new Rect(0,0,2340,1080)); Force(canvas);
        }),"status-keywords-relic.png");
    }

    public static string CaptureSkill() => Persist(BattleSkillVisualTools.Capture("status-keywords-skill.png",2340,1080,1,null,true),"status-keywords-skill.png");

    private static Canvas MakeCanvas(GameObject host,Camera camera,string name)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Canvas)); go.transform.SetParent(host.transform,false);
        Canvas canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=camera; canvas.sortingOrder=9500;
        RectTransform rect=(RectTransform)go.transform; rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.sizeDelta=new Vector2(2340,1080); rect.position=new Vector3(0,0,-3); rect.localScale=Vector3.one*(camera.orthographicSize/540f);
        return canvas;
    }

    private static RectTransform Rect(string name,RectTransform parent,float x,float y,float width,float height)
    {
        RectTransform rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
        // The canvas uses center coordinates; each panel uses its top-left corner.
        rect.anchorMin=rect.anchorMax=parent.GetComponent<Canvas>()!=null?Vector2.one*.5f:new Vector2(0,1);
        rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,parent.GetComponent<Canvas>()!=null?y:-y); rect.sizeDelta=new Vector2(width,height); return rect;
    }
    private static void Face(RectTransform target,MapCraftingFrameGraphic.Style style) { var face=target.gameObject.AddComponent<MapCraftingFrameGraphic>(); face.craftingStyle=style; face.raycastTarget=false; }
    private static void Label(RectTransform parent,string name,string value,float size,Color color,float x,float y,float width,float height,bool inline)
    {
        TextMeshProUGUI text=Rect(name,parent,x,y,width,height).gameObject.AddComponent<TextMeshProUGUI>();
        text.font=Font; text.fontSize=size; text.color=color; text.text=value; text.raycastTarget=false;
        text.alignment=inline?TextAlignmentOptions.MidlineLeft:TextAlignmentOptions.Center;
        if(inline) ResourceKeywordText.Attach(text);
    }
    private static void Force(Canvas canvas)
    {
        Canvas.ForceUpdateCanvases();
        foreach(TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true,true);
        foreach(Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
        { if(graphic is TMP_Text||graphic is TMP_SubMeshUI) continue; graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); }
        Canvas.ForceUpdateCanvases();
    }
    private static string Persist(string source,string name)
    { Directory.CreateDirectory(Output); string path=Path.GetFullPath(Path.Combine(Output,name)); File.Copy(source,path,true); return path; }
}
