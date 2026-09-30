"""Independent effective PRTO geometry/cell/reference reader; proprietary detail stays private."""
import argparse
import collections
import contextlib
import hashlib
import json
import math
import struct
import subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes


def payload(node):
    tag,value=node
    if isinstance(value,bytes):return value
    return b''.join((b'FORM' if isinstance(v,list) else t)+struct.pack('>I',len(payload((t,v)))+(4 if isinstance(v,list) else 0))+
        (t if isinstance(v,list) else b'')+payload((t,v)) for t,v in value)


def inspect(data):
    root,=nodes(data);assert root[0]==b'PRTO';version,=root[1];assert version[0] in (b'0001',b'0003',b'0004')
    datum,prts,cels,*tail=version[1];assert datum[0]==b'DATA' and len(datum[1])==8 and prts[0]==b'PRTS' and cels[0]==b'CELS'
    geometry_count,cell_count=struct.unpack('<II',datum[1]);assert geometry_count==len(prts[1]) and cell_count==len(cels[1])
    geometries=[]
    for tag,value in prts[1]:
        if version[0]==b'0004':
            assert tag==b'IDTL';gv,=value;assert gv[0]==b'0000';vertices,indices=gv[1];assert vertices[0]==b'VERT' and indices[0]==b'INDX'
            raw=vertices[1];assert len(raw)%12==0 and len(indices[1])%12==0;index_values=list(struct.unpack('<'+'I'*(len(indices[1])//4),indices[1]))
        else:
            assert tag==b'PRTL';n,=struct.unpack_from('<I',value);raw=value[4:];assert len(raw)==n*12
            index_values=[value for i in range(2,n) for value in (0,i-1,i)]
        assert all(math.isfinite(value) for value in struct.unpack('<'+'f'*(len(raw)//4),raw))
        assert all(value<len(raw)//12 for value in index_values)
        geometries.append({'vertex_hex':raw.hex().upper(),'indices':index_values})
    cells=[]
    def string(value,offset):
        end=value.index(0,offset);text=value[offset:end].decode('ascii');assert all(32<=ord(c)<=126 for c in text)
        return text,end+1
    for tag,values in cels[1]:
        assert tag==b'CELL';cv,=values;assert cv[0] in (b'0004',b'0005');datum,*children=cv[1];assert datum[0]==b'DATA'
        raw=datum[1];count,=struct.unpack_from('<I',raw);flag=raw[4];name,offset=string(raw,5);appearance,offset=string(raw,offset)
        floor_flag=raw[offset];offset+=1;floor=None
        if floor_flag:floor,offset=string(raw,offset)
        assert offset==len(raw)
        bounds=None
        if cv[0]==b'0005' and children and children[0][0] not in (b'PRTL',b'LGHT'):
            bounds=hashlib.sha256(payload(children.pop(0))).hexdigest().upper()
        references=[]
        for _ in range(count):
            tag,fields=children.pop(0);assert tag==b'PRTL';(pv,value),=fields;assert isinstance(value,bytes) and pv in (b'0002',b'0004',b'0005')
            offset=1 if pv==b'0005' else 0;leading=value[0] if offset else None
            solid=value[offset];geometry,=struct.unpack_from('<i',value,offset+1);winding=value[offset+5];target,=struct.unpack_from('<i',value,offset+6);offset+=10
            door='';transform_flag=None;transform_hex=None
            if pv!=b'0002':
                door,offset=string(value,offset);transform_flag=value[offset];offset+=1
                transform=value[offset:];assert len(transform)==48 and all(math.isfinite(v) for v in struct.unpack('<12f',transform));transform_hex=transform.hex().upper();offset+=48
            assert offset==len(value) and 0<=geometry<geometry_count
            references.append({'version':pv.decode(),'leading_byte':leading,'solid_byte':solid,'winding_byte':winding,
                'geometry_index':geometry,'target_cell':target,'door_name':door,'transform_flag':transform_flag,'transform_hex':transform_hex})
        assert all(tag==b'LGHT' for tag,_ in children)
        cells.append({'version':cv[0].decode(),'name':name,'raw_flag':flag,'appearance':appearance,'floor':floor,'bounds_sha256':bounds,
            'lights_sha256':[hashlib.sha256(value).hexdigest().upper() for _,value in children],'portals':references})
    graph=None
    if tail and tail[0][0]==b'PGRF':graph=hashlib.sha256(payload(tail.pop(0))).hexdigest().upper()
    crc,=tail;assert crc[0]==b'CRC ' and len(crc[1])==4
    return {'version':version[0].decode(),'crc':struct.unpack('<I',crc[1])[0],'geometries':geometries,'cells':cells,'path_graph_sha256':graph}


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('root',type=Path);parser.add_argument('configuration',type=Path)
    parser.add_argument('private_output',type=Path);parser.add_argument('--core-dll',type=Path);args=parser.parse_args()
    args.private_output.mkdir(parents=True,exist_ok=True);seen=set();records=[];counts=collections.Counter();versions=collections.Counter();side_counts=collections.Counter();target_mismatch=0
    with contextlib.ExitStack() as stack:
        for priority,path in archives(args.root,args.configuration):
            index=Index(stack.enter_context(path.open('rb')))
            for member,entry in index.entries.items():
                if not member.endswith('.pob') or member in seen:continue
                seen.add(member)
                if entry[1]==0:counts['masks']+=1;continue
                data=index.lookup(member)
                try:record=inspect(data)
                except Exception as failure:raise ValueError('Portal member '+member+': '+str(failure)) from failure
                record={'member':member,'priority':priority,'sha256':hashlib.sha256(data).hexdigest().upper(),**record};records.append(record)
                versions[record['version']]+=1;counts['cells']+=len(record['cells']);counts['geometries']+=len(record['geometries'])
                sides=collections.defaultdict(list)
                for cell_index,cell in enumerate(record['cells']):
                    for reference in cell['portals']:
                        counts['references']+=1;sides[reference['geometry_index']].append((cell_index,reference['target_cell']))
                        counts['door_names']+=bool(reference['door_name']);counts['door_transform_enabled']+=bool(reference['transform_flag'])
                for geometry in range(len(record['geometries'])):
                    links=sides[geometry];side_counts[len(links)]+=1
                    if len(links)==2:target_mismatch+=sum(target!=links[1-side][0] for side,(_,target) in enumerate(links))
    ordered=sorted(records,key=lambda record:record['member']);(args.private_output/'layouts.json').write_text(json.dumps(ordered,indent=2),encoding='utf-8')
    members=args.private_output/'members.txt';content=('\n'.join(record['member'] for record in ordered)+'\n').encode();members.write_bytes(content);digest=hashlib.sha256(content).hexdigest().upper()
    print('configuration_sha256='+hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper());print('members_sha256='+digest)
    print(f'live={len(records)} counts={dict(counts)} versions={dict(versions)} side_counts={dict(side_counts)} paired_target_mismatches={target_mismatch}',flush=True)
    if args.core_dll:
        result=subprocess.run(['dotnet',str(args.core_dll),'--portal-list',str(args.root),'--configuration',str(args.configuration),'--members',str(members),'--expected-members-sha256',digest],capture_output=True,text=True,check=True,timeout=60)
        (args.private_output/'native-portal-agreement.txt').write_text(result.stdout,encoding='utf-8')
        native={record['member']:record for record in map(json.loads,(line[7:] for line in result.stdout.splitlines() if line.startswith('portal=')))}
        if native!={record['member']:record for record in ordered}:raise ValueError('Native portal agreement mismatch')
        print(f'PASS independent PRTO files={len(records)} cells={counts["cells"]} geometries={counts["geometries"]} references={counts["references"]}')


if __name__=='__main__':main()
