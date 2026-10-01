"""Read-only aggregate of one named shader's geometry in at most three pinned meshes."""
import argparse
import contextlib
import hashlib
import json
import math
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_static_mesh_slice import inspect, nodes
from verify_textured_mesh_bundle import Index, name


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root',type=Path)
    parser.add_argument('configuration',type=Path)
    parser.add_argument('source_manifest',type=Path)
    parser.add_argument('--configuration-sha256',required=True)
    parser.add_argument('--shader',required=True)
    args=parser.parse_args()
    assert hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper()==args.configuration_sha256.upper(), 'Configuration pin mismatch'
    assert args.source_manifest.stat().st_size<=262144, 'Manifest budget'
    records=json.loads(args.source_manifest.read_text(encoding='utf-8'))
    selected=[r for r in records if r['Path'].startswith('appearance/mesh/') and r['Path'].endswith('.msh')]
    assert 1<=len(selected)<=3 and len({r['Path'] for r in selected})==len(selected), 'Explicit selected mesh budget/uniqueness'
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(args.root,args.configuration)]
        for record in selected:
            for priority,index in indices:
                data=index.lookup(record['Path'])
                if data is not None:break
            else:raise ValueError('Missing selected mesh')
            assert priority==record['Priority'] and len(data)==record['Bytes'] and hashlib.sha256(data).hexdigest().upper()==record['Sha256'], 'Selected mesh identity mismatch'
            inspect(data)  # Existing independent grammar/vertex/index validation.
            root,=nodes(data);version,=root[1];sps_version,=version[1][1][1]
            points=[];triangles=0;primitive_count=0;degenerate=0;up=down=vertical=sloped=0
            for _,children in sps_version[1][1:]:
                if name(children[0][1])!=args.shader:continue
                for primitive_version,primitive in children[2:]:
                    info,vtxa,indx,*_=primitive;vtx_version,=vtxa[1];vinfo,vdata=vtx_version[1]
                    declaration,count=struct.unpack('<II',vinfo[1])
                    stride=len(vdata[1])//count
                    vertices=[struct.unpack_from('<3f',vdata[1],i*stride) for i in range(count)]
                    n,=struct.unpack_from('<I',indx[1]);width='I' if primitive_version==b'0000' else 'H'
                    values=[v&65535 for v in struct.unpack_from('<'+width*n,indx[1],4)]
                    primitive_count+=1;points.extend(vertices);triangles+=n//3
                    for i in range(0,n,3):
                        a,b,c=(vertices[values[i+j]] for j in range(3))
                        u=[b[j]-a[j] for j in range(3)];v=[c[j]-a[j] for j in range(3)]
                        normal=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
                        length=math.sqrt(sum(x*x for x in normal))
                        if length==0:degenerate+=1;continue
                        y=normal[1]/length
                        if abs(y)<1e-6:vertical+=1
                        elif y>1-1e-6:up+=1
                        elif y<-1+1e-6:down+=1
                        else:sloped+=1
            result=dict(member=record['Path'],sha256=record['Sha256'],priority=priority,shader=args.shader,
                        primitives=primitive_count,vertices=len(points),triangles=triangles,degenerate=degenerate,
                        normal_up=up,normal_down=down,normal_vertical=vertical,normal_sloped=sloped)
            if points:result['bounds']=dict(minimum=[min(p[i] for p in points) for i in range(3)],maximum=[max(p[i] for p in points) for i in range(3)])
            print(json.dumps(result))


if __name__=='__main__':main()
