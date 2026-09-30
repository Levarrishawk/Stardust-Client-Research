"""Independent repeated original-doorway actor/owner/controller trace. Proprietary-derived details stay private."""
import argparse, hashlib, json, subprocess
from pathlib import Path
from door_actor_reference import TABLE_SHA, LEAF_SHA, MEMBER, LAYOUT_SHA, f32, load_reference, ReferenceDoorActor, close

def phases():
    for _ in range(10):
        for _ in range(20):yield 'forward',f32(.08),True,False
        for _ in range(20):yield 'reverse',f32(-.08),True,False
    yield 'deny-reset',0.,False,True
    for _ in range(12):yield 'deny',f32(.08),False,False
    for _ in range(8):yield 'retreat',f32(-.04),False,False
    for _ in range(12):yield 'unknown',f32(.04),None,False
    for _ in range(20):yield 'recover',f32(.08),True,False
    for _ in range(20):yield 'return',f32(-.08),True,False
    yield 'final-reset',0.,True,True
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path)
    p.add_argument('--member');p.add_argument('--layout-sha256');p.add_argument('--cell',type=int);p.add_argument('--portal',type=int);p.add_argument('--leaf-sha256');a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True);pins={}
    if any(v is not None for v in [a.member,a.layout_sha256,a.cell,a.portal,a.leaf_sha256]):
        if any(v is None for v in [a.member,a.layout_sha256,a.cell,a.portal,a.leaf_sha256]):p.error('Generic doorway requires all five identity/reference options.')
        from doorway_reference import load_doorway_reference
        reference=load_doorway_reference(a.root,a.configuration,a.member,a.layout_sha256,a.cell,a.portal,a.leaf_sha256)
        member,layout_sha,cell,portal,leaf_sha=a.member,a.layout_sha256,a.cell,a.portal,a.leaf_sha256
    else:
        reference=load_reference(a.root,a.configuration);member,layout_sha,cell,portal,leaf_sha=MEMBER,LAYOUT_SHA,3,10,LEAF_SHA
    pins=reference['pins'];direction=reference.get('direction',[-1,0,0])
    command=['dotnet',str(a.core_dll),'--persistent-door',str(a.root),'--configuration',str(a.configuration),'--member',member,'--expected-sha256',layout_sha,'--cell',str(cell),'--portal',str(portal),'--table-sha256',TABLE_SHA,'--leaf-sha256',leaf_sha]
    run=subprocess.run(command,capture_output=True,text=True,timeout=90);(a.private_output/'native-persistent.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    sources=[json.loads(s[len('persistent_source='):]) for s in run.stdout.splitlines() if s.startswith('persistent_source=')];assert {s['Path']:s['Sha256'] for s in sources}==pins
    records=[json.loads(s[len('persistent_actor='):]) for s in run.stdout.splitlines() if s.startswith('persistent_actor=')];assert len(records)==948
    probes=transfers=blocked=0
    for transformed in [False,True]:
        actor=ReferenceDoorActor(reference,transformed);rows=[r for r in records if r['transformed']==transformed]
        for sequence,((phase,step,allowed,reset),n) in enumerate(zip(phases(),rows)):
            if reset:actor.reset()
            delta=[f32(v*step) for v in direction]
            if transformed:delta=[delta[2],0,-delta[0]]
            expected=actor.advance(.05,delta,allowed)
            assert (n['sequence'],n['phase'],n['reset'],n['allowed'])==(sequence,phase,reset,allowed)
            for key in ['frame','total','cell','retained','transition','status','kind','transfers']:assert n[key]==expected[key],(sequence,key,n[key],expected[key])
            assert n['diagnostic'] is None;close(delta,n['delta'])
            for key in ['fraction','curve']:close(expected[key],n[key],1e-6)
            for key in ['world','local']:close(expected[key],n[key])
            close(expected['parameter'],n['parameter'],5e-5);probes+=1;transfers+=expected['transfers'];blocked+=int(expected['kind'] is not None)
    summary=dict(member=member,cell=cell,portal=portal,configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),source_pins=pins,ticks=probes,owner_poses=2,transfers=transfers,blocked_ticks=blocked)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(f'PASS independent_ticks={probes} owner_poses=2 transfers={transfers} blocked_ticks={blocked}')
if __name__=='__main__':main()
