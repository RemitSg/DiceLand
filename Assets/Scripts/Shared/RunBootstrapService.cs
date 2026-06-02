using System.Collections.Generic;
using UnityEngine;

public sealed class RunStartingInventoryDefinition
{
    public IReadOnlyList<RunGameManager.StartingLootDefinition> startingLoot;
    public IReadOnlyList<RunGameManager.StartingWeaponDefinition> startingWeapons;
    public IReadOnlyList<RunGameManager.StartingBlueprintDefinition> startingBlueprints;
    public IReadOnlyList<RunGameManager.StartingRelicDefinition> startingRelics;
}

public static class RunBootstrapService
{
    private const int TutorialEncounterSeedBase = 26000;

    private static readonly MapTileType[] TutorialTileSequence =
    {
        MapTileType.Empty,
        MapTileType.Empty,
        MapTileType.Battle,
        MapTileType.Battle,
        MapTileType.Rest,
        MapTileType.Battle,
        MapTileType.Merchant,
        MapTileType.Battle,
        MapTileType.Forge,
        MapTileType.Battle,
        MapTileType.Key,
        MapTileType.Boss
    };

    public static int ResolveActionPointLimitForTier(DifficultyConfig_SO config, int calamityTier)
    {
        if (config == null)
            return 6;

        return Mathf.Max(1, config.GetActionPointLimitForCalamityTier(calamityTier));
    }

    public static List<BoardTileData> BuildTutorialBoardTiles(int boardSize)
    {
        int row = Mathf.Max(1, boardSize / 2);
        List<BoardTileData> tiles = new List<BoardTileData>(TutorialTileSequence.Length);
        for (int i = 0; i < TutorialTileSequence.Length; i++)
        {
            MapTileType tileType = TutorialTileSequence[i];
            TutorialBattleStageId stageId = ResolveTutorialStageId(i);
            tiles.Add(new BoardTileData
            {
                coord = new Vector2Int(i, row),
                tileType = tileType,
                dangerBand = ResolveTutorialDangerBand(i),
                isVisited = i == 0,
                isResolved = tileType == MapTileType.Empty,
                isSpawnTile = i == 0,
                encounterSeed = TutorialEncounterSeedBase + i,
                previewEnemyCount = ResolveTutorialPreviewEnemyCount(tileType, stageId),
                previewDangerRating = ResolveTutorialPreviewDanger(tileType, stageId, ResolveTutorialDangerBand(i)),
                previewTag = ResolveTutorialPreviewTag(tileType, stageId)
            });
        }

        return tiles;
    }

    public static TutorialBattleStageId ResolveTutorialStageId(int sequenceIndex)
    {
        switch (sequenceIndex)
        {
            case 2:
                return TutorialBattleStageId.Battle01;
            case 3:
                return TutorialBattleStageId.Battle02;
            case 5:
                return TutorialBattleStageId.Battle03;
            case 7:
                return TutorialBattleStageId.Battle04;
            case 9:
                return TutorialBattleStageId.Battle05;
            case 11:
                return TutorialBattleStageId.Battle07;
            default:
                return TutorialBattleStageId.None;
        }
    }

    public static MapDangerBand ResolveTutorialDangerBand(int sequenceIndex)
    {
        if (sequenceIndex <= 2)
            return MapDangerBand.Outer;
        if (sequenceIndex <= 5)
            return MapDangerBand.Mid;
        if (sequenceIndex <= 7)
            return MapDangerBand.Inner;
        return MapDangerBand.Core;
    }

    public static int ResolveTutorialPreviewEnemyCount(MapTileType tileType, TutorialBattleStageId stageId)
    {
        return TutorialBattleStageUtility.IsBattleStage(stageId) || tileType == MapTileType.Boss ? 1 : 0;
    }

    public static int ResolveTutorialPreviewDanger(TutorialBattleStageId stageId)
    {
        return ResolveTutorialPreviewDanger(MapTileType.Battle, stageId, MapDangerBand.Outer);
    }

    public static int ResolveTutorialPreviewDanger(MapTileType tileType, TutorialBattleStageId stageId, MapDangerBand dangerBand)
    {
        switch (stageId)
        {
            case TutorialBattleStageId.Battle01:
            case TutorialBattleStageId.Battle02:
                return 1;
            case TutorialBattleStageId.Battle03:
            case TutorialBattleStageId.Battle04:
                return 2;
            case TutorialBattleStageId.Battle05:
                return 4;
            case TutorialBattleStageId.Battle07:
                return 5;
            default:
                return tileType == MapTileType.Boss
                    ? MapDangerRatingService.ResolveDefaultRating(tileType, dangerBand)
                    : 0;
        }
    }

    public static string ResolveTutorialPreviewTag(MapTileType tileType, TutorialBattleStageId stageId)
    {
        if (TutorialBattleStageUtility.IsBattleStage(stageId))
            return TutorialBattleStageUtility.IsFinalStage(stageId) ? "教程终战" : "教程战斗";

        switch (tileType)
        {
            case MapTileType.Rest:
                return "教程营地";
            case MapTileType.Merchant:
                return "教程商店";
            case MapTileType.Forge:
                return "教程锻造";
            case MapTileType.Key:
                return "教程钥匙";
            case MapTileType.Boss:
                return "教程首领";
            default:
                return "教程路线";
        }
    }

