#!/usr/bin/env python3
"""Audit effective APT redirects with correct uncompressed TRE record lengths."""
from __future__ import annotations
import argparse,csv,re,struct,sys
from collections import Counter
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records,expand

def node(blob,at,limit):
    if at+8>limit:raise ValueError('short header')
    tag,size=struct.unpack_from('>4sI',blob,at);end=at+8+size
    if end>limit:raise ValueError('record overrun')
    return tag,at+8,end

def parse_apt(blob):
    tag,body,end=node(blob,0,len(blob))
    if tag!=b'FORM' or blob[body:body+4]!=b'APT ' or end!=len(blob):raise ValueError('not APT FORM')
    tag,ver,end=node(blob,body+4,end)
    if tag!=b'FORM' or end!=len(blob):raise ValueError('version FORM')
    version=blob[ver:ver+4].decode('ascii','replace')
    tag,start,stop=node(blob,ver+4,end)
    if tag!=b'NAME' or stop!=end:raise ValueError('NAME chunk')
    raw=blob[start:stop]
    if not raw or raw[-1]!=0 or b'\0' in raw[:-1]:raise ValueError('NAME string')
    return version,raw[:-1].decode('ascii','replace').lower().replace('\\','/')

def main():
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);p.add_argument('cfg',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    archives=[]
    for line in a.cfg.read_text(errors='replace').splitlines():
        m=re.search(r'searchTree_00_(\d+)\s*=\s*([^\s;]+)',line,re.I)
        if m and (a.root/m.group(2)).exists():archives.append((int(m.group(1)),a.root/m.group(2)))
    winners={};raw_apt=[];all_count=Counter()
    for priority,archive in sorted(archives,reverse=True):
        for name,usize,kind,packed in archive_records(archive):
            if name in winners:continue
            winners[name]=(archive.name,priority,usize,kind,len(packed),packed if name.endswith('.apt') else b'')
            all_count[Path(name).suffix]+=1
            if name.endswith('.apt'):raw_apt.append(name)
    rows=[];versions=Counter();targets=Counter();errors=[];empty=[]
    for name in raw_apt:
        archive,priority,usize,kind,stored,blob=winners[name]
        if not blob:empty.append((name,archive,usize));continue
        try:version,target=parse_apt(expand(kind,blob,usize))
        except Exception as e:errors.append((name,archive,str(e)));continue
        versions[version]+=1;targets[Path(target).suffix]+=1
        found=target in winners and winners[target][4]>0
        rows.append((name,archive,priority,version,target,found))
    graph={r[0]:r[4] for r in rows};indirect=[r for r in rows if r[4].endswith('.apt')];unresolved=[r for r in rows if not r[5]]
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open('w',newline='',encoding='utf-8') as h:
        w=csv.writer(h);w.writerow(('apt','archive','priority','version','target','target_present'));w.writerows(rows)
    print('configured',len(archives),'effective_all',len(winners),'effective_apt',len(raw_apt),'valid_apt',len(rows),'empty_apt',len(empty),'parse_errors',len(errors))
    for r in empty:print('EMPTY_APT',*r,sep='\t')
    print('versions',dict(versions),'target_suffixes',dict(targets))
    print('unresolved_targets',len(unresolved),'redirect_to_apt',len(indirect))
    for r in unresolved[:20]:print('UNRESOLVED',r[0],r[4],'zero-payload' if r[4] in winners else 'absent',sep='\t')
    for r in indirect[:20]:print('APT_TARGET',r[0],r[4],sep='\t')
    for e in errors[:20]:print('ERROR',*e,sep='\t')
if __name__=='__main__':main()
