using UnityEngine;

public class BattleData
{
    public int CurrentWave { get; set; }
    public int EnemyCount { get; set; }
    public bool IsBattleActive { get; set; }

    public BattleData()
    {
        CurrentWave = 1;
        EnemyCount = 0;
        IsBattleActive = false;
    }
}

public class BattleManager : GenericManager<BattleData>
{
    public GameObject alliesParent;
    public GameObject enemiesParent;
    public Transform LocationPoint;
    public GameObject SpeedButtonPrefab;
    public Material EnemyMaterial;
    public LevelTable_SO LevelTable;

    protected override void Awake()
    {
        BattleManager[] managers = FindObjectsOfType<BattleManager>(true);
        for (int i = 0; i < managers.Length; i++)
        {
            BattleManager other = managers[i];
            if (other == null || other == this)
                continue;

            bool thisIsPersistent = gameObject.scene.name == "DontDestroyOnLoad";
            bool otherIsPersistent = other.gameObject.scene.name == "DontDestroyOnLoad";

            if (thisIsPersistent && !otherIsPersistent)
            {
                Destroy(gameObject);
                return;
            }

            if (!thisIsPersistent && otherIsPersistent)
            {
                Destroy(other.gameObject);
                continue;
            }

            Destroy(other.gameObject);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        ManagedData = new BattleData();
        Debug.Log("BattleManager initialized with battle data");
    }

    public void StartBattle()
    {
        if (ManagedData == null)
            return;

        ManagedData.IsBattleActive = true;
        Debug.Log($"Battle started - Wave: {ManagedData.CurrentWave}");
    }

    public void EndBattle()
    {
        if (ManagedData == null)
            return;

        ManagedData.IsBattleActive = false;
        ManagedData.CurrentWave++;
        Debug.Log($"Battle ended - Next wave: {ManagedData.CurrentWave}");
    }

    public void AddEnemy()
    {
        if (ManagedData == null)
            return;

        ManagedData.EnemyCount++;
        Debug.Log($"Enemy added - Total enemies: {ManagedData.EnemyCount}");
    }

    public void RemoveEnemy()
    {
        if (ManagedData == null || ManagedData.EnemyCount <= 0)
            return;

        ManagedData.EnemyCount--;
        Debug.Log($"Enemy removed - Total enemies: {ManagedData.EnemyCount}");
    }
}
