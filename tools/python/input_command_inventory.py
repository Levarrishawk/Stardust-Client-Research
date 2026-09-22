#!/usr/bin/env python3
"""Extract effective IMAP command definitions and numeric action records."""
import csv,re,struct,sys,zlib
from pathlib import Path

def ex(k,d,n):
    x=zlib.decompress(d) if k==2 else d[:n]
    if len(x)!=n: raise ValueError("length")
    return x
def recs(p):
    raw=p.read_bytes()
    if raw[:8]!=b"EERT5000": return
    n,o,rk,rs,nk,ns,nu=struct.unpack_from("<7I",raw,8); rr=ex(rk,raw[o:o+rs],n*24); nn=ex(nk,raw[o+rs:o+rs+ns],nu)
    for i in range(n):
        _,us,pos,k,cs,no=struct.unpack_from("<6I",rr,i*24); end=nn.find(b"\0",no); name=nn[no:end].decode("ascii","replace").replace("\\","/").lower()
        data=None
        if name in {"input/groundinputmap_cmds.iff","input/spaceinputmap_cmds.iff","input/ui_inputmap_cmds.iff"}: data=b"" if cs==0 else ex(k,raw[pos:pos+cs],us)
        yield name,cs!=0,data
def kids(d,a,b):
    p=a
    while p+8<=b:
        t=d[p:p+4]; n=struct.unpack_from(">I",d,p+4)[0]; e=p+8+n
        if e>b: raise ValueError("truncated")
        yield t,p+8,e; p=e
    if p!=b: raise ValueError("tail")
def cstr(d,p,e):
    q=d.find(b"\0",p,e)
    if q<0: raise ValueError("cstring")
    return d[p:q].decode("ascii","replace"),q+1
def parse(name,d):
    out=[]; top=list(kids(d,0,len(d)))
    if len(top)!=1 or top[0][0]!=b"FORM" or d[top[0][1]:top[0][1]+4]!=b"CMDS": raise ValueError("CMDS")
    versions=list(kids(d,top[0][1]+4,top[0][2])); v=versions[0]; version=d[v[1]:v[1]+4].decode("ascii")
    for tag,a,b in kids(d,v[1]+4,v[2]):
        if tag!=b"FORM" or d[a:a+4]!=b"CATE": continue
        category=""
        for t,c,e in kids(d,a+4,b):
            if t==b"NAME": category=d[c:e].split(b"\0",1)[0].decode("ascii","replace")
            elif t==b"CMD ":
                p=c; command,p=cstr(d,p,e); shift=struct.unpack_from("<I",d,p)[0]; p+=4; f0=d[p];f1=d[p+1];p+=2
                for slot in range(4):
                    v0,v1=struct.unpack_from("<II",d,p);p+=8; payload,p=cstr(d,p,e)
                    out.append(dict(source=name,version=version,category=category,command=command,allowed_device_mask=f"0x{shift:08X}",flag0=f0,flag1=f1,action_slot=slot,value0=v0,value1=v1,payload=payload))
                if p!=e: raise ValueError(f"CMD tail {command}")
    return out
root=Path(sys.argv[1]); cfg=root/"swgemu_live.cfg"; order=[]
for line in cfg.read_text(errors="replace").splitlines():
    m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s]+)",line)
    if m: order.append((int(m.group(1)),m.group(2)))
overlay={}
for pr,n in sorted(order):
    p=root/n
    if p.exists():
        for name,present,data in recs(p):
            if data is not None: overlay[name]=(pr,n,present,data)
rows=[]
for name,(pr,archive,present,data) in sorted(overlay.items()):
    if present:
        for row in parse(name,data): row["archive"]=archive; rows.append(row)
fields=["source","archive","version","category","command","allowed_device_mask","flag0","flag1","action_slot","value0","value1","payload"]
with Path(sys.argv[2]).open("w",newline="",encoding="utf-8") as f:
    w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
commands={(r['source'],r['command']) for r in rows}; active=[r for r in rows if r['value0'] or r['value1'] or r['payload']]
print(f"sources={len(set(r['source'] for r in rows))} commands={len(commands)} action_records={len(rows)} nonempty_actions={len(active)} unique_value0={len(set(r['value0'] for r in active))}")
from collections import Counter
print("versions",Counter(r['version'] for r in rows));print("flags",Counter((r['flag0'],r['flag1']) for r in rows));print("slots_nonempty",Counter(r['action_slot'] for r in active))
if len(sys.argv) > 3:
    grouped={}
    for r in active:
        grouped.setdefault(r["value0"],[]).append(f"{r['source']}:{r['category']}:{r['command']}:slot{r['action_slot']}")
    with Path(sys.argv[3]).open("w",newline="",encoding="utf-8") as f:
        w=csv.writer(f);w.writerow(["numeric_action_id","references"])
        for key in sorted(grouped): w.writerow([key," | ".join(sorted(grouped[key]))])
