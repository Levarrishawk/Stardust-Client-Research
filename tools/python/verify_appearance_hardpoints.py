"""Discover and independently parse effective APPR/0003 hardpoints in static appearance families."""
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
from verify_textured_mesh_bundle import swg_crc


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root',type=Path);parser.add_argument('configuration',type=Path)
    parser.add_argument('private_output',type=Path);parser.add_argument('--core-dll',type=Path)
    args=parser.parse_args();args.private_output.mkdir(parents=True,exist_ok=True)
    seen=set();records=[];counts=collections.Counter();versions=collections.Counter();duplicates=0;maximum_name=0;different_duplicates=0
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(args.root,args.configuration)]
        for priority,index in indices:
            for member,record in index.entries.items():
                if not member.endswith(('.msh','.lod','.cmp')) or member in seen:continue
                seen.add(member)
                if record[1]==0:counts['tombstones']+=1;continue
                data=index.lookup(member)
                if data[:4]!=b'FORM':counts['non_iff_suffix']+=1;continue
                try:root,=nodes(data)
                except ValueError as failure:
                    raise ValueError(f'Invalid IFF member={member} priority={priority} size={len(data)}: {failure}') from failure
                if len(root[1])!=1:counts['other_outer_layout']+=1;continue
                version,=root[1]
                versions[(root[0].decode(),version[0].decode())]+=1
                bases=[child for child in version[1] if child[0]==b'APPR']
                if len(bases)!=1:counts['no_single_appr']+=1;continue
                base,=bases[0][1]
                if base[0]!=b'0003':counts['other_appr_version']+=1;continue
                counts['appr_0003']+=1
                forms=[child for child in base[1] if child[0]==b'HPTS']
                if not forms:counts['no_hpts']+=1;continue
                if len(forms)!=1:raise ValueError('Duplicate HPTS form')
                hardpoints=[]
                for tag,payload in forms[0][1]:
                    if tag!=b'HPNT' or len(payload)<49:raise ValueError('Unexpected hardpoint record')
                    values=struct.unpack_from('<12f',payload)
                    if not all(math.isfinite(value) for value in values):raise ValueError('Nonfinite hardpoint transform')
                    name=payload[48:]
                    if not name or name[-1]!=0 or 0 in name[:-1] or any(c<32 or c>126 for c in name[:-1]):raise ValueError('Invalid hardpoint string slice')
                    maximum_name=max(maximum_name,len(name)-1)
                    hardpoints.append({'name':name[:-1].decode('ascii'),'name_checksum':swg_crc(name[:-1]),'transform_hex':payload[:48].hex().upper()})
                duplicates+=len(hardpoints)-len({hp['name'] for hp in hardpoints})
                groups=collections.defaultdict(list)
                for hp in hardpoints:groups[hp['name']].append(hp['transform_hex'])
                different_duplicates+=sum(len(set(values))>1 for values in groups.values())
                counts['nonempty_hpts_files']+=bool(hardpoints)
                counts['hpts_files']+=1;counts['hardpoints']+=len(hardpoints)
                sorted_indices=sorted(range(len(hardpoints)),key=lambda i:(hardpoints[i]['name_checksum'],hardpoints[i]['name'],i))
                records.append({'member':member,'priority':priority,'sha256':hashlib.sha256(data).hexdigest().upper(),'hardpoints':hardpoints,
                    'sorted_indices':sorted_indices,'duplicate_names':sorted(name for name,values in groups.items() if len(values)>1)})
    ordered=sorted(records,key=lambda record:record['member'])
    (args.private_output/'hardpoints.json').write_text(json.dumps(ordered,indent=2),encoding='utf-8')
    members=args.private_output/'members.txt';member_bytes=('\n'.join(record['member'] for record in ordered)+'\n').encode();members.write_bytes(member_bytes)
    manifest_hash=hashlib.sha256(member_bytes).hexdigest().upper()
    print('configuration_sha256='+hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper())
    print('member_list_sha256='+manifest_hash)
    print('counts='+json.dumps(dict(counts),sort_keys=True));print('versions='+str(dict(versions)))
    print(f'duplicate_extra_records={duplicates} duplicate_groups_with_different_transforms={different_duplicates} maximum_name_bytes={maximum_name}',flush=True)
    if args.core_dll:
        result=subprocess.run(['dotnet',str(args.core_dll),'--hardpoint-list',str(args.root),'--configuration',str(args.configuration),
            '--members',str(members),'--expected-members-sha256',manifest_hash],capture_output=True,text=True,check=True,timeout=60)
        (args.private_output/'native-hardpoint-agreement.txt').write_text(result.stdout,encoding='utf-8')
        native={record['member']:record for record in map(json.loads,(line[10:] for line in result.stdout.splitlines() if line.startswith('hardpoint=')))}
        if native!={record['member']:record for record in ordered}:raise ValueError('Native hardpoint inventory mismatch')
        print(f'PASS: independent APPR hardpoints files={len(records)} records={counts["hardpoints"]} floats={counts["hardpoints"]*12}')


if __name__=='__main__':main()
