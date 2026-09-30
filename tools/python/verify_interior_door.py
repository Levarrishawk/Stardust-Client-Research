"""Independent original POB reference/floor pairing and integrated leaf/barrier contact oracle. Output stays private."""
import argparse, contextlib, hashlib, json, math, struct, subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_portal_layout import inspect
from verify_static_mesh_slice import nodes
from verify_single_leaf_door import TABLE, TABLE_SHA, LEAF_SHA, MESH_SHA, table_rows, f32, walk

MEMBER='appearance/thm_tato_cantina.pob'
LAYOUT_SHA='3DAA136761AB8073F93250098CB1D38807E5279337C29F196D71C207B6D08984'
def add(a,b):return [x+y for x,y in zip(a,b)]
def sub(a,b):return [x-y for x,y in zip(a,b)]
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def point(p,v):return [dot(row,v)+t for row,t in zip(p[:3],p[3])]
def local(p,v):
    d=sub(v,p[3]);return [sum(p[j][i]*d[j] for j in range(3)) for i in range(3)]
def floor(raw):
    version=nodes(raw)[0][1][0];vp,tr=version[1][:2];v=vp[1];t=tr[1]
    if version[0]==b'0006':v=v[4:];t=t[4:]
    return list(struct.iter_unpack('<3f',v)),[struct.unpack_from('<3iI3i3f4Bi3i',t,i) for i in range(0,len(t),60)]
def project(f,p):
    hits=[];vertices,rows=f
    for ti,r in enumerate(rows):
        if r[13]:continue
        a,b,c=[vertices[j] for j in r[:3]]
        det=(b[0]-a[0])*(c[2]-a[2])-(c[0]-a[0])*(b[2]-a[2])
        if abs(det)<1e-12:continue
        u=((p[0]-a[0])*(c[2]-a[2])-(c[0]-a[0])*(p[2]-a[2]))/det
        v=((b[0]-a[0])*(p[2]-a[2])-(p[0]-a[0])*(b[2]-a[2]))/det
        if min(u,v,1-u-v)>=-1e-6:hits.append((ti,[p[0],a[1]+u*(b[1]-a[1])+v*(c[1]-a[1]),p[2]]))
    assert hits,'No independent floor hit';return min(hits,key=lambda h:h[0])
