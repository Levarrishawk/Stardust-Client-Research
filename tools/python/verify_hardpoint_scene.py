"""Independently compose raw APPR hardpoints through one pinned static appearance/cell graph."""
import argparse,contextlib,hashlib,json,math,struct,subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives,parse as component
from verify_static_mesh_slice import nodes
from verify_textured_mesh_bundle import Index,swg_crc
from verify_portal_layout import inspect as portal

IDENTITY=(1,0,0,0,0,1,0,0,0,0,1,0)
def compose(parent,local):
    result=[]
    for row in range(3):
        for col in range(3):result.append(sum(parent[row*4+k]*local[k*4+col] for k in range(3)))
        result.append(parent[row*4+3]+sum(parent[row*4+k]*local[k*4+3] for k in range(3)))
    return tuple(result)

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration'):p.add_argument(name,type=Path)
    p.add_argument('member');p.add_argument('sha256');p.add_argument('core_dll',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    private=(Path(__file__).resolve().parents[2]/'local-private').resolve();assert a.output.resolve().is_relative_to(private),'Detailed placements must stay private'
    sources={};instances=0;placements=[];omissions=[]
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        def read(path):
            if path in sources:return sources[path][1]
            for priority,index in indices:
                data=index.lookup(path)
                if data is not None:
                    assert data,'Masked appearance';sources[path]=(priority,data);return data
            raise ValueError('Missing appearance '+path)
        def normalize(path):
            path=path.lower().replace('\\','/')
            return path if path.startswith('appearance/') else 'appearance/'+path
        def template(path,ancestors=()):
            assert path not in ancestors and len(ancestors)<32
            data=read(path);root,=nodes(data);kind=root[0];version,=root[1]
            if kind==b'APT ':
                target=next(v for tag,v in version[1] if tag==b'NAME').rstrip(b'\0').decode('ascii')
                terminal=template(normalize(target),ancestors+(path,));return (terminal[0],terminal[1])
            if kind not in (b'MESH',b'DTLA',b'CMPA'):raise ValueError('Unsupported appearance family '+repr(kind))
            base=next(value for tag,value in version[1] if tag==b'APPR');bv,=base;assert bv[0]==b'0003';records=[]
            for tag,children in bv[1]:
                if tag==b'HPTS':
                    for rt,payload in children:
                        assert rt==b'HPNT';name=payload[48:-1].decode('ascii');assert payload[-1]==0
                        records.append((name,swg_crc(payload[48:-1]),struct.unpack_from('<12f',payload)))
            children=[]
            if kind==b'CMPA':
                for ordinal,(child,transform) in enumerate(component(data)[1]):
                    child=normalize(child)
                    try:child_template=template(child,ancestors+(path,))
                    except ValueError as failure:
                        if not str(failure).startswith(('Unsupported appearance family','Missing appearance')):raise
                        omissions.append((path,ordinal,child));continue
                    children.append((ordinal,child,transform,child_template))
            elif kind==b'DTLA':
                info=next(value for tag,value in version[1] if tag==b'INFO');table=next(value for tag,value in version[1] if tag==b'DATA')
                paths={struct.unpack_from('<I',payload)[0]:normalize(payload[4:-1].decode('ascii')) for tag,payload in table if tag==b'CHLD'}
                for ordinal,offset in enumerate(range(0,len(info),12)):
                    child=paths[struct.unpack_from('<I',info,offset)[0]];children.append((ordinal,child,IDENTITY,template(child,ancestors+(path,))))
            return records,children
        def visit(path,t,world,chain):
            nonlocal instances
            ordinal=instances;instances+=1
            for authored,(name,checksum,matrix) in enumerate(t[0]):
                placements.append(dict(instance=ordinal,owner=path,children=chain,authored=authored,name=name,checksum=checksum,matrix=compose(world,matrix)))
            for index,child,local,ct in t[1]:visit(child,ct,compose(world,local),chain+[index])
        root=read(a.member);assert hashlib.sha256(root).hexdigest().upper()==a.sha256.upper()
        if root[8:12]==b'PRTO':
            instances=1
            for ci,cell in enumerate(portal(root)['cells']):
                if cell['appearance']:
                    path=normalize(cell['appearance']);visit(path,template(path),IDENTITY,[ci])
        else:visit(a.member,template(a.member),IDENTITY,[])
        native=subprocess.run(['dotnet',str(a.core_dll),'--hardpoint-scene',str(a.root),'--configuration',str(a.configuration),'--member',a.member,'--expected-sha256',a.sha256],capture_output=True,text=True,check=True,timeout=60).stdout
        actual=[json.loads(line.split('=',1)[1]) for line in native.splitlines() if line.startswith('hardpoint_placement=')]
        assert len(actual)==len(placements),'Placement cardinality disagreement'
        maximum_error=0.0
        for expected,observed in zip(placements,actual):
            for key in ('instance','owner','children','authored','name','checksum'):assert expected[key]==observed[key],(key,expected,observed)
            for x,y in zip(expected['matrix'],observed['matrix']):
                assert math.isclose(x,y,rel_tol=2e-6,abs_tol=2e-5),('Composed transform disagreement',x,y)
                maximum_error=max(maximum_error,abs(x-y))
        assert f'PASS hardpoint_scene instances={instances} placements={len(placements)}' in native
        for path,(priority,data) in sources.items():assert f'source={path} priority={priority} bytes={len(data)} sha256={hashlib.sha256(data).hexdigest().upper()}' in native
        a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(dict(member=a.member,sha256=a.sha256,configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),instances=instances,placements=placements,omissions=omissions,native=actual),indent=2),encoding='utf-8')
        print(f'PASS hardpoint_scene sources={len(sources)} instances={instances} placements={len(placements)} matched_float_fields={12*len(placements)} local_omissions={len(omissions)} maximum_absolute_error={maximum_error:.9g}')
if __name__=='__main__':main()
