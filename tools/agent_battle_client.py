"""Standard-library client for DiceLand's shared-rule, graphics-free battle API."""
import argparse
import json
from pathlib import Path
import sys
import urllib.error
import urllib.request
import uuid


class Client:
    def __init__(self, url="http://127.0.0.1:8091/v1/command"):
        self.url = url

    def call(self, **command):
        request = urllib.request.Request(self.url, json.dumps(command, ensure_ascii=False).encode("utf-8"), {"Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                result = json.load(response)
        except urllib.error.HTTPError as error:
            result = json.load(error)
        if not result["success"]:
            raise RuntimeError(f"{result['error']}: {result['message']}")
        return result

    def act(self, state, **action):
        return self.call(**action, sessionId=state["sessionId"], revision=state["revision"], requestId=uuid.uuid4().hex)["state"]


def asset(catalogue, group, name):
    matches = [item for item in catalogue[group] if item["name"] == name]
    if len(matches) != 1:
        raise ValueError(f"Expected one {group} entry named {name!r}, found {len(matches)}. Use catalog IDs for ambiguous names.")
    return matches[0]["id"]


def canonical(state):
    result = dict(state)
    result["sessionId"] = ""
    return result


def demo(client, args):
    catalogue = client.call(op="catalog")["catalogue"]
    configuration = dict(op="create", seed=args.seed, maxRounds=args.max_rounds,
                         party=[dict(character=asset(catalogue, "characters", args.character), weapon=asset(catalogue, "weapons", args.weapon))],
                         enemies=[asset(catalogue, "enemies", name) for name in args.enemy],
                         relics=[asset(catalogue, "relics", name) for name in args.relic])
    state = client.call(**configuration)["state"]
    session_id = state["sessionId"]
    try:
        while state["phase"] == "targeting":
            # A deliberately simple baseline policy. Agents can replace this with their own decisions.
            slots = [(a["id"], b["index"]) for a in state["actors"] if a["side"] == "ally" and not a["dead"] for b in a["slots"]]
            for actor_id, button_index in slots:
                targets = [a for a in state["legalActions"] if a["op"] == "target" and a["actorId"] == actor_id and a["buttonIndex"] == button_index]
                if targets:
                    target = targets[0]
                    state = client.act(state, op="target", actorId=actor_id, buttonIndex=button_index,
                                       targetId=target["targetId"], targetButtonIndex=target["targetButtonIndex"])
            state = client.act(state, op="commit")
        replay = client.call(op="replay", sessionId=session_id)["replay"]
        result = dict(replay=replay, result=canonical(state))
        if args.output:
            path = Path(args.output)
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
        return dict(outcome=state["outcome"], round=state["round"], actions=state["revision"],
                    actors=[dict(name=a["name"], hp=a["hp"], maxHp=a["maxHp"]) for a in state["actors"]], replay=args.output)
    finally:
        client.call(op="close", sessionId=session_id)


def replay(client, path):
    saved = json.loads(Path(path).read_text(encoding="utf-8"))
    record = saved["replay"]
    state = client.call(**record["create"])["state"]
    session_id = state["sessionId"]
    try:
        current = client.call(op="replay", sessionId=session_id)["replay"]
        if (current["contentHash"], current["engineVersion"]) != (record["contentHash"], record["engineVersion"]):
            raise RuntimeError("Content or engine version changed; this replay cannot be validated against the original result.")
        for command in record["commands"]:
            command = dict(command, sessionId=session_id)
            state = client.call(**command)["state"]
        if canonical(state) != saved["result"]:
            raise RuntimeError("Replay diverged from the recorded result.")
        return dict(matched=True, outcome=state["outcome"], round=state["round"], actions=state["revision"])
    finally:
        client.call(op="close", sessionId=session_id)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--url", default="http://127.0.0.1:8091/v1/command")
    sub = parser.add_subparsers(dest="mode", required=True)
    sub.add_parser("health")
    sub.add_parser("catalog")
    command = sub.add_parser("command")
    command.add_argument("json", help="JSON command, or - to read stdin")
    play = sub.add_parser("demo")
    play.add_argument("--character", default="Chara")
    play.add_argument("--weapon", default="新手木剑")
    play.add_argument("--enemy", action="append", default=None)
    play.add_argument("--relic", action="append", default=[])
    play.add_argument("--seed", type=int, default=1234)
    play.add_argument("--max-rounds", type=int, default=100)
    play.add_argument("--output")
    check = sub.add_parser("replay")
    check.add_argument("path")
    args = parser.parse_args()
    client = Client(args.url)
    if args.mode in ("health", "catalog"):
        result = client.call(op=args.mode)
    elif args.mode == "command":
        result = client.call(**json.loads(sys.stdin.read() if args.json == "-" else args.json))
    elif args.mode == "demo":
        args.enemy = args.enemy or ["绿色史莱姆"]
        result = demo(client, args)
    else:
        result = replay(client, args.path)
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