def sweep(p,lo,hi,a,b):
    body=[.25,.9,.25];s=local(p,add(a,[0,.9,0]));e=local(p,add(b,[0,.9,0]))
    extent=[sum(abs(p[j][i])*body[j] for j in range(3)) for i in range(3)]
    enter=0.;exit=1.;axis=-1;sign=0
    for i in range(3):
        lower=lo[i]-extent[i];upper=hi[i]+extent[i];delta=e[i]-s[i]
        if abs(delta)<1e-12:
            if not lower<=s[i]<=upper:return None
            continue
        near=(lower-s[i])/delta;far=(upper-s[i])/delta;n=-1
        if near>far:near,far,n=far,near,1
        if near>enter:enter,axis,sign=near,i,n
        exit=min(exit,far)
        if enter>exit:return None
    if enter>1 or exit<0:return None
    assert axis>=0
    normal=[p[j][axis]*sign for j in range(3)]
    return enter,add([u+(v-u)*enter for u,v in zip(a,b)],[0,.9,0]),normal
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    pins={}
    with contextlib.ExitStack() as stack:
        ix=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(a.root,a.configuration)]
        def read(m):
            for index in ix:
                raw=index.lookup(m)
                if raw is not None:
                    assert raw,'Masked input';pins[m]=hashlib.sha256(raw).hexdigest().upper();return raw
            raise ValueError('Missing input '+m)
        layout=inspect(read(MEMBER));assert pins[MEMBER]==LAYOUT_SHA
        style=next(r for r in table_rows(read(TABLE)) if r['name']=='cantina_door');assert pins[TABLE]==TABLE_SHA
        apt=read(style['primary']);assert pins[style['primary']]==LEAF_SHA
        mesh_path=next(value.rstrip(b'\0').decode('ascii') for tag,value in walk(nodes(apt)) if tag==b'NAME')
        mesh=read(mesh_path);assert pins[mesh_path]==MESH_SHA
        appr=next(v for t,v in walk(nodes(mesh)) if t==b'APPR');box=next(v for t,v in walk(appr) if t==b'BOX ');hi=struct.unpack_from('<3f',box);lo=struct.unpack_from('<3f',box,12)
        floors={ci:floor(read(layout['cells'][ci]['floor'])) for ci in [3,12]}
        assert pins[layout['cells'][3]['floor']]=='DC61558BC59587861D0AAD94724783A0400662F302885656C96BE75A4E8DBAD5'
        assert pins[layout['cells'][12]['floor']]=='5A28497651ADAD4FE477BF0DE3245749A2CBC314C242E4B469E7C79F705F9A2C'
    probes=0;results=[]
    def near(x,y,tol=3e-5):
        if isinstance(x,(list,tuple)):assert len(x)==len(y);[near(u,v,tol) for u,v in zip(x,y)]
        else:assert abs(x-y)<tol,(x,y)
    for cell,portal in [(3,10),(12,0)]:
        r=layout['cells'][cell]['portals'][portal];assert r['door_name']==style['name'] and r['transform_flag']==1 and r['solid_byte'] and not r['leading_byte']
        target=r['target_cell'];values=struct.unpack('<12f',bytes.fromhex(r['transform_hex']));pose=[list(values[0:3]),list(values[4:7]),list(values[8:11]),[values[3],values[7],values[11]]]
        fv,fr=floors[cell];tv,tr=floors[target]
        edges=[(ti,e) for ti,row in enumerate(fr) for e in range(3) if row[15+e]==portal and row[4+e]==-1];assert len(edges)==1
        ti,e=edges[0];row=fr[ti];edge=[fv[row[j]] for j in [e,(e+1)%3]]
        target_refs=[pi for pi,v in enumerate(layout['cells'][target]['portals']) if v['geometry_index']==r['geometry_index'] and v['target_cell']==cell];assert len(target_refs)==1
        matches=[(nt,ne) for nt,nr in enumerate(tr) for ne in range(3) if nr[15+ne]==target_refs[0] and nr[4+ne]==-1 and max(math.dist(x,y) for x,y in zip(edge,[tv[nr[(ne+1)%3]],tv[nr[ne]]]))<1e-4];assert len(matches)==1
        center=[sum(fv[row[j]][i] for j in range(3))/3 for i in range(3)];normal=[pose[j][2] for j in range(3)];side=1 if dot(sub(center,pose[3]),normal)>0 else -1
        st,sp=project(floors[cell],point(pose,[0,0,side*.8]));_,ep=project(floors[target],point(pose,[0,0,-side*.8]))
        command=['dotnet',str(a.core_dll),'--interior-door',str(a.root),'--configuration',str(a.configuration),'--member',MEMBER,'--expected-sha256',LAYOUT_SHA,'--cell',str(cell),'--portal',str(portal),'--table-sha256',TABLE_SHA,'--leaf-sha256',LEAF_SHA]
        run=subprocess.run(command,capture_output=True,text=True,timeout=90);(a.private_output/f'native-{cell}-{portal}.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
        source_rows=[json.loads(s[21:]) for s in run.stdout.splitlines() if s.startswith('interior_door_source=')]
        assert {s['Path']:s['Sha256'] for s in source_rows}==pins
        observed=[json.loads(s[14:]) for s in run.stdout.splitlines() if s.startswith('interior_door=')];assert len(observed)==12
        for n in observed:
            near(pose,n['doorPose']);near(sp,n['start']);near(ep,n['end']);assert n['startTriangle']==st
            transformed=n['transformed'];convert=lambda v:[v[2]+30,v[1]+4,-v[0]-11] if transformed else list(v)
            world_pose=[[pose[2][i] for i in range(3)],pose[1],[-pose[0][i] for i in range(3)],convert(pose[3])] if transformed else pose
            fraction=0 if n['stage'] in ('closed','reset') else .25 if n['stage']=='partial' else 1.;powered=fraction**(1/style['spring']);curve=f32(powered*(1-style['smooth'])+(1-math.cos(f32(math.pi)*powered))*.5*style['smooth'])
            near(fraction,n['fraction']);near(curve,n['curve'])
            if n['stage'] in ('deny','unknown'):
                vertices=list(struct.iter_unpack('<3f',bytes.fromhex(layout['geometries'][r['geometry_index']]['vertex_hex'])));local_vertices=[local(pose,v) for v in vertices]
                lower=[min(v[i] for v in local_vertices)-.05 for i in range(3)];upper=[max(v[i] for v in local_vertices)+.05 for i in range(3)];contact=sweep(world_pose,lower,upper,convert(sp),convert(ep));kind='PermissionBarrier';status='EntryDenied' if n['stage']=='deny' else 'PermissionUnknown'
            else:
                leaf_pose=world_pose[:3]+[point(world_pose,[v*curve for v in style['move']])];contact=sweep(leaf_pose,lo,hi,convert(sp),convert(ep));kind='DoorLeaf' if contact else None;status='PortalBlocked' if contact else 'Accepted'
            assert n['status']==status and n['kind']==kind and n['diagnostic'] is None
            assert n['owner']==(cell if contact else target) and n['transfers']==(0 if contact else 1)
            if contact:
                parameter,center,normal=contact;near(parameter,n['parameter']);near(parameter,n['contact']['parameter']);near(center,n['contact']['center']);near(normal,n['contact']['normal'])
                position=sub(center,[0,.9,0]);near(position,n['position']);_,foot=project(floors[cell],n['localPosition']);near(foot,n['localPosition'])
            else:assert n['parameter']==1 and n['contact'] is None;near(convert(ep),n['position'])
            probes+=1;results.append(dict(cell=cell,portal=portal,stage=n['stage'],transformed=transformed,status=status))
    summary=dict(member=MEMBER,source_pins=pins,configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),references=2,owner_poses=2,states=6,independent_probes=probes,results=results)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(f'PASS original_references=2 owner_poses=2 independent_probes={probes} sources={len(pins)}')
if __name__=='__main__':main()
