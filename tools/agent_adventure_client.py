"""Standard-library client for the shared-rule adventure API.

The demo can save/resume a playable session, recover a persistent profile's last
committed command, and export/verify timestamped journals.
"""
import argparse
import json
from pathlib import Path
import sys

from agent_battle_client import Client
from agent_adventure_policy import choose_battle_action, choose_calamity_action, choose_treasure_action, preparation_assessment


def resolve_difficulty(client, value):
    definitions = client.call(op="catalog")["catalogue"]["difficulties"]
    matches = [d for d in definitions if value.casefold() in (d["id"].casefold(), d["name"].casefold())
               or (value.casefold() == "normal" and d["isDefault"])]
    if len(matches) != 1:
        raise ValueError(f"Expected one menu difficulty named or identified by {value!r}; found {len(matches)}. Use an ID from difficulties.")
    return matches[0]["id"]


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
    catalogue = client.call(op="catalog")["catalogue"]
    create = dict(op="create_run", seed=args.seed, maxRounds=args.max_rounds)
    if getattr(args, "difficulty", None):
        create["difficultyId"] = resolve_difficulty(client, args.difficulty)
    if getattr(args, "profile", None):
        create["profileId"] = args.profile
    if getattr(args, "resume_slot", None):
        state = client.call(op="run_load", slotId=args.resume_slot)["state"]
    elif getattr(args, "recover_profile", None):
        state = client.call(op="run_recover", profileId=args.recover_profile)["state"]
    else:
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
                    hostile_ready = tile["type"] in ("Battle", "Elite") and tile["encounterAvailable"] and preparation_assessment(state, tile["type"], catalogue)["ready"]
                    if tile["type"] in ("Empty", "Key", "Merchant", "Forge") or (tile["type"] in ("Event", "Treasure") and not tile["resolved"]) or (tile["type"] == "Rest" and not tile["resolved"]) or hostile_ready:
                        candidates.append((tile, move))
                candidates.sort(key=lambda pair: (pair[0]["visited"], pair[0]["type"] != "Key", pair[0]["danger"], pair[0]["x"], pair[0]["y"]))
                if candidates:
                    action = candidates[0][1]
                    moved += 1
            elif state["phase"] == "treasure":
                action = choose_treasure_action(state, catalogue)
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
                action = choose_calamity_action(state)
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
                health = sum(p["hp"] for p in state["party"]) / max(1, sum(p["maxHp"] for p in state["party"]))
                if action is None and len(state["party"]) < 4 and health >= .7:
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
        if getattr(args, "replay_slot", None):
            client.call(op="run_export", sessionId=session_id, slotId=args.replay_slot)
        saved = client.call(op="run_save", sessionId=session_id, slotId=args.save_slot)["save"] if getattr(args, "save_slot", None) else None
        return {"phase": state["phase"], "outcome": state.get("outcome"), "moves": moved, "actions": len(trace),
                "cycle": state["cycle"], "money": state["money"], "keys": state["keys"], "party": state["party"],
                "configurationIssues": state["configurationIssues"], "settlement": state["settlement"], "meta": state["meta"],
                "trace": args.output, "replaySlot": getattr(args, "replay_slot", None), "save": saved}
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
    start = play.add_mutually_exclusive_group()
    start.add_argument("--profile", help="Create a fresh run in an isolated persistent profile; replaces its recoverable adventure")
    start.add_argument("--resume-slot", help="Resume a playable save slot instead of creating a new run")
    start.add_argument("--recover-profile", help="Recover the last committed command of a persistent profile instead of creating a new run")
    play.add_argument("--save-slot", help="Save a playable checkpoint before closing")
    play.add_argument("--difficulty", help="Menu difficulty ID or exact name for a new adventure; omitted keeps the Normal default")
    play.add_argument("--replay-slot", help="Export the complete journal into a named slot on this host before closing")
    play.add_argument("--verify-replay", action="store_true", help="After closing, verify --replay-slot with recorded timestamps and a temporary profile")
    raw = commands.add_parser("command")
    raw.add_argument("json", help="JSON command, or - to read stdin")
    inspect = commands.add_parser("inspect-equipment", help="Read authored item skills and blueprint materials through the public catalog")
    inspect.add_argument("item", help="Catalog asset ID or exact equipment/blueprint/character name")
    verify = commands.add_parser("verify-replay", help="Verify a named journal on this host; all sessions must be closed")
    verify.add_argument("slot")
    save = commands.add_parser("save-run", help="Save the active session to a named checkpoint")
    save.add_argument("session_id"); save.add_argument("slot")
    load = commands.add_parser("load-run", help="Restore a named checkpoint and leave its new session active")
    load.add_argument("slot")
    recover = commands.add_parser("recover-run", help="Recover a persistent profile and leave its new session active")
    recover.add_argument("profile")
    commands.add_parser("difficulties", help="List actual saved-menu difficulties and their base action-point/recruit rules")
    next_run = commands.add_parser("next-run", help="Start the next run of a finished active session")
    next_run.add_argument("session_id")
    next_run.add_argument("--seed", type=int, default=1)
    next_run.add_argument("--difficulty", help="Menu difficulty ID or exact name; omitted retains the current mode")
    args = parser.parse_args()
    client = Client(args.url)
    if args.mode == "demo":
        if args.difficulty and (args.resume_slot or args.recover_profile):
            parser.error("Restored adventures retain their difficulty; change it with next-run after settlement.")
        if args.verify_replay and not args.replay_slot:
            parser.error("--verify-replay requires --replay-slot")
        result = demo(client, args)
        if args.verify_replay:
            verified = client.call(op="run_verify", slotId=args.replay_slot)["verification"]
            result["replayVerification"] = {k: verified[k] for k in ("verified", "commandsChecked", "finalStateHash")}
    elif args.mode == "inspect-equipment":
        result = inspect_equipment(client, args.item)
    elif args.mode == "verify-replay":
        result = client.call(op="run_verify", slotId=args.slot)["verification"]
    elif args.mode == "save-run":
        result = client.call(op="run_save", sessionId=args.session_id, slotId=args.slot)["save"]
    elif args.mode == "load-run":
        result = client.call(op="run_load", slotId=args.slot)["state"]
    elif args.mode == "recover-run":
        result = client.call(op="run_recover", profileId=args.profile)["state"]
    elif args.mode == "difficulties":
        result = client.call(op="catalog")["catalogue"]["difficulties"]
    elif args.mode == "next-run":
        action = dict(op="run_next", seed=args.seed)
        if args.difficulty:
            action["difficultyId"] = resolve_difficulty(client, args.difficulty)
        state = client.call(op="observe_run", sessionId=args.session_id)["state"]
        result = client.act(state, **action)
    else:
        result = client.call(**json.loads(sys.stdin.read() if args.json == "-" else args.json))
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
