from __future__ import annotations
import pathlib,re,struct,sys,zlib,collections

def ex(k,d,n):
    x=d[:n] if k==0 else zlib.decompress(d) if k==2 else None
    if x is None or len(x)!=n: raise ValueError
    return x
root,cfg=pathlib.Path(sys.argv[1]),pathlib.Path(sys.argv[2]); suffix=sys.argv[3] if len(sys.argv)>3 else '.swh'; outer=(sys.argv[4] if len(sys.argv)>4 else 'SWSH').encode(); search=[]
for line in cfg.read_text(errors='replace').splitlines():
    m=re.search(r'searchTree_00_(\d+)\s*=\s*([^\s;]+)',line,re.I)
    if m: search.append((int(m.group(1)),m.group(2)))
seen=set(); versions=collections.Counter(); sizes=collections.Counter(); samples=[]; anomalies=[]
for pri,an in sorted(search,reverse=True):
    p=root/an
    if not p.exists(): continue
    raw=p.read_bytes()
    if raw[:8]!=b'EERT5000': continue
    cnt,off,rk,rs,nk,ns,nu=struct.unpack_from('<7I',raw,8); rec=ex(rk,raw[off:off+rs],cnt*24); names=ex(nk,raw[off+rs:off+rs+ns],nu)
    for i in range(cnt):
        _,usize,po,k,cs,no=struct.unpack_from('<6I',rec,i*24); ne=names.find(b'\0',no); name=names[no:ne].decode('ascii','replace').lower().replace('\\','/')
        if not name.endswith(suffix) or name in seen: continue
        seen.add(name)
        try:d=ex(k,raw[po:po+cs],usize)
        except Exception:continue
        if len(d)>=24 and d[:4]==b'FORM' and d[8:12]==outer and d[12:16]==b'FORM':
            ver=d[20:24].decode('ascii','replace'); versions[ver]+=1; sizes[len(d)]+=1
            if len(samples)<12:samples.append((name,an,pri,len(d),ver))
        else: anomalies.append((name,an,pri,len(d),d[:24].hex()))
print('files',len(seen));print('versions',dict(versions));print('sizes',sizes.most_common(20))
for x in samples:print('\t'.join(map(str,x)))
for x in anomalies:print('ANOMALY\t'+'\t'.join(map(str,x)))
