using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMapController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private Transform unitsRoot;
    [SerializeField] private Transform playerPawn;

    [Header("Visuals")]
    [SerializeField] private Sprite playerPawnSprite;
    [SerializeField] private Color playerPawnColor = new Color(0.22f, 0.9f, 1f, 1f);
    [SerializeField] private Color playerOutlineColor = new Color(1f, 1f, 1f, 0.28f);
    [SerializeField] private TMP_FontAsset worldFont;
    [SerializeField] private float moveDuration = 0.14f;
    [SerializeField] private float jumpVisualHeight = 0.2f;
    [SerializeField] private float moveTiltAngle = 16f;
    [SerializeField] private float moveStretchAmount = 0.12f;

    [Header("Landing Effect")]
    [SerializeField] private GameObject landingEffectPrefab;
    [SerializeField] private Vector3 landingEffectOffset = new Vector3(0f, -0.1f, 0f);
    [SerializeField] private Vector3 landingEffectRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 landingEffectScale = Vector3.one;
    [SerializeField] private bool overrideLandingEffectSorting = true;
    [SerializeField] private int landingEffectSortingOrder = 60;
    [SerializeField] private bool overrideLandingEffectDuration;
    [SerializeField] private float landingEffectDuration = 2f;

    private BoardManager boardManager;
    private ActionPointSystem actionPointSystem;
    private TileResolver tileResolver;
    private RunState runState;
    private Sprite runtimeWhiteSprite;
    private Coroutine moveRoutine;
    private Coroutine battlePreviewRoutine;
    private Coroutine previewCommitCoroutine;
    private IEnumerator previewCommitIterator;
    private Transform visualRoot;
    private bool isMoving;
    private bool previewCommitPending;
    private bool battlePreviewActive;
    private bool hasBattlePreviewSnapshot;
    private int battlePreviewGeneration;
    private BoardTileData battlePreviewTarget;
    private Vector3 previewPawnLocalPosition;
    private Quaternion previewPawnLocalRotation;
    private Vector3 previewPawnLocalScale;
    private Vector3 previewVisualLocalPosition;
    private Quaternion previewVisualLocalRotation;
    private Vector3 previewVisualLocalScale;
    private int previewCommitGeneration;
    private BoardTileData previewCommittedTile;
    private RunState previewCommittedRunState;
    private TileResolver previewCommittedResolver;
    private Vector2Int previewCommittedCoordinate;

    public event Action<BoardTileData, string> PlayerMovedToTile;

    public bool IsBattlePreviewActive => battlePreviewActive;
    public bool IsPreviewCommitPending => previewCommitPending;
    public Vector3 PawnWorldPosition => playerPawn != null ? playerPawn.position : Vector3.zero;

    public void Initialize(Transform root, BoardManager board, ActionPointSystem points, TileResolver resolver, RunState state)
    {
        InvalidatePendingPreviewCommit();
        InvalidateBattlePreview(true);
        unitsRoot = root != null ? root : unitsRoot;
        boardManager = board;
        actionPointSystem = points;
        tileResolver = resolver;
        runState = state;

        if (unitsRoot == null)
        {
            GameObject go = new GameObject("UnitsRoot");
            go.transform.SetParent(transform, false);
            unitsRoot = go.transform;
        }

        EnsurePawn();
    }

    public void SetSpawn(BoardTileData spawnTile)
    {
        InvalidatePendingPreviewCommit();
        InvalidateBattlePreview(true);
        if (spawnTile == null || boardManager == null || runState == null)
            return;

        EnsurePawn();
        runState.playerGridPosition = spawnTile.coord;
        playerPawn.position = boardManager.GridToWorld(spawnTile.coord) + new Vector3(0f, 0f, -0.2f);
        PlayLandingEffect(playerPawn.position);
    }

    public void SetPosition(BoardTileData tile, bool playLandingEffect = false)
    {
        InvalidatePendingPreviewCommit();
        InvalidateBattlePreview(true);
        if (tile == null || boardManager == null || runState == null)
            return;

        EnsurePawn();
        runState.playerGridPosition = tile.coord;
        playerPawn.position = boardManager.GridToWorld(tile.coord) + new Vector3(0f, 0f, -0.2f);
        if (visualRoot != null)
        {
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }

        if (playLandingEffect)
            PlayLandingEffect(playerPawn.position);
    }

    public bool TryMoveTo(BoardTileData targetTile, int moveCost)
    {
        if (targetTile == null || boardManager == null || actionPointSystem == null || runState == null)
            return false;

        if (isMoving || previewCommitPending)
            return false;

        bool commitsBattlePreview = battlePreviewActive && BattlePreviewMatches(targetTile);

        if (targetTile.coord == runState.playerGridPosition)
        {
            string sameTileMessage = (tileResolver != null && ShouldResolveImmediately(targetTile))
                ? tileResolver.ResolveTile(targetTile, runState)
                : string.Empty;
            PlayerMovedToTile?.Invoke(targetTile, sameTileMessage);
            return false;
        }

        if (!boardManager.IsAdjacent(runState.playerGridPosition, targetTile.coord, runState != null && runState.allowDiagonalMapMovement))
            return false;

        int safeMoveCost = Mathf.Max(0, moveCost);
        if (safeMoveCost > 0 && !actionPointSystem.CanSpend(safeMoveCost))
            return false;

        if (battlePreviewActive && !commitsBattlePreview)
            InvalidateBattlePreview(true);

        if (safeMoveCost > 0 && !actionPointSystem.TrySpend(safeMoveCost))
            return false;

        runState.playerGridPosition = targetTile.coord;
        Vector3 worldTarget = boardManager.GridToWorld(targetTile.coord) + new Vector3(0f, 0f, -0.2f);

        if (commitsBattlePreview)
        {
            CommitBattlePreview(worldTarget);
            BeginPendingPreviewCommit(targetTile);
            return true;
        }

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        isMoving = true;
        moveRoutine = StartCoroutine(MovePawnTo(worldTarget, targetTile));
        return true;
    }

    public bool BeginBattleTilePreview(BoardTileData target, bool immediate)
    {
        if (isMoving || previewCommitPending)
            return false;

        if (immediate)
        {
            if (!PrepareBattlePreview(target, out int generation, out Vector3 worldTarget))
                return false;

            ApplyBattlePreviewPose(worldTarget, 0f, Vector3.right);
            if (generation == battlePreviewGeneration)
                battlePreviewRoutine = null;
            return true;
        }

        if (target == null || boardManager == null)
            return false;

        battlePreviewRoutine = StartCoroutine(PlayBattleTilePreviewRoutine(target, moveDuration));
        return true;
    }

    public void CancelBattleTilePreview(bool immediate)
    {
        if (!hasBattlePreviewSnapshot)
        {
            InvalidateBattlePreview(false);
            return;
        }

        if (immediate)
        {
            InvalidateBattlePreview(true);
            return;
        }

        battlePreviewRoutine = StartCoroutine(RestoreBattleTilePreviewRoutine(moveDuration));
    }

    public IEnumerator PlayBattleTilePreviewRoutine(BoardTileData target, float duration)
    {
        if (!PrepareBattlePreview(target, out int generation, out Vector3 worldTarget))
            yield break;

        float safeDuration = NormalizePreviewDuration(duration);
        Vector3 start = playerPawn.position;
        Vector3 direction = worldTarget - start;
        direction.z = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        if (safeDuration <= 0f)
        {
            ApplyBattlePreviewPose(worldTarget, 0f, direction);
            battlePreviewRoutine = null;
            yield break;
        }

        for (float time = 0f; time < safeDuration; time += PreviewDeltaTime())
        {
            if (generation != battlePreviewGeneration)
                yield break;

            float t = Mathf.Clamp01(time / safeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            ApplyBattlePreviewPose(Vector3.LerpUnclamped(start, worldTarget, eased), Mathf.Sin(t * Mathf.PI), direction);
            yield return null;
        }

        if (generation != battlePreviewGeneration)
            yield break;

        ApplyBattlePreviewPose(worldTarget, 0f, direction);
        battlePreviewRoutine = null;
    }

    public IEnumerator RestoreBattleTilePreviewRoutine(float duration)
    {
        if (!hasBattlePreviewSnapshot || playerPawn == null)
            yield break;

        StopOwnedBattlePreviewRoutine();
        int generation = ++battlePreviewGeneration;
        float safeDuration = NormalizePreviewDuration(duration);
        Vector3 start = playerPawn.localPosition;
        Vector3 direction = previewPawnLocalPosition - start;
        direction.z = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        if (safeDuration <= 0f)
        {
            RestoreBattlePreviewSnapshot();
            ClearBattlePreviewState();
            yield break;
        }

        for (float time = 0f; time < safeDuration; time += PreviewDeltaTime())
        {
            if (generation != battlePreviewGeneration)
                yield break;

            float t = Mathf.Clamp01(time / safeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            playerPawn.localPosition = Vector3.LerpUnclamped(start, previewPawnLocalPosition, eased);
            ApplyPreviewVisual(Mathf.Sin(t * Mathf.PI), direction);
            yield return null;
        }

        if (generation != battlePreviewGeneration)
            yield break;

        RestoreBattlePreviewSnapshot();
        ClearBattlePreviewState();
    }

    private bool PrepareBattlePreview(BoardTileData target, out int generation, out Vector3 worldTarget)
    {
        generation = battlePreviewGeneration;
        worldTarget = Vector3.zero;
        if (target == null || boardManager == null || isMoving || previewCommitPending)
            return false;

        EnsurePawn();
        if (playerPawn == null)
            return false;

        StopOwnedBattlePreviewRoutine();
        if (!hasBattlePreviewSnapshot)
            CaptureBattlePreviewSnapshot();

        battlePreviewActive = true;
        battlePreviewTarget = target;
        generation = ++battlePreviewGeneration;
        worldTarget = boardManager.GridToWorld(target.coord) + new Vector3(0f, 0f, -0.2f);
        return true;
    }

    private void CaptureBattlePreviewSnapshot()
    {
        previewPawnLocalPosition = playerPawn.localPosition;
        previewPawnLocalRotation = playerPawn.localRotation;
        previewPawnLocalScale = playerPawn.localScale;
        if (visualRoot != null)
        {
            previewVisualLocalPosition = visualRoot.localPosition;
            previewVisualLocalRotation = visualRoot.localRotation;
            previewVisualLocalScale = visualRoot.localScale;
        }
        hasBattlePreviewSnapshot = true;
    }

    private void ApplyBattlePreviewPose(Vector3 worldPosition, float hop, Vector3 direction)
    {
        if (playerPawn == null)
            return;

        playerPawn.position = worldPosition;
        playerPawn.localRotation = previewPawnLocalRotation;
        playerPawn.localScale = previewPawnLocalScale;
        ApplyPreviewVisual(hop, direction);
    }

    private void ApplyPreviewVisual(float hop, Vector3 direction)
    {
        if (visualRoot == null)
            return;

        float tiltSign = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? -Mathf.Sign(direction.x)
            : Mathf.Sign(direction.y) * 0.6f;
        float stretch = 1f + hop * moveStretchAmount;
        float squash = 1f - hop * moveStretchAmount * 0.45f;
        visualRoot.localPosition = previewVisualLocalPosition + new Vector3(0f, hop * jumpVisualHeight, 0f);
        visualRoot.localRotation = previewVisualLocalRotation * Quaternion.Euler(0f, 0f, hop * moveTiltAngle * tiltSign);
        visualRoot.localScale = Vector3.Scale(previewVisualLocalScale, new Vector3(squash, stretch, 1f));
    }

    private void InvalidateBattlePreview(bool restoreSnapshot)
    {
        StopOwnedBattlePreviewRoutine();
        ++battlePreviewGeneration;
        if (restoreSnapshot && hasBattlePreviewSnapshot)
            RestoreBattlePreviewSnapshot();
        ClearBattlePreviewState();
    }

    private void CommitBattlePreview(Vector3 worldTarget)
    {
        StopOwnedBattlePreviewRoutine();
        ++battlePreviewGeneration;
        if (playerPawn != null)
        {
            playerPawn.position = worldTarget;
            playerPawn.localRotation = previewPawnLocalRotation;
            playerPawn.localScale = previewPawnLocalScale;
        }
        if (visualRoot != null)
        {
            visualRoot.localPosition = previewVisualLocalPosition;
            visualRoot.localRotation = previewVisualLocalRotation;
            visualRoot.localScale = previewVisualLocalScale;
        }
        ClearBattlePreviewState();
    }

    private void RestoreBattlePreviewSnapshot()
    {
        if (playerPawn != null)
        {
            playerPawn.localPosition = previewPawnLocalPosition;
            playerPawn.localRotation = previewPawnLocalRotation;
            playerPawn.localScale = previewPawnLocalScale;
        }
        if (visualRoot != null)
        {
            visualRoot.localPosition = previewVisualLocalPosition;
            visualRoot.localRotation = previewVisualLocalRotation;
            visualRoot.localScale = previewVisualLocalScale;
        }
    }

    private void ClearBattlePreviewState()
    {
        battlePreviewActive = false;
        hasBattlePreviewSnapshot = false;
        battlePreviewTarget = null;
        battlePreviewRoutine = null;
    }

    private void StopOwnedBattlePreviewRoutine()
    {
        if (battlePreviewRoutine == null)
            return;

        StopCoroutine(battlePreviewRoutine);
        battlePreviewRoutine = null;
    }

    private bool BattlePreviewMatches(BoardTileData target)
    {
        return battlePreviewTarget != null && target != null && battlePreviewTarget.coord == target.coord;
    }

    private void BeginPendingPreviewCommit(BoardTileData targetTile)
    {
        InvalidatePendingPreviewCommit();
        int generation = ++previewCommitGeneration;
        previewCommitPending = true;
        previewCommittedTile = targetTile;
        previewCommittedRunState = runState;
        previewCommittedResolver = tileResolver;
        previewCommittedCoordinate = targetTile.coord;
        previewCommitIterator = NotifyBattlePreviewCommitNextTick(
            generation,
            previewCommittedTile,
            previewCommittedRunState,
            previewCommittedResolver,
            previewCommittedCoordinate);
        previewCommitCoroutine = StartCoroutine(previewCommitIterator);
    }

    private IEnumerator NotifyBattlePreviewCommitNextTick(
        int generation,
        BoardTileData targetTile,
        RunState committedRunState,
        TileResolver committedResolver,
        Vector2Int committedCoordinate)
    {
        yield return null;

        if (!IsPendingPreviewCommitValid(
                generation,
                targetTile,
                committedRunState,
                committedResolver,
                committedCoordinate))
        {
            ClearPendingPreviewCommit(generation);
            yield break;
        }

        try
        {
            string resultMessage = (committedResolver != null && ShouldResolveImmediately(targetTile))
                ? committedResolver.ResolveTile(targetTile, committedRunState)
                : string.Empty;
            PlayerMovedToTile?.Invoke(targetTile, resultMessage);
        }
        finally
        {
            ClearPendingPreviewCommit(generation);
        }
    }

    private bool IsPendingPreviewCommitValid(
        int generation,
        BoardTileData targetTile,
        RunState committedRunState,
        TileResolver committedResolver,
        Vector2Int committedCoordinate)
    {
        return previewCommitPending &&
               generation == previewCommitGeneration &&
               isActiveAndEnabled &&
               ReferenceEquals(previewCommittedTile, targetTile) &&
               ReferenceEquals(previewCommittedRunState, committedRunState) &&
               ReferenceEquals(previewCommittedResolver, committedResolver) &&
               ReferenceEquals(runState, committedRunState) &&
               ReferenceEquals(tileResolver, committedResolver) &&
               committedRunState != null &&
               previewCommittedCoordinate == committedCoordinate &&
               targetTile != null &&
               targetTile.coord == committedCoordinate &&
               committedRunState.playerGridPosition == committedCoordinate;
    }

    private void InvalidatePendingPreviewCommit()
    {
        ++previewCommitGeneration;
        if (previewCommitCoroutine != null)
            StopCoroutine(previewCommitCoroutine);

        previewCommitPending = false;
        previewCommitCoroutine = null;
        previewCommitIterator = null;
        previewCommittedTile = null;
        previewCommittedRunState = null;
        previewCommittedResolver = null;
        previewCommittedCoordinate = default;
    }

    private void ClearPendingPreviewCommit(int generation)
    {
        if (generation != previewCommitGeneration)
            return;

        previewCommitPending = false;
        previewCommitCoroutine = null;
        previewCommitIterator = null;
        previewCommittedTile = null;
        previewCommittedRunState = null;
        previewCommittedResolver = null;
        previewCommittedCoordinate = default;
    }

    private static float NormalizePreviewDuration(float duration)
    {
        return float.IsNaN(duration) || float.IsInfinity(duration) ? 0f : Mathf.Clamp(duration, 0f, 10f);
    }

    private static float PreviewDeltaTime()
    {
        return Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
    }

    private void OnDisable()
    {
        InvalidatePendingPreviewCommit();
        InvalidateBattlePreview(true);
    }

    private IEnumerator MovePawnTo(Vector3 target, BoardTileData targetTile)
    {
        if (playerPawn == null)
        {
            isMoving = false;
            yield break;
        }

        EnsurePawn();
        Vector3 start = playerPawn.position;
        Vector3 direction = (target - start);
        direction.z = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        float tiltSign = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? -Mathf.Sign(direction.x)
            : Mathf.Sign(direction.y) * 0.6f;
        float duration = Mathf.Max(0.01f, moveDuration);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float t = Mathf.Clamp01(time / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float hop = Mathf.Sin(t * Mathf.PI);
            playerPawn.position = Vector3.LerpUnclamped(start, target, eased);

            if (visualRoot != null)
            {
                float stretch = 1f + hop * moveStretchAmount;
                float squash = 1f - hop * moveStretchAmount * 0.45f;
                visualRoot.localPosition = new Vector3(0f, hop * jumpVisualHeight, 0f);
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, hop * moveTiltAngle * tiltSign);
                visualRoot.localScale = new Vector3(squash, stretch, 1f);
            }
            yield return null;
        }

        playerPawn.position = target;
        if (visualRoot != null)
        {
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }

        PlayLandingEffect(target);
        string resultMessage = (tileResolver != null && ShouldResolveImmediately(targetTile))
            ? tileResolver.ResolveTile(targetTile, runState)
            : string.Empty;
        PlayerMovedToTile?.Invoke(targetTile, resultMessage);
        isMoving = false;
        moveRoutine = null;
    }

    private void EnsurePawn()
    {
        EnsureWhiteSprite();
        if (playerPawn != null)
        {
            if (visualRoot == null)
            {
                Transform existingVisualRoot = playerPawn.Find("VisualRoot");
                if (existingVisualRoot != null)
                {
                    visualRoot = existingVisualRoot;
                }
                else
                {
                    GameObject runtimeVisualRootGo = new GameObject("VisualRoot");
                    runtimeVisualRootGo.transform.SetParent(playerPawn, false);
                    visualRoot = runtimeVisualRootGo.transform;

                    while (playerPawn.childCount > 1)
                    {
                        Transform child = playerPawn.GetChild(0);
                        if (child == visualRoot)
                            child = playerPawn.GetChild(1);
                        child.SetParent(visualRoot, true);
                    }
                }
            }
            return;
        }

        GameObject pawnRoot = new GameObject("PlayerMapPawn");
        pawnRoot.transform.SetParent(unitsRoot, false);
        playerPawn = pawnRoot.transform;

        GameObject visualRootGo = new GameObject("VisualRoot");
        visualRootGo.transform.SetParent(playerPawn, false);
        visualRoot = visualRootGo.transform;

        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(visualRoot, false);
        SpriteRenderer outlineRenderer = outline.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = runtimeWhiteSprite;
        outlineRenderer.color = playerOutlineColor;
        outlineRenderer.sortingOrder = 100;
        outline.transform.localScale = new Vector3(0.92f, 0.92f, 1f);

        GameObject body = new GameObject("Body");
        body.transform.SetParent(visualRoot, false);
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = playerPawnSprite != null ? playerPawnSprite : runtimeWhiteSprite;
        bodyRenderer.color = playerPawnColor;
        bodyRenderer.sortingOrder = 101;
        body.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(visualRoot, false);
        labelGo.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        TextMeshPro label = labelGo.AddComponent<TextMeshPro>();
        if (worldFont != null)
            label.font = worldFont;
        label.text = playerPawnSprite == null ? "P" : string.Empty;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 4f;
        label.color = new Color(0.04f, 0.1f, 0.14f, 1f);
        label.sortingOrder = 102;
        label.rectTransform.sizeDelta = new Vector2(4f, 2f);
    }

    private static bool ShouldResolveImmediately(BoardTileData tile)
    {
        if (tile == null || tile.IsHostileTile)
            return false;

        switch (tile.tileType)
        {
            case MapTileType.Event:
            case MapTileType.Merchant:
            case MapTileType.Forge:
            case MapTileType.Rest:
                return false;
            default:
                return true;
        }
    }

    private void EnsureWhiteSprite()
    {
        if (runtimeWhiteSprite != null)
            return;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, false);
        runtimeWhiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private void PlayLandingEffect(Vector3 playerWorldPosition)
    {
        if (landingEffectPrefab == null)
            return;

        Vector3 spawnPosition = playerWorldPosition + landingEffectOffset;
        Quaternion spawnRotation = Quaternion.Euler(landingEffectRotationOffset);
        Transform parent = unitsRoot != null ? unitsRoot : null;
        GameObject instance = Instantiate(landingEffectPrefab, spawnPosition, spawnRotation, parent);
        instance.transform.localScale = landingEffectScale;

        if (overrideLandingEffectSorting)
            ApplyLandingEffectSorting(instance);

        if (overrideLandingEffectDuration && landingEffectDuration > 0f)
            Destroy(instance, landingEffectDuration);
    }

    private void ApplyLandingEffectSorting(GameObject instance)
    {
        if (instance == null)
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        int minimumSortingOrder = int.MaxValue;
        for (int i = 0; i < renderers.Length; i++)
            minimumSortingOrder = Mathf.Min(minimumSortingOrder, renderers[i].sortingOrder);

        if (minimumSortingOrder == int.MaxValue)
            minimumSortingOrder = 0;

        int delta = landingEffectSortingOrder - minimumSortingOrder;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder += delta;
    }
}
