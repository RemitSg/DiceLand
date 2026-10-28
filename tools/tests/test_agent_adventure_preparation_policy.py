"""Regressions for the actual first Easy loss; no engine outcomes are mocked."""
import copy
import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_adventure_policy import choose_treasure_action, preparation_assessment, next_recruit_cost, clash_preference, skill_value, choose_calamity_action, choose_battle_action, calamity_risk


class PreparationPolicyTests(unittest.TestCase):
    def setUp(self):
        self.f = json.loads((Path(__file__).parent / "fixtures/easy_preparation_policy.json").read_text(encoding="utf-8"))

    def kind(self, action, state):
        return next(o["kind"] for o in state["treasure"]["options"] if o["index"] == action["choiceIndex"])

    def test_real_61_gold_treasure_funds_the_90_gold_third_member(self):
        state = self.f["treasure"]
        self.assertEqual(90, next_recruit_cost(state, self.f["catalogue"]))
        action = choose_treasure_action(state, self.f["catalogue"])
        self.assertIn(action, state["legalActions"])
        self.assertEqual("Money", self.kind(action, state))

    def test_relic_bag_is_recognized_when_recruit_funds_are_already_available(self):
        state = copy.deepcopy(self.f["treasure"])
        state["money"] = 111
        action = choose_treasure_action(state, self.f["catalogue"])
        self.assertEqual("RelicBag", self.kind(action, state))

    def test_missing_recipe_materials_do_not_outweigh_a_relic_bag(self):
        state = copy.deepcopy(self.f["treasure"])
        state["money"] = 111
        self.assertFalse(state["loot"])
        self.assertNotEqual("Blueprint", self.kind(choose_treasure_action(state, self.f["catalogue"]), state))

    def test_empty_authored_recipe_is_not_assumed_to_be_free_equipment(self):
        state = copy.deepcopy(self.f["treasure"])
        state["money"] = 111
        catalogue = copy.deepcopy(self.f["catalogue"])
        catalogue["blueprintDetails"][0]["materials"] = []
        self.assertEqual("RelicBag", self.kind(choose_treasure_action(state, catalogue), state))

    def test_actual_two_starter_characters_are_not_ready_for_first_elite(self):
        result = preparation_assessment(self.f["underprepared"], "Elite", self.f["catalogue"])
        self.assertFalse(result["ready"])
        self.assertEqual(2, result["livingParty"])
        self.assertEqual(0, result["upgradedWeapons"])
        self.assertGreaterEqual(len(result["reasons"]), 3)

    def test_same_full_health_party_can_attempt_an_ordinary_encounter(self):
        self.assertTrue(preparation_assessment(self.f["underprepared"], "Battle", self.f["catalogue"])["ready"])

    def test_low_health_blocks_even_ordinary_preparation(self):
        state = copy.deepcopy(self.f["underprepared"])
        for member in state["party"]:
            member["hp"] = 1
        self.assertFalse(preparation_assessment(state, "Battle", self.f["catalogue"])["ready"])

    def test_losing_attack_into_shield_is_worse_than_equal_attack_range(self):
        attack = {"dice": [{"min": 2, "max": 4, "category": "Attack"}]}
        shield = {"dice": [{"min": 3, "max": 4, "category": "Defense"}]}
        hostile = {"dice": [{"min": 3, "max": 4, "category": "Attack"}]}
        self.assertLess(clash_preference(attack, shield), clash_preference(attack, hostile))

    def test_weak_front_hit_bonus_does_not_beat_a_stronger_counter(self):
        stab = {"dice": [{"min": 2, "max": 4, "category": "Attack"}], "energyCost": 0}
        backstab = {"dice": [{"min": 2, "max": 2, "category": "Attack"}], "energyCost": 2,
                    "description": "[ 命中时 ] 对 当前目标 造成 2 点伤害"}
        # The actual two-point on-hit bonus remains useful unopposed; it is not
        # worth trading away the front die against a stronger incoming attack.
        self.assertGreater(skill_value(backstab), skill_value(stab))
        self.assertGreater(skill_value(stab, incoming=True), skill_value(backstab, incoming=True))

    def test_preparation_and_reward_decisions_are_read_only_and_deterministic(self):
        before = json.dumps(self.f, sort_keys=True)
        self.assertEqual(choose_treasure_action(self.f["treasure"], self.f["catalogue"]),
                         choose_treasure_action(self.f["treasure"], self.f["catalogue"]))
        preparation_assessment(self.f["underprepared"], "Boss", self.f["catalogue"])
        self.assertEqual(before, json.dumps(self.f, sort_keys=True))

    def test_fixed_delayed_death_allows_regeneration_instead_of_more_enemy_hp(self):
        state = self.f["regenChoice"]
        self.assertTrue(any("不屈" in c["name"] for c in state["activeCalamities"]))
        action = choose_calamity_action(state)
        chosen = state["calamityChoices"][action["choiceIndex"]]
        self.assertIn("回复机体", chosen["name"])

    def test_fixed_regeneration_does_not_add_a_bug_specific_delayed_death_penalty(self):
        state = {"activeCalamities": [{"name": "回复机体", "description": "所有敌人额外恢复 2 点血量"}]}
        self.assertEqual(100, calamity_risk({"name": "劲敌 · 不屈", "description": "承受致死伤害时不死亡"}, state))

    def test_wounded_molly_defends_when_the_multi_hit_attack_targets_her(self):
        state = copy.deepcopy(self.f["woundedMultiHit"])
        # Counterfactual incoming intent, using the same real HP, skill offers
        # and dice: direct attacks should be defended, redirects avoided below.
        next(a for a in state["battle"]["actors"] if a["id"] == "enemy_0")["slots"][0]["targetId"] = "ally_1"
        state["legalActions"] = [a for a in state["legalActions"] if a["op"] == "run_battle" and
                                 a["battleAction"].get("actorId") == "ally_1"]
        action = choose_battle_action(state)
        self.assertIn(action, state["legalActions"])
        self.assertEqual(("set_skill", "ally_1", 2), tuple(action["battleAction"][k] for k in ("op", "actorId", "skillIndex")))

    def test_regenerating_adds_are_focused_without_losing_dangerous_counter(self):
        state = copy.deepcopy(self.f["woundedMultiHit"])
        enemies = [a for a in state["battle"]["actors"] if a["side"] == "enemy" and not a["dead"]]
        focus = min(enemies, key=lambda a: (a["hp"], a["id"]))["id"]
        state["legalActions"] = [a for a in state["legalActions"] if a["op"] == "run_battle" and
                                 a["battleAction"]["op"] == "target" and a["battleAction"]["actorId"] == "ally_0"]
        self.assertEqual(focus, choose_battle_action(state)["battleAction"]["targetId"])
        state = copy.deepcopy(self.f["woundedMultiHit"])
        state["legalActions"] = [a for a in state["legalActions"] if a["op"] == "run_battle" and
                                 a["battleAction"]["op"] == "target" and a["battleAction"]["actorId"] == "ally_1"]
        # In the real frame the giant targets Chara, not wounded Molly. She
        # must not use her higher speed to steal that dangerous multi-hit clash.
        self.assertNotEqual("enemy_0", choose_battle_action(state)["battleAction"]["targetId"])


if __name__ == "__main__":
    unittest.main()