    public static List<RunPartyMemberState> BuildTutorialStartingPartyMembers(
        IReadOnlyList<RunGameManager.StartingPartyMemberDefinition> tutorialDefinitions,
        IReadOnlyList<RunGameManager.StartingPartyMemberDefinition> fallbackDefinitions)
    {
        IReadOnlyList<RunGameManager.StartingPartyMemberDefinition> source = tutorialDefinitions;
        if (source == null || source.Count == 0)
            source = fallbackDefinitions;

        return BuildStartingPartyMembers(source);
    }

    public static List<RunPartyMemberState> BuildStartingPartyMembers(
        IReadOnlyList<RunGameManager.StartingPartyMemberDefinition> sourceDefinitions)
    {
        List<RunPartyMemberState> result = new List<RunPartyMemberState>();
        if (sourceDefinitions != null)
        {
            for (int i = 0; i < sourceDefinitions.Count; i++)
            {
                RunGameManager.StartingPartyMemberDefinition definition = sourceDefinitions[i];
                if (definition == null || definition.characterData == null)
                    continue;

                int baseHP = Mathf.Max(1, definition.characterData.baseHP);
                Weapon_SO resolvedStartingWeapon = definition.equippedWeapon != null
                    ? definition.equippedWeapon
                    : definition.characterData.initialWeaponData;
                int maxHP = definition.startingMaxHP > 0 ? Mathf.Max(1, definition.startingMaxHP) : baseHP;
                result.Add(new RunPartyMemberState
                {
                    memberId = string.IsNullOrWhiteSpace(definition.memberId) ? definition.characterData.characterName : definition.memberId,
                    characterData = definition.characterData,
                    weaponData = resolvedStartingWeapon,
                    level = Mathf.Max(1, definition.startingLevel),
                    experience = Mathf.Max(0, definition.startingExperience),
                    maxHP = maxHP,
                    currentHP = definition.startingCurrentHP > 0
                        ? Mathf.Clamp(definition.startingCurrentHP, 0, maxHP)
                        : maxHP
                });
            }
        }

        if (result.Count == 0)
        {
            result.Add(new RunPartyMemberState
            {
                memberId = "DefaultPartyMember",
                level = 1,
                experience = 0,
                maxHP = 20,
                currentHP = 20
            });
        }

        return result;
    }

    public static void ApplyStartingInventory(RunState targetRunState, RunStartingInventoryDefinition inventoryDefinition)
    {
        if (targetRunState == null || inventoryDefinition == null)
            return;

        ApplyStartingLoot(targetRunState, inventoryDefinition.startingLoot);
        ApplyStartingWeapons(targetRunState, inventoryDefinition.startingWeapons);
        ApplyStartingBlueprints(targetRunState, inventoryDefinition.startingBlueprints);
        ApplyStartingRelics(targetRunState, inventoryDefinition.startingRelics);
        AddEquippedWeaponsToInventory(targetRunState);
    }

    private static void ApplyStartingLoot(
        RunState targetRunState,
        IReadOnlyList<RunGameManager.StartingLootDefinition> startingLoot)
    {
        if (startingLoot == null)
            return;

        for (int i = 0; i < startingLoot.Count; i++)
        {
            RunGameManager.StartingLootDefinition definition = startingLoot[i];
            if (definition != null && definition.lootItem != null && definition.amount > 0)
                targetRunState.AddLoot(definition.lootItem, definition.amount);
        }
    }

    private static void ApplyStartingWeapons(
        RunState targetRunState,
        IReadOnlyList<RunGameManager.StartingWeaponDefinition> startingWeapons)
    {
        if (startingWeapons == null)
            return;

        for (int i = 0; i < startingWeapons.Count; i++)
        {
            RunGameManager.StartingWeaponDefinition definition = startingWeapons[i];
            if (definition != null && definition.weaponData != null && definition.amount > 0)
                targetRunState.AddWeapon(definition.weaponData, definition.amount);
        }
    }

    private static void ApplyStartingBlueprints(
        RunState targetRunState,
        IReadOnlyList<RunGameManager.StartingBlueprintDefinition> startingBlueprints)
    {
        if (startingBlueprints == null)
            return;

        for (int i = 0; i < startingBlueprints.Count; i++)
        {
            RunGameManager.StartingBlueprintDefinition definition = startingBlueprints[i];
            if (definition != null && definition.blueprintData != null && definition.amount > 0)
                targetRunState.AddBlueprint(definition.blueprintData, definition.amount);
        }
    }

    private static void ApplyStartingRelics(
        RunState targetRunState,
        IReadOnlyList<RunGameManager.StartingRelicDefinition> startingRelics)
    {
        if (startingRelics == null)
            return;

        for (int i = 0; i < startingRelics.Count; i++)
        {
            RunGameManager.StartingRelicDefinition definition = startingRelics[i];
            if (definition != null && definition.relicData != null && definition.amount > 0)
                targetRunState.AddRelic(definition.relicData, definition.amount);
        }
    }

    private static void AddEquippedWeaponsToInventory(RunState targetRunState)
    {
        if (targetRunState.partyMembers == null)
            return;

        for (int i = 0; i < targetRunState.partyMembers.Count; i++)
        {
            RunPartyMemberState member = targetRunState.partyMembers[i];
            if (member != null && member.weaponData != null)
                targetRunState.AddWeapon(member.weaponData, 1);
        }
    }
}
