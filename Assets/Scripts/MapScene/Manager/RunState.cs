using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunPartyMemberState
{
    public string memberId;
    public CharacterData_SO characterData;
    public Weapon_SO weaponData;
    public int level = 1;
    public int experience;
    public int maxHP;
    public int currentHP;

    public string DisplayName =>
        characterData != null && !string.IsNullOrWhiteSpace(characterData.characterName)
            ? characterData.characterName
            : (!string.IsNullOrWhiteSpace(memberId) ? memberId : "PartyMember");

    public Sprite BattleSprite =>
        characterData != null
            ? (characterData.battleSprite != null ? characterData.battleSprite : characterData.icon)
            : null;

    public bool IsAlive => currentHP > 0;

    public RunPartyMemberState Clone()
    {
        return new RunPartyMemberState
        {
            memberId = memberId,
            characterData = characterData,
            weaponData = weaponData,
            level = level,
            experience = experience,
            maxHP = maxHP,
            currentHP = currentHP
        };
    }
}

[Serializable]
public class RunLootState
{
    public LootItem_SO lootItem;
    public int amount;

    public string DisplayName =>
        lootItem != null && !string.IsNullOrWhiteSpace(lootItem.lootName)
            ? lootItem.lootName
            : "Loot";

    public RunLootState Clone()
    {
        return new RunLootState
        {
            lootItem = lootItem,
            amount = amount
        };
    }
}

[Serializable]
public class RunWeaponState
{
    public Weapon_SO weaponData;
    public int amount;

    public string DisplayName =>
        weaponData != null && !string.IsNullOrWhiteSpace(weaponData.weaponName)
            ? weaponData.weaponName
            : "Weapon";

    public RunWeaponState Clone()
    {
        return new RunWeaponState
        {
            weaponData = weaponData,
            amount = amount
        };
    }
}

[Serializable]
public class RunBlueprintState
{
    public CraftingBlueprint_SO blueprintData;
    public int amount;

    public string DisplayName =>
        blueprintData != null && !string.IsNullOrWhiteSpace(blueprintData.DisplayName)
            ? blueprintData.DisplayName
            : "Blueprint";

    public RunBlueprintState Clone()
    {
        return new RunBlueprintState
        {
            blueprintData = blueprintData,
            amount = amount
        };
    }
}

[Serializable]
public class RunRelicState
{
    public Relic_SO relicData;
    public int amount;

    public string DisplayName =>
        relicData != null && !string.IsNullOrWhiteSpace(relicData.DisplayName)
            ? relicData.DisplayName
            : "Relic";

    public RunRelicState Clone()
    {
        return new RunRelicState
        {
            relicData = relicData,
            amount = amount
        };
    }
}

[Serializable]
public class RunCalamityState
{
    public Calamity_SO calamityData;
    public int acquiredAtTier;

    public string DisplayName =>
        calamityData != null && !string.IsNullOrWhiteSpace(calamityData.DisplayName)
            ? calamityData.DisplayName
            : "Calamity";

    public RunCalamityState Clone()
    {
        return new RunCalamityState
        {
            calamityData = calamityData,
            acquiredAtTier = acquiredAtTier
        };
    }
}

[Serializable]
public class PendingBattleStatusGrant
{
    public BattleStatus_SO statusData;
    public int stacks;

    public PendingBattleStatusGrant Clone()
    {
        return new PendingBattleStatusGrant
        {
            statusData = statusData,
            stacks = stacks
        };
    }
}

[Serializable]
public class RunState
{
    public string difficultyName;
    public int boardSize;
    public Vector2Int playerGridPosition;
    public int money;
    public int recruitedMemberCount;
    public int currentKeys;
    public int requiredKeys;
    public int playerLevel = 1;
    public int playerExperience;
    public int calamityTier;
    public int currentCycle;
    public List<RunPartyMemberState> partyMembers = new List<RunPartyMemberState>();
    public List<RunLootState> lootInventory = new List<RunLootState>();
    public List<RunWeaponState> weaponInventory = new List<RunWeaponState>();
    public List<RunBlueprintState> blueprintInventory = new List<RunBlueprintState>();
    public List<RunRelicState> relicInventory = new List<RunRelicState>();
    public List<RunCalamityState> activeCalamities = new List<RunCalamityState>();
    public List<string> drawnCalamityIds = new List<string>();
    public List<string> drawnEventIds = new List<string>();
    public List<PendingBattleStatusGrant> pendingNextBattleAllyStatusGrants = new List<PendingBattleStatusGrant>();

