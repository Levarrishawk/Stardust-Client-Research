"""Independently verify primary DTLA APPR extents and PIVT bytes from an existing effective inventory."""
import argparse
import collections
import contextlib
import csv
import hashlib
import math
import struct
import subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('inventory',type=Path)
    p.add_argument('core_dll',type=Path);p.add_argument('private_output',type=Path);a=p.parse_args()
    with a.inventory.open(encoding='utf-8',newline='') as stream:members=sorted({row['name'] for row in csv.DictReader(stream)})
    a.private_output.mkdir(parents=True,exist_ok=True)
    member_file=a.private_output/'extent-members.txt';member_bytes=('\n'.join(members)+'\n').encode('utf-8');member_file.write_bytes(member_bytes)
    output=subprocess.run(['dotnet',str(a.core_dll),'--extent-list',str(a.root),'--configuration',str(a.configuration),
        '--members',str(member_file),'--expected-members-sha256',hashlib.sha256(member_bytes).hexdigest().upper()],capture_output=True,text=True,check=True,timeout=60).stdout
    (a.private_output/'native-extent-agreement.txt').write_text(output,encoding='utf-8')
    counts=collections.Counter();pivots=collections.Counter();distinct_centers=0;nonzero_centers=0;material_differences=0;maximum_difference=0.0
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        for member in members:
            for priority,index in indices:
                blob=index.lookup(member)
                if blob is not None:break
            else:raise ValueError('Missing extent input')
            root,=nodes(blob);assert root[0]==b'DTLA';version,=root[1];appr=version[1][0];assert appr[0]==b'APPR'
            base,=appr[1];assert base[0]==b'0003';extent=base[1][0];kind=extent[0].decode('ascii')
            pivot,=[value[0] for tag,value in version[1] if tag==b'PIVT'] or ['absent'];pivots[(version[0].decode(),str(pivot))]+=1
            sphere_bits=box_bits='none'
            if kind=='NULL':assert extent[1]==[]
            elif kind=='EXBX':
                box_version,=extent[1];assert box_version[0]==b'0001';sphere,box=box_version[1]
                assert sphere[0]==b'EXSP' and box[0]==b'BOX '
                sphere_version,=sphere[1];assert sphere_version[0]==b'0001';sphr,=sphere_version[1];assert sphr[0]==b'SPHR'
                values=struct.unpack('<4f',sphr[1]);bounds=struct.unpack('<6f',box[1])
                assert all(math.isfinite(v) for v in values+bounds) and values[3]>=0 and all(bounds[i]>=bounds[i+3] for i in range(3))
                sphere_bits=''.join(f'{v:08X}' for v in struct.unpack('<4I',sphr[1]));box_bits=''.join(f'{v:08X}' for v in struct.unpack('<6I',box[1]))
                nonzero_centers+=any(v!=0 for v in values[:3])
                difference=max(abs(values[i]-(bounds[i]+bounds[i+3])*0.5) for i in range(3))
                distinct_centers+=difference!=0;material_differences+=difference>1e-4;maximum_difference=max(maximum_difference,difference)
            else:raise ValueError('Unexpected deployed primary extent')
            counts[kind]+=1
            expected=f'extent_member={member} priority={priority} sha256={hashlib.sha256(blob).hexdigest().upper()} kind={kind} sphere_bits={sphere_bits} box_bits={box_bits} pivot={pivot}'
            if expected not in output:raise ValueError('Native/reference primary extent mismatch')
    print(f'configuration_sha256={hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper()} member_list_sha256={hashlib.sha256(member_bytes).hexdigest().upper()}')
    print(f'PASS members={len(members)} extents={dict(counts)} pivots={dict(pivots)} nonzero_sphere_centers={nonzero_centers} sphere_center_differs_from_box_midpoint={distinct_centers} difference_above_1e4_inverse={material_differences} maximum_center_linf_difference={maximum_difference:g}')


if __name__=='__main__':main()
