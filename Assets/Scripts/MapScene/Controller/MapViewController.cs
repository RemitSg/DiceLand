using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public struct MapCameraPreviewState
{
    public Vector3 position;
    public Quaternion rotation;
    public bool orthographic;
    public float orthographicSize;
    public float fieldOfView;
}

[DisallowMultipleComponent]
public class MapViewController : MonoBehaviour
{
    private const float MinimumTransitionDeltaTime = 0.0001f;
    private const float MinimumPlaneIntersectionDistance = 0.0001f;
    private const float DefaultPreviewZoomMultiplier = 1.75f;
    private const float DefaultPreviewBaseDistance = 7f;
    private const float DefaultPreviewApproachOffset = 1.2f;
    private const float DefaultPreviewFieldOfView = 34f;

    [Header("Zoom")]
    [SerializeField] private float minZoomSize = 3f;
    [SerializeField] private float maxZoomMultiplier = 1.45f;
    [SerializeField] private float mouseWheelZoomStep = 1.4f;
    [SerializeField] private float pinchZoomSensitivity = 0.01f;
    [SerializeField] private float zoomSmoothTime = 0.12f;

    [Header("Pan")]
    [SerializeField] private float dragThresholdPixels = 14f;
    [SerializeField] private float longPressMoveTolerance = 12f;
    [SerializeField] private float panSmoothTime = 0.1f;

    [Header("Inspect")]
    [SerializeField] private float longPressDuration = 0.45f;

    [Header("Spawn Intro")]
    [SerializeField] private bool playSpawnIntro = true;
    [SerializeField] private Vector3 spawnIntroFocusOffset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float spawnIntroZoomSize = 3.4f;
    [SerializeField] private float spawnIntroHoldDuration = 0.45f;
    [SerializeField] private float spawnIntroTravelDuration = 0.95f;
    [SerializeField] private AnimationCurve spawnIntroCurve = null;

    [Header("Battle Preview")]
    [SerializeField] private float previewBaseDistance = 7f;
    [SerializeField] private float previewApproachOffset = 1.2f;
    [SerializeField] private float previewFieldOfView = 34f;
    [SerializeField] private float previewEnterDuration = 0.8f;
    [SerializeField] private float previewExitDuration = 0.3f;

    private Camera targetCamera;
    private BoardManager boardManager;
    private float boardPadding;
    private float maxZoomSize;
    private bool configured;
    private Vector3 targetCameraPosition;
    private float targetZoomSize;
    private Vector3 panSmoothVelocity;
    private float zoomSmoothVelocity;

    private bool pointerActive;
    private Vector2 pointerStartScreenPosition;
    private Vector2 lastPointerScreenPosition;
    private float pointerStartTime;
    private BoardTileData pressedTile;
    private bool dragging;
    private bool longPressTriggered;
    private Coroutine introRoutine;
    private Func<bool> inputBlockedEvaluator;
    private Func<bool> pointerOverUiIgnoredEvaluator;
    private bool battlePreviewActive;
    private bool hasPreBattlePreviewState;
    private MapCameraPreviewState preBattlePreviewState;
    private Vector3 preBattleTargetCameraPosition;
    private float preBattleTargetZoomSize;
    private int previewOperationGeneration;

    public event Action<BoardTileData> TileLongPressed;
    public event Action ViewNavigationPerformed;

    public bool IsBattlePreviewActive => battlePreviewActive;
    public bool IsSpawnIntroActive => introRoutine != null;
    public float DefaultPreviewEnterDuration => previewEnterDuration;
    public float DefaultPreviewExitDuration => previewExitDuration;

    public void SetInputBlockedEvaluator(Func<bool> evaluator)
    {
        inputBlockedEvaluator = evaluator;
    }

    public void SetPointerOverUiIgnoredEvaluator(Func<bool> evaluator)
    {
        pointerOverUiIgnoredEvaluator = evaluator;
    }

