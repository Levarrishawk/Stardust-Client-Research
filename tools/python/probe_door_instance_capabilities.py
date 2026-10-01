"""Bounded native round-trip screening with independent installed identities and floor-edge pairing. Not general locomotion coverage."""
import argparse, collections, contextlib, hashlib, json, math, subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_portal_layout import inspect
from verify_interior_door import floor
from connected_door_approach_reference import verify_connected_approach

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','capability_details','private_output','core_dll'):p.add_argument(name,type=Path)
    p.add_argument('--precise',action='store_true')
    p.add_argument('--steps',action='store_true')
    p.add_argument('--local-ray',action='store_true')
    p.add_argument('--connected',action='store_true')
    a=p.parse_args();private=(Path(__file__).resolve().parents[2]/'local-private').resolve()
    if a.steps and not a.precise:p.error('--steps requires --precise')
    if a.local_ray and not a.steps:p.error('--local-ray requires --steps')
    if a.connected and not a.local_ray:p.error('--connected requires --local-ray')
    a.private_output.resolve().relative_to(private);a.capability_details.resolve().relative_to(private);a.private_output.mkdir(parents=True,exist_ok=True)
    inventory=json.loads(a.capability_details.read_text(encoding='utf-8'));cfg_hash=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper()
    assert inventory['summary']['configuration_sha256']==cfg_hash
    candidates=inventory['candidates'];assert len(candidates)<=1024
    path=a.private_output/'candidates.json';path.write_text(json.dumps(candidates,indent=2)+'\n',encoding='utf-8');digest=hashlib.sha256(path.read_bytes()).hexdigest().upper()
    run=subprocess.run(['dotnet',str(a.core_dll),'--connected-ray-step-door-instance-capabilities' if a.connected else '--ray-step-door-instance-capabilities' if a.local_ray else '--step-door-instance-capabilities' if a.steps else '--precise-door-instance-capabilities' if a.precise else '--door-instance-capabilities',str(a.root),'--configuration',str(a.configuration),'--candidates',str(path),'--expected-candidates-sha256',digest],capture_output=True,text=True,timeout=120)
    (a.private_output/'native-instances.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    native=[json.loads(s[len('door_instance_capability='):]) for s in run.stdout.splitlines() if s.startswith('door_instance_capability=')]
    by_key={(r['member'],r['cell'],r['portal']):r for r in native};assert len(native)==len(by_key)==len(candidates)
    layouts={};floors={};raw_cache={};pins={};pair_accepts=source_checks=connected_checks=0;boundaries=[]
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(a.root,a.configuration)]
        def read(member):
            if member in raw_cache:return raw_cache[member]
            for index in indices:
                raw=index.lookup(member)
                if raw is not None:
                    assert raw;raw_cache[member]=raw;pins[member]=hashlib.sha256(raw).hexdigest().upper();return raw
            raise AssertionError('Missing screened input '+member)
        def edges(f,portal):
            vs,rows=f
            return [[vs[r[e]],vs[r[(e+1)%3]]] for r in rows for e in range(3) if r[15+e]==portal and r[4+e]==-1]
        for c in candidates:
            key=(c['member'],c['cell'],c['portal']);n=by_key[key]
            if c['member'] not in layouts:layouts[c['member']]=inspect(read(c['member']))
            layout=layouts[c['member']];assert pins[c['member']]==c['layout_sha256']
            ref=layout['cells'][c['cell']]['portals'][c['portal']];sides=[(ci,pi) for ci,cell in enumerate(layout['cells']) for pi,r in enumerate(cell['portals']) if r['geometry_index']==ref['geometry_index']]
            side_edges=[]
            for cell,portal in [(c['cell'],c['portal']),(c['target'],c['target_portal'])]:
                name=layout['cells'][cell]['floor']
                if name not in floors:floors[name]=floor(read(name))
                side_edges.append(edges(floors[name],portal))
            unique=any(sum(max(math.dist(p,q) for p,q in zip(x,reversed(y)))<=1e-4 for y in side_edges[1])==1 for x in side_edges[0])
            paired=len(sides)==2 and len({cell for cell,_ in sides})==2 and unique
            assert (n['failure']=='Door reference lacks a uniquely paired floor edge.')== (not paired), (key,n['failure'],paired)
            pair_accepts+=int(paired)
            if a.connected and n['initial'] is not None:
                connected_checks += verify_connected_approach(layout,c,n,floors)
            for identity in n['sources'] or []:
                raw=read(identity['Path']);assert pins[identity['Path']]==identity['Sha256'] and len(raw)==identity['Bytes'];source_checks+=1
            if n['status'] is not None and not n['passed']:
                stop=n.get('forwardStop');item=dict(member=c['member'],cell=c['cell'],portal=c['portal'],boundary=stop)
                if stop is None:item['classification']='NoForwardStop'
                elif stop['floor_status']=='UnsupportedSurface':
                    name=layout['cells'][stop['cell']]['floor'];vs,rows=floors[name];row=rows[stop['triangle']];neighbor=row[4+stop['floor_edge']];assert neighbor>=0
                    target=rows[neighbor];ps=[vs[i] for i in target[:3]];u=[ps[1][i]-ps[0][i] for i in range(3)];v=[ps[2][i]-ps[0][i] for i in range(3)]
                    normal=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];length=math.sqrt(sum(x*x for x in normal));ratio=abs(normal[1])/length if length else 0
                    item.update(floor=name,floor_sha256=pins[name],neighbor=neighbor,non_solid=target[13],vertical_ratio=ratio,height_span=max(p[1] for p in ps)-min(p[1] for p in ps))
                    item['classification']='NonSolidNeighbor' if target[13] else 'VerticalNeighbor' if ratio<1e-4 else 'OtherUnsupportedSurface'
                elif stop['status']=='MissingTargetFloor':
                    actual=layout['cells'][stop['cell']]['portals'][stop['floor_portal']]['target_cell'];name=layout['cells'][actual]['floor']
                    item.update(encountered_target_cell=actual,encountered_target_floor=name,selected_target_cell=c['target'])
                    item['classification']='UnloadedOtherTargetFloor' if name and actual not in (c['cell'],c['target']) else 'AbsentTargetFloor'
                else:item['classification']=stop['contact_kind'] or stop['floor_status'] or stop['status']
                boundaries.append(item)
    passed=sum(r['passed'] for r in native);instantiated=sum(r['status'] is not None for r in native)
    failure_counts=collections.Counter(r['failure'] for r in native if r['failure'])
    held=collections.Counter(r['diagnostic'] or r['status'] for r in native if r['status'] is not None and not r['passed'])
    summary=dict(approach_mode='connected' if a.connected else 'vertical_inspection',movement_mode='precise_ray_steps' if a.local_ray else 'precise_steps' if a.steps else 'precise' if a.precise else 'level',configuration_sha256=cfg_hash,candidates_sha256=digest,candidates=len(candidates),independently_paired_candidates=pair_accepts,
                 source_identity_checks=source_checks,independent_connected_endpoints=connected_checks,native_instantiated=instantiated,native_fixture_ticks=instantiated*60,native_roundtrip_passes=passed,
                 pre_simulation_failures=dict(failure_counts),non_roundtrip_final_results=dict(held),first_forward_boundaries=dict(collections.Counter(r['classification'] for r in boundaries)))
    (a.private_output/'boundary-details.json').write_text(json.dumps(boundaries,indent=2)+'\n',encoding='utf-8')
    (a.private_output/'screening-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
    (a.private_output/'passing-candidates.json').write_text(json.dumps([c for c in candidates if by_key[(c['member'],c['cell'],c['portal'])]['passed']],indent=2)+'\n',encoding='utf-8')
    print('PASS: identity/floor-pair reconciled native screening '+json.dumps(summary))

if __name__=='__main__':main()
