#!/usr/bin/env python3
"""Summarize latest structured diagnostics from Blade & Sorcery Player.log."""

import argparse
import re
import sys
from collections import defaultdict

MODS = ("DOT", "CSM", "EIP", "IDM")
DIAG_RE = re.compile(r"\[(DOT|CSM|EIP|IDM)\]\s+diag\s+evt=([a-z_]+)\s*(.*)")
KV_RE = re.compile(r"([A-Za-z0-9_]+)=([^\s]+)")


def parse_key_values(tail: str):
    data = {}
    for key, value in KV_RE.findall(tail or ""):
        data[key] = value
    return data


def main():
    parser = argparse.ArgumentParser(description="Summarize latest mod telemetry runs from Player.log")
    parser.add_argument("log_path", help="Path to Player.log")
    args = parser.parse_args()

    try:
        with open(args.log_path, "r", encoding="utf-8", errors="replace") as handle:
            lines = handle.readlines()
    except OSError as exc:
        print(f"error: failed to read log file: {exc}", file=sys.stderr)
        return 2

    events_by_mod = defaultdict(list)
    signal_counts = {mod: {"error": 0, "warning": 0, "exception": 0} for mod in MODS}

    for index, raw_line in enumerate(lines):
        line = raw_line.strip()

        for mod in MODS:
            tag = f"[{mod}]"
            if tag not in line:
                continue
            lower = line.lower()
            if "error" in lower:
                signal_counts[mod]["error"] += 1
            if "warning" in lower or "warn" in lower:
                signal_counts[mod]["warning"] += 1
            if "exception" in lower:
                signal_counts[mod]["exception"] += 1

        match = DIAG_RE.search(line)
        if not match:
            continue

        mod, event, tail = match.groups()
        fields = parse_key_values(tail)
        events_by_mod[mod].append(
            {
                "line": index + 1,
                "event": event,
                "run": fields.get("run", "none"),
                "fields": fields,
                "raw": line,
            }
        )

    print("=== Player.log Diagnostics Report ===")
    for mod in MODS:
        events = events_by_mod.get(mod, [])
        if not events:
            print(f"\n[{mod}] no structured diagnostics found")
            counts = signal_counts[mod]
            print(
                f"  log_signals: errors={counts['error']} warnings={counts['warning']} exceptions={counts['exception']}"
            )
            continue

        last_start = next((evt for evt in reversed(events) if evt["event"] == "session_start"), None)
        selected_run = last_start["run"] if last_start else events[-1]["run"]
        run_events = [evt for evt in events if evt["run"] == selected_run]
        if not run_events:
            run_events = [events[-1]]

        last_totals = next((evt for evt in reversed(run_events) if evt["event"] == "session_totals"), None)
        last_kpi = next((evt for evt in reversed(run_events) if evt["event"] == "session_kpi"), None)
        last_end = next((evt for evt in reversed(run_events) if evt["event"] == "session_end"), None)

        print(f"\n[{mod}] run={selected_run} diag_events={len(run_events)}")
        if last_totals:
            fields = last_totals["fields"]
            print(
                "  totals: "
                f"uptimeSec={fields.get('uptimeSec', 'n/a')} "
                f"summaryCount={fields.get('summaryCount', 'n/a')} "
                f"errors={fields.get('errors', fields.get('errorCount', 'n/a'))}"
            )
        else:
            print("  totals: missing")

        if last_kpi:
            fields = last_kpi["fields"]
            important_keys = [
                "triggerRate",
                "blockRate",
                "applyRate",
                "skipRate",
                "adjustmentRate",
                "hitSkipRate",
                "tickKillRate",
                "avgTickDamage",
                "peakActive",
                "frameDrop",
                "severeDropRate",
                "errors",
            ]
            parts = [f"{key}={fields[key]}" for key in important_keys if key in fields]
            print("  kpi: " + (" ".join(parts) if parts else "present (custom fields)"))
        else:
            print("  kpi: missing")

        if last_end:
            print(f"  session_end_line: {last_end['line']}")

        counts = signal_counts[mod]
        print(
            f"  log_signals: errors={counts['error']} warnings={counts['warning']} exceptions={counts['exception']}"
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
