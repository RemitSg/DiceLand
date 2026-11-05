using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Capture the real receipt presenter and blueprint inventory detail using actual assets.</summary>
public static class RunItemAcquisitionVisualTools
{
    private const string OutputPath = "outputs/agent-battle/item-acquisition-20261105";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string Capture(string name,int width,int height,bool blueprint=false,bool ready=false,bool inventory=false,Rect? safe=null)
    {
        string source=MapVisualTools.Capture(name,width,height,MapBackgroundPhase.Morning,false,1,safe,(host,camera)=>
        {
            MapHUD hud=host.GetComponent<MapHUD>();
            RunState state=(RunState)typeof(MapHUD).GetField("currentRunState",Private).GetValue(hud);
            CraftingBlueprint_SO recipe=All<CraftingBlueprint_SO>().First(b=>b.blueprintID==202);
            Relic_SO relic=All<Relic_SO>().Where(r=>!string.IsNullOrWhiteSpace(r.additionalDescription)).OrderByDescending(r=>(r.description??"").Length+(r.additionalDescription??"").Length).First();
            if(ready) foreach(CraftingMaterialRequirement requirement in RunCraftingService.GetRequirements(recipe)) state.AddLoot(requirement.lootItem,requirement.amount);
            else
            {
                var first=RunCraftingService.GetRequirements(recipe).FirstOrDefault();
                if(first!=null && first.amount>1) state.AddLoot(first.lootItem,first.amount-1);
            }
            if(inventory)
            {
                state.AddPreviewedBlueprint(recipe); hud.Refresh(state,host.GetComponent<ActionPointSystem>()); hud.ShowInventoryPanel();
                ((Button)typeof(MapHUD).GetField("blueprintTabButton",Private).GetValue(hud)).onClick.Invoke();
                var slots=(System.Collections.Generic.List<GameObject>)typeof(MapHUD).GetField("inventorySlotObjects",Private).GetValue(hud);
                slots[0].GetComponent<Button>().onClick.Invoke();
                return;
            }
            if(blueprint) state.AddBlueprint(recipe,1); else state.AddRelic(relic,1);
            var presenter=host.AddComponent<RunItemAcquisitionOverlay>();
            var heading=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("1927579a4b7620b428469b73f8d5046f"));
            var body=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
            presenter.Configure(state,heading,body); presenter.ProcessPending();
            Canvas canvas=presenter.PresentationCanvas; canvas.GetComponent<CanvasScaler>().enabled=false;
            canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=camera;
            RectTransform rect=(RectTransform)canvas.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
            rect.sizeDelta=new Vector2(1080f*width/height,1080); rect.position=new Vector3(0,0,-3);
            rect.localScale=Vector3.one*(camera.orthographicSize/540f);
            presenter.DetailView.ApplyViewport(new Vector2(width,height),safe??new Rect(0,0,width,height));
            Canvas.ForceUpdateCanvases();
            foreach(TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true,true);
            foreach(Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
            {
                if(graphic is TMP_Text || graphic is TMP_SubMeshUI)continue;
                graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
        });
        Directory.CreateDirectory(OutputPath);
        string output=Path.GetFullPath(Path.Combine(OutputPath,name)); File.Copy(source,output,true); return output;
    }

    private static T[] All<T>() where T:UnityEngine.Object => AssetDatabase.FindAssets("t:"+typeof(T).Name,new[]{"Assets/Data"})
        .Select(g=>AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(value=>value!=null).ToArray();
}
