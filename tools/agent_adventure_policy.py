"""Visible-state baseline decisions; combat outcomes are always executed by the server."""
import re


def crafting_plan(state, blueprint, catalogue=None):
    """Read every public ingredient; the forge's canCraft is authoritative."""
    matches = [b for b in (catalogue or {}).get("blueprintDetails", []) if blueprint in (b["id"], b["name"])]
    if len(matches) != 1:
        raise ValueError("Expected one blueprint ID or exact name")
    recipe = matches[0]
    actual = next((r for r in state.get("recipes", []) if r["id"] == recipe["id"]), None)
    owned = {m["id"]: m["amount"] for m in state.get("loot", [])}
    materials = actual["materials"] if actual else [dict(m, required=m["amount"], available=owned.get(m["id"], 0)) for m in recipe["materials"]]
    materials = [dict(m, missing=max(0, m["required"] - m["available"])) for m in materials]
    cost = actual["cost"] if actual else state.get("craftingCost", 0)
    valid = bool(materials) and bool(recipe.get("weapon"))
    return {"blueprint": recipe["id"], "weapon": recipe.get("weapon"), "materials": materials,
            "missingMaterials": [m for m in materials if m["missing"]], "cost": cost,
            "moneyShortfall": max(0, cost - state.get("money", 0)), "validRecipe": valid,
            "canCraft": actual["canCraft"] if actual else None,
            "materialsReady": valid and not any(m["missing"] for m in materials),
            "authority": "forge" if actual else "catalogue estimate; inspect forge before crafting"}


def relic_value(item, state):
    text = (item.get("name") or "") + " " + (item.get("description") or "")
    dungeon = bool((state.get("dungeon") or {}).get("active"))
    if any(w in text for w in ("特殊副本", "特殊地牢")) and not dungeon:
        return 5
    score = {"Common": 30, "Uncommon": 40, "Rare": 50, "Epic": 60, "Legendary": 70}.get(item.get("rarity"), 35)
    if "燃烧" in text and any(w in text for w in ("战斗开始", "回合开始", "回合结束", "每回合")):
        score += 35
    if any(w in text for w in ("减免", "减少", "护盾", "恢复", "回复", "最大生命")):
        score += 18
    if "龙鳞甲" in text:
        score += 25 if any(p.get("name") == "Jenny" for p in state.get("party", [])) else 0
    if "电流" in text and any("电磁" in p.get("weaponName", "") for p in state.get("party", [])):
        score += 15
    return score


def choose_material_purchase(state, blueprint, catalogue=None, reserve=0):
    """Buy a missing ingredient only when this shop can fund the complete plan."""
    plan = crafting_plan(state, blueprint, catalogue)
    if not plan["validRecipe"] or not plan["missingMaterials"]:
        return None
    offers = [o for o in state.get("merchantOffers", []) if o.get("canPurchase") and not o.get("soldOut") and o.get("kind") == "Loot"]
    needed = []
    for material in plan["missingMaterials"]:
        candidates = sorted((o for o in offers if o["id"] == material["id"]), key=lambda o: o["price"])
        if len(candidates) < material["missing"]:
            return None
        needed.extend(candidates[:material["missing"]])
    if state.get("money", 0) < reserve + plan["cost"] + sum(o["price"] for o in needed):
        return None
    indexes = {o["index"] for o in needed}
    return next((a for a in state["legalActions"] if a["op"] == "run_buy" and a["choiceIndex"] in indexes), None)


def choose_relic_action(state):
    phase = state.get("phase")
    if phase == "battle" or (state.get("battle") or {}).get("phase") == "reward":
        choices = state["battle"].get("rewards", {}).get("choices", [])
        actions = [a for a in state["legalActions"] if a["op"] == "run_battle" and a["battleAction"]["op"] == "choose_relic"]
        index = lambda a: a["battleAction"]["choiceIndex"]
    else:
        starting = phase == "starting_relic"
        choices = state["startingRelic"]["choices"] if starting else state["treasure"]["relicChoices"]
        op = "run_choose_starting_relic" if starting else "run_choose_treasure_relic"
        actions = [a for a in state["legalActions"] if a["op"] == op]
        index = lambda a: a["choiceIndex"]
    offered = {c.get("index", i): c for i, c in enumerate(choices)}
    return max(actions, key=lambda a: relic_value(offered.get(index(a), {}), state), default=None)


