#!/usr/bin/env python3
"""Summarize effective TXM shader texture records emitted by shader_version_payload_inventory.py."""
import csv
import sys
from collections import Counter

rows = []
with open(sys.argv[1], newline="", encoding="utf-8") as handle:
    for row in csv.DictReader(handle):
        if row["record"] == "TXM":
            rows.append(row)

print("records", len(rows))
print("versions", Counter((r["version"], int(r["length"])) for r in rows))

tags = Counter()
dynamic = Counter()
fields = [Counter() for _ in range(8)]
for row in rows:
    payload = bytes.fromhex(row["value"])
    version = row["version"]
    if version == "0000":
        tag = payload[1:5]
        special = payload[0]
        values = ()
    else:
        tag = payload[0:4]
        special = payload[4]
        values = payload[5:]
    # IFF TAG values are serialized least-significant byte first by the engine.
    text = tag[::-1].decode("ascii", "replace")
    tags[text] += 1
    if text == "ENVM" or text.startswith("_") or special:
        dynamic[(text, special)] += 1
    for index, value in enumerate(values[:8]):
        fields[index][value] += 1

print("unique_tags", len(tags))
print("top_tags", tags.most_common(30))
print("dynamic_or_special", sum(dynamic.values()), dynamic.most_common(30))
for index, counts in enumerate(fields):
    if counts:
        print("byte", index, counts.most_common())
