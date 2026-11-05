using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class MainMenuSettingsForestTests
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private GameObject host;
    private MainMenuSettingsOverlay overlay;
    private MainMenuSettingsData original,saved;
    private int saves;

    [SetUp]
    public void SetUp()
    {
        saves=0; saved=null;
        host=new GameObject("Settings forest test"); overlay=host.AddComponent<MainMenuSettingsOverlay>();
        overlay.ConfigureForestFont(AssetDatabase.LoadAssetAtPath<Font>(MainMenuVisualTools.FontPath));
        original=new MainMenuSettingsData { musicVolume=.23f,sfxVolume=.61f,isFullscreen=false,vibrationEnabled=false,qualityLevel=0,resolutionWidth=1280,resolutionHeight=720,targetFrameRate=90 };
        overlay.Show(original,data=>{ saves++; saved=data; });
    }
    [TearDown] public void TearDown() { Object.DestroyImmediate(host); }

    [Test]
    public void ExistingSettingsPopulateNativeControlsIncludingOffStateAndExactFrameRate()
    {
        Assert.AreEqual("设 置",Field<Text>("titleText").text);
        Assert.AreEqual("23%",Field<Text>("musicValueText").text); Assert.AreEqual("61%",Field<Text>("sfxValueText").text);
        Assert.IsFalse(Field<Toggle>("fullscreenToggle").isOn); Assert.IsFalse(Field<Toggle>("vibrationToggle").isOn);
        Assert.AreEqual("关闭",Field<Toggle>("vibrationToggle").transform.Find("State").GetComponent<Text>().text);
        Dropdown frames=Field<Dropdown>("frameRateDropdown"); Assert.AreEqual("90 FPS",frames.options[frames.value].text);
        Assert.AreEqual(QualitySettings.names.Length,Field<Dropdown>("qualityDropdown").options.Count);
        Assert.AreEqual(MainMenuSettingsPersistence.SupportsRuntimeResolutionChanges,Field<Dropdown>("resolutionDropdown").interactable);
        Assert.Greater(Field<Dropdown>("resolutionDropdown").options.Count,0);
        Assert.AreEqual(0,saves);
    }

    [Test]
    public void SaveCollectsEverySettingOnceWithoutMutatingTheInputSnapshot()
    {
        Field<Slider>("musicSlider").value=.84f; Field<Slider>("sfxSlider").value=.36f;
        Field<Toggle>("fullscreenToggle").isOn=true; Field<Toggle>("vibrationToggle").isOn=true;
        Field<Dropdown>("frameRateDropdown").value=5;
        Field<Dropdown>("qualityDropdown").value=Mathf.Min(1,QualitySettings.names.Length-1);
        Field<Button>("saveButton").onClick.Invoke(); Field<Button>("saveButton").onClick.Invoke();
        Assert.AreEqual(1,saves); Assert.AreEqual(.84f,saved.musicVolume,.001f); Assert.AreEqual(.36f,saved.sfxVolume,.001f);
        Assert.IsTrue(saved.isFullscreen); Assert.IsTrue(saved.vibrationEnabled); Assert.AreEqual(144,saved.targetFrameRate);
        Assert.AreEqual(Mathf.Min(1,QualitySettings.names.Length-1),saved.qualityLevel);
        Assert.Greater(saved.resolutionWidth,0); Assert.Greater(saved.resolutionHeight,0); Assert.GreaterOrEqual(saved.refreshRate,30);
        Assert.AreEqual(.23f,original.musicVolume,.001f); Assert.IsFalse(original.isFullscreen); Assert.AreEqual(90,original.targetFrameRate);
        Assert.IsFalse(overlay.IsVisible);
    }

    [Test]
    public void ReturningDiscardsDraftAndReopeningRestoresItsOriginalValues()
    {
        Field<Slider>("musicSlider").value=0; Field<Toggle>("vibrationToggle").isOn=true;
        Field<Button>("closeButton").onClick.Invoke(); Assert.AreEqual(0,saves);
        overlay.Show(original,null);
        Assert.AreEqual(.23f,Field<Slider>("musicSlider").value,.001f);
        Assert.IsFalse(Field<Toggle>("vibrationToggle").isOn);
        Assert.AreEqual("关闭",Field<Toggle>("vibrationToggle").transform.Find("State").GetComponent<Text>().text);
    }

    [TestCase(2340,1080,0,0)]
    [TestCase(1920,1080,0,0)]
    [TestCase(1600,1200,0,0)]
    [TestCase(2400,1080,140,60)]
    [TestCase(1080,1920,50,30)]
    public void SettingsControlsRemainInsideTheSafeAreaWithIndependentSections(int width,int height,int left,int right)
    {
        Canvas canvas=overlay.PresentationCanvas; canvas.GetComponent<CanvasScaler>().enabled=false; canvas.renderMode=RenderMode.WorldSpace;
        RectTransform root=(RectTransform)canvas.transform; root.sizeDelta=new Vector2(1080f*width/height,1080);
        overlay.ApplyViewport(new Vector2(width,height),new Rect(left,24,width-left-right,height-48)); Canvas.ForceUpdateCanvases();
        Rect safe=Bounds(Field<RectTransform>("safeRoot")),panel=Bounds(Field<RectTransform>("panelRect"));
        Contains(safe,panel); Contains(safe,Bounds(Field<RectTransform>("titlePlaque")));
        Assert.IsFalse(Bounds(Field<RectTransform>("audioSection")).Overlaps(Bounds(Field<RectTransform>("displaySection"))));
        foreach(Selectable control in canvas.GetComponentsInChildren<Selectable>().Where(c=>!(c is Scrollbar))) Contains(panel,Bounds(control.transform));
        foreach(Slider slider in new[]{Field<Slider>("musicSlider"),Field<Slider>("sfxSlider")})
        {
            slider.value=0; Canvas.ForceUpdateCanvases(); Contains(Bounds(slider.transform),Bounds(slider.handleRect));
            slider.value=1; Canvas.ForceUpdateCanvases(); Contains(Bounds(slider.transform),Bounds(slider.handleRect));
        }
        Assert.IsTrue(canvas.GetComponentsInChildren<MainMenuSettingsGraphic>().Where(g=>g.part!=MainMenuSettingsGraphic.Part.Grip).All(g=>!g.raycastTarget));
    }

    private T Field<T>(string name) => (T)typeof(MainMenuSettingsOverlay).GetField(name,Private).GetValue(overlay);
    private static Rect Bounds(Transform transform) { var c=new Vector3[4]; ((RectTransform)transform).GetWorldCorners(c); return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y); }
    private static void Contains(Rect outer,Rect inner) { Assert.GreaterOrEqual(inner.xMin,outer.xMin-.25f); Assert.GreaterOrEqual(inner.yMin,outer.yMin-.25f); Assert.LessOrEqual(inner.xMax,outer.xMax+.25f); Assert.LessOrEqual(inner.yMax,outer.yMax+.25f); }
}