    public void Initialize(Camera mapCamera, BoardManager board, float padding)
    {
        InvalidatePreviewOperations();
        if (targetCamera != null && battlePreviewActive && hasPreBattlePreviewState)
            CompleteBattlePreviewRestore();

        configured = false;
        battlePreviewActive = false;
        hasPreBattlePreviewState = false;
        targetCamera = mapCamera;
        boardManager = board;
        boardPadding = Mathf.Max(0f, padding);

        if (targetCamera == null || boardManager == null || boardManager.BoardSize <= 0)
            return;

        float halfBoard = boardManager.BoardHalfExtentWorld;
        float initialZoom = halfBoard + boardPadding;
        maxZoomSize = Mathf.Max(initialZoom * Mathf.Max(1f, maxZoomMultiplier), minZoomSize + 1f);

        targetCamera.orthographic = true;
        targetCameraPosition = new Vector3(0f, 0f, -10f);
        targetZoomSize = initialZoom;
        panSmoothVelocity = Vector3.zero;
        zoomSmoothVelocity = 0f;
        targetCamera.transform.position = targetCameraPosition;
        targetCamera.orthographicSize = targetZoomSize;
        ClampTargetCameraToBoard();
        ApplyCameraStateImmediate();
        if (spawnIntroCurve == null || spawnIntroCurve.length == 0)
            spawnIntroCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        configured = true;
    }

    public void PlaySpawnIntro(Vector3 focusWorldPosition)
    {
        if (!configured || targetCamera == null || !playSpawnIntro || battlePreviewActive)
            return;

        if (introRoutine != null)
            StopCoroutine(introRoutine);

        ResetPointerState();
        introRoutine = StartCoroutine(PlaySpawnIntroRoutine(focusWorldPosition));
    }

    private void Update()
    {
        if (!configured || targetCamera == null || boardManager == null)
            return;

        if (battlePreviewActive)
            return;

        if (introRoutine != null)
            return;

        if (IsInputBlocked())
        {
            ResetPointerState();
            ApplyCameraSmoothing();
            return;
        }

        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }

