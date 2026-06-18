#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AbilityPointWalletEditor
{
    [MenuItem("Tools/Meta Progression/Show Ability Points")]
    public static void ShowAbilityPoints()
    {
        AbilityPointWallet wallet = new AbilityPointWallet();
        Debug.Log($"[AbilityPointWallet] Current ability points: {wallet.GetCurrentPoints()} | Path: {wallet.FilePath}");
    }

    [MenuItem("Tools/Meta Progression/Clear Ability Points")]
    public static void ClearAbilityPoints()
    {
        AbilityPointWallet wallet = new AbilityPointWallet();
        wallet.Clear();
        Debug.Log($"[AbilityPointWallet] Cleared ability points. Current ability points: {wallet.GetCurrentPoints()} | Path: {wallet.FilePath}");
    }

    [MenuItem("Tools/Meta Progression/Clear Ability Progression")]
    public static void ClearAbilityProgression()
    {
        AbilityProgressionService service = new AbilityProgressionService(null);
        if (!EditorUtility.DisplayDialog(
                "Clear Ability Progression",
                $"Clear unlocked ability nodes and enabled states?\n\nAbility points will not be changed.\n\nPath:\n{service.FilePath}",
                "Clear",
                "Cancel"))
            return;

        service.ClearProgression();
        Debug.Log($"[AbilityProgressionService] Cleared ability progression. Ability points were not changed. Path: {service.FilePath}");
    }

    [MenuItem("Tools/Meta Progression/Set Ability Points...")]
    public static void OpenSetAbilityPointsWindow()
    {
        AbilityPointWalletEditorWindow.Open();
    }
}

public sealed class AbilityPointWalletEditorWindow : EditorWindow
{
    private int points;

    public static void Open()
    {
        AbilityPointWalletEditorWindow window = GetWindow<AbilityPointWalletEditorWindow>("Ability Points");
        window.points = new AbilityPointWallet().GetCurrentPoints();
        window.minSize = new Vector2(280f, 96f);
        window.Show();
    }

    private void OnGUI()
    {
        AbilityPointWallet wallet = new AbilityPointWallet();
        EditorGUILayout.LabelField("Wallet Path", wallet.FilePath);
        points = EditorGUILayout.IntField("Ability Points", Mathf.Max(0, points));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save"))
            {
                wallet.SetPoints(points);
                Debug.Log($"[AbilityPointWallet] Set ability points to {wallet.GetCurrentPoints()} | Path: {wallet.FilePath}");
            }

            if (GUILayout.Button("Reload"))
                points = wallet.GetCurrentPoints();
        }
    }
}
#endif
