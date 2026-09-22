#!/usr/bin/env python3
"""Inventory effective IMAP/0006 physical bindings and command resolution."""
import csv,re,struct,sys,zlib
from pathlib import Path

def ex(k,d,n):
    x=zlib.decompress(d) if k==2 else d[:n]
    if len(x)!=n: raise ValueError("length")
    return x
def tre_records(path):
    raw=path.read_bytes()
    if raw[:8]!=b"EERT5000": return
    n,o,rk,rs,nk,ns,nu=struct.unpack_from("<7I",raw,8)
    rr=ex(rk,raw[o:o+rs],n*24); nn=ex(nk,raw[o+rs:o+rs+ns],nu)
    for i in range(n):
        _,us,pos,k,cs,no=struct.unpack_from("<6I",rr,i*24)
        end=nn.find(b"\0",no); name=nn[no:end].decode("ascii","replace").replace("\\","/").lower()
        data=b"" if cs==0 else ex(k,raw[pos:pos+cs],us)
        yield name,cs!=0,data
def children(d,a,b):
    p=a
    while p+8<=b:
        tag=d[p:p+4]; n=struct.unpack_from(">I",d,p+4)[0]; e=p+8+n
        if e>b: raise ValueError(f"truncated {tag!r}")
        yield tag,p+8,e; p=e
    if p!=b: raise ValueError("tail")
def cstring(d,p,e):
    q=d.find(b"\0",p,e)
    if q<0: raise ValueError("cstring")
    return d[p:q].decode("ascii","replace")
def parse_map(name,d):
    top=list(children(d,0,len(d)))
    if len(top)!=1 or top[0][0]!=b"FORM" or d[top[0][1]:top[0][1]+4]!=b"IMAP": raise ValueError("IMAP")
    vers=list(children(d,top[0][1]+4,top[0][2])); v=vers[0]
    version=d[v[1]:v[1]+4].decode("ascii","replace")
    command_asset=""; shifts=[]; rows=[]
    for tag,a,b in children(d,v[1]+4,v[2]):
        if tag!=b"FORM": continue
        form=d[a:a+4]; body=a+4
        if form==b"CMDS":
            for t,x,y in children(d,body,b):
                if t==b"NAME": command_asset=d[x:y].split(b"\0",1)[0].decode("ascii","replace").replace("\\","/").lower()
        elif form==b"SHFT":
            for t,x,y in children(d,body,b):
                if t==b"FORM" and d[x:x+4]==b"KEY ":
                    for q,m,n in children(d,x+4,y):
                        if q==b"DATA":
                            if n-m!=9: raise ValueError("shift DATA")
                            key,bit=struct.unpack_from("<II",d,m); shifts.append((key,bit,d[m+8]))
        elif form==b"MAP ":
            mask=None; mapflag=None
            families=[]
            for t,x,y in children(d,body,b):
                if t==b"INFO":
                    if y-x!=5: raise ValueError("INFO")
                    mask=struct.unpack_from("<I",d,x)[0]; mapflag=d[x+4]
                elif t==b"FORM":
                    family=d[x:x+4].decode("ascii","replace")
                    if family in {"KEYS","MOSB","JOYB","JOYX","JOYS","POV "}:
                        for q,m,n in children(d,x+4,y):
                            if q==b"DATA":
                                if n-m<5: raise ValueError("binding DATA")
                                physical=struct.unpack_from("<I",d,m)[0]
                                command=cstring(d,m+4,n)
                                families.append((family,physical,command))
            if mask is None: raise ValueError("MAP INFO")
            for family,physical,command in families:
                rows.append(dict(source=name,version=version,command_asset=command_asset,
                    shift_mask=f"0x{mask:08X}",map_flag=mapflag,family=family,
                    physical_index=physical,command=command))
    return version,command_asset,shifts,rows

root=Path(sys.argv[1]); order=[]
for line in (root/"swgemu_live.cfg").read_text(errors="replace").splitlines():
    m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s]+)",line)
    if m: order.append((int(m.group(1)),m.group(2)))
overlay={}
for priority,archive in sorted(order):
    path=root/archive
    if path.exists():
        for name,present,data in tre_records(path):
            if name.startswith("input/") and "inputmap" in name and not name.endswith("_cmds.iff"):
                overlay[name]=(priority,archive,present,data)
rows=[]; summaries=[]
for name,(priority,archive,present,data) in sorted(overlay.items()):
    if not present: continue
    try: version,cmds,shifts,bindings=parse_map(name,data)
    except Exception as exc:
        summaries.append((name,archive,"ERROR",str(exc),0,0)); continue
    for row in bindings: row["archive"]=archive; rows.append(row)
    summaries.append((name,archive,version,cmds,len(shifts),len(bindings)))
fields=["source","archive","version","command_asset","shift_mask","map_flag","family","physical_index","command"]
with Path(sys.argv[2]).open("w",newline="",encoding="utf-8") as f:
    w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print("maps",len(summaries),"bindings",len(rows))
for s in summaries: print(*s,sep="\t")
