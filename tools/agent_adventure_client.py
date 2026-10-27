"""Standard-library client for the in-progress shared-rule adventure API.

The demo records a trace; full adventure replay/save is a later batch.
"""
import argparse
import json
from pathlib import Path
import sys

from agent_battle_client import Client
from agent_adventure_policy import choose_battle_action


def inspect_equipment(client, item):
    catalog = client.call(op="catalog")["catalogue"]
    matches = [(kind, definition) for kind, key in (("weapon", "weaponDetails"), ("blueprint", "blueprintDetails"), ("character", "characterDetails"))
               for definition in catalog[key] if item in (definition["id"], definition["name"])]
    if len(matches) != 1:
        raise ValueError(f"Expected one equipment/blueprint/character named or identified by {item!r}; found {len(matches)}. Use its catalog ID.")
    kind, definition = matches[0]
    result = {"kind": kind, "definition": definition}
    weapon_id = definition.get("weapon") if kind == "blueprint" else definition.get("initialWeapon") if kind == "character" else None
    if weapon_id:
        result["weapon"] = next((w for w in catalog["weaponDetails"] if w["id"] == weapon_id), None)
    return result


def choose_reforge_action(state):
    actions = state["legalActions"]
    reforge = state["reforge"]
    confirm = next((a for a in actions if a["op"] == "run_reforge_confirm"), None)
    if confirm:
        return confirm
    close = next((a for a in actions if a["op"] in ("run_reforge_finish", "run_reforge_cancel")), None)
    if reforge["stage"] == "first":
        materials = {m["id"]: m for m in reforge["materials"]}
        for action in actions:
            if action["op"] != "run_reforge_first":
                continue
            material = materials[action["assetId"]]
            if material["amount"] >= 2 or any(m["id"] != material["id"] and m["rarity"] == material["rarity"] for m in materials.values()):
                return action
        if reforge["mode"] == "loot":
            return next((a for a in actions if a["op"] == "run_reforge_relic"), close)
    elif reforge["stage"] == "second":
        return next((a for a in actions if a["op"] == "run_reforge_second"), close)
    elif reforge["stage"] == "attribute":
        return next((a for a in actions if a["op"] == "run_reforge_attribute"), close)
    return close


