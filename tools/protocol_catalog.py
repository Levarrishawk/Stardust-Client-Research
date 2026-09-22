#!/usr/bin/env python3
"""Compare server packet class names with message-like strings in Stardust.exe.

This is an inventory aid, not evidence that every matching string is a registered
or handled wire message. Results are printed only; the tool is read-only.
"""

import argparse
import json
import pathlib
import re


ASCII_RE = re.compile(rb"[A-Za-z][A-Za-z0-9_:]{3,127}")
CLASS_RE = re.compile(r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*(?:Message|Msg))\b")
HEX_RE = re.compile(r"insertInt\s*\(\s*(0x[0-9A-Fa-f]+)")
HASH_RE = re.compile(r"insertInt\s*\(\s*String::hashCode\s*\(\s*\"([^\"]+)\"")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("image")
    ap.add_argument("server_root")
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()

    data = pathlib.Path(args.image).read_bytes()
    exe_strings = {m.group().decode("ascii") for m in ASCII_RE.finditer(data)}
    exe_messages = {s for s in exe_strings if s.endswith(("Message", "Msg"))}

    server = {}
    root = pathlib.Path(args.server_root)
    for path in root.rglob("*.h"):
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        for cm in CLASS_RE.finditer(text):
            name = cm.group(1)
            tail = text[cm.start():cm.start() + 5000]
            hm = HEX_RE.search(tail)
            sm = HASH_RE.search(tail)
            rec = server.setdefault(name, {"files": [], "literal_opcodes": set(), "hash_names": set()})
            rec["files"].append(str(path.relative_to(root)))
            if hm:
                rec["literal_opcodes"].add(hm.group(1).upper().replace("X", "x"))
            if sm:
                rec["hash_names"].add(sm.group(1))

    server_names = set(server)
    exact = sorted(server_names & exe_messages)
    only_server = sorted(server_names - exe_messages)
    only_exe = sorted(exe_messages - server_names)
    payload = {
        "server_packet_class_names": len(server_names),
        "exe_message_like_strings": len(exe_messages),
        "exact_name_overlap": len(exact),
        "overlap_percent_of_server_classes": round(100 * len(exact) / len(server_names), 2) if server_names else 0,
        "exact_names": exact,
        "server_only_names": only_server,
        "exe_only_names": only_exe,
    }
    if args.json:
        print(json.dumps(payload, indent=2))
    else:
        for k in ("server_packet_class_names", "exe_message_like_strings", "exact_name_overlap", "overlap_percent_of_server_classes"):
            print(f"{k}={payload[k]}")
        print("\nEXACT OVERLAP")
        for name in exact:
            rec = server[name]
            opcodes = ",".join(sorted(rec["literal_opcodes"])) or "hash/runtime"
            print(f"{name}\t{opcodes}\t{rec['files'][0]}")


if __name__ == "__main__":
    main()
