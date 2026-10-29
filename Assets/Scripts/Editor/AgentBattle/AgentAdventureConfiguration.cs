using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reads saved game configuration without running the formal map or touching user saves.
public sealed class AgentAdventureConfiguration
{
    public RunStartingConfiguration starting;
    public EnemyRoster_SO enemies;
    public MoneyRewardTable_SO money;
    public RelicTable_SO eliteRelics;
    public LevelTable_SO levels;
    public CalamityPool_SO calamities;
    public MapEventPool_SO events;
    public MerchantCatalog_SO merchant;
    public TreasureRewardTable_SO treasure;
    public AbilityTreeConfig_SO abilities;
    public RunReforgeDefinition reforge;

    public static DifficultyConfig_SO[] SelectableDifficulties()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Read adventure configuration in Edit Mode.");
        MethodInfo open = typeof(EditorSceneManager).GetMethod("OpenPreviewScene", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(string) }, null);
        if (open == null) throw new NotSupportedException("This engine does not expose the required preview scene reader.");
        var outsideRandom = UnityEngine.Random.state;
        bool outsideInput = SpeedButton.InputSuppressed, outsideCombat = SpeedButton.CombatLocked;
        Scene preview = default;
        try
        {
            preview = (Scene)open.Invoke(null, new object[] { "Assets/Scenes/Menu Selection.unity" });
            return preview.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenuSceneController>(true))
                .Single().GetDifficultyConfigurations();
        }
        finally
        {
            try { if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview); }
            finally
            {
                UnityEngine.Random.state = outsideRandom;
                SpeedButton.InputSuppressed = outsideInput; SpeedButton.CombatLocked = outsideCombat;
            }
        }
    }

    public static DifficultyConfig_SO ResolveDifficulty(string id)
    {
        var difficulty = SelectableDifficulties().FirstOrDefault(d => string.Equals(
            AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d)), id, StringComparison.OrdinalIgnoreCase));
        if (difficulty == null) throw new ArgumentException("Unknown difficultyId; select an ID from catalog.difficulties.");
        return difficulty;
    }

    public static AgentAdventureDifficulty[] DescribeDifficulties()
    {
        return SelectableDifficulties().Select(d => new AgentAdventureDifficulty
        {
            id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d)), name = d.difficultyName,
            isDefault = AssetDatabase.GetAssetPath(d) == "Assets/Data/Game/NormalConfig.asset",
            boardSize = d.BoardSize, requiredKeys = d.requiredKeys, maxRecruitCount = Mathf.Max(1, d.maxRecruitCount),
            startingActionPoints = RunBootstrapService.ResolveActionPointLimitForTier(d, 0),
            actionPointLimits = Enumerable.Range(0, 11).Select(t => RunBootstrapService.ResolveActionPointLimitForTier(d, t)).ToArray(),
            recruitCosts = Enumerable.Range(0, Mathf.Max(1, d.maxRecruitCount)).Select(d.GetRecruitCostForCurrentRecruitCount).ToArray()
        }).ToArray();
    }

    public static AgentAdventureConfiguration Load(DifficultyConfig_SO difficulty = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Read adventure configuration in Edit Mode.");
        // This engine version exposes preview loading internally. Fail explicitly if that API changes.
        MethodInfo open = typeof(EditorSceneManager).GetMethod("OpenPreviewScene", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(string) }, null);
        if (open == null) throw new NotSupportedException("This engine does not expose the required preview scene reader.");
        Scene preview = (Scene)open.Invoke(null, new object[] { "Assets/MapScene.scene" });
        try
        {
            RunGameManager manager = preview.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RunGameManager>(true)).Single();
            var fields = new SerializedObject(manager);
            // NormalConfig is the actual shipped medium/normal difficulty; the scene's fallback is Easy.
            difficulty = difficulty != null ? difficulty : AssetDatabase.LoadAssetAtPath<DifficultyConfig_SO>("Assets/Data/Game/NormalConfig.asset");
            if (difficulty == null) throw new InvalidOperationException("The normal difficulty asset is missing.");
            return new AgentAdventureConfiguration
            {
                starting = manager.GetStartingConfiguration(difficulty),
                enemies = Asset<EnemyRoster_SO>(fields, "enemyRoster"), money = Asset<MoneyRewardTable_SO>(fields, "moneyRewardTable"),
                eliteRelics = Asset<RelicTable_SO>(fields, "eliteRelicChoiceTable"), levels = Asset<LevelTable_SO>(fields, "levelTable"),
                calamities = Asset<CalamityPool_SO>(fields, "calamityPool"), events = Asset<MapEventPool_SO>(fields, "eventPool"),
                merchant = Asset<MerchantCatalog_SO>(fields, "merchantCatalog"), treasure = Asset<TreasureRewardTable_SO>(fields, "treasureRewardTable"),
                abilities = Asset<AbilityTreeConfig_SO>(fields, "abilityTreeConfig"),
                reforge = LoadReforgeDefinition(open)
            };
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static RunReforgeDefinition LoadReforgeDefinition(MethodInfo open)
    {
        Scene preview = (Scene)open.Invoke(null, new object[] { "Assets/Scenes/RestScene.scene" });
        try { return preview.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RestSceneReforgeInteractable>(true)).Single().GetReforgeDefinition(); }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static T Asset<T>(SerializedObject fields, string name) where T : UnityEngine.Object
    {
        return fields.FindProperty(name).objectReferenceValue as T;
    }
}
