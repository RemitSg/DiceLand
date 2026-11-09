"""One model-selected action per call. No policy, route search or automatic turns.

Create/recover a run with agent_adventure_client.py command, then attach its session.
Logs contain complete observations and short user/model-supplied action rationales.
"""
import argparse
import copy
from datetime import datetime, timezone
import json
from pathlib import Path
import uuid

from agent_battle_client import Client


def select_offered(state, selector):
    """Match supplied fields, including nested battleAction; never rank candidates."""
    if not isinstance(selector, dict) or not selector.get("op"):
        raise ValueError("Supply an action selector with op")
    if any(k in selector for k in ("sessionId", "revision", "requestId")):
        raise ValueError("Transport identity comes from the current observation")

    def matches(action, wanted):
        return all(k in action and (matches(action[k], v) if isinstance(v, dict) and isinstance(action[k], dict)
                                    else action[k] == v) for k, v in wanted.items())

    found = [a for a in state["legalActions"] if matches(a, selector)]
    if len(found) != 1:
        raise ValueError(f"Selector matches {len(found)} offered actions; supply explicit IDs, indices or coordinates")
    return copy.deepcopy(found[0])


class ManualSession:
    def __init__(self, client, directory):
        self.client = client
        self.root = Path(directory)
        self.record = json.loads((self.root / "session.json").read_text(encoding="utf-8"))

    @classmethod
    def attach(cls, client, directory, session_id):
        root = Path(directory)
        root.mkdir(parents=True, exist_ok=True)
        if (root / "session.json").exists():
            raise ValueError("Log directory already belongs to a session")
        state = client.call(op="observe_run", sessionId=session_id)["state"]
        catalog = client.call(op="catalog")["catalogue"]
        cls._write(root / "catalogue.json", catalog)
        cls._write(root / "session.json", {"sessionId": session_id, "state": state,
                    "pending": None, "decisionSource": "model", "attachedRevision": state["revision"]})
        result = cls(client, root)
        result._append({"kind": "attach", "state": state})
        return result

    @staticmethod
    def _write(path, value):
        temp = path.with_suffix(path.suffix + ".tmp")
        temp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        temp.replace(path)

    def _append(self, value):
        with (self.root / "observations.jsonl").open("a", encoding="utf-8") as stream:
            stream.write(json.dumps(dict(time=datetime.now(timezone.utc).isoformat(), **value), ensure_ascii=False) + "\n")

    def _persist(self):
        self._write(self.root / "session.json", self.record)

    def observe(self):
        state = self.client.call(op="observe_run", sessionId=self.record["sessionId"])["state"]
        self._append({"kind": "observe", "state": state})
        self.record["state"] = state
        self._persist()
        return state

    def act(self, selector, reason):
        if not isinstance(reason, str) or not reason.strip():
            raise ValueError("A short decision rationale is required")
        if self.record["pending"]:
            raise ValueError("An unresolved request is pending; use retry before submitting a new action")
        # Re-observe before matching; a cached retry response or another caller may be older.
        state = self.observe()
        action = select_offered(state, selector)
        if action["op"] == "run_next":
            raise ValueError("Start the next run explicitly with a newly registered seed; run_next defaults to seed1")
        request = dict(action, sessionId=state["sessionId"], revision=state["revision"], requestId=uuid.uuid4().hex)
        pending = {"request": request, "reason": reason, "decisionSource": "model", "before": state}
        self.record["pending"] = pending
        self._persist()
        self._append({"kind": "request", **pending})
        return self._send_pending()

    def _send_pending(self):
        pending = self.record["pending"]
        if pending is None:
            raise ValueError("No pending request")
        # Exceptions deliberately leave the exact request on disk. No new ID or auto retry.
        response = self.client.call(**pending["request"])
        self._append({"kind": "response", **pending, "response": response})
        self.record["state"] = response["state"]
        self.record["pending"] = None
        self._persist()
        # A matching retry can return a historical cached observation.
        return self.observe()

    def retry(self):
        return self._send_pending()

    def export(self, slot):
        if self.record["pending"]:
            raise ValueError("Resolve the pending request before exporting")
        response = self.client.call(op="run_export", sessionId=self.record["sessionId"], slotId=slot)
        self._write(self.root / "replay.json", response["replay"])
        self._append({"kind": "export", "slotId": slot, "response": response})
        return response


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--url", default="http://127.0.0.1:8091/v1/command")
    parser.add_argument("--log-dir", required=True)
    commands = parser.add_subparsers(dest="mode", required=True)
    attach = commands.add_parser("attach")
    attach.add_argument("session_id")
    commands.add_parser("observe")
    action = commands.add_parser("act")
    action.add_argument("selector", help="JSON selector, or - for JSON on stdin")
    action.add_argument("--reason", required=True)
    commands.add_parser("retry")
    export = commands.add_parser("export")
    export.add_argument("slot")
    args = parser.parse_args()
    client = Client(args.url)
    if args.mode == "attach":
        result = ManualSession.attach(client, args.log_dir, args.session_id).record["state"]
    else:
        session = ManualSession(client, args.log_dir)
        if args.mode == "act":
            import sys
            selector = json.loads(sys.stdin.read() if args.selector == "-" else args.selector)
            result = session.act(selector, args.reason)
        elif args.mode == "retry":
            result = session.retry()
        elif args.mode == "export":
            result = session.export(args.slot)
        else:
            result = session.observe()
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
