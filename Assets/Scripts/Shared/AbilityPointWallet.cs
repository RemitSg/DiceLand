using System;
using System.IO;
using UnityEngine;

public sealed class AbilityPointWallet
{
    private const string FileName = "ability_points.json";
    private readonly string path;

    public static string DefaultPath => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    public AbilityPointWallet() : this(DefaultPath)
    {
    }

    public AbilityPointWallet(string path)
    {
        this.path = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
    }

    public string FilePath => path;

    public static bool IsValidSaveJson(string json)
    {
        try { var data = JsonUtility.FromJson<WalletData>(json); return data != null && data.version == 1 && data.abilityPoints >= 0; }
        catch { return false; }
    }

    public int GetCurrentPoints()
    {
        return Load().abilityPoints;
    }

    public int AddPoints(int amount)
    {
        if (amount <= 0)
            return GetCurrentPoints();

        WalletData data = Load();
        data.abilityPoints = Mathf.Max(0, data.abilityPoints + amount);
        Save(data);
        return data.abilityPoints;
    }

    public void SetPoints(int amount)
    {
        Save(new WalletData { version = 1, abilityPoints = Mathf.Max(0, amount) });
    }

    public bool TrySpendPoints(int amount, out int remainingPoints)
    {
        int cost = Mathf.Max(0, amount);
        WalletData data = Load();
        if (data.abilityPoints < cost)
        {
            remainingPoints = data.abilityPoints;
            return false;
        }

        data.abilityPoints = Mathf.Max(0, data.abilityPoints - cost);
        Save(data);
        remainingPoints = data.abilityPoints;
        return true;
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AbilityPointWallet] Failed to clear wallet at {path}: {ex.Message}");
        }
    }

    private WalletData Load()
    {
        if (!File.Exists(path))
            return new WalletData();

        try
        {
            WalletData data = JsonUtility.FromJson<WalletData>(File.ReadAllText(path));
            if (data == null)
                return new WalletData();

            data.version = Mathf.Max(1, data.version);
            data.abilityPoints = Mathf.Max(0, data.abilityPoints);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AbilityPointWallet] Failed to read wallet at {path}: {ex.Message}");
            return new WalletData();
        }
    }

    private void Save(WalletData data)
    {
        if (data == null)
            data = new WalletData();

        data.version = Mathf.Max(1, data.version);
        data.abilityPoints = Mathf.Max(0, data.abilityPoints);

        try
        {
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AbilityPointWallet] Failed to save wallet at {path}: {ex.Message}");
        }
    }

    [Serializable]
    private sealed class WalletData
    {
        public int version = 1;
        public int abilityPoints;
    }
}
