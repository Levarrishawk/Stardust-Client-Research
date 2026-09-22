#!/usr/bin/env python3
"""Inventory the 28 common SD2D/0003 scalar words in effective TRE winners."""
from __future__ import annotations
import collections, csv, re, struct, sys
from pathlib import Path
from sound_overlay_inventory import records

OFFSETS = (0x1c,0x20,0x24,0x28,0x2c,0x30,0x34,0x38,0x3c,0x40,0x44,0x48,0x4c,0x50,0x54,0x58,
           0x70,0x74,0x78,0x7c,0x80,0x5c,0x60,0x64,0x68,0x6c,0x84,0x8c)
INTEGRAL = {0x2c,0x30,0x44,0x48,0x4c,0x50,0x54,0x58,0x70,0x5c,0x84}

def chunks(data, start=0, end=None):
    end=len(data) if end is None else end; p=start
    while p+8<=end:
        tag=data[p:p+4]; size=struct.unpack_from('>I',data,p+4)[0]; stop=p+8+size
        if stop>end: raise ValueError('bad IFF')
        if tag==b'FORM': yield from chunks(data,p+12,stop)
        else: yield tag,data[p+8:stop]
        p=stop

def main(root,cfg,out=None,values_out=None):
    root=Path(root); cfg=Path(cfg); priorities=[]
    for line in cfg.read_text(errors='replace').splitlines():
        m=re.search(r'searchTree_00_(\d+)\s*=\s*([^\s;]+)',line,re.I)
        if m: priorities.append((int(m.group(1)),m.group(2)))
    winners={}
    for priority,archive in sorted(priorities,reverse=True):
        for name,data in records(root/archive):
            if name not in winners: winners[name]=data
    counters={offset:collections.Counter() for offset in OFFSETS}; parsed=0; rows=[]
    for name,data in sorted(winners.items()):
        if data is None: continue
        payload=next((body for tag,body in chunks(data) if tag==b'0003'),None)
        if payload is None or len(payload)<112: continue
        words=payload[-112:]; parsed+=1; values=[]
        for i,offset in enumerate(OFFSETS):
            raw=words[i*4:(i+1)*4]
            value=struct.unpack('<i' if offset in INTEGRAL else '<f',raw)[0]
            counters[offset][value]+=1; values.append(value)
        rows.append((name,*values))
    print('parsed',parsed)
    for offset in OFFSETS:
        c=counters[offset]; common=', '.join(f'{v}:{n}' for v,n in c.most_common(12))
        print(f'+0x{offset:02X}\tunique={len(c)}\t{common}')
    if out:
        with open(out,'w',newline='',encoding='utf-8') as handle:
            writer=csv.writer(handle); writer.writerow(('offset','kind','value','count'))
            for offset in OFFSETS:
                kind='int32' if offset in INTEGRAL else 'float32'
                for value,count in counters[offset].most_common(): writer.writerow((f'+0x{offset:02X}',kind,value,count))
    if values_out:
        with open(values_out,'w',newline='',encoding='utf-8') as handle:
            writer=csv.writer(handle); writer.writerow(('name',*(f'+0x{o:02X}' for o in OFFSETS))); writer.writerows(rows)

if __name__=='__main__': main(*sys.argv[1:])
