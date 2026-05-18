using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BattleSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene activeScene, LoadSceneMode mode)
    {
        bool hasEncounterForThisScene =
            BattleEncounterRuntime.CurrentEncounter != null &&
            !string.IsNullOrWhiteSpace(BattleEncounterRuntime.CurrentEncounter.targetSceneName) &&
            activeScene.name.Equals(BattleEncounterRuntime.CurrentEncounter.targetSceneName, StringComparison.OrdinalIgnoreCase);

        if (!hasEncounterForThisScene && !activeScene.name.Equals("BattleScene", StringComparison.OrdinalIgnoreCase))
            return;

        if (UnityEngine.Object.FindObjectOfType<BattleSceneEncounterBootstrap>() != null)
            return;

        Debug.Log($"[BattleSceneBootstrap] Scene loaded: {activeScene.name}, encounter match: {hasEncounterForThisScene}.");
        GameObject root = new GameObject("BattleSceneRoot");
        root.AddComponent<BattleSceneEncounterBootstrap>();
    }
}

[DisallowMultipleComponent]
public class BattleSceneEncounterBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private Transform alliesParent;
    [SerializeField] private Transform enemiesParent;
    [SerializeField] private Transform anchorRoot;
    [SerializeField] private Transform spawnedAlliesRoot;
    [SerializeField] private Transform spawnedEnemiesRoot;
    [SerializeField] private GameObject allyTemplatePrefab;
    [SerializeField] private GameObject enemyTemplatePrefab;

    [Header("Anchors")]
    [SerializeField] private string allyAnchorNamePrefix = "Ally";
    [SerializeField] private string enemyAnchorNamePrefix = "Enemy";

    [Header("Fallback Formation")]
    [SerializeField] private Vector3 allyFormationCenter = new Vector3(-4.8f, 0f, 0f);
    [SerializeField] private Vector3 enemyFormationCenter = new Vector3(4.8f, 0f, 0f);
    [SerializeField] private Vector3 formationSlotSpacing = new Vector3(0f, 1.8f, -0.05f);

    [Header("Visual")]
    [SerializeField] private Color allyBodyColor = new Color(0.72f, 0.9f, 1f, 1f);
    [SerializeField] private Color enemyBodyColor = new Color(1f, 0.52f, 0.52f, 1f);
    [SerializeField] private Vector3 allyBodyScale = new Vector3(1.15f, 1.15f, 1f);
    [SerializeField] private Vector3 enemyBodyScale = new Vector3(1.15f, 1.15f, 1f);
    [SerializeField] private int allySortingOrder = 20;
    [SerializeField] private int enemySortingOrder = 20;
    [SerializeField] private int weaponSortingOrderOffset = -5;
    [SerializeField] private float weaponDepthOffset = 0.05f;
    [SerializeField] private Vector2 allySpeedButtonLocalOffset = new Vector2(0f, 1f);
    [SerializeField] private Vector2 allyMultiSpeedButtonGroupOffset = Vector2.zero;
    [SerializeField] private float allyMultiSpeedButtonHorizontalSpacing = 0.82f;
    [SerializeField] private Vector2 enemySpeedButtonLocalOffset = new Vector2(0f, 0.8f);
    [SerializeField] private Vector2 enemyMultiSpeedButtonGroupOffset = Vector2.zero;
    [SerializeField] private float enemyMultiSpeedButtonHorizontalSpacing = 0.82f;

    private Sprite runtimeWhiteSprite;

    private void Awake()
    {
        EnsureBattleSetup();
    }

    private void Start()
    {
        SpawnEncounterIfAvailable();
    }

    private void EnsureBattleSetup()
    {
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null)
        {
            GameObject managerGo = new GameObject("BattleManager");
            battleManager = managerGo.AddComponent<BattleManager>();
        }

        if (!battleManager.IsInitialized)
            battleManager.Initialize();

        if (alliesParent == null)
            alliesParent = battleManager.alliesParent != null ? battleManager.alliesParent.transform : CreateRoot("AlliesParent");
        if (enemiesParent == null)
            enemiesParent = battleManager.enemiesParent != null ? battleManager.enemiesParent.transform : CreateRoot("EnemiesParent");
        if (anchorRoot == null)
            anchorRoot = battleManager.LocationPoint != null ? battleManager.LocationPoint : enemiesParent;

        battleManager.alliesParent = alliesParent.gameObject;
        battleManager.enemiesParent = enemiesParent.gameObject;
        if (!battleManager.alliesParent.activeSelf)
            battleManager.alliesParent.SetActive(true);
        if (!battleManager.enemiesParent.activeSelf)
            battleManager.enemiesParent.SetActive(true);

        if (spawnedAlliesRoot == null)
        {
            Transform existing = alliesParent.Find("SpawnedAllies");
            spawnedAlliesRoot = existing != null ? existing : CreateRoot("SpawnedAllies", alliesParent);
        }

        if (spawnedEnemiesRoot == null)
        {
            Transform existing = enemiesParent.Find("SpawnedEnemies");
            spawnedEnemiesRoot = existing != null ? existing : CreateRoot("SpawnedEnemies", enemiesParent);
        }

        if (!spawnedAlliesRoot.gameObject.activeSelf)
            spawnedAlliesRoot.gameObject.SetActive(true);
        if (!spawnedEnemiesRoot.gameObject.activeSelf)
            spawnedEnemiesRoot.gameObject.SetActive(true);

    }

    private void SpawnEncounterIfAvailable()
    {
        BattleEncounterData encounterData = BattleEncounterRuntime.ConsumeEncounter();
        if (encounterData == null || !encounterData.HasEnemies)
        {
            Debug.LogWarning("[BattleSceneEncounterBootstrap] No runtime encounter payload found. Keeping existing battle scene setup.");
            return;
        }

        ClearChildren(spawnedAlliesRoot);
        ClearChildren(spawnedEnemiesRoot);
        DisablePrebuiltCombatants<BattleAllyController>(alliesParent, spawnedAlliesRoot);
        DisablePrebuiltCombatants<BattleEnemyController>(enemiesParent, spawnedEnemiesRoot);

        if (!battleManager.IsInitialized)
            battleManager.Initialize();
        if (battleManager.ManagedData == null)
            battleManager.ManagedData = new BattleData();

        battleManager.ManagedData.EnemyCount = 0;
        battleManager.ManagedData.IsBattleActive = true;

        SpawnAllies(encounterData.Allies);
        SpawnEnemies(encounterData.Enemies, encounterData.encounterLabel);
    }

    private void SpawnAllies(IReadOnlyList<RunPartyMemberState> allies)
    {
        if (allies == null || allies.Count == 0)
        {
            Debug.LogWarning("[BattleSceneEncounterBootstrap] Encounter contains no party members. Allies will not be spawned.");
            return;
        }

        List<Transform> anchorPoints = CollectAnchorsByPrefix(allyAnchorNamePrefix);
        Debug.Log($"[BattleSceneEncounterBootstrap] Encounter requested {allies.Count} allies. Ally anchor count: {anchorPoints.Count}.");

        for (int i = 0; i < allies.Count; i++)
        {
            RunPartyMemberState allyState = allies[i];
            if (allyState == null)
                continue;

            Transform anchor = i < anchorPoints.Count ? anchorPoints[i] : null;
            Vector3 spawnPosition = anchor != null ? anchor.position : CalculateFallbackSpawnPosition(allyFormationCenter, i, allies.Count);
            Debug.Log($"[BattleSceneEncounterBootstrap] Spawning ally '{allyState.DisplayName}' at {(anchor != null ? anchor.name : "fallback position")} -> {spawnPosition}.");
            SpawnAlly(allyState, spawnPosition, i);
        }
    }

    private void SpawnEnemies(IReadOnlyList<EnemyData_SO> enemies, string encounterLabel)
    {
        int count = enemies != null ? enemies.Count : 0;
        List<Transform> anchorPoints = CollectAnchorsByPrefix(enemyAnchorNamePrefix);
        Debug.Log($"[BattleSceneEncounterBootstrap] Encounter '{encounterLabel}' requested {count} enemies. Enemy anchor count: {anchorPoints.Count}.");

        for (int i = 0; i < count; i++)
        {
            EnemyData_SO enemyData = enemies[i];
            if (enemyData == null)
            {
                Debug.LogWarning($"[BattleSceneEncounterBootstrap] Encounter enemy index {i} is null.");
                continue;
            }

            Transform anchor = i < anchorPoints.Count ? anchorPoints[i] : null;
            Vector3 spawnPosition = anchor != null ? anchor.position : CalculateFallbackSpawnPosition(enemyFormationCenter, i, count);
            Debug.Log($"[BattleSceneEncounterBootstrap] Spawning enemy '{enemyData.enemyName}' at {(anchor != null ? anchor.name : "fallback position")} -> {spawnPosition}.");
            SpawnEnemy(enemyData, spawnPosition, i);
        }

        Debug.Log($"[BattleSceneEncounterBootstrap] Spawned encounter '{encounterLabel}' with {battleManager.ManagedData.EnemyCount} enemies.");
    }

    public BattleEnemyController SpawnEnemyDuringBattle(EnemyData_SO enemyData)
    {
        if (enemyData == null)
            return null;

        EnsureBattleSetup();
        if (!battleManager.IsInitialized)
            battleManager.Initialize();
        if (battleManager.ManagedData == null)
            battleManager.ManagedData = new BattleData();

        int existingCount = 0;
        BattleEnemyController[] existingEnemies = enemiesParent != null
            ? enemiesParent.GetComponentsInChildren<BattleEnemyController>(false)
            : Array.Empty<BattleEnemyController>();
        for (int i = 0; i < existingEnemies.Length; i++)
        {
            if (existingEnemies[i] != null && !existingEnemies[i].IsDead)
                existingCount++;
        }

        List<Transform> anchorPoints = CollectAnchorsByPrefix(enemyAnchorNamePrefix);
        Transform anchor = existingCount < anchorPoints.Count ? anchorPoints[existingCount] : null;
        Vector3 spawnPosition = anchor != null
            ? anchor.position
            : CalculateFallbackSpawnPosition(enemyFormationCenter, existingCount, existingCount + 1);

        int previousEnemyCount = battleManager.ManagedData.EnemyCount;
        SpawnEnemy(enemyData, spawnPosition, existingCount);

        BattleEnemyController[] refreshedEnemies = enemiesParent != null
            ? enemiesParent.GetComponentsInChildren<BattleEnemyController>(false)
            : Array.Empty<BattleEnemyController>();
        BattleEnemyController spawnedEnemy = BattleSummonEffectService.FindNewlySpawnedEnemy(existingEnemies, refreshedEnemies, enemyData);
        if (spawnedEnemy != null)
        {
            if (battleManager.ManagedData.EnemyCount <= previousEnemyCount)
                battleManager.ManagedData.EnemyCount = previousEnemyCount + 1;
            return spawnedEnemy;
        }

        return null;
    }

    public BattleAllyController SpawnAllyFromEnemyDataDuringBattle(EnemyData_SO enemyData)
    {
        if (enemyData == null)
            return null;

        EnsureBattleSetup();
        if (!battleManager.IsInitialized)
            battleManager.Initialize();

        int existingCount = 0;
        BattleAllyController[] existingAllies = alliesParent != null
            ? alliesParent.GetComponentsInChildren<BattleAllyController>(false)
            : Array.Empty<BattleAllyController>();
        for (int i = 0; i < existingAllies.Length; i++)
        {
            if (existingAllies[i] != null && !existingAllies[i].IsDead)
                existingCount++;
        }

        List<Transform> anchorPoints = CollectAnchorsByPrefix(allyAnchorNamePrefix);
        Transform anchor = existingCount < anchorPoints.Count ? anchorPoints[existingCount] : null;
        Vector3 spawnPosition = anchor != null
            ? anchor.position
            : CalculateFallbackSpawnPosition(allyFormationCenter, existingCount, existingCount + 1);

        return SpawnEnemyDataAsAlly(enemyData, spawnPosition, existingCount);
    }

    private void SpawnAlly(RunPartyMemberState allyState, Vector3 worldPosition, int index)
    {
        EnsureWhiteSprite();

        GameObject allyObject = BuildAllyObject(allyState, worldPosition, index);

        AllyController allyController = GetOrCreateRootComponent<AllyController>(allyObject);
        allyController.characterData = allyState.characterData;

        if (HasCharacterPrefab(allyState))
            ApplyActorRendererSettings(allyObject, allySortingOrder, true, battleManager != null ? battleManager.EnemyMaterial : null);
        else
            ApplyActorVisual(
                allyObject,
                allyState.BattleSprite,
                allyBodyColor,
                allyBodyScale,
                Vector3.zero,
                allySortingOrder,
                true,
                battleManager != null ? battleManager.EnemyMaterial : null);

        EnsureActorSortingGroup(allyObject, allySortingOrder);

        Transform characterHoldPoint = FindChildRecursive(allyObject.transform, "HoldPoint");
        SpriteRenderer allyPrimaryRenderer = FindPrimaryBodyRenderer(allyObject.transform);
        allyController.weapon = CreateEquippedWeapon(
            allyState.weaponData,
            allyObject.transform,
            characterHoldPoint,
            allyPrimaryRenderer,
            allySortingOrder + weaponSortingOrderOffset);
        if (allyController.weapon == null)
        {
            allyController.weapon = allyObject.GetComponent<Weapon>();
            if (allyController.weapon == null)
                allyController.weapon = allyObject.AddComponent<Weapon>();
            allyController.weapon.weaponData = allyState.weaponData;
            allyController.weapon.HoldPoint = characterHoldPoint;
        }

        BattleAllyController battleAlly = GetOrCreateRootComponent<BattleAllyController>(allyObject);
        battleAlly.ApplyRuntimeState(allyState);
        Debug.Log($"[BattleSceneEncounterBootstrap] Ally runtime state applied: {allyState.DisplayName} HP {battleAlly.CurrentHP}/{battleAlly.MaxHP}, Level {battleAlly.Level}, EXP {battleAlly.Experience}.");

        float topY = HasCharacterPrefab(allyState)
            ? CalculateActorTopLocalY(allyObject.transform)
            : CalculateSpriteTopLocalY(allyState.BattleSprite, allyBodyScale, Vector3.zero);
        List<SpeedButton> allyButtons = CreateOrAttachSpeedButtons(
            allyObject.transform,
            topY,
            SpeedButton.ButtonType.Ally,
            allyState.weaponData != null ? allyState.weaponData.speedButtonCount : 1,
            allySpeedButtonLocalOffset,
            allyMultiSpeedButtonGroupOffset,
            allyMultiSpeedButtonHorizontalSpacing);
        battleAlly.RegisterSpeedButtons(allyButtons);
        battleAlly.ConfigureSpeedButtonLayout(
            battleAlly.speedButton,
            topY,
            allySpeedButtonLocalOffset,
            allyMultiSpeedButtonGroupOffset,
            allyMultiSpeedButtonHorizontalSpacing);

        if (!allyObject.activeSelf)
            allyObject.SetActive(true);
    }

    private void SpawnEnemy(EnemyData_SO enemyData, Vector3 worldPosition, int index)
    {
        EnsureWhiteSprite();
        Vector3 resolvedEnemyScale = ResolveEnemyBodyScale(enemyData);
        Vector3 resolvedEnemyOffset = ResolveEnemyVisualOffset(enemyData);

        GameObject enemyObject;
        if (enemyTemplatePrefab != null)
        {
            enemyObject = Instantiate(enemyTemplatePrefab, spawnedEnemiesRoot);
            enemyObject.name = string.IsNullOrWhiteSpace(enemyData.enemyName) ? $"Enemy_{index + 1}" : enemyData.enemyName;
            enemyObject.transform.position = worldPosition;
            enemyObject.transform.rotation = Quaternion.identity;
            enemyObject.SetActive(false);
        }
        else
        {
            enemyObject = BuildRuntimeEnemy(enemyData, worldPosition, index);
        }

        BattleEnemyController enemyController = GetOrCreateRootComponent<BattleEnemyController>(enemyObject);
        enemyController.ApplyEnemyData(enemyData);
        Sprite currentStageSprite = enemyController.GetCurrentStageSprite();
        BattleEnemyDetailClickHandler detailClickHandler = enemyObject.GetComponent<BattleEnemyDetailClickHandler>();
        if (detailClickHandler == null)
            detailClickHandler = enemyObject.AddComponent<BattleEnemyDetailClickHandler>();
        detailClickHandler.Bind(enemyController);

        ApplyActorVisual(
            enemyObject,
            currentStageSprite,
            enemyBodyColor,
            resolvedEnemyScale,
            resolvedEnemyOffset,
            enemySortingOrder,
            true,
            battleManager != null ? battleManager.EnemyMaterial : null);
        EnsureActorSortingGroup(enemyObject, enemySortingOrder);

        float topY = CalculateSpriteTopLocalY(
            currentStageSprite,
            resolvedEnemyScale,
            resolvedEnemyOffset);
        List<SpeedButton> enemyButtons = CreateOrAttachSpeedButtons(
            enemyObject.transform,
            topY,
            SpeedButton.ButtonType.Enemy,
            enemyData != null ? enemyData.GetStageSpeedButtonCount(enemyController.CurrentStageIndex) : enemyController.EffectiveSpeedButtonCount,
            enemySpeedButtonLocalOffset,
            enemyMultiSpeedButtonGroupOffset,
            enemyMultiSpeedButtonHorizontalSpacing);
        enemyController.RegisterSpeedButtons(enemyButtons);
        enemyController.ConfigureSpeedButtonLayout(
            enemyButtons.Count > 0 ? enemyButtons[0] : null,
            SpeedButton.ButtonType.Enemy,
            enemySpeedButtonLocalOffset,
            enemyMultiSpeedButtonGroupOffset,
            enemyMultiSpeedButtonHorizontalSpacing);

        if (!enemyObject.activeSelf)
            enemyObject.SetActive(true);

        battleManager.AddEnemy();
    }

    private BattleAllyController SpawnEnemyDataAsAlly(EnemyData_SO enemyData, Vector3 worldPosition, int index)
    {
        EnsureWhiteSprite();

        CharacterData_SO runtimeCharacter = ScriptableObject.CreateInstance<CharacterData_SO>();
        runtimeCharacter.name = $"{enemyData.name}_TemporaryAllyCharacter";
        runtimeCharacter.characterName = string.IsNullOrWhiteSpace(enemyData.enemyName) ? enemyData.name : enemyData.enemyName;
        runtimeCharacter.baseHP = Mathf.Max(1, enemyData.baseHP);
        runtimeCharacter.minSpeed = Mathf.Max(0, enemyData.minSpeed);
        runtimeCharacter.maxSpeed = Mathf.Max(runtimeCharacter.minSpeed, enemyData.maxSpeed);
        runtimeCharacter.battleSprite = enemyData.GetStageSprite(0);

        Weapon_SO runtimeWeapon = ScriptableObject.CreateInstance<Weapon_SO>();
        runtimeWeapon.name = $"{enemyData.name}_TemporaryAllyWeapon";
        runtimeWeapon.weaponName = runtimeCharacter.characterName;
        runtimeWeapon.speedButtonCount = Mathf.Max(1, enemyData.GetStageSpeedButtonCount(0));
        runtimeWeapon.skillPool = enemyData.skillPool != null ? enemyData.skillPool.ToArray() : Array.Empty<EnemySkill_SO>();

        RunPartyMemberState allyState = new RunPartyMemberState
        {
            memberId = runtimeCharacter.characterName,
            characterData = runtimeCharacter,
            weaponData = runtimeWeapon,
            level = 1,
            experience = 0,
            maxHP = runtimeCharacter.baseHP,
            currentHP = runtimeCharacter.baseHP
        };

        SpawnAlly(allyState, worldPosition, index);
        BattleAllyController[] refreshedAllies = alliesParent != null
            ? alliesParent.GetComponentsInChildren<BattleAllyController>(false)
            : Array.Empty<BattleAllyController>();
        BattleAllyController spawned = refreshedAllies.Length > 0 ? refreshedAllies[refreshedAllies.Length - 1] : null;
        if (spawned != null && enemyData.battleStartStatusGains != null)
        {
            for (int i = 0; i < enemyData.battleStartStatusGains.Count; i++)
            {
                EnemyData_SO.StatusGainEntry gain = enemyData.battleStartStatusGains[i];
                if (gain.statusData != null && gain.stackAmount > 0)
                    spawned.AddStatus(gain.statusData, gain.statusData.StatusId, gain.statusData.DisplayName, gain.stackAmount);
            }
        }

        return spawned;
    }

    private GameObject BuildAllyObject(RunPartyMemberState allyState, Vector3 worldPosition, int index)
    {
        if (HasCharacterPrefab(allyState))
            return BuildPrefabBackedAlly(allyState, worldPosition, index);

        if (allyTemplatePrefab != null)
        {
            GameObject allyObject = Instantiate(allyTemplatePrefab, spawnedAlliesRoot);
            allyObject.name = string.IsNullOrWhiteSpace(allyState.DisplayName) ? $"Ally_{index + 1}" : allyState.DisplayName;
            allyObject.transform.position = worldPosition;
            allyObject.transform.rotation = Quaternion.identity;
            allyObject.SetActive(false);
            return allyObject;
        }

        return BuildRuntimeAlly(allyState, worldPosition, index);
    }

    private GameObject BuildPrefabBackedAlly(RunPartyMemberState allyState, Vector3 worldPosition, int index)
    {
        GameObject root = new GameObject(string.IsNullOrWhiteSpace(allyState.DisplayName) ? $"Ally_{index + 1}" : allyState.DisplayName);
        root.transform.SetParent(spawnedAlliesRoot, false);
        root.transform.position = worldPosition;
        root.transform.rotation = Quaternion.identity;
        root.SetActive(false);

        PolygonCollider2D collider2D = root.AddComponent<PolygonCollider2D>();
        collider2D.isTrigger = true;

        LineRenderer lineRenderer = root.AddComponent<LineRenderer>();
        lineRenderer.enabled = false;

        GameObject visual = Instantiate(allyState.characterData.characterPrefab, root.transform);
        visual.name = "CharacterVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = allyBodyScale;

        AlignVisualRootToAnchor(root.transform, visual.transform, collider2D);
        return root;
    }

    private GameObject BuildRuntimeAlly(RunPartyMemberState allyState, Vector3 worldPosition, int index)
    {
        EnsureWhiteSprite();

        GameObject root = new GameObject(string.IsNullOrWhiteSpace(allyState.DisplayName) ? $"Ally_{index + 1}" : allyState.DisplayName);
        root.transform.SetParent(spawnedAlliesRoot, false);
        root.transform.position = worldPosition;
        root.transform.rotation = Quaternion.identity;
        root.SetActive(false);

        PolygonCollider2D collider2D = root.AddComponent<PolygonCollider2D>();
        collider2D.isTrigger = true;

        LineRenderer lineRenderer = root.AddComponent<LineRenderer>();
        lineRenderer.enabled = false;

        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = allyState.BattleSprite != null ? allyState.BattleSprite : runtimeWhiteSprite;
        bodyRenderer.color = allyState.BattleSprite != null ? Color.white : allyBodyColor;
        bodyRenderer.sortingOrder = allySortingOrder;

        ApplySpriteAnchorAlignment(allyState.BattleSprite, allyBodyScale, Vector3.zero, body.transform, collider2D);
        return root;
    }

    private GameObject BuildRuntimeEnemy(EnemyData_SO enemyData, Vector3 worldPosition, int index)
    {
        EnsureWhiteSprite();

        GameObject root = new GameObject(string.IsNullOrWhiteSpace(enemyData.enemyName) ? $"Enemy_{index + 1}" : enemyData.enemyName);
        root.transform.SetParent(spawnedEnemiesRoot, false);
        root.transform.position = worldPosition;
        root.transform.rotation = Quaternion.identity;
        root.SetActive(false);

        PolygonCollider2D collider2D = root.AddComponent<PolygonCollider2D>();
        collider2D.isTrigger = true;

        LineRenderer lineRenderer = root.AddComponent<LineRenderer>();
        lineRenderer.enabled = false;

        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        Sprite initialSprite = enemyData != null ? enemyData.GetStageSprite(0) : null;
        bodyRenderer.sprite = initialSprite != null ? initialSprite : runtimeWhiteSprite;
        bodyRenderer.color = initialSprite != null ? Color.white : enemyBodyColor;
        bodyRenderer.sortingOrder = enemySortingOrder;
        bodyRenderer.shadowCastingMode = ShadowCastingMode.On;
        bodyRenderer.receiveShadows = true;
        if (battleManager != null && battleManager.EnemyMaterial != null)
            bodyRenderer.sharedMaterial = battleManager.EnemyMaterial;

        ApplySpriteAnchorAlignment(initialSprite, ResolveEnemyBodyScale(enemyData), ResolveEnemyVisualOffset(enemyData), body.transform, collider2D);
        return root;
    }

    private void ApplyActorVisual(
        GameObject target,
        Sprite sprite,
        Color fallbackColor,
        Vector3 scale,
        Vector3 localOffset,
        int sortingOrder,
        bool applyShadowMaterial,
        Material material)
    {
        if (target == null)
            return;

        SpriteRenderer renderer = FindPrimaryBodyRenderer(target.transform);
        if (renderer == null)
            return;

        renderer.sprite = sprite != null ? sprite : runtimeWhiteSprite;
        renderer.color = sprite != null ? Color.white : fallbackColor;
        renderer.sortingOrder = sortingOrder;

        PolygonCollider2D polygonCollider = target.GetComponent<PolygonCollider2D>();
        if (polygonCollider == null)
        {
            polygonCollider = target.AddComponent<PolygonCollider2D>();
            polygonCollider.isTrigger = true;
        }

        ApplySpriteAnchorAlignment(sprite, scale, localOffset, renderer.transform, polygonCollider);

        if (applyShadowMaterial)
        {
            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer currentRenderer = renderers[i];
                if (currentRenderer == null)
                    continue;

                currentRenderer.shadowCastingMode = ShadowCastingMode.On;
                currentRenderer.receiveShadows = true;
                currentRenderer.sharedMaterial = material;
            }
        }
        else
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = null;
        }
    }

    private void ApplyActorRendererSettings(
        GameObject target,
        int targetSortingOrder,
        bool applyShadowMaterial,
        Material material)
    {
        if (target == null)
            return;

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        int minimumSortingOrder = int.MaxValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.GetComponentInParent<SpeedButton>() != null)
                continue;

            minimumSortingOrder = Mathf.Min(minimumSortingOrder, renderer.sortingOrder);
        }

        if (minimumSortingOrder == int.MaxValue)
            minimumSortingOrder = 0;

        int sortingDelta = targetSortingOrder - minimumSortingOrder;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.GetComponentInParent<SpeedButton>() != null)
                continue;

            renderer.sortingOrder += sortingDelta;
            if (applyShadowMaterial)
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.sharedMaterial = material;
            }
            else
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = null;
            }
        }
    }

    private List<SpeedButton> CreateOrAttachSpeedButtons(
        Transform ownerRoot,
        float topLocalY,
        SpeedButton.ButtonType buttonType,
        int buttonCount,
        Vector2 baseOffset,
        Vector2 multiGroupOffset,
        float horizontalSpacing)
    {
        List<SpeedButton> buttons = new List<SpeedButton>();
        if (ownerRoot == null)
            return buttons;

        int desiredCount = Mathf.Max(1, buttonCount);
        List<SpeedButton> existingButtons = new List<SpeedButton>(ownerRoot.GetComponentsInChildren<SpeedButton>(true));

        if (battleManager != null && battleManager.SpeedButtonPrefab != null)
        {
            while (existingButtons.Count < desiredCount)
            {
                GameObject buttonObject = Instantiate(battleManager.SpeedButtonPrefab, ownerRoot);
                buttonObject.name = $"SpeedButton_{existingButtons.Count + 1}";
                buttonObject.transform.localRotation = Quaternion.identity;
                buttonObject.transform.localScale = Vector3.one;
                buttonObject.SetActive(true);
                SpeedButton created = buttonObject.GetComponent<SpeedButton>() ?? buttonObject.GetComponentInChildren<SpeedButton>(true);
                if (created != null)
                    existingButtons.Add(created);
            }
        }
        else if (existingButtons.Count == 0)
        {
            LineRenderer lineRenderer = ownerRoot.GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = ownerRoot.gameObject.AddComponent<LineRenderer>();
            SpeedButton created = ownerRoot.GetComponent<SpeedButton>() ?? ownerRoot.gameObject.AddComponent<SpeedButton>();
            if (created != null)
                existingButtons.Add(created);
        }

        if (existingButtons.Count == 0)
            return buttons;

        float effectiveSpacing = Mathf.Abs(horizontalSpacing);
        float xStart = -((desiredCount - 1) * effectiveSpacing) * 0.5f;
        for (int i = 0; i < existingButtons.Count; i++)
        {
            SpeedButton speedButton = existingButtons[i];
            if (speedButton == null)
                continue;

            speedButton.SetButtonType(buttonType);
            speedButton.IsInteractable = false;

            Transform speedTransform = speedButton.transform;
            if (speedTransform.parent != ownerRoot)
                speedTransform.SetParent(ownerRoot, true);

            if (i < desiredCount)
            {
                float x = baseOffset.x + (desiredCount > 1 ? multiGroupOffset.x + xStart + i * effectiveSpacing : 0f);
                float y = topLocalY + baseOffset.y + (desiredCount > 1 ? multiGroupOffset.y : 0f);
                speedTransform.localPosition = new Vector3(x, y, speedTransform.localPosition.z);
                speedTransform.localRotation = Quaternion.identity;
                speedTransform.localScale = Vector3.one;
                speedButton.gameObject.name = desiredCount > 1 ? $"SpeedButton_{i + 1}" : "SpeedButton";
                speedButton.gameObject.SetActive(true);
                buttons.Add(speedButton);
            }
            else
            {
                speedButton.gameObject.SetActive(false);
            }
        }

        return buttons;
    }

    private void ApplySpriteAnchorAlignment(Sprite sprite, Vector3 scale, Vector3 localOffset, Transform body, PolygonCollider2D collider2D)
    {
        if (body == null || collider2D == null)
            return;

        Sprite effectiveSprite = sprite != null ? sprite : runtimeWhiteSprite;
        if (effectiveSprite == null)
            return;

        Bounds bounds = effectiveSprite.bounds;
        Vector3 bottomCenterOffset = new Vector3(
            -bounds.center.x * scale.x,
            -bounds.min.y * scale.y,
            0f);

        body.localPosition = bottomCenterOffset + localOffset;
        body.localScale = scale;

        float width = Mathf.Max(0.8f, bounds.size.x * scale.x);
        float height = Mathf.Max(1f, bounds.size.y * scale.y);
        collider2D.points = new[]
        {
            new Vector2(-width * 0.42f, 0f),
            new Vector2(-width * 0.5f, height * 0.45f),
            new Vector2(-width * 0.24f, height),
            new Vector2(width * 0.24f, height),
            new Vector2(width * 0.5f, height * 0.45f),
            new Vector2(width * 0.42f, 0f)
        };
    }

    private List<Transform> CollectAnchorsByPrefix(string prefix)
    {
        List<Transform> anchors = new List<Transform>();
        if (anchorRoot == null)
            return anchors;

        for (int i = 0; i < anchorRoot.childCount; i++)
        {
            Transform child = anchorRoot.GetChild(i);
            if (child == null || child == spawnedAlliesRoot || child == spawnedEnemiesRoot)
                continue;

            if (!string.IsNullOrWhiteSpace(prefix) &&
                !child.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            anchors.Add(child);
        }

        anchors.Sort((a, b) => ExtractAnchorIndex(a.name).CompareTo(ExtractAnchorIndex(b.name)));
        return anchors;
    }

    private Vector3 CalculateFallbackSpawnPosition(Vector3 center, int index, int totalCount)
    {
        Vector3 start = center - formationSlotSpacing * ((totalCount - 1) * 0.5f);
        return start + formationSlotSpacing * index;
    }

    private Vector3 ResolveEnemyBodyScale(EnemyData_SO enemyData)
    {
        float multiplier = enemyData != null ? Mathf.Max(0.01f, enemyData.enemyVisualScale) : 1f;
        return new Vector3(enemyBodyScale.x * multiplier, enemyBodyScale.y * multiplier, enemyBodyScale.z);
    }

    private Vector3 ResolveEnemyVisualOffset(EnemyData_SO enemyData)
    {
        return enemyData != null ? enemyData.enemyVisualOffset : Vector3.zero;
    }

    private float CalculateSpriteTopLocalY(Sprite sprite, Vector3 scale, Vector3 localOffset)
    {
        Sprite effectiveSprite = sprite != null ? sprite : runtimeWhiteSprite;
        if (effectiveSprite == null)
            return 0.5f;

        Bounds bounds = effectiveSprite.bounds;
        float baseOffset = -bounds.min.y * scale.y;
        return localOffset.y + baseOffset + (bounds.max.y * scale.y);
    }

    private float CalculateActorTopLocalY(Transform root)
    {
        if (!TryGetCombinedRendererBounds(root, out Bounds combinedBounds))
            return 0.5f;

        return Mathf.Max(0.5f, combinedBounds.max.y);
    }

    private static SpriteRenderer FindPrimaryBodyRenderer(Transform root)
    {
        if (root == null)
            return null;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer best = null;
        int bestSorting = int.MinValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (renderer.GetComponentInParent<SpeedButton>() != null)
                continue;

            if (renderer.sortingOrder > bestSorting)
            {
                bestSorting = renderer.sortingOrder;
                best = renderer;
            }
        }

        return best;
    }

    private void AlignVisualRootToAnchor(Transform actorRoot, Transform visualRoot, PolygonCollider2D collider2D)
    {
        if (actorRoot == null || visualRoot == null || collider2D == null)
            return;

        if (!TryGetCombinedRendererBounds(actorRoot, out Bounds combinedBounds))
            return;

        Vector3 bottomCenterOffset = new Vector3(-combinedBounds.center.x, -combinedBounds.min.y, 0f);
        visualRoot.localPosition += bottomCenterOffset;

        if (!TryGetCombinedRendererBounds(actorRoot, out combinedBounds))
            return;

        float width = Mathf.Max(0.8f, combinedBounds.size.x);
        float height = Mathf.Max(1f, combinedBounds.size.y);
        collider2D.points = new[]
        {
            new Vector2(-width * 0.42f, 0f),
            new Vector2(-width * 0.5f, height * 0.45f),
            new Vector2(-width * 0.24f, height),
            new Vector2(width * 0.24f, height),
            new Vector2(width * 0.5f, height * 0.45f),
            new Vector2(width * 0.42f, 0f)
        };
    }

    private bool TryGetCombinedRendererBounds(Transform root, out Bounds combinedBounds)
    {
        combinedBounds = default;
        if (root == null)
            return false;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.GetComponentInParent<SpeedButton>() != null)
                continue;

            Bounds localBounds = TransformBoundsToLocalSpace(root, renderer.transform, renderer.localBounds);
            if (!hasBounds)
            {
                combinedBounds = localBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(localBounds.min);
                combinedBounds.Encapsulate(localBounds.max);
            }
        }

        return hasBounds;
    }

    private Bounds TransformBoundsToLocalSpace(Transform targetSpace, Transform sourceTransform, Bounds sourceBounds)
    {
        Vector3 min = sourceBounds.min;
        Vector3 max = sourceBounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        Bounds transformedBounds = new Bounds();
        bool initialized = false;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 worldCorner = sourceTransform.TransformPoint(corners[i]);
            Vector3 localCorner = targetSpace.InverseTransformPoint(worldCorner);
            if (!initialized)
            {
                transformedBounds = new Bounds(localCorner, Vector3.zero);
                initialized = true;
            }
            else
            {
                transformedBounds.Encapsulate(localCorner);
            }
        }

        return transformedBounds;
    }

    private static bool HasCharacterPrefab(RunPartyMemberState allyState)
    {
        return allyState != null &&
               allyState.characterData != null &&
               allyState.characterData.characterPrefab != null;
    }

    private Weapon CreateEquippedWeapon(
        Weapon_SO weaponData,
        Transform actorRoot,
        Transform characterHoldPoint,
        SpriteRenderer referenceRenderer,
        int targetSortingOrder)
    {
        if (weaponData == null || weaponData.weaponPrefab == null || actorRoot == null)
            return null;

        GameObject weaponObject = Instantiate(weaponData.weaponPrefab, actorRoot);
        weaponObject.name = weaponData.weaponPrefab.name;
        weaponObject.transform.localPosition = Vector3.zero;
        weaponObject.transform.localRotation = Quaternion.identity;

        Weapon weapon = weaponObject.GetComponent<Weapon>() ?? weaponObject.GetComponentInChildren<Weapon>(true);
        if (weapon == null)
            weapon = weaponObject.AddComponent<Weapon>();

        weapon.weaponData = weaponData;
        if (weapon.HoldPoint == null)
            weapon.HoldPoint = FindChildRecursive(weaponObject.transform, "HoldPoint");

        if (characterHoldPoint != null && weapon.HoldPoint != null)
        {
            Quaternion rotationOffset = characterHoldPoint.rotation * Quaternion.Inverse(weapon.HoldPoint.rotation);
            weaponObject.transform.rotation = rotationOffset * weaponObject.transform.rotation;

            Vector3 gripOffset = characterHoldPoint.position - weapon.HoldPoint.position;
            weaponObject.transform.position += gripOffset;
        }

        weaponObject.transform.position += Vector3.forward * Mathf.Abs(weaponDepthOffset);

        ApplyWeaponRendererSettings(weaponObject, referenceRenderer, targetSortingOrder);

        return weapon;
    }

    private void EnsureActorSortingGroup(GameObject target, int sortingOrder)
    {
        if (target == null)
            return;

        SortingGroup sortingGroup = target.GetComponent<SortingGroup>();
        if (sortingGroup == null)
            sortingGroup = target.AddComponent<SortingGroup>();

        SpriteRenderer referenceRenderer = FindPrimaryBodyRenderer(target.transform);
        if (referenceRenderer != null)
        {
            sortingGroup.sortingLayerID = referenceRenderer.sortingLayerID;
            sortingGroup.sortingLayerName = referenceRenderer.sortingLayerName;
        }

        sortingGroup.sortingOrder = sortingOrder;
    }

    private void ApplyWeaponRendererSettings(GameObject weaponObject, SpriteRenderer referenceRenderer, int targetSortingOrder)
    {
        if (weaponObject == null)
            return;

        SpriteRenderer[] renderers = weaponObject.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        int minimumSortingOrder = int.MaxValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            minimumSortingOrder = Mathf.Min(minimumSortingOrder, renderer.sortingOrder);
        }

        if (minimumSortingOrder == int.MaxValue)
            minimumSortingOrder = 0;

        int sortingDelta = targetSortingOrder - minimumSortingOrder;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (referenceRenderer != null)
            {
                renderer.sortingLayerID = referenceRenderer.sortingLayerID;
                renderer.sortingLayerName = referenceRenderer.sortingLayerName;
            }

            renderer.sortingOrder += sortingDelta;

            if (battleManager != null && battleManager.EnemyMaterial != null)
            {
                renderer.sharedMaterial = battleManager.EnemyMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private T GetOrCreateRootComponent<T>(GameObject rootObject) where T : Component
    {
        if (rootObject == null)
            return null;

        T primary = rootObject.GetComponent<T>();
        if (primary == null)
            primary = rootObject.AddComponent<T>();

        T[] descendants = rootObject.GetComponentsInChildren<T>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            T duplicate = descendants[i];
            if (duplicate == null || duplicate == primary)
                continue;

            if (duplicate is Behaviour duplicateBehaviour)
                duplicateBehaviour.enabled = false;
        }

        return primary;
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private void DisablePrebuiltCombatants<T>(Transform parentRoot, Transform runtimeRoot) where T : Component
    {
        if (parentRoot == null)
            return;

        T[] combatants = parentRoot.GetComponentsInChildren<T>(true);
        for (int i = 0; i < combatants.Length; i++)
        {
            T combatant = combatants[i];
            if (combatant == null)
                continue;

            Transform combatantTransform = combatant.transform;
            if (runtimeRoot != null &&
                (combatantTransform == runtimeRoot || combatantTransform.IsChildOf(runtimeRoot)))
                continue;

            combatant.gameObject.SetActive(false);
        }
    }

    private Transform CreateRoot(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private Transform CreateRoot(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private int ExtractAnchorIndex(string anchorName)
    {
        if (string.IsNullOrWhiteSpace(anchorName))
            return int.MaxValue;

        int value = 0;
        bool foundDigit = false;
        for (int i = 0; i < anchorName.Length; i++)
        {
            char c = anchorName[i];
            if (!char.IsDigit(c))
                continue;

            foundDigit = true;
            value = (value * 10) + (c - '0');
        }

        return foundDigit ? value : int.MaxValue;
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
}