    public bool IsBossUnlocked => currentKeys >= requiredKeys;

    public RunPartyMemberState PrimaryPartyMember =>
        partyMembers != null && partyMembers.Count > 0 ? partyMembers[0] : null;

    public IReadOnlyList<RunPartyMemberState> GetLivingPartyMembers()
    {
        if (partyMembers == null || partyMembers.Count == 0)
            return Array.Empty<RunPartyMemberState>();

        List<RunPartyMemberState> living = new List<RunPartyMemberState>(partyMembers.Count);
        for (int i = 0; i < partyMembers.Count; i++)
        {
            RunPartyMemberState member = partyMembers[i];
            if (member == null || !member.IsAlive)
                continue;

            living.Add(member.Clone());
        }

        return living;
    }

    public int GetPartyMemberCount()
    {
        return partyMembers != null ? partyMembers.Count : 0;
    }

    public bool HasCharacter(CharacterData_SO characterData)
    {
        if (characterData == null || partyMembers == null)
            return false;

        for (int i = 0; i < partyMembers.Count; i++)
        {
            RunPartyMemberState member = partyMembers[i];
            if (member != null && member.characterData == characterData)
                return true;
        }

        return false;
    }

    public void AddPartyMember(RunPartyMemberState member)
    {
        if (member == null)
            return;

        if (partyMembers == null)
            partyMembers = new List<RunPartyMemberState>();

        partyMembers.Add(member);
        SyncLegacyPlayerFieldsFromParty();
    }

    public void SyncLegacyPlayerFieldsFromParty()
    {
        RunPartyMemberState primaryMember = PrimaryPartyMember;
        if (primaryMember == null)
            return;

        playerLevel = Mathf.Max(1, primaryMember.level);
        playerExperience = Mathf.Max(0, primaryMember.experience);
    }

    public void ApplyExperienceToPrimaryMember(int amount)
    {
        if (amount <= 0)
            return;

        RunPartyMemberState primaryMember = PrimaryPartyMember;
        if (primaryMember != null)
            primaryMember.experience = Mathf.Max(0, primaryMember.experience + amount);

        SyncLegacyPlayerFieldsFromParty();
    }

    public void AddLoot(LootItem_SO lootItem, int amount)
    {
        if (lootItem == null || amount <= 0)
            return;

        if (lootInventory == null)
            lootInventory = new List<RunLootState>();

        for (int i = 0; i < lootInventory.Count; i++)
        {
            RunLootState stack = lootInventory[i];
            if (stack == null || stack.lootItem != lootItem)
                continue;

            stack.amount += amount;
            return;
        }

        lootInventory.Add(new RunLootState
        {
            lootItem = lootItem,
            amount = amount
        });
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        money = Mathf.Max(0, money + amount);
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0)
            return true;
        if (money < amount)
            return false;

