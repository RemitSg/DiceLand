"""Regression decisions captured from an actual normal seed-127 Boss attempt."""
import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_adventure_policy import choose_battle_action


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


if __name__ == "__main__":
    unittest.main()
