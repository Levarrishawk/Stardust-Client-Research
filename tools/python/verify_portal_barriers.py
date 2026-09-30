"""Independent installed portal flags, local bounds and analytic axial actor-contact probes. Detail stays private."""
import argparse, contextlib, hashlib, json, math, struct, subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_portal_layout import inspect

def dot(a,b): return sum(x*y for x,y in zip(a,b))
def sub(a,b): return [x-y for x,y in zip(a,b)]
def cross(a,b): return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def unit(a):
    n=math.sqrt(dot(a,a));assert n>1e-6;return [x/n for x in a]
def pose_for(geometry,reference):
    vertices=list(struct.iter_unpack('<3f',bytes.fromhex(geometry['vertex_hex'])))
    if reference['transform_flag']:
        v=struct.unpack('<12f',bytes.fromhex(reference['transform_hex']));return vertices,[list(v[0:3]),list(v[4:7]),list(v[8:11]),[v[3],v[7],v[11]]]
    for i in range(0,len(geometry['indices']),3):
        a,b,c=[vertices[j] for j in geometry['indices'][i:i+3]];n=cross(sub(b,a),sub(c,a))
        if dot(n,n)>1e-12:break
    z=unit(n);x=unit(cross([0,1,0] if abs(z[1])<.99 else [1,0,0],z));y=cross(z,x)
    return vertices,[[x[i],y[i],z[i]] for i in range(3)]+[list(vertices[0])]
def local(p,world):
    v=sub(world,p[3]);return [sum(p[j][i]*v[j] for j in range(3)) for i in range(3)]
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    layouts={};seen=set()
    with contextlib.ExitStack() as stack:
        for _,path in archives(a.root,a.configuration):
            index=Index(stack.enter_context(path.open('rb')))
            for member,entry in index.entries.items():
                if not member.endswith('.pob') or member in seen:continue
                seen.add(member)
                if entry[1]==0:continue
                raw=index.lookup(member);layouts[member]=(hashlib.sha256(raw).hexdigest().upper(),inspect(raw))
    manifest=a.private_output/'members.txt';data=('\n'.join(sorted(layouts))+'\n').encode();manifest.write_bytes(data);digest=hashlib.sha256(data).hexdigest().upper()
    run=subprocess.run(['dotnet',str(a.core_dll),'--portal-barriers',str(a.root),'--configuration',str(a.configuration),'--members',str(manifest),'--expected-members-sha256',digest],capture_output=True,text=True,timeout=90)
    (a.private_output/'native-barriers.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    observed={r['member']:r for r in (json.loads(line[9:]) for line in run.stdout.splitlines() if line.startswith('barriers='))};assert observed.keys()==layouts.keys()
    references=eligible=probes=0
    for member,(sha,layout) in layouts.items():
        native=observed[member];assert native['sha256']==sha;rows={(r['cell'],r['reference']):r for r in native['rows']}
        expected_keys={(ci,ri) for ci,c in enumerate(layout['cells']) for ri,_ in enumerate(c['portals'])};assert rows.keys()==expected_keys
        for ci,ri in expected_keys:
            r=layout['cells'][ci]['portals'][ri];n=rows[(ci,ri)];references+=1
            accepted=not r['leading_byte'] and r['solid_byte']!=0;assert n['eligible']==accepted
            if not accepted:continue
            eligible+=1;vertices,pose=pose_for(layout['geometries'][r['geometry_index']],r)
            assert max(abs(x-y) for a,b in zip(pose,n['pose']) for x,y in zip(a,b))<1e-4
            points=[local(pose,v) for v in vertices];lo=[min(v[i] for v in points)-.05 for i in range(3)];hi=[max(v[i] for v in points)+.05 for i in range(3)]
            assert max(abs(x-y) for x,y in zip(lo,n['min']))<1e-4 and max(abs(x-y) for x,y in zip(hi,n['max']))<1e-4
            # Independent axial contact uses the selected thin box face; other axes stay at its center.
            s=local(pose,n['start']);e=local(pose,n['end']);axis=max(range(3),key=lambda i:abs(e[i]-s[i]));extent=sum(abs(pose[j][axis])*n['body'][j] for j in range(3))
            t=(lo[axis]-extent-s[axis])/(e[axis]-s[axis]);assert 0<t<1
            for hit in n['contacts']:
                allowed=hit['allowed'];assert hit['status']==('Clear' if allowed else 'PermissionUnknown' if allowed is None else 'Blocked')
                expected_t=1 if allowed else t;assert abs(hit['parameter']-expected_t)<2e-5
                expected_center=[u+(v-u)*expected_t for u,v in zip(n['start'],n['end'])]
                assert max(abs(x-y) for x,y in zip(hit['center'],expected_center))<2e-4
                expected_normal=[0,0,0] if allowed else [-pose[i][axis] for i in range(3)]
                assert max(abs(x-y) for x,y in zip(hit['normal'],expected_normal))<1e-4;probes+=1
    summary=dict(configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),members_sha256=digest,layouts=len(layouts),references=references,eligible=eligible,probes=probes,source_pins={m:s for m,(s,_) in layouts.items()})
    (a.private_output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print(f'PASS layouts={len(layouts)} references={references} eligible_permission_barriers={eligible} independent_axial_probes={probes}')
if __name__=='__main__':main()