def demo(client, args):
    create = dict(op="create_run", seed=args.seed, maxRounds=args.max_rounds)
    if getattr(args, "profile", None):
        create["profileId"] = args.profile
    state = client.call(**create)["state"]
    session_id = state["sessionId"]
    trace = []
    moved = 0
    seen_equipment = {member["weapon"] for member in state["party"]}
    considered_reforges = set()
    try:
        for _ in range(args.max_actions):
            actions = state["legalActions"]
            action = None
            if state["phase"] in ("victory_pending_settlement", "defeat_pending_settlement"):
                action = next((a for a in actions if a["op"] == "run_settle"), None)
            elif state["phase"] == "finished":
                break
            elif state["phase"] == "starting_relic":
                action = next((a for a in actions if a["op"] == "run_choose_starting_relic"), None)
            elif state["phase"] == "map":
                equipment = next((a for a in actions if a["op"] == "run_equip" and a["assetId"] not in seen_equipment), None)
                if equipment:
                    seen_equipment.add(equipment["assetId"])
                    trace.append(equipment)
                    state = client.act(state, **equipment)
                    continue
                if moved >= args.max_moves:
                    break
                if (state.get("dungeon") or {}).get("active", False):
                    # The shared dungeon board is a line: retreat is left, completion is right.
                    forward = next((a for a in actions if a["op"] == "run_move" and a["x"] > state["x"]), None)
                    if forward:
                        trace.append(forward)
                        state = client.act(state, **forward)
                        moved += 1
                        continue
                tiles = {(t["x"], t["y"]): t for t in state["tiles"]}
                candidates = []
                for move in actions:
                    if move["op"] != "run_move":
                        continue
                    tile = tiles[(move["x"], move["y"])]
                    if tile["type"] in ("Empty", "Key", "Merchant", "Forge") or (tile["type"] in ("Event", "Treasure") and not tile["resolved"]) or (tile["type"] == "Rest" and not tile["resolved"]) or (tile["type"] in ("Battle", "Elite") and tile["encounterAvailable"]):
                        candidates.append((tile, move))
                candidates.sort(key=lambda pair: (pair[0]["visited"], pair[0]["type"] != "Key", pair[0]["danger"], pair[0]["x"], pair[0]["y"]))
                if candidates:
                    action = candidates[0][1]
                    moved += 1
            elif state["phase"] == "treasure":
                # Wealth is the baseline; other agents can value the displayed blueprint and bag.
                action = next((a for a in actions if a["op"] == "run_choose_treasure"), None)
            elif state["phase"] == "treasure_relic":
                action = next((a for a in actions if a["op"] == "run_choose_treasure_relic"), None)
            elif state["phase"] == "treasure_result":
                action = next((a for a in actions if a["op"] == "run_continue_treasure"), None)
            elif state["phase"] == "event":
                # No textual strategy is assumed: choose the lowest-cost legal option.
                options = {o["index"]: o for o in state["mapEvent"]["options"]}
                choices = [a for a in actions if a["op"] == "run_choose_event"]
                if choices:
                    action = min(choices, key=lambda a: options[a["choiceIndex"]]["requiredMoney"])
            elif state["phase"] == "event_result":
                action = next((a for a in actions if a["op"] == "run_continue_event"), None)
            elif state["phase"] == "calamity":
                action = next((a for a in actions if a["op"] == "run_choose_calamity"), None)
            elif state["phase"] == "battle":
                action = choose_battle_action(state)
            elif state["phase"] == "merchant":
                buyable = [a for a in actions if a["op"] == "run_buy"]
                offers = {o["index"]: o for o in state["merchantOffers"]}
                if buyable:
                    action = min(buyable, key=lambda a: offers[a["choiceIndex"]]["price"])
                else:
                    action = next((a for a in actions if a["op"] == "run_leave_location"), None)
            elif state["phase"] == "forge":
                action = next((a for a in actions if a["op"] == "run_craft"), None)
                if action is None:
                    action = next((a for a in actions if a["op"] == "run_leave_location"), None)
            elif state["phase"] == "rest":
                location = (state["x"], state["y"])
                if getattr(args, "reforge", False) and location not in considered_reforges:
                    considered_reforges.add(location)
                    action = next((a for a in actions if a["op"] == "run_open_reforge"), None)
                if action is None and len(state["party"]) < 2:
                    action = next((a for a in actions if a["op"] == "run_recruit"), None)
                if action is None:
                    op = "run_rest_heal" if any(p["hp"] < p["maxHp"] for p in state["party"]) else "run_rest_restore_ap"
                    action = next((a for a in actions if a["op"] == op), None)
            elif state["phase"] == "reforge":
                action = choose_reforge_action(state)
            if action is None:
                break
            trace.append(action)
            state = client.act(state, **action)
        result = {"create": create, "actions": trace, "result": state}
        if args.output:
            path = Path(args.output)
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
        return {"phase": state["phase"], "outcome": state.get("outcome"), "moves": moved, "actions": len(trace),
                "cycle": state["cycle"], "money": state["money"], "keys": state["keys"], "party": state["party"],
                "configurationIssues": state["configurationIssues"], "settlement": state["settlement"], "meta": state["meta"], "trace": args.output}
    finally:
        client.call(op="run_close", sessionId=session_id)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--url", default="http://127.0.0.1:8091/v1/command")
    commands = parser.add_subparsers(dest="mode", required=True)
    play = commands.add_parser("demo")
    play.add_argument("--seed", type=int, default=1234)
    play.add_argument("--max-rounds", type=int, default=1000)
    play.add_argument("--max-moves", type=int, default=2)
    play.add_argument("--max-actions", type=int, default=300)
    play.add_argument("--output")
    play.add_argument("--reforge", action="store_true", help="Try a legal reforge once per rest visit before other rest actions")
    play.add_argument("--profile", help="Explicit isolated persistent agent profile; omitted profiles are discarded on close")
    raw = commands.add_parser("command")
    raw.add_argument("json", help="JSON command, or - to read stdin")
    inspect = commands.add_parser("inspect-equipment", help="Read authored item skills and blueprint materials through the public catalog")
    inspect.add_argument("item", help="Catalog asset ID or exact equipment/blueprint/character name")
    args = parser.parse_args()
    client = Client(args.url)
    if args.mode == "demo":
        result = demo(client, args)
    elif args.mode == "inspect-equipment":
        result = inspect_equipment(client, args.item)
    else:
        result = client.call(**json.loads(sys.stdin.read() if args.json == "-" else args.json))
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