        ApplyCameraSmoothing();
    }

    public MapCameraPreviewState CaptureCameraState()
    {
        if (targetCamera == null)
            return default;

        return new MapCameraPreviewState
        {
            position = targetCamera.transform.position,
            rotation = targetCamera.transform.rotation,
            orthographic = targetCamera.orthographic,
            orthographicSize = targetCamera.orthographicSize,
            fieldOfView = targetCamera.fieldOfView
        };
    }

    public void ApplyBattlePreviewImmediate(Vector3 origin, Vector3 target, float zoomMultiplier = 1.75f)
    {
        BeginPreviewOperation();
        if (!BeginBattlePreview())
            return;

        ApplyCameraState(CalculateBattlePreviewState(origin, target, zoomMultiplier));
    }

    public void RestoreBattlePreviewImmediate()
    {
        BeginPreviewOperation();
        if (targetCamera == null || !battlePreviewActive || !hasPreBattlePreviewState)
            return;

        CompleteBattlePreviewRestore();
    }

    public IEnumerator PlayBattlePreviewRoutine(Vector3 origin, Vector3 target, float zoomMultiplier, float duration)
    {
        int operationGeneration = BeginPreviewOperation();
        if (!BeginBattlePreview())
            yield break;

        MapCameraPreviewState start = CaptureCameraState();
        MapCameraPreviewState destination = CalculateBattlePreviewState(origin, target, zoomMultiplier);
        float safeDuration = NormalizePreviewDuration(duration);
        if (safeDuration <= 0.0001f)
        {
            if (OwnsPreviewOperation(operationGeneration))
                ApplyCameraState(destination);
            yield break;
        }

        MapCameraPreviewState perspectiveStart = CreatePerspectiveTransitionState(start);
        if (!OwnsPreviewOperation(operationGeneration))
            yield break;
        ApplyCameraState(perspectiveStart);

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            if (!OwnsPreviewOperation(operationGeneration))
                yield break;

            float t = Mathf.Clamp01(elapsed / safeDuration);
            ApplyInterpolatedCameraState(perspectiveStart, destination, t, false);
            yield return null;
            elapsed += Mathf.Max(MinimumTransitionDeltaTime, Time.unscaledDeltaTime);
        }

        if (!OwnsPreviewOperation(operationGeneration))
            yield break;
        ApplyCameraState(destination);
    }

    public IEnumerator RestoreBattlePreviewRoutine(float duration)
    {
        int operationGeneration = BeginPreviewOperation();
        if (targetCamera == null || !battlePreviewActive || !hasPreBattlePreviewState)
            yield break;

        MapCameraPreviewState start = CaptureCameraState();
        MapCameraPreviewState destination = preBattlePreviewState;
        float safeDuration = NormalizePreviewDuration(duration);
        if (safeDuration <= 0.0001f)
        {
            if (OwnsPreviewOperation(operationGeneration))
                CompleteBattlePreviewRestore();
            yield break;
        }

        MapCameraPreviewState perspectiveDestination = CreatePerspectiveTransitionState(destination);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            if (!OwnsPreviewOperation(operationGeneration))
                yield break;

            float t = Mathf.Clamp01(elapsed / safeDuration);
            ApplyInterpolatedCameraState(start, perspectiveDestination, t, false);
            yield return null;
            elapsed += Mathf.Max(MinimumTransitionDeltaTime, Time.unscaledDeltaTime);
        }

        if (!OwnsPreviewOperation(operationGeneration))
            yield break;
        ApplyCameraState(perspectiveDestination);
        CompleteBattlePreviewRestore();
    }

    private void OnDisable()
    {
        InvalidatePreviewOperations();
        if (targetCamera != null && battlePreviewActive && hasPreBattlePreviewState)
            CompleteBattlePreviewRestore();

        if (introRoutine != null)
            StopCoroutine(introRoutine);
        introRoutine = null;
        ResetPointerState();
    }

    private bool BeginBattlePreview()
    {
        if (targetCamera == null)
            return false;

        if (!battlePreviewActive)
        {
            preBattlePreviewState = CaptureCameraState();
            preBattleTargetCameraPosition = targetCameraPosition;
            preBattleTargetZoomSize = targetZoomSize;
            hasPreBattlePreviewState = true;
        }

        battlePreviewActive = true;
        ResetPointerState();
        panSmoothVelocity = Vector3.zero;
        zoomSmoothVelocity = 0f;

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        return true;
    }

    private MapCameraPreviewState CalculateBattlePreviewState(Vector3 origin, Vector3 target, float zoomMultiplier)
    {
        Vector3 targetDelta = target - origin;
        Vector3 approach = targetDelta.sqrMagnitude > 0.001f ? targetDelta.normalized : Vector3.up;
        float safeZoomMultiplier = IsFinite(zoomMultiplier)
            ? Mathf.Max(1f, zoomMultiplier)
            : DefaultPreviewZoomMultiplier;
        float safeBaseDistance = IsFinite(previewBaseDistance)
            ? Mathf.Max(MinimumPlaneIntersectionDistance, previewBaseDistance)
            : DefaultPreviewBaseDistance;
        float safeApproachOffset = IsFinite(previewApproachOffset)
            ? Mathf.Max(0f, previewApproachOffset)
            : DefaultPreviewApproachOffset;
        float safeFieldOfView = IsFinite(previewFieldOfView)
            ? Mathf.Clamp(previewFieldOfView, 1f, 179f)
            : DefaultPreviewFieldOfView;
        float distance = safeBaseDistance / safeZoomMultiplier;
        Vector3 position = target - approach * safeApproachOffset + Vector3.back * distance;
        Vector3 lookDirection = target - position;
        Quaternion rotation = lookDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lookDirection, Vector3.up)
            : Quaternion.identity;

        return new MapCameraPreviewState
        {
            position = position,
            rotation = rotation,
            orthographic = false,
            orthographicSize = targetCamera != null ? targetCamera.orthographicSize : 0f,
            fieldOfView = safeFieldOfView
        };
    }

    private MapCameraPreviewState CreatePerspectiveTransitionState(MapCameraPreviewState state)
    {
        if (!state.orthographic)
            return state;

        state.orthographic = false;
        state.fieldOfView = CalculateEquivalentPerspectiveFieldOfView(
            state.position,
            state.rotation,
            state.orthographicSize,
            state.fieldOfView);
        return state;
    }

    public static float CalculateEquivalentPerspectiveFieldOfView(
        Vector3 cameraPosition,
        Quaternion cameraRotation,
        float orthographicSize,
        float fallbackFieldOfView)
    {
        float fallback = IsFinite(fallbackFieldOfView)
            ? Mathf.Clamp(fallbackFieldOfView, 1f, 179f)
            : 60f;
        if (!IsFinite(orthographicSize) || orthographicSize <= 0f)
            return fallback;

        Vector3 forward = cameraRotation * Vector3.forward;
        if (!IsFinite(forward.z) || Mathf.Abs(forward.z) <= MinimumPlaneIntersectionDistance)
            return fallback;

        float distance = -cameraPosition.z / forward.z;
        if (!IsFinite(distance) || distance <= MinimumPlaneIntersectionDistance)
            return fallback;

        float fieldOfView = 2f * Mathf.Atan(orthographicSize / distance) * Mathf.Rad2Deg;
        return IsFinite(fieldOfView) ? Mathf.Clamp(fieldOfView, 1f, 179f) : fallback;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static float NormalizePreviewDuration(float duration)
    {
        return IsFinite(duration) && duration > 0f ? duration : 0f;
    }

    private int BeginPreviewOperation()
    {
        InvalidatePreviewOperations();
        return previewOperationGeneration;
    }

    private void InvalidatePreviewOperations()
    {
        unchecked
        {
            previewOperationGeneration++;
        }
    }

    private bool OwnsPreviewOperation(int operationGeneration)
    {
        return operationGeneration == previewOperationGeneration;
    }

    private void ApplyInterpolatedCameraState(
        MapCameraPreviewState start,
        MapCameraPreviewState destination,
        float t,
        bool orthographic)
    {
        targetCamera.orthographic = orthographic;
        targetCamera.transform.SetPositionAndRotation(
            Vector3.LerpUnclamped(start.position, destination.position, t),
            Quaternion.SlerpUnclamped(start.rotation, destination.rotation, t));
        targetCamera.orthographicSize = Mathf.LerpUnclamped(start.orthographicSize, destination.orthographicSize, t);
        targetCamera.fieldOfView = Mathf.LerpUnclamped(start.fieldOfView, destination.fieldOfView, t);
    }

    private void ApplyCameraState(MapCameraPreviewState state)
    {
        if (targetCamera == null)
            return;

        targetCamera.transform.SetPositionAndRotation(state.position, state.rotation);
        targetCamera.orthographicSize = state.orthographicSize;
        targetCamera.fieldOfView = state.fieldOfView;
        targetCamera.orthographic = state.orthographic;
    }

    private void CompleteBattlePreviewRestore()
    {
        ApplyCameraState(preBattlePreviewState);
        targetCameraPosition = preBattleTargetCameraPosition;
        targetZoomSize = preBattleTargetZoomSize;
        panSmoothVelocity = Vector3.zero;
        zoomSmoothVelocity = 0f;
        battlePreviewActive = false;
        hasPreBattlePreviewState = false;
    }

    private void HandleMouseInput()
    {
        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.001f)
        {
            ZoomBy(-Input.mouseScrollDelta.y * mouseWheelZoomStep, Input.mousePosition);
            ViewNavigationPerformed?.Invoke();
        }

        if (Input.GetMouseButtonDown(0))
            BeginPointer(Input.mousePosition, -1);

        if (!pointerActive)
            return;

        if (Input.GetMouseButton(0))
            UpdatePointer(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            EndPointer(Input.mousePosition);
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount >= 2)
        {
            ResetPointerState();
            HandlePinchZoom();
            return;
        }

        Touch touch = Input.GetTouch(0);
        switch (touch.phase)
        {
            case TouchPhase.Began:
                BeginPointer(touch.position, touch.fingerId);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                UpdatePointer(touch.position);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                EndPointer(touch.position);
                break;
        }
    }

    private void HandlePinchZoom()
    {
        Touch touchA = Input.GetTouch(0);
        Touch touchB = Input.GetTouch(1);

        Vector2 currentMidpoint = (touchA.position + touchB.position) * 0.5f;
        Vector2 previousA = touchA.position - touchA.deltaPosition;
        Vector2 previousB = touchB.position - touchB.deltaPosition;
        Vector2 previousMidpoint = (previousA + previousB) * 0.5f;

        float previousDistance = Vector2.Distance(previousA, previousB);
        float currentDistance = Vector2.Distance(touchA.position, touchB.position);
        float deltaDistance = currentDistance - previousDistance;

        ZoomBy(-deltaDistance * pinchZoomSensitivity, currentMidpoint);
        PanBetweenScreenPositions(previousMidpoint, currentMidpoint);
        ViewNavigationPerformed?.Invoke();
    }

    private void BeginPointer(Vector2 screenPosition, int pointerId)
    {
        if (IsPointerOverUi(pointerId) && !ShouldIgnorePointerOverUi())
        {
            ResetPointerState();
            return;
        }

        pointerActive = true;
        dragging = false;
        longPressTriggered = false;
        pointerStartScreenPosition = screenPosition;
        lastPointerScreenPosition = screenPosition;
        pointerStartTime = Time.unscaledTime;
        pressedTile = GetTileAtScreenPosition(screenPosition);
    }

    private void UpdatePointer(Vector2 screenPosition)
    {
        if (!pointerActive)
            return;

        Vector2 totalDelta = screenPosition - pointerStartScreenPosition;
        if (!dragging && totalDelta.sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
            dragging = true;

        if (dragging)
        {
            PanBetweenScreenPositions(lastPointerScreenPosition, screenPosition);
            ViewNavigationPerformed?.Invoke();
        }

        if (!dragging && !longPressTriggered && pressedTile != null)
        {
            float heldTime = Time.unscaledTime - pointerStartTime;
            float holdDistanceSq = (screenPosition - pointerStartScreenPosition).sqrMagnitude;
            if (heldTime >= longPressDuration && holdDistanceSq <= longPressMoveTolerance * longPressMoveTolerance)
            {
                longPressTriggered = true;
                boardManager.SetSelectedTile(pressedTile.coord);
                TileLongPressed?.Invoke(pressedTile);
            }
        }

        lastPointerScreenPosition = screenPosition;
    }

    private void EndPointer(Vector2 screenPosition)
    {
        if (!pointerActive)
            return;

        BoardTileData releasedTile = GetTileAtScreenPosition(screenPosition);
        bool shouldClick = !dragging && !longPressTriggered && pressedTile != null && releasedTile == pressedTile;
        BoardTileData clickedTile = shouldClick ? pressedTile : null;

        ResetPointerState();

        if (clickedTile != null)
            boardManager.NotifyTileClicked(clickedTile);
    }

    private void ResetPointerState()
    {
        pointerActive = false;
        pressedTile = null;
        dragging = false;
        longPressTriggered = false;
    }

    private BoardTileData GetTileAtScreenPosition(Vector2 screenPosition)
    {
        return boardManager != null && boardManager.TryGetTileAtScreenPosition(targetCamera, screenPosition, out BoardTileData tile)
            ? tile
            : null;
    }

    private void ZoomBy(float zoomDelta, Vector2 focusScreenPosition)
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        float oldSize = targetZoomSize;
        float newSize = Mathf.Clamp(oldSize + zoomDelta, minZoomSize, maxZoomSize);
        if (Mathf.Approximately(oldSize, newSize))
            return;

        Vector3 before = ScreenToWorldWithState(focusScreenPosition, targetCameraPosition, oldSize);
        targetZoomSize = newSize;
        Vector3 after = ScreenToWorldWithState(focusScreenPosition, targetCameraPosition, targetZoomSize);
        targetCameraPosition += before - after;
        ClampTargetCameraToBoard();
    }

    private void PanBetweenScreenPositions(Vector2 fromScreenPosition, Vector2 toScreenPosition)
    {
        Vector2 screenDelta = toScreenPosition - fromScreenPosition;
        if (targetCamera == null || screenDelta.sqrMagnitude <= 0.0001f)
            return;

        Vector3 from = ScreenToWorldWithState(fromScreenPosition, targetCameraPosition, targetZoomSize);
        Vector3 to = ScreenToWorldWithState(toScreenPosition, targetCameraPosition, targetZoomSize);
        Vector3 worldDelta = from - to;
        targetCameraPosition += new Vector3(worldDelta.x, worldDelta.y, 0f);
        ClampTargetCameraToBoard();
    }

    private void ApplyCameraSmoothing()
    {
        if (targetCamera == null)
            return;

        float deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        float smoothZoomTime = Mathf.Max(0.0001f, zoomSmoothTime);
        float smoothPanTime = Mathf.Max(0.0001f, panSmoothTime);

        float nextZoom = Mathf.SmoothDamp(
            targetCamera.orthographicSize,
            targetZoomSize,
            ref zoomSmoothVelocity,
            smoothZoomTime,
            Mathf.Infinity,
            deltaTime);

        Vector3 nextPosition = Vector3.SmoothDamp(
            targetCamera.transform.position,
            targetCameraPosition,
            ref panSmoothVelocity,
            smoothPanTime,
            Mathf.Infinity,
            deltaTime);

        nextPosition = ClampCameraPosition(nextPosition, nextZoom);
        nextPosition.z = -10f;

        targetCamera.orthographicSize = nextZoom;
        targetCamera.transform.position = nextPosition;
    }

    private void ApplyCameraStateImmediate()
    {
        if (targetCamera == null)
            return;

        targetCamera.orthographicSize = targetZoomSize;
        targetCamera.transform.position = ClampCameraPosition(targetCameraPosition, targetZoomSize);
        targetCamera.transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, -10f);
    }

    private void ClampTargetCameraToBoard()
    {
        targetCameraPosition = ClampCameraPosition(targetCameraPosition, targetZoomSize);
        targetCameraPosition.z = -10f;
    }

    private Vector3 ClampCameraPosition(Vector3 position, float orthographicSize)
    {
        if (targetCamera == null || boardManager == null)
            return position;

        float extent = boardManager.BoardHalfExtentWorld + boardPadding;
        float visibleHalfHeight = orthographicSize;
        float visibleHalfWidth = visibleHalfHeight * targetCamera.aspect;

        float maxX = Mathf.Max(0f, extent - visibleHalfWidth);
        float maxY = Mathf.Max(0f, extent - visibleHalfHeight);

        position.x = Mathf.Clamp(position.x, -maxX, maxX);
        position.y = Mathf.Clamp(position.y, -maxY, maxY);
        position.z = -10f;
        return position;
    }

    private Vector3 ScreenToWorldWithState(Vector2 screenPosition, Vector3 cameraPosition, float orthographicSize)
    {
        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        float normalizedX = screenPosition.x / screenWidth;
        float normalizedY = screenPosition.y / screenHeight;
        float halfHeight = orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        float worldX = cameraPosition.x + (normalizedX - 0.5f) * 2f * halfWidth;
        float worldY = cameraPosition.y + (normalizedY - 0.5f) * 2f * halfHeight;
        return new Vector3(worldX, worldY, 0f);
    }

    private static bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsInputBlocked()
    {
        return inputBlockedEvaluator != null && inputBlockedEvaluator.Invoke();
    }

    private bool ShouldIgnorePointerOverUi()
    {
        return pointerOverUiIgnoredEvaluator != null && pointerOverUiIgnoredEvaluator.Invoke();
    }

    private IEnumerator PlaySpawnIntroRoutine(Vector3 focusWorldPosition)
    {
        panSmoothVelocity = Vector3.zero;
        zoomSmoothVelocity = 0f;

        Vector3 destinationPosition = ClampCameraPosition(targetCameraPosition, targetZoomSize);
        destinationPosition.z = -10f;
        float destinationZoom = targetZoomSize;

        Vector3 introPosition = focusWorldPosition + spawnIntroFocusOffset;
        introPosition = ClampCameraPosition(introPosition, spawnIntroZoomSize);
        introPosition.z = -10f;

        targetCamera.transform.position = introPosition;
        targetCamera.orthographicSize = Mathf.Clamp(spawnIntroZoomSize, minZoomSize, maxZoomSize);

        if (spawnIntroHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(spawnIntroHoldDuration);

        float duration = Mathf.Max(0.01f, spawnIntroTravelDuration);
        Vector3 startPosition = targetCamera.transform.position;
        float startZoom = targetCamera.orthographicSize;

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = spawnIntroCurve != null ? spawnIntroCurve.Evaluate(t) : t;
            targetCamera.transform.position = Vector3.LerpUnclamped(startPosition, destinationPosition, eased);
            targetCamera.orthographicSize = Mathf.LerpUnclamped(startZoom, destinationZoom, eased);
            yield return null;
        }

        targetCamera.transform.position = destinationPosition;
        targetCamera.orthographicSize = destinationZoom;
        introRoutine = null;
    }
}
