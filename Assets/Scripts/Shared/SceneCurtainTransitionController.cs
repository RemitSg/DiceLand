using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class SceneCurtainTransitionController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float curtainCloseDuration = 0.32f;
    [SerializeField] private float fullBlackHoldDuration = 0.14f;
    [SerializeField] private float curtainOpenDuration = 0.34f;

    [Header("Curtain")]
    [SerializeField] private Color curtainColor = Color.black;
    [SerializeField] private float loadingTextDelay = 0.08f;
    [SerializeField] private TMP_FontAsset loadingFont;
    [SerializeField] private string loadingText = "Loading Battle...";

    private static SceneCurtainTransitionController instance;

    private Canvas canvas;
    private RectTransform rootRect;
    private Image leftCurtain;
    private Image rightCurtain;
    private Image blackout;
    private TextMeshProUGUI loadingLabel;
    private Coroutine transitionRoutine;
    private bool isTransitioning;

    public static SceneCurtainTransitionController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SceneCurtainTransitionController>(true);
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneCurtainTransitionController");
                    instance = go.AddComponent<SceneCurtainTransitionController>();
                }
            }

            return instance;
        }
    }

    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
        SetOverlayVisible(false);
    }

    public void TransitionToScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneCurtainTransitionController] Scene name is empty.");
            return;
        }

        EnsureOverlay();

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;
        SetOverlayVisible(true);
        blackout.gameObject.SetActive(false);
        yield return AnimateCurtains(0f, 1f, curtainCloseDuration);

        blackout.color = curtainColor;
        blackout.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(fullBlackHoldDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            Debug.LogWarning($"[SceneCurtainTransitionController] Failed to load scene: {sceneName}");
            SetOverlayVisible(false);
            isTransitioning = false;
            transitionRoutine = null;
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        bool loadingTextShown = false;
        float labelTimer = 0f;
        while (!loadOperation.isDone)
        {
            if (!loadingTextShown)
            {
                labelTimer += Time.unscaledDeltaTime;
                if (labelTimer >= loadingTextDelay)
                {
                    loadingTextShown = true;
                    if (loadingLabel != null)
                        loadingLabel.gameObject.SetActive(true);
                }
            }

            if (loadOperation.progress >= 0.9f)
            {
                loadOperation.allowSceneActivation = true;
            }

            yield return null;

            if (loadOperation.isDone)
                break;
        }

        yield return null;
        EnsureOverlay();
        SetCurtainProgress(1f);
        blackout.color = curtainColor;
        blackout.gameObject.SetActive(true);
        if (loadingLabel != null)
            loadingLabel.gameObject.SetActive(false);
        PrepareLoadedSceneForReveal();
        yield return WaitForSceneRenderReady();
        blackout.gameObject.SetActive(false);
        yield return AnimateCurtains(1f, 0f, curtainOpenDuration);

        SetOverlayVisible(false);
        isTransitioning = false;
        transitionRoutine = null;
    }

    private IEnumerator AnimateCurtains(float from, float to, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            SetCurtainProgress(Mathf.Lerp(from, to, eased));
            yield return null;
        }

        SetCurtainProgress(to);
    }

    private IEnumerator WaitForSceneRenderReady()
    {
        int guardFrames = 0;
        while (!HasRenderableSceneCamera() && guardFrames < 12)
        {
            guardFrames++;
            yield return null;
        }

        yield return null;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
    }

    private void PrepareLoadedSceneForReveal()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        List<Camera> sceneCameras = CollectSceneCameras(activeScene);
        for (int i = 0; i < sceneCameras.Count; i++)
        {
            Camera cam = sceneCameras[i];
            if (cam == null)
                continue;

            cam.rect = new Rect(0f, 0f, 1f, 1f);

            if (cam.clearFlags == CameraClearFlags.Nothing || cam.clearFlags == CameraClearFlags.Depth)
                cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    private static List<Camera> CollectSceneCameras(Scene scene)
    {
        List<Camera> cameras = new List<Camera>();
        if (!scene.IsValid() || !scene.isLoaded)
            return cameras;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            Camera[] rootCameras = root.GetComponentsInChildren<Camera>(true);
            for (int j = 0; j < rootCameras.Length; j++)
            {
                Camera cam = rootCameras[j];
                if (cam == null || cameras.Contains(cam))
                    continue;

                cameras.Add(cam);
            }
        }

        return cameras;
    }

    private static bool HasRenderableSceneCamera()
    {
        List<Camera> cameras = CollectSceneCameras(SceneManager.GetActiveScene());
        for (int i = 0; i < cameras.Count; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || !cam.isActiveAndEnabled)
                continue;

            if (cam.targetTexture != null)
                continue;

            if (cam.clearFlags == CameraClearFlags.Nothing)
                continue;

            Rect rect = cam.rect;
            if (rect.width <= 0.001f || rect.height <= 0.001f)
                continue;

            return true;
        }

        return false;
    }

    private void SetCurtainProgress(float progress)
    {
        EnsureOverlay();
        progress = Mathf.Clamp01(progress);
        float halfWidth = rootRect.rect.width * 0.5f;
        float height = rootRect.rect.height;
        float curtainWidth = Mathf.Ceil(halfWidth * progress);

        SetCurtainRect(leftCurtain.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(curtainWidth, height), new Vector2(curtainWidth * 0.5f, 0f));
        SetCurtainRect(rightCurtain.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(curtainWidth, height), new Vector2(-curtainWidth * 0.5f, 0f));

    }

    private static void SetCurtainRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    private void EnsureOverlay()
    {
        if (canvas != null)
            return;

        GameObject canvasGo = new GameObject("CurtainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20000;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        rootRect = canvasGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        blackout = CreateImage("Blackout", rootRect, curtainColor);
        blackout.rectTransform.anchorMin = Vector2.zero;
        blackout.rectTransform.anchorMax = Vector2.one;
        blackout.rectTransform.offsetMin = Vector2.zero;
        blackout.rectTransform.offsetMax = Vector2.zero;
        blackout.gameObject.SetActive(false);

        leftCurtain = CreateImage("LeftCurtain", rootRect, curtainColor);
        rightCurtain = CreateImage("RightCurtain", rootRect, curtainColor);

        loadingLabel = CreateText("LoadingLabel", rootRect);
        loadingLabel.text = loadingText;
        loadingLabel.color = new Color(1f, 1f, 1f, 0.92f);
        loadingLabel.fontSize = 42f;
        loadingLabel.alignment = TextAlignmentOptions.Center;
        loadingLabel.gameObject.SetActive(false);
        RectTransform labelRect = loadingLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, -220f);
        labelRect.sizeDelta = new Vector2(900f, 100f);
    }

    private void SetOverlayVisible(bool visible)
    {
        EnsureOverlay();
        canvas.gameObject.SetActive(visible);
        if (!visible)
        {
            if (loadingLabel != null)
                loadingLabel.gameObject.SetActive(false);
            if (blackout != null)
                blackout.gameObject.SetActive(false);
            SetCurtainProgress(0f);
        }
    }

    private Image CreateImage(string name, RectTransform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (loadingFont != null)
            text.font = loadingFont;
        text.enableWordWrapping = false;
        return text;
    }
}
