using System;
using System.Collections;
using UnityEngine;
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
    [SerializeField] private float hitEffectDuration = 0.24f;
    [SerializeField] private float builtInHitDamageDelay = 0.07f;

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
    [SerializeField, Min(0f)] private float clashShakeAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float hitShakeAmplitude = 0.16f;
    [SerializeField, Min(0f)] private float cameraPowerNormalization = 12f;
    [SerializeField, Range(0f, 1f)] private float minimumCameraIntensity = 0.18f;
    [SerializeField, Min(0.01f)] private float cameraShakeFrequency = 28f;
    [SerializeField] private AnimationCurve cameraShakeDecayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

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
    private Material particleMat;
    private Sprite whiteSprite;
    private Texture2D whiteTexture;
    private Coroutine activeCameraBeatRoutine;
    private Coroutine activeCloseupShotRoutine;
    private BattleFeedbackSystem battleFeedbackSystem;
    private int activeCloseupShotId;
    private readonly System.Collections.Generic.Dictionary<SpriteRenderer, ActiveSpriteSwap> activeSpriteSwaps = new System.Collections.Generic.Dictionary<SpriteRenderer, ActiveSpriteSwap>();
    private bool closeupDefaultsCached;
    private Vector3 closeupDefaultPosition;
    private Quaternion closeupDefaultRotation;
    private float closeupDefaultLensValue;
    private float closeupShakePulse;
    private float closeupShakeDuration;
    private float closeupShakeRemaining;
    private float closeupRangePulseDuration;
    private float closeupRangePulseRemaining;

    private void Awake()
    {
        EnsureRuntimeResources();
        ResolveFeedbackSystem()?.SetPresentationCamera(ResolveBattleCamera(), 1f);
    }

    public IEnumerator PlayClashSequence(
        Transform ally,
        Transform enemy,
        bool hasWinner,
        bool allyWon,
        EnemySkill_SO.DicePowerRange allyRange,
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

        EnsureRuntimeResources();
        BattleFeedbackSystem feedbackSystem = ResolveFeedbackSystem();

        PlaneData plane = BuildPlane(ally, enemy, clashPlaneReference);
        float gap = Mathf.Min(clashFaceGap, Mathf.Max(clashMinimumGap, plane.distance - 0.05f));
        Vector3 allyTarget = plane.midpoint - plane.direction * (gap * 0.5f);
        Vector3 enemyTarget = plane.midpoint + plane.direction * (gap * 0.5f);
        allyTarget.z = plane.z;
        enemyTarget.z = plane.z;

        BattleFeedbackSystem.ClashDiceDisplayHandle allyDiceDisplay = feedbackSystem?.ShowClashDiceRange(ally, false, allyRange);
        BattleFeedbackSystem.ClashDiceDisplayHandle enemyDiceDisplay = feedbackSystem?.ShowClashDiceRange(enemy, true, enemyRange);

        if (moveIntoPosition)
            yield return MovePair(ally, plane.firstStart, allyTarget, enemy, plane.secondStart, enemyTarget, advanceDuration);
        int clashShotId = StartCloseupShot(ally, enemy, plane.midpoint + Vector3.up * impactHeightOffset, Mathf.Max(clashCameraPower, hitCameraPower));
        BeginCloseupRangePulse(clashCloseupLeadDuration + delayBeforeClashDiceReveal);
        if (clashShotId != 0 && clashCloseupLeadDuration > 0f)
            yield return new WaitForSeconds(clashCloseupLeadDuration);
        if (delayBeforeClashDiceReveal > 0f)
            yield return new WaitForSeconds(delayBeforeClashDiceReveal);
        feedbackSystem?.RevealClashDiceValue(allyDiceDisplay, allyPower, allyPowerModifier);
        feedbackSystem?.RevealClashDiceValue(enemyDiceDisplay, enemyPower, enemyPowerModifier);
        yield return PlayClashExchangeFx(ally, enemy, plane.z, plane.midpoint + Vector3.up * impactHeightOffset, plane.direction, clashCameraPower, allyClashFx, enemyClashFx);

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

        if (returnToOrigin)
            yield return MovePair(ally, ally.position, plane.firstStart, enemy, enemy.position, plane.secondStart, returnDuration);
        feedbackSystem?.HideClashDiceDisplay(allyDiceDisplay);
        feedbackSystem?.HideClashDiceDisplay(enemyDiceDisplay);
        yield return EndCloseupShot(clashShotId, hasWinner ? hitCloseupHoldDuration : clashCloseupHoldDuration);
    }

    public IEnumerator PlaySingleAttackSequence(
        Transform attacker,
        Transform defender,
        bool attackerIsAlly,
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
        diceDisplay = feedbackSystem?.ShowClashDiceRange(attacker, !attackerIsAlly, attackRange);
        BeginCloseupRangePulse(singleAttackRangeDisplayDuration);

        if (singleAttackRangeDisplayDuration > 0f)
            yield return new WaitForSeconds(singleAttackRangeDisplayDuration);

        feedbackSystem?.RevealClashDiceValue(diceDisplay, attackPower, attackPowerModifier);
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
        TriggerCameraImpact(impactPosition, clashCameraPower, false);
        float waitDuration = Mathf.Max(0f, clashEffectDuration);

        PlaySideClashFx(ally, planeZ, allyFx, clashDirection, allyHitColor);
        PlaySideClashFx(enemy, planeZ, enemyFx, -clashDirection, enemyHitColor);

        StartCoroutine(PlayBuiltInClashImpactFx(impactPosition, clashDirection));
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
        GameObject root = CreateEffectRoot("ClashEffect", position, direction);
        ParticleSystem sparks = CreateBurst(root.transform, clashSparkColor, clashSparks, clashParticleFadeDuration);
        ParticleSystem ring = CreateRing(root.transform, clashPrimaryColor, clashRing, clashParticleFadeDuration);

        if (clashEffectDuration > 0f)
            yield return new WaitForSeconds(clashEffectDuration);

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
        TriggerCameraImpact(position, cameraImpactPower, true);
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
                flash.color = WithAlpha(defenderFlashColor, Mathf.Lerp(0.95f, 0f, normalized));
                flash.transform.localScale = Vector3.one * Mathf.Lerp(defenderReactionScale, defenderReactionScale * 1.8f, normalized);
            }

            yield return null;
        }

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
                flash.color = WithAlpha(flashColor, Mathf.Lerp(0.95f, 0f, normalized));
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
            sortingOrder = primaryRenderer.sortingOrder
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

    private float SpawnCustomEffect(GameObject prefab, Vector3 basePosition, Vector3 direction, Vector3 localOffset, Vector3 rotationOffset, Vector3 scale, float fallbackDuration, int sortingOrderOffset, bool forceDuration = false)
    {
        if (prefab == null)
            return fallbackDuration;

        Quaternion rotation = BuildFacingRotation(direction) * Quaternion.Euler(rotationOffset);
        Vector3 spawnPosition = basePosition + rotation * localOffset;
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
        renderer.sprite = whiteSprite;
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
        renderer.numCapVertices = 6;
        renderer.numCornerVertices = 4;
        renderer.material = spriteMat;
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

    private void TriggerCameraImpact(Vector3 focusPosition, float power, bool isHit)
    {
        if (!enableCameraImpact)
            return;

        if (UseCloseupCutCamera())
        {
            float intensity = NormalizeCameraIntensity(power);
            closeupShakePulse = Mathf.Max(closeupShakePulse, intensity);
            closeupShakeDuration = Mathf.Max(closeupShakeDuration, isHit ? hitCloseupHoldDuration : clashCloseupHoldDuration);
            closeupShakeRemaining = closeupShakeDuration;
            return;
        }

        Camera cameraToUse = ResolveBattleCamera();
        if (cameraToUse == null)
            return;

        if (activeCameraBeatRoutine != null)
            StopCoroutine(activeCameraBeatRoutine);

        activeCameraBeatRoutine = StartCoroutine(PlayCameraImpactBeat(cameraToUse, focusPosition, power, isHit));
    }

    private IEnumerator PlayCameraImpactBeat(Camera targetCamera, Vector3 focusPosition, float power, bool isHit)
    {
        if (targetCamera == null)
            yield break;

        Vector3 startPosition = targetCamera.transform.position;
        float startLensValue = targetCamera.orthographic ? targetCamera.orthographicSize : targetCamera.fieldOfView;
        float duration = Mathf.Max(0.01f, isHit ? hitCloseupHoldDuration : clashCloseupHoldDuration);
        float intensity = NormalizeCameraIntensity(power);
        float amplitude = (isHit ? hitShakeAmplitude : clashShakeAmplitude) * intensity;
        Vector3 desiredPosition = focusPosition + cameraFocusOffset;
        desiredPosition.z = startPosition.z;

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            if (targetCamera == null)
                yield break;

            float normalized = Mathf.Clamp01(time / duration);
            float envelope = Mathf.Sin(normalized * Mathf.PI);
            float decay = cameraShakeDecayCurve.Evaluate(normalized);
            float angle = time * cameraShakeFrequency * Mathf.PI * 2f;
            Vector2 shake = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle * 0.83f)) * (amplitude * decay);

            targetCamera.transform.position = Vector3.Lerp(startPosition, desiredPosition, envelope * 0.28f) + new Vector3(shake.x, shake.y, 0f);

            float lensTarget = targetCamera.orthographic
                ? Mathf.Max(0.2f, startLensValue - intensity * 0.45f)
                : Mathf.Max(1f, startLensValue - intensity * 4f);
            if (targetCamera.orthographic)
                targetCamera.orthographicSize = Mathf.Lerp(startLensValue, lensTarget, envelope);
            else
                targetCamera.fieldOfView = Mathf.Lerp(startLensValue, lensTarget, envelope);

            yield return null;
        }

        if (targetCamera != null)
        {
            targetCamera.transform.position = startPosition;
            if (targetCamera.orthographic)
                targetCamera.orthographicSize = startLensValue;
            else
                targetCamera.fieldOfView = startLensValue;
        }

        activeCameraBeatRoutine = null;
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

    private bool UseCloseupCutCamera()
    {
        return enableCameraImpact && enableCloseupCuts && closeupCamera != null && ResolveBattleCamera() != closeupCamera;
    }

    private int StartCloseupShot(Transform first, Transform second, Vector3 fallbackFocus, float power)
    {
        if (!UseCloseupCutCamera())
            return 0;

        StopActiveCloseupShotImmediate();
        CacheCloseupDefaults();
        activeCloseupShotId++;
        int shotId = activeCloseupShotId;
        closeupShakePulse = 0f;
        closeupShakeDuration = 0f;
        closeupShakeRemaining = 0f;
        closeupRangePulseDuration = 0f;
        closeupRangePulseRemaining = 0f;
        activeCloseupShotRoutine = StartCoroutine(RunCloseupShot(shotId, first, second, fallbackFocus, power));
        return shotId;
    }

    private IEnumerator EndCloseupShot(int shotId, float holdDuration)
    {
        if (shotId == 0 || shotId != activeCloseupShotId)
            yield break;

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

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

        float baseIntensity = NormalizeCameraIntensity(power);
        float baseShakeAmplitude = Mathf.Lerp(clashShakeAmplitude, hitShakeAmplitude, baseIntensity);
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

        while (shotId == activeCloseupShotId)
        {
            float desiredFrameSize = ResolveCloseupFrameSize(first, second);
            Vector3 framingOffset = ResolveCloseupFramingOffset(desiredFrameSize);
            Vector3 focus = ResolveShotFocus(first, second, fallbackFocus) + cameraFocusOffset + new Vector3(0f, framingOffset.y, 0f);
            Vector3 desiredPosition = focus + closeupCameraOffset + new Vector3(0f, 0f, framingOffset.z);
            float followT = 1f - Mathf.Exp(-Mathf.Max(0.01f, closeupFollowSharpness) * Time.deltaTime);

            if (closeupShakeRemaining > 0f)
                closeupShakeRemaining = Mathf.Max(0f, closeupShakeRemaining - Time.deltaTime);
            else
                closeupShakePulse = 0f;

            if (closeupRangePulseRemaining > 0f)
                closeupRangePulseRemaining = Mathf.Max(0f, closeupRangePulseRemaining - Time.deltaTime);

            float shakeNormalized = closeupShakeDuration > 0.0001f
                ? Mathf.Clamp01(closeupShakeRemaining / closeupShakeDuration)
                : 0f;
            float shakeDecay = cameraShakeDecayCurve.Evaluate(1f - shakeNormalized);
            float shakeStrength = closeupShakePulse * shakeDecay;
            float angle = Time.time * cameraShakeFrequency * Mathf.PI * 2f;
            Vector2 shake = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle * 0.83f)) * (baseShakeAmplitude * shakeStrength);

            float rangePulse = EvaluateCloseupRangePulse();
            Vector3 pulsePositionOffset = new Vector3(
                Mathf.Sin(Time.time * closeupRangePulseFrequency * Mathf.PI * 1.2f),
                Mathf.Cos(Time.time * closeupRangePulseFrequency * Mathf.PI * 0.9f),
                0f) * (closeupRangePositionPulseAmplitude * rangePulse);

            Vector3 cameraPosition = Vector3.Lerp(shotCamera.transform.position, desiredPosition, followT);
            cameraPosition += new Vector3(shake.x, shake.y, 0f) + pulsePositionOffset;
            shotCamera.transform.position = cameraPosition;

            if (shotCamera.orthographic)
            {
                float targetSize = Mathf.Max(0.2f, desiredFrameSize - closeupRangeLensPulseAmplitude * rangePulse);
                shotCamera.orthographicSize = Mathf.Lerp(shotCamera.orthographicSize, targetSize, followT);
                shotCamera.transform.rotation = closeupDefaultRotation;
            }
            else
            {
                float targetFov = Mathf.Max(1f, ResolveCloseupFieldOfView(desiredFrameSize) - closeupRangeLensPulseAmplitude * rangePulse * 2.2f);
                shotCamera.fieldOfView = Mathf.Lerp(shotCamera.fieldOfView, targetFov, followT);
                Vector3 lookTarget = focus + closeupLookOffset;
                shotCamera.transform.rotation = Quaternion.Slerp(
                    shotCamera.transform.rotation,
                    Quaternion.LookRotation((lookTarget - cameraPosition).normalized, Vector3.up),
                    followT);
            }

            yield return null;
        }

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

    private void BeginCloseupRangePulse(float duration)
    {
        if (duration <= 0f)
            return;

        closeupRangePulseDuration = Mathf.Max(closeupRangePulseDuration, duration);
        closeupRangePulseRemaining = Mathf.Max(closeupRangePulseRemaining, duration);
    }

    private float EvaluateCloseupRangePulse()
    {
        if (closeupRangePulseRemaining <= 0f || closeupRangePulseDuration <= 0.0001f)
            return 0f;

        float elapsedNormalized = 1f - Mathf.Clamp01(closeupRangePulseRemaining / closeupRangePulseDuration);
        float envelope = Mathf.Sin(elapsedNormalized * Mathf.PI);
        float wave = 0.5f - 0.5f * Mathf.Cos(elapsedNormalized * closeupRangePulseFrequency * Mathf.PI * 2f);
        return envelope * wave;
    }

    private void StopActiveCloseupShotImmediate()
    {
        if (activeCloseupShotRoutine != null)
            StopCoroutine(activeCloseupShotRoutine);

        activeCloseupShotRoutine = null;
        activeCloseupShotId++;
        closeupShakePulse = 0f;
        closeupShakeDuration = 0f;
        closeupShakeRemaining = 0f;
        closeupRangePulseDuration = 0f;
        closeupRangePulseRemaining = 0f;

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
        if (first != null && second != null)
            return (first.position + second.position) * 0.5f;
        if (first != null)
            return first.position;
        if (second != null)
            return second.position;
        return fallbackFocus;
    }

    private float ResolveCloseupFrameSize(Transform first, Transform second)
    {
        Bounds combinedBounds;
        bool hasBounds = false;

        if (BattleActorVisualUtility.TryGetActorSpriteBounds(first, out Bounds firstBounds))
        {
            combinedBounds = firstBounds;
            hasBounds = true;
        }
        else
        {
            combinedBounds = default;
        }

        if (BattleActorVisualUtility.TryGetActorSpriteBounds(second, out Bounds secondBounds))
        {
            if (hasBounds)
            {
                combinedBounds.Encapsulate(secondBounds.min);
                combinedBounds.Encapsulate(secondBounds.max);
            }
            else
            {
                combinedBounds = secondBounds;
                hasBounds = true;
            }
        }

        if (!hasBounds)
            return closeupOrthographicSize;

        float paddedHalfHeight = combinedBounds.extents.y + closeupBoundsPadding;
        float widthDrivenHalfHeight = combinedBounds.extents.x * Mathf.Max(0.1f, closeupWidthToHeightRatio) + closeupBoundsPadding;
        return Mathf.Max(closeupOrthographicSize, paddedHalfHeight, widthDrivenHalfHeight);
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

    private void EnsureRuntimeResources()
    {
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

        if (particleMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                particleMat = new Material(shader);
                ApplyTexture(particleMat, whiteTexture);
                ConfigureTransparentParticleMaterial(particleMat);
            }
        }

        if (whiteSprite == null)
            whiteSprite = Sprite.Create(whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
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
        if (spriteMat != null)
            Destroy(spriteMat);
        if (particleMat != null)
            Destroy(particleMat);
        if (whiteSprite != null)
            Destroy(whiteSprite);
        if (whiteTexture != null)
            Destroy(whiteTexture);
    }
}