def can_intercept(slot, hostile, actor_id):
    return (hostile.get("targetId") == actor_id and hostile.get("targetButtonIndex") == slot["index"]) or slot["speed"] >= hostile["speed"]


def next_recruit_cost(state, catalogue=None):
    if state.get("phase") == "rest":
        return state["rest"]["recruitCost"]
    difficulty = next((d for d in (catalogue or {}).get("difficulties", [])
                       if d["id"] == state.get("difficultyId")), None)
    costs = difficulty.get("recruitCosts", []) if difficulty else []
    index = max(0, len(state.get("party", [])) - 1)
    return costs[index] if index < len(costs) else None


def choose_treasure_action(state, catalogue=None):
    """Value the offered reward against the next affordable preparation step."""
    options = {o["index"]: o for o in state["treasure"]["options"]}
    cost = next_recruit_cost(state, catalogue)
    can_visit_rest = any(t["type"] == "Rest" and not t["resolved"] for t in state.get("tiles", []))
    recruit_goal = cost if can_visit_rest and len(state.get("party", [])) < 4 else None

    def value(action):
        option = options[action["choiceIndex"]]
        if option["kind"] == "Money":
            closes_gap = recruit_goal is not None and state["money"] < recruit_goal <= state["money"] + option["money"]
            return 105 if closes_gap else (65 if state["money"] < 30 else 25)
        if option["kind"] in ("RelicBag", "Relic"):
            if option["kind"] == "Relic":
                item = next((r for r in (catalogue or {}).get("relics", []) if r["id"] == option.get("relic")), option)
                return relic_value(item, state)
            return {"Common": 45, "Uncommon": 60, "Rare": 75, "Epic": 90, "Legendary": 115}.get(option.get("rarity"), 50)
        if option["kind"] == "Blueprint":
            recipe = next((b for b in (catalogue or {}).get("blueprintDetails", []) if b["id"] == option["blueprint"]), None)
            # An empty authored material list can mean an invalid recipe, not
            # free equipment. Actual forge canCraft remains authoritative.
            ready = recipe and crafting_plan(state, recipe["id"], catalogue)["materialsReady"]
            return 70 if ready and state["money"] >= state.get("craftingCost", 0) else 10
        return 0

    return max((a for a in state["legalActions"] if a["op"] == "run_choose_treasure"), key=value, default=None)


def preparation_assessment(state, encounter_type, catalogue=None, starting_loadout=None):
    """Conservative encounter gate from visible preparation, never a win prediction."""
    party = [p for p in state.get("party", []) if p["hp"] > 0]
    initial = {c["id"]: c["initialWeapon"] for c in (catalogue or {}).get("characterDetails", [])}
    # A character can use a runtime fallback weapon absent from the catalogue.
    # Only a recorded fresh opening is a trustworthy baseline; a resumed party
    # may already be equipped and must not be treated as its own starting gear.
    initial.update({key: value for key, value in (starting_loadout or {}).items() if value})
    unknown = [p["character"] for p in party if not initial.get(p["character"])]
    upgraded = sum(bool(initial.get(p["character"])) and p["weapon"] != initial[p["character"]] for p in party)
    health = sum(p["hp"] for p in party) / max(1, sum(p["maxHp"] for p in party))
    levels = sum(p["level"] for p in party) / max(1, len(party))
    relics = sum(r.get("amount", 1) for r in state.get("relics", []))
    reasons = []
    if encounter_type in ("Elite", "Boss"):
        if len(party) < 3:
            reasons.append("高风险遭遇前至少准备三名存活队员")
        if health < .75:
            reasons.append("全队生命不足75%，先安排营地恢复")
        if levels < 1.7:
            reasons.append("先通过普通遭遇积累等级")
        if upgraded < (2 if encounter_type == "Boss" else 1) and relics < (3 if encounter_type == "Boss" else 2):
            reasons.append("装备或遗物成长不足，不能把钥匙齐全当作战力齐备")
        if encounter_type == "Boss":
            if len(party) < 4 or levels < 3.5:
                reasons.append("Boss前准备四名存活队员并达到平均3.5级")
            if upgraded < 3:
                reasons.append("Boss前需确认至少三人更换初始武器，不能用无关遗物数量代替输出成长")
            weakness = sum("虚弱" in c.get("description", "") and any(w in c.get("description", "") for w in ("友方", "我方")) for c in state.get("activeCalamities", []))
            if weakness and (levels < 4.5 or upgraded < len(party)):
                reasons.append("持续虚弱降低实际骰面，需要全员升级装备与更高等级补偿")
    elif encounter_type == "Battle":
        if len(party) < 2:
            reasons.append("先招募第二名队员")
        if health < .5:
            reasons.append("全队生命不足50%，先恢复")
    return {"ready": not reasons, "reasons": reasons, "livingParty": len(party), "healthRatio": health,
            "averageLevel": levels, "upgradedWeapons": upgraded, "relicCount": relics,
            "unknownWeaponBaselines": unknown}


