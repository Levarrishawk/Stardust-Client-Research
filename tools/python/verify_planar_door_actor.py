"""Independent original doorway lateral/diagonal actor trajectory and normalized input mapping."""
import argparse, hashlib, json, math, subprocess
from pathlib import Path
from door_actor_reference import TABLE_SHA, LEAF_SHA, MEMBER, LAYOUT_SHA, f32, load_reference, ReferenceDoorActor, close

PHASES=[('wait',30,0,0),('right',8,0,1),('diagonal-forward',40,1,-.2),
        ('left',8,0,-1),('diagonal-return',40,-1,.2),('settle',8,0,0)]

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','private_output','core_dll'):p.add_argument(name,type=Path)
    a=p.parse_args();a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    reference=load_reference(a.root,a.configuration)
    command=['dotnet',str(a.core_dll),'--planar-actor',str(a.root),'--configuration',str(a.configuration),'--member',MEMBER,'--expected-sha256',LAYOUT_SHA,'--cell','3','--portal','10','--table-sha256',TABLE_SHA,'--leaf-sha256',LEAF_SHA]
    run=subprocess.run(command,capture_output=True,text=True,timeout=90);(a.private_output/'native-planar.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    def records(prefix):return [json.loads(line[len(prefix):]) for line in run.stdout.splitlines() if line.startswith(prefix)]
    assert {s['Path']:s['Sha256'] for s in records('planar_source=')}==reference['pins']
    rows=records('planar_tick=');ticks=transfers=0
    for transformed in [False,True]:
        actor=ReferenceDoorActor(reference,transformed);samples=[r for r in rows if r['transformed']==transformed];index=0
        for name,count,forward,right in PHASES:
            forward,right=f32(forward),f32(right);scale=f32(1.6)/max(1,math.hypot(forward,right));velocity=[f32(-forward*scale),0,f32(right*scale)]
            if transformed:velocity=[velocity[2],0,-velocity[0]]
            assert math.dist(velocity,[0,0,0])<1.600001
            for _ in range(count):
                row=samples[index];index+=1;state=actor.advance(.02,[f32(v*f32(.02)) for v in velocity],True)
                assert row['phase']==name and row['now']==index*20_000;close(row['velocity'],velocity,1e-7)
                for source,key in [('frame','frame'),('transfers','total'),('cell','cell'),('retained','retained'),('status','status'),('kind','kind')]:assert row[source]==state[key],(transformed,index,source,row[source],state[key])
                close(row['local'],state['local']);close(row['world'],state['world']);close(row['fraction'],state['fraction'],1e-6);close(row['curve'],state['curve'],1e-6);ticks+=1
        assert index==len(samples) and state['total']==2 and state['cell']==3
        close(state['local'],reference['initial']);transfers+=state['total']
    summary=dict(configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),source_pins=reference['pins'],owner_poses=2,phases=len(PHASES),independent_ticks=ticks,transfers=transfers)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8');print('PASS: independent planar doorway '+json.dumps(summary))

if __name__=='__main__':main()
