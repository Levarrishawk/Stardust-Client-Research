#!/usr/bin/env python3
"""Resolve the configured TRE TRE overlay for SHT/EFT records, including tombstones."""
from __future__ import annotations
import csv, re, struct, sys, zlib
from collections import Counter
from pathlib import Path

TARGET = {b"SSHT", b"EFCT", b"IMPL", b"PASS"}

def expand(kind, data, expected):
    out = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if out is None or len(out) != expected: raise ValueError("invalid compressed block")
    return out

def forms(data):
    result=[]
    def walk(start,end):
        p=start
        while p+8<=end:
            tag=data[p:p+4]; size=struct.unpack_from(">I",data,p+4)[0]; stop=p+8+size
            if stop>end: raise ValueError("bad IFF")
            if tag==b"FORM":
                kind=data[p+8:p+12]; inner=p+12; version=""
                if inner+12<=stop and data[inner:inner+4]==b"FORM": version=data[inner+8:inner+12].decode("ascii","replace")
                if kind in TARGET: result.append(f"{kind.decode()}/{version}")
                walk(inner,stop)
            p=stop
        if p!=end: raise ValueError("trailing IFF bytes")
    walk(0,len(data)); return ";".join(result)

def records(path):
    raw=path.read_bytes(); count,off,rk,rs,nk,ns,nu=struct.unpack_from("<7I",raw,8)
    recs=expand(rk,raw[off:off+rs],count*24); names=expand(nk,raw[off+rs:off+rs+ns],nu)
    for i in range(count):
        _,usize,pos,kind,csize,noff=struct.unpack_from("<6I",recs,i*24)
        end=names.find(b"\0",noff); name=names[noff:end].decode("utf-8","replace").lower().replace("\\","/")
        if Path(name).suffix not in (".sht",".eft"): continue
        yield name, None if csize == 0 else expand(kind,raw[pos:pos+csize],usize)

def main(root,cfg,out):
    root=Path(root); cfg=Path(cfg)
    priorities=[]
    for line in cfg.read_text(errors="replace").splitlines():
        m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)",line,re.I)
        if m: priorities.append((int(m.group(1)),m.group(2)))
    winners={}; errors=[]
    for priority,archive in sorted(priorities,reverse=True):
        path=root/archive
        if not path.exists(): errors.append((archive,"missing")); continue
        for name,data in records(path):
            if name in winners: continue
            if data is None: winners[name]=(priority,archive,"tombstone","")
            else:
                try: winners[name]=(priority,archive,"live",forms(data))
                except Exception as exc: winners[name]=(priority,archive,"error",str(exc)); errors.append((archive,f"{name}: {exc}"))
    with open(out,"w",newline="",encoding="utf-8") as f:
        w=csv.writer(f); w.writerow(("name","priority","archive","status","forms"))
        for name,(p,a,s,ff) in sorted(winners.items()): w.writerow((name,p,a,s,ff))
    live=[v for v in winners.values() if v[2]=="live"]
    print("configured_archives",len(priorities),"winners",len(winners),"live",len(live),"tombstones",sum(v[2]=="tombstone" for v in winners.values()))
    unsupported=[(n,v) for n,v in winners.items() if v[2]=="live" and re.search(r"PASS/0010|IMPL/000[89]",v[3])]
    print("unsupported_live_winners",len(unsupported))
    print("unsupported_by_archive",Counter(v[1] for _,v in unsupported))
    for n,v in unsupported[:100]: print(v[0],v[1],n,v[3])
    print("errors",len(errors)); [print("ERROR",*e) for e in errors[:20]]

if __name__=="__main__": main(*sys.argv[1:4])
