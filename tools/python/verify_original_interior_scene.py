"""Pinned existing Mustafar three-cell/two-door scene, independent original oracle."""
import argparse
import hashlib
import json
import math
import subprocess
from pathlib import Path
from doorway_reference import load_doorway_reference
from door_actor_reference import cast, point, close
from step_door_actor_reference import StepReferenceDoorActor
from verify_floor_path_boxes import reference_path, f32

MEMBER='appearance/thm_must_mining_outpost.pob'
LAYOUT_SHA='79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818'
LEAF_SHA='99C8ED2EEE49BF44DB9CCDFBA4436A2A56949389D3AB79306A1196493481A3D3'

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','core_dll','private_output'):p.add_argument(name,type=Path)
    p.add_argument('--content-bundle',action='store_true');p.add_argument('--appearance-source-manifest',type=Path)
    p.add_argument('--extra-source-manifest',action='append',type=Path,default=[])
    a=p.parse_args();a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    refs=[load_doorway_reference(a.root,a.configuration,MEMBER,LAYOUT_SHA,c,1,LEAF_SHA,True) for c in (5,6)]
    floors={cell:f for r in refs for cell,f in r['floors'].items()};pins={path:sha for r in refs for path,sha in r['pins'].items()}
    spec=dict(member=MEMBER,layout_sha256=LAYOUT_SHA,doors=[dict(cell=c,portal=1,leaf_sha256=LEAF_SHA) for c in (5,6)])
    path=a.private_output/'scene-spec.json';path.write_text(json.dumps(spec,indent=2)+'\n',encoding='utf-8');digest=hashlib.sha256(path.read_bytes()).hexdigest().upper()
    if a.content_bundle != bool(a.appearance_source_manifest):p.error('Content bundle requires an independently verified appearance source manifest')
    run=subprocess.run(['dotnet',str(a.core_dll),'--original-interior-content' if a.content_bundle else '--original-interior-scene',str(a.root),'--configuration',str(a.configuration),'--scene-spec',str(path),'--expected-sha256',digest],capture_output=True,text=True,timeout=90)
    (a.private_output/'native-scene.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    sources=[json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('scene_source=')];assert {s['Path']:s['Sha256'] for s in sources}==pins
    if a.content_bundle:
        manifest_by_path={}
        for manifest_path in [a.appearance_source_manifest,*a.extra_source_manifest]:
            manifest_path.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());assert manifest_path.stat().st_size<=262144
            entries=json.loads(manifest_path.read_text(encoding='utf-8'))
            assert isinstance(entries,list) and 1<=len(entries)<=256 and len({s['Path'] for s in entries})==len(entries)
            for s in entries:
                assert s['Path'] not in manifest_by_path or manifest_by_path[s['Path']]==s;manifest_by_path[s['Path']]=s
        manifest=list(manifest_by_path.values());expected=dict(pins)
        for source in manifest:
            assert source['Path'] not in expected or expected[source['Path']]==source['Sha256'];expected[source['Path']]=source['Sha256']
        combined=[json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('content_source=')]
        assert {s['Path']:s['Sha256'] for s in combined}==expected
        combined_by_path={s['Path']:s for s in combined}
        assert all(combined_by_path[s['Path']]==s for s in manifest)
        assert f'PASS content_sources={len(expected)} external_lookups={len(expected)} all_once=true' in run.stdout
        print(f'PASS combined_sources={len(expected)} bytes={sum(s["Bytes"] for s in combined)} all_external_lookups_once=true')
    records=[json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('interior_scene=')];assert len(records)==2880
    schedule=[]
    for _ in range(2):schedule.extend([('forward',refs[1]['end'],True,False)]*200+[('reverse',refs[0]['initial'],True,False)]*200)
    schedule.extend([('deny',refs[1]['end'],False,i==0) for i in range(200)])
    for name,count,target,allowed in [('retreat',20,refs[1]['initial'],False),('unknown',20,refs[1]['end'],None),('recover',200,refs[1]['end'],True),('return',200,refs[0]['initial'],True)]:schedule.extend([(name,target,allowed,False)]*count)
    ticks=transfers=blocked=steps=0
    for transformed in (False,True):
        convert=lambda v:cast([v[2]+20,v[1]+4,-v[0]-9]) if transformed else cast(v)
        inverse=lambda v:cast([-f32(v[2]+9),f32(v[1]-4),f32(v[0]-20)]) if transformed else cast(v)
        world_double=lambda v:[v[2]+20,v[1]+4,-v[0]-9] if transformed else list(v)
        poses=[]
        for r in refs:
            pose=r['pose'];poses.append([pose[2],pose[1],[-v for v in pose[0]],convert(pose[3])] if transformed else pose)
        oracle=StepReferenceDoorActor.__new__(StepReferenceDoorActor);oracle.local_ray=True;oracle.transformed=transformed;oracle.convert=convert;oracle.inverse=inverse;oracle.world_double=world_double
        def reset():
            oracle.position=convert(refs[0]['initial']);oracle.local_position=cast(refs[0]['initial']);oracle.cell=5;oracle.total=0
            return 0,[dict(fraction=0.,request=None,retained=False) for _ in refs]
        frame,doors=reset()
        rows=[n for n in records if n['transformed']==transformed]
        for sequence,(n,(phase,target,allowed,reset_now)) in enumerate(zip(rows,schedule)):
            assert (n['sequence'],n['phase'],n['reset'],n['second_permission'])==(sequence,phase,reset_now,allowed)
            close(target,n['target'],8e-5)
            if reset_now:frame,doors=reset()
            start=oracle.local_position;local=cast([target[0]-start[0],0,target[2]-start[2]]);length=math.hypot(local[0],local[2])
            if length>.08:local=cast([v*.08/length for v in local])
            expected_delta=cast([local[2],0,-local[0]]) if transformed else local;close(expected_delta,n['delta'],8e-5)
            desired=cast([x+y for x,y in zip(oracle.position,expected_delta)]);leaf_poses=[];events=[];curves=[]
            for g,(r,d,pose) in enumerate(zip(refs,doors,poses)):
                style=r['style'];present=oracle.cell in (r['source'],r['target']);entry=True if g==0 else allowed
                center=[oracle.position[0],oracle.position[1]+f32(.9),oracle.position[2]]
                if present and entry is True and math.dist(center,point(pose,[0,1,0]))<=style['radius']:d['request']=frame;d['retained']=True
                opening=d['request'] is not None and frame-d['request']<=1;raw=d['fraction']+(f32(.05)/style['open'] if opening else -f32(.05)/style['close'])
                event=0 if d['fraction']==0 and raw>0 else 1 if d['fraction']<1 and raw>=1 else 2 if d['fraction']==1 and raw<1 else 3 if d['fraction']>0 and raw<=0 else -1
                d['fraction']=f32(min(1,max(0,raw)));powered=d['fraction']**(1/style['spring']);curve=f32(powered*(1-style['smooth'])+(1-math.cos(f32(math.pi)*powered))*.5*style['smooth'])
                if not present or math.dist(oracle.position,pose[3])>=1.5*style['radius']:d['retained']=False
                if d['retained'] and entry is True:d['request']=frame
                leaf_poses.append(pose[:3]+[cast(point(pose,cast([v*curve for v in style['move']])))]);events.append(event);curves.append(curve)
            active=0 if oracle.cell==5 else 1 if oracle.cell==7 else int(inverse(desired)[2]>=start[2]);other=1-active
            # A second physical leaf is present, and must independently be clear on each offered short path.
            r=refs[other]
            hit=reference_path(dict(minimum=r['lower'],maximum=r['upper'],pose=leaf_poses[other],half=cast([.25,.9,.25]),allowed=False,separating=True),[0.,1.],[oracle.position,desired])
            assert hit['status']=='Clear',(sequence,'inactive physical leaf intersects route')
            oracle.r=refs[active];oracle.r['floors']=floors;oracle.world_pose=poses[active]
            expected=oracle.precise_move(desired,leaf_poses[active],True if active==0 else allowed);frame+=1
            for key,value in dict(frame=frame,total=oracle.total,cell=oracle.cell,status=expected['status'],kind=expected['kind'],transfers=expected['transfers'],steps=expected['steps']).items():assert n[key]==value,(sequence,key,n[key],value)
            try:
                close(expected['local'],n['local'],8e-5);close(oracle.position,n['world'],8e-5);close(expected['parameter'],n['parameter'],5e-5)
            except AssertionError as error:
                raise AssertionError((transformed,sequence,phase,expected['local'],n['local'],error)) from error
            ordered=sorted(n['doors'],key=lambda d:d['geometry'])
            geometries=[r['layout']['cells'][r['source']]['portals'][r['portal']]['geometry_index'] for r in refs]
            observed={d['geometry']:d for d in ordered}
            for g,d in enumerate(doors):
                o=observed[geometries[g]];assert o['retained']==int(d['retained']) and o['transition']==events[g],(sequence,g,'controller state')
                close(d['fraction'],o['fraction'],1e-6);close(curves[g],o['curve'],1e-6)
            ticks+=1;transfers+=expected['transfers'];blocked+=int(expected['kind'] is not None);steps+=expected['steps']
    summary=dict(member=MEMBER,scene_spec_sha256=digest,configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),source_pins=pins,ticks=ticks,owner_poses=2,cells=3,physical_leaves=2,transfers=transfers,blocked_ticks=blocked,step_events=steps)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8');print('PASS independent original scene '+json.dumps({k:summary[k] for k in ('ticks','transfers','blocked_ticks','step_events')}))

if __name__=='__main__':main()
