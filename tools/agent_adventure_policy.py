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


def skill_value(skill, enemy_count=1, actor=None, incoming=False):
    """A preference score, not a damage or clash simulator."""
    dice = skill.get("displayedDice") or skill.get("dice") or []
    attack = sum((d["min"] + d["max"]) / 2 for d in dice if d["category"] == "Attack")
    description = skill.get("description") or ""
    extra = re.findall(r"造成\s*(\d+)\s*(?:点)?(?:额外)?伤害", description)
    self_harm = re.findall(r"对\s*自己\s*造成\s*(\d+)", description)
    bonus = max(0, sum(int(n) for n in extra) - sum(int(n) for n in self_harm)) * (enemy_count if "所有目标" in description else 1)
    setup = 0
    if actor is not None and incoming:
        charge = sum(s["stacks"] for s in actor.get("statuses", []) if s["id"] == "energy")
        guaranteed_charge = any(re.search(r"\[\s*使用时\s*\].*自己.*获得\s*\d+\s*层\s*蓄能", line) for line in description.splitlines())
        if charge < 2 and guaranteed_charge:
            setup = 2.5 + .7 * sum((d["min"] + d["max"]) / 2 for d in dice if d["category"] == "Defense")
    return attack + bonus + setup - sum(int(n) for n in self_harm) * 1.5 - skill.get("energyCost", 0) * .12


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
    claimed = {(s["targetId"], s["targetButtonIndex"]) for a in allies for s in a["slots"] if s.get("targetId")}
    for actor in allies:
        skills = {s["index"]: s for s in actor["skills"]}
        for slot in actor["slots"]:
            incoming = any(s.get("targetId") == actor["id"] and s.get("targetButtonIndex") == slot["index"]
                           for e in enemies.values() for s in e["slots"])
            available = [a for a in inner if a["battleAction"]["op"] == "set_skill"
                         and a["battleAction"]["actorId"] == actor["id"] and a["battleAction"]["buttonIndex"] == slot["index"]]
            value = lambda skill: skill_value(skill, len(enemies), actor, incoming)
            best = max(available, key=lambda a: value(skills[a["battleAction"]["skillIndex"]]), default=None)
            current_legal = any(a["battleAction"]["skillIndex"] == slot["skillIndex"] for a in available)
            if best and (not current_legal or (not slot.get("targetId") and value(skills[best["battleAction"]["skillIndex"]])
                                             > value(slot["selectedSkill"]) + .01)):
                return best
            if slot.get("targetId"):
                continue
            targets = [a for a in inner if a["battleAction"]["op"] == "target"
                       and a["battleAction"]["actorId"] == actor["id"] and a["battleAction"]["buttonIndex"] == slot["index"]
                       and a["battleAction"]["targetId"] in enemies]

            def priority(action):
                target = action["battleAction"]
                enemy = enemies[target["targetId"]]
                enemy_slot = next(s for s in enemy["slots"] if s["index"] == target["targetButtonIndex"])
                unclaimed = (enemy["id"], enemy_slot["index"]) not in claimed
                incoming = enemy_slot.get("targetId") == actor["id"] and enemy_slot.get("targetButtonIndex") == slot["index"]
                # Preserve reciprocal counters before redirecting another ally's incoming attack.
                can_intercept = incoming or slot["speed"] > enemy_slot["speed"]
                threat = skill_value(enemy_slot.get("selectedSkill") or {}, len(allies))
                return (unclaimed and incoming, unclaimed and can_intercept, unclaimed, threat, -enemy["hp"])

            if targets:
                return max(targets, key=priority)
    return next((a for a in inner if a["battleAction"]["op"] == "commit"), None)
