using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSettingsVisualTools
{
    private const string Output="outputs/agent-battle/settings-ui-20261105";
    public static string Capture(string name,int width,int height,bool dropdownOpen=false,Rect? safe=null)
    {
        Scene original=SceneManager.GetActiveScene(),preview=EditorSceneManager.NewPreviewScene();
        RenderTexture previous=RenderTexture.active,target=null; Texture2D pixels=null; GameObject host=null;
        Dropdown openedDropdown=null; GameObject selection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
        try
        {
            host=new GameObject("SettingsVisualPreview"); SceneManager.MoveGameObjectToScene(host,preview);
            Font font=AssetDatabase.LoadAssetAtPath<Font>(MainMenuVisualTools.FontPath);
            MainMenuForestView forest=host.AddComponent<MainMenuForestView>();
            forest.Build(font,new Dictionary<MainMenuSceneController.MenuAction,string>(),Array.Empty<DifficultyConfig_SO>(),null,null);
            forest.SetExternalPageVisible(true);
            MainMenuSettingsOverlay settings=host.AddComponent<MainMenuSettingsOverlay>(); settings.ConfigureForestFont(font);
            settings.Show(new MainMenuSettingsData { musicVolume=.72f,sfxVolume=.85f,isFullscreen=true,vibrationEnabled=true,qualityLevel=Mathf.Min(1,QualitySettings.names.Length-1),resolutionWidth=width,resolutionHeight=height,targetFrameRate=120 },null);
            SceneManager.MoveGameObjectToScene(settings.PresentationCanvas.gameObject,preview);
            target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
            var cameraObject=new GameObject("SettingsPreviewCamera",typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject,preview);
            var lightObject=new GameObject("PreviewLight",typeof(Light)); SceneManager.MoveGameObjectToScene(lightObject,preview); lightObject.GetComponent<Light>().type=LightType.Directional;
            Camera camera=cameraObject.GetComponent<Camera>(); camera.scene=preview; camera.transform.position=new Vector3(0,0,-1500);
            camera.orthographic=true; camera.orthographicSize=540; camera.farClipPlane=3000; camera.backgroundColor=new Color(.01f,.025f,.02f); camera.clearFlags=CameraClearFlags.SolidColor; camera.targetTexture=target;
            foreach(Canvas canvas in new[]{forest.Canvas,settings.PresentationCanvas})
            {
                canvas.GetComponent<CanvasScaler>().enabled=false; canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=camera;
                RectTransform rect=(RectTransform)canvas.transform; rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
                rect.sizeDelta=new Vector2(1080f*width/height,1080); rect.position=new Vector3(0,0,canvas==forest.Canvas?0:-10);
            }
            Rect safePixels=safe??new Rect(0,0,width,height);
            forest.ApplyLayout(new Vector2(width,height),safePixels); settings.ApplyViewport(new Vector2(width,height),safePixels);
            if(dropdownOpen)
            {
                openedDropdown=(Dropdown)typeof(MainMenuSettingsOverlay).GetField("frameRateDropdown",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(settings);
                if(!Application.isPlaying)
                {
                    // Native Dropdown initializes its tween runner in Start, which EditMode previews do not run.
                    typeof(Dropdown).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(openedDropdown,null);
                    openedDropdown.alphaFadeSpeed=0;
                }
                openedDropdown.Show();
                foreach(CanvasGroup group in openedDropdown.GetComponentsInChildren<CanvasGroup>(true)) group.alpha=1;
            }
            Canvas.ForceUpdateCanvases();
            foreach(Graphic graphic in settings.PresentationCanvas.GetComponentsInChildren<Graphic>()) { graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); }
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
            pixels=new Texture2D(width,height,TextureFormat.RGB24,false); pixels.ReadPixels(new Rect(0,0,width,height),0,0); pixels.Apply();
            Directory.CreateDirectory(Output); string path=Path.GetFullPath(Path.Combine(Output,name)); File.WriteAllBytes(path,pixels.EncodeToPNG()); return path;
        }
        finally
        {
            RenderTexture.active=previous;
            // Native Dropdown schedules Destroy in OnDisable; disposable EditMode previews need immediate cleanup.
            if(openedDropdown!=null)
            {
                foreach(string fieldName in new[]{"m_Dropdown","m_Blocker"})
                {
                    FieldInfo field=typeof(Dropdown).GetField(fieldName,BindingFlags.Instance|BindingFlags.NonPublic);
                    var popup=(GameObject)field.GetValue(openedDropdown); field.SetValue(openedDropdown,null);
                    if(popup!=null) UnityEngine.Object.DestroyImmediate(popup);
                }
            }
            if(host!=null) UnityEngine.Object.DestroyImmediate(host);
            if(pixels!=null) UnityEngine.Object.DestroyImmediate(pixels);
            EditorSceneManager.ClosePreviewScene(preview);
            if(target!=null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if(original.IsValid()&&original.isLoaded) SceneManager.SetActiveScene(original);
            if(selection!=null&&selection.activeInHierarchy&&EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(selection);
        }
    }
}