def clash_preference(skill, opposing_skill):
    """Rank visible first-die matchups; the server still rolls and resolves rules."""
    own = skill.get("displayedDice") or skill.get("dice") or []
    enemy = opposing_skill.get("displayedDice") or opposing_skill.get("dice") or []
    if not own or not enemy:
        return 0
    a, b = own[0], enemy[0]
    margin = (a["min"] + a["max"] - b["min"] - b["max"]) / 2
    # A losing attack into a shield offers little pressure. Do not prefer it
    # merely because the defensive skill has many textual damage triggers.
    return margin - (1.5 if a["category"] == "Attack" and b["category"] == "Defense" and margin <= 0 else 0)


def calamity_risk(choice, state):
    """Visible-text preference for preserving combat strength, not a rules simulator."""
    text = (choice.get("name") or "") + " " + (choice.get("description") or "")
    # The engine now preserves the death deadline through healing. Evaluate
    # each calamity normally; do not retain the old bug-specific prohibition.
    if "遗物" in text and "失去" in text:
        relics = state.get("relics") or []
        if relics and all(r.get("name") == "小猪储蓄罐" for r in relics) and state.get("money", 0) >= 100:
            return 80
        return 1100 if relics else 5
    if "休息格" in text and "恢复" in text:
        return 5 if "血量" in text else 10
    if "商品价格" in text or "锻造" in text:
        return 2
    if "金币获取" in text:
        return 30
    if "伤害" in text and any(word in text for word in ("-30%", "减免", "减退")):
        return 1000
    if "虚弱" in text and any(word in text for word in ("友方", "我方")):
        return 1050
    if "敌人死亡时" in text:
        return 20
    if "抵抗" in text:
        return 900
    if any(word in text for word in ("强壮", "力量")):
        return 850
    if "重防" in text:
        return 800
    if "恢复" in text or "回复" in text:
        return 170
    if "生命" in text and "损失" in text and "战斗开始" in text:
        return 25
    if "血量" in text or "生命" in text:
        percent = re.search(r"(\d+)\s*%", text)
        return 50 + (int(percent.group(1)) * 4 if percent else 100)
    if "不屈" in text:
        return 100
    if "伤害" in text:
        return 220
    if "速度" in text:
        return 60
    if "能量" in text:
        return 130
    if "休息格" in text:
        return 100
    if "传送" in text or "伏击" in text:
        return 120
    if "行动点" in text:
        return 250
    return 150


def choose_calamity_action(state):
    actions = [a for a in state["legalActions"] if a["op"] == "run_choose_calamity"]
    return min(actions, key=lambda a: calamity_risk(state["calamityChoices"][a["choiceIndex"]], state), default=None)


