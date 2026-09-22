#!/usr/bin/env python3
"""Inventory the four PEFT/0002 scalar controls in effective particle assets."""
from __future__ import annotations
import csv, re, struct, sys
from collections import Counter
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from scan_tre_sound_references import archive_records, expand

def items(data, start, end):
    pos = start
    while pos + 8 <= end:
        size = struct.unpack_from(">I", data, pos + 4)[0]
        stop = pos + 8 + size
        if stop > end or stop < pos + 8: return
        yield pos, data[pos:pos+4], stop
        pos = stop

def locate(data):
    outer = list(items(data, 0, len(data)))
    if not outer or outer[0][1] != b"FORM" or data[outer[0][0]+8:outer[0][0]+12] != b"PEFT": return None
    versions = list(items(data, outer[0][0]+12, outer[0][2]))
    if not versions or versions[0][1] != b"FORM" or data[versions[0][0]+8:versions[0][0]+12] != b"0002": return None
    for pos, kind, stop in items(data, versions[0][0]+12, versions[0][2]):
        if kind == b"0000":
            payload = data[pos+8:stop]
            if len(payload) < 20: raise ValueError("short PEFT/0002 count/scalar chunk")
            return struct.unpack_from("<Iffff", payload)
    raise ValueError("missing PEFT/0002 count/scalar chunk")

def main(root_value, cfg_value, output_value):
    root, cfg, output = Path(root_value), Path(cfg_value), Path(output_value)
    configured=[]
    for line in cfg.read_text(errors="replace").splitlines():
        m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)",line,re.I)
        if m: configured.append((int(m.group(1)),m.group(2)))
    winners=set(); rows=[]; errors=[]
    for _, archive_name in sorted(configured,reverse=True):
        archive=root/archive_name
        if not archive.exists(): continue
        for name,usize,kind,packed in archive_records(archive):
            if name in winners: continue
            winners.add(name)
            if not name.endswith('.prt') or not packed: continue
            try:
                result=locate(expand(kind,packed,usize))
                if result: rows.append((name,*result))
            except Exception as exc: errors.append((name,str(exc)))
    with output.open('w',newline='',encoding='utf-8') as handle:
        w=csv.writer(handle); w.writerow(('asset','group_count','initial_time_scale','time_scale_transition','sustaining_time_scale','effect_scale')); w.writerows(rows)
    print(f"records={len(rows)} errors={len(errors)}")
    for i,label in enumerate(('group_count','initial_time_scale','time_scale_transition','sustaining_time_scale','effect_scale'),1):
        c=Counter(row[i] for row in rows)
        print(label,len(c),c.most_common(12),sep='\t')
    for error in errors[:20]: print('ERROR',*error,sep='\t')

if __name__ == '__main__': main(*sys.argv[1:4])
