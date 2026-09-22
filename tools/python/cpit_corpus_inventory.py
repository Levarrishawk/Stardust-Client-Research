#!/usr/bin/env python3
"""Inventory effective CPIT/0000 cockpit profiles from the configured TRE overlay."""
from __future__ import annotations
import csv, re, struct, sys, zlib
from pathlib import Path

def expand(kind, data, expected):
    out = zlib.decompress(data) if kind == 2 else data[:expected]
    if len(out) != expected: raise ValueError("expanded length mismatch")
    return out

def records(path):
    raw = path.read_bytes()
    if raw[:8] != b"EERT5000": return
    count, off, rk, rs, nk, ns, nu = struct.unpack_from("<7I", raw, 8)
    recs = expand(rk, raw[off:off+rs], count*24)
    names = expand(nk, raw[off+rs:off+rs+ns], nu)
    for i in range(count):
        _, usize, pos, kind, csize, noff = struct.unpack_from("<6I", recs, i*24)
        end = names.find(b"\0", noff)
        name = names[noff:end].decode("ascii", "replace").replace("\\", "/").lower()
        payload = None
        if name.startswith("cockpit/") and name.endswith(".iff"):
            stored_size = usize if kind == 0 else csize
            payload = b"" if usize == 0 else expand(kind, raw[pos:pos+stored_size], usize)
        yield name, usize != 0, payload

def children(data, start, end):
    p = start
    while p + 8 <= end:
        tag = data[p:p+4]; size = struct.unpack_from(">I", data, p+4)[0]; stop = p+8+size
        if stop > end: raise ValueError(f"truncated {tag!r}")
        yield tag, p+8, stop
        p = stop
    if p != end: raise ValueError("trailing bytes")

def parse_cpit(data):
    top = list(children(data, 0, len(data)))
    if len(top) != 1 or top[0][0] != b"FORM" or data[top[0][1]:top[0][1]+4] != b"CPIT": raise ValueError("not CPIT")
    outer = list(children(data, top[0][1]+4, top[0][2]))
    if len(outer) != 1 or outer[0][0] != b"FORM": raise ValueError("missing version")
    version = data[outer[0][1]:outer[0][1]+4].decode("ascii", "replace")
    vals = {"frame":"", "zooms":[], "frst":None, "off3":[], "off1":[], "hypr":[]}
    for tag, a, b in children(data, outer[0][1]+4, outer[0][2]):
        payload=data[a:b]
        if tag == b"FRAM": vals["frame"] = payload.split(b"\0",1)[0].decode("ascii","replace").lower()
        elif tag == b"ZOOM": vals["zooms"] = list(struct.unpack("<"+"f"*(len(payload)//4), payload))
        elif tag == b"FRST": vals["frst"] = struct.unpack("<f",payload)[0]
        elif tag == b"3OFF": vals["off3"] = list(struct.unpack("<3f",payload))
        elif tag == b"1OFF": vals["off1"] = list(struct.unpack("<3f",payload))
        elif tag == b"FORM" and payload[:4] == b"ISPB":
            for t2,c,d in children(data,a+4,b):
                if t2 == b"HYPR": vals["hypr"] = list(struct.unpack("<3f",data[c:d]))
    return version, vals

root=Path(sys.argv[1]); cfg=root/"swgemu_live.cfg"; out=Path(sys.argv[2])
entries=[]
for line in cfg.read_text(errors="replace").splitlines():
    m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s]+)",line)
    if m: entries.append((int(m.group(1)),m.group(2)))
overlay={}; cockpit_payloads={}
for priority,name in sorted(entries):
    path=root/name
    if not path.exists(): continue
    for asset,present,data in records(path):
        overlay[asset]=(priority,name,present)
        if data is not None: cockpit_payloads[asset]=(priority,name,data)
rows=[]; errors=[]
for asset,(priority,archive,data) in sorted(cockpit_payloads.items()):
    if not data or overlay.get(asset) != (priority,archive,True): continue
    try:
        version,v=parse_cpit(data); frame=v["frame"]
        rows.append({"name":asset,"archive":archive,"version":version,"frame":frame,
            "frame_present":str(frame in overlay and overlay[frame][2]).lower(),"zoom_count":len(v["zooms"]),
            "zoom_sorted":str(all(a<=b for a,b in zip(v["zooms"],v["zooms"][1:]))).lower(),
            "first_person_threshold":v["frst"],"third_person_offset":";".join(map(str,v["off3"])),
            "first_person_offset":";".join(map(str,v["off1"])),"hyperspace_offset":";".join(map(str,v["hypr"]))})
    except Exception as exc: errors.append((asset,str(exc)))
with out.open("w",newline="",encoding="utf-8") as f:
    w=csv.DictWriter(f,fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)
print(f"effective_profiles={len(rows)} errors={len(errors)} missing_frames={sum(r['frame_present']=='false' for r in rows)} unsorted_zoom={sum(r['zoom_sorted']=='false' for r in rows)}")
from collections import Counter
print("versions",Counter(r["version"] for r in rows)); print("zoom_counts",Counter(r["zoom_count"] for r in rows)); print("hyperspace_profiles",sum(bool(r["hyperspace_offset"]) for r in rows))
for item in errors: print("ERROR",item)
