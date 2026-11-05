using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Receipts survive scene snapshots; the modal only acknowledges already awarded items.</summary>
[DisallowMultipleComponent]
public sealed class RunItemAcquisitionOverlay : MonoBehaviour
{
    private static RunItemAcquisitionOverlay instance;
    private Canvas canvas;
    private MapInventoryDetailView detail;
    private RunState boundState, displayedState;
    private MonoBehaviour mapOwner;
    private Func<bool> mapAvailability;
    private RunItemAcquisitionQueue displayedQueue;
    private RunItemAcquisitionBatch current;
    private TMP_FontAsset keywordFont, bodyFont;
    private SceneCurtainTransitionController curtain;
    private bool globalBinding, suppressHidden;
    private float shownAt;
    private int remainingShown = -1;

    public static bool IsBlockingInput => instance != null && instance.IsVisible;
    public bool IsVisible => detail != null && detail.IsVisible;
    public MapInventoryDetailView DetailView => detail;
    public Canvas PresentationCanvas => canvas;

    public static void BindToMap(MonoBehaviour owner, RunState state, TMP_FontAsset keyword, TMP_FontAsset body, Func<bool> canPresent)
    {
        if (!Application.isPlaying || owner == null || !owner.isActiveAndEnabled || state == null) return;
        if (instance == null)
        {
            var root = new GameObject("ItemAcquisitionOverlay");
            instance = root.AddComponent<RunItemAcquisitionOverlay>();
            DontDestroyOnLoad(root);
        }
        instance.Configure(state, keyword, body);
        instance.globalBinding = true;
        instance.mapOwner = owner;
        instance.mapAvailability = canPresent;
        if (instance.curtain == null) instance.curtain = FindObjectOfType<SceneCurtainTransitionController>(true);
    }

    // Also used by isolated previews and interaction tests without creating a global presenter.
    public void Configure(RunState state, TMP_FontAsset keyword, TMP_FontAsset body)
    {
        EnsureUi();
        if (boundState != state) SuspendCurrent();
        boundState = state;
        if (keywordFont != keyword || bodyFont != body)
        {
            keywordFont = keyword; bodyFont = body;
            detail.ConfigureFonts(keyword, body);
        }
        detail.ConfigureInventoryContext(state);
    }

    private void EnsureUi()
    {
        if (detail != null) return;
        EventSystemUtility.EnsureSingleEventSystem();
        var root = new GameObject("AcquisitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9500;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;
        detail = MapInventoryDetailView.Create((RectTransform)root.transform, "AcquiredItemDetail");
        detail.DismissAllowed = () => !Application.isPlaying || Time.unscaledTime - shownAt >= 0.18f;
        detail.Hidden += HandleHidden;
    }

    private void LateUpdate()
    {
        ProcessPending();
        if (IsVisible && Input.GetKeyDown(KeyCode.Escape)) detail.Hide();
    }

    public void ProcessPending()
    {
        if (detail == null) return;
        bool restScene;
        RunState state = ResolveState(out restScene);
        if (state == null || (globalBinding && curtain != null && curtain.IsTransitioning))
        {
            SuspendCurrent();
            return;
        }
        if (IsVisible)
        {
            if (displayedState != state) SuspendCurrent();
            else
            {
                int remaining = displayedQueue.CountAfter(current);
                if (remaining != remainingShown)
                {
                    detail.RefreshAcquisitionPresentation(current, remaining);
                    remainingShown = remaining;
                }
                return;
            }
        }
        if (globalBinding && (restScene ? RestSceneModalState.IsLocked : mapAvailability != null && !mapAvailability())) return;
        RunItemAcquisitionBatch next;
        if (!state.ItemAcquisitions.TryGetNext(out next)) return;
        if (restScene && !RestSceneModalState.TryAcquire(this)) return;
        current = next; displayedState = state; displayedQueue = state.ItemAcquisitions;
        shownAt = Time.unscaledTime;
        remainingShown = displayedQueue.CountAfter(current);
        detail.ConfigureInventoryContext(state);
        detail.ShowAcquisition(current, remainingShown);
        if (EventSystem.current != null)
        {
            Transform button = detail.transform.Find("CloseButton");
            if (button != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private RunState ResolveState(out bool restScene)
    {
        restScene = false;
        if (!globalBinding) return boundState;
        Scene active = SceneManager.GetActiveScene();
        if (mapOwner != null && mapOwner.isActiveAndEnabled && mapOwner.gameObject.scene == active) return boundState;
        if (RestSceneRuntime.HasPendingVisit && active.name != RestSceneRuntime.ReturnSceneName)
        {
            restScene = true;
            return MapRunRuntime.GetRunStateForAcquisitionDisplay(RestSceneRuntime.ReturnSceneName);
        }
        return null;
    }

    private void HandleHidden()
    {
        RestSceneModalState.Release(this);
        if (suppressHidden) return;
        displayedQueue?.Acknowledge(current);
        current = null; displayedState = null; displayedQueue = null;
        ProcessPending();
    }

    private void SuspendCurrent()
    {
        suppressHidden = true;
        if (detail != null && detail.IsVisible) detail.HideImmediately();
        suppressHidden = false;
        RestSceneModalState.Release(this);
        current = null; displayedState = null; displayedQueue = null;
    }

    private void OnDisable() => SuspendCurrent();
    private void OnDestroy()
    {
        SuspendCurrent();
        if (instance == this) instance = null;
    }
}
