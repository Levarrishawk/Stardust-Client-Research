"""Occupied closing/denial/retreat on the existing pinned Mustafar second doorway."""
import argparse
import hashlib
import json
import math
import subprocess
from pathlib import Path
from doorway_reference import load_doorway_reference
from verify_original_interior_scene import MEMBER,LAYOUT_SHA,LEAF_SHA
from door_actor_reference import close,cast
from verify_floor_path_boxes import f32
from closing_door_actor_reference import ClosingReferenceDoorActor

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','core_dll','private_output'):p.add_argument(name,type=Path)
    a=p.parse_args();a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    refs=[load_doorway_reference(a.root,a.configuration,MEMBER,LAYOUT_SHA,c,1,LEAF_SHA,True) for c in (5,6)];r=refs[1]
    pins={p:s for ref in refs for p,s in ref['pins'].items()}
    spec=dict(member=MEMBER,layout_sha256=LAYOUT_SHA,doors=[dict(cell=c,portal=1,leaf_sha256=LEAF_SHA) for c in (5,6)])
    path=a.private_output/'scene-spec.json';path.write_text(json.dumps(spec,indent=2)+'\n',encoding='utf-8');digest=hashlib.sha256(path.read_bytes()).hexdigest().upper()
    run=subprocess.run(['dotnet',str(a.core_dll),'--original-closing-scene',str(a.root),'--configuration',str(a.configuration),'--scene-spec',str(path),'--expected-sha256',digest],capture_output=True,text=True,timeout=90)
    (a.private_output/'native-closing.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    sources=[json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('scene_source=')];assert {s['Path']:s['Sha256'] for s in sources}==pins
    records=[json.loads(x.split('=',1)[1]) for x in run.stdout.splitlines() if x.startswith('scene_closing=')]
    schedule=[('warm',0.,True)]*(math.ceil(r['style']['open']/.05)+2)+[('approach',f32(.08),True)]*6+[('deny-close',0.,False)]*80+[('retreat',f32(-.08),False)]*20+[('settle',0.,False)]*60
    assert len(records)==len(schedule)*4;held=blocked=0;finals=[]
    for transformed in (False,True):
        for guarded in (False,True):
            actor=ClosingReferenceDoorActor(r,transformed,guarded);selected=[n for n in records if n['transformed']==transformed and n['guarded']==guarded]
            for sequence,(n,(phase,meters,allowed)) in enumerate(zip(selected,schedule)):
                assert (n['sequence'],n['phase'],n['allowed'])==(sequence,phase,allowed)
                delta=cast([v*meters for v in r['direction']]);delta=cast([delta[2],0,-delta[0]]) if transformed else delta;close(delta,n['delta'],1e-6)
                e=actor.advance(.05,delta,allowed)
                for key in ('frame','cell','total','status','kind','retained','transition','held'):assert n[key]==e[key],(transformed,guarded,sequence,key,n[key],e[key])
                for key in ('fraction','curve'):close(e[key],n[key],1e-6)
                for key in ('local','world','leaf_position'):close(e[key],n[key],8e-5)
                close(e['parameter'],n['parameter'],5e-5);held+=int(e['held']);blocked+=int(e['kind'] is not None)
            retreat=math.dist(actor.local_position,r['initial']);assert actor.total==0 and actor.fraction==0
            finals.append(dict(transformed=transformed,guarded=guarded,final_offset_from_approach=retreat))
            assert retreat>1 if guarded else .47<retreat<.49
    summary=dict(member=MEMBER,scene_spec_sha256=digest,configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),source_pins=pins,ticks=len(records),owner_poses=2,guarded_and_historical=True,held_ticks=held,blocked_ticks=blocked,finals=finals)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8');print('PASS independent original closing '+json.dumps({k:summary[k] for k in ('ticks','held_ticks','blocked_ticks')}))

if __name__=='__main__':main()
