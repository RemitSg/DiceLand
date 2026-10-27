"""Visible-state baseline decisions; combat outcomes are always executed by the server."""
import re


def calamity_risk(choice, state):
    """Visible-text preference for preserving combat strength, not a rules simulator."""
    text = (choice.get("name") or "") + " " + (choice.get("description") or "")
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
    if "伤害" in text:
        return 220
    if "不屈" in text:
        return 100
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
    burst = 0
    if enemies and "电流 爆发" in description:
        charges = [sum(s["stacks"] for s in e.get("statuses", []) if s["id"] == "current") for e in enemies]
        # Prefer an explicitly described burst when observable stacks have built up.
        # This is a bounded policy preference; the shared engine resolves the effect.
        burst = 2 * min(12, sum(charges) if "所有目标" in description else max(charges, default=0))
    return attack + bonus + setup + burst - sum(int(n) for n in self_harm) * 1.5 - skill.get("energyCost", 0) * .12


def choose_battle_action(state):
    actions = state["legalActions"]
    battle = state["battle"]
    returned = next((a for a in actions if a["op"] == "run_return"), None)
    if returned:
        return returned
    inner = [a for a in actions if a["op"] == "run_battle"]
    if battle["phase"] == "reward":
        return next((a for a in inner if a["battleAction"]["op"] == "choose_relic"), None)
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

    # A later fast attacker can take over a clash. Connect the weaker fronts
    # first so a fragile caster does not take it back from a stronger weapon.
    for actor, slot, _, _, _ in sorted(slots, key=clash_front):
        if slot.get("targetId"):
            continue
        targets = [a for a in inner if a["battleAction"]["op"] == "target"
                   and a["battleAction"]["actorId"] == actor["id"] and a["battleAction"]["buttonIndex"] == slot["index"]
                   and a["battleAction"]["targetId"] in enemies]
        can_redirect_all = all(slot["speed"] > hostile["speed"] for enemy in enemies.values() for hostile in enemy["slots"])

        def priority(action):
            target = action["battleAction"]
            enemy = enemies[target["targetId"]]
            enemy_slot = next(s for s in enemy["slots"] if s["index"] == target["targetButtonIndex"])
            unclaimed = (enemy["id"], enemy_slot["index"]) not in claimed
            incoming = enemy_slot.get("targetId") == actor["id"] and enemy_slot.get("targetButtonIndex") == slot["index"]
            # Preserve reciprocal counters before redirecting another ally's incoming attack.
            can_intercept = incoming or slot["speed"] > enemy_slot["speed"]
            threat = skill_value(enemy_slot.get("selectedSkill") or {}, len(allies))
            if len(enemies) == 1 and len(actor["slots"]) > 1 and can_redirect_all:
                # Inherited actions belong to one health pool. Spread damage into
                # one opponent without making that unit clash every hostile slot.
                focused = [(s["targetId"], s["targetButtonIndex"]) for s in actor["slots"] if s.get("targetId")]
                focus = max(set(focused), key=lambda pair: (focused.count(pair), -pair[1]), default=None)
                return ((enemy["id"], enemy_slot["index"]) == focus, -threat, incoming, can_intercept)
            return (unclaimed and incoming, unclaimed and can_intercept, unclaimed, threat, -enemy["hp"])

        if targets:
            return max(targets, key=priority)
    return next((a for a in inner if a["battleAction"]["op"] == "commit"), None)
