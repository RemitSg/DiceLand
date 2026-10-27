"""Regression decisions captured from an actual normal seed-127 Boss attempt."""
import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_adventure_policy import choose_battle_action, choose_calamity_action, skill_value


class AdventurePolicyTests(unittest.TestCase):
    def setUp(self):
        self.frames = json.loads((Path(__file__).parent / "fixtures" / "normal_boss_policy.json").read_text(encoding="utf-8"))["frames"]

    def test_unaffordable_persisted_skill_is_replaced_before_commit(self):
        state = self.frames["unaffordable_persisted_skill"]
        result = choose_battle_action(state)
        self.assertIn(result, state["legalActions"])
        action = result["battleAction"]
        self.assertEqual((action["op"], action["actorId"], action["skillIndex"]), ("set_skill", "ally_2", 0))

    def test_counter_own_incoming_attack_instead_of_redirecting_a_teammates(self):
        state = self.frames["reciprocal_counter"]
        action = choose_battle_action(state)["battleAction"]
        self.assertEqual((action["op"], action["actorId"], action["targetButtonIndex"]), ("target", "ally_0", 1))

    def test_avoid_weak_self_damage_skill_when_free_attack_is_available(self):
        state = self.frames["self_harm_choice"]
        action = choose_battle_action(state)["battleAction"]
        self.assertEqual((action["op"], action["actorId"]), ("target", "ally_2"))
        actor = next(a for a in state["battle"]["actors"] if a["id"] == "ally_2")
        self.assertEqual(actor["slots"][0]["skillIndex"], 0)

    def test_deciding_does_not_mutate_or_randomize_observation(self):
        for state in self.frames.values():
            before = json.dumps(state, ensure_ascii=False)
            first = choose_battle_action(state)
            self.assertEqual(first, choose_battle_action(state))
            self.assertEqual(before, json.dumps(state, ensure_ascii=False))
            self.assertIn(first, state["legalActions"])

    def test_faulted_battle_has_no_invented_play_action(self):
        state = {"battle": {"phase": "faulted"}, "legalActions": [{"op": "run_abandon"}]}
        self.assertIsNone(choose_battle_action(state))

    def test_return_and_reward_follow_offered_legal_actions(self):
        for phase, action in (("terminal", {"op": "run_return"}),
                              ("reward", {"op": "run_battle", "battleAction": {"op": "choose_relic", "choiceIndex": 2}})):
            state = {"battle": {"phase": phase}, "legalActions": [action]}
            self.assertEqual(action, choose_battle_action(state))


class CalamityPolicyTests(unittest.TestCase):
    def choose(self, descriptions, relics=None, legal=None, money=0):
        state = {"calamityChoices": [{"name": "", "description": d} for d in descriptions],
                 "money": money,
                 "relics": relics or [], "legalActions": legal if legal is not None else
                 [{"op": "run_choose_calamity", "choiceIndex": i} for i in range(len(descriptions))]}
        before = json.dumps(state, sort_keys=True)
        action = choose_calamity_action(state)
        self.assertEqual(before, json.dumps(state, sort_keys=True))
        return action

    def test_rest_heal_penalty_is_preferred_to_enemy_damage(self):
        action = self.choose(["所有敌人伤害额外增加 20%", "在休息格恢复血量时，效果变为恢复友方角色最大生命值的 70%"])
        self.assertEqual(1, action["choiceIndex"])

    def test_enemy_regeneration_is_preferred_to_permanent_ally_weakness(self):
        action = self.choose(["回合结束时，所有敌人额外恢复 2 点血量。", "战斗开始时，所有友方角色获得 1 层虚弱。"])
        self.assertEqual(0, action["choiceIndex"])

    def test_battle_start_health_loss_is_preferred_to_large_damage_boost(self):
        action = self.choose(["所有敌人造成伤害额外增加 40%。", "战斗开始时，所有友方角色损失 2 生命值。"])
        self.assertEqual(1, action["choiceIndex"])

    def test_owned_economy_relic_is_preserved_instead_of_discarded(self):
        action = self.choose(["随机失去 1 个遗物。", "战斗开始时，失去 2 能量"], [{"name": "小猪储蓄罐"}])
        self.assertEqual(1, action["choiceIndex"])

    def test_funded_piggy_run_can_sacrifice_income_instead_of_enemy_strength(self):
        action = self.choose(["所有敌人开局获得 1 层力量", "随机失去 3 个遗物。"], [{"name": "小猪储蓄罐"}], money=150)
        self.assertEqual(1, action["choiceIndex"])

    def test_only_offered_actions_are_selected(self):
        offered = [{"op": "run_choose_calamity", "choiceIndex": 1}]
        self.assertEqual(offered[0], self.choose(["商店中的商品价格额外提升 5%", "所有敌人伤害额外增加 20%"], legal=offered))
        self.assertIsNone(self.choose([], legal=[]))


class ChargePolicyTests(unittest.TestCase):
    def setUp(self):
        self.state = json.loads((Path(__file__).parent / "fixtures" / "normal_charge_policy.json").read_text(encoding="utf-8"))["state"]
        self.mage = next(a for a in self.state["battle"]["actors"] if any(s["name"] == "随意防御" for s in a["skills"]))
        self.defense = next(s for s in self.mage["skills"] if s["name"] == "随意防御")
        self.attack = next(s for s in self.mage["skills"] if s["name"] == "随意施法")

    def test_guaranteed_charge_defense_is_valued_when_under_attack(self):
        self.assertGreater(skill_value(self.defense, 2, self.mage, True), skill_value(self.attack, 2, self.mage, True))
        self.assertLess(skill_value(self.defense, 2, self.mage, False), skill_value(self.attack, 2, self.mage, False))

    def test_caster_setup_still_selects_an_actual_offered_action(self):
        # Focus the decision on this actor using a subset of the recorded legal actions.
        self.state["legalActions"] = [a for a in self.state["legalActions"] if a["battleAction"].get("actorId") == self.mage["id"]]
        action = choose_battle_action(self.state)
        self.assertIn(action, self.state["legalActions"])
        self.assertEqual("set_skill", action["battleAction"]["op"])
        self.assertEqual(self.defense["index"], action["battleAction"]["skillIndex"])


if __name__ == "__main__":
    unittest.main()
