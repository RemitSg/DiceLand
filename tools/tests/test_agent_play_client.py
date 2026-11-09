"""Manual execution must not choose candidates or mint a new ID after uncertain writes."""
import copy
import json
from pathlib import Path
import sys
import tempfile
import unittest
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_play_client import ManualSession, select_offered


class FakeClient:
    def __init__(self):
        self.state = {"sessionId": "s", "revision": 3, "phase": "map", "legalActions": [
            {"op": "run_move", "x": 1, "y": 1}, {"op": "run_move", "x": 1, "y": 0},
            {"op": "run_battle", "battleAction": {"op": "target", "actorId": "ally_3", "buttonIndex": 2,
                                                   "targetId": "enemy_7", "targetButtonIndex": 1}}]}
        self.calls = []
        self.fail_after_commit = False
        self.cached = None

    def call(self, **request):
        self.calls.append(copy.deepcopy(request))
        if request["op"] == "observe_run":
            return {"state": copy.deepcopy(self.state)}
        if request["op"] == "catalog":
            return {"catalogue": {"relics": [{"id": "guid", "name": "English name"}]}}
        if request["op"] == "run_export":
            return {"replay": {"commands": []}}
        if self.cached is None:
            self.state["revision"] += 1
            self.cached = {"state": copy.deepcopy(self.state)}
            if self.fail_after_commit:
                raise TimeoutError("response lost after commit")
        return copy.deepcopy(self.cached)


class ManualPlayTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.client = FakeClient()
        self.session = ManualSession.attach(self.client, self.directory.name, "s")

    def test_attach_is_read_only_and_never_overwrites_existing_logs(self):
        self.assertEqual(["observe_run", "catalog"], [r["op"] for r in self.client.calls])
        with self.assertRaises(ValueError):
            ManualSession.attach(self.client, self.directory.name, "other")

    def test_diagonal_action_is_matched_without_cardinal_route_assumption(self):
        before = copy.deepcopy(self.client.state)
        result = select_offered(before, {"op": "run_move", "x": 1, "y": 1})
        self.assertEqual(result, before["legalActions"][0])
        result["x"] = 99
        self.assertEqual(before, self.client.state)

    def test_ambiguous_invalid_and_transport_identity_selectors_do_not_write(self):
        for selector in ({"op": "run_move"}, {"op": "run_move", "x": 99}, {"op": "run_move", "sessionId": "other"}):
            with self.subTest(selector=selector), self.assertRaises(ValueError):
                self.session.act(selector, "Explicit model rationale")
        self.assertFalse(any("requestId" in r for r in self.client.calls))

    def test_nested_target_retains_inherited_slot_and_logs_before_response(self):
        wanted = {"op": "run_battle", "battleAction": {"op": "target", "actorId": "ally_3", "buttonIndex": 2,
                                                       "targetId": "enemy_7", "targetButtonIndex": 1}}
        self.session.act(wanted, "Protect this ally using the inherited slot")
        request = next(r for r in self.client.calls if "requestId" in r)
        self.assertEqual(wanted["battleAction"], request["battleAction"])
        self.assertEqual(3, request["revision"])
        rows = [json.loads(x) for x in (Path(self.directory.name) / "observations.jsonl").read_text(encoding="utf-8").splitlines()]
        self.assertLess(next(i for i, r in enumerate(rows) if r["kind"] == "request"),
                        next(i for i, r in enumerate(rows) if r["kind"] == "response"))

    def test_missing_rationale_and_default_seed_next_run_are_rejected(self):
        with self.assertRaises(ValueError):
            self.session.act({"op": "run_move", "x": 1, "y": 0}, " ")
        self.client.state["legalActions"] = [{"op": "run_next"}]
        with self.assertRaises(ValueError):
            self.session.act({"op": "run_next"}, "Another game")
        self.assertFalse(any("requestId" in r for r in self.client.calls))

    def test_lost_response_persists_exact_request_and_blocks_another_action(self):
        self.client.fail_after_commit = True
        with self.assertRaises(TimeoutError):
            self.session.act({"op": "run_move", "x": 1, "y": 1}, "Take the offered diagonal")
        restored = ManualSession(self.client, self.directory.name)
        original = copy.deepcopy(restored.record["pending"]["request"])
        with self.assertRaises(ValueError):
            restored.act({"op": "run_move", "x": 1, "y": 0}, "Different action")
        with self.assertRaises(ValueError):
            restored.export("unfinished")
        self.client.state["revision"] = 6  # Cache is older than current observation.
        state = restored.retry()
        self.assertEqual([original, original], [r for r in self.client.calls if "requestId" in r])
        self.assertEqual(6, state["revision"])
        self.assertIsNone(restored.record["pending"])

    def test_observe_retains_dead_actors_and_unsettled_outer_party(self):
        self.client.state.update(phase="battle", party=[{"id": "p", "hp": 20}], battle={"actors": [
            {"id": "ally_0", "hp": 0, "dead": True}, {"id": "ally_5", "hp": 7, "dead": False}]})
        state = self.session.observe()
        self.assertEqual(self.client.state, state)
        self.assertEqual(0, state["battle"]["actors"][0]["hp"])
        self.assertEqual(20, state["party"][0]["hp"])

    def test_export_does_not_close_settle_or_launch_a_run(self):
        self.session.export("verify-later")
        self.assertEqual("run_export", self.client.calls[-1]["op"])
        self.assertTrue((Path(self.directory.name) / "replay.json").exists())


if __name__ == "__main__":
    unittest.main()
