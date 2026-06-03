using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class MapViewController : MonoBehaviour
{
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

    public event Action<BoardTileData> TileLongPressed;
    public event Action ViewNavigationPerformed;

    public void SetInputBlockedEvaluator(Func<bool> evaluator)
    {
        inputBlockedEvaluator = evaluator;
    }

    public void Initialize(Camera mapCamera, BoardManager board, float padding)
    {
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
        if (!configured || targetCamera == null || !playSpawnIntro)
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
        if (IsPointerOverUi(pointerId))
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
