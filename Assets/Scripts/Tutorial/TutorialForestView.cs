using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One painted, responsive presentation for menu, map and battle tutorials; callers own progression.</summary>
public sealed class TutorialForestView : IDisposable
{
    public readonly Canvas Canvas;
    public readonly RectTransform Root, Panel, BodyArea, BodyContent;
    public readonly Graphic Title, Body, PrimaryLabel, SecondaryLabel;
    public readonly Button Primary, Secondary;
    public readonly ScrollRect Reading;
    public readonly TutorialSpotlightGraphic Shade;
    public readonly RectTransform Focus;
    public RectTransform Catalog;
    public bool CatalogVisible;
    public bool IsClosing { get; private set; }
    public bool HasFocus { get; private set; }
    public Rect FocusScreenRect { get; private set; }
    public Rect SafeRect { get; private set; }
    private readonly bool tmp, welcome;
    private readonly Graphic bannerLabel;
    private readonly RectTransform banner, illustration, topics, rule, symbol;
    private readonly CanvasGroup group;
    private readonly TutorialForestGraphic focusFace, leader, symbolFace;
    private readonly RectTransform[] topicItems = new RectTransform[3];
    private readonly Graphic[] topicLabels = new Graphic[3];
    private readonly Scrollbar scrollbar;
    private Func<Rect?> targetProvider, avoidanceProvider;
    private Rect? legacyTarget;
    private bool focused, targetOnly, dirty = true, viewportOverride;
    private Vector2 pixels, lastRootSize, center;
    private Rect safePixels, targetLocal, avoidLocal;
    private float progress, fit = 1, pulse, targetReveal = 1;
    private Action completed;
    private GameObject previousSelection;
    public Vector2 WindowSize => Panel.sizeDelta;

