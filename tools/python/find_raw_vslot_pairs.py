#!/usr/bin/env python3
import bisect, csv, sys
import pefile

pe = pefile.PE(sys.argv[1])
base = pe.OPTIONAL_HEADER.ImageBase
text = next(s for s in pe.sections if s.Name.rstrip(b"\0") == b".text")
data = text.get_data()
va0 = base + text.VirtualAddress
with open(sys.argv[2], newline="", encoding="utf-8") as f:
    rows = list(csv.DictReader(f, delimiter="\t"))
starts = [int(r["entry"], 16) for r in rows]
hits = {}
patterns = []
for reg in range(8):
    if reg != 4:
        patterns.append((bytes((0xff, 0x10 + reg)), 0))
        patterns.append((bytes((0xff, 0x50 + reg, 0x04)), 4))
for needle, slot in patterns:
    pos = 0
    while True:
        pos = data.find(needle, pos)
        if pos < 0:
            break
        va = va0 + pos
        i = bisect.bisect_right(starts, va) - 1
        if i >= 0 and va < int(rows[i]["end"], 16):
            hits.setdefault(i, []).append((va, slot))
        pos += 1
for i, found in hits.items():
    if {slot for _, slot in found} == {0, 4}:
        print(rows[i]["entry"], rows[i]["end"], rows[i]["name"], ",".join(f"{a:08X}:{s}" for a,s in found), sep="\t")
