"""Party decisions from real opening and exhausted-energy Boss observations."""
import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_adventure_policy import choose_battle_action, skill_value


class SharedEnergyPolicyTests(unittest.TestCase):
    def setUp(self):
        self.frames = json.loads((Path(__file__).parent / "fixtures" / "shared_energy_policy.json").read_text(encoding="utf-8"))["frames"]

    def test_high_value_field_is_selected_before_small_sword_upgrade(self):
        state = self.frames["opening"]
        action = choose_battle_action(state)
        self.assertIn(action, state["legalActions"])
        self.assertEqual(("set_skill", "ally_2", 1), tuple(action["battleAction"][k] for k in ("op", "actorId", "skillIndex")))

    def test_carried_selections_release_low_value_cost_before_targeting(self):
        state = self.frames["exhausted"]
        self.assertEqual(3, state["battle"]["energy"])
        action = choose_battle_action(state)
        self.assertIn(action, state["legalActions"])
        self.assertEqual(("set_skill", "ally_1", 0), tuple(action["battleAction"][k] for k in ("op", "actorId", "skillIndex")))

    def test_burst_is_valued_after_real_observed_charge_build_up(self):
        state = self.frames["exhausted"]
        actors = state["battle"]["actors"]
        enemies = [a for a in actors if a["side"] == "enemy" and not a["dead"]]
        man = next(a for a in actors if a["id"] == "ally_2")
        field, burst = (next(s for s in man["skills"] if s["name"] == name) for name in ("电场", "磁暴"))
        self.assertGreater(skill_value(burst, enemies=enemies), skill_value(field, enemies=enemies))

    def test_zero_enemy_charge_does_not_receive_burst_preference(self):
        state = self.frames["opening"]
        enemies = [a for a in state["battle"]["actors"] if a["side"] == "enemy"]
        man = next(a for a in state["battle"]["actors"] if a["id"] == "ally_2")
        field, burst = (next(s for s in man["skills"] if s["name"] == name) for name in ("电场", "磁暴"))
        self.assertLess(skill_value(burst, enemies=enemies), skill_value(field, enemies=enemies))

    def test_valuable_but_unaffordable_burst_is_never_invented(self):
        state = self.frames["exhausted"]
        self.assertFalse(any(a.get("battleAction", {}).get("actorId") == "ally_2" and
                             a["battleAction"]["op"] == "set_skill" and a["battleAction"]["skillIndex"] == 2 for a in state["legalActions"]))
        self.assertIn(choose_battle_action(state), state["legalActions"])

    def test_long_fight_preserves_energy_instead_of_small_paid_sword_gain(self):
        state = self.frames["opening"]
        # Scope this decision to the sword's offered skills and commit, freezing
        # other choices so the conservation preference can be checked directly.
        state["legalActions"] = [a for a in state["legalActions"] if a["battleAction"]["op"] == "commit" or
                                 (a["battleAction"]["op"] == "set_skill" and a["battleAction"]["actorId"] == "ally_0")]
        self.assertEqual("commit", choose_battle_action(state)["battleAction"]["op"])

    def test_weak_caster_connects_before_stronger_fronts(self):
        state = self.frames["opening"]
        # All opening selected skills are free and legal. Freeze skill changes
        # to test the order in which these actual target commands are offered.
        state["legalActions"] = [a for a in state["legalActions"] if a["battleAction"]["op"] == "target"]
        self.assertEqual("ally_3", choose_battle_action(state)["battleAction"]["actorId"])

    def test_policy_is_deterministic_read_only_and_always_uses_offered_actions(self):
        for state in self.frames.values():
            before = json.dumps(state, sort_keys=True)
            action = choose_battle_action(state)
            self.assertEqual(action, choose_battle_action(state))
            self.assertIn(action, state["legalActions"])
            self.assertEqual(before, json.dumps(state, sort_keys=True))


if __name__ == "__main__":
    unittest.main()