def skill_value(skill, enemy_count=1, actor=None, incoming=False, enemies=None, charge_armor=False):
    """A preference score, not a damage or clash simulator."""
    dice = skill.get("displayedDice") or skill.get("dice") or []
    attack = sum((d["min"] + d["max"]) / 2 for d in dice if d["category"] == "Attack")
    description = skill.get("description") or ""
    extra = re.findall(r"造成\s*(\d+)\s*(?:点)?(?:额外)?伤害", description)
    self_harm = re.findall(r"对\s*自己\s*造成\s*(\d+)", description)
    bonus = max(0, sum(int(n) for n in extra) - sum(int(n) for n in self_harm)) * (enemy_count if "所有目标" in description else 1)
    setup = 0
    if actor is not None and (incoming or charge_armor):
        charge = sum(s["stacks"] for s in actor.get("statuses", []) if s["id"] == "energy")
        guaranteed_charge = any(re.search(r"\[\s*使用时\s*\].*自己.*获得\s*\d+\s*层\s*蓄能", line) for line in description.splitlines())
        if charge < 2 and guaranteed_charge:
            setup = 2.5 + .7 * sum((d["min"] + d["max"]) / 2 for d in dice if d["category"] == "Defense")
        if charge_armor and incoming and actor.get("maxHp", 0) > 0 and actor.get("hp", 0) <= actor["maxHp"] * .3:
            setup = max(setup, 2.5 * sum((d["min"] + d["max"]) / 2 for d in dice if d["category"] == "Defense"))
        multi_hit_incoming = incoming and any(len(s.get("selectedSkill", {}).get("dice", [])) > 1
            and s.get("targetId") == actor.get("id") for enemy in (enemies or []) for s in enemy.get("slots", []))
        if multi_hit_incoming and actor.get("hp", 0) <= actor.get("maxHp", 0) * .45 and not (charge_armor and charge >= 2):
            setup = max(setup, 2.8 * sum((d["min"] + d["max"]) / 2 for d in dice if d["category"] == "Defense"))
    burst = 0
    if enemies and "电流 爆发" in description:
        charges = [sum(s["stacks"] for s in e.get("statuses", []) if s["id"] == "current") for e in enemies]
        # Prefer an explicitly described burst when observable stacks have built up.
        # This is a bounded policy preference; the shared engine resolves the effect.
        burst = 2 * min(12, sum(charges) if "所有目标" in description else max(charges, default=0))
    front = .8 * (dice[0]["min"] + dice[0]["max"]) / 2 if incoming and dice else 0
    if charge_armor and actor is not None and actor.get("hp", 0) <= actor.get("maxHp", 0) * .3 and dice and dice[0]["category"] == "Attack":
        front = 0  # Preserve the existing emergency-defense preference.
    return attack + bonus + setup + burst + front - sum(int(n) for n in self_harm) * 1.5 - skill.get("energyCost", 0) * .12


