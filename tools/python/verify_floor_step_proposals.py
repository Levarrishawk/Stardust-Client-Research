"""Independent original riser topology/plane checks; proposals alone do not establish actor clearance."""
import argparse,collections,contextlib,hashlib,json,math,subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_interior_door import floor
from verify_floor_path_boxes import f32
from verify_original_floor_boxes import coordinates

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','boundary_details','native_screen','private_output','core_dll'):p.add_argument(name,type=Path)
    p.add_argument('--with-movement',action='store_true');p.add_argument('--local-ray',action='store_true');a=p.parse_args();private=(Path(__file__).resolve().parents[2]/'local-private').resolve()
    for path in (a.boundary_details,a.native_screen,a.private_output):path.resolve().relative_to(private)
    a.private_output.mkdir(parents=True,exist_ok=True)
    details=json.loads(a.boundary_details.read_text());screen=[json.loads(x.split('=',1)[1]) for x in a.native_screen.read_text().splitlines() if x.startswith('door_instance_capability=')]
    by={(r['member'],r['cell'],r['portal']):r for r in screen};groups={}
    for d in details:
        if d['classification']!='VerticalNeighbor':continue
        n=by[(d['member'],d['cell'],d['portal'])];s=d['boundary'];delta=[f32(n['initial']['end'][i]-n['initial']['local'][i]) if i!=1 else 0 for i in range(3)]
        length=f32(math.hypot(delta[0],delta[2]));delta=[f32(f32(v/length)*f32(.08)) for v in delta]
        fixture=groups.setdefault(d['floor'],dict(member=d['floor'],sha256=d['floor_sha256'],queries=[]))
        fixture['queries'].append(dict(triangle=s['source_triangle'],start=[f32(v) for v in s['source_local']],end=[f32(f32(v)+delta[i]) for i,v in enumerate(s['source_local'])]))
    fixture=a.private_output/'fixtures.json';fixture.write_text(json.dumps(list(groups.values()),separators=(',',':')));digest=hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
    flag='--floor-ray-step-movement' if a.local_ray and a.with_movement else '--floor-ray-step-proposals' if a.local_ray else '--floor-step-movement' if a.with_movement else '--floor-step-proposals'
    run=subprocess.run(['dotnet',str(a.core_dll),flag,str(a.root),'--configuration',str(a.configuration),'--fixtures',str(fixture),'--expected-sha256',digest],capture_output=True,text=True,timeout=90)
    (a.private_output/'native.txt').write_text(run.stdout+run.stderr);run.check_returncode()
    proposals=[json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('step_proposal=')]
    walks={(r['member'],r['index']):r for r in [json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('walk=')]}
    assert len(proposals)==sum(len(f['queries']) for f in groups.values());counts=collections.Counter();checked=0
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(a.root,a.configuration)]
        floors={}
        for name,g in groups.items():
            raw=next(raw for index in indices if (raw:=index.lookup(name)) is not None)
            assert hashlib.sha256(raw).hexdigest().upper()==g['sha256'];floors[name]=floor(raw)
        for n in proposals:
            n['landing']=[f32(v) for v in n['landing']]
            vs,rows=floors[n['member']];query=groups[n['member']]['queries'][n['index']];walk=walks[(n['member'],n['index'])]
            assert walk['status']=='UnsupportedSurface' and n['resume_status']=='Accepted';counts[n['status']]+=1
            if n['status']!='Accepted':
                assert n['triangle']==walk['triangle'] and max(abs(f32(x)-f32(y)) for x,y in zip(n['landing'],walk['position']))<1e-7
                continue
            source=n['source_contact'];target=n['target_contact'];assert source[0]==target[0] and source[2]==target[2] and 1e-6<abs(source[1]-target[1])<=.2
            source_row=rows[walk['triangle']];edge=walk['edge'];pa,pb=[vs[source_row[i]] for i in (edge,(edge+1)%3)]
            dx,dz=query['end'][0]-query['start'][0],query['end'][2]-query['start'][2];ex,ez=pb[0]-pa[0],pb[2]-pa[2]
            t=((pa[0]-query['start'][0])*ez-(pa[2]-query['start'][2])*ex)/(dx*ez-dz*ex)
            x,z=query['start'][0]+dx*t,query['start'][2]+dz*t;_,y=coordinates([vs[i] for i in source_row[:3]],x,z)
            assert max(abs(p-q) for p,q in zip(source,[x,y,z]))<1e-8,'Independent source-edge intersection'
            target_points=[vs[i] for i in rows[n['triangle']][:3]];w,height=coordinates(target_points,target[0],target[2]);assert min(w)>=-1e-8 and abs(height-target[1])<1e-9
            w,height=coordinates(target_points,n['landing'][0],n['landing'][2]);assert min(w)>=-1e-8 and abs(f32(height)-f32(n['landing'][1]))<1e-7 and math.dist(target,n['landing'])<=1e-4
            visited={walk['triangle']};pending=list(n['strip']);assert 0<len(pending)<=8 and len(set(pending))==len(pending)
            while pending:
                available=[i for i in pending if any(neighbor in visited for neighbor in rows[i][4:7])];assert available,'Disconnected proposed strip'
                for i in available:
                    row=rows[i];assert row[13]==0
                    for edge,neighbor in enumerate(row[4:7]):
                        if neighbor not in visited:continue
                        other=rows[neighbor];start,end=row[edge],row[(edge+1)%3]
                        assert row[10+edge]==1 and row[15+edge]==-1 and any(other[e]==end and other[(e+1)%3]==start and other[4+e]==i and other[10+e]==1 and other[15+e]==-1 for e in range(3))
                    pending.remove(i);visited.add(i)
            assert any(neighbor in visited for neighbor in rows[n['triangle']][4:7]);checked+=1
            if a.local_ray:
                # Independent chord cuts in a vertical 2-D projection, rather
                # than the native 3-D Gram/barycentric interval calculation.
                intervals=[]
                for ti in n['strip']:
                    ps=[vs[i] for i in rows[ti][:3]];edges=[(ps[i],ps[(i+1)%3]) for i in range(3)]
                    pa,pb=max(edges,key=lambda e:math.hypot(e[1][0]-e[0][0],e[1][2]-e[0][2]));dx,dz=pb[0]-pa[0],pb[2]-pa[2];length=math.hypot(dx,dz);assert length>0;dx/=length;dz/=length
                    h=source[0]*dx+source[2]*dz;cuts=[]
                    for pa,pb in edges:
                        ha,hb=pa[0]*dx+pa[2]*dz,pb[0]*dx+pb[2]*dz
                        if abs(hb-ha)<1e-12:
                            if abs(h-ha)<1e-8:cuts.extend((pa[1],pb[1]))
                        else:
                            fraction=(h-ha)/(hb-ha)
                            if -1e-8<=fraction<=1+1e-8:cuts.append(pa[1]+fraction*(pb[1]-pa[1]))
                    assert cuts,'Selected strip misses independent vertical chord';intervals.append((min(cuts),max(cuts)))
                reached=min(source[1],target[1]);end=max(source[1],target[1])
                for lo,hi in sorted(intervals):
                    if hi<reached:continue
                    assert lo<=reached+1e-6,'Gap in independent vertical coverage';reached=max(reached,hi)
                    if reached>=end-1e-6:break
                assert reached>=end-1e-6,'Incomplete independent vertical coverage'
        movement_rows=[json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('step_move=')]
        if a.with_movement:
            from verify_floor_path_boxes import reference_path
            lookup={(n['member'],n['index']):n for n in proposals};assert len(movement_rows)==checked*4
            for n in movement_rows:
                proposal=lookup[(n['member'],n['index'])];query=groups[n['member']]['queries'][n['index']];vs,rows=floors[n['member']]
                n['position']=[f32(v) for v in n['position']];assert n['resume_status']=='Accepted'
                if n['mode'] in ('clear','permitted'):
                    assert n['steps']==1 and n['status']=='Accepted' and n['parameter']==1
                    assert n['position'][0]==query['end'][0] and n['position'][2]==query['end'][2]
                else:
                    box=dict(minimum=[f32(v) for v in n['minimum']],maximum=[f32(v) for v in n['maximum']],pose=[[1,0,0],[0,1,0],[0,0,1],[0,0,0]],half=[f32(.25),f32(.9),f32(.25)],allowed=None if n['mode']=='unknown' else False,separating=True)
                    hit=reference_path(box,[0,1],[proposal['source_contact'],proposal['target_contact']]);assert hit['status']!='Clear'
                    assert n['steps']==0 and n['status']=='BoxStopped' and n['diagnostic']=='VerticalStepBlocked' and n['repeat_drift']==0
                    assert math.dist(n['position'],proposal['source_contact'])<=1e-4
                    assert (n['contact_status']=='PermissionUnknown')==(n['mode']=='unknown')
                ps=[vs[i] for i in rows[n['triangle']][:3]];weights,height=coordinates(ps,n['position'][0],n['position'][2]);assert min(weights)>=-1e-8 and abs(f32(height)-n['position'][1])<1e-7
            roundtrips=[json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('step_roundtrip=')];assert len(roundtrips)==checked
            for n in roundtrips:assert n['valid'] and n['steps']==40 and n['return_distance']<=1e-4,(n,'round-trip failure')
    summary=dict(local_ray_coverage=a.local_ray,configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),fixtures_sha256=digest,queries=len(proposals),outcomes=dict(counts),independently_checked_landings=checked,actor_clearance_verified=a.with_movement,movement_queries=len(movement_rows))
    (a.private_output/'summary.json').write_text(json.dumps(summary,indent=2)+'\n');print('PASS independent original riser proposals '+json.dumps(summary))

if __name__=='__main__':main()
