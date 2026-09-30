"""Independent effective FLOR geometry and opaque metadata agreement; private output only."""
import argparse,collections,contextlib,hashlib,json,math,struct,subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_portal_layout import payload

def inspect(data):
    root,=nodes(data);assert root[0]==b'FLOR';v,=root[1];assert v[0] in (b'0003',b'0004',b'0005',b'0006')
    vert,tris,*optional=v[1];assert vert[0]==b'VERT' and tris[0]==b'TRIS';p=vert[1];r=tris[1]
    if v[0]==b'0006':
        n,=struct.unpack_from('<I',p);p=p[4:];assert len(p)==12*n
        n,=struct.unpack_from('<I',r);r=r[4:];assert len(r)==60*n
    assert len(p)%12==0 and len(r)%60==0;nv=len(p)//12;nt=len(r)//60
    assert all(math.isfinite(x) for x in struct.unpack('<'+'f'*(len(p)//4),p))
    ids=[];flags=collections.Counter();solid=collections.Counter();portals=collections.Counter();edges=[]
    for offset in range(0,len(r),60):
        row=r[offset:offset+60];assert all(0<=i<nv for i in struct.unpack_from('<3i',row));assert all(i==-1 or 0<=i<nt for i in struct.unpack_from('<3i',row,16))
        assert all(math.isfinite(x) for x in struct.unpack_from('<3f',row,28));ids.append(struct.unpack_from('<I',row,12)[0]);flags.update(row[40:43]);solid.update([row[43]]);portals.update(struct.unpack_from('<3i',row,48))
    hashes={k:None for k in ('tree_sha256','graph_sha256','legacy_nodes_sha256','legacy_edges_sha256')};boundary=b''
    for tag,value in optional:
        if tag==b'BEDG':
            n,=struct.unpack_from('<I',value);boundary=value[4:];assert len(boundary)==9*n
            for offset in range(0,len(boundary),9):
                t,e,f=struct.unpack_from('<iiB',boundary,offset);assert 0<=t<nt and e>=0;edges.append(e)
        else:
            k={b'BTRE':'tree_sha256',b'PGRF':'graph_sha256',b'PNOD':'legacy_nodes_sha256',b'PEDG':'legacy_edges_sha256'}[tag];assert hashes[k] is None;hashes[k]=hashlib.sha256(payload((tag,value))).hexdigest().upper()
    geometry=struct.pack('<I',nv)+p+struct.pack('<I',nt)+r+struct.pack('<I',len(boundary)//9)+boundary
    return dict(version=v[0].decode(),vertices=nv,triangles=nt,boundary_edges=len(boundary)//9,geometry_sha256=hashlib.sha256(geometry).hexdigest().upper(),**hashes,diagnostics=len(ids)-len(set(ids))),flags,solid,portals,edges

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('private_output',type=Path);p.add_argument('--core-dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    seen=set();records=[];counts=collections.Counter();versions=collections.Counter();flags=collections.Counter();solid=collections.Counter();portals=collections.Counter();maximum=-1
    with contextlib.ExitStack() as stack:
        for priority,path in archives(a.root,a.configuration):
            index=Index(stack.enter_context(path.open('rb')))
            for member,entry in index.entries.items():
                if not member.endswith('.flr') or member in seen:continue
                seen.add(member)
                if entry[1]==0:counts['masks']+=1;continue
                data=index.lookup(member)
                try:record,f,s,p,e=inspect(data)
                except Exception as error:raise ValueError('Floor '+member+': '+str(error)) from error
                records.append(dict(member=member,priority=priority,sha256=hashlib.sha256(data).hexdigest().upper(),**record));versions[record['version']]+=1
                for k in ('vertices','triangles','boundary_edges','diagnostics'):counts[k]+=record[k]
                flags.update(f);solid.update(s);portals.update(p);maximum=max(maximum,max(e,default=-1))
    ordered=sorted(records,key=lambda r:r['member']);(a.private_output/'floors.json').write_text(json.dumps(ordered,indent=2),encoding='utf-8')
    members=a.private_output/'members.txt';content=('\n'.join(r['member'] for r in ordered)+'\n').encode();members.write_bytes(content);digest=hashlib.sha256(content).hexdigest().upper()
    print('configuration_sha256='+hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper());print('members_sha256='+digest)
    print(f'live={len(records)} versions={dict(versions)} counts={dict(counts)} flags={dict(flags)} non_solid={dict(solid)} portal_min={min(portals)} portal_max={max(portals)} boundary_edge_max={maximum}',flush=True)
    if a.core_dll:
        result=subprocess.run(['dotnet',str(a.core_dll),'--floor-list',str(a.root),'--configuration',str(a.configuration),'--members',str(members),'--expected-members-sha256',digest],capture_output=True,text=True,check=True,timeout=90)
        (a.private_output/'native-floor-agreement.txt').write_text(result.stdout,encoding='utf-8');native={r['member']:r for r in map(json.loads,(l[6:] for l in result.stdout.splitlines() if l.startswith('floor=')))}
        expected={r['member']:r for r in ordered}
        if native!=expected:raise ValueError('Native floor mismatch '+str([(k,expected[k],native.get(k)) for k in expected if expected[k]!=native.get(k)][:2]))
        print(f'PASS independent floor files={len(records)} triangles={counts["triangles"]} boundary_edges={counts["boundary_edges"]}')
if __name__=='__main__':main()
