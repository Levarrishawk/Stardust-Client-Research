#!/usr/bin/env python3
"""Trace configured CPIT frame APTs through immediate DTLA child paths."""
from __future__ import annotations
import argparse,csv,re,sys
from collections import Counter
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records

def read_rows(path):
    with path.open(newline='',encoding='utf-8') as f:
        return list(csv.DictReader(f))

def main():
    p=argparse.ArgumentParser()
    for key in ('root','config','cpit','apt','dtla','output'):p.add_argument(key,type=Path)
    a=p.parse_args()
    archives=[]
    for line in a.config.read_text(errors='replace').splitlines():
        m=re.search(r'searchTree_00_(\d+)\s*=\s*([^\s;]+)',line,re.I)
        if m and (a.root/m.group(2)).exists():archives.append((int(m.group(1)),a.root/m.group(2)))
    winners={}
    for priority,archive in sorted(archives,reverse=True):
        for name,usize,kind,packed in archive_records(archive):
            if name not in winners:winners[name]=(archive.name,priority,bool(packed))
    apt={r['apt']:r for r in read_rows(a.apt)}
    dtla={r['name']:r for r in read_rows(a.dtla)}
    rows=[]
    for cpit in read_rows(a.cpit):
        frame=cpit['frame']
        if not frame:continue
        redirect=apt.get(frame)
        target=redirect['target'] if redirect else ''
        children=[]
        if target in dtla:
            children=[r if r.startswith('appearance/') else 'appearance/'+r for r in dtla[target]['child_refs'].split(';') if r]
        statuses=[('live' if winners[c][2] else 'masked') if c in winners else 'absent' for c in children]
        rows.append(dict(cpit=cpit['name'],frame=frame,target=target,target_suffix=Path(target).suffix,
            target_present=str(bool(target in winners and winners[target][2])).lower(),lod_children=len(children),
            live_children=statuses.count('live'),masked_children=statuses.count('masked'),absent_children=statuses.count('absent'),
            missing_children=';'.join(c for c,s in zip(children,statuses) if s!='live')))
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open('w',newline='',encoding='utf-8') as f:
        w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
    print('frames',len(rows),'targets',dict(Counter(r['target_suffix'] for r in rows)))
    print('target_missing',sum(r['target_present']=='false' for r in rows))
    print('lods',sum(r['target_suffix']=='.lod' for r in rows),'lods_with_missing_children',sum(r['absent_children']+r['masked_children']>0 for r in rows))
    print('children',sum(r['lod_children'] for r in rows),'live',sum(r['live_children'] for r in rows),'masked',sum(r['masked_children'] for r in rows),'absent',sum(r['absent_children'] for r in rows))
    for r in rows:
        if r['absent_children']+r['masked_children']>0:print('MISSING',r['cpit'],r['missing_children'])

if __name__=='__main__':main()
