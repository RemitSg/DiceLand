using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class BoardManager : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private BoardTileView tileViewPrefab;

    [Header("Layout")]
    [SerializeField] private float tileSpacing = 1.35f;
    [SerializeField] private Vector2 tileVisualSize = new Vector2(1.12f, 1.12f);
    [SerializeField] private int tileSortingOrder = 10;
    [SerializeField] private Vector3 tileIconLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 tileIconLocalScale = new Vector3(0.42f, 0.42f, 1f);
    [SerializeField] private Vector3 tileIconGlowLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 tileIconGlowLocalScale = new Vector3(0.62f, 0.62f, 1f);
    [SerializeField] private Vector3 tileIconGlowMaskLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 tileIconGlowMaskLocalScale = new Vector3(0.86f, 0.86f, 1f);

    [Header("Icons")]
    [SerializeField] private Sprite defaultTileSprite;
    [SerializeField] private Sprite battleTileIcon;
    [SerializeField] private Sprite eliteTileIcon;
    [SerializeField] private Sprite eventTileIcon;
    [SerializeField] private Sprite merchantTileIcon;
    [SerializeField] private Sprite treasureTileIcon;
    [SerializeField] private Sprite forgeTileIcon;
    [SerializeField] private Sprite restTileIcon;
    [SerializeField] private Sprite keyTileIcon;
    [SerializeField] private Sprite bossTileIcon;
    [SerializeField] private Sprite specialDungeonExitIcon;
    [SerializeField] private TMP_FontAsset worldTextFont;
    [SerializeField] private Sprite tileFrameSprite;
    [SerializeField] private Sprite tileIconGlowMaskSprite;
    [SerializeField] private Vector3 tileFrameLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 tileFrameLocalScale = new Vector3(1.04f, 1.04f, 1f);
    [SerializeField] private Color tileFrameColor = new Color(0.94f, 0.97f, 1f, 0.9f);

    [Header("Colors")]
    [SerializeField] private Color outerTileColor = new Color(0.24f, 0.32f, 0.42f, 1f);
    [SerializeField] private Color midTileColor = new Color(0.3f, 0.38f, 0.5f, 1f);
    [SerializeField] private Color innerTileColor = new Color(0.42f, 0.34f, 0.22f, 1f);
    [SerializeField] private Color coreTileColor = new Color(0.52f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color emptyTileColor = new Color(0.28f, 0.3f, 0.34f, 1f);
    [SerializeField] private Color visitedTileColor = new Color(0.42f, 0.44f, 0.48f, 1f);
    [SerializeField] private Color iconTintColor = Color.white;
    [SerializeField] private Color fallbackIconColor = Color.white;
    [SerializeField] private Color previewTextColor = new Color(0.93f, 0.95f, 1f, 1f);
    [SerializeField] private Color lockedBossColor = new Color(0.42f, 0.18f, 0.24f, 1f);
    [SerializeField] private Color eliteTileAccentColor = new Color(0.9f, 0.42f, 0.22f, 1f);
    [SerializeField] private Color spawnHighlightColor = new Color(0.24f, 0.9f, 0.95f, 0.38f);
    [SerializeField] private Color selectedHighlightColor = new Color(1f, 0.9f, 0.36f, 0.42f);
    [SerializeField] private float tileBackgroundDarkening = 0.22f;
    [SerializeField] private Color tileInnerGlowTint = new Color(1f, 1f, 1f, 0.72f);
    [SerializeField] private Vector3 tileInnerGlowLocalScale = new Vector3(0.84f, 0.84f, 1f);

    private readonly Dictionary<Vector2Int, BoardTileData> tiles = new Dictionary<Vector2Int, BoardTileData>();
    private readonly Dictionary<Vector2Int, BoardTileView> views = new Dictionary<Vector2Int, BoardTileView>();
    private Sprite runtimeWhiteSprite;
    private Sprite runtimeRadialSprite;
    private Vector2Int selectedCoord = new Vector2Int(int.MinValue, int.MinValue);

    public event Action<BoardTileData> TileClicked;

    public int BoardSize { get; private set; }
    public Vector2Int SpawnCoord { get; private set; }
    public Vector2Int BossCoord { get; private set; }
    public float TileSpacing => tileSpacing;
    public Vector2 TileVisualSize => tileVisualSize;
    public int TileSortingOrder => tileSortingOrder;
    public Vector2Int SelectedCoord => selectedCoord;
    public Sprite DefaultTileSprite => runtimeWhiteSprite != null ? runtimeWhiteSprite : defaultTileSprite;
    public Sprite TileInnerGlowSprite => runtimeRadialSprite != null ? runtimeRadialSprite : DefaultTileSprite;
    public TMP_FontAsset WorldTextFont => worldTextFont;
    public Color IconTintColor => iconTintColor;
    public float BoardHalfExtentWorld => ((BoardSize - 1) * 0.5f * tileSpacing) + Mathf.Max(tileVisualSize.x, tileVisualSize.y) * 0.5f;
    public Vector3 TileIconLocalOffset => tileIconLocalOffset;
    public Vector3 TileIconLocalScale => tileIconLocalScale;
    public Vector3 TileIconGlowLocalOffset => tileIconGlowLocalOffset;
    public Vector3 TileIconGlowLocalScale => tileIconGlowLocalScale;
    public Vector3 TileIconGlowMaskLocalOffset => tileIconGlowMaskLocalOffset;
    public Vector3 TileIconGlowMaskLocalScale => tileIconGlowMaskLocalScale;
    public Sprite TileFrameSprite => tileFrameSprite;
    public Sprite TileIconGlowMaskSprite => tileIconGlowMaskSprite != null ? tileIconGlowMaskSprite : DefaultTileSprite;
    public Vector3 TileFrameLocalOffset => tileFrameLocalOffset;
    public Vector3 TileFrameLocalScale => tileFrameLocalScale;
    public Vector3 TileInnerGlowLocalScale => tileInnerGlowLocalScale;
    public Color TileFrameColor => tileFrameColor;

    public void Initialize(Transform root = null)
    {
        if (root != null)
            boardRoot = root;
        if (boardRoot == null)
        {
            GameObject go = new GameObject("BoardRoot");
            go.transform.SetParent(transform, false);
            boardRoot = go.transform;
        }

        EnsureDefaultSprite();
    }

    public void GenerateBoard(DifficultyConfig_SO config, int seed)
    {
        Initialize();
        ClearBoard();

        DifficultyConfig_SO effectiveConfig = config != null ? config : CreateFallbackConfig();
        BoardSize = effectiveConfig.BoardSize;
        BossCoord = new Vector2Int(BoardSize / 2, BoardSize / 2);

        System.Random rng = new System.Random(seed);
        SpawnCoord = PickSpawnCoord(effectiveConfig, rng);
        HashSet<Vector2Int> keyCoords = PickKeyCoords(effectiveConfig, rng);

        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                BoardTileData tile = new BoardTileData
                {
                    coord = coord,
                    encounterSeed = rng.Next(int.MinValue, int.MaxValue),
                    dangerBand = CalculateDangerBand(coord),
                    isSpawnTile = coord == SpawnCoord
                };

                if (coord == BossCoord)
                {
                    tile.tileType = MapTileType.Boss;
                }
                else if (coord == SpawnCoord)
                {
                    tile.tileType = MapTileType.Empty;
                    tile.isResolved = true;
                }
                else if (keyCoords.Contains(coord))
                {
                    tile.tileType = MapTileType.Key;
                }
                else
                {
                    tile.tileType = PickWeightedTileType(effectiveConfig, rng);
                }

                tiles[coord] = tile;
            }
        }

        ApplyGuaranteedTileMinimums(effectiveConfig, rng);

        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (!tiles.TryGetValue(coord, out BoardTileData tile) || tile == null)
                    continue;

                PopulatePreview(tile, rng);
                CreateTileView(tile);
            }
        }
    }

    public void LoadBoard(int boardSize, Vector2Int spawnCoord, Vector2Int bossCoord, Vector2Int restoredSelectedCoord, IReadOnlyList<BoardTileData> restoredTiles)
    {
        Initialize();
        ClearBoard();

        BoardSize = Mathf.Max(1, boardSize);
        SpawnCoord = spawnCoord;
        BossCoord = bossCoord;
        selectedCoord = restoredSelectedCoord;

        if (restoredTiles == null)
            return;

        for (int i = 0; i < restoredTiles.Count; i++)
        {
            BoardTileData tile = restoredTiles[i];
            if (tile == null)
                continue;

            BoardTileData clone = tile.Clone();
            tiles[clone.coord] = clone;
            CreateTileView(clone);
        }

        RefreshAllViews();
    }

    public void RegenerateTilesExceptCurrentAndBoss(DifficultyConfig_SO config, int seed, Vector2Int currentCoord)
    {
        Initialize();
        DifficultyConfig_SO effectiveConfig = config != null ? config : CreateFallbackConfig();
        System.Random rng = new System.Random(seed);
        HashSet<Vector2Int> keyCoords = PickKeyCoordsForRegeneration(effectiveConfig, rng, currentCoord);
        Dictionary<Vector2Int, BoardTileData> preservedTiles = new Dictionary<Vector2Int, BoardTileData>();
        if (tiles.TryGetValue(currentCoord, out BoardTileData currentTile) && currentTile != null)
            preservedTiles[currentCoord] = currentTile.Clone();
        if (tiles.TryGetValue(BossCoord, out BoardTileData bossTile) && bossTile != null)
            preservedTiles[BossCoord] = bossTile.Clone();

        ClearBoard();

        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (preservedTiles.TryGetValue(coord, out BoardTileData preserved))
                {
                    tiles[coord] = preserved;
                    continue;
                }

                BoardTileData tile = new BoardTileData
                {
                    coord = coord,
                    encounterSeed = rng.Next(int.MinValue, int.MaxValue),
                    dangerBand = CalculateDangerBand(coord),
                    isSpawnTile = false
                };

                tile.tileType = keyCoords.Contains(coord)
                    ? MapTileType.Key
                    : PickWeightedTileType(effectiveConfig, rng);

                tiles[coord] = tile;
            }
        }

        ApplyGuaranteedTileMinimums(
            effectiveConfig,
            rng,
            new HashSet<Vector2Int> { currentCoord, BossCoord });

        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (!tiles.TryGetValue(coord, out BoardTileData tile) || tile == null)
                    continue;

                PopulatePreview(tile, rng);
                CreateTileView(tile);
            }
        }

        selectedCoord = currentCoord;
        RefreshAllViews();
    }

    public IEnumerable<BoardTileData> GetAllTiles()
    {
        return tiles.Values;
    }

    public bool TryGetTile(Vector2Int coord, out BoardTileData tile)
    {
        return tiles.TryGetValue(coord, out tile);
    }

    public bool TryGetTileScreenHighlightRect(Vector2Int coord, Camera targetCamera, out Rect screenRect, float paddingPixels = 18f)
    {
        screenRect = default;
        if (targetCamera == null)
            return false;

        if (!views.TryGetValue(coord, out BoardTileView view) || view == null)
            return false;

        return view.TryGetScreenHighlightRect(targetCamera, out screenRect, paddingPixels);
    }

    public bool IsAdjacent(Vector2Int from, Vector2Int to)
    {
        return IsAdjacent(from, to, false);
    }

    public bool IsAdjacent(Vector2Int from, Vector2Int to, bool allowDiagonal)
    {
        Vector2Int delta = to - from;
        int absX = Mathf.Abs(delta.x);
        int absY = Mathf.Abs(delta.y);
        if (absX + absY == 1)
            return true;

        return allowDiagonal && absX == 1 && absY == 1;
    }

    public void SetSelectedTile(Vector2Int coord)
    {
        selectedCoord = coord;
        RefreshAllViews();
    }

    public void RefreshTile(Vector2Int coord)
    {
        if (views.TryGetValue(coord, out BoardTileView view) && view != null)
            view.Refresh();
    }

    public void RefreshAllViews()
    {
        foreach (BoardTileView view in views.Values)
        {
            if (view != null)
                view.Refresh();
        }
    }

    public Vector3 GridToWorld(Vector2Int coord)
    {
        float half = (BoardSize - 1) * 0.5f;
        return new Vector3((coord.x - half) * tileSpacing, (coord.y - half) * tileSpacing, 0f);
    }

    public Color GetTileBaseColor(BoardTileData tile)
    {
        if (ShouldCollapseVisitedTile(tile))
            return Color.Lerp(visitedTileColor, Color.black, tileBackgroundDarkening * 0.65f);

        Color baseColor = GetDangerBandColor(tile.dangerBand);
        switch (tile.tileType)
        {
            case MapTileType.Empty:
                baseColor = emptyTileColor;
                break;
            case MapTileType.Rest:
                baseColor *= new Color(0.75f, 1.15f, 0.82f, 1f);
                break;
            case MapTileType.Merchant:
                baseColor *= new Color(1.08f, 1.02f, 0.84f, 1f);
                break;
            case MapTileType.Treasure:
                baseColor *= new Color(1.18f, 1.04f, 0.66f, 1f);
                break;
            case MapTileType.Forge:
                baseColor *= new Color(1.14f, 0.94f, 0.72f, 1f);
                break;
            case MapTileType.Event:
                baseColor *= new Color(0.82f, 0.92f, 1.18f, 1f);
                break;
            case MapTileType.SpecialDungeonExit:
                baseColor *= new Color(0.8f, 1.1f, 1.2f, 1f);
                break;
            case MapTileType.Elite:
                baseColor = Color.Lerp(baseColor, eliteTileAccentColor, 0.48f);
                break;
            case MapTileType.Key:
                baseColor *= new Color(1.28f, 1.22f, 0.62f, 1f);
                break;
            case MapTileType.Boss:
                baseColor = lockedBossColor;
                break;
        }

        return Color.Lerp(baseColor, Color.black, tileBackgroundDarkening);
    }

    public Color GetTileInnerGlowColor(BoardTileData tile)
    {
        Color baseColor = ShouldKeepPersistentPresentation(tile)
            ? GetDangerBandColor(tile.dangerBand)
            : (tile.isVisited ? visitedTileColor : GetDangerBandColor(tile.dangerBand));

        switch (tile.tileType)
        {
            case MapTileType.Empty:
                baseColor = emptyTileColor;
                break;
            case MapTileType.Event:
                baseColor *= new Color(0.9f, 0.98f, 1.12f, 1f);
                break;
            case MapTileType.Merchant:
                baseColor *= new Color(1.08f, 1.04f, 0.88f, 1f);
                break;
            case MapTileType.Treasure:
                baseColor *= new Color(1.18f, 1.08f, 0.72f, 1f);
                break;
            case MapTileType.Forge:
                baseColor *= new Color(1.12f, 0.96f, 0.82f, 1f);
                break;
            case MapTileType.Rest:
                baseColor *= new Color(0.88f, 1.12f, 0.9f, 1f);
                break;
            case MapTileType.SpecialDungeonExit:
                baseColor *= new Color(0.78f, 1.08f, 1.22f, 1f);
                break;
            case MapTileType.Elite:
                baseColor = Color.Lerp(baseColor, eliteTileAccentColor, 0.58f);
                break;
            case MapTileType.Key:
                baseColor *= new Color(1.24f, 1.18f, 0.72f, 1f);
                break;
            case MapTileType.Boss:
                baseColor = Color.Lerp(lockedBossColor, Color.white, 0.08f);
                break;
        }

        if (ShouldCollapseVisitedTile(tile))
            baseColor = Color.Lerp(baseColor, Color.white, 0.08f);

        return MultiplyColor(baseColor, tileInnerGlowTint);
    }

    public Color GetTileIconGlowColor(BoardTileData tile)
    {
        if (tile == null || ShouldCollapseVisitedTile(tile) || tile.tileType == MapTileType.Empty)
            return Color.clear;

        Color baseColor;
        switch (tile.tileType)
        {
            case MapTileType.Battle:
                baseColor = new Color(1f, 0.82f, 0.36f, 0.34f);
                break;
            case MapTileType.Elite:
                baseColor = new Color(1f, 0.58f, 0.28f, 0.42f);
                break;
            case MapTileType.Event:
                baseColor = new Color(0.7f, 0.9f, 1f, 0.3f);
                break;
            case MapTileType.Merchant:
                baseColor = new Color(1f, 0.9f, 0.54f, 0.34f);
                break;
            case MapTileType.Treasure:
                baseColor = new Color(1f, 0.78f, 0.28f, 0.42f);
                break;
            case MapTileType.Forge:
                baseColor = new Color(1f, 0.86f, 0.54f, 0.34f);
                break;
            case MapTileType.Rest:
                baseColor = new Color(0.72f, 1f, 0.78f, 0.3f);
                break;
            case MapTileType.Key:
                baseColor = new Color(1f, 0.97f, 0.42f, 0.46f);
                break;
            case MapTileType.Boss:
                baseColor = new Color(1f, 0.68f, 0.72f, 0.4f);
                break;
            case MapTileType.SpecialDungeonExit:
                baseColor = new Color(0.52f, 0.95f, 1f, 0.4f);
                break;
            default:
                baseColor = new Color(1f, 1f, 1f, 0.28f);
                break;
        }

        return baseColor;
    }

    public Color GetTileFrameColor(BoardTileData tile)
    {
        if (tile == null)
            return tileFrameColor;

        Color baseColor = ShouldCollapseVisitedTile(tile)
            ? Color.Lerp(visitedTileColor, Color.white, 0.22f)
            : GetDangerBandColor(tile.dangerBand);

        switch (tile.tileType)
        {
            case MapTileType.Empty:
                baseColor = Color.Lerp(emptyTileColor, Color.white, 0.18f);
                break;
            case MapTileType.Event:
                baseColor = Color.Lerp(baseColor, new Color(0.86f, 0.94f, 1f, 1f), 0.42f);
                break;
            case MapTileType.Merchant:
                baseColor = Color.Lerp(baseColor, new Color(1f, 0.92f, 0.62f, 1f), 0.46f);
                break;
            case MapTileType.Treasure:
                baseColor = Color.Lerp(baseColor, new Color(1f, 0.82f, 0.36f, 1f), 0.55f);
                break;
            case MapTileType.Forge:
                baseColor = Color.Lerp(baseColor, new Color(1f, 0.86f, 0.58f, 1f), 0.45f);
                break;
            case MapTileType.Rest:
                baseColor = Color.Lerp(baseColor, new Color(0.84f, 1f, 0.86f, 1f), 0.42f);
                break;
            case MapTileType.SpecialDungeonExit:
                baseColor = Color.Lerp(baseColor, new Color(0.62f, 0.96f, 1f, 1f), 0.52f);
                break;
            case MapTileType.Key:
                baseColor = Color.Lerp(baseColor, new Color(1f, 0.98f, 0.48f, 1f), 0.58f);
                break;
            case MapTileType.Elite:
                baseColor = Color.Lerp(baseColor, eliteTileAccentColor, 0.58f);
                break;
            case MapTileType.Boss:
                baseColor = Color.Lerp(lockedBossColor, new Color(1f, 0.76f, 0.8f, 1f), 0.3f);
                break;
        }

        return MultiplyColor(baseColor, tileFrameColor);
    }

    public Color GetTileHighlightColor(BoardTileData tile)
    {
        if (tile.coord == selectedCoord)
            return selectedHighlightColor;
        if (tile.isSpawnTile)
            return spawnHighlightColor;
        return Color.clear;
    }

    public bool ShouldHighlightTile(BoardTileData tile)
    {
        return tile.coord == selectedCoord || tile.isSpawnTile;
    }

    public Sprite GetTileIcon(BoardTileData tile)
    {
        if (tile == null || ShouldCollapseVisitedTile(tile) || tile.tileType == MapTileType.Empty)
            return null;

        switch (tile.tileType)
        {
            case MapTileType.Battle:
                return battleTileIcon;
            case MapTileType.Elite:
                return eliteTileIcon;
            case MapTileType.Event:
                return eventTileIcon;
            case MapTileType.Merchant:
                return merchantTileIcon;
            case MapTileType.Treasure:
                return treasureTileIcon;
            case MapTileType.Forge:
                return forgeTileIcon;
            case MapTileType.Rest:
                return restTileIcon;
            case MapTileType.Key:
                return keyTileIcon;
            case MapTileType.Boss:
                return bossTileIcon;
            case MapTileType.SpecialDungeonExit:
                return specialDungeonExitIcon != null ? specialDungeonExitIcon : DefaultTileSprite;
            default:
                return null;
        }
    }

    public Color GetFallbackIconColor(BoardTileData tile)
    {
        if (tile == null || ShouldCollapseVisitedTile(tile) || tile.tileType == MapTileType.Empty)
            return new Color(0.72f, 0.75f, 0.8f, 1f);

        switch (tile.tileType)
        {
            case MapTileType.Empty:
                return new Color(0.72f, 0.75f, 0.8f, 1f);
            case MapTileType.Battle:
                return new Color(1f, 0.86f, 0.35f, 1f);
            case MapTileType.Elite:
                return new Color(1f, 0.56f, 0.28f, 1f);
            case MapTileType.Event:
                return new Color(0.8f, 0.92f, 1f, 1f);
            case MapTileType.Merchant:
                return new Color(1f, 0.9f, 0.58f, 1f);
            case MapTileType.Treasure:
                return new Color(1f, 0.82f, 0.32f, 1f);
            case MapTileType.Forge:
                return new Color(1f, 0.88f, 0.56f, 1f);
            case MapTileType.Rest:
                return new Color(0.78f, 1f, 0.78f, 1f);
            case MapTileType.Key:
                return new Color(1f, 0.98f, 0.42f, 1f);
            case MapTileType.Boss:
                return new Color(1f, 0.68f, 0.68f, 1f);
            case MapTileType.SpecialDungeonExit:
                return new Color(0.58f, 0.94f, 1f, 1f);
            default:
                return fallbackIconColor;
        }
    }

    public string GetTilePrimaryText(BoardTileData tile, bool fallbackMode)
    {
        return string.Empty;
    }

    public Color GetTilePrimaryTextColor(BoardTileData tile)
    {
        return tile.isSpawnTile && tile.tileType != MapTileType.Key ? new Color(0.84f, 0.98f, 1f, 1f) : fallbackIconColor;
    }

    public string GetTilePreviewText(BoardTileData tile)
    {
        return string.Empty;
    }

    public Color GetTilePreviewTextColor(BoardTileData tile)
    {
        if (tile.tileType == MapTileType.Elite)
            return new Color(1f, 0.84f, 0.72f, 1f);
        return tile.tileType == MapTileType.Boss ? new Color(1f, 0.76f, 0.76f, 1f) : previewTextColor;
    }

    public bool TryGetTileAtScreenPosition(Camera targetCamera, Vector2 screenPosition, out BoardTileData tile)
    {
        tile = null;
        if (targetCamera == null)
            return false;

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
            return false;

        BoardTileView view = hit.collider.GetComponent<BoardTileView>();
        if (view == null)
            view = hit.collider.GetComponentInParent<BoardTileView>();

        if (view == null || view.TileData == null)
            return false;

        tile = view.TileData;
        return true;
    }

    public void NotifyTileClicked(BoardTileData tile)
    {
        if (tile == null)
            return;

        TileClicked?.Invoke(tile);
    }

    private void CreateTileView(BoardTileData tile)
    {
        BoardTileView view;
        if (tileViewPrefab != null)
        {
            view = Instantiate(tileViewPrefab, boardRoot);
        }
        else
        {
            GameObject go = new GameObject($"Tile_{tile.coord.x}_{tile.coord.y}");
            go.transform.SetParent(boardRoot, false);
            view = go.AddComponent<BoardTileView>();
        }

        view.Setup(this, tile);
        views[tile.coord] = view;
    }

    private void PopulatePreview(BoardTileData tile, System.Random rng)
    {
        if (!tile.IsHostileTile)
            return;

        if (tile.tileType == MapTileType.Boss)
        {
            tile.previewEnemyCount = 1;
            tile.previewDangerRating = MapDangerRatingService.ResolveDefaultRating(tile.tileType, tile.dangerBand);
            tile.previewTag = "Boss";
            return;
        }

        bool isElite = tile.tileType == MapTileType.Elite;
        int minEnemies;
        int maxEnemiesExclusive;

        switch (tile.dangerBand)
        {
            case MapDangerBand.Outer:
                minEnemies = isElite ? 1 : 1;
                maxEnemiesExclusive = isElite ? 3 : 4;
                break;
            case MapDangerBand.Mid:
                minEnemies = isElite ? 2 : 1;
                maxEnemiesExclusive = isElite ? 4 : 5;
                break;
            case MapDangerBand.Inner:
                minEnemies = isElite ? 2 : 2;
                maxEnemiesExclusive = isElite ? 5 : 5;
                break;
            default:
                minEnemies = isElite ? 3 : 2;
                maxEnemiesExclusive = isElite ? 5 : 6;
                break;
        }

        tile.previewEnemyCount = rng.Next(minEnemies, maxEnemiesExclusive);
        tile.previewDangerRating = MapDangerRatingService.ResolveDefaultRating(tile.tileType, tile.dangerBand);
        tile.previewTag = isElite ? $"Elite {tile.GetDangerLabel()}" : tile.GetDangerLabel();
    }

    private void ApplyGuaranteedTileMinimums(
        DifficultyConfig_SO config,
        System.Random rng,
        HashSet<Vector2Int> protectedCoords = null)
    {
        if (config == null || tiles.Count == 0)
            return;

        List<MapTileType> guaranteedTypes = GetGuaranteedTileTypes(config);
        if (guaranteedTypes.Count == 0)
            return;

        Dictionary<MapTileType, int> tileCounts = BuildGuaranteedTileCounts();

        for (int i = 0; i < guaranteedTypes.Count; i++)
        {
            MapTileType targetType = guaranteedTypes[i];
            int targetMinimum = Mathf.Max(0, config.GetMinimumCount(targetType));
            while (GetCount(tileCounts, targetType) < targetMinimum)
            {
                MapTileType donorType = FindGuaranteeDonorType(config, tileCounts, targetType);
                if (donorType == MapTileType.Empty)
                    break;

                List<BoardTileData> donorTiles = GetTilesByType(donorType, protectedCoords);
                if (donorTiles.Count == 0)
                    break;

                BoardTileData chosenTile = donorTiles[rng.Next(0, donorTiles.Count)];
                chosenTile.tileType = targetType;
                tileCounts[donorType] = Mathf.Max(0, GetCount(tileCounts, donorType) - 1);
                tileCounts[targetType] = GetCount(tileCounts, targetType) + 1;
            }
        }
    }

    private List<MapTileType> GetGuaranteedTileTypes(DifficultyConfig_SO config)
    {
        List<MapTileType> result = new List<MapTileType>();
        if (config == null || config.tileMinimums == null)
            return result;

        for (int i = 0; i < config.tileMinimums.Count; i++)
        {
            DifficultyConfig_SO.TileMinimumEntry entry = config.tileMinimums[i];
            if (entry == null || entry.minimumCount <= 0)
                continue;

            MapTileType tileType = entry.tileType;
            if (!IsGuaranteeEligibleTileType(tileType) || result.Contains(tileType))
                continue;

            result.Add(tileType);
        }

        return result;
    }

    private Dictionary<MapTileType, int> BuildGuaranteedTileCounts()
    {
        Dictionary<MapTileType, int> counts = new Dictionary<MapTileType, int>();
        MapTileType[] eligibleTypes = GetGuaranteeEligibleTileTypes();
        for (int i = 0; i < eligibleTypes.Length; i++)
            counts[eligibleTypes[i]] = 0;

        foreach (BoardTileData tile in tiles.Values)
        {
            if (tile == null || !counts.ContainsKey(tile.tileType))
                continue;

            counts[tile.tileType] += 1;
        }

        return counts;
    }

    private MapTileType FindGuaranteeDonorType(
        DifficultyConfig_SO config,
        Dictionary<MapTileType, int> tileCounts,
        MapTileType targetType)
    {
        MapTileType[] eligibleTypes = GetGuaranteeEligibleTileTypes();
        MapTileType preferredDonor = MapTileType.Empty;
        int preferredDonorCount = -1;

        for (int i = 0; i < eligibleTypes.Length; i++)
        {
            MapTileType candidateType = eligibleTypes[i];
            if (candidateType == targetType)
                continue;

            int currentCount = GetCount(tileCounts, candidateType);
            int minimumCount = Mathf.Max(0, config.GetMinimumCount(candidateType));
            if (currentCount <= minimumCount)
                continue;

            if (currentCount > preferredDonorCount)
            {
                preferredDonor = candidateType;
                preferredDonorCount = currentCount;
            }
        }

        if (preferredDonor != MapTileType.Empty)
            return preferredDonor;

        MapTileType fallbackDonor = MapTileType.Empty;
        int fallbackCount = -1;
        for (int i = 0; i < eligibleTypes.Length; i++)
        {
            MapTileType candidateType = eligibleTypes[i];
            if (candidateType == targetType)
                continue;

            int currentCount = GetCount(tileCounts, candidateType);
            if (currentCount <= 0)
                continue;

            if (currentCount > fallbackCount)
            {
                fallbackDonor = candidateType;
                fallbackCount = currentCount;
            }
        }

        return fallbackDonor;
    }

    private List<BoardTileData> GetTilesByType(MapTileType tileType, HashSet<Vector2Int> protectedCoords = null)
    {
        List<BoardTileData> result = new List<BoardTileData>();
        foreach (BoardTileData tile in tiles.Values)
        {
            if (tile != null
                && tile.tileType == tileType
                && IsGuaranteeEligibleTile(tile)
                && (protectedCoords == null || !protectedCoords.Contains(tile.coord)))
            {
                result.Add(tile);
            }
        }

        return result;
    }

    private MapTileType PickWeightedTileType(DifficultyConfig_SO config, System.Random rng)
    {
        int battleWeight = Mathf.Max(0, config.GetWeight(MapTileType.Battle));
        int eliteWeight = Mathf.Max(0, config.GetWeight(MapTileType.Elite));
        int eventWeight = Mathf.Max(0, config.GetWeight(MapTileType.Event));
        int merchantWeight = Mathf.Max(0, config.GetWeight(MapTileType.Merchant));
        int treasureWeight = Mathf.Max(0, config.GetWeight(MapTileType.Treasure));
        int forgeWeight = Mathf.Max(0, config.GetWeight(MapTileType.Forge));
        int restWeight = Mathf.Max(0, config.GetWeight(MapTileType.Rest));
        int total = battleWeight + eliteWeight + eventWeight + merchantWeight + treasureWeight + forgeWeight + restWeight;

        if (total <= 0)
            return MapTileType.Battle;

        int roll = rng.Next(0, total);
        if (roll < battleWeight)
            return MapTileType.Battle;

        roll -= battleWeight;
        if (roll < eliteWeight)
            return MapTileType.Elite;

        roll -= eliteWeight;
        if (roll < eventWeight)
            return MapTileType.Event;

        roll -= eventWeight;
        if (roll < merchantWeight)
            return MapTileType.Merchant;

        roll -= merchantWeight;
        if (roll < treasureWeight)
            return MapTileType.Treasure;

        roll -= treasureWeight;
        if (roll < forgeWeight)
            return MapTileType.Forge;

        return MapTileType.Rest;
    }

    private Vector2Int PickSpawnCoord(DifficultyConfig_SO config, System.Random rng)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        int maxRing = Mathf.Max(1, BoardSize / 2);
        int outerTwoRingsMin = Mathf.Max(1, maxRing - 1);
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (coord == BossCoord)
                    continue;

                if (GetRingDistance(coord, BossCoord) >= outerTwoRingsMin)
                    candidates.Add(coord);
            }
        }

        if (candidates.Count == 0)
            candidates.Add(new Vector2Int(0, 0));

        return candidates[rng.Next(0, candidates.Count)];
    }

    private HashSet<Vector2Int> PickKeyCoords(DifficultyConfig_SO config, System.Random rng)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (coord == BossCoord || coord == SpawnCoord)
                    continue;

                candidates.Add(coord);
            }
        }

        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        int targetCount = Mathf.Min(config.generatedKeyTileCount, candidates.Count);
        while (result.Count < targetCount && candidates.Count > 0)
        {
            int index = rng.Next(0, candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return result;
    }

    private HashSet<Vector2Int> PickKeyCoordsForRegeneration(DifficultyConfig_SO config, System.Random rng, Vector2Int currentCoord)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (coord == BossCoord || coord == currentCoord)
                    continue;

                candidates.Add(coord);
            }
        }

        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        int targetCount = Mathf.Min(config.generatedKeyTileCount, candidates.Count);
        while (result.Count < targetCount && candidates.Count > 0)
        {
            int index = rng.Next(0, candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return result;
    }

    private MapDangerBand CalculateDangerBand(Vector2Int coord)
    {
        int ring = GetRingDistance(coord, BossCoord);
        int maxRing = Mathf.Max(1, BoardSize / 2);
        float normalized = 1f - (ring / (float)maxRing);

        if (ring == 0)
            return MapDangerBand.Core;
        if (normalized >= 0.72f)
            return MapDangerBand.Inner;
        if (normalized >= 0.36f)
            return MapDangerBand.Mid;
        return MapDangerBand.Outer;
    }

    private static int GetRingDistance(Vector2Int coord, Vector2Int center)
    {
        return Mathf.Max(Mathf.Abs(coord.x - center.x), Mathf.Abs(coord.y - center.y));
    }

    private static int GetCount(Dictionary<MapTileType, int> counts, MapTileType tileType)
    {
        return counts != null && counts.TryGetValue(tileType, out int count) ? count : 0;
    }

    private Color GetDangerBandColor(MapDangerBand band)
    {
        switch (band)
        {
            case MapDangerBand.Outer:
                return outerTileColor;
            case MapDangerBand.Mid:
                return midTileColor;
            case MapDangerBand.Inner:
                return innerTileColor;
            default:
                return coreTileColor;
        }
    }

    private void EnsureDefaultSprite()
    {
        if (defaultTileSprite != null)
        {
            runtimeWhiteSprite = defaultTileSprite;
        }
        else
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, false);
            runtimeWhiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        if (runtimeRadialSprite == null)
            runtimeRadialSprite = CreateRadialGlowSprite("BoardTileInnerGlow", 128);
    }

    private void ClearBoard()
    {
        tiles.Clear();
        views.Clear();

        if (boardRoot == null)
            return;

        for (int i = boardRoot.childCount - 1; i >= 0; i--)
            Destroy(boardRoot.GetChild(i).gameObject);
    }

    private DifficultyConfig_SO CreateFallbackConfig()
    {
        DifficultyConfig_SO fallback = ScriptableObject.CreateInstance<DifficultyConfig_SO>();
        fallback.difficultyName = "Fallback";
        fallback.boardSize = 7;
        fallback.startingActionPoints = 6;
        fallback.requiredKeys = 2;
        fallback.generatedKeyTileCount = 3;
        fallback.tileWeights = new List<DifficultyConfig_SO.TileWeightEntry>
        {
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Battle, weight = 4 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Elite, weight = 1 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Event, weight = 4 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Merchant, weight = 1 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Treasure, weight = 1 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Forge, weight = 1 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Rest, weight = 1 }
        };
        return fallback;
    }

    private static Color MultiplyColor(Color a, Color b)
    {
        return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
    }

    private static bool IsGuaranteeEligibleTileType(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Battle:
            case MapTileType.Elite:
            case MapTileType.Event:
            case MapTileType.Merchant:
            case MapTileType.Treasure:
            case MapTileType.Forge:
            case MapTileType.Rest:
                return true;
            default:
                return false;
        }
    }

    private static MapTileType[] GetGuaranteeEligibleTileTypes()
    {
        return new[]
        {
            MapTileType.Battle,
            MapTileType.Elite,
            MapTileType.Event,
            MapTileType.Merchant,
            MapTileType.Treasure,
            MapTileType.Forge,
            MapTileType.Rest
        };
    }

    private static bool IsGuaranteeEligibleTile(BoardTileData tile)
    {
        return tile != null && IsGuaranteeEligibleTileType(tile.tileType);
    }

    private static bool ShouldCollapseVisitedTile(BoardTileData tile)
    {
        if (tile == null || !tile.isVisited)
            return false;

        return tile.tileType != MapTileType.Boss
            && tile.tileType != MapTileType.Merchant
            && tile.tileType != MapTileType.Forge
            && tile.tileType != MapTileType.SpecialDungeonExit;
    }

    public bool ShouldKeepPersistentPresentation(BoardTileData tile)
    {
        return tile != null
            && (tile.tileType == MapTileType.Boss
                || tile.tileType == MapTileType.Merchant
                || tile.tileType == MapTileType.Forge
                || tile.tileType == MapTileType.SpecialDungeonExit);
    }

    private Sprite CreateRadialGlowSprite(string textureName, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = center.x;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / Mathf.Max(0.0001f, radius);
                float alpha;
                if (normalizedDistance <= 0.22f)
                {
                    alpha = 1f;
                }
                else
                {
                    float falloff = Mathf.Clamp01(1f - (normalizedDistance - 0.22f) / 0.78f);
                    alpha = falloff * falloff;
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}
