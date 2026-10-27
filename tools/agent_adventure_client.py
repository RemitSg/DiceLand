"""Standard-library client for the in-progress shared-rule adventure API.

The demo records a trace; full adventure replay/save and location features are later batches.
"""
import argparse
import json
from pathlib import Path
import sys

from agent_battle_client import Client


def choose_battle_action(state):
    actions = state["legalActions"]
    battle = state["battle"]
    action = next((a for a in actions if a["op"] == "run_return"), None)
    if action is None and battle["phase"] == "reward":
        action = next((a for a in actions if a["battleAction"]["op"] == "choose_relic"), None)
    if action is None and battle["phase"] == "targeting":
        # Fill each unconnected slot using its current skill, then commit.
        for actor in battle["actors"]:
            if actor["side"] != "ally" or actor["dead"]:
                continue
            for slot in actor["slots"]:
                if slot.get("targetId"):
                    continue
                action = next((a for a in actions if a["op"] == "run_battle" and a["battleAction"]["op"] == "target"
                               and a["battleAction"]["actorId"] == actor["id"] and a["battleAction"]["buttonIndex"] == slot["index"]), None)
                if action:
                    break
            if action:
                break
        if action is None:
            action = next((a for a in actions if a["op"] == "run_battle" and a["battleAction"]["op"] == "commit"), None)
    return action


def demo(client, args):
    state = client.call(op="create_run", seed=args.seed, maxRounds=args.max_rounds)["state"]
    session_id = state["sessionId"]
    trace = []
    moved = 0
    try:
        for _ in range(args.max_actions):
            actions = state["legalActions"]
            action = None
            if state["phase"] == "map":
                if moved >= args.max_moves:
                    break
                tiles = {(t["x"], t["y"]): t for t in state["tiles"]}
                candidates = []
                for move in actions:
                    if move["op"] != "run_move":
                        continue
                    tile = tiles[(move["x"], move["y"])]
                    if tile["type"] in ("Empty", "Key", "Merchant", "Forge") or (tile["type"] in ("Battle", "Elite") and tile["encounterAvailable"]):
                        candidates.append((tile, move))
                candidates.sort(key=lambda pair: (pair[0]["visited"], pair[0]["type"] != "Key", pair[0]["danger"], pair[0]["x"], pair[0]["y"]))
                if candidates:
                    action = candidates[0][1]
                    moved += 1
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
            if action is None:
                break
            trace.append(action)
            state = client.act(state, **action)
        result = {"create": {"op": "create_run", "seed": args.seed, "maxRounds": args.max_rounds}, "actions": trace, "result": state}
        if args.output:
            path = Path(args.output)
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
        return {"phase": state["phase"], "outcome": state.get("outcome"), "moves": moved, "actions": len(trace),
                "cycle": state["cycle"], "money": state["money"], "keys": state["keys"], "party": state["party"],
                "configurationIssues": state["configurationIssues"], "trace": args.output}
    finally:
        client.call(op="run_close", sessionId=session_id)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--url", default="http://127.0.0.1:8091/v1/command")
    commands = parser.add_subparsers(dest="mode", required=True)
    play = commands.add_parser("demo")
    play.add_argument("--seed", type=int, default=1234)
    play.add_argument("--max-rounds", type=int, default=1000)
    play.add_argument("--max-moves", type=int, default=2)
    play.add_argument("--max-actions", type=int, default=300)
    play.add_argument("--output")
    raw = commands.add_parser("command")
    raw.add_argument("json", help="JSON command, or - to read stdin")
    args = parser.parse_args()
    client = Client(args.url)
    result = demo(client, args) if args.mode == "demo" else client.call(**json.loads(sys.stdin.read() if args.json == "-" else args.json))
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
