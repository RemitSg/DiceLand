"""Difficulty lookup must only select IDs offered by the running server."""
import copy
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_adventure_client import resolve_difficulty


class CatalogueClient:
    def __init__(self, modes):
        self.modes = modes
        self.calls = []

    def call(self, **request):
        self.calls.append(request)
        return {"catalogue": {"difficulties": self.modes}}


class DifficultyLookupTests(unittest.TestCase):
    def setUp(self):
        self.modes = [{"id": "easy-id", "name": "Easy", "isDefault": False},
                      {"id": "normal-id", "name": "Medium", "isDefault": True}]
        self.client = CatalogueClient(copy.deepcopy(self.modes))

    def test_name_and_id_return_the_server_id_without_writing(self):
        self.assertEqual("easy-id", resolve_difficulty(self.client, "EASY"))
        self.assertEqual("normal-id", resolve_difficulty(self.client, "NORMAL-ID"))
        self.assertEqual(self.modes, self.client.modes)
        self.assertTrue(all(c == {"op": "catalog"} for c in self.client.calls))

    def test_normal_alias_uses_the_server_default_flag(self):
        self.assertEqual("normal-id", resolve_difficulty(self.client, "normal"))
        self.assertEqual("normal-id", resolve_difficulty(self.client, "Medium"))

    def test_unknown_name_is_rejected(self):
        with self.assertRaises(ValueError):
            resolve_difficulty(self.client, "unlisted-test-copy")

    def test_ambiguous_display_name_requires_an_id(self):
        self.client.modes.append({"id": "another-easy", "name": "Easy", "isDefault": False})
        with self.assertRaises(ValueError):
            resolve_difficulty(self.client, "Easy")
        self.assertEqual("another-easy", resolve_difficulty(self.client, "another-easy"))


if __name__ == "__main__":
    unittest.main()
