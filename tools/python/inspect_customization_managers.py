#!/usr/bin/env python3
"""Decode and validate SWG CIDM and ACST customization-manager assets."""
from __future__ import annotations

import argparse
import struct
from collections import Counter
from pathlib import Path


def chunks(raw: bytes, start: int = 0, end: int | None = None):
    end = len(raw) if end is None else end
    off = start
    while off + 8 <= end:
        tag = raw[off:off + 4].decode("ascii", "replace")
        size = struct.unpack_from(">I", raw, off + 4)[0]
        payload = off + 8
        if tag == "FORM":
            form = raw[payload:payload + 4].decode("ascii", "replace")
            yield f"FORM:{form}", raw[payload + 4:payload + size]
        else:
            yield tag, raw[payload:payload + size]
        # SOE's shipped IFF dialect stores chunks back-to-back without RIFF-style
        # even-byte padding (several ACST chunks deliberately have odd sizes).
        off = payload + size


def form_payload(raw: bytes, outer: str, version: str) -> dict[str, bytes]:
    top = dict(chunks(raw))
    inner = dict(chunks(top[f"FORM:{outer}"]))
    return dict(chunks(inner[f"FORM:{version}"]))


def cstrings(blob: bytes):
    result = {}
    off = 0
    while off < len(blob):
        end = blob.find(b"\0", off)
        if end < 0:
            raise ValueError(f"unterminated NAME string at {off}")
        result[off] = blob[off:end].decode("ascii", "replace")
        off = end + 1
    return result


def inspect_cidm(path: Path):
    data = form_payload(path.read_bytes(), "CIDM", "0001")["DATA"]
    rows = []
    off = 0
    while off < len(data):
        ident = struct.unpack_from("<H", data, off)[0]
        off += 2
        end = data.index(0, off)
        rows.append((ident, data[off:end].decode("ascii")))
        off = end + 1
    ids = [x[0] for x in rows]
    names = [x[1] for x in rows]
    print(f"CIDM rows={len(rows)} id_min={min(ids)} id_max={max(ids)} consecutive={ids == list(range(1, len(ids)+1))}")
    print(f"CIDM duplicate_ids={len(ids)-len(set(ids))} duplicate_names={len(names)-len(set(names))}")
    for ident, name in rows:
        print(f"CIDM {ident:3d} {name}")