def choose_battle_action(state):
    actions = state["legalActions"]
    battle = state["battle"]
    returned = next((a for a in actions if a["op"] == "run_return"), None)
    if returned:
        return returned
    inner = [a for a in actions if a["op"] == "run_battle"]
    if battle["phase"] == "reward":
        return choose_relic_action(state)
    if battle["phase"] != "targeting":
        return None
    enemies = {a["id"]: a for a in battle["actors"] if a["side"] == "enemy" and not a["dead"]}
    allies = [a for a in battle["actors"] if a["side"] == "ally" and not a["dead"]]
    charge_armor = any(relic.get("name") == "龙鳞甲" for relic in battle.get("relics", []))
    claimed = {(s["targetId"], s["targetButtonIndex"]) for a in allies for s in a["slots"] if s.get("targetId")}
    slots = []
    for actor in allies:
        skills = {s["index"]: s for s in actor["skills"]}
        for slot in actor["slots"]:
            incoming = any(s.get("targetId") == actor["id"] and s.get("targetButtonIndex") == slot["index"]
                           for e in enemies.values() for s in e["slots"])
            available = [a for a in inner if a["battleAction"]["op"] == "set_skill"
                         and a["battleAction"]["actorId"] == actor["id"] and a["battleAction"]["buttonIndex"] == slot["index"]]
            value = lambda skill: skill_value(skill, len(enemies), actor, incoming, list(enemies.values()), charge_armor)
            current_legal = any(a["battleAction"]["skillIndex"] == slot["skillIndex"] for a in available)
            if available and not current_legal:
                # Release an unaffordable carried-over selection before allocating this
                # round's shared energy. A targeted slot needs its final legal skill now.
                if slot.get("targetId"):
                    return max(available, key=lambda a: value(skills[a["battleAction"]["skillIndex"]]))
                return min(available, key=lambda a: (skills[a["battleAction"]["skillIndex"]].get("energyCost", 0),
                                                    -value(skills[a["battleAction"]["skillIndex"]])))
            slots.append((actor, slot, skills, available, incoming))

    def charge_cost(skill):
        text = skill.get("description") or ""
        required = re.search(r"自身至少拥有\s*(\d+)\s*层\s*蓄能", text)
        return int(required.group(1)) if required and "施放后消耗" in text else 0

    charges = {actor["id"]: sum(s["stacks"] for s in actor.get("statuses", []) if s["id"] == "energy") for actor in allies}
    charge_spend = {actor["id"]: sum(charge_cost(slot.get("selectedSkill") or {}) for slot in actor["slots"]) for actor in allies}
    if charge_armor:
        for actor, slot, skills, available, incoming in slots:
            current = slot.get("selectedSkill") or {}
            if slot.get("targetId") or not charge_cost(current) or charge_spend[actor["id"]] <= max(0, charges[actor["id"]] - 2):
                continue
            alternatives = [a for a in available if charge_cost(skills[a["battleAction"]["skillIndex"]]) == 0
                            and skills[a["battleAction"]["skillIndex"]].get("energyCost", 0) <= current.get("energyCost", 0)]
            if alternatives:
                return max(alternatives, key=lambda a: skill_value(skills[a["battleAction"]["skillIndex"]], len(enemies), actor, incoming, list(enemies.values()), charge_armor))

    budget = battle.get("energy", 0) + battle.get("temporaryEnergy", 0)
    selected_cost = sum((slot.get("selectedSkill") or {}).get("energyCost", 0) for _, slot, _, _, _ in slots)
    if selected_cost > budget:
        releases = []
        for actor, slot, skills, available, incoming in slots:
            if slot.get("targetId"):
                continue
            current = slot.get("selectedSkill") or {}
            value = lambda skill: skill_value(skill, len(enemies), actor, incoming, list(enemies.values()), charge_armor)
            for action in available:
                skill = skills[action["battleAction"]["skillIndex"]]
                saved = current.get("energyCost", 0) - skill.get("energyCost", 0)
                if saved > 0:
                    releases.append(((value(current) - value(skill)) / saved, -saved, action))
        if releases:
            return min(releases, key=lambda release: release[:2])[2]

    long_fight = len(enemies) == 1 and next(iter(enemies.values())).get("maxHp", 0) > 1.5 * max((a.get("maxHp", 0) for a in allies), default=0)
    minimum_efficiency = .75 if long_fight and budget < 5 else 0
    upgrades = []
    for actor, slot, skills, available, incoming in slots:
        if slot.get("targetId"):
            continue
        value = lambda skill: skill_value(skill, len(enemies), actor, incoming, list(enemies.values()), charge_armor)
        current = slot.get("selectedSkill") or {}
        for action in available:
            skill = skills[action["battleAction"]["skillIndex"]]
            if charge_armor and charge_spend[actor["id"]] - charge_cost(current) + charge_cost(skill) > max(0, charges[actor["id"]] - 2):
                continue
            gain = value(skill) - value(current)
            cost_change = skill.get("energyCost", 0) - current.get("energyCost", 0)
            if gain > .01 and selected_cost + cost_change <= budget:
                extra_cost = max(1, cost_change)
                if cost_change <= 0 or gain / extra_cost >= minimum_efficiency:
                    upgrades.append((gain / extra_cost, gain, action))
    if upgrades:
        # Allocate across the party before choosing targets, so the first actor does
        # not consume the entire shared budget on a small damage improvement.
        return max(upgrades, key=lambda upgrade: upgrade[:2])[2]

    def clash_front(entry):
        dice = (entry[1].get("selectedSkill") or {}).get("displayedDice") or (entry[1].get("selectedSkill") or {}).get("dice") or []
        if not dice:
            return 0
        first = dice[0]
        power = (first["min"] + first["max"]) / 2 + .25 * (first["max"] - first["min"])
        return power * (.9 if first["category"] == "Defense" else 1)

    regen_focus = len(enemies) > 1 and any("敌人" in c.get("description", "") and
        any(w in c.get("description", "") for w in ("恢复", "回复")) for c in state.get("activeCalamities", []))
    focus_target = min(enemies.values(), key=lambda e: (e["hp"], e["id"]))["id"] if regen_focus else None
    # A later fast attacker can take over a clash. Connect the weaker fronts
    # first so a fragile caster does not take it back from a stronger weapon.
    for actor, slot, _, _, _ in sorted(slots, key=clash_front):
        if slot.get("targetId"):
            continue
        targets = [a for a in inner if a["battleAction"]["op"] == "target"
                   and a["battleAction"]["actorId"] == actor["id"] and a["battleAction"]["buttonIndex"] == slot["index"]
                   and a["battleAction"]["targetId"] in enemies]
        can_redirect_all = all(slot["speed"] >= hostile["speed"] for enemy in enemies.values() for hostile in enemy["slots"])

        def priority(action):
            target = action["battleAction"]
            enemy = enemies[target["targetId"]]
            enemy_slot = next(s for s in enemy["slots"] if s["index"] == target["targetButtonIndex"])
            unclaimed = (enemy["id"], enemy_slot["index"]) not in claimed
            incoming = enemy_slot.get("targetId") == actor["id"] and enemy_slot.get("targetButtonIndex") == slot["index"]
            # Preserve reciprocal counters before redirecting another ally's incoming attack.
            intercepts = can_intercept(slot, enemy_slot, actor["id"])
            threat = skill_value(enemy_slot.get("selectedSkill") or {}, len(allies))
            if len(enemies) == 1 and len(actor["slots"]) > 1 and can_redirect_all:
                # Inherited actions belong to one health pool. Spread damage into
                # one opponent without making that unit clash every hostile slot.
                focused = [(s["targetId"], s["targetButtonIndex"]) for s in actor["slots"] if s.get("targetId")]
                focus = max(set(focused), key=lambda pair: (focused.count(pair), -pair[1]), default=None)
                return (not enemy.get("pendingDelayedDeath", False), (enemy["id"], enemy_slot["index"]) == focus, -threat, incoming, intercepts)
            matchup = clash_preference(slot.get("selectedSkill") or {}, enemy_slot.get("selectedSkill") or {})
            safe_redirect = not (not incoming and slot["speed"] >= enemy_slot["speed"]
                and len((enemy_slot.get("selectedSkill") or {}).get("dice", [])) > 1
                and actor.get("hp", 0) <= actor.get("maxHp", 0) * .45)
            if regen_focus:
                dangerous_counter = incoming and (len((enemy_slot.get("selectedSkill") or {}).get("dice", [])) > 1
                    or actor.get("hp", 0) <= actor.get("maxHp", 0) * .45)
                return (safe_redirect, dangerous_counter, not enemy.get("pendingDelayedDeath", False), enemy["id"] == focus_target, unclaimed and intercepts, incoming, matchup, -enemy["hp"])
            return (safe_redirect, unclaimed and incoming, not enemy.get("pendingDelayedDeath", False), unclaimed and intercepts, unclaimed, matchup, threat, -enemy["hp"])

        if targets:
            return max(targets, key=priority)
    return next((a for a in inner if a["battleAction"]["op"] == "commit"), None)