    public TutorialForestView(string name, Transform parent, int order, bool useTmp, bool isWelcome)
    {
        tmp = useTmp; welcome = isWelcome;
        GameObject go = new GameObject(name, typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        if (parent != null) go.transform.SetParent(parent,false);
        go.hideFlags = HideFlags.DontSave;
        Canvas = go.GetComponent<Canvas>(); Canvas.renderMode = RenderMode.ScreenSpaceOverlay; Canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        Root = (RectTransform)go.transform;
        RectTransform shade = Rect(Root,"Backdrop"); Stretch(shade);
        Shade = shade.gameObject.AddComponent<TutorialSpotlightGraphic>(); Shade.color = new Color(.005f,.015f,.009f,.58f);
        RectTransform line = Rect(Root,"GuideLeader"); Stretch(line); leader = Face(line,TutorialForestGraphic.TutorialStyle.Leader);
        Focus = Rect(Root,"HighlightFrame"); focusFace = Face(Focus,TutorialForestGraphic.TutorialStyle.Focus); Focus.gameObject.SetActive(false);
        Panel = Rect(Root,"Panel"); Panel.anchorMin = Panel.anchorMax = Panel.pivot = Vector2.one*.5f;
        var hit = Panel.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        group = Panel.gameObject.AddComponent<CanvasGroup>();
        RectTransform backing = Rect(Panel,"OpaqueGreenBacking"); Stretch(backing,10,10);
        var backingImage=backing.gameObject.AddComponent<Image>();backingImage.color=new Color32(5,32,13,255);backingImage.raycastTarget=false;
        RectTransform art = Rect(Panel,"ForestFrame"); Stretch(art); art.offsetMin = new Vector2(-16,-22); art.offsetMax = new Vector2(16,22);
        Image face = art.gameObject.AddComponent<Image>(); face.sprite = Resources.Load<Sprite>("TutorialUI/ForestWindow");
        face.type = Image.Type.Sliced; face.pixelsPerUnitMultiplier = 3; face.raycastTarget = false;
        if (face.sprite == null) { face.color=Color.clear; var fallback=art.gameObject.AddComponent<MapCraftingFrameGraphic>(); fallback.craftingStyle=MapCraftingFrameGraphic.Style.Window; fallback.raycastTarget=false; }
        banner = Rect(Panel,"TitlePlaque"); var plaque = banner.gameObject.AddComponent<Image>(); plaque.sprite=Resources.Load<Sprite>("TutorialUI/WoodTitle");
        plaque.raycastTarget=false;
        bannerLabel = Text(banner,"Context",welcome?42:34,new Color32(246,206,119,255)); Stretch((RectTransform)bannerLabel.transform,48,2); Align(bannerLabel,true);
        illustration = Rect(Panel,"AdventureIllustration"); var picture=illustration.gameObject.AddComponent<Image>();picture.sprite=welcome?Resources.Load<Sprite>("TutorialUI/AdventureGuide"):null;picture.preserveAspect=true;picture.raycastTarget=false;MapItemIconFit.Attach(picture);
        symbol = Rect(Panel,"GuideSymbol"); symbolFace=Face(symbol,TutorialForestGraphic.TutorialStyle.Compass);
        Title = Text(Panel,welcome?"SummaryTitle":"Title",welcome?40:38,Ivory);
        BodyArea = Rect(Panel,"BodyReading"); Reading = Scroll(BodyArea,out RectTransform viewport,out RectTransform content,out scrollbar); BodyContent=content;
        Body = Text(content,welcome?"SummaryBody":"Body",28,Ivory); Align(Body,false);
        topics=Rect(Panel,"LearningTopics");
        for(int i=0;welcome&&i<3;i++)
        {
            topicItems[i]=Rect(topics,"Topic_"+i); RectTransform icon=Rect(topicItems[i],"Icon");
            Face(icon,i==0?TutorialForestGraphic.TutorialStyle.Footprint:i==1?TutorialForestGraphic.TutorialStyle.Die:TutorialForestGraphic.TutorialStyle.Growth);
            topicLabels[i]=Text(topicItems[i],"Label",23,Gold); SetText(topicLabels[i],i==0?GameText.Get("tutorial.map.context", "地图探索"):i==1?GameText.Get("ui.tutorial_forest_view.dice_combat", "骰子战斗"):GameText.Get("ui.tutorial_forest_view.resources_progression", "资源与成长"));
        }
        rule=Rect(Panel,"FooterRule");Face(rule,TutorialForestGraphic.TutorialStyle.Divider);
        Primary=Button(Panel,welcome?"StartButton":"ContinueButton",true,out PrimaryLabel);
        if(welcome)Secondary=Button(Panel,"BackButton",false,out SecondaryLabel);
        SetContext(welcome?GameText.Get("ui.tutorial_forest_view.adventure_tutorial", "冒险教程"):GameText.Get("ui.menu.tutorial", "教程"));
        UnityEngine.Canvas.willRenderCanvases += BeforeRender;
        go.SetActive(false);
    }

    public static readonly Color Ivory = new Color32(244,234,207,255), Gold = new Color32(230,191,104,255);
    public static void SetText(Graphic graphic,string value) { if(graphic is TMP_Text t)t.text=value??string.Empty;else if(graphic is Text text)text.text=value??string.Empty; }
    public static string GetText(Graphic graphic) => graphic is TMP_Text t?t.text:graphic is Text text?text.text:string.Empty;
    public void SetContext(string value) { SetText(bannerLabel,value); }
    public void SetLegacyFonts(Font title,Font body,Font button)
    {
        Font fallback=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach(Text text in Root.GetComponentsInChildren<Text>(true))text.font=text==Title || text==bannerLabel?title??body??fallback:text==PrimaryLabel||text==SecondaryLabel?button??body??fallback:body??title??fallback;
        dirty=true;
    }
    public void SetTmpFonts(TMP_FontAsset title,TMP_FontAsset body)
    {
        foreach(TMP_Text text in Root.GetComponentsInChildren<TMP_Text>(true))
        { var font=text==Title||text==bannerLabel||text==PrimaryLabel||text==SecondaryLabel?title??body:body??title;if(font!=null)text.font=font; }
        dirty=true;
    }
    public bool ShowFootprint { get; set; }
    public void MarkLayoutDirty() { dirty=true; }
    public void Show(bool highlight=false,Func<Rect?> target=null,Func<Rect?> keepClear=null)
    {
        bool wasVisible=Canvas.gameObject.activeSelf && Panel.gameObject.activeSelf;
        if(!wasVisible && EventSystem.current!=null)previousSelection=EventSystem.current.currentSelectedGameObject;
        focused=highlight;targetOnly=false;targetProvider=target;avoidanceProvider=keepClear;legacyTarget=null;
        symbolFace.tutorialStyle=ShowFootprint?TutorialForestGraphic.TutorialStyle.Footprint:tmp?TutorialForestGraphic.TutorialStyle.Die:TutorialForestGraphic.TutorialStyle.Compass;
        symbolFace.SetVerticesDirty();
        completed=null;IsClosing=false;Panel.gameObject.SetActive(true);Canvas.gameObject.SetActive(true);
        group.interactable=true;group.blocksRaycasts=true;
        if(!wasVisible)progress=Application.isPlaying?0:1;
        dirty=true;UnityEngine.Canvas.ForceUpdateCanvases();RefreshGeometry();ApplyMotion();
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(Primary.gameObject);
    }
    public void ShowLegacyHighlight(Vector2 anchor,Vector2 size)
    {
        focused=true;targetProvider=null;avoidanceProvider=null;
        legacyTarget=new Rect(anchor.x,anchor.y-size.y,size.x,size.y);dirty=true;RefreshGeometry();
    }
    public void ShowTarget(Func<Rect?> target)
    {
        if(Panel.gameObject.activeSelf)RestoreSelection();
        focused=true;targetOnly=true;targetProvider=target;avoidanceProvider=null;legacyTarget=null;
        targetReveal=Application.isPlaying?0:1;
        completed=null;IsClosing=false;Panel.gameObject.SetActive(false);Canvas.gameObject.SetActive(true);progress=1;dirty=true;
        UnityEngine.Canvas.ForceUpdateCanvases();RefreshGeometry();ApplyMotion();
    }
    public void TrackTarget(Func<Rect?> target,Func<Rect?> keepClear=null) { targetProvider=target;avoidanceProvider=keepClear;legacyTarget=null;focused=true;RefreshGeometry(); }
    public void Close(Action afterClose)
    {
        if(IsClosing)return;
        if(!Application.isPlaying || !Canvas.gameObject.activeSelf || !Panel.gameObject.activeSelf) { afterClose?.Invoke();return; }
        IsClosing=true;completed=afterClose;group.interactable=false;
    }
    public void HideImmediately()
    {
        completed=null;IsClosing=false;progress=0;targetProvider=avoidanceProvider=null;legacyTarget=null;focused=targetOnly=HasFocus=false;
        if(Canvas==null)return;
        Reading.StopMovement();Shade.SetHole(default,false,true);Focus.gameObject.SetActive(false);Canvas.gameObject.SetActive(false);RestoreSelection();
    }
    private void RestoreSelection()
    {
        EventSystem e=EventSystem.current;
        if(e!=null && previousSelection!=null && previousSelection.activeInHierarchy && (e.currentSelectedGameObject==null || e.currentSelectedGameObject.transform.IsChildOf(Panel)))e.SetSelectedGameObject(previousSelection);
        previousSelection=null;
    }
    public void Advance(float delta)
    {
        if(Canvas==null || !Canvas.gameObject.activeSelf)return;
        RefreshGeometry();pulse+=Mathf.Max(0,delta);
        if(targetOnly)targetReveal=Mathf.MoveTowards(targetReveal,1,Mathf.Max(0,delta)/.14f);
        if(Panel.gameObject.activeSelf)progress=Mathf.MoveTowards(progress,IsClosing?0:1,Mathf.Max(0,delta)/(IsClosing?.16f:.22f));
        ApplyMotion();
        if(IsClosing && progress<=0) { Action callback=completed;completed=null;IsClosing=false;callback?.Invoke(); }
    }
    private void ApplyMotion()
    {
        float ease=1-Mathf.Pow(1-progress,3);
        group.alpha=ease;Panel.localScale=Vector3.one*(fit*Mathf.Lerp(.97f,1,ease));
        Panel.anchoredPosition=center+Vector2.down*((1-ease)*14*fit);
        float spotlightAlpha=targetOnly?targetReveal:ease;
        Shade.color=new Color(.005f,.015f,.009f,(targetOnly?.62f:.58f)*spotlightAlpha);
        focusFace.glow=.5f+.5f*Mathf.Sin(pulse*3.9f);focusFace.color=new Color(1,1,1,spotlightAlpha);focusFace.SetVerticesDirty();
        leader.color=new Color(1,1,1,Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.4f)/.6f)));
    }
    public void ApplyViewport(Vector2 viewport,Rect safe)
    { pixels=viewport;safePixels=safe;viewportOverride=true;dirty=true;RefreshGeometry();ApplyMotion(); }
    public void UseScreenViewport() { viewportOverride=false;dirty=true; }
    private void BeforeRender() { if(Canvas!=null && Canvas.gameObject.activeInHierarchy)RefreshGeometry(); }
    private void RefreshGeometry()
    {
        if(Root==null)return;
        if(!viewportOverride)
        {
            Vector2 current=new Vector2(Screen.width,Screen.height);
            if(pixels!=current||safePixels!=Screen.safeArea){pixels=current;safePixels=Screen.safeArea;dirty=true;}
        }
        if(pixels.x<=0||pixels.y<=0)return;
        Rect root=Root.rect;if(root.width<1||root.height<1)return;
        if(lastRootSize!=root.size){lastRootSize=root.size;dirty=true;}
        SafeRect=UnityEngine.Rect.MinMaxRect(root.xMin+safePixels.xMin/pixels.x*root.width,root.yMin+safePixels.yMin/pixels.y*root.height,
            root.xMin+safePixels.xMax/pixels.x*root.width,root.yMin+safePixels.yMax/pixels.y*root.height);
        HasFocus=false;Rect focus=default;FocusScreenRect=default;
        if(focused)
        {
            if(legacyTarget.HasValue){Rect r=legacyTarget.Value;focus=new Rect(root.xMin+r.x,root.yMax+r.y,r.width,r.height);HasFocus=r.width>.5f&&r.height>.5f;}
            else
            {
                Rect? screen=targetProvider?.Invoke();
                if(screen.HasValue){FocusScreenRect=screen.Value;HasFocus=TutorialHighlightCoordinateUtility.TryScreenRectToLocal(Root,screen.Value,out focus);}
            }
            if(HasFocus)
            {
                focus=UnityEngine.Rect.MinMaxRect(focus.xMin-5,focus.yMin-5,focus.xMax+5,focus.yMax+5);
                HasFocus=TutorialHighlightCoordinateUtility.Intersect(focus,root,out focus);
            }
        }
        targetLocal=focus;avoidLocal=focus;
        Rect? avoid=avoidanceProvider?.Invoke();
        if(avoid.HasValue && TutorialHighlightCoordinateUtility.TryScreenRectToLocal(Root,avoid.Value,out Rect a))avoidLocal=a;
        Shade.SetHole(focus,HasFocus,!targetOnly);
        Focus.gameObject.SetActive(HasFocus);if(HasFocus)SetLocalRect(Focus,focus);
        if(dirty){Layout();dirty=false;}
        Vector2 scaled=Panel.sizeDelta*fit;
        center=HasFocus&&!targetOnly?ChoosePosition(SafeRect,avoidLocal,scaled):SafeRect.center;
        Panel.anchoredPosition=center+Vector2.down*(Mathf.Pow(1-progress,3)*14*fit);
        UpdateLeader(scaled);
    }
    private void Layout()
    {
        bool stack=welcome&&SafeRect.width<1000;
        float w=Mathf.Min(welcome?(stack?880:1200):focused?660:960,Mathf.Max(320,SafeRect.width-64));
        float contentWidth=welcome?(stack?w-96:w-466):w-96;
        float titleHeight=Mathf.Clamp(Height(Title,welcome?contentWidth:w-164,56),56,144);
        float bodyHeight=Height(Body,contentWidth-14,45);
        float bodyTop=welcome?(stack?316:86)+titleHeight+14:72+titleHeight+18;
        float maxBody=welcome?(CatalogVisible?180:240):360;
        float readHeight=Mathf.Min(bodyHeight,maxBody);
        float footer=welcome&&w<680?186:112;
        float h=welcome?(CatalogVisible?860:stack?bodyTop+readHeight+100+footer:Mathf.Max(560,bodyTop+readHeight+98+footer)):Mathf.Max(310,bodyTop+readHeight+footer+10);
        h=Mathf.Min(h,Mathf.Max(290,SafeRect.height-96));
        float x=welcome&&!stack?418:48;
        float available=Mathf.Max(60,h-footer-bodyTop-(welcome&&!CatalogVisible?90:10));
        if(CatalogVisible)available=Mathf.Min(180,available*.45f);
        readHeight=Mathf.Min(readHeight,available);
        Panel.sizeDelta=new Vector2(w,h);
        fit=Mathf.Min(1,SafeRect.width/(w+40),SafeRect.height/(h+(welcome?148:112)));
        Place(banner,(w-(welcome?450:320))*.5f,welcome?-66:-46,welcome?450:320,welcome?144:104);
        illustration.gameObject.SetActive(welcome);symbol.gameObject.SetActive(!welcome);
        if(welcome)Place(illustration,stack?(w-238)*.5f:52,stack?65:95,stack?238:330,stack?228:325);
        else Place(symbol,42,73,48,48);
        Place((RectTransform)Title.transform,welcome?x:108,welcome?(stack?316:86):72,welcome?contentWidth:w-154,titleHeight);
        Place(BodyArea,x,bodyTop,contentWidth,readHeight);
        Place((RectTransform)Body.transform,0,0,contentWidth-14,bodyHeight);
        BodyContent.sizeDelta=new Vector2(0,bodyHeight);
        bool overflow=bodyHeight>readHeight+.5f;scrollbar.gameObject.SetActive(overflow);
        if(!overflow)BodyContent.anchoredPosition=Vector2.zero;
        topics.gameObject.SetActive(welcome&&!CatalogVisible);
        if(welcome&&!CatalogVisible)
        {
            float top=Mathf.Min(h-footer-78,bodyTop+readHeight+22);Place(topics,x,top,contentWidth,68);
            for(int i=0;i<3;i++)
            {
                float col=contentWidth/3;Place(topicItems[i],i*col,0,col,64);
                Place((RectTransform)topicItems[i].GetChild(0),0,4,col<180?30:44,48);
                Place((RectTransform)topicLabels[i].transform,col<180?34:50,0,col-(col<180?34:50),64);
                Size(topicLabels[i],col<180?18:23);
            }
        }
        if(Catalog!=null){Catalog.gameObject.SetActive(CatalogVisible);Place(Catalog,x,bodyTop+readHeight+24,contentWidth,h-footer-bodyTop-readHeight-34);}
        Place(rule,40,h-footer+4,w-80,5);
        if(welcome)
        {
            float buttonWidth=w<680?Mathf.Min(w-96,360):Mathf.Min(340,(w-160)/2);
            if(w<680){Place((RectTransform)Primary.transform,(w-buttonWidth)*.5f,h-162,buttonWidth,66);Place((RectTransform)Secondary.transform,(w-buttonWidth)*.5f,h-84,buttonWidth,66);}
            else{Place((RectTransform)Primary.transform,w*.5f-buttonWidth-20,h-88,buttonWidth,68);Place((RectTransform)Secondary.transform,w*.5f+20,h-88,buttonWidth,68);}
        }
        else Place((RectTransform)Primary.transform,w-260,h-86,216,64);
    }
    private static Vector2 ChoosePosition(Rect safe,Rect avoid,Vector2 size)
    {
        float margin=38,gap=36;Rect limit=UnityEngine.Rect.MinMaxRect(safe.xMin+margin+size.x*.5f,safe.yMin+margin+size.y*.5f,safe.xMax-margin-size.x*.5f,safe.yMax-56-size.y*.5f);
        Vector2[] candidates={new Vector2(avoid.xMax+gap+size.x*.5f,avoid.yMax-size.y*.5f),new Vector2(avoid.xMin-gap-size.x*.5f,avoid.yMax-size.y*.5f),new Vector2(avoid.center.x,avoid.yMin-gap-size.y*.5f),new Vector2(avoid.center.x,avoid.yMax+gap+size.y*.5f)};
        float best=float.PositiveInfinity;Vector2 result=safe.center;
        for(int i=0;i<candidates.Length;i++)
        {
            Vector2 p=new Vector2(Mathf.Clamp(candidates[i].x,limit.xMin,Mathf.Max(limit.xMin,limit.xMax)),Mathf.Clamp(candidates[i].y,limit.yMin,Mathf.Max(limit.yMin,limit.yMax)));
            Rect r=new Rect(p-size*.5f,size);float overlap=TutorialHighlightCoordinateUtility.Intersect(r,avoid,out Rect cross)?cross.width*cross.height:0;
            float score=overlap*1000+(p-candidates[i]).sqrMagnitude+i*80;
            if(score<best){best=score;result=p;}
        }
        return result;
    }
    private void UpdateLeader(Vector2 size)
    {
        bool sideBySide=center.x+size.x*.5f<=avoidLocal.xMin || center.x-size.x*.5f>=avoidLocal.xMax;
        bool visible=HasFocus&&!targetOnly&&Panel.gameObject.activeSelf&&sideBySide;
        leader.gameObject.SetActive(visible);if(!visible)return;
        float side=center.x<avoidLocal.center.x?1:-1;
        Vector2 start=new Vector2(center.x+side*size.x*.5f,center.y+size.y*.5f-80*fit);
        float corridor=Mathf.Min(SafeRect.yMax-8,Mathf.Max(avoidLocal.yMax,targetLocal.yMax)+18);
        Vector2[] route={start,start+Vector2.right*side*18,new Vector2(start.x+side*18,corridor),new Vector2(targetLocal.center.x,corridor),new Vector2(targetLocal.center.x,targetLocal.yMax)};
        bool changed=leader.points==null||leader.points.Length!=route.Length;
        if(!changed)for(int i=0;i<route.Length;i++)if(leader.points[i]!=route[i]){changed=true;break;}
        if(changed){leader.points=route;leader.SetVerticesDirty();}
    }
    private Graphic Text(Transform parent,string name,int size,Color tint)
    {
        RectTransform rect=Rect(parent,name);
        if(tmp)
        {
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.fontSize=size;text.color=tint;text.enableWordWrapping=true;text.overflowMode=TextOverflowModes.Overflow;text.alignment=TextAlignmentOptions.TopLeft;text.raycastTarget=false;ResourceKeywordText.Attach(text,false);return text;
        }
        var legacy=rect.gameObject.AddComponent<ResourceKeywordLegacyText>();legacy.IncludeIcons=false;legacy.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");legacy.fontSize=size;legacy.color=tint;legacy.horizontalOverflow=HorizontalWrapMode.Wrap;legacy.verticalOverflow=VerticalWrapMode.Overflow;legacy.alignment=TextAnchor.UpperLeft;legacy.lineSpacing=1.18f;legacy.raycastTarget=false;return legacy;
    }
    private Button Button(Transform parent,string name,bool primary,out Graphic label)
    {
        RectTransform rect=Rect(parent,name);Graphic face;
        if(primary){var image=rect.gameObject.AddComponent<Image>();image.sprite=Resources.Load<Sprite>("MainMenu/GildedButton");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=3;face=image;}
        else{var image=rect.gameObject.AddComponent<MapMerchantFrameGraphic>();image.style=MapMerchantFrameGraphic.Style.SecondaryButton;face=image;}
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=face;button.transition=Selectable.Transition.None;rect.gameObject.AddComponent<MapCraftingButtonFeedback>();
        label=Text(rect,"Label",30,primary?new Color32(49,31,9,255):Ivory);Stretch((RectTransform)label.transform,22,0);Align(label,true);return button;
    }
    public static float Height(Graphic text,float width,float minimum=0)
    {
        if(text is TMP_Text tmp)return Mathf.Max(minimum,tmp.GetPreferredValues(tmp.text,Mathf.Max(30,width),float.PositiveInfinity).y+8);
        var legacy=text as Text;if(legacy==null||legacy.font==null||legacy.pixelsPerUnit<.001f)return minimum;
        float height=legacy.cachedTextGeneratorForLayout.GetPreferredHeight(legacy.text,legacy.GetGenerationSettings(new Vector2(Mathf.Max(30,width),0)))/legacy.pixelsPerUnit+8;
        return float.IsNaN(height)||float.IsInfinity(height)?minimum:Mathf.Max(minimum,height);
    }
    public static void Align(Graphic graphic,bool center)
    {if(graphic is TMP_Text t)t.alignment=center?TextAlignmentOptions.Midline:TextAlignmentOptions.TopLeft;else if(graphic is Text l)l.alignment=center?TextAnchor.MiddleCenter:TextAnchor.UpperLeft;}
    private static void Size(Graphic graphic,int size){if(graphic is TMP_Text t)t.fontSize=size;else if(graphic is Text l)l.fontSize=size;}
    public static RectTransform Rect(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
    public static void Place(RectTransform rect,float x,float y,float w,float h){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(Mathf.Max(1,w),Mathf.Max(1,h));}
    public static void Stretch(RectTransform rect,float x=0,float y=0){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=Vector2.one*.5f;rect.offsetMin=new Vector2(x,y);rect.offsetMax=new Vector2(-x,-y);}
    private static void SetLocalRect(RectTransform rect,Rect r){rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=r.center;rect.sizeDelta=r.size;}
    private static TutorialForestGraphic Face(RectTransform parent,TutorialForestGraphic.TutorialStyle style){var graphic=parent.gameObject.AddComponent<TutorialForestGraphic>();graphic.tutorialStyle=style;graphic.raycastTarget=false;return graphic;}
    public static ScrollRect Scroll(RectTransform parent,out RectTransform viewport,out RectTransform content,out Scrollbar scrollbar)
    {
        var scroll=parent.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=38;
        viewport=Rect(parent,"Viewport");Stretch(viewport);viewport.offsetMax=new Vector2(-14,0);var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
        content=Rect(viewport,"Content");content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(0,1);content.sizeDelta=Vector2.zero;
        RectTransform track=Rect(parent,"Scrollbar");track.anchorMin=new Vector2(1,0);track.anchorMax=Vector2.one;track.pivot=new Vector2(1,.5f);track.sizeDelta=new Vector2(5,0);
        var background=track.gameObject.AddComponent<Image>();background.color=new Color(.08f,.14f,.08f,.8f);scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
        RectTransform slide=Rect(track,"SlidingArea");Stretch(slide);RectTransform handle=Rect(slide,"Handle");Stretch(handle);var image=handle.gameObject.AddComponent<Image>();image.color=Gold;scrollbar.handleRect=handle;scrollbar.targetGraphic=image;
        scroll.viewport=viewport;scroll.content=content;scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;return scroll;
    }
    public void Dispose(){UnityEngine.Canvas.willRenderCanvases-=BeforeRender;completed=null;if(Canvas!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(Canvas.gameObject);else UnityEngine.Object.DestroyImmediate(Canvas.gameObject);}}
}
