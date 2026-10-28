import copy
import json
from pathlib import Path
import sys
import unittest
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_adventure_policy import can_intercept, crafting_plan, preparation_assessment, choose_battle_action, choose_relic_action, relic_value, choose_material_purchase


class Run4PolicyTests(unittest.TestCase):
    def test_equal_speed_can_intercept_and_slower_reciprocal_can_counter(self):
        own = dict(index=0, speed=3)
        self.assertTrue(can_intercept(own, dict(speed=3, targetId="other", targetButtonIndex=0), "a"))
        self.assertFalse(can_intercept(own, dict(speed=4, targetId="other", targetButtonIndex=0), "a"))
        self.assertTrue(can_intercept(own, dict(speed=4, targetId="a", targetButtonIndex=0), "a"))

    def test_equal_speed_wounded_molly_does_not_steal_the_multi_hit(self):
        f = json.loads((Path(__file__).parent / "fixtures/easy_preparation_policy.json").read_text(encoding="utf-8"))
        s = copy.deepcopy(f["woundedMultiHit"])
        molly = next(a for a in s["battle"]["actors"] if a["id"] == "ally_1")
        giant = next(a for a in s["battle"]["actors"] if a["id"] == "enemy_0")
        molly["slots"][0]["speed"] = giant["slots"][0]["speed"]
        s["legalActions"] = [a for a in s["legalActions"] if a["op"] == "run_battle" and a["battleAction"]["op"] == "target" and a["battleAction"]["actorId"] == "ally_1"]
        self.assertNotEqual("enemy_0", choose_battle_action(s)["battleAction"]["targetId"])

    def recipe(self):
        return {"blueprintDetails": [{"id": "axe", "name": "Axe recipe", "weapon": "weapon", "materials": [
            dict(id="crystal", name="Crystal", amount=2), dict(id="wing", name="Wing", amount=1), dict(id="iron", name="Iron", amount=1)]}]}

    def test_material_plan_includes_the_previously_missed_wing(self):
        s = dict(money=10, craftingCost=4, loot=[dict(id="crystal", amount=2), dict(id="iron", amount=1)])
        before = copy.deepcopy(s)
        plan = crafting_plan(s, "axe", self.recipe())
        self.assertEqual([("wing", 1)], [(m["id"], m["missing"]) for m in plan["missingMaterials"]])
        self.assertFalse(plan["materialsReady"])
        self.assertEqual(before, s)

    def test_forge_can_craft_and_cost_override_catalog_estimate(self):
        s = dict(money=2, craftingCost=0, recipes=[dict(id="axe", cost=7, canCraft=False,
            materials=[dict(id="wing", name="Wing", required=1, available=1)])])
        plan = crafting_plan(s, "axe", self.recipe())
        self.assertEqual(5, plan["moneyShortfall"])
        self.assertTrue(plan["materialsReady"])
        self.assertFalse(plan["canCraft"])

    def test_empty_recipe_is_not_a_free_weapon(self):
        cat = self.recipe(); cat["blueprintDetails"][0]["materials"] = []
        self.assertFalse(crafting_plan({}, "axe", cat)["validRecipe"])

    def test_shopping_buys_missing_wing_and_keeps_forge_budget(self):
        s = dict(money=15, craftingCost=4, loot=[dict(id="crystal", amount=2), dict(id="iron", amount=1)],
            merchantOffers=[dict(index=0, id="trash", price=1, kind="Loot", canPurchase=True, soldOut=False),
                            dict(index=1, id="wing", price=11, kind="Loot", canPurchase=True, soldOut=False)],
            legalActions=[dict(op="run_buy", choiceIndex=i) for i in range(2)])
        self.assertEqual(1, choose_material_purchase(s, "axe", self.recipe())["choiceIndex"])
        s["money"] = 14
        self.assertIsNone(choose_material_purchase(s, "axe", self.recipe()))

    def test_boss_gate_checks_equipment_and_persistent_weakness(self):
        cat = {"characterDetails": [dict(id="c", initialWeapon="starter")]}
        s = {"party": [dict(character="c", weapon="upgrade" if i < 3 else "starter", hp=20, maxHp=20, level=4) for i in range(4)], "relics": [{}, {}, {}]}
        self.assertTrue(preparation_assessment(s, "Boss", cat)["ready"])
        s["party"][2]["weapon"] = "starter"
        self.assertFalse(preparation_assessment(s, "Boss", cat)["ready"])
        s["party"][2]["weapon"] = "upgrade"
        s["activeCalamities"] = [dict(description="战斗开始所有友方获得1层虚弱")]
        self.assertFalse(preparation_assessment(s, "Boss", cat)["ready"])

    def test_reward_compares_actual_effects_instead_of_first_index(self):
        choices = [dict(index=0, name="Bottle", description="特殊副本中，战斗后恢复生命"),
                   dict(index=1, name="Ash", description="回合结束时所有敌人获得1层燃烧")]
        s = {"phase": "battle", "battle": {"rewards": {"choices": choices}}, "legalActions": [
            dict(op="run_battle", battleAction=dict(op="choose_relic", choiceIndex=i)) for i in range(2)]}
        self.assertEqual(1, choose_relic_action(s)["battleAction"]["choiceIndex"])
        self.assertLess(relic_value(choices[0], s), relic_value(choices[0], dict(dungeon=dict(active=True))))

if __name__ == "__main__": unittest.main()
