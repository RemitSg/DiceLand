using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Fits the authored sprite layers and signs together; visit rules stay in the original controllers.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RestSceneForestView : MonoBehaviour
{
    public enum Station { ActionPoints, Forge, Recruit, Rest }

    [Serializable]
    public sealed class StationVisual
    {
        public Station station;
        public RestSceneStationTarget plaque;
        public Image frame;
        public Text label;
        public Graphic icon;
        [NonSerialized] public float focus;
    }

    public static readonly Vector2 ReferenceSize = new Vector2(1672f, 941f);
    private static readonly Color Gold = new Color(1f, 0.82f, 0.39f);
    [SerializeField] private RestSceneActionPointRestoreInteractable actionPoints;
    [SerializeField] private RestSceneReforgeInteractable forge;
    [SerializeField] private RestSceneRecruitInteractable recruit;
    [SerializeField] private RestSceneInteractable rest;
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private Transform worldComposition;
    [SerializeField] private SpriteRenderer ambientBackground;
    [SerializeField] private RectTransform canvasRect, composition;
    [SerializeField] private CanvasGroup interactionGroup;
    [SerializeField] private Sprite modalFrame;
    [SerializeField] private Text stationHeading;
    [SerializeField] private StationVisual[] stations = Array.Empty<StationVisual>();
    private Vector2 lastCanvasSize, lastViewport;
    private Rect lastSafeArea;
    private bool wasLocked;

    public RectTransform Composition => composition;
    public Transform WorldComposition => worldComposition;
    public StationVisual[] Stations => stations;

    public void Configure(RestSceneActionPointRestoreInteractable ap, RestSceneReforgeInteractable forging,
        RestSceneRecruitInteractable recruiting, RestSceneInteractable resting, Camera camera,
        Transform world, SpriteRenderer ambient, RectTransform canvas, RectTransform signs,
        CanvasGroup interactions, Sprite windowFrame, StationVisual[] visuals)
    {
        actionPoints = ap; forge = forging; recruit = recruiting; rest = resting;
        sceneCamera = camera; worldComposition = world; ambientBackground = ambient;
        canvasRect = canvas; composition = signs; interactionGroup = interactions;
        modalFrame = windowFrame; stations = visuals;
        RefreshLayout(true);
    }

    private void OnEnable() { RefreshLayout(true); }

    private void OnDisable()
    {
        foreach (StationVisual visual in stations)
            if (visual != null && Controller(visual.station) is IRestSceneStationPresentation target)
                target.SetPresentationFocus(false);
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        if (GetComponent<RestSceneInventory>() == null) gameObject.AddComponent<RestSceneInventory>();
        LocalizeSigns();
        StyleExistingModals();
        RefreshInteractionState();
    }

    private void LocalizeSigns()
    {
        if (stationHeading != null)
        {
            stationHeading.text = GameText.Get("rest.station.title", stationHeading.text);
            stationHeading.resizeTextForBestFit = true;
            stationHeading.resizeTextMaxSize = stationHeading.fontSize;
            stationHeading.resizeTextMinSize = 20;
        }
        foreach (StationVisual visual in stations)
        {
            if (visual?.label == null) continue;
            visual.label.text = GameText.Content("rest.station", visual.station.ToString(), visual.label.text);
            visual.label.resizeTextForBestFit = true;
            visual.label.resizeTextMaxSize = visual.label.fontSize;
            visual.label.resizeTextMinSize = 18;
        }
    }

    private void LateUpdate()
    {
        RefreshLayout(false);
        if (!Application.isPlaying) return;
        RefreshInteractionState();
        bool available = RestSceneRuntime.HasPendingVisit && !RestSceneModalState.IsLocked;
        foreach (StationVisual visual in stations)
        {
            if (visual == null || visual.plaque == null) continue;
            Behaviour controller = Controller(visual.station);
            bool enabled = controller != null && controller.isActiveAndEnabled;
            IRestSceneStationPresentation target = controller as IRestSceneStationPresentation;
            bool signFocus = available && enabled && visual.plaque.HasFocus;
            target?.SetPresentationFocus(signFocus);
            bool focused = available && enabled && (signFocus || (target != null && target.HasInteractionFocus));
            bool pressed = focused && visual.plaque.IsPointerPressed;
            visual.focus = Mathf.MoveTowards(visual.focus, focused ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            visual.plaque.interactable = available && enabled;
            visual.plaque.transform.localScale = Vector3.one * (1f + visual.focus * 0.025f - (pressed ? 0.025f : 0f));
            if (visual.frame != null) visual.frame.color = Color.Lerp(new Color(0.96f, 0.96f, 0.96f), Color.white, visual.focus);
            if (visual.label != null) visual.label.color = Color.Lerp(Gold, new Color(1f, 0.96f, 0.74f), visual.focus);
            if (visual.icon != null) visual.icon.color = visual.label != null ? visual.label.color : Gold;
        }
        bool locked = RestSceneModalState.IsLocked;
        if (wasLocked && !locked && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        wasLocked = locked;
    }

    public Behaviour Controller(Station station)
    {
        switch (station)
        {
            case Station.ActionPoints: return actionPoints;
            case Station.Forge: return forge;
            case Station.Recruit: return recruit;
            case Station.Rest: return rest;
            default: return null;
        }
    }

    public bool TryActivateStation(Station station)
    {
        Behaviour controller = Controller(station);
        if (!Application.isPlaying || !isActiveAndEnabled || !RestSceneRuntime.HasPendingVisit || RestSceneModalState.IsLocked ||
            controller == null || !controller.isActiveAndEnabled || controller.gameObject.scene != gameObject.scene) return false;
        switch (station)
        {
            case Station.ActionPoints: actionPoints.ShowPrompt(); break;
            case Station.Forge: forge.ShowOpenPrompt(); break;
            case Station.Recruit: recruit.ShowPanel(); break;
            case Station.Rest: rest.ShowPrompt(); break;
        }
        RefreshInteractionState();
        return RestSceneModalState.IsLockedBy(controller);
    }

    private void RefreshInteractionState()
    {
        bool available = RestSceneRuntime.HasPendingVisit && !RestSceneModalState.IsLocked;
        if (interactionGroup == null) return;
        interactionGroup.interactable = available;
        interactionGroup.blocksRaycasts = available;
    }

    public void RefreshLayout(bool force)
    {
        if (canvasRect == null || composition == null || sceneCamera == null) return;
        Vector2 size = canvasRect.rect.size;
        Vector2 viewport = new Vector2(Screen.width, Screen.height);
        Rect safe = Screen.safeArea;
        if (!force && size == lastCanvasSize && viewport == lastViewport && safe == lastSafeArea) return;
        lastCanvasSize = size; lastViewport = viewport; lastSafeArea = safe;
        ApplyLayout(viewport, safe);
    }

    public void ApplyLayout(Vector2 viewportPixels, Rect safeArea)
    {
        if (canvasRect == null || composition == null || sceneCamera == null) return;
        Vector2 size = canvasRect.rect.size;
        if (size.x <= 0f || size.y <= 0f || viewportPixels.x <= 0f || viewportPixels.y <= 0f) return;
        if (safeArea.width <= 0f || safeArea.height <= 0f) safeArea = new Rect(Vector2.zero, viewportPixels);
        Rect usable = new Rect((safeArea.x / viewportPixels.x - 0.5f) * size.x,
            (safeArea.y / viewportPixels.y - 0.5f) * size.y,
            safeArea.width / viewportPixels.x * size.x, safeArea.height / viewportPixels.y * size.y);
        Rect fitted = FitComposition(usable, ReferenceSize);
        composition.anchoredPosition = fitted.center;
        composition.sizeDelta = ReferenceSize;
        composition.localScale = Vector3.one * (fitted.width / ReferenceSize.x);
        if (worldComposition != null)
        {
            Vector3 viewportCenter = new Vector3(fitted.center.x / size.x + 0.5f, fitted.center.y / size.y + 0.5f,
                -sceneCamera.transform.position.z);
            worldComposition.position = sceneCamera.ViewportToWorldPoint(viewportCenter);
            worldComposition.localScale = Vector3.one * (sceneCamera.orthographicSize * 2f / size.y * fitted.width / ReferenceSize.x * 100f);
        }
        if (ambientBackground != null && ambientBackground.sprite != null)
        {
            ambientBackground.transform.position = new Vector3(sceneCamera.transform.position.x, sceneCamera.transform.position.y, 1f);
            Vector2 bounds = ambientBackground.sprite.bounds.size;
            float scale = Mathf.Max(sceneCamera.orthographicSize * 2f / bounds.y,
                sceneCamera.orthographicSize * 2f * sceneCamera.aspect / bounds.x);
            ambientBackground.transform.localScale = Vector3.one * scale;
        }
    }

    public static Rect FitComposition(Rect viewport, Vector2 artSize)
    {
        if (viewport.width <= 0 || viewport.height <= 0 || artSize.x <= 0 || artSize.y <= 0) return new Rect(viewport.center, Vector2.zero);
        float scale = Mathf.Min(viewport.width / artSize.x, viewport.height / artSize.y);
        Vector2 size = artSize * scale;
        return new Rect(viewport.center - size * 0.5f, size);
    }

    private void StyleExistingModals()
    {
        if (modalFrame == null) return;
        foreach (StationVisual visual in stations)
        {
            if (visual == null) continue;
            Behaviour controller = Controller(visual.station);
            if (controller == null) continue;
            foreach (Canvas canvas in controller.GetComponentsInChildren<Canvas>(true))
            {
                RectTransform canvasTransform = (RectTransform)canvas.transform;
                if (canvasTransform.Find("ForestModalShade") != null) continue;
                RectTransform shade = new GameObject("ForestModalShade", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                shade.SetParent(canvasTransform, false);
                shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
                shade.GetComponent<Image>().color = new Color(0.008f, 0.02f, 0.015f, 0.64f);
                shade.SetAsFirstSibling();
                foreach (Image panel in canvas.GetComponentsInChildren<Image>(true))
                {
                    if (panel.transform.parent != canvas.transform || panel.name == "ForestModalShade") continue;
                    RectTransform rim = new GameObject("ForestWindowFrame", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    rim.SetParent(panel.transform, false); rim.anchorMin = Vector2.zero; rim.anchorMax = Vector2.one;
                    rim.offsetMin = rim.offsetMax = Vector2.zero;
                    Image frame = rim.GetComponent<Image>(); frame.sprite = modalFrame; frame.type = Image.Type.Sliced;
                    frame.pixelsPerUnitMultiplier = 3.5f; frame.raycastTarget = false; rim.SetAsFirstSibling();
                }
            }
        }
    }
}
