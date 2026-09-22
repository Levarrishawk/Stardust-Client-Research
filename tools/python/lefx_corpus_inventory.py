#!/usr/bin/env python3
"""Inventory effective LEFX lightning appearance records."""
from __future__ import annotations
import argparse,csv,re,struct
from collections import Counter
from pathlib import Path
from scan_tre_sound_references import archive_records,expand

def configured(root,cfg):
    out=[]
    for line in cfg.read_text(errors='replace').splitlines():
        m=re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)",line,re.I)
        if m and (root/m.group(2)).exists():out.append((int(m.group(1)),root/m.group(2)))
    return sorted(out,reverse=True)

def nodes(data,start,end):
    pos=start
    while pos<end:
        tag=data[pos:pos+4].decode('ascii','replace'); size=struct.unpack_from('>I',data,pos+4)[0]
        body=pos+8; stop=body+size
        if stop>end:raise ValueError(f'{tag} overruns parent')
        if tag=='FORM':
            form=data[body:body+4].decode('ascii','replace')
            yield ('FORM',form,pos,stop,data[body+4:stop])
        else:yield ('CHUNK',tag,pos,stop,data[body:stop])
        # SWG IFF records are packed to their declared size; odd chunks are not
        # followed by RIFF-style pad bytes.
        pos=stop
    if pos!=end:raise ValueError('alignment')

def parse(data):
    if data[:4]!=b'FORM':raise ValueError('not FORM')
    size=struct.unpack_from('>I',data,4)[0]
    if size+8!=len(data):raise ValueError('outer size')
    if data[8:12]!=b'LEFX':raise ValueError('not LEFX')
    top=list(nodes(data,12,len(data)))
    if len(top)!=1 or top[0][0]!='FORM':raise ValueError('version form')
    version=top[0][1]; children=list(nodes(data,top[0][2]+12,top[0][3]))
    parts=[];shader=''
    for x in children:
        if x[0]=='FORM':
            nested=list(nodes(data,x[2]+12,x[3])); nested_version=nested[0][1] if nested else '?'
            parts.append(f'FORM:{x[1]}:{nested_version}')
            if x[1]=='PTEX' and nested:
                p=nested[0][4];end=p.find(b'\0')
                if end<0:raise ValueError('PTEX resource string')
                shader=p[:end].decode('ascii','replace').lower().replace('\\','/')
        else:parts.append(x[1])
    sig='/'.join(parts)
    chunks=[x for x in children if x[0]=='CHUNK']
    if len(chunks)!=1:raise ValueError('expected one scalar chunk')
    payload=chunks[0][4]
    return version,sig,chunks[0][1],payload,shader

def scalar_fields(version,payload):
    pos=0; nums=[];u32=[]
    def f():
        nonlocal pos
        value=struct.unpack_from('<f',payload,pos)[0];pos+=4;nums.append(value)
    def s():
        nonlocal pos
        end=payload.find(b'\0',pos)
        if end<0:raise ValueError('unterminated string')
        value=payload[pos:end].decode('ascii','replace').lower().replace('\\','/');pos=end+1;return value
    def u():
        nonlocal pos
        value=struct.unpack_from('<I',payload,pos)[0];pos+=4;u32.append(value)
    f(); refs=(s(),s(),s())
    if version=='0000':
        for _ in range(15):f()
        flag=''
    elif version=='0001':
        f();u();u();
        flag=payload[pos];pos+=1
        for _ in range(14):f()
    elif version=='0002':
        u();u()
        for _ in range(18):f()
        flag=''
    else:raise ValueError('unsupported version')
    if pos!=len(payload):raise ValueError(f'{version} scalar trailing bytes {len(payload)-pos}')
    return refs,flag,u32,nums

def main():
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);p.add_argument('cfg',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    winners=set();rows=[];versions=Counter();sigs=Counter();sizes=Counter();refs=Counter();shaders=Counter();flags=Counter();tomb=0;errors=[]
    for priority,archive in configured(a.root,a.cfg):
        for name,usize,kind,packed in archive_records(archive):
            if name in winners:continue
            winners.add(name)
            if not name.endswith('.ltn'):continue
            if not packed:tomb+=1;continue
            try:version,sig,chunk,payload,shader=parse(expand(kind,packed,usize))
            except Exception as exc:errors.append((archive.name,name,str(exc)));continue
            strings,flag,u32,numerics=scalar_fields(version,payload)
            versions[version]+=1;sigs[sig]+=1;sizes[(version,len(payload))]+=1;refs.update(strings);shaders[shader]+=1;flags[(version,flag)]+=1
            rows.append((archive.name,priority,name,version,sig,shader,chunk,len(payload),*strings,flag,' '.join(map(str,u32)),' '.join(f'{x:.9g}' for x in numerics)))
    a.output.parent.mkdir(parents=True,exist_ok=True)
    with a.output.open('w',newline='',encoding='utf-8') as h:
        w=csv.writer(h);w.writerow(('archive','priority','name','version','signature','ptex_shader','scalar_chunk','scalar_bytes','start_appearance','end_appearance','optional_sound','v1_legacy_byte','attachment_transform_flags','numeric_f32'));w.writerows(rows)
    print(f'effective_ltn={len(rows)} tombstones={tomb} versions={dict(versions)} errors={len(errors)}')
    missing=sorted(x for x in list(refs)+list(shaders) if x and x not in winners)
    print('signatures='+repr(sigs));print('payload_sizes='+repr(sizes));print('v1_legacy_bytes='+repr(flags));print('references='+repr(refs.most_common(30)));print('ptex_shaders='+repr(shaders));print('missing_references='+repr(missing))
    for e in errors[:20]:print('ERROR\t'+'\t'.join(e))
if __name__=='__main__':main()
