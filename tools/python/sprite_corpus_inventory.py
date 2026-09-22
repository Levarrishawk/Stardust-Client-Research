#!/usr/bin/env python3
"""Inventory effective SPRT/QUAD v0000 appearance payloads."""
from __future__ import annotations
import argparse,csv,re,struct
from collections import Counter
from pathlib import Path
from scan_tre_sound_references import archive_records,expand
def configured(root,cfg):
    result=[]
    for line in cfg.read_text(errors='replace').splitlines():
        m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)",line,re.I)
        if m and (root/m.group(2)).exists():result.append((int(m.group(1)),root/m.group(2)))
    return sorted(result,reverse=True)
def parse(data):
    if data[:4]!=b'FORM':raise ValueError('not FORM')
    size=struct.unpack_from('>I',data,4)[0]
    if size+8!=len(data):raise ValueError('outer size')
    tag=data[8:12].decode('ascii','replace')
    version=data[12:16].decode('ascii','replace'); n=struct.unpack_from('>I',data,16)[0];payload=data[20:20+n]
    if version!='0000' or len(payload)!=n:raise ValueError('version/size')
    values=struct.unpack_from('<8f',payload,0); end=payload.find(b'\0',32)
    if end<0 or end+1!=len(payload):raise ValueError('shader string')
    return tag,version,values,payload[32:end].decode('ascii','replace').lower().replace('\\','/')
def main():
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);p.add_argument('cfg',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    winners=set();rows=[];forms=Counter();vectors=Counter();scalars=Counter();colors=Counter();shaders=Counter();errors=[];tomb=0
    for priority,archive in configured(a.root,a.cfg):
        for name,usize,kind,packed in archive_records(archive):
            if name in winners:continue
            winners.add(name)
            if not name.endswith('.spr'):continue
            if not packed:tomb+=1;continue
            try:tag,version,v,shader=parse(expand(kind,packed,usize))
            except Exception as exc:errors.append((archive.name,name,str(exc)));continue
            forms[tag]+=1;vectors[tuple(v[:3])]+=1;scalars[v[3]]+=1;colors[tuple(v[4:])]+=1;shaders[shader]+=1
            rows.append((archive.name,priority,name,tag,version,*v,shader))
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open('w',newline='',encoding='utf-8') as h:
        w=csv.writer(h);w.writerow(('archive','priority','name','form','version','v0','v1','v2','scalar','c0','c1','c2','c3','shader'));w.writerows(rows)
    print(f'effective_spr={len(rows)} tombstones={tomb} forms={dict(forms)} errors={len(errors)} unique_shaders={len(shaders)}')
    print('vectors='+repr(vectors.most_common(12)));print('scalars='+repr(scalars.most_common(12)));print('colors='+repr(colors.most_common(12)))
    for e in errors[:20]:print('ERROR\t'+'\t'.join(e))
if __name__=='__main__':main()
