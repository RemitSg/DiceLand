using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class MainMenuSettingsOverlay
{
    private static readonly Color Ivory=new Color32(239,231,201,255);
    private static readonly Color Gold=new Color32(221,185,108,255);
    private static readonly Color Muted=new Color32(166,184,158,255);
    private Font forestFont;
    private RectTransform safeRoot,titlePlaque,audioSection,displaySection,musicRow,sfxRow,vibrationRow;
    private RectTransform gameplaySection, battleAdvanceRow, detailSizeRow, languageRow;
    private RectTransform fullscreenRow,qualityRow,resolutionRow,frameRateRow,footerRule;
    private Text subtitleText, footerHint,deviceHint;
    private Button closeXButton;
    private GameObject previousSelection;
    private bool saveInvoked,focusWhenReady;
    private Vector2 layoutViewport,layoutCanvas;
    private Rect layoutSafe;

    public void ConfigureForestFont(Font font) { forestFont=font; ApplyConfiguredFonts(); }

    private void BuildForestUi()
    {
        EventSystemUtility.EnsureSingleEventSystem();
        var root=new GameObject("MainMenuSettingsOverlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=9500;
        CanvasScaler scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=1;
        RectTransform backdrop=MakeRect("SettingsBackdrop",(RectTransform)root.transform); Stretch(backdrop);
        backdrop.gameObject.AddComponent<Image>().color=new Color(.004f,.012f,.008f,.46f);
        safeRoot=MakeRect("SettingsSafeArea",(RectTransform)root.transform); Stretch(safeRoot);
        panelRect=MakeRect("Panel",safeRoot); panelRect.anchorMin=panelRect.anchorMax=panelRect.pivot=Vector2.one*.5f;
        panelRect.gameObject.AddComponent<Image>().color=Color.clear;
        MapCraftingFrameGraphic frame=MakeRect("ForestFrame",panelRect).gameObject.AddComponent<MapCraftingFrameGraphic>();
        Stretch(frame.rectTransform); frame.craftingStyle=MapCraftingFrameGraphic.Style.Window; frame.raycastTarget=false;
        titlePlaque=MakeRect("TitlePlaque",panelRect); Skin(titlePlaque,MainMenuSettingsGraphic.Part.Title);
        titleText=MakeText("Title",titlePlaque,GameText.Get("ui.settings.title", "设 置"),46,ResolveTitleFont(),Gold,TextAnchor.MiddleCenter); Stretch(titleText.rectTransform,48,4);
        subtitleText=MakeText("Subtitle",panelRect,GameText.Get("ui.settings.subtitle", "声音 · 画面 · 游玩"),22,ResolveLabelFont(),Muted,TextAnchor.MiddleCenter); labelTexts.Add(subtitleText);
        audioSection=BuildSection("AudioSection",GameText.Get("ui.settings.sound", "声音"),MainMenuSettingsGraphic.Part.Speaker);
        displaySection=BuildSection("DisplaySection",GameText.Get("ui.settings.display", "画面与性能"),MainMenuSettingsGraphic.Part.Display);
        gameplaySection = BuildSection("GameplaySection", GameText.Get("ui.settings.gameplay", "游玩与反馈"), MainMenuSettingsGraphic.Part.Grip);
        musicRow=BuildAudioRow("MusicRow",GameText.Get("ui.settings.music", "音乐音量"),audioSection,out musicSlider,out musicValueText);
        sfxRow=BuildAudioRow("SfxRow",GameText.Get("ui.settings.sfx", "音效音量"),audioSection,out sfxSlider,out sfxValueText);
        battleAdvanceRow = BuildDropdownRow("BattleAdvanceRow", GameText.Get("ui.settings.battle_advance", "战斗推进"), gameplaySection, out battleAdvanceDropdown);
        detailSizeRow = BuildDropdownRow("DetailSizeRow", GameText.Get("ui.settings.detail_size", "详情字号"), gameplaySection, out detailSizeDropdown);
        vibrationRow=BuildSwitchRow("VibrationRow",GameText.Get("ui.settings.vibration", "振动反馈"),gameplaySection,out vibrationToggle);
        languageRow = BuildDropdownRow("LanguageRow", GameText.Get("ui.settings.language", "语言"), gameplaySection, out languageDropdown);
        languageDropdown.AddOptions(new List<string> { GameText.Get("ui.settings.build_forest_ui", "简体中文"), "English" });
        fullscreenRow=BuildSwitchRow("FullscreenRow",GameText.Get("ui.settings.fullscreen", "全屏显示"),displaySection,out fullscreenToggle);
        qualityRow=BuildDropdownRow("QualityRow",GameText.Get("ui.settings.quality", "画质预设"),displaySection,out qualityDropdown);
        resolutionRow=BuildDropdownRow("ResolutionRow",GameText.Get("ui.settings.resolution", "分辨率"),displaySection,out resolutionDropdown);
        frameRateRow=BuildDropdownRow("FrameRateRow",GameText.Get("ui.settings.frame_limit", "帧率上限"),displaySection,out frameRateDropdown);
        PopulateFrameRateOptions();
        deviceHint=MakeText("DeviceHint",displaySection,GameText.Get("ui.settings.device_resolution", "分辨率跟随当前设备"),18,ResolveLabelFont(),Muted,TextAnchor.MiddleRight); labelTexts.Add(deviceHint);
        footerRule=MakeRect("FooterRule",panelRect); var rule=footerRule.gameObject.AddComponent<MapMerchantFrameGraphic>(); rule.style=MapMerchantFrameGraphic.Style.Divider; rule.raycastTarget=false;
        footerHint=MakeText("FooterHint",panelRect,GameText.Get("ui.settings.hint", "保存设置后生效，返回将放弃本次修改"),22,ResolveLabelFont(),Muted,TextAnchor.MiddleCenter); labelTexts.Add(footerHint);
        saveButton=BuildButton("SaveButton",GameText.Get("ui.settings.save", "保存设置"),true,HandleSaveClicked);
        closeButton=BuildButton("CloseButton",GameText.Get("ui.back", "返回"),false,Hide);
        closeXButton=BuildButton("CloseX","×",false,Hide);
        musicSlider.onValueChanged.AddListener(_=>RefreshSliderTexts()); sfxSlider.onValueChanged.AddListener(_=>RefreshSliderTexts());
        ApplyConfiguredFonts(); root.SetActive(false);
    }

    private void RefreshLocalizedText()
    {
        titleText.text = gameplayOnly ? GameText.Get("ui.settings.in_run", "局内设置") : GameText.Get("ui.settings.title", "设 置");
        subtitleText.text = gameplayOnly ? GameText.Get("ui.settings.in_run_subtitle", "战斗 · 阅读 · 反馈")
            : GameText.Get("ui.settings.subtitle", "声音 · 画面 · 游玩");
        SetLocalizedText(audioSection, "Heading", "ui.settings.sound", "声音");
        SetLocalizedText(displaySection, "Heading", "ui.settings.display", "画面与性能");
        SetLocalizedText(gameplaySection, "Heading", "ui.settings.gameplay", "游玩与反馈");
        SetLocalizedText(musicRow, "Label", "ui.settings.music", "音乐音量");
        SetLocalizedText(sfxRow, "Label", "ui.settings.sfx", "音效音量");
        SetLocalizedText(battleAdvanceRow, "Label", "ui.settings.battle_advance", "战斗推进");
        SetLocalizedText(detailSizeRow, "Label", "ui.settings.detail_size", "详情字号");
        SetLocalizedText(vibrationRow, "Label", "ui.settings.vibration", "振动反馈");
        SetLocalizedText(languageRow, "Label", "ui.settings.language", "语言");
        SetLocalizedText(fullscreenRow, "Label", "ui.settings.fullscreen", "全屏显示");
        SetLocalizedText(qualityRow, "Label", "ui.settings.quality", "画质预设");
        SetLocalizedText(resolutionRow, "Label", "ui.settings.resolution", "分辨率");
        SetLocalizedText(frameRateRow, "Label", "ui.settings.frame_limit", "帧率上限");
        SetLocalizedText(saveButton.transform, "Label", "ui.settings.save", "保存设置");
        SetLocalizedText(closeButton.transform, "Label", "ui.back", "返回");
        footerHint.text = GameText.Get("ui.settings.hint", "保存设置后生效，返回将放弃本次修改");
        deviceHint.text = GameText.Get("ui.settings.device_resolution", "分辨率跟随当前设备");

        battleAdvanceDropdown.ClearOptions();
        battleAdvanceDropdown.AddOptions(new List<string>
        {
            GameText.Get("ui.settings.manual", "手动"), GameText.Get("ui.settings.auto", "自动"), GameText.Get("ui.settings.fast", "快速")
        });
        detailSizeDropdown.ClearOptions();
        detailSizeDropdown.AddOptions(new List<string> { GameText.Get("ui.settings.standard", "标准"), GameText.Get("ui.settings.large", "大字") });
        qualityDropdown.ClearOptions();
        var qualities = new List<string>();
        foreach (string name in QualitySettings.names) qualities.Add(QualityLabel(name));
        qualityDropdown.AddOptions(qualities);
    }

    private static void SetLocalizedText(Transform parent, string child, string key, string source)
    {
        Text label = parent.Find(child).GetComponent<Text>();
        label.text = GameText.Get(key, source);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 18;
        label.resizeTextMaxSize = label.fontSize;
    }

    private RectTransform BuildSection(string name,string heading,MainMenuSettingsGraphic.Part icon)
    {
        RectTransform section=MakeRect(name,panelRect); Skin(section,MainMenuSettingsGraphic.Part.Section);
        RectTransform mark=MakeRect("Icon",section); Skin(mark,icon); Place(mark,24,23,32,32);
        Text label=MakeText("Heading",section,heading,32,ResolveLabelFont(),Gold); labelTexts.Add(label); Place(label.rectTransform,72,16,400,46);
        RectTransform line=MakeRect("SectionRule",section); var graphic=line.gameObject.AddComponent<MapMerchantFrameGraphic>(); graphic.style=MapMerchantFrameGraphic.Style.Divider; graphic.raycastTarget=false;
        return section;
    }

    private RectTransform BuildAudioRow(string name,string label,RectTransform parent,out Slider slider,out Text value)
    {
        RectTransform row=MakeRect(name,parent);
        labelTexts.Add(MakeText("Label",row,label,28,ResolveLabelFont(),Ivory));
        RectTransform badge=MakeRect("ValueBadge",row); Skin(badge,MainMenuSettingsGraphic.Part.Field);
        value=MakeText("Value",badge,"100%",24,ResolveValueFont(),Gold,TextAnchor.MiddleCenter); Stretch(value.rectTransform,4,1); valueTexts.Add(value);
        RectTransform hit=MakeRect(name=="MusicRow"?"MusicSlider":"SfxSlider",row); hit.gameObject.AddComponent<Image>().color=Color.clear;
        RectTransform track=MakeRect("Track",hit); Stretch(track,0,18); Skin(track,MainMenuSettingsGraphic.Part.Track);
        RectTransform fillArea=MakeRect("FillArea",hit); Stretch(fillArea,22,21);
        RectTransform fill=MakeRect("Fill",fillArea); Stretch(fill); Skin(fill,MainMenuSettingsGraphic.Part.Fill);
        RectTransform handleArea=MakeRect("HandleArea",hit); Stretch(handleArea,22,0);
        // Slider stretches the handle on the cross axis; zero delta keeps its hit area in the row.
        RectTransform handle=MakeRect("Handle",handleArea); handle.sizeDelta=new Vector2(44,0);
        MainMenuSettingsGraphic grip=Skin(handle,MainMenuSettingsGraphic.Part.Grip,true);
        slider=hit.gameObject.AddComponent<Slider>(); slider.transition=Selectable.Transition.None; slider.direction=Slider.Direction.LeftToRight;
        slider.minValue=0; slider.maxValue=1; slider.fillRect=fill; slider.handleRect=handle; slider.targetGraphic=grip;
        hit.gameObject.AddComponent<MainMenuSettingsControl>().Bind(slider,grip,handle);
        return row;
    }

    private RectTransform BuildSwitchRow(string name,string label,RectTransform parent,out Toggle toggle)
    {
        RectTransform row=MakeRect(name,parent); labelTexts.Add(MakeText("Label",row,label,28,ResolveLabelFont(),Ivory));
        RectTransform hit=MakeRect(name=="FullscreenRow"?"FullscreenToggle":"VibrationToggle",row); hit.gameObject.AddComponent<Image>().color=Color.clear;
        MainMenuSettingsGraphic face=Skin(MakeRect("SwitchFace",hit),MainMenuSettingsGraphic.Part.Switch); Stretch(face.rectTransform);
        Text state=MakeText("State",hit,GameText.Get("ui.on", "开启"),24,ResolveToggleStateFont(),Ivory,TextAnchor.MiddleCenter); Stretch(state.rectTransform,32,2); toggleStateTexts.Add(state);
        RectTransform knob=MakeRect("Thumb",hit); knob.anchorMin=knob.anchorMax=new Vector2(0,.5f); knob.pivot=Vector2.one*.5f; knob.sizeDelta=new Vector2(40,38);
        Skin(knob,MainMenuSettingsGraphic.Part.Knob);
        toggle=hit.gameObject.AddComponent<Toggle>(); toggle.transition=Selectable.Transition.None; toggle.targetGraphic=face;
        var feedback=hit.gameObject.AddComponent<MainMenuSettingsControl>(); feedback.Bind(toggle,face,knob,state); toggle.onValueChanged.AddListener(_=>feedback.Refresh());
        return row;
    }

    private RectTransform BuildDropdownRow(string name,string label,RectTransform parent,out Dropdown dropdown)
    {
        RectTransform row=MakeRect(name,parent); labelTexts.Add(MakeText("Label",row,label,28,ResolveLabelFont(),Ivory));
        RectTransform hit=MakeRect(name.Replace("Row","Dropdown"),row); hit.gameObject.AddComponent<Image>().color=Color.clear;
        MainMenuSettingsGraphic face=Skin(MakeRect("FieldFace",hit),MainMenuSettingsGraphic.Part.Field); Stretch(face.rectTransform);
        Text caption=MakeText("Label",hit,string.Empty,24,ResolveDropdownFont(),Ivory); Stretch(caption.rectTransform,18,4); caption.rectTransform.offsetMax=new Vector2(-50,-4);
        caption.resizeTextForBestFit=true; caption.resizeTextMinSize=18; caption.resizeTextMaxSize=24; caption.horizontalOverflow=HorizontalWrapMode.Overflow;
        RectTransform arrow=MakeRect("Arrow",hit); arrow.anchorMin=arrow.anchorMax=new Vector2(1,.5f); arrow.anchoredPosition=new Vector2(-25,0); arrow.sizeDelta=new Vector2(26,26); Skin(arrow,MainMenuSettingsGraphic.Part.Chevron);
        dropdown=hit.gameObject.AddComponent<Dropdown>(); dropdown.transition=Selectable.Transition.None; dropdown.targetGraphic=face; dropdown.captionText=caption; dropdownTexts.Add(caption);
        RectTransform template=MakeRect("Template",hit); template.anchorMin=new Vector2(0,0); template.anchorMax=new Vector2(1,0); template.pivot=new Vector2(.5f,1); template.anchoredPosition=new Vector2(0,-8); template.sizeDelta=new Vector2(0,250);
        template.gameObject.AddComponent<Image>().color=new Color32(10,26,17,255); var rim=Skin(MakeRect("PopupFrame",template),MainMenuSettingsGraphic.Part.Field); Stretch(rim.rectTransform);
        ScrollRect scroll=template.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=32;
        RectTransform viewport=MakeRect("Viewport",template); Stretch(viewport,8,8); viewport.offsetMax=new Vector2(-19,-8); viewport.gameObject.AddComponent<RectMask2D>(); viewport.gameObject.AddComponent<Image>().color=Color.clear;
        RectTransform content=MakeRect("Content",viewport); content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(.5f,1); content.sizeDelta=new Vector2(0,52);
        RectTransform item=MakeRect("Item",content); item.anchorMin=new Vector2(0,.5f); item.anchorMax=new Vector2(1,.5f); item.sizeDelta=new Vector2(0,48);
        Image itemFace=item.gameObject.AddComponent<Image>(); itemFace.color=new Color32(23,47,31,255);
        RectTransform check=MakeRect("ItemCheckmark",item); check.anchorMin=check.anchorMax=new Vector2(0,.5f); check.anchoredPosition=new Vector2(20,0); check.sizeDelta=new Vector2(22,22);
        MainMenuSettingsGraphic jewel=Skin(check,MainMenuSettingsGraphic.Part.Check);
        Text itemLabel=MakeText("ItemLabel",item,GameText.Get("ui.settings.options", "选项"),24,ResolveDropdownFont(),Ivory); Stretch(itemLabel.rectTransform,42,2); itemLabel.rectTransform.offsetMax=new Vector2(-12,-2); dropdownTexts.Add(itemLabel);
        Toggle itemToggle=item.gameObject.AddComponent<Toggle>(); itemToggle.targetGraphic=itemFace; itemToggle.graphic=jewel;
        ColorBlock colors=itemToggle.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.6f,1.65f,1.4f); colors.selectedColor=colors.highlightedColor; colors.pressedColor=new Color(.8f,.9f,.8f); itemToggle.colors=colors;
        RectTransform track=MakeRect("Scrollbar",template); track.anchorMin=new Vector2(1,0); track.anchorMax=Vector2.one; track.pivot=new Vector2(1,.5f); track.offsetMin=new Vector2(-13,10); track.offsetMax=new Vector2(-7,-10);
        track.gameObject.AddComponent<Image>().color=new Color32(24,39,25,255); Scrollbar bar=track.gameObject.AddComponent<Scrollbar>(); bar.direction=Scrollbar.Direction.BottomToTop;
        RectTransform barHandle=MakeRect("Handle",track); Stretch(barHandle); Image barImage=barHandle.gameObject.AddComponent<Image>(); barImage.color=new Color32(157,137,74,255); bar.handleRect=barHandle; bar.targetGraphic=barImage;
        scroll.content=content; scroll.viewport=viewport; scroll.verticalScrollbar=bar; dropdown.template=template; dropdown.itemText=itemLabel;
        template.gameObject.SetActive(false); hit.gameObject.AddComponent<MainMenuSettingsControl>().Bind(dropdown,face);
        return row;
    }

    private Button BuildButton(string name,string label,bool primary,UnityEngine.Events.UnityAction clicked)
    {
        RectTransform root=MakeRect(name,panelRect); root.gameObject.AddComponent<Image>().color=Color.clear;
        MapCraftingFrameGraphic face=MakeRect("ButtonFace",root).gameObject.AddComponent<MapCraftingFrameGraphic>(); Stretch(face.rectTransform);
        face.craftingStyle=primary?MapCraftingFrameGraphic.Style.Secondary:MapCraftingFrameGraphic.Style.Action; face.raycastTarget=false;
        Button button=root.gameObject.AddComponent<Button>(); button.targetGraphic=face; button.transition=Selectable.Transition.None; button.onClick.AddListener(clicked);
        Text text=MakeText("Label",root,label,name=="CloseX"?34:30,ResolveButtonFont(),primary?new Color32(62,42,19,255):Ivory,TextAnchor.MiddleCenter); Stretch(text.rectTransform,10,2); buttonTexts.Add(text);
        root.gameObject.AddComponent<MapCraftingButtonFeedback>(); return button;
    }

    public void ApplyViewport(Vector2 viewport,Rect safePixels)
    {
        if(canvas==null || viewport.x<=0 || viewport.y<=0) return;
        if(safePixels.width<=0 || safePixels.height<=0) safePixels=new Rect(Vector2.zero,viewport);
        safePixels=Rect.MinMaxRect(Mathf.Clamp(safePixels.xMin,0,viewport.x),Mathf.Clamp(safePixels.yMin,0,viewport.y),Mathf.Clamp(safePixels.xMax,0,viewport.x),Mathf.Clamp(safePixels.yMax,0,viewport.y));
        safeRoot.anchorMin=new Vector2(safePixels.xMin/viewport.x,safePixels.yMin/viewport.y); safeRoot.anchorMax=new Vector2(safePixels.xMax/viewport.x,safePixels.yMax/viewport.y); safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
        Vector2 canvasSize=((RectTransform)canvas.transform).rect.size;
        Vector2 safeSize=new Vector2(canvasSize.x*safePixels.width/viewport.x,canvasSize.y*safePixels.height/viewport.y);
        bool portrait=viewport.x/viewport.y<1.1f;
        float width = gameplayOnly || portrait ? 680 : 1160;
        float height = gameplayOnly ? 574 : portrait ? 1357 : 953;
        panelRect.sizeDelta=new Vector2(width,height); panelRect.anchoredPosition=Vector2.zero;
        panelRect.localScale = Vector3.one * Mathf.Max(0.01f, Mathf.Min(1, (safeSize.x - 40) / width, (safeSize.y - 76) / height));
        Place(titlePlaque,(width-390)/2,-28,390,88);
        Place(subtitleText.rectTransform,40,77,width-80,34);
        Place((RectTransform)closeXButton.transform,width-85,27,54,54);

        bool narrow = gameplayOnly || portrait;
        float sectionX = narrow ? 32 : 44;
        float sectionWidth = narrow ? 616 : 470;
        float gameplayTop = gameplayOnly ? 126 : portrait ? 420 : 430;
        Place(gameplaySection, sectionX, gameplayTop, sectionWidth, gameplayOnly ? 284 : 351);
        LayoutSection(gameplaySection);
        float gameplayWidth = sectionWidth - 48;
        LayoutDropdownRow(battleAdvanceRow, 24, 82, gameplayWidth);
        LayoutDropdownRow(detailSizeRow, 24, 149, gameplayWidth);
        LayoutSwitchRow(vibrationRow, 24, 216, gameplayWidth);

        if (!gameplayOnly)
        {
            LayoutDropdownRow(languageRow, 24, 283, gameplayWidth);
            Place(audioSection, sectionX, portrait ? 124 : 134, sectionWidth, 284);
            LayoutSection(audioSection);
            LayoutAudioRow(musicRow, 24, 78, sectionWidth - 48);
            LayoutAudioRow(sfxRow, 24, 179, sectionWidth - 48);

            float displayWidth = portrait ? 616 : 584;
            Place(displaySection, portrait ? 32 : 532, portrait ? 783 : 134, displayWidth, portrait ? 414 : 647);
            LayoutSection(displaySection);
            LayoutSwitchRow(fullscreenRow, 24, portrait ? 82 : 95, displayWidth - 48);
            LayoutDropdownRow(qualityRow, 24, portrait ? 155 : 201, displayWidth - 48);
            LayoutDropdownRow(resolutionRow, 24, portrait ? 228 : 307, displayWidth - 48);
            LayoutDropdownRow(frameRateRow, 24, portrait ? 301 : 413, displayWidth - 48);
            deviceHint.gameObject.SetActive(!MainMenuSettingsPersistence.SupportsRuntimeResolutionChanges);
            Place(deviceHint.rectTransform, 210, portrait ? 374 : 502, displayWidth - 234, 24);
        }

        float footerTop = gameplayOnly ? 424 : portrait ? 1211 : 802;
        Place(footerRule, 36, footerTop, width - 72, 4);
        Place(footerHint.rectTransform, 40, footerTop + 17, width - 80, 34);
        Place((RectTransform)closeButton.transform, narrow ? 64 : 260, footerTop + 64, narrow ? 240 : 272, 60);
        Place((RectTransform)saveButton.transform, narrow ? 352 : 610, footerTop + 64, narrow ? 264 : 300, 60);
        layoutViewport=viewport; layoutSafe=safePixels; layoutCanvas=canvasSize;
        RefreshSwitches();
    }

    private static void LayoutSection(RectTransform section)
    {
        Place(section.Find("Heading") as RectTransform,72,16,section.rect.width-96,46);
        Place(section.Find("SectionRule") as RectTransform,20,70,section.rect.width-40,3);
    }
    private static void LayoutAudioRow(RectTransform row,float x,float y,float width)
    {
        Place(row,x,y,width,100); Place(row.Find("Label") as RectTransform,0,0,width-100,36);
        Place(row.Find("ValueBadge") as RectTransform,width-86,0,86,36);
        Place(row.Find(row.name=="MusicRow"?"MusicSlider":"SfxSlider") as RectTransform,0,44,width,48);
    }
    private static void LayoutSwitchRow(RectTransform row,float x,float y,float width)
    {
        Place(row,x,y,width,58); Place(row.Find("Label") as RectTransform,0,0,width-210,58);
        Place((RectTransform)row.GetComponentInChildren<Toggle>().transform,width-190,3,190,52);
    }
    private static void LayoutDropdownRow(RectTransform row,float x,float y,float width)
    {
        Place(row,x,y,width,58); Place(row.Find("Label") as RectTransform,0,0,164,58);
        Place((RectTransform)row.GetComponentInChildren<Dropdown>().transform,184,0,width-184,58);
    }
    private void LateUpdate()
    {
        if(!IsVisible) return;
        Vector2 viewport=new Vector2(Screen.width,Screen.height);
        if(viewport!=layoutViewport || Screen.safeArea!=layoutSafe || ((RectTransform)canvas.transform).rect.size!=layoutCanvas) ApplyViewport(viewport,Screen.safeArea);
        if(focusWhenReady&&!IsPageMoving()) { focusWhenReady=false; if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(saveButton.gameObject); }
    }
    private void RememberSelection() { if(!IsVisible&&EventSystem.current!=null) previousSelection=EventSystem.current.currentSelectedGameObject; focusWhenReady=true; }
    private void RestoreSelection() { focusWhenReady=false; if(previousSelection!=null&&previousSelection.activeInHierarchy&&EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(previousSelection); previousSelection=null; }
    private bool IsPageMoving() { var motion=canvas!=null?canvas.GetComponent<MainMenuPageMotion>():null; return motion!=null&&motion.IsTransitioning; }
    private void CloseDropdowns()
    {
        if (qualityDropdown != null) qualityDropdown.Hide();
        if (resolutionDropdown != null) resolutionDropdown.Hide();
        if (frameRateDropdown != null) frameRateDropdown.Hide();
        if (battleAdvanceDropdown != null) battleAdvanceDropdown.Hide();
        if (detailSizeDropdown != null) detailSizeDropdown.Hide();
        if (languageDropdown != null) languageDropdown.Hide();
    }
    private void RefreshSwitches() { if(canvas!=null) foreach(var control in canvas.GetComponentsInChildren<MainMenuSettingsControl>(true)) control.Refresh(true); }
    private static string QualityLabel(string value)
    {
        switch ((value ?? string.Empty).ToLowerInvariant())
        {
            case "very low": return GameText.Get("ui.settings.quality.very_low", "最低");
            case "low": return GameText.Get("ui.settings.quality.low", "低");
            case "medium": return GameText.Get("ui.settings.quality.medium", "中等");
            case "high": return GameText.Get("ui.settings.quality.high", "高");
            case "very high": return GameText.Get("ui.settings.quality.very_high", "很高");
            case "ultra": return GameText.Get("ui.settings.quality.ultra", "极高");
            default: return value;
        }
    }
    private static RectTransform MakeRect(string name,RectTransform parent) { var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false); return rect; }
    private static void Stretch(RectTransform rect,float x=0,float y=0) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=new Vector2(x,y); rect.offsetMax=new Vector2(-x,-y); }
    private static void Place(RectTransform rect,float x,float y,float w,float h) { rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=Vector2.one*.5f; rect.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f); rect.sizeDelta=new Vector2(w,h); }
    private static MainMenuSettingsGraphic Skin(RectTransform rect,MainMenuSettingsGraphic.Part part,bool raycast=false) { var graphic=rect.gameObject.AddComponent<MainMenuSettingsGraphic>(); graphic.part=part; graphic.raycastTarget=raycast; return graphic; }
    private static Text MakeText(string name,RectTransform parent,string value,int size,Font font,Color tint,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        var text=MakeRect(name,parent).gameObject.AddComponent<ResourceKeywordLegacyText>(); text.text=value; text.font=font; text.fontSize=size;
        text.color=tint; text.alignment=alignment; text.raycastTarget=false; text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate; return text;
    }
}
