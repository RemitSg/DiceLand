using UnityEngine;

[CreateAssetMenu(fileName = "NewBattleStatus", menuName = "Battle/Status")]
public class BattleStatus_SO : ScriptableObject
{
    public enum StatusBehaviorType
    {
        None = 0,
        DamageAtRoundStart = 1,
        SpeedBonusAtRoundStart = 2,
        DamageWhenHit = 3,
        DamageTakenBonusPercent = 4,
        ApplyLinkedStatusAtRoundStart = 5,
        SpeedPenaltyAtRoundStart = 6,
        MinPowerPenalty = 7,
        PowerBonusFlat = 8,
        PowerPenaltyFlat = 9,
        DefensePowerBonusFlat = 10,
        DefensePowerPenaltyFlat = 11
    }

    public enum StackDecayMode
    {
        None = 0,
        HalveRoundedDown = 1,
        LoseAll = 2
    }

    [Header("Identity")]
    [SerializeField] private string statusId = "burn";
    [SerializeField] private string displayName = "\u71c3\u70e7";

    [TextArea(2, 4)]
    [SerializeField] private string description = "\u6bcf\u56de\u5408\u5f00\u59cb\u65f6\u53d7\u5230\u7b49\u540c\u4e8e\u5c42\u6570\u7684\u4f24\u5bb3\u3002";

    [Header("Visual")]
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Color accentColor = Color.white;
    [SerializeField] private GameObject tickEffectPrefab;
    [SerializeField] private Vector3 tickEffectPositionOffset;
    [SerializeField] private Vector3 tickEffectRotationOffset;
    [SerializeField] private Vector3 tickEffectScale = Vector3.one;
    [SerializeField] private int tickEffectSortingOrderOffset;
    [SerializeField] private bool overrideTickEffectDuration;
    [SerializeField, Min(0.01f)] private float tickEffectDuration = 0.5f;

    [Header("Behavior")]
    [SerializeField] private StatusBehaviorType behaviorType = StatusBehaviorType.DamageAtRoundStart;
    [SerializeField, Min(0)] private int powerPerStack = 1;
    [SerializeField] private BattleStatus_SO linkedStatusData;
    [SerializeField] private StackDecayMode onHitDecayMode = StackDecayMode.None;
    [SerializeField] private StackDecayMode roundStartDecayMode = StackDecayMode.HalveRoundedDown;
    [SerializeField] private StackDecayMode roundEndDecayMode = StackDecayMode.None;

    public string StatusId => string.IsNullOrWhiteSpace(statusId) ? name : statusId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite IconSprite => iconSprite;
    public Color AccentColor => accentColor;
    public GameObject TickEffectPrefab => tickEffectPrefab;
    public Vector3 TickEffectPositionOffset => tickEffectPositionOffset;
    public Vector3 TickEffectRotationOffset => tickEffectRotationOffset;
    public Vector3 TickEffectScale => IsZeroScale(tickEffectScale) ? Vector3.one : tickEffectScale;
    public int TickEffectSortingOrderOffset => tickEffectSortingOrderOffset;
    public bool OverrideTickEffectDuration => overrideTickEffectDuration;
    public float TickEffectDuration => Mathf.Max(0.01f, tickEffectDuration);
    public StatusBehaviorType BehaviorType => behaviorType;
    public int PowerPerStack => Mathf.Max(0, powerPerStack);
    public BattleStatus_SO LinkedStatusData => linkedStatusData;
    public StackDecayMode OnHitDecayMode => onHitDecayMode;
    public StackDecayMode RoundStartDecayMode => roundStartDecayMode;
    public StackDecayMode RoundEndDecayMode => roundEndDecayMode;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(statusId))
            statusId = name;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;
        if (powerPerStack < 0)
            powerPerStack = 0;
    }

    private static bool IsZeroScale(Vector3 scale)
    {
        return Mathf.Approximately(scale.x, 0f)
            && Mathf.Approximately(scale.y, 0f)
            && Mathf.Approximately(scale.z, 0f);
    }
}
