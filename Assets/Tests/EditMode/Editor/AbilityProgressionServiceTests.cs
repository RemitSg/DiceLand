using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class AbilityProgressionServiceTests
{
    private string walletPath;
    private string progressionPath;

    [SetUp]
    public void SetUp()
    {
        string suffix = System.Guid.NewGuid().ToString("N");
        walletPath = Path.Combine(Path.GetTempPath(), $"ability-wallet-{suffix}.json");
        progressionPath = Path.Combine(Path.GetTempPath(), $"ability-progression-{suffix}.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(walletPath))
            File.Delete(walletPath);
        if (File.Exists(progressionPath))
            File.Delete(progressionPath);
    }

    [Test]
    public void GetNodeState_RequiresPrerequisitesBeforeNodeBecomesAvailable()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("root", "Root", 1, new Vector2(0f, 0f), null, Effect(AbilityEffectType.StartingMoneyFlat, 10)),
            Node("child", "Child", 1, new Vector2(160f, 0f), new[] { "root" }, Effect(AbilityEffectType.StartingActionPointLimitFlat, 2)));
        AbilityPointWallet wallet = new AbilityPointWallet(walletPath);
        wallet.SetPoints(5);
        AbilityProgressionService service = new AbilityProgressionService(config, wallet, progressionPath);

        Assert.AreEqual(AbilityNodeState.Available, service.GetNodeState("root"));
        Assert.AreEqual(AbilityNodeState.Locked, service.GetNodeState("child"));

        Assert.IsTrue(service.TryUnlock("root").success);

        Assert.AreEqual(AbilityNodeState.Unlocked, service.GetNodeState("root"));
        Assert.AreEqual(AbilityNodeState.Available, service.GetNodeState("child"));
    }

    [Test]
    public void TryUnlock_SpendsPointsAndPersistsUnlockedNode()
    {
        AbilityTreeConfig_SO config = CreateConfig(Node("root", "Root", 3, Vector2.zero, null, Effect(AbilityEffectType.StartingMoneyFlat, 10)));
        AbilityPointWallet wallet = new AbilityPointWallet(walletPath);
        wallet.SetPoints(5);
        AbilityProgressionService service = new AbilityProgressionService(config, wallet, progressionPath);

        AbilityUnlockResult result = service.TryUnlock("root");

        Assert.IsTrue(result.success);
        Assert.AreEqual(2, wallet.GetCurrentPoints());
        Assert.AreEqual(AbilityNodeState.Unlocked, service.GetNodeState("root"));

        AbilityProgressionService reloaded = new AbilityProgressionService(config, new AbilityPointWallet(walletPath), progressionPath);
        Assert.AreEqual(AbilityNodeState.Unlocked, reloaded.GetNodeState("root"));
    }

    [Test]
    public void ClearProgression_RemovesUnlockedNodesWithoutClearingAbilityPoints()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("root", "Root", 1, Vector2.zero, null, Effect(AbilityEffectType.StartingMoneyFlat, 10)),
            Node("child", "Child", 1, new Vector2(160f, 0f), new[] { "root" }, Effect(AbilityEffectType.StartingActionPointLimitFlat, 2)));
        AbilityPointWallet wallet = new AbilityPointWallet(walletPath);
        wallet.SetPoints(5);
        AbilityProgressionService service = new AbilityProgressionService(config, wallet, progressionPath);
        Assert.IsTrue(service.TryUnlock("root").success);
        Assert.IsTrue(File.Exists(progressionPath));

        service.ClearProgression();

        Assert.IsFalse(File.Exists(progressionPath));
        Assert.AreEqual(4, wallet.GetCurrentPoints());
        Assert.AreEqual(AbilityNodeState.Available, service.GetNodeState("root"));
        Assert.AreEqual(AbilityNodeState.Locked, service.GetNodeState("child"));
    }

    [Test]
    public void TryUnlock_FailsWithoutEnoughPoints()
    {
        AbilityTreeConfig_SO config = CreateConfig(Node("root", "Root", 3, Vector2.zero, null, Effect(AbilityEffectType.StartingMoneyFlat, 10)));
        AbilityPointWallet wallet = new AbilityPointWallet(walletPath);
        wallet.SetPoints(2);
        AbilityProgressionService service = new AbilityProgressionService(config, wallet, progressionPath);

        AbilityUnlockResult result = service.TryUnlock("root");

        Assert.IsFalse(result.success);
        Assert.AreEqual(2, wallet.GetCurrentPoints());
        Assert.AreEqual(AbilityNodeState.Available, service.GetNodeState("root"));
    }

    [Test]
    public void TryUnlock_FailsWhenPrerequisiteIsLocked()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("root", "Root", 1, Vector2.zero, null, Effect(AbilityEffectType.StartingMoneyFlat, 10)),
            Node("child", "Child", 1, new Vector2(160f, 0f), new[] { "root" }, Effect(AbilityEffectType.StartingActionPointLimitFlat, 2)));
        AbilityPointWallet wallet = new AbilityPointWallet(walletPath);
        wallet.SetPoints(5);
        AbilityProgressionService service = new AbilityProgressionService(config, wallet, progressionPath);

        AbilityUnlockResult result = service.TryUnlock("child");

        Assert.IsFalse(result.success);
        Assert.AreEqual(5, wallet.GetCurrentPoints());
        Assert.AreEqual(AbilityNodeState.Locked, service.GetNodeState("child"));
    }

    [Test]
    public void BuildRunBonus_SumsUnlockedEffects()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("money1", "Money I", 0, Vector2.zero, null, Effect(AbilityEffectType.StartingMoneyFlat, 10)),
            Node("money2", "Money II", 0, new Vector2(160f, 0f), new[] { "money1" }, Effect(AbilityEffectType.StartingMoneyFlat, 15)),
            Node("ap1", "AP I", 0, new Vector2(320f, 0f), new[] { "money2" }, Effect(AbilityEffectType.StartingActionPointLimitFlat, 2)));
        AbilityProgressionService service = new AbilityProgressionService(config, new AbilityPointWallet(walletPath), progressionPath);
        service.TryUnlock("money1");
        service.TryUnlock("money2");
        service.TryUnlock("ap1");

        AbilityRunBonus bonus = service.BuildRunBonus();

        Assert.AreEqual(25, bonus.startingMoneyFlat);
        Assert.AreEqual(2, bonus.startingActionPointLimitFlat);
    }

    [Test]
    public void BuildRunBonus_CollectsNewRunModifiersFromUnlockedEffects()
    {
        BlueprintTable_SO blueprintTable = ScriptableObject.CreateInstance<BlueprintTable_SO>();
        LootTable_SO lootTable = ScriptableObject.CreateInstance<LootTable_SO>();
        TreasureRewardTable_SO treasureTable = ScriptableObject.CreateInstance<TreasureRewardTable_SO>();
        CharacterData_SO extraCompanion = ScriptableObject.CreateInstance<CharacterData_SO>();
        Weapon_SO replacementWeapon = ScriptableObject.CreateInstance<Weapon_SO>();
        AbilityTreeConfig_SO config = CreateConfig(
            Node("blueprint", "Blueprint", 0, Vector2.zero, null, Effect(AbilityEffectType.StartingBlueprint, 2, null, null, null, null, null, blueprintTable)),
            Node("loot-table", "Loot Table", 0, new Vector2(160f, 0f), null, Effect(AbilityEffectType.StartingLootTableRoll, 3, null, lootTable)),
            Node("merchant", "Merchant", 0, new Vector2(320f, 0f), null, Effect(AbilityEffectType.MerchantFreeRefreshPerShop, 1), Effect(AbilityEffectType.FirstMerchantPurchaseHalfDiscount, 1)),
            Node("energy", "Energy", 0, new Vector2(480f, 0f), null, Effect(AbilityEffectType.BattleStartTemporaryEnergy, 2), Effect(AbilityEffectType.BattleStartEnergyMaxFlat, 1)),
            Node("party", "Party", 0, new Vector2(640f, 0f), null, Effect(AbilityEffectType.StartingPartySizeFlat, 1, null, null, null, extraCompanion)),
            Node("treasure", "Treasure", 0, new Vector2(800f, 0f), null, Effect(AbilityEffectType.TreasureRewardTableOverride, 0, null, null, treasureTable)),
            Node("weapon", "Weapon", 0, new Vector2(960f, 0f), null, Effect(AbilityEffectType.StartingWeaponReplacement, 0, null, null, null, null, replacementWeapon)),
            Node("relic-refresh", "Relic Refresh", 0, new Vector2(1120f, 0f), null, Effect(AbilityEffectType.RelicChoiceRefreshCount, 1)),
            Node("health", "Health", 0, new Vector2(1280f, 0f), null, Effect(AbilityEffectType.BattleStartMaxHealthFlat, 8)));
        AbilityProgressionService service = new AbilityProgressionService(config, new AbilityPointWallet(walletPath), progressionPath);
        foreach (AbilityNodeDefinition node in config.nodes)
            service.TryUnlock(node.nodeId);

        AbilityRunBonus bonus = service.BuildRunBonus();

        Assert.AreEqual(1, bonus.startingBlueprints.Count);
        Assert.AreEqual(blueprintTable, bonus.startingBlueprints[0].blueprintTable);
        Assert.AreEqual(2, bonus.startingBlueprints[0].rollCount);
        Assert.AreEqual(1, bonus.startingLootTableRolls.Count);
        Assert.AreEqual(lootTable, bonus.startingLootTableRolls[0].lootTable);
        Assert.AreEqual(3, bonus.startingLootTableRolls[0].rollCount);
        Assert.AreEqual(1, bonus.merchantFreeRefreshPerShop);
        Assert.IsTrue(bonus.firstMerchantPurchaseHalfDiscount);
        Assert.AreEqual(2, bonus.battleStartTemporaryEnergy);
        Assert.AreEqual(1, bonus.battleStartEnergyMaxFlat);
        Assert.AreEqual(1, bonus.startingPartySizeFlat);
        Assert.AreEqual(extraCompanion, bonus.extraStartingCompanion);
        Assert.AreEqual(treasureTable, bonus.treasureRewardTableOverride);
        Assert.AreEqual(replacementWeapon, bonus.startingWeaponReplacement);
        Assert.AreEqual(1, bonus.relicChoiceRefreshCount);
        Assert.AreEqual(8, bonus.battleStartMaxHealthFlat);
    }

    [Test]
    public void ToggleNodeEnabled_EnforcesMutualExclusionGroup()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("extra-a", "Extra A", 0, Vector2.zero, null, Effect(AbilityEffectType.ExtraStartingCompanion, 0)),
            Node("extra-b", "Extra B", 0, new Vector2(160f, 0f), null, Effect(AbilityEffectType.ExtraStartingCompanion, 0)));
        config.nodes[0].isToggleable = true;
        config.nodes[0].mutualExclusionGroupId = "extra-companion";
        config.nodes[1].isToggleable = true;
        config.nodes[1].mutualExclusionGroupId = "extra-companion";
        AbilityProgressionService service = new AbilityProgressionService(config, new AbilityPointWallet(walletPath), progressionPath);
        service.TryUnlock("extra-a");
        service.TryUnlock("extra-b");

        Assert.IsTrue(service.SetNodeEnabled("extra-a", true).success);
        Assert.IsTrue(service.SetNodeEnabled("extra-b", true).success);

        Assert.IsFalse(service.IsNodeEnabled("extra-a"));
        Assert.IsTrue(service.IsNodeEnabled("extra-b"));
    }

    [Test]
    public void TryUnlock_DefaultEnabledToggleNodeDisablesEveryOtherNodeInMutualExclusionGroup()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("extra-a", "Extra A", 0, Vector2.zero, null, Effect(AbilityEffectType.ExtraStartingCompanion, 0)),
            Node("extra-b", "Extra B", 0, new Vector2(160f, 0f), null, Effect(AbilityEffectType.ExtraStartingCompanion, 0)),
            Node("extra-c", "Extra C", 0, new Vector2(320f, 0f), null, Effect(AbilityEffectType.ExtraStartingCompanion, 0)));
        for (int i = 0; i < config.nodes.Count; i++)
        {
            config.nodes[i].isToggleable = true;
            config.nodes[i].enabledByDefault = true;
            config.nodes[i].mutualExclusionGroupId = "extra-companion";
        }
        AbilityProgressionService service = new AbilityProgressionService(config, new AbilityPointWallet(walletPath), progressionPath);

        Assert.IsTrue(service.TryUnlock("extra-a").success);
        Assert.IsTrue(service.TryUnlock("extra-b").success);
        Assert.IsTrue(service.TryUnlock("extra-c").success);

        Assert.IsFalse(service.IsNodeEnabled("extra-a"));
        Assert.IsFalse(service.IsNodeEnabled("extra-b"));
        Assert.IsTrue(service.IsNodeEnabled("extra-c"));
    }

    [Test]
    public void ValidateTree_DetectsDuplicateIdsMissingPrerequisitesAndCycles()
    {
        AbilityTreeConfig_SO config = CreateConfig(
            Node("a", "A", 1, Vector2.zero, new[] { "b" }, Effect(AbilityEffectType.StartingMoneyFlat, 10)),
            Node("b", "B", 1, new Vector2(160f, 0f), new[] { "a" }, Effect(AbilityEffectType.StartingMoneyFlat, 10)),
            Node("a", "Duplicate", 1, new Vector2(320f, 0f), new[] { "missing" }, Effect(AbilityEffectType.StartingMoneyFlat, 10)),
            Node(string.Empty, "Empty", -1, new Vector2(480f, 0f), null, Effect(AbilityEffectType.None, 0)));

        AbilityTreeValidationResult result = AbilityProgressionService.ValidateTree(config);

        Assert.IsFalse(result.isValid);
        Assert.IsTrue(result.messages.Exists(message => message.Contains("[DUPLICATE_ID]")));
        Assert.IsTrue(result.messages.Exists(message => message.Contains("[MISSING_PREREQUISITE]")));
        Assert.IsTrue(result.messages.Exists(message => message.Contains("[CYCLE]")));
        Assert.IsTrue(result.messages.Exists(message => message.Contains("[EMPTY_ID]")));
        Assert.IsTrue(result.messages.Exists(message => message.Contains("[NEGATIVE_COST]")));
    }

    private static AbilityTreeConfig_SO CreateConfig(params AbilityNodeDefinition[] nodes)
    {
        AbilityTreeConfig_SO config = ScriptableObject.CreateInstance<AbilityTreeConfig_SO>();
        config.nodes = new List<AbilityNodeDefinition>(nodes);
        return config;
    }

    private static AbilityNodeDefinition Node(
        string nodeId,
        string displayName,
        int cost,
        Vector2 position,
        IEnumerable<string> prerequisites,
        params AbilityNodeEffect[] effects)
    {
        return new AbilityNodeDefinition
        {
            nodeId = nodeId,
            displayName = displayName,
            description = displayName,
            cost = cost,
            position = position,
            prerequisiteNodeIds = prerequisites != null ? new List<string>(prerequisites) : new List<string>(),
            effects = new List<AbilityNodeEffect>(effects)
        };
    }

    private static AbilityNodeEffect Effect(AbilityEffectType effectType, int value)
    {
        return new AbilityNodeEffect { effectType = effectType, intValue = value };
    }

    private static AbilityNodeEffect Effect(
        AbilityEffectType effectType,
        int value,
        CraftingBlueprint_SO blueprint,
        LootTable_SO lootTable = null,
        TreasureRewardTable_SO treasureRewardTable = null,
        CharacterData_SO characterData = null,
        Weapon_SO weaponData = null,
        BlueprintTable_SO blueprintTable = null)
    {
        return new AbilityNodeEffect
        {
            effectType = effectType,
            intValue = value,
            blueprintData = blueprint,
            blueprintTable = blueprintTable,
            lootTable = lootTable,
            treasureRewardTable = treasureRewardTable,
            characterData = characterData,
            weaponData = weaponData
        };
    }
}
