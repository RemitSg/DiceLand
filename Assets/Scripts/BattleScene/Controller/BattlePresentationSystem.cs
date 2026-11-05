using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class BattlePresentationSystem : MonoBehaviour
{
    [Serializable]
    private class BurstSettings
    {
        [Min(1)] public int burst = 24;
        [Min(0.01f)] public float speed = 2.4f;
        [Min(0.01f)] public float life = 0.28f;
        [Min(0.01f)] public float size = 0.14f;
        [Min(0.01f)] public float endSize = 0.48f;
        [Min(0f)] public float gravity = 1.2f;
        [Range(0f, 90f)] public float angle = 28f;
        [Min(0f)] public float radius = 0.05f;
        public int orderOffset = 1;
    }

    [Serializable]
    private class RingSettings
    {
        [Min(1)] public short burst = 14;
        [Min(0.01f)] public float speed = 1.6f;
        [Min(0.01f)] public float life = 0.2f;
        [Min(0.01f)] public float size = 0.12f;
        [Min(0.01f)] public float endSize = 0.34f;
        [Min(0f)] public float radius = 0.06f;
        public int orderOffset = 0;
    }

    private enum PlaneReference
    {
        Average,
        FirstActor,
        SecondActor
    }

    private struct PlaneData
    {
        public float z;
        public Vector3 firstStart;
        public Vector3 secondStart;
        public Vector3 firstProjected;
        public Vector3 secondProjected;
        public Vector3 direction;
        public Vector3 midpoint;
        public float distance;
    }

    private struct SpriteSwapState
    {
        public SpriteRenderer renderer;
        public Sprite sprite;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public int sortingOrder;
        public bool flipX;
    }

    private sealed class ActiveSpriteSwap
    {
        public Coroutine coroutine;
        public SpriteSwapState originalState;
    }

    [Header("Plane")]
    [SerializeField] private bool useFixedPlaneZ;
    [SerializeField] private float fixedPlaneZ;
    [SerializeField] private float planeZOffset;
    [SerializeField] private PlaneReference clashPlaneReference = PlaneReference.Average;
    [SerializeField] private PlaneReference singleAttackPlaneReference = PlaneReference.SecondActor;
    [SerializeField] private bool flattenCustomPrefabDepth = true;

    [Header("Move")]
    [SerializeField] private float advanceDuration = 0.22f;
    [SerializeField] private float returnDuration = 0.18f;
    [SerializeField] private float clashFaceGap = 0.95f;
    [SerializeField] private float clashMinimumGap = 0.4f;
    [SerializeField] private float singleAttackStopDistance = 1.05f;
    [SerializeField] private float impactLerp = 0.84f;
    [SerializeField] private float impactHeightOffset = 0.15f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Timing")]
    [SerializeField] private float delayAfterClash = 0.06f;
    [SerializeField] private float delayBeforeHitAfterClash = 0.1f;
    [FormerlySerializedAs("delayBeforeSingleAttackRangeDisplay")]
    [FormerlySerializedAs("delayBeforeHitOnSingleAttack")]
    [SerializeField] private float singleAttackRangeDisplayDuration = 0.08f;
    [SerializeField] private float delayBeforeClashDiceReveal = 0.14f;
    [SerializeField] private float clashEffectDuration = 0.26f;
    [SerializeField] private float hitEffectDuration = 0.5f;
    [SerializeField] private float builtInHitDamageDelay = 0.07f;

    [Header("Interaction")]
    [SerializeField] private bool waitForPlayerClickDuringDicePresentation = true;

    [Header("Audio")]
    [SerializeField] private BattleSfxPlayer battleSfxPlayer;

    [Header("Knockback")]
    [SerializeField] private float defenderKnockbackDistance = 0.32f;
    [SerializeField] private float defenderKnockbackOutDuration = 0.08f;
    [SerializeField] private float defenderKnockbackReturnDuration = 0.1f;

    [Header("Hit Reaction")]
    [SerializeField] private float defaultHitReactionDelay = 0f;
    [SerializeField] private float defenderFlashDuration = 0.14f;
    [SerializeField] private float defenderFlashStrength = 0.9f;
    [SerializeField] private float defenderReactionFxDuration = 0.26f;
    [SerializeField] private float defenderReactionScale = 0.68f;
    [SerializeField] private int defenderReactionSortingBoost = 40;
    [Header("Ally Hurt Prefab")]
    [SerializeField] private GameObject allyDefenderReactionPrefab;
    [SerializeField] private Vector3 allyDefenderReactionOffset = Vector3.zero;
    [SerializeField] private Vector3 allyDefenderReactionRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 allyDefenderReactionScale = Vector3.one;
    [SerializeField] private int allyDefenderReactionSortingOrderOffset = 45;
    [SerializeField] private bool overrideAllyDefenderReactionDuration;
    [SerializeField, Min(0.01f)] private float allyDefenderReactionDuration = 0.26f;

    [Header("Camera Impact")]
    [SerializeField] private Camera battleCamera;
    [SerializeField] private Camera closeupCamera;
    [SerializeField] private bool enableCameraImpact = true;
    [SerializeField] private bool enableCloseupCuts = true;
    [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private Vector3 closeupCameraOffset = new Vector3(0f, 0.9f, -5.5f);
    [SerializeField] private Vector3 closeupLookOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField, Min(0f)] private float clashCloseupLeadDuration = 0.04f;
    [SerializeField, Min(0.01f)] private float clashCloseupHoldDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float hitCloseupHoldDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float closeupFollowSharpness = 16f;
    [SerializeField, Min(0.1f)] private float closeupOrthographicSize = 2.5f;
    [SerializeField, Min(1f)] private float closeupFieldOfView = 18f;
    [SerializeField, Min(0f)] private float closeupBoundsPadding = 0.45f;
    [SerializeField, Min(0.1f)] private float closeupWidthToHeightRatio = 0.62f;
    [SerializeField, Min(0f)] private float closeupLargeTargetVerticalBias = 0.35f;
    [SerializeField, Min(0f)] private float closeupLargeTargetDepthOffset = 0.28f;
    [SerializeField, Min(1f)] private float closeupDamagePopupScale = 1.45f;
    [SerializeField, Min(0f)] private float closeupRangeLensPulseAmplitude = 0.18f;
    [SerializeField, Min(0f)] private float closeupRangePositionPulseAmplitude = 0.06f;
    [SerializeField, Min(0.01f)] private float closeupRangePulseFrequency = 2.1f;
    [SerializeField, Range(0f, 1f)] private float continuousRangePulseIntensity = 0.45f;
    [SerializeField, Min(0f)] private float clashShakeAmplitude = 0.28f;
    [SerializeField, Min(0f)] private float hitShakeAmplitude = 0.40f;
    [SerializeField, Min(0.01f)] private float clashCameraImpactDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float hitCameraImpactDuration = 0.30f;
    [SerializeField, Range(0f, 0.08f)] private float cameraImpactLensKick = 0.032f;
    [SerializeField, Range(0f, 2f)] private float cameraImpactRoll = 0.95f;
    [SerializeField, Min(0f)] private float cameraPowerNormalization = 9f;
    [SerializeField, Range(0f, 1f)] private float minimumCameraIntensity = 0.46f;
    [SerializeField, Min(0.01f)] private float cameraShakeFrequency = 18f;
    [SerializeField] private AnimationCurve cameraShakeDecayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Death Camera")]
    [SerializeField] private bool enableDeathCamera = true;
    [SerializeField, Range(0.25f, 1.5f)] private float deathCameraStrength = 1f;
    private BattleDeathCameraMotion deathCameraMotion;

    [Header("Colors")]
    [SerializeField] private int effectSortingOrder = 250;
    [SerializeField] private float effectDepthOffset = -0.35f;
    [SerializeField] private Color clashPrimaryColor = new Color(1f, 0.95f, 0.85f, 1f);
    [SerializeField] private Color clashSecondaryColor = new Color(1f, 0.48f, 0.2f, 1f);
    [SerializeField] private Color clashSparkColor = new Color(1f, 0.82f, 0.42f, 1f);
    [SerializeField] private Color allyHitColor = new Color(0.72f, 1f, 0.92f, 1f);
    [SerializeField] private Color enemyHitColor = new Color(1f, 0.3f, 0.25f, 1f);
    [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color defenderFlashColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private AnimationCurve effectScaleCurve = AnimationCurve.EaseInOut(0f, 0.65f, 1f, 1.25f);

    [Header("Particles")]
    [SerializeField] private float clashParticleFadeDuration = 0.18f;
    [SerializeField] private float hitParticleFadeDuration = 0.18f;
    [SerializeField] private bool enableParticleGroundCollision = true;
    [SerializeField] private LayerMask particleCollisionLayers = ~0;
    [SerializeField, Range(0f, 1f)] private float particleCollisionBounce = 0.45f;
    [SerializeField, Range(0f, 1f)] private float particleCollisionDampen = 0.35f;
    [SerializeField, Range(0f, 1f)] private float particleCollisionLifetimeLoss = 0.05f;
    [SerializeField, Range(0f, 1f)] private float particleCollisionRadiusScale = 0.9f;
    [SerializeField, Min(0f)] private float particleCollisionMinKillSpeed = 0.05f;
    [SerializeField, Min(1)] private int particleCollisionMaxShapes = 256;
    [SerializeField] private BurstSettings clashSparks = new BurstSettings { burst = 30, speed = 3.1f, life = 0.34f, size = 0.16f, endSize = 0.56f, gravity = 1.3f, angle = 34f, radius = 0.06f, orderOffset = 2 };
    [SerializeField] private RingSettings clashRing = new RingSettings { burst = 18, speed = 1.9f, life = 0.24f, size = 0.14f, endSize = 0.42f, radius = 0.08f, orderOffset = 1 };
    [SerializeField] private BurstSettings hitSparks = new BurstSettings { burst = 26, speed = 2.7f, life = 0.28f, size = 0.13f, endSize = 0.48f, gravity = 1.15f, angle = 26f, radius = 0.05f, orderOffset = 2 };
    [SerializeField] private BurstSettings hitDebris = new BurstSettings { burst = 18, speed = 1.9f, life = 0.32f, size = 0.11f, endSize = 0.32f, gravity = 0.95f, angle = 48f, radius = 0.04f, orderOffset = 1 };

    private Material spriteMat;
    private Material streakMat;
    private Material particleMat;
    private Sprite flashSprite;
    private Texture2D whiteTexture;
    private Texture2D softImpactTexture;
    private Light impactLight;
    private float impactLightDuration;
    private float impactLightRemaining;
    private int impactLightStartFrame;
    private Coroutine activeCameraBeatRoutine;
    private Camera cameraBeatCamera;
    private Vector3 cameraBeatBasePosition;
    private Quaternion cameraBeatBaseRotation;
    private float cameraBeatBaseLens;
    private Coroutine activeCloseupShotRoutine;
    private BattleFeedbackSystem battleFeedbackSystem;
    private int activeCloseupShotId;
    private readonly BattleCloseupVisibility closeupVisibility = new BattleCloseupVisibility();
    private readonly System.Collections.Generic.Dictionary<SpriteRenderer, ActiveSpriteSwap> activeSpriteSwaps = new System.Collections.Generic.Dictionary<SpriteRenderer, ActiveSpriteSwap>();
    private bool closeupDefaultsCached;
    private Vector3 closeupDefaultPosition;
    private Quaternion closeupDefaultRotation;
    private float closeupDefaultLensValue;
    private float closeupShakePulse;
    private float closeupShakeDuration;
    private float closeupShakeRemaining;
    private bool closeupImpactIsHit;
    private float closeupRangePulseDuration;
    private float closeupRangePulseRemaining;
    private float closeupRangePulseStartTime;
    private bool closeupRangePulseContinuous;

    private void Awake()
    {
        EnsureRuntimeResources();
        ResolveFeedbackSystem()?.SetPresentationCamera(ResolveBattleCamera(), 1f);
    }

    public bool IsCombatantHiddenByCloseup(Transform actor) => closeupVisibility.IsSpectator(actor);
    public bool IsPlayingDeathCamera => deathCameraMotion != null && deathCameraMotion.IsActive;

    public void PlayDeathCamera(BattleDeathVfx effect)
    {
        if (!enableCameraImpact || !enableDeathCamera || effect == null) return;
        Camera active = closeupCamera != null && closeupCamera.enabled ? closeupCamera : ResolveBattleCamera();
        if (active == null || !active.isActiveAndEnabled) return;
        StopCameraImpactImmediate(); closeupShakeRemaining = 0; StopCloseupRangePulse();
        if (deathCameraMotion == null) deathCameraMotion = gameObject.AddComponent<BattleDeathCameraMotion>();
        deathCameraMotion.Configure(() => closeupCamera != null && closeupCamera.enabled ? closeupCamera : ResolveBattleCamera(), deathCameraStrength);
        deathCameraMotion.Play(effect);
    }

    public IEnumerator PlayClashSequence(
        Transform ally,
        Transform enemy,
        bool hasWinner,
        bool allyWon,
        EnemySkill_SO.DicePowerRange allyBaseRange,
        EnemySkill_SO.DicePowerRange allyRange,
        EnemySkill_SO.DicePowerRange enemyBaseRange,
        EnemySkill_SO.DicePowerRange enemyRange,
        int allyPower,
        int enemyPower,
        int allyPowerModifier,
        int enemyPowerModifier,
        float clashCameraPower,
        float hitCameraPower,
        EnemySkill_SO.DicePresentationSettings allyClashFx,
        EnemySkill_SO.DicePresentationSettings enemyClashFx,
        EnemySkill_SO.DicePresentationSettings hitFx,
        Action onImpact,
        bool moveIntoPosition = true,
        bool returnToOrigin = true)
    {
        if (ally == null || enemy == null)
            yield break;

        using var presentationInteractionLock = BattlePresentationContinueInputService.BeginPresentationInteractionLock();

        EnsureRuntimeResources();
        BattleFeedbackSystem feedbackSystem = ResolveFeedbackSystem();

        PlaneData plane = BuildPlane(ally, enemy, clashPlaneReference);
        float gap = Mathf.Min(clashFaceGap, Mathf.Max(clashMinimumGap, plane.distance - 0.05f));
        gap = CalculateClashVisualSeparation(ally, enemy, plane.direction, gap, clashMinimumGap);
        Vector3 allyTarget = plane.midpoint - plane.direction * (gap * 0.5f);
        Vector3 enemyTarget = plane.midpoint + plane.direction * (gap * 0.5f);
        allyTarget.z = plane.z;
        enemyTarget.z = plane.z;

        BattleFeedbackSystem.ClashDiceDisplayHandle allyDiceDisplay = feedbackSystem?.ShowClashDiceRange(ally, false, allyRange, allyBaseRange);
        BattleFeedbackSystem.ClashDiceDisplayHandle enemyDiceDisplay = feedbackSystem?.ShowClashDiceRange(enemy, true, enemyRange, enemyBaseRange);

        if (moveIntoPosition)
            yield return MovePair(ally, plane.firstStart, allyTarget, enemy, plane.secondStart, enemyTarget, advanceDuration);
        int clashShotId = StartCloseupShot(ally, enemy, plane.midpoint + Vector3.up * impactHeightOffset, Mathf.Max(clashCameraPower, hitCameraPower));
        BeginCloseupRangePulse(clashCloseupLeadDuration + delayBeforeClashDiceReveal, waitForPlayerClickDuringDicePresentation);
        if (clashShotId != 0 && clashCloseupLeadDuration > 0f)
            yield return new WaitForSeconds(clashCloseupLeadDuration);
        if (waitForPlayerClickDuringDicePresentation)
        {
            yield return WaitForPresentationContinueClick();
            StopCloseupRangePulse();
        }
        else if (delayBeforeClashDiceReveal > 0f)
            yield return new WaitForSeconds(delayBeforeClashDiceReveal);
        feedbackSystem?.RevealClashDiceValue(allyDiceDisplay, allyPower, allyPowerModifier);
        feedbackSystem?.RevealClashDiceValue(enemyDiceDisplay, enemyPower, enemyPowerModifier);
        ResolveBattleSfxPlayer()?.PlayClashAnimation();
        yield return PlayClashExchangeFx(ally, enemy, plane.z, plane.midpoint + Vector3.up * impactHeightOffset, plane.direction, clashCameraPower, allyClashFx, enemyClashFx);
        if (waitForPlayerClickDuringDicePresentation && hasWinner)
            yield return WaitForPresentationContinueClick();

        if (delayAfterClash > 0f)
            yield return new WaitForSeconds(delayAfterClash);

        if (hasWinner)
        {
            if (delayBeforeHitAfterClash > 0f)
                yield return new WaitForSeconds(delayBeforeHitAfterClash);

            Transform attacker = allyWon ? ally : enemy;
            Transform defender = allyWon ? enemy : ally;
            EnemySkill_SO.DicePresentationSettings winnerClashFx = allyWon ? allyClashFx : enemyClashFx;
            Color hitColor = allyWon ? allyHitColor : enemyHitColor;
            bool useCustomHit = hitFx.hitEffectPrefab != null;
            Vector3 attackerCenter = BattleActorVisualUtility.GetActorCenterWorld(attacker);
            Vector3 defenderCenter = BattleActorVisualUtility.GetActorCenterWorld(defender);
            float winnerSkillSpriteDuration = winnerClashFx.overrideSkillSpriteDuration
                ? Mathf.Max(0.01f, winnerClashFx.skillSpriteDuration)
                : hitEffectDuration;
            TriggerSkillSpriteSwap(attacker, winnerClashFx, winnerSkillSpriteDuration);

            yield return PlayHitMoment(
                defender,
                ImpactPoint(attackerCenter, defenderCenter, plane.z),
                FlatDir(attackerCenter, defenderCenter, plane.z),
                hitColor,
                hitCameraPower,
                hitFx,
                useCustomHit,
                defender,
                attackerCenter,
                plane.z,
                onImpact);
        }
        else
        {
            onImpact?.Invoke();
        }

        // Hold the impact in close-up, then return through the already-framed overview.
        yield return EndCloseupShot(clashShotId, hasWinner ? hitCloseupHoldDuration : clashCloseupHoldDuration);
        if (returnToOrigin)
            yield return MovePair(ally, ally.position, plane.firstStart, enemy, enemy.position, plane.secondStart, returnDuration);
        feedbackSystem?.HideClashDiceDisplay(allyDiceDisplay);
        feedbackSystem?.HideClashDiceDisplay(enemyDiceDisplay);
    }

    public IEnumerator PlaySingleAttackSequence(
        Transform attacker,
        Transform defender,
        bool attackerIsAlly,
        EnemySkill_SO.DicePowerRange baseAttackRange,
        EnemySkill_SO.DicePowerRange attackRange,
        int attackPower,
        int attackPowerModifier,
        float hitCameraPower,
        EnemySkill_SO.DicePresentationSettings fx,
        Action onImpact,
        bool moveIntoPosition = true,
        bool returnToOrigin = true)
    {
        if (attacker == null || defender == null)
            yield break;

        using var presentationInteractionLock = BattlePresentationContinueInputService.BeginPresentationInteractionLock();

        EnsureRuntimeResources();
        BattleFeedbackSystem feedbackSystem = ResolveFeedbackSystem();
        BattleFeedbackSystem.ClashDiceDisplayHandle diceDisplay = null;

        PlaneData plane = BuildPlane(attacker, defender, singleAttackPlaneReference);
        Vector3 defenderTarget = plane.secondProjected;
        Vector3 attackerTarget = plane.secondProjected + FlatDir(plane.secondProjected, plane.firstProjected, plane.z) * singleAttackStopDistance;
        attackerTarget.z = plane.z;

        Color hitColor = attackerIsAlly ? allyHitColor : enemyHitColor;
        bool useCustomHit = fx.hitEffectPrefab != null;

        if (moveIntoPosition)
            yield return MovePair(attacker, plane.firstStart, attackerTarget, defender, plane.secondStart, defenderTarget, advanceDuration);

        Vector3 attackerCenter = BattleActorVisualUtility.GetActorCenterWorld(attacker);
        Vector3 defenderCenter = BattleActorVisualUtility.GetActorCenterWorld(defender);

        int hitShotId = StartCloseupShot(attacker, defender, Project(attackerCenter, plane.z) + Vector3.up * impactHeightOffset, hitCameraPower);
        diceDisplay = feedbackSystem?.ShowClashDiceRange(attacker, !attackerIsAlly, attackRange, baseAttackRange);
        BeginCloseupRangePulse(singleAttackRangeDisplayDuration, waitForPlayerClickDuringDicePresentation);

        if (waitForPlayerClickDuringDicePresentation)
        {
            yield return WaitForPresentationContinueClick();
            StopCloseupRangePulse();
        }
        else if (singleAttackRangeDisplayDuration > 0f)
            yield return new WaitForSeconds(singleAttackRangeDisplayDuration);

        feedbackSystem?.RevealClashDiceValue(diceDisplay, attackPower, attackPowerModifier);
        if (waitForPlayerClickDuringDicePresentation)
        {
            yield return WaitForDiceValueReveal(feedbackSystem);
            yield return WaitForPresentationContinueClick();
        }
        float skillSpriteDuration = fx.overrideSkillSpriteDuration
            ? Mathf.Max(0.01f, fx.skillSpriteDuration)
            : hitEffectDuration;
        TriggerSkillSpriteSwap(attacker, fx, skillSpriteDuration);

        if (onImpact != null)
        {
            Vector3 hitPosition = ImpactPoint(attackerCenter, defenderCenter, plane.z);
            Vector3 hitDirection = FlatDir(attackerCenter, defenderCenter, plane.z);

            yield return PlayHitMoment(
                defender,
                hitPosition,
                hitDirection,
                hitColor,
                hitCameraPower,
                fx,
                useCustomHit,
                defender,
                attackerCenter,
                plane.z,
                onImpact);
        }
        else
        {
            yield return new WaitForSeconds(Mathf.Max(0.08f, hitEffectDuration * 0.35f));
        }
        yield return EndCloseupShot(hitShotId, hitCloseupHoldDuration);
        if (returnToOrigin)
            yield return MovePair(attacker, attacker.position, plane.firstStart, defender, defender.position, plane.secondStart, returnDuration);
        feedbackSystem?.HideClashDiceDisplay(diceDisplay);
    }

    public IEnumerator ReturnActorsToPositions(
        Transform first,
        Vector3 firstTarget,
        Transform second,
        Vector3 secondTarget)
    {
        if (first == null || second == null)
            yield break;

        Vector3 currentFirst = first.position;
        Vector3 currentSecond = second.position;
        if ((currentFirst - firstTarget).sqrMagnitude <= 0.0001f && (currentSecond - secondTarget).sqrMagnitude <= 0.0001f)
            yield break;

        yield return MovePair(first, currentFirst, firstTarget, second, currentSecond, secondTarget, returnDuration);
    }

    private IEnumerator WaitForPresentationContinueClick()
    {
        BattlePresentationContinueInputState inputState = BattlePresentationContinueInputService.CreateState(Input.GetMouseButton(0));
        while (true)
        {
            bool mousePressedThisFrame = Input.GetMouseButtonDown(0);
            bool blockedGameplayClick = mousePressedThisFrame && IsPresentationContinueClickBlocked();
            if (BattlePresentationContinueInputService.ShouldContinue(
                    ref inputState,
                    Input.GetMouseButton(0),
                    mousePressedThisFrame,
                    blockedGameplayClick))
                yield break;

            yield return null;
        }
    }

    private static IEnumerator WaitForDiceValueReveal(BattleFeedbackSystem feedbackSystem)
    {
        float revealDuration = feedbackSystem != null ? feedbackSystem.GetClashDiceValueRevealDuration() : 0.01f;
        if (revealDuration > 0f)
            yield return new WaitForSeconds(revealDuration);
    }

    public void PlayStatusTickEffect(Transform target, BattleStatus_SO statusData)
    {
        if (target == null || statusData == null || statusData.TickEffectPrefab == null)
            return;

        Vector3 center = BattleActorVisualUtility.GetActorCenterWorld(target);
        float fallbackDuration = statusData.OverrideTickEffectDuration
            ? statusData.TickEffectDuration
            : Mathf.Max(0.05f, hitEffectDuration);

        SpawnCustomEffect(
            statusData.TickEffectPrefab,
            center,
            Vector3.right,
            statusData.TickEffectPositionOffset,
            statusData.TickEffectRotationOffset,
            EnemySkill_SO.TransformOffsetSpace.Local,
            statusData.TickEffectScale,
            fallbackDuration,
            statusData.TickEffectSortingOrderOffset,
            statusData.OverrideTickEffectDuration);
    }

    private IEnumerator MovePair(Transform first, Vector3 firstFrom, Vector3 firstTo, Transform second, Vector3 secondFrom, Vector3 secondTo, float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        for (float time = 0f; time < safeDuration; time += Time.deltaTime)
        {
            float eased = moveCurve.Evaluate(Mathf.Clamp01(time / safeDuration));
            if (first != null)
                first.position = Vector3.LerpUnclamped(firstFrom, firstTo, eased);
            if (second != null)
                second.position = Vector3.LerpUnclamped(secondFrom, secondTo, eased);
            yield return null;
        }

        if (first != null)
            first.position = firstTo;
        if (second != null)
            second.position = secondTo;
    }

    private IEnumerator MoveOne(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        for (float time = 0f; time < safeDuration; time += Time.deltaTime)
        {
            float eased = moveCurve.Evaluate(Mathf.Clamp01(time / safeDuration));
            target.position = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        target.position = to;
    }

    private IEnumerator Knockback(Transform defender, Vector3 attackerPos, float planeZ)
    {
        if (defender == null || defenderKnockbackDistance <= 0f)
            yield break;

        Vector3 start = Project(defender.position, planeZ);
        Vector3 end = start + GroundKnockbackDir(attackerPos, defender.position, planeZ) * defenderKnockbackDistance;
        end.z = planeZ;

        yield return MoveOne(defender, defender.position, end, defenderKnockbackOutDuration);
        yield return MoveOne(defender, defender.position, start, defenderKnockbackReturnDuration);
    }

    private IEnumerator PlayClashExchangeFx(
        Transform ally,
        Transform enemy,
        float planeZ,
        Vector3 impactPosition,
        Vector3 clashDirection,
        float clashCameraPower,
        EnemySkill_SO.DicePresentationSettings allyFx,
        EnemySkill_SO.DicePresentationSettings enemyFx)
    {
        float waitDuration = Mathf.Max(0f, clashEffectDuration);
        Vector3 contactPosition = ResolveClashContactPoint(ally, enemy, impactPosition, clashDirection, impactHeightOffset);
        GameHaptics.Play(GameHapticCue.Clash);
        TriggerCameraImpact(impactPosition, clashCameraPower, false, contactPosition);

        PlaySideClashFx(ally, planeZ, allyFx, clashDirection, allyHitColor);
        PlaySideClashFx(enemy, planeZ, enemyFx, -clashDirection, enemyHitColor);

        StartCoroutine(PlayBuiltInClashImpactFx(contactPosition, clashDirection));
        if (waitDuration > 0f)
            yield return new WaitForSeconds(waitDuration);
    }

    private void PlaySideClashFx(
        Transform source,
        float planeZ,
        EnemySkill_SO.DicePresentationSettings fx,
        Vector3 direction,
        Color fallbackColor)
    {
        if (source == null)
            return;

        Vector3 sourcePosition = Project(BattleActorVisualUtility.GetActorCenterWorld(source), planeZ) + Vector3.up * impactHeightOffset;
        float spriteSwapDuration = fx.overrideSkillSpriteDuration
            ? Mathf.Max(0.01f, fx.skillSpriteDuration)
            : clashEffectDuration;
        TriggerSkillSpriteSwap(source, fx, spriteSwapDuration);

        if (fx.clashEffectPrefab != null)
        {
            float fallbackDuration = fx.overrideClashDuration ? Mathf.Max(0.01f, fx.clashDuration) : clashEffectDuration;
            SpawnCustomEffect(
                fx.clashEffectPrefab,
                sourcePosition,
                direction,
                fx.clashPositionOffset,
                fx.clashRotationOffset,
                fx.clashOffsetSpace,
                fx.SafeClashScale,
                fallbackDuration,
                fx.clashSortingOrderOffset,
                true);
            return;
        }

        StartCoroutine(PlayBuiltInHitFx(sourcePosition, direction, fallbackColor));
    }

    private IEnumerator PlayBuiltInClashImpactFx(Vector3 position, Vector3 direction)
    {
        PulseImpactLight(position, clashSparkColor, 0.12f);
        GameObject root = CreateEffectRoot("ClashEffect", position, direction);
        ParticleSystem sparks = CreateBurst(root.transform, clashSparkColor, clashSparks, clashParticleFadeDuration);
        ParticleSystem ring = CreateRing(root.transform, clashPrimaryColor, clashRing, clashParticleFadeDuration);
        SpriteRenderer flash = CreateSprite(root.transform, "ClashFlash", flashColor, 0.20f);
        LineRenderer primary = CreateLine(root.transform, "ClashStreak", clashPrimaryColor, 0.13f,
            new Vector3(-0.9f, -0.4f, 0f), new Vector3(0.9f, 0.4f, 0f));
        LineRenderer secondary = CreateLine(root.transform, "ClashCrossStreak", clashSecondaryColor, 0.09f,
            new Vector3(-0.6f, 0.65f, 0f), new Vector3(0.6f, -0.65f, 0f));
        yield return AnimateSlash(root, flash, primary, secondary, clashPrimaryColor, clashSecondaryColor,
            Mathf.Max(0.05f, clashEffectDuration), 0.16f, 0.55f, 0.16f, 0.55f);

        StopParticles(sparks);
        StopParticles(ring);
        if (root != null)
            Destroy(root, GetCleanupDelay(sparks, ring));
    }

    private IEnumerator PlayHitMoment(
        Transform defender,
        Vector3 position,
        Vector3 direction,
        Color hitColor,
        float cameraImpactPower,
        EnemySkill_SO.DicePresentationSettings fx,
        bool useCustom,
        Transform customAnchor,
        Vector3 attackerPosition,
        float planeZ,
        Action onImpact)
    {
        float damageDelay;
        if (useCustom)
        {
            Vector3 anchorPosition = customAnchor != null ? customAnchor.position : position;
            anchorPosition.y = position.y;
            anchorPosition.z = position.z;

            float life = SpawnCustomEffect(
                fx.hitEffectPrefab,
                anchorPosition,
                direction,
                fx.hitPositionOffset,
                fx.hitRotationOffset,
                fx.hitOffsetSpace,
                fx.SafeHitScale,
                hitEffectDuration,
                fx.hitSortingOrderOffset,
                true);

            damageDelay = fx.overrideHitDamageDelay
                ? fx.hitDamageDelay
                : Mathf.Clamp(life * 0.35f, 0.02f, Mathf.Max(0.02f, life));
        }
        else
        {
            StartCoroutine(PlayBuiltInHitFx(position, direction, hitColor));
            damageDelay = builtInHitDamageDelay;
        }

        float reactionDelay = fx.overrideHitReactionDelay ? fx.hitReactionDelay : defaultHitReactionDelay;
        float impactDelay = defender != null ? reactionDelay : damageDelay;
        bool damageResolved = false;
        bool reactionResolved = defender == null;

        StartCoroutine(ResolveImpactAfterDelay(impactDelay, () =>
        {
            // Land the camera beat with damage and the defender's reaction, rather
            // than spending it during a custom effect's anticipation frames.
            if (!useCustom)
                PulseImpactLight(position, hitColor, 0.16f);
            TriggerCameraImpact(position, cameraImpactPower, true);
            // A lethal callback may hide the defender before the reaction coroutine runs.
            CacheLastHitDirection(defender, GroundKnockbackDir(attackerPosition, defender != null ? defender.position : position, planeZ));
            onImpact?.Invoke();
            damageResolved = true;
        }));

        if (defender != null)
        {
            StartCoroutine(PlayDefenderReaction(defender, attackerPosition, planeZ, hitColor, reactionDelay, () =>
            {
                reactionResolved = true;
            }));
        }

        while (!damageResolved || !reactionResolved)
            yield return null;
    }

    private IEnumerator ResolveImpactAfterDelay(float delay, Action onResolved)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        onResolved?.Invoke();
    }

    private IEnumerator PlayDefenderReaction(Transform defender, Vector3 attackerPosition, float planeZ, Color hitColor, float reactionDelay, Action onResolved)
    {
        if (defender == null)
        {
            onResolved?.Invoke();
            yield break;
        }

        if (reactionDelay > 0f)
            yield return new WaitForSeconds(reactionDelay);

        Vector3 defenderCenter = BattleActorVisualUtility.GetActorCenterWorld(defender);
        Vector3 knockbackDirection = GroundKnockbackDir(attackerPosition, defender.position, planeZ);
        CacheLastHitDirection(defender, knockbackDirection);
        Vector3 reactionPosition = Project(defenderCenter, planeZ) + Vector3.up * impactHeightOffset;
        StartCoroutine(FlashDefenderSprites(defender));
        PlayDefenderReactionFx(defender, reactionPosition, knockbackDirection, hitColor);
        yield return Knockback(defender, attackerPosition, planeZ);
        onResolved?.Invoke();
    }

    private void CacheLastHitDirection(Transform defender, Vector3 direction)
    {
        if (defender == null || direction.sqrMagnitude <= 0.0001f)
            return;

        BattleAllyController ally = defender.GetComponent<BattleAllyController>();
        if (ally == null)
            ally = defender.GetComponentInParent<BattleAllyController>();
        if (ally != null)
        {
            ally.RegisterLastHitDirection(direction);
            return;
        }

        BattleEnemyController enemy = defender.GetComponent<BattleEnemyController>();
        if (enemy == null)
            enemy = defender.GetComponentInParent<BattleEnemyController>();
        if (enemy != null)
            enemy.RegisterLastHitDirection(direction);
    }

    private IEnumerator FlashDefenderSprites(Transform defender)
    {
        if (defender == null)
            yield break;

        SpriteRenderer[] renderers = defender.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
            yield break;

        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        float duration = Mathf.Max(0.01f, defenderFlashDuration);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float normalized = Mathf.Clamp01(time / duration);
            float blend = 1f - Mathf.Abs(normalized * 2f - 1f);
            blend *= defenderFlashStrength;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.color = Color.Lerp(originalColors[i], defenderFlashColor, blend);
            }

            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = originalColors[i];
        }
    }

    private void PlayDefenderReactionFx(Transform defender, Vector3 position, Vector3 direction, Color hitColor)
    {
        if (ShouldUseCustomAllyDefenderReaction(defender))
        {
            float fallbackDuration = overrideAllyDefenderReactionDuration
                ? Mathf.Max(0.01f, allyDefenderReactionDuration)
                : Mathf.Max(0.05f, defenderReactionFxDuration);

            SpawnCustomEffect(
                allyDefenderReactionPrefab,
                position,
                direction,
                allyDefenderReactionOffset,
                allyDefenderReactionRotationOffset,
                EnemySkill_SO.TransformOffsetSpace.Local,
                SafeScale(allyDefenderReactionScale),
                fallbackDuration,
                allyDefenderReactionSortingOrderOffset);
            return;
        }

        StartCoroutine(PlayBuiltInDefenderReactionFx(position, hitColor));
    }

    private IEnumerator PlayBuiltInDefenderReactionFx(Vector3 position, Color hitColor)
    {
        GameObject root = CreateEffectRoot("DefenderReactionFx", position, Vector3.right);
        SpriteRenderer flash = CreateSprite(root.transform, "DefenderFlash", WithAlpha(defenderFlashColor, 0.92f), defenderReactionScale);
        ParticleSystem sparks = CreateBurst(root.transform, hitColor, hitSparks, hitParticleFadeDuration);
        ParticleSystem debris = CreateBurst(root.transform, WithAlpha(hitColor, 0.95f), hitDebris, hitParticleFadeDuration);
        ParticleSystem ring = CreateRing(root.transform, WithAlpha(Color.white, 0.95f), clashRing, hitParticleFadeDuration);

        ApplySortingBoost(root, defenderReactionSortingBoost);

        float duration = Mathf.Max(0.05f, defenderReactionFxDuration);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float normalized = Mathf.Clamp01(time / duration);
            if (flash != null)
            {
                flash.color = WithAlpha(defenderFlashColor, 0.95f * (1f - normalized) * (1f - normalized));
                flash.transform.localScale = Vector3.one * Mathf.Lerp(defenderReactionScale, defenderReactionScale * 1.8f, normalized);
            }

            yield return null;
        }

        if (flash != null)
            flash.color = WithAlpha(flash.color, 0f);
        StopParticles(sparks);
        StopParticles(debris);
        StopParticles(ring);
        if (root != null)
            Destroy(root, GetCleanupDelay(sparks, debris, ring));
    }

    private bool ShouldUseCustomAllyDefenderReaction(Transform defender)
    {
        if (allyDefenderReactionPrefab == null || defender == null)
            return false;

        BattleAllyController ally = defender.GetComponent<BattleAllyController>();
        if (ally == null)
            ally = defender.GetComponentInParent<BattleAllyController>();
        return ally != null;
    }

    private IEnumerator PlayBuiltInHitFx(Vector3 position, Vector3 direction, Color hitColor)
    {
        GameObject root = CreateEffectRoot("HitEffect", position, direction);
        SpriteRenderer flash = CreateSprite(root.transform, "HitFlash", flashColor, 0.34f);
        LineRenderer mainSlash = CreateLine(root.transform, "SlashMain", hitColor, 0.26f, new Vector3(-0.82f, -0.12f, 0f), new Vector3(0.82f, 0.12f, 0f));
        ParticleSystem sparks = CreateBurst(root.transform, hitColor, hitSparks, hitParticleFadeDuration);
        ParticleSystem debris = CreateBurst(root.transform, WithAlpha(hitColor, 0.86f), hitDebris, hitParticleFadeDuration);

        yield return AnimateSlash(root, flash, mainSlash, null, hitColor, WithAlpha(hitColor, 0.8f), hitEffectDuration, 0.28f, 1.02f, 0.28f, 1.02f);

        StopParticles(sparks);
        StopParticles(debris);
        if (root != null)
            Destroy(root, GetCleanupDelay(sparks, debris));
    }

    private IEnumerator AnimateSlash(GameObject root, SpriteRenderer flash, LineRenderer primary, LineRenderer secondary, Color primaryColor, Color secondaryColor, float duration, float baseX, float growX, float baseY, float growY)
    {
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            float normalized = Mathf.Clamp01(time / duration);
            float alpha = 1f - normalized;

            if (root != null)
                root.transform.localScale = Vector3.one * effectScaleCurve.Evaluate(normalized);

            if (flash != null)
            {
                flash.color = WithAlpha(flashColor, 0.95f * alpha * alpha);
                flash.transform.localScale = new Vector3(baseX + normalized * growX, baseY + normalized * growY, 1f);
            }

            ApplyLineColor(primary, WithAlpha(primaryColor, alpha));
            ApplyLineColor(secondary, WithAlpha(secondaryColor, alpha * 0.9f));
            yield return null;
        }

        if (flash != null)
            flash.color = WithAlpha(flash.color, 0f);
        if (primary != null)
            ApplyLineColor(primary, WithAlpha(primaryColor, 0f));
        if (secondary != null)
            ApplyLineColor(secondary, WithAlpha(secondaryColor, 0f));
    }

    private void TriggerSkillSpriteSwap(Transform source, EnemySkill_SO.DicePresentationSettings fx, float fallbackDuration)
    {
        if (source == null || fx.skillSpriteOverride == null)
            return;

        SpriteRenderer primaryRenderer = FindPrimaryCombatSpriteRenderer(source);
        if (primaryRenderer == null)
            return;

        float duration = fx.overrideSkillSpriteDuration
            ? Mathf.Max(0.01f, fx.skillSpriteDuration)
            : Mathf.Max(0.01f, fallbackDuration);

        if (activeSpriteSwaps.TryGetValue(primaryRenderer, out ActiveSpriteSwap existingSwap))
        {
            if (existingSwap.coroutine != null)
                StopCoroutine(existingSwap.coroutine);

            RestoreSpriteSwapState(existingSwap.originalState);
            activeSpriteSwaps.Remove(primaryRenderer);
        }

        Transform spriteTransform = primaryRenderer.transform;
        SpriteSwapState state = new SpriteSwapState
        {
            renderer = primaryRenderer,
            sprite = primaryRenderer.sprite,
            localPosition = spriteTransform.localPosition,
            localRotation = spriteTransform.localRotation,
            localScale = spriteTransform.localScale,
            sortingOrder = primaryRenderer.sortingOrder,
            flipX = primaryRenderer.flipX
        };

        ActiveSpriteSwap swap = new ActiveSpriteSwap
        {
            originalState = state
        };
        swap.coroutine = StartCoroutine(PlaySkillSpriteSwap(primaryRenderer, fx, duration, swap));
        activeSpriteSwaps[primaryRenderer] = swap;
    }

    private IEnumerator PlaySkillSpriteSwap(SpriteRenderer primaryRenderer, EnemySkill_SO.DicePresentationSettings fx, float duration, ActiveSpriteSwap swap)
    {
        if (primaryRenderer == null)
            yield break;

        Transform spriteTransform = primaryRenderer.transform;
        SpriteSwapState state = swap != null ? swap.originalState : default;

        primaryRenderer.sprite = fx.skillSpriteOverride;
        primaryRenderer.flipX = state.flipX;
        spriteTransform.localPosition = state.localPosition + fx.skillSpritePositionOffset;
        spriteTransform.localRotation = state.localRotation * Quaternion.Euler(fx.skillSpriteRotationOffset);
        spriteTransform.localScale = Vector3.Scale(state.localScale, fx.SafeSkillSpriteScale);
        primaryRenderer.sortingOrder = state.sortingOrder + fx.skillSpriteSortingOrderOffset;

        yield return new WaitForSeconds(Mathf.Max(0.01f, duration));

        if (state.renderer == null)
            yield break;

        RestoreSpriteSwapState(state);

        if (swap != null && activeSpriteSwaps.TryGetValue(primaryRenderer, out ActiveSpriteSwap currentSwap) && ReferenceEquals(currentSwap, swap))
            activeSpriteSwaps.Remove(primaryRenderer);
    }

    private static void RestoreSpriteSwapState(SpriteSwapState state)
    {
        if (state.renderer == null)
            return;

        Transform restoreTransform = state.renderer.transform;
        state.renderer.sprite = state.sprite;
        state.renderer.flipX = state.flipX;
        restoreTransform.localPosition = state.localPosition;
        restoreTransform.localRotation = state.localRotation;
        restoreTransform.localScale = state.localScale;
        state.renderer.sortingOrder = state.sortingOrder;
    }

    private static SpriteRenderer FindPrimaryCombatSpriteRenderer(Transform root)
    {
        if (root == null)
            return null;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer best = null;
        int bestSortingOrder = int.MinValue;
        int bestDepth = int.MaxValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (renderer.GetComponentInParent<SpeedButton>() != null)
                continue;

            int depth = GetTransformDepth(root, renderer.transform);
            if (renderer.sortingOrder > bestSortingOrder || (renderer.sortingOrder == bestSortingOrder && depth < bestDepth))
            {
                best = renderer;
                bestSortingOrder = renderer.sortingOrder;
                bestDepth = depth;
            }
        }

        return best;
    }

    private static int GetTransformDepth(Transform root, Transform target)
    {
        if (root == null || target == null)
            return int.MaxValue;

        int depth = 0;
        Transform current = target;
        while (current != null && current != root)
        {
            depth++;
            current = current.parent;
        }

        return current == root ? depth : int.MaxValue;
    }

    public static float CalculateClashVisualSeparation(Transform first, Transform second, Vector3 direction,
        float rootGap, float bodyClearance)
    {
        if (!BattleActorVisualUtility.TryGetActorSpriteBounds(first, out Bounds firstBounds)
            || !BattleActorVisualUtility.TryGetActorSpriteBounds(second, out Bounds secondBounds))
            return rootGap;

        direction.z = 0f;
        direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.right;
        Vector3 axis = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), 0f);
        float firstReach = Vector3.Dot(firstBounds.center - first.position, direction)
            + Vector3.Dot(firstBounds.extents, axis);
        float secondReach = Vector3.Dot(second.position - secondBounds.center, direction)
            + Vector3.Dot(secondBounds.extents, axis);
        // Roots can be close while a large illustrated body already covers its opponent's weapon.
        // Preserve the configured approach distance when it already leaves enough visual room.
        return Mathf.Max(rootGap, Mathf.Max(0f, firstReach) + Mathf.Max(0f, secondReach) + Mathf.Max(0f, bodyClearance));
    }

    public static Vector3 ResolveClashContactPoint(Transform first, Transform second, Vector3 fallback,
        Vector3 direction, float heightOffset)
    {
        if (!BattleActorVisualUtility.TryGetActorSpriteBounds(first, out Bounds firstBody)
            || !BattleActorVisualUtility.TryGetActorSpriteBounds(second, out Bounds secondBody)
            || firstBody.size.y <= 0.0001f || secondBody.size.y <= 0.0001f)
            return fallback;

        direction.z = 0f;
        direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.right;
        float low = Mathf.Max(firstBody.min.y, secondBody.min.y);
        float high = Mathf.Min(firstBody.max.y, secondBody.max.y);
        float height = low <= high ? Mathf.Lerp(low, high, 0.6f)
            : (firstBody.ClosestPoint(secondBody.center).y + secondBody.ClosestPoint(firstBody.center).y) * 0.5f;
        bool firstHasWeapon = TryGetClashWeaponPoint(first, firstBody, direction, height, out Vector3 firstWeapon);
        bool secondHasWeapon = TryGetClashWeaponPoint(second, secondBody, -direction, height, out Vector3 secondWeapon);
        if (low <= high)
        {
            if (firstHasWeapon && secondHasWeapon) height = (firstWeapon.y + secondWeapon.y) * 0.5f;
            else if (firstHasWeapon) height = firstWeapon.y;
            else if (secondHasWeapon) height = secondWeapon.y;
            height = Mathf.Clamp(height + heightOffset, low, high);
        }
        else height += heightOffset;

        Vector3 firstFace = firstBody.ClosestPoint(new Vector3(secondBody.center.x, height, fallback.z));
        Vector3 secondFace = secondBody.ClosestPoint(new Vector3(firstBody.center.x, height, fallback.z));
        float left = Mathf.Min(firstFace.x, secondFace.x), right = Mathf.Max(firstFace.x, secondFace.x);
        if (firstHasWeapon) firstFace.x = Mathf.Clamp(firstWeapon.x, left, right);
        if (secondHasWeapon) secondFace.x = Mathf.Clamp(secondWeapon.x, left, right);
        return new Vector3((firstFace.x + secondFace.x) * 0.5f, height, fallback.z);
    }

    private static bool TryGetClashWeaponPoint(Transform actor, Bounds body, Vector3 direction,
        float preferredHeight, out Vector3 point)
    {
        point = Vector3.zero;
        float best = Vector3.Dot(body.extents, new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), 0f));
        bool found = false;
        foreach (Weapon weapon in actor.GetComponentsInChildren<Weapon>(true))
        {
            // A root Weapon can hold equipment data without a separate weapon illustration.
            if (weapon.transform == actor) continue;
            foreach (SpriteRenderer renderer in weapon.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null || !renderer.enabled || renderer.forceRenderingOff
                    || !renderer.gameObject.activeInHierarchy || renderer.color.a <= 0.001f
                    || renderer.GetComponentInParent<SpeedButton>(true) != null) continue;
                Vector2[] vertices = renderer.sprite.vertices;
                if (renderer.drawMode != SpriteDrawMode.Simple || vertices.Length <= 4)
                {
                    Vector3 candidate = renderer.bounds.ClosestPoint(body.center + direction * 1000f
                        + Vector3.up * (preferredHeight - body.center.y));
                    float reach = Vector3.Dot(candidate - body.center, direction);
                    if (reach > best + 0.0001f) { best = reach; point = candidate; found = true; }
                    continue;
                }
                foreach (Vector2 vertex in vertices)
                {
                    Vector3 candidate = renderer.transform.TransformPoint(new Vector3(
                        renderer.flipX ? -vertex.x : vertex.x, renderer.flipY ? -vertex.y : vertex.y, 0f));
                    float reach = Vector3.Dot(candidate - body.center, direction);
                    if (reach > best + 0.0001f || (found && Mathf.Abs(reach - best) <= 0.0001f
                        && Mathf.Abs(candidate.y - preferredHeight) < Mathf.Abs(point.y - preferredHeight)))
                    {
                        best = reach; point = candidate; found = true;
                    }
                }
            }
        }
        return found;
    }

    private PlaneData BuildPlane(Transform first, Transform second, PlaneReference planeReference)
    {
        float planeZ = ResolvePlaneZ(first.position.z, second.position.z, planeReference);
        Vector3 firstProjected = Project(first.position, planeZ);
        Vector3 secondProjected = Project(second.position, planeZ);
        Vector3 direction = FlatDir(firstProjected, secondProjected, planeZ);
        Vector3 midpoint = Vector3.Lerp(firstProjected, secondProjected, 0.5f);
        float distance = Vector2.Distance(firstProjected, secondProjected);

        return new PlaneData
        {
            z = planeZ,
            firstStart = first.position,
            secondStart = second.position,
            firstProjected = firstProjected,
            secondProjected = secondProjected,
            direction = direction,
            midpoint = midpoint,
            distance = distance
        };
    }

    private float ResolvePlaneZ(float firstZ, float secondZ, PlaneReference planeReference)
    {
        if (useFixedPlaneZ)
            return fixedPlaneZ + planeZOffset;

        switch (planeReference)
        {
            case PlaneReference.FirstActor:
                return firstZ + planeZOffset;
            case PlaneReference.SecondActor:
                return secondZ + planeZOffset;
            default:
                return ((firstZ + secondZ) * 0.5f) + planeZOffset;
        }
    }

    private Vector3 ImpactPoint(Vector3 attacker, Vector3 defender, float planeZ)
    {
        Vector3 attackerOnPlane = Project(attacker, planeZ);
        Vector3 defenderOnPlane = Project(defender, planeZ);
        Vector3 point = Vector3.Lerp(attackerOnPlane, defenderOnPlane, impactLerp);
        point.y += impactHeightOffset;
        point.z = planeZ;
        return point;
    }

    private static Vector3 Project(Vector3 value, float planeZ)
    {
        value.z = planeZ;
        return value;
    }

    private static Vector3 FlatDir(Vector3 from, Vector3 to, float planeZ)
    {
        Vector3 start = Project(from, planeZ);
        Vector3 end = Project(to, planeZ);
        Vector3 direction = end - start;
        direction.z = 0f;
        return direction.sqrMagnitude <= 0.0001f ? Vector3.right : direction.normalized;
    }

    private static Vector3 GroundKnockbackDir(Vector3 attacker, Vector3 defender, float planeZ)
    {
        Vector3 start = Project(attacker, planeZ);
        Vector3 end = Project(defender, planeZ);
        Vector3 direction = end - start;
        direction.y = 0f;
        direction.z = 0f;
        return direction.sqrMagnitude <= 0.0001f ? Vector3.right : direction.normalized;
    }

    private GameObject CreateEffectRoot(string name, Vector3 position, Vector3 direction)
    {
        GameObject root = new GameObject(name);
        position.z += effectDepthOffset;
        root.transform.position = position;
        root.transform.rotation = BuildFacingRotation(direction);
        return root;
    }

    private float SpawnCustomEffect(
        GameObject prefab,
        Vector3 basePosition,
        Vector3 direction,
        Vector3 positionOffset,
        Vector3 rotationOffset,
        EnemySkill_SO.TransformOffsetSpace offsetSpace,
        Vector3 scale,
        float fallbackDuration,
        int sortingOrderOffset,
        bool forceDuration = false)
    {
        if (prefab == null)
            return fallbackDuration;

        Quaternion facingRotation = BuildFacingRotation(direction);
        Quaternion eulerRotationOffset = Quaternion.Euler(rotationOffset);
        bool useGlobalOffset = offsetSpace == EnemySkill_SO.TransformOffsetSpace.Global;
        Quaternion rotation = useGlobalOffset
            ? eulerRotationOffset * facingRotation
            : facingRotation * eulerRotationOffset;
        Vector3 spawnPosition = basePosition + (useGlobalOffset ? positionOffset : rotation * positionOffset);
        spawnPosition.z += effectDepthOffset;
        GameObject instance = Instantiate(prefab, spawnPosition, rotation);
        instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale);

        if (flattenCustomPrefabDepth)
            FlattenHierarchyDepth(instance.transform);

        ApplySortingOrder(instance, sortingOrderOffset);

        float life = forceDuration ? Mathf.Max(0.05f, fallbackDuration) : CalcLife(instance, fallbackDuration);
        ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        if (particleSystems != null && particleSystems.Length > 0)
        {
            StartCoroutine(StopAndCleanupCustomEffect(instance, particleSystems, life));
        }
        else
        {
            Destroy(instance, life + 0.15f);
        }

        return life;
    }

    private IEnumerator StopAndCleanupCustomEffect(GameObject instance, ParticleSystem[] particleSystems, float activeDuration)
    {
        if (instance == null)
            yield break;

        if (activeDuration > 0f)
            yield return new WaitForSeconds(activeDuration);

        if (particleSystems == null || particleSystems.Length == 0)
        {
            Destroy(instance);
            yield break;
        }

        foreach (ParticleSystem particleSystem in particleSystems)
            StopParticles(particleSystem);

        float cleanupDelay = GetCleanupDelay(particleSystems);
        float elapsed = 0f;
        while (elapsed < cleanupDelay)
        {
            bool anyAlive = false;
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem != null && particleSystem.IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (instance != null)
            Destroy(instance);
    }

    private static Quaternion BuildFacingRotation(Vector3 direction)
    {
        float horizontal = Mathf.Abs(direction.x) <= 0.0001f ? 1f : Mathf.Sign(direction.x);
        return horizontal < 0f ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
    }

    private static Vector3 SafeScale(Vector3 scale)
    {
        return Mathf.Approximately(scale.x, 0f)
            && Mathf.Approximately(scale.y, 0f)
            && Mathf.Approximately(scale.z, 0f)
            ? Vector3.one
            : scale;
    }

    private void FlattenHierarchyDepth(Transform root)
    {
        foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
        {
            if (node == root)
                continue;

            Vector3 localPosition = node.localPosition;
            localPosition.z = 0f;
            node.localPosition = localPosition;
        }
    }

    private void ApplySortingOrder(GameObject instance, int sortingOrderOffset)
    {
        int sortingOrder = effectSortingOrder + sortingOrderOffset;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.sortingOrder = sortingOrder;
    }

    private static void ApplySortingBoost(GameObject instance, int sortingBoost)
    {
        if (instance == null || sortingBoost == 0)
            return;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.sortingOrder += sortingBoost;
    }

    private float CalcLife(GameObject instance, float fallbackDuration)
    {
        float maxDuration = Mathf.Max(0.05f, fallbackDuration);

        foreach (ParticleSystem particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particleSystem.main;
            float life = main.duration;
            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                life += Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);
            else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                life += main.startLifetime.constant;

            maxDuration = Mathf.Max(maxDuration, life);
        }

        foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
        {
            if (animator.runtimeAnimatorController == null)
                continue;

            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                maxDuration = Mathf.Max(maxDuration, clip.length);
        }

        return maxDuration;
    }

    private SpriteRenderer CreateSprite(Transform parent, string name, Color color, float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = flashSprite;
        renderer.color = color;
        renderer.material = spriteMat;
        renderer.sortingOrder = effectSortingOrder;
        return renderer;
    }

    private LineRenderer CreateLine(Transform parent, string name, Color color, float width, Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        LineRenderer renderer = go.AddComponent<LineRenderer>();
        renderer.useWorldSpace = false;
        renderer.positionCount = 2;
        renderer.SetPosition(0, start);
        renderer.SetPosition(1, end);
        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.numCapVertices = 0;
        renderer.numCornerVertices = 0;
        renderer.sharedMaterial = streakMat;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.sortingOrder = effectSortingOrder;
        renderer.alignment = LineAlignment.TransformZ;
        ApplyLineColor(renderer, color);
        return renderer;
    }

    private ParticleSystem CreateBurst(Transform parent, Color color, BurstSettings settings, float fadeDuration)
    {
        GameObject go = new GameObject("Burst");
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.up);

        ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = effectSortingOrder + settings.orderOffset;

        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = settings.life;
        main.startSpeed = settings.speed;
        main.startSize = settings.size;
        main.startColor = color;
        main.gravityModifier = settings.gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(24, settings.burst * 2);

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, settings.burst) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = settings.angle;
        shape.radius = settings.radius;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            BuildParticleGradient(color, Color.Lerp(color, Color.white, 0.2f), color, 1f, fadeDuration, settings.life));

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, settings.endSize / Mathf.Max(0.01f, settings.size))));

        ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity = particleSystem.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.limit = settings.speed * 0.7f;
        limitVelocity.dampen = 0.4f;

        ConfigureParticleCollision(particleSystem);

        particleSystem.Play();
        return particleSystem;
    }

    private ParticleSystem CreateRing(Transform parent, Color color, RingSettings settings, float fadeDuration)
    {
        GameObject go = new GameObject("Ring");
        go.transform.SetParent(parent, false);

        ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = effectSortingOrder + settings.orderOffset;

        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = settings.life;
        main.startSpeed = settings.speed;
        main.startSize = settings.size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(18, settings.burst * 2);

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, settings.burst) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = settings.radius;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, settings.endSize / Mathf.Max(0.01f, settings.size))));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            BuildParticleGradient(color, Color.white, color, 0.95f, fadeDuration, settings.life));

        ConfigureParticleCollision(particleSystem);

        particleSystem.Play();
        return particleSystem;
    }

    private static void StopParticles(ParticleSystem particleSystem)
    {
        if (particleSystem != null)
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void ConfigureParticleCollision(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
            return;

        ParticleSystem.CollisionModule collision = particleSystem.collision;
        collision.enabled = enableParticleGroundCollision;
        if (!enableParticleGroundCollision)
            return;

        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.collidesWith = particleCollisionLayers;
        collision.enableDynamicColliders = true;
        collision.sendCollisionMessages = false;
        collision.bounce = particleCollisionBounce;
        collision.dampen = particleCollisionDampen;
        collision.lifetimeLoss = particleCollisionLifetimeLoss;
        collision.radiusScale = particleCollisionRadiusScale;
        collision.minKillSpeed = particleCollisionMinKillSpeed;
        collision.maxKillSpeed = float.MaxValue;
        collision.maxCollisionShapes = particleCollisionMaxShapes;
        collision.quality = ParticleSystemCollisionQuality.High;
    }

    private float GetCleanupDelay(params ParticleSystem[] particleSystems)
    {
        float maxLifetime = 0.15f;
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem == null)
                continue;

            maxLifetime = Mathf.Max(maxLifetime, GetMaxLifetime(particleSystem));
        }

        return maxLifetime + 0.08f;
    }

    private static float GetMaxLifetime(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
            return 0f;

        ParticleSystem.MainModule main = particleSystem.main;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            return main.startLifetime.constantMax;
        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            return main.startLifetime.constant;

        return 0.35f;
    }

    private static Gradient BuildParticleGradient(Color startColor, Color midColor, Color endColor, float maxAlpha, float fadeDuration, float lifetime)
    {
        Gradient gradient = new Gradient();
        float fadeStart = ResolveFadeStart(fadeDuration, lifetime);
        float midTime = Mathf.Clamp01(Mathf.Min(fadeStart, 0.28f));

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(midColor, midTime),
                new GradientColorKey(endColor, 1f)
            },
            BuildFadeAlphaKeys(maxAlpha, fadeStart));

        return gradient;
    }

    private static GradientAlphaKey[] BuildFadeAlphaKeys(float maxAlpha, float fadeStart)
    {
        if (fadeStart <= 0.001f)
        {
            return new[]
            {
                new GradientAlphaKey(maxAlpha, 0f),
                new GradientAlphaKey(0f, 1f)
            };
        }

        return new[]
        {
            new GradientAlphaKey(maxAlpha, 0f),
            new GradientAlphaKey(maxAlpha, fadeStart),
            new GradientAlphaKey(0f, 1f)
        };
    }

    private static float ResolveFadeStart(float fadeDuration, float lifetime)
    {
        if (lifetime <= 0.0001f)
            return 0f;

        return Mathf.Clamp01(1f - Mathf.Clamp(fadeDuration, 0f, lifetime) / lifetime);
    }

    private static void ApplyLineColor(LineRenderer renderer, Color color)
    {
        if (renderer == null)
            return;

        renderer.startColor = color;
        renderer.endColor = color;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void TriggerCameraImpact(Vector3 focusPosition, float power, bool isHit, Vector3? pulsePosition = null)
    {
        if (!enableCameraImpact)
            return;

        ResolveFeedbackSystem()?.PlayImpactPulse(pulsePosition ?? focusPosition, isHit ? flashColor : clashSparkColor,
            Mathf.Lerp(0.45f, 1f, NormalizeCameraIntensity(power)));
        if (IsPlayingDeathCamera) return;

        if (UseCloseupCutCamera())
        {
            float intensity = NormalizeCameraIntensity(power);
            float currentDecay = closeupShakeDuration > 0.0001f
                ? cameraShakeDecayCurve.Evaluate(1f - Mathf.Clamp01(closeupShakeRemaining / closeupShakeDuration)) : 0f;
            float currentAmplitude = (closeupImpactIsHit ? hitShakeAmplitude : clashShakeAmplitude)
                * closeupShakePulse * currentDecay;
            float incomingAmplitude = (isHit ? hitShakeAmplitude : clashShakeAmplitude) * intensity;
            if (closeupShakeRemaining > 0f && incomingAmplitude < currentAmplitude)
                return;
            closeupShakePulse = intensity;
            closeupImpactIsHit = isHit;
            closeupShakeDuration = isHit ? hitCameraImpactDuration : clashCameraImpactDuration;
            closeupShakeRemaining = closeupShakeDuration;
            return;
        }

        Camera cameraToUse = ResolveBattleCamera();
        if (cameraToUse == null)
            return;

        StopCameraImpactImmediate();

        activeCameraBeatRoutine = StartCoroutine(PlayCameraImpactBeat(cameraToUse, focusPosition, power, isHit));
    }

    private IEnumerator PlayCameraImpactBeat(Camera targetCamera, Vector3 focusPosition, float power, bool isHit)
    {
        if (targetCamera == null)
            yield break;

        cameraBeatCamera = targetCamera;
        cameraBeatBasePosition = targetCamera.transform.position;
        cameraBeatBaseRotation = targetCamera.transform.rotation;
        cameraBeatBaseLens = targetCamera.orthographic ? targetCamera.orthographicSize : targetCamera.fieldOfView;
        Vector3 startPosition = cameraBeatBasePosition;
        float startLensValue = targetCamera.orthographic ? targetCamera.orthographicSize : targetCamera.fieldOfView;
        float duration = Mathf.Max(0.01f, isHit ? hitCameraImpactDuration : clashCameraImpactDuration);
        float intensity = NormalizeCameraIntensity(power);
        float amplitude = ResolveImpactWorldAmplitude(targetCamera, focusPosition, isHit) * intensity;

        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            if (targetCamera == null)
                yield break;

            float normalized = Mathf.Clamp01(time / duration);
            float envelope = Mathf.Pow(1f - normalized, 2f);
            float decay = cameraShakeDecayCurve.Evaluate(normalized);
            float angle = time * cameraShakeFrequency * Mathf.PI * 2f;
            Vector2 shake = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle * 0.83f)) * (amplitude * decay);

            targetCamera.transform.position = startPosition + targetCamera.transform.right * shake.x + targetCamera.transform.up * shake.y;
            targetCamera.transform.rotation = cameraBeatBaseRotation * Quaternion.Euler(0f, 0f,
                Mathf.Sin(angle * 0.7f) * cameraImpactRoll * intensity * decay);

            float lensTarget = targetCamera.orthographic
                ? Mathf.Max(0.2f, startLensValue * (1f - intensity * cameraImpactLensKick))
                : Mathf.Max(1f, startLensValue * (1f - intensity * cameraImpactLensKick));
            if (targetCamera.orthographic)
                targetCamera.orthographicSize = Mathf.Lerp(startLensValue, lensTarget, envelope);
            else
                targetCamera.fieldOfView = Mathf.Lerp(startLensValue, lensTarget, envelope);

            yield return null;
        }

        if (targetCamera != null)
        {
            targetCamera.transform.position = startPosition;
            targetCamera.transform.rotation = cameraBeatBaseRotation;
            if (targetCamera.orthographic)
                targetCamera.orthographicSize = startLensValue;
            else
                targetCamera.fieldOfView = startLensValue;
        }

        activeCameraBeatRoutine = null;
        cameraBeatCamera = null;
    }

    private float ResolveImpactWorldAmplitude(Camera camera, Vector3 focus, bool isHit)
    {
        float depth = Mathf.Max(0.1f, Vector3.Dot(focus - camera.transform.position, camera.transform.forward));
        float height = camera.orthographic ? 2f * camera.orthographicSize
            : 2f * depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        return height * (isHit ? hitShakeAmplitude : clashShakeAmplitude) * 0.1f;
    }

    private void StopCameraImpactImmediate()
    {
        if (activeCameraBeatRoutine != null)
            StopCoroutine(activeCameraBeatRoutine);
        activeCameraBeatRoutine = null;
        if (cameraBeatCamera == null)
            return;
        cameraBeatCamera.transform.SetPositionAndRotation(cameraBeatBasePosition, cameraBeatBaseRotation);
        if (cameraBeatCamera.orthographic)
            cameraBeatCamera.orthographicSize = cameraBeatBaseLens;
        else
            cameraBeatCamera.fieldOfView = cameraBeatBaseLens;
        cameraBeatCamera = null;
    }

    private Camera ResolveBattleCamera()
    {
        if (battleCamera == null)
            battleCamera = Camera.main;

        return battleCamera;
    }

    private BattleFeedbackSystem ResolveFeedbackSystem()
    {
        if (battleFeedbackSystem == null)
            battleFeedbackSystem = FindObjectOfType<BattleFeedbackSystem>();

        return battleFeedbackSystem;
    }

    private BattleSfxPlayer ResolveBattleSfxPlayer()
    {
        if (battleSfxPlayer == null)
            battleSfxPlayer = GetComponent<BattleSfxPlayer>();
        if (battleSfxPlayer == null)
            battleSfxPlayer = FindObjectOfType<BattleSfxPlayer>();

        return battleSfxPlayer;
    }

    private bool UseCloseupCutCamera()
    {
        return enableCameraImpact && enableCloseupCuts && closeupCamera != null && ResolveBattleCamera() != closeupCamera;
    }

    private int StartCloseupShot(Transform first, Transform second, Vector3 fallbackFocus, float power)
    {
        if (!UseCloseupCutCamera())
            return 0;

        if (deathCameraMotion != null) deathCameraMotion.Cancel();
        StopActiveCloseupShotImmediate();
        CacheCloseupDefaults();
        activeCloseupShotId++;
        int shotId = activeCloseupShotId;
        closeupShakePulse = 0f;
        closeupShakeDuration = 0f;
        closeupShakeRemaining = 0f;
        closeupRangePulseDuration = 0f;
        closeupRangePulseRemaining = 0f;
        closeupRangePulseStartTime = 0f;
        closeupRangePulseContinuous = false;
        activeCloseupShotRoutine = StartCoroutine(RunCloseupShot(shotId, first, second, fallbackFocus, power));
        return shotId;
    }

    private IEnumerator EndCloseupShot(int shotId, float holdDuration)
    {
        if (shotId == 0 || shotId != activeCloseupShotId)
            yield break;

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        if (shotId != activeCloseupShotId)
            yield break;

        while (IsPlayingDeathCamera && shotId == activeCloseupShotId) yield return null;
        if (shotId != activeCloseupShotId) yield break;
        StopActiveCloseupShotImmediate();
    }

    private IEnumerator RunCloseupShot(int shotId, Transform first, Transform second, Vector3 fallbackFocus, float power)
    {
        Camera mainCamera = ResolveBattleCamera();
        Camera shotCamera = closeupCamera;
        if (shotCamera == null)
            yield break;
        BattleFeedbackSystem feedbackSystem = ResolveFeedbackSystem();

        if (mainCamera != null)
            mainCamera.enabled = false;
        shotCamera.enabled = true;
        feedbackSystem?.SetPresentationCamera(shotCamera, closeupDamagePopupScale);

        float initialFrameSize = ResolveCloseupFrameSize(first, second);
        Vector3 initialFramingOffset = ResolveCloseupFramingOffset(initialFrameSize);
        Vector3 initialFocus = ResolveShotFocus(first, second, fallbackFocus) + cameraFocusOffset + new Vector3(0f, initialFramingOffset.y, 0f);
        Vector3 initialPosition = initialFocus + closeupCameraOffset + new Vector3(0f, 0f, initialFramingOffset.z);
        shotCamera.transform.position = initialPosition;
        if (shotCamera.orthographic)
        {
            shotCamera.orthographicSize = initialFrameSize;
            shotCamera.transform.rotation = closeupDefaultRotation;
        }
        else
        {
            shotCamera.fieldOfView = ResolveCloseupFieldOfView(initialFrameSize);
            Vector3 initialLookTarget = initialFocus + closeupLookOffset;
            shotCamera.transform.rotation = Quaternion.LookRotation((initialLookTarget - initialPosition).normalized, Vector3.up);
        }

        // Follow an unshaken pose. Feeding last frame's displaced pose back into
        // the follow filter accumulates drift and makes the shake frame-rate dependent.
        Vector3 followedPosition = initialPosition;
        Quaternion followedRotation = shotCamera.transform.rotation;
        float followedLens = shotCamera.orthographic ? shotCamera.orthographicSize : shotCamera.fieldOfView;
        while (shotId == activeCloseupShotId)
        {
            closeupVisibility.Update(gameObject.scene, first, second);
            float desiredFrameSize = ResolveCloseupFrameSize(first, second);
            Vector3 framingOffset = ResolveCloseupFramingOffset(desiredFrameSize);
            Vector3 focus = ResolveShotFocus(first, second, fallbackFocus) + cameraFocusOffset + new Vector3(0f, framingOffset.y, 0f);
            Vector3 desiredPosition = focus + closeupCameraOffset + new Vector3(0f, 0f, framingOffset.z);
            float followT = 1f - Mathf.Exp(-Mathf.Max(0.01f, closeupFollowSharpness) * Time.deltaTime);

            if (closeupShakeRemaining > 0f)
                closeupShakeRemaining = Mathf.Max(0f, closeupShakeRemaining - Time.unscaledDeltaTime);
            else
                closeupShakePulse = 0f;

            if (closeupRangePulseRemaining > 0f)
                closeupRangePulseRemaining = Mathf.Max(0f, closeupRangePulseRemaining - Time.deltaTime);

            float shakeNormalized = closeupShakeDuration > 0.0001f
                ? Mathf.Clamp01(closeupShakeRemaining / closeupShakeDuration)
                : 0f;
            float shakeDecay = cameraShakeDecayCurve.Evaluate(1f - shakeNormalized);
            float shakeStrength = closeupShakePulse * shakeDecay;
            float impactTime = closeupShakeDuration - closeupShakeRemaining;
            float angle = impactTime * cameraShakeFrequency * Mathf.PI * 2f;
            float shakeAmplitude = ResolveImpactWorldAmplitude(shotCamera, focus, closeupImpactIsHit);
            Vector2 shake = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle * 0.83f)) * (shakeAmplitude * shakeStrength);

            float rangePulse = EvaluateCloseupRangePulse();
            if (closeupRangePulseContinuous)
                rangePulse *= continuousRangePulseIntensity;
            Vector3 pulsePositionOffset = new Vector3(
                Mathf.Sin(Time.time * closeupRangePulseFrequency * Mathf.PI * 1.2f),
                Mathf.Cos(Time.time * closeupRangePulseFrequency * Mathf.PI * 0.9f),
                0f) * (closeupRangePositionPulseAmplitude * rangePulse);

            followedPosition = Vector3.Lerp(followedPosition, desiredPosition + pulsePositionOffset, followT);
            shotCamera.transform.position = followedPosition;

            if (shotCamera.orthographic)
            {
                float targetSize = Mathf.Max(0.2f, desiredFrameSize - closeupRangeLensPulseAmplitude * rangePulse);
                followedLens = Mathf.Lerp(followedLens, targetSize, followT);
                followedRotation = closeupDefaultRotation;
            }
            else
            {
                float targetFov = Mathf.Max(1f, ResolveCloseupFieldOfView(desiredFrameSize) - closeupRangeLensPulseAmplitude * rangePulse * 2.2f);
                followedLens = Mathf.Lerp(followedLens, targetFov, followT);
                Vector3 lookTarget = focus + closeupLookOffset;
                followedRotation = Quaternion.Slerp(
                    followedRotation,
                    Quaternion.LookRotation((lookTarget - followedPosition).normalized, Vector3.up),
                    followT);
            }

            // Apply displacement after aiming, otherwise LookRotation cancels the
            // translation at the focal point. A small lens kick settles with the impact.
            shotCamera.transform.rotation = followedRotation;
            shotCamera.transform.position += shotCamera.transform.right * shake.x + shotCamera.transform.up * shake.y;
            shotCamera.transform.rotation = followedRotation * Quaternion.Euler(0f, 0f,
                Mathf.Sin(angle * 0.7f) * cameraImpactRoll * shakeStrength);
            float impactLens = followedLens * (1f - cameraImpactLensKick * shakeStrength);
            if (shotCamera.orthographic)
                shotCamera.orthographicSize = Mathf.Max(0.2f, impactLens);
            else
                shotCamera.fieldOfView = Mathf.Max(1f, impactLens);

            yield return null;
        }

        closeupVisibility.Restore();
        if (shotCamera != null)
        {
            RestoreCloseupDefaults();
            shotCamera.enabled = false;
        }

        if (mainCamera != null)
            mainCamera.enabled = true;
        feedbackSystem?.SetPresentationCamera(mainCamera, 1f);

        activeCloseupShotRoutine = null;
    }

    private void BeginCloseupRangePulse(float duration, bool continuous = false)
    {
        if (duration <= 0f)
            return;

        closeupRangePulseDuration = Mathf.Max(closeupRangePulseDuration, duration);
        closeupRangePulseRemaining = Mathf.Max(closeupRangePulseRemaining, duration);
        closeupRangePulseStartTime = Time.time;
        closeupRangePulseContinuous = continuous;
    }

    private void StopCloseupRangePulse()
    {
        closeupRangePulseRemaining = 0f;
        closeupRangePulseContinuous = false;
    }

    private bool IsPresentationContinueClickBlocked()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private float EvaluateCloseupRangePulse()
    {
        if (!closeupRangePulseContinuous && closeupRangePulseRemaining <= 0f)
            return 0f;

        return BattlePresentationRangePulseService.Evaluate(
            Time.time - closeupRangePulseStartTime,
            closeupRangePulseDuration,
            closeupRangePulseFrequency,
            closeupRangePulseContinuous);
    }

    private void StopActiveCloseupShotImmediate()
    {
        if (deathCameraMotion != null) deathCameraMotion.RestoreBasePose();
        ResolveFeedbackSystem()?.StopImpactPulse();
        if (activeCloseupShotRoutine != null)
            StopCoroutine(activeCloseupShotRoutine);

        closeupVisibility.Restore();
        activeCloseupShotRoutine = null;
        activeCloseupShotId++;
        closeupShakePulse = 0f;
        closeupShakeDuration = 0f;
        closeupShakeRemaining = 0f;
        closeupRangePulseDuration = 0f;
        closeupRangePulseRemaining = 0f;
        closeupRangePulseStartTime = 0f;
        closeupRangePulseContinuous = false;

        Camera mainCamera = ResolveBattleCamera();
        if (closeupCamera != null)
        {
            RestoreCloseupDefaults();
            closeupCamera.enabled = false;
        }
        if (mainCamera != null)
            mainCamera.enabled = true;
        ResolveFeedbackSystem()?.SetPresentationCamera(mainCamera, 1f);
    }

    private static Vector3 ResolveShotFocus(Transform first, Transform second, Vector3 fallbackFocus)
    {
        if (TryGetShotBounds(first, second, out Bounds bounds))
            return bounds.center;
        if (first != null && second != null)
            return (BattleActorVisualUtility.GetActorCenterWorld(first) + BattleActorVisualUtility.GetActorCenterWorld(second)) * 0.5f;
        if (first != null)
            return BattleActorVisualUtility.GetActorCenterWorld(first);
        if (second != null)
            return BattleActorVisualUtility.GetActorCenterWorld(second);
        return fallbackFocus;
    }

    private float ResolveCloseupFrameSize(Transform first, Transform second)
    {
        if (!TryGetShotBounds(first, second, out Bounds combinedBounds))
            return closeupOrthographicSize;

        Vector3 forward = (closeupLookOffset - closeupCameraOffset).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right).normalized;
        Vector3 extents = combinedBounds.extents;
        float projectedHalfHeight = Mathf.Abs(up.x) * extents.x + Mathf.Abs(up.y) * extents.y + Mathf.Abs(up.z) * extents.z;
        float projectedHalfWidth = Mathf.Abs(right.x) * extents.x + Mathf.Abs(right.y) * extents.y + Mathf.Abs(right.z) * extents.z;
        float widthToHeightRatio = closeupCamera != null
            ? 1f / Mathf.Max(0.1f, closeupCamera.aspect)
            : Mathf.Max(0.1f, closeupWidthToHeightRatio);
        // Reserve head/foot room for the HUD and camera pulses, using the same union center as the focus.
        float paddedHalfHeight = (projectedHalfHeight + closeupBoundsPadding) / 0.78f;
        float widthDrivenHalfHeight = (projectedHalfWidth * widthToHeightRatio + closeupBoundsPadding) / 0.88f;
        return Mathf.Max(closeupOrthographicSize, paddedHalfHeight, widthDrivenHalfHeight);
    }

    private static bool TryGetShotBounds(Transform first, Transform second, out Bounds bounds)
    {
        bool hasBounds = BattleActorVisualUtility.TryGetActorSpriteBounds(first, out bounds);
        if (BattleActorVisualUtility.TryGetActorSpriteBounds(second, out Bounds secondBounds))
        {
            if (hasBounds)
                bounds.Encapsulate(secondBounds);
            else
                bounds = secondBounds;
            hasBounds = true;
        }
        return hasBounds;
    }

    private float ResolveCloseupFieldOfView(float desiredFrameSize)
    {
        float verticalDistance = Mathf.Max(0.1f, Mathf.Abs(closeupCameraOffset.z));
        float requiredFov = Mathf.Rad2Deg * 2f * Mathf.Atan(Mathf.Max(0.1f, desiredFrameSize) / verticalDistance);
        return Mathf.Max(closeupFieldOfView, requiredFov);
    }

    private Vector3 ResolveCloseupFramingOffset(float desiredFrameSize)
    {
        float extraFrameSize = Mathf.Max(0f, desiredFrameSize - closeupOrthographicSize);
        return new Vector3(
            0f,
            extraFrameSize * closeupLargeTargetVerticalBias,
            -extraFrameSize * closeupLargeTargetDepthOffset);
    }

    private void CacheCloseupDefaults()
    {
        if (closeupDefaultsCached || closeupCamera == null)
            return;

        closeupDefaultPosition = closeupCamera.transform.position;
        closeupDefaultRotation = closeupCamera.transform.rotation;
        closeupDefaultLensValue = closeupCamera.orthographic ? closeupCamera.orthographicSize : closeupCamera.fieldOfView;
        closeupDefaultsCached = true;
    }

    private void RestoreCloseupDefaults()
    {
        if (!closeupDefaultsCached || closeupCamera == null)
            return;

        closeupCamera.transform.position = closeupDefaultPosition;
        closeupCamera.transform.rotation = closeupDefaultRotation;
        if (closeupCamera.orthographic)
            closeupCamera.orthographicSize = closeupDefaultLensValue;
        else
            closeupCamera.fieldOfView = closeupDefaultLensValue;
    }

    private float NormalizeCameraIntensity(float power)
    {
        float normalized = power <= 0f ? 0f : power / Mathf.Max(0.01f, cameraPowerNormalization);
        return Mathf.Clamp01(Mathf.Max(minimumCameraIntensity, normalized));
    }

    private void PulseImpactLight(Vector3 position, Color tint, float duration)
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
            return;
        if (impactLight == null)
        {
            var item = new GameObject("Battle impact light");
            item.hideFlags = HideFlags.HideAndDontSave;
            item.transform.SetParent(transform, false);
            impactLight = item.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.shadows = LightShadows.None;
            impactLight.renderMode = LightRenderMode.ForcePixel;
            impactLight.range = 3.2f;
        }
        // One owned light also bounds simultaneous side flashes to a single lighting pulse.
        impactLight.transform.position = position + Vector3.forward * (effectDepthOffset - 0.3f);
        impactLight.color = Color.Lerp(WithAlpha(tint, 1f), Color.white, 0.35f);
        impactLight.intensity = 1.4f;
        impactLightDuration = impactLightRemaining = Mathf.Max(0.01f, duration);
        impactLightStartFrame = Time.frameCount;
        impactLight.enabled = true;
    }

    private void LateUpdate()
    {
        if (impactLight == null || impactLightRemaining <= 0f)
            return;
        // A long impact frame must still render the pulse before its decay starts.
        if (Time.frameCount == impactLightStartFrame)
            return;
        impactLightRemaining = Mathf.Max(0f, impactLightRemaining - Time.deltaTime);
        float weight = impactLightRemaining / impactLightDuration;
        impactLight.intensity = 1.4f * weight * weight;
        impactLight.enabled = impactLightRemaining > 0f;
    }

    private void EnsureRuntimeResources()
    {
        if (softImpactTexture == null)
            softImpactTexture = BattleImpactLightTexture.Create("Battle presentation soft impact");

        if (whiteTexture == null)
        {
            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply(false, false);
        }

        if (spriteMat == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                spriteMat = new Material(shader);
                ApplyTexture(spriteMat, whiteTexture);
            }
        }

        if (streakMat == null && spriteMat != null)
        {
            streakMat = new Material(spriteMat) { name = "Battle soft impact streak" };
            ApplyTexture(streakMat, softImpactTexture);
            Color lightCore = new Color(1.8f, 1.8f, 1.8f, 1f);
            if (streakMat.HasProperty("_Color")) streakMat.SetColor("_Color", lightCore);
            if (streakMat.HasProperty("_BaseColor")) streakMat.SetColor("_BaseColor", lightCore);
        }

        if (particleMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                particleMat = new Material(shader);
                ApplyTexture(particleMat, softImpactTexture);
                ConfigureTransparentParticleMaterial(particleMat);
            }
        }

        if (flashSprite == null)
        {
            flashSprite = Sprite.Create(softImpactTexture,
                new Rect(0f, 0f, softImpactTexture.width, softImpactTexture.height),
                new Vector2(0.5f, 0.5f), softImpactTexture.width, 0, SpriteMeshType.FullRect);
            flashSprite.name = "Battle soft impact flash";
        }
    }

    private static void ApplyTexture(Material material, Texture texture)
    {
        if (material == null || texture == null)
            return;

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
    }

    private static void ConfigureTransparentParticleMaterial(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SurfaceType"))
            material.SetFloat("_SurfaceType", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 2f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void OnDestroy()
    {
        if (deathCameraMotion != null) deathCameraMotion.Cancel();
        closeupVisibility.Restore();
        if (spriteMat != null)
            Destroy(spriteMat);
        if (streakMat != null)
            Destroy(streakMat);
        if (particleMat != null)
            Destroy(particleMat);
        if (flashSprite != null)
            Destroy(flashSprite);
        if (whiteTexture != null)
            Destroy(whiteTexture);
        if (softImpactTexture != null)
            Destroy(softImpactTexture);
        if (impactLight != null)
            Destroy(impactLight.gameObject);
    }

    private void OnDisable()
    {
        if (deathCameraMotion != null) deathCameraMotion.Cancel();
        impactLightRemaining = 0f;
        if (impactLight != null)
            impactLight.enabled = false;
        StopCameraImpactImmediate();
        StopActiveCloseupShotImmediate();
    }
}
