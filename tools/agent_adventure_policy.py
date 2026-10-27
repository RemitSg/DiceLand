"""Visible-state baseline decisions; combat outcomes are always executed by the server."""
import re


def skill_value(skill, enemy_count=1):
    """A preference score, not a damage or clash simulator."""
    dice = skill.get("displayedDice") or skill.get("dice") or []
    attack = sum((d["min"] + d["max"]) / 2 for d in dice if d["category"] == "Attack")
    description = skill.get("description") or ""
    extra = re.findall(r"造成\s*(\d+)\s*(?:点)?(?:额外)?伤害", description)
    self_harm = re.findall(r"对\s*自己\s*造成\s*(\d+)", description)
    bonus = max(0, sum(int(n) for n in extra) - sum(int(n) for n in self_harm)) * (enemy_count if "所有目标" in description else 1)
    return attack + bonus - sum(int(n) for n in self_harm) * 1.5 - skill.get("energyCost", 0) * .12


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
            available = [a for a in inner if a["battleAction"]["op"] == "set_skill"
                         and a["battleAction"]["actorId"] == actor["id"] and a["battleAction"]["buttonIndex"] == slot["index"]]
            best = max(available, key=lambda a: skill_value(skills[a["battleAction"]["skillIndex"]], len(enemies)), default=None)
            current_legal = any(a["battleAction"]["skillIndex"] == slot["skillIndex"] for a in available)
            if best and (not current_legal or skill_value(skills[best["battleAction"]["skillIndex"]], len(enemies))
                         > skill_value(slot["selectedSkill"], len(enemies)) + .01):
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
