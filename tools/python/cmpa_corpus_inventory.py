#!/usr/bin/env python3
"""Inventory effective CMPA component-appearance resources."""
from __future__ import annotations
import argparse, csv, re, struct
from collections import Counter
from pathlib import Path
from scan_tre_sound_references import archive_records, expand

def archives(root: Path, cfg: Path):
    found=[]
    for line in cfg.read_text(errors='replace').splitlines():
        m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)",line,re.I)
        if m and (root/m.group(2)).exists(): found.append((int(m.group(1)),root/m.group(2)))
    return sorted(found,reverse=True)

def record(data: bytes, offset: int):
    tag,size=struct.unpack_from('>4sI',data,offset); end=offset+8+size
    if end>len(data): raise ValueError('record exceeds payload')
    return tag,data[offset+8:end],end

def parse(data: bytes):
    tag,outer,end=record(data,0)
    if tag!=b'FORM' or end!=len(data) or outer[:4]!=b'CMPA': raise ValueError('not CMPA')
    tag,version_body,end=record(outer,4)
    if tag!=b'FORM' or end!=len(outer): raise ValueError('bad version form')
    version=version_body[:4].decode('ascii','replace'); cursor=4
    tag,base,cursor=record(version_body,cursor)
    if tag!=b'FORM' or base[:4]!=b'APPR': raise ValueError('missing base APPR')
    parts=[]; auxiliary=[]
    while cursor<len(version_body):
        tag,payload,cursor=record(version_body,cursor)
        if tag==b'FORM':
            auxiliary.append(payload[:4].decode('ascii','replace'))
            continue
        if tag!=b'PART': raise ValueError(f'unexpected {tag!r}')
        stop=payload.find(b'\0')
        if stop<0 or len(payload)-stop-1!=48: raise ValueError('bad PART')
        name=payload[:stop].decode('ascii','replace').lower().replace('\\','/')
        parts.append((name,struct.unpack_from('<12f',payload,stop+1)))
    return version,parts,auxiliary

def main():
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);p.add_argument('cfg',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    winners=set(); rows=[]; versions=Counter(); suffixes=Counter(); counts=Counter(); auxiliaries=Counter(); sources=Counter(); errors=[]; tombstones=0
    for priority,archive in archives(a.root,a.cfg):
        for name,usize,kind,packed in archive_records(archive):
            if name in winners: continue
            winners.add(name)
            if not name.endswith('.cmp'): continue
            if not packed: tombstones+=1; continue
            try: version,parts,auxiliary=parse(expand(kind,packed,usize))
            except Exception as exc: errors.append((archive.name,name,str(exc)));continue
            versions[version]+=1;counts[len(parts)]+=1;sources[archive.name]+=1;auxiliaries.update(auxiliary)
            for index,(part,transform) in enumerate(parts):
                suffixes[Path(part).suffix]+=1;rows.append((archive.name,priority,name,version,index,part,*transform))
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open('w',newline='',encoding='utf-8') as h:
        w=csv.writer(h);w.writerow(('archive','priority','component','version','part','appearance',*[f'm{i}' for i in range(12)]));w.writerows(rows)
    print(f'effective_cmp={sum(versions.values())} tombstones={tombstones} versions={dict(versions)} parts={len(rows)} errors={len(errors)}')
    print(f'part_count_distribution={dict(sorted(counts.items()))}')
    print(f'auxiliary_forms={dict(auxiliaries)}')
    print(f'reference_suffixes={dict(suffixes)}')
    print(f'archives={dict(sources)}')
    for e in errors[:20]: print('ERROR\t'+'\t'.join(e))
if __name__=='__main__': main()
