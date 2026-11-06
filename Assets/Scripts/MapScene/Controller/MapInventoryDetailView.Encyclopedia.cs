using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class MapInventoryDetailView
{
    private bool encyclopediaTextOnly, encyclopediaEntry;
    private BattleSkillInfoOverlay encyclopediaSkillFormatter;

    public void ShowEncyclopediaEntry(EncyclopediaCatalog.Entry entry)
    {
        if (modalRoot == null || entry?.Asset == null)
            return;

        encyclopediaTextOnly = entry.Category == EncyclopediaCategory.Calamities;
        bool hasRarity = entry.Rarity.HasValue;
        float left = encyclopediaTextOnly ? 62 : 460;
        float width = encyclopediaTextOnly ? 872 : 470;
        quantityText.transform.parent.gameObject.SetActive(false);
        displayEngraving.gameObject.SetActive(!encyclopediaTextOnly);
        columnRule.gameObject.SetActive(!encyclopediaTextOnly);
        rarityFace.gameObject.SetActive(hasRarity);
        PlaceDetail(titleText.rectTransform, left, 62, width - 18, 78);
        PlaceDetail(nameRule, left, 142, width, 6);
        PlaceDetail(rarityFace.rectTransform, left, 164, 164, 46);
        PlaceDetail(categoryFace.rectTransform, left + (hasRarity ? 188 : 0), 164, 200, 46);
        ResourceKeywordText.Attach(titleText, false);
        ResourceKeywordText.Attach(descriptionText, !encyclopediaTextOnly);
        ResourceKeywordText.Attach(relicAdditionalDescriptionText, !encyclopediaTextOnly);

        string category = MainMenuEncyclopediaOverlay.CategoryLabel(entry.Category);
        string description = string.Empty;
        string notes = string.Empty;
        switch (entry.Asset)
        {
            case EnemyData_SO enemy:
                description = DescribeEncyclopediaEnemy(enemy);
                break;
            case Weapon_SO weapon:
                category = GetWeaponTypeLabel(weapon.weaponType);
                description = DescribeEncyclopediaWeapon(weapon);
                break;
            case Relic_SO relic:
                description = relic.LocalizedDescription;
                notes = relic.LocalizedNotes;
                break;
            case LootItem_SO loot:
                category = loot.attribute != null ? loot.attribute.LocalizedName
                    : GameText.Get("ui.map_inventory_detail_view.no_attribute", "无属性");
                description = loot.LocalizedDescription;
                break;
            case Calamity_SO calamity:
                description = calamity.LocalizedDescription;
                break;
        }

        PresentItemCore(entry.Asset, entry.Icon, entry.DisplayName,
            hasRarity ? EncyclopediaRarityLabel(entry.Rarity.Value) : string.Empty,
            hasRarity ? GetLootRarityColor(entry.Rarity.Value) : DetailGold, category, 0, description, notes, true);
        headingText.text = GameText.Get("ui.encyclopedia.details", "图鉴详情");
        descriptionHeading.text = entry.Category == EncyclopediaCategory.Relics || encyclopediaTextOnly
            ? GameText.Get("ui.encyclopedia.effect", "效果") : GameText.Get("ui.encyclopedia.description", "详细信息");
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
    }

    internal static string EncyclopediaRarityLabel(LootRarity rarity) => rarity == LootRarity.Trash
        ? GameText.Get("ui.map_crafting.poor", "劣质") : GetLootRarityLabel(rarity);

    private string DescribeEncyclopediaEnemy(EnemyData_SO enemy)
    {
        var lines = new List<string>
        {
            GameText.Format("battle.skill.rules.max_hp", "最大生命：{baseHP}", ("baseHP", enemy.baseHP))
        };
        for (int stage = 0; stage < enemy.StageCount; stage++)
        {
            string speed = GameText.Format("ui.encyclopedia.enemy_speed", "速度 {min}–{max} · 速度骰 {slots}",
                ("min", enemy.GetStageMinSpeed(stage)), ("max", enemy.GetStageMaxSpeed(stage)),
                ("slots", enemy.GetStageSpeedButtonCount(stage)));
            lines.Add(enemy.StageCount == 1 ? speed : GameText.Format("ui.encyclopedia.stage", "阶段 {stage} · {speed}",
                ("stage", stage + 1), ("speed", speed)));
        }

        if (!string.IsNullOrWhiteSpace(enemy.LocalizedDescription))
            lines.Add("\n" + enemy.LocalizedDescription);
        if (enemy.passiveDescriptions != null)
        {
            for (int i = 0; i < enemy.passiveDescriptions.Count; i++)
            {
                EnemyData_SO.PassiveDescriptionEntry passive = enemy.passiveDescriptions[i];
                string name = enemy.Localize($"passive.{i}.name", passive.passiveName);
                string text = enemy.Localize($"passive.{i}.description", passive.description);
                if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(text))
                    lines.Add("\n" + name + "\n" + text);
            }
        }

        var skills = new List<EnemySkill_SO>();
        if (enemy.skillPool != null)
            skills.AddRange(enemy.skillPool);
        if (enemy.conditionalSkillRules != null)
            skills.AddRange(enemy.conditionalSkillRules.Select(rule => rule.skill));
        AppendEncyclopediaSkills(lines, skills, false);
        return string.Join("\n", lines);
    }

    private string DescribeEncyclopediaWeapon(Weapon_SO weapon)
    {
        var lines = new List<string>
        {
            GameText.Format("ui.encyclopedia.speed_slots", "速度骰 {slots}", ("slots", weapon.speedButtonCount))
        };
        if (!string.IsNullOrWhiteSpace(weapon.LocalizedDescription))
            lines.Add("\n" + weapon.LocalizedDescription);
        AppendEncyclopediaSkills(lines, weapon.skillPool, true);
        return string.Join("\n", lines);
    }

    private void AppendEncyclopediaSkills(List<string> lines, IEnumerable<EnemySkill_SO> skills, bool attackerIsAlly)
    {
        if (skills == null)
            return;
        foreach (EnemySkill_SO skill in skills.Where(skill => skill != null).Distinct())
        {
            if (encyclopediaSkillFormatter == null)
            {
                encyclopediaSkillFormatter = gameObject.AddComponent<BattleSkillInfoOverlay>();
                encyclopediaSkillFormatter.enabled = false;
            }
            lines.Add("\n" + encyclopediaSkillFormatter.DescribeSkillRules(skill, attackerIsAlly));
        }
    }
}
