"""Decisions from actual inherited slots and charge-based damage reduction."""
import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_adventure_policy import choose_battle_action


class InheritedAndChargePolicyTests(unittest.TestCase):
    def setUp(self):
        self.frames = json.loads((Path(__file__).parent / "fixtures" / "inherited_charge_policy.json").read_text(encoding="utf-8"))["frames"]

    def test_inherited_actions_focus_the_existing_button_instead_of_new_clashes(self):
        state = self.frames["partialInherited"]
        actor = next(a for a in state["battle"]["actors"] if a["side"] == "ally" and not a["dead"])
        self.assertEqual(4, len(actor["slots"]))
        focused = {(s["targetId"], s["targetButtonIndex"]) for s in actor["slots"] if s.get("targetId")}
        self.assertEqual(1, len(focused))
        action = choose_battle_action(state)
        self.assertIn(action, state["legalActions"])
        choice = action["battleAction"]
        self.assertEqual("target", choice["op"])
        self.assertIn((choice["targetId"], choice["targetButtonIndex"]), focused)

    def test_dragon_armor_does_not_spend_its_only_two_charges(self):
        state = self.frames["dragonTwoCharges"]
        mage = next(a for a in state["battle"]["actors"] if a["name"].startswith("Jenny"))
        self.assertEqual(2, sum(s["stacks"] for s in mage["statuses"] if s["id"] == "energy"))
        self.assertTrue(any(r["name"] == "龙鳞甲" for r in state["battle"]["relics"]))
        # Scope the decision to the caster's actual offered skills.
        state["legalActions"] = [a for a in state["legalActions"] if a["battleAction"]["op"] == "commit" or
                                 (a["battleAction"]["op"] == "set_skill" and a["battleAction"]["actorId"] == mage["id"])]
        self.assertTrue(any(a["battleAction"].get("skillIndex") == 2 for a in state["legalActions"] if a["battleAction"]["op"] == "set_skill"))
        action = choose_battle_action(state)["battleAction"]
        self.assertFalse(action["op"] == "set_skill" and action["skillIndex"] == 2)

    def test_four_charges_allow_a_star_cast_while_retaining_armor(self):
        state = self.frames["dragonCastWithReserve"]
        mage = next(a for a in state["battle"]["actors"] if a["name"].startswith("Jenny"))
        self.assertEqual(4, sum(s["stacks"] for s in mage["statuses"] if s["id"] == "energy"))
        action = choose_battle_action(state)
        self.assertIn(action, state["legalActions"])
        self.assertEqual(("set_skill", mage["id"], 2), tuple(action["battleAction"][k] for k in ("op", "actorId", "skillIndex")))

    def test_carried_star_is_released_when_it_would_remove_charge_protection(self):
        state = self.frames["dragonCarriedStar"]
        mage = next(a for a in state["battle"]["actors"] if a["name"].startswith("Jenny"))
        self.assertEqual(2, sum(s["stacks"] for s in mage["statuses"] if s["id"] == "energy"))
        self.assertEqual("星辰冲击", mage["slots"][0]["selectedSkill"]["name"])
        action = choose_battle_action(state)
        self.assertIn(action, state["legalActions"])
        self.assertEqual(("set_skill", mage["id"], 0), tuple(action["battleAction"][k] for k in ("op", "actorId", "skillIndex")))

    def test_low_health_armored_caster_prepares_defense_for_incoming_attacks(self):
        state = self.frames["slowLowHpOpening"]
        mage = next(a for a in state["battle"]["actors"] if a["side"] == "ally" and not a["dead"])
        self.assertEqual(3, mage["hp"])
        action = choose_battle_action(state)
        self.assertIn(action, state["legalActions"])
        self.assertEqual(("set_skill", mage["id"], 0, 1), tuple(action["battleAction"][k] for k in ("op", "actorId", "buttonIndex", "skillIndex")))

    def test_slow_inherited_caster_counters_actual_incoming_slots(self):
        first = choose_battle_action(self.frames["slowPrepared"])["battleAction"]
        second = choose_battle_action(self.frames["slowAfterFirstTarget"])["battleAction"]
        self.assertEqual(("target", 0, 1), tuple(first[k] for k in ("op", "buttonIndex", "targetButtonIndex")))
        self.assertEqual(("target", 1, 2), tuple(second[k] for k in ("op", "buttonIndex", "targetButtonIndex")))

    def test_real_frames_remain_read_only_deterministic_and_legal(self):
        for state in self.frames.values():
            before = json.dumps(state, sort_keys=True)
            action = choose_battle_action(state)
            self.assertIn(action, state["legalActions"])
            self.assertEqual(action, choose_battle_action(state))
            self.assertEqual(before, json.dumps(state, sort_keys=True))


if __name__ == "__main__":
    unittest.main()