def inspect_acst(path: Path):
    t = form_payload(path.read_bytes(), "ACST", "0000")
    names = cstrings(t["NAME"])
    pnof = struct.unpack(f"<{len(t['PNOF'])//2}H", t["PNOF"])
    vnof = struct.unpack(f"<{len(t['VNOF'])//2}H", t["VNOF"])
    defv = struct.unpack(f"<{len(t['DEFV'])//4}i", t["DEFV"])
    irng = [struct.unpack_from("<ii", t["IRNG"], i) for i in range(0, len(t["IRNG"]), 8)]
    rtyp = struct.unpack(f"<{len(t['RTYP'])//2}H", t["RTYP"])
    ucmp = [struct.unpack_from("<BBB", t["UCMP"], i) for i in range(0, len(t["UCMP"]), 3)]
    ulst = struct.unpack(f"<{len(t['ULST'])//2}H", t["ULST"])
    uid = [struct.unpack_from("<HHB", t["UIDX"], i) for i in range(0, len(t["UIDX"]), 5)]
    llst = struct.unpack(f"<{len(t['LLST'])//2}H", t["LLST"])
    lid = [struct.unpack_from("<HHB", t["LIDX"], i) for i in range(0, len(t["LIDX"]), 5)]
    cid = [struct.unpack_from("<IH", t["CIDX"], i) for i in range(0, len(t["CIDX"]), 6)]

    errors = []
    def name_at(off, kind):
        if off not in names: errors.append(f"{kind} bad NAME offset {off}")
        return names.get(off, f"<bad:{off}>")
    palettes = [name_at(x, "PNOF") for x in pnof]
    variables = [name_at(x, "VNOF") for x in vnof]
    for i, (vn, rt, dv) in enumerate(ucmp, 1):
        if not 1 <= vn <= len(variables): errors.append(f"UCMP[{i}] bad VNOF {vn}")
        if not 1 <= rt <= len(rtyp): errors.append(f"UCMP[{i}] bad RTYP {rt}")
        if not 1 <= dv <= len(defv): errors.append(f"UCMP[{i}] bad DEFV {dv}")
    for i, x in enumerate(rtyp, 1):
        idx = x & 0x7fff
        limit = len(palettes) if x & 0x8000 else len(irng)
        if not 1 <= idx <= limit: errors.append(f"RTYP[{i}] bad {'palette' if x&0x8000 else 'range'} {idx}")
    for kind, rows, lst in (("UIDX", uid, ulst), ("LIDX", lid, llst)):
        for key, start, count in rows:
            if start + count > len(lst): errors.append(f"{kind} key={key} slice {start}+{count}>{len(lst)}")
    max_key = max([x[0] for x in uid] + [x[0] for x in lid])
    for x in ulst:
        if not 1 <= x <= len(ucmp): errors.append(f"ULST bad UCMP {x}")
    for x in llst:
        if not 1 <= x <= max_key: errors.append(f"LLST bad key {x}")
    for crc, key in cid:
        if not 1 <= key <= max_key: errors.append(f"CIDX {crc:08x} bad key {key}")

    print("ACST " + " ".join(f"{k}={len(v)//({'NAME':1,'PNOF':2,'VNOF':2,'DEFV':4,'IRNG':8,'RTYP':2,'UCMP':3,'ULST':2,'UIDX':5,'LLST':2,'LIDX':5,'CIDX':6}[k])}" for k,v in t.items()))
    print(f"ACST parsed_names={len(names)} key_max={max_key} validation_errors={len(errors)}")
    print(f"ACST CIDX_sorted={cid == sorted(cid)} CIDX_duplicate_crc={len(cid)-len(set(x[0] for x in cid))}")
    inversions = [(i, cid[i-1], cid[i]) for i in range(1, len(cid)) if cid[i-1][0] > cid[i][0]]
    print(f"ACST CIDX_inversions={len(inversions)} first_inversions={inversions[:5]}")
    print(f"ACST UIDX_sorted={uid == sorted(uid)} LIDX_sorted={lid == sorted(lid)}")
    print(f"ACST variable_names={len(variables)} palette_names={len(palettes)} defaults={len(defv)} ranges={len(irng)}")
    print("VARIABLES")
    for i, value in enumerate(variables, 1): print(f"  {i:3d} {value}")
    print("PALETTES")
    for i, value in enumerate(palettes, 1): print(f"  {i:3d} {value}")
    print("RANGE_TYPES", Counter(irng))
    print("DEFAULTS", Counter(defv).most_common())

    direct = {key: tuple(ulst[start:start+count]) for key, start, count in uid}
    links = {key: tuple(llst[start:start+count]) for key, start, count in lid}
    known_keys = set(direct) | set(links)
    bad_links = [(key, child) for key, children in links.items() for child in children if child not in known_keys]
    cycle_paths = []
    memo = {}
    def expand(key, stack=()):
        if key in memo: return memo[key]
        if key in stack:
            cycle_paths.append(stack[stack.index(key):] + (key,))
            return ()
        ordered = []
        seen_names = set()
        for ui in direct.get(key, ()):
            name = variables[ucmp[ui-1][0]-1]
            if name not in seen_names:
                ordered.append(ui); seen_names.add(name)
        for child in links.get(key, ()):
            for ui in expand(child, stack + (key,)):
                name = variables[ucmp[ui-1][0]-1]
                if name not in seen_names:
                    ordered.append(ui); seen_names.add(name)
        memo[key] = tuple(ordered)
        return memo[key]
    expanded_counts = []
    for _, key in cid:
        expanded_counts.append(len(expand(key)))
    print(f"ACST keys={len(known_keys)} bad_inheritance_links={len(bad_links)} cycles={len(cycle_paths)}")
    mapped_keys = {key for _, key in cid}
    print(f"ACST orphan_keys={sorted(known_keys-mapped_keys)}")
    print(f"ACST appearance_expanded_variables min={min(expanded_counts)} max={max(expanded_counts)} zero={expanded_counts.count(0)} mean={sum(expanded_counts)/len(expanded_counts):.2f}")
    print("ACST expanded_variable_count_histogram", sorted(Counter(expanded_counts).items()))
    type_use = Counter()
    for ui in set(x for rows in memo.values() for x in rows):
        vn, rt, dv = ucmp[ui-1]
        raw_type = rtyp[rt-1]
        type_use["palette" if raw_type & 0x8000 else "range"] += 1
    print("ACST reachable_unique_UCMP", sum(type_use.values()), "kinds", dict(type_use))
    reachable_ucmp = set(x for rows in memo.values() for x in rows)
    for ui in sorted(set(range(1, len(ucmp)+1)) - reachable_ucmp):
        vn, rt, dv = ucmp[ui-1]
        raw_type = rtyp[rt-1]
        target = palettes[(raw_type & 0x7fff)-1] if raw_type & 0x8000 else irng[(raw_type & 0x7fff)-1]
        print(f"ACST unreachable_UCMP={ui} variable={variables[vn-1]} type={target!r} default={defv[dv-1]}")

    # Model the conventional midpoint binary search used by the client/server.
    def binary_find(target):
        lo, hi = 0, len(cid)
        while lo < hi:
            mid = (lo + hi) // 2
            value = cid[mid][0]
            if value < target: lo = mid + 1
            elif value > target: hi = mid
            else: return mid
        return None
    misses = [(crc, key) for crc, key in cid if binary_find(crc) is None]
    print(f"ACST CIDX_binary_search_misses={misses}")
    if errors:
        print("ERRORS")
        for e in errors[:100]: print(" ", e)


parser = argparse.ArgumentParser()
parser.add_argument("cidm", type=Path)
parser.add_argument("acst", type=Path)
args = parser.parse_args()
inspect_cidm(args.cidm)
inspect_acst(args.acst)
