from __future__ import annotations

import pathlib, re, struct, sys, zlib

def expand(kind, data, expected):
    out = data[:expected] if kind == 0 else zlib.decompress(data) if kind == 2 else None
    if out is None or len(out) != expected: raise ValueError
    return out

def chunks(data, start, end):
    p = start
    while p + 8 <= end:
        tag=data[p:p+4]; n=struct.unpack_from(">I",data,p+4)[0]; ps=p+8; pe=ps+n
        if pe > end: return
        yield tag,ps,pe
        p=pe

def walk(data,start,end):
    for tag,ps,pe in chunks(data,start,end):
        if tag in (b"LOBJ",b"HLOB"): yield tag,data[ps:pe]
        if tag==b"FORM" and ps+4<=pe: yield from walk(data,ps+4,pe)

root,cfg=pathlib.Path(sys.argv[1]),pathlib.Path(sys.argv[2]); configured=[]
for line in cfg.read_text(errors="replace").splitlines():
    m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)",line,re.I)
    if m: configured.append((int(m.group(1)),m.group(2)))
seen=set(); out=[]
for _,aname in sorted(configured,reverse=True):
    ap=root/aname
    if not ap.exists(): continue
    raw=ap.read_bytes()
    if raw[:8]!=b"EERT5000": continue
    count,off,rk,rs,nk,ns,nu=struct.unpack_from("<7I",raw,8)
    recs=expand(rk,raw[off:off+rs],count*24); names=expand(nk,raw[off+rs:off+rs+ns],nu)
    for i in range(count):
        _,usize,poff,kind,csize,noff=struct.unpack_from("<6I",recs,i*24)
        q=names.find(b"\0",noff); name=names[noff:q].decode("ascii","replace").lower().replace("\\","/")
        if not name.endswith(".cdf") or name in seen: continue
        seen.add(name)
        try: data=expand(kind,raw[poff:poff+csize],usize)
        except Exception: continue
        for tag,payload in walk(data,0,len(data)):
            try:
                typ=struct.unpack_from("<I",payload,0)[0]; q=payload.find(b"\0",4)
                resource=payload[4:q].decode("ascii","replace"); p=q+1
                common=struct.unpack_from("<13fI",payload,p)
                p += 56
                if tag == b"LOBJ":
                    placement = struct.unpack_from("<3f",payload,p)
                else:
                    placement = (payload[p:].decode("ascii","replace").rstrip("\0"), "", "")
                out.append((name,tag.decode(),typ,resource,*common,*placement,len(payload)))
            except Exception: out.append((name,tag.decode(),"PARSE_ERROR","",len(payload)))
for row in out: print("\t".join(str(x) for x in row))
print(f"records={len(out)} files={len(set(r[0] for r in out))}",file=sys.stderr)