        money -= amount;
        if (money < 0)
            money = 0;
        return true;
    }

    public void AddWeapon(Weapon_SO weaponData, int amount = 1)
    {
        if (weaponData == null || amount <= 0)
            return;

        if (weaponInventory == null)
            weaponInventory = new List<RunWeaponState>();

        for (int i = 0; i < weaponInventory.Count; i++)
        {
            RunWeaponState stack = weaponInventory[i];
            if (stack == null || stack.weaponData != weaponData)
                continue;

            stack.amount += amount;
            return;
        }

        weaponInventory.Add(new RunWeaponState
        {
            weaponData = weaponData,
            amount = amount
        });
    }

    public int GetWeaponAmount(Weapon_SO weaponData)
    {
        if (weaponData == null || weaponInventory == null)
            return 0;

        for (int i = 0; i < weaponInventory.Count; i++)
        {
            RunWeaponState stack = weaponInventory[i];
            if (stack == null || stack.weaponData != weaponData)
                continue;

            return Mathf.Max(0, stack.amount);
        }

        return 0;
    }

    public RunPartyMemberState GetEquippedMember(Weapon_SO weaponData)
    {
        if (weaponData == null || partyMembers == null)
            return null;

        for (int i = 0; i < partyMembers.Count; i++)
        {
            RunPartyMemberState member = partyMembers[i];
            if (member != null && member.weaponData == weaponData)
                return member;
        }

        return null;
    }

    public RunPartyMemberState GetPreferredCompatibleMember(Weapon_SO weaponData)
    {
        if (weaponData == null || partyMembers == null)
            return null;

        RunPartyMemberState firstCompatible = null;
        for (int i = 0; i < partyMembers.Count; i++)
        {
            RunPartyMemberState member = partyMembers[i];
            if (member == null || member.characterData == null)
                continue;
            if (member.characterData.compatibleWeaponType != weaponData.weaponType)
                continue;

            if (firstCompatible == null)
                firstCompatible = member;

            if (member.weaponData == null)
                return member;
        }

        return firstCompatible;
    }

    public bool TryEquipWeapon(Weapon_SO weaponData, out RunPartyMemberState equippedMember, out string errorMessage)
    {
        equippedMember = null;
        errorMessage = string.Empty;

        if (weaponData == null)
        {
            errorMessage = "未选择武器。";
            return false;
        }

        if (GetWeaponAmount(weaponData) <= 0)
        {
            errorMessage = "背包中没有该武器。";
            return false;
        }

        RunPartyMemberState currentOwner = GetEquippedMember(weaponData);
        if (currentOwner != null)
        {
            equippedMember = currentOwner;
            errorMessage = $"{currentOwner.DisplayName} 当前已装备该武器。";
            return false;
        }

        RunPartyMemberState targetMember = GetPreferredCompatibleMember(weaponData);
        if (targetMember == null)
        {
            errorMessage = "当前队伍中没有适配该武器的角色。";
            return false;
        }

        targetMember.weaponData = weaponData;
        equippedMember = targetMember;
        SyncLegacyPlayerFieldsFromParty();
        return true;
    }

    public void AddBlueprint(CraftingBlueprint_SO blueprintData, int amount = 1)
    {
        if (blueprintData == null || amount <= 0)
            return;

        if (blueprintInventory == null)
            blueprintInventory = new List<RunBlueprintState>();

        for (int i = 0; i < blueprintInventory.Count; i++)
        {
            RunBlueprintState stack = blueprintInventory[i];
            if (stack == null || stack.blueprintData != blueprintData)
                continue;

            stack.amount += amount;
            return;
        }

        blueprintInventory.Add(new RunBlueprintState
        {
            blueprintData = blueprintData,
            amount = amount
        });
    }

    public int GetBlueprintAmount(CraftingBlueprint_SO blueprintData)
    {
        if (blueprintData == null || blueprintInventory == null)
            return 0;

        for (int i = 0; i < blueprintInventory.Count; i++)
        {
            RunBlueprintState stack = blueprintInventory[i];
            if (stack == null || stack.blueprintData != blueprintData)
                continue;

            return Mathf.Max(0, stack.amount);
        }

        return 0;
    }

    public void AddRelic(Relic_SO relicData, int amount = 1)
    {
        if (relicData == null || amount <= 0)
            return;

        if (relicInventory == null)
            relicInventory = new List<RunRelicState>();

        for (int i = 0; i < relicInventory.Count; i++)
        {
            RunRelicState stack = relicInventory[i];
            if (stack == null || stack.relicData != relicData)
                continue;

            stack.amount += amount;
            return;
        }

        relicInventory.Add(new RunRelicState
        {
            relicData = relicData,
            amount = amount
        });
    }

    public int GetRelicAmount(Relic_SO relicData)
    {
        if (relicData == null || relicInventory == null)
            return 0;

        for (int i = 0; i < relicInventory.Count; i++)
        {
            RunRelicState stack = relicInventory[i];
            if (stack == null || stack.relicData != relicData)
                continue;

            return Mathf.Max(0, stack.amount);
        }

        return 0;
    }

    public bool TryConsumeRelic(Relic_SO relicData, int amount)
    {
        if (relicData == null || amount <= 0 || relicInventory == null)
            return false;

        for (int i = 0; i < relicInventory.Count; i++)
        {
            RunRelicState stack = relicInventory[i];
            if (stack == null || stack.relicData != relicData)
                continue;

            if (stack.amount < amount)
                return false;

            stack.amount -= amount;
            if (stack.amount <= 0)
                relicInventory.RemoveAt(i);
            return true;
        }

        return false;
    }

    public bool HasDrawnCalamity(Calamity_SO calamityData)
    {
        string calamityId = GetCalamityIdentity(calamityData);
        if (string.IsNullOrWhiteSpace(calamityId) || drawnCalamityIds == null)
            return false;

        for (int i = 0; i < drawnCalamityIds.Count; i++)
        {
            if (string.Equals(drawnCalamityIds[i], calamityId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public void MarkCalamityDrawn(Calamity_SO calamityData)
    {
        string calamityId = GetCalamityIdentity(calamityData);
        if (string.IsNullOrWhiteSpace(calamityId))
            return;

        if (drawnCalamityIds == null)
            drawnCalamityIds = new List<string>();

        for (int i = 0; i < drawnCalamityIds.Count; i++)
        {
            if (string.Equals(drawnCalamityIds[i], calamityId, StringComparison.OrdinalIgnoreCase))
                return;
        }

        drawnCalamityIds.Add(calamityId);
    }

    public bool HasDrawnEvent(MapEvent_SO eventData)
    {
        string eventId = GetEventIdentity(eventData);
        if (string.IsNullOrWhiteSpace(eventId) || drawnEventIds == null)
            return false;

        for (int i = 0; i < drawnEventIds.Count; i++)
        {
            if (string.Equals(drawnEventIds[i], eventId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public void MarkEventDrawn(MapEvent_SO eventData)
    {
        string eventId = GetEventIdentity(eventData);
        if (string.IsNullOrWhiteSpace(eventId))
            return;

        if (drawnEventIds == null)
            drawnEventIds = new List<string>();

        for (int i = 0; i < drawnEventIds.Count; i++)
        {
            if (string.Equals(drawnEventIds[i], eventId, StringComparison.OrdinalIgnoreCase))
                return;
        }

        drawnEventIds.Add(eventId);
    }

    public void AddCalamity(Calamity_SO calamityData, int acquiredTier)
    {
        if (calamityData == null)
            return;

        if (activeCalamities == null)
            activeCalamities = new List<RunCalamityState>();

        string calamityId = GetCalamityIdentity(calamityData);
        for (int i = 0; i < activeCalamities.Count; i++)
        {
            RunCalamityState state = activeCalamities[i];
            if (state == null || state.calamityData == null)
                continue;

            if (string.Equals(GetCalamityIdentity(state.calamityData), calamityId, StringComparison.OrdinalIgnoreCase))
                return;
        }

        activeCalamities.Add(new RunCalamityState
        {
            calamityData = calamityData,
            acquiredAtTier = Mathf.Max(1, acquiredTier)
        });
    }

    public void QueueNextBattleAllyStatusGrant(BattleStatus_SO statusData, int stacks)
    {
        if (statusData == null || stacks <= 0)
            return;

        if (pendingNextBattleAllyStatusGrants == null)
            pendingNextBattleAllyStatusGrants = new List<PendingBattleStatusGrant>();

        for (int i = 0; i < pendingNextBattleAllyStatusGrants.Count; i++)
        {
            PendingBattleStatusGrant entry = pendingNextBattleAllyStatusGrants[i];
            if (entry == null || entry.statusData != statusData)
                continue;

            entry.stacks += stacks;
            return;
        }

        pendingNextBattleAllyStatusGrants.Add(new PendingBattleStatusGrant
        {
            statusData = statusData,
            stacks = stacks
        });
    }

    public List<PendingBattleStatusGrant> ConsumePendingNextBattleAllyStatusGrants()
    {
        List<PendingBattleStatusGrant> consumed = new List<PendingBattleStatusGrant>();
        if (pendingNextBattleAllyStatusGrants != null)
        {
            for (int i = 0; i < pendingNextBattleAllyStatusGrants.Count; i++)
            {
                PendingBattleStatusGrant entry = pendingNextBattleAllyStatusGrants[i];
                if (entry != null && entry.statusData != null && entry.stacks > 0)
                    consumed.Add(entry.Clone());
            }
        }

        pendingNextBattleAllyStatusGrants = new List<PendingBattleStatusGrant>();
        return consumed;
    }

    private static string GetCalamityIdentity(Calamity_SO calamityData)
    {
        if (calamityData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(calamityData.UniqueId))
            return calamityData.UniqueId;

        return calamityData.name;
    }

    private static string GetEventIdentity(MapEvent_SO eventData)
    {
        if (eventData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(eventData.eventId))
            return eventData.eventId;

        return eventData.name;
    }

    public int GetLootAmount(LootItem_SO lootItem)
    {
        if (lootItem == null || lootInventory == null)
            return 0;

        for (int i = 0; i < lootInventory.Count; i++)
        {
            RunLootState stack = lootInventory[i];
            if (stack == null || stack.lootItem != lootItem)
                continue;

            return Mathf.Max(0, stack.amount);
        }

        return 0;
    }

    public bool TryConsumeLoot(LootItem_SO lootItem, int amount)
    {
        if (lootItem == null || amount <= 0 || lootInventory == null)
            return false;

        for (int i = 0; i < lootInventory.Count; i++)
        {
            RunLootState stack = lootInventory[i];
            if (stack == null || stack.lootItem != lootItem)
                continue;

            if (stack.amount < amount)
                return false;

            stack.amount -= amount;
            if (stack.amount <= 0)
                lootInventory.RemoveAt(i);
            return true;
        }

        return false;
    }

    public RunState Clone()
    {
        RunState clone = new RunState
        {
            difficultyName = difficultyName,
            boardSize = boardSize,
            playerGridPosition = playerGridPosition,
            money = money,
            recruitedMemberCount = recruitedMemberCount,
            currentKeys = currentKeys,
            requiredKeys = requiredKeys,
            playerLevel = playerLevel,
            playerExperience = playerExperience,
            calamityTier = calamityTier,
            currentCycle = currentCycle
        };

        if (partyMembers != null)
        {
            clone.partyMembers = new List<RunPartyMemberState>(partyMembers.Count);
            for (int i = 0; i < partyMembers.Count; i++)
            {
                RunPartyMemberState member = partyMembers[i];
                if (member != null)
                    clone.partyMembers.Add(member.Clone());
            }
        }

        if (lootInventory != null)
        {
            clone.lootInventory = new List<RunLootState>(lootInventory.Count);
            for (int i = 0; i < lootInventory.Count; i++)
            {
                RunLootState loot = lootInventory[i];
                if (loot != null)
                    clone.lootInventory.Add(loot.Clone());
            }
        }

        if (weaponInventory != null)
        {
            clone.weaponInventory = new List<RunWeaponState>(weaponInventory.Count);
            for (int i = 0; i < weaponInventory.Count; i++)
            {
                RunWeaponState weapon = weaponInventory[i];
                if (weapon != null)
                    clone.weaponInventory.Add(weapon.Clone());
            }
        }

        if (blueprintInventory != null)
        {
            clone.blueprintInventory = new List<RunBlueprintState>(blueprintInventory.Count);
            for (int i = 0; i < blueprintInventory.Count; i++)
            {
                RunBlueprintState blueprint = blueprintInventory[i];
                if (blueprint != null)
                    clone.blueprintInventory.Add(blueprint.Clone());
            }
        }

        if (relicInventory != null)
        {
            clone.relicInventory = new List<RunRelicState>(relicInventory.Count);
            for (int i = 0; i < relicInventory.Count; i++)
            {
                RunRelicState relic = relicInventory[i];
                if (relic != null)
                    clone.relicInventory.Add(relic.Clone());
            }
        }

        if (activeCalamities != null)
        {
            clone.activeCalamities = new List<RunCalamityState>(activeCalamities.Count);
            for (int i = 0; i < activeCalamities.Count; i++)
            {
                RunCalamityState calamity = activeCalamities[i];
                if (calamity != null)
                    clone.activeCalamities.Add(calamity.Clone());
            }
        }

        if (drawnCalamityIds != null)
            clone.drawnCalamityIds = new List<string>(drawnCalamityIds);
        if (drawnEventIds != null)
            clone.drawnEventIds = new List<string>(drawnEventIds);
        if (pendingNextBattleAllyStatusGrants != null)
        {
            clone.pendingNextBattleAllyStatusGrants = new List<PendingBattleStatusGrant>(pendingNextBattleAllyStatusGrants.Count);
            for (int i = 0; i < pendingNextBattleAllyStatusGrants.Count; i++)
            {
                PendingBattleStatusGrant entry = pendingNextBattleAllyStatusGrants[i];
                if (entry != null)
                    clone.pendingNextBattleAllyStatusGrants.Add(entry.Clone());
            }
        }

        return clone;
    }
}
