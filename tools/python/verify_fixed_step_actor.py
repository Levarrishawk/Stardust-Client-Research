"""Independent integer scheduler accounting and original 50 Hz actor trajectory under three render partitions."""
import argparse, hashlib, json, subprocess
from pathlib import Path
from door_actor_reference import TABLE_SHA, LEAF_SHA, MEMBER, LAYOUT_SHA, f32, load_reference, ReferenceDoorActor, close

PHASES=[('forward',1_200_000,1.6,True),('reverse',2_400_000,-1.6,True),('denied',2_800_000,1.6,False),('retreat',3_000_000,-1.6,False),('recover',4_200_000,1.6,True)]
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True);reference=load_reference(a.root,a.configuration)
    command=['dotnet',str(a.core_dll),'--fixed-door-actor',str(a.root),'--configuration',str(a.configuration),'--member',MEMBER,'--expected-sha256',LAYOUT_SHA,'--cell','3','--portal','10','--table-sha256',TABLE_SHA,'--leaf-sha256',LEAF_SHA]
    run=subprocess.run(command,capture_output=True,text=True,timeout=90);(a.private_output/'native-fixed.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    def records(prefix):return [json.loads(s[len(prefix):]) for s in run.stdout.splitlines() if s.startswith(prefix)]
    assert {s['Path']:s['Sha256'] for s in records('fixed_actor_source=')}==reference['pins']
    clocks=records('fixed_actor_clock=');phases=records('fixed_actor_phase=');assert len(phases)==30;probes=ticks=0
    for pattern,partition in [('100hz',[10_000]),('20hz',[50_000]),('jitter',[7_000,11_000,23_000,41_000])]:
        for transformed in [False,True]:
            rows=[r for r in clocks if r['pattern']==pattern and r['transformed']==transformed];results=[r for r in phases if r['pattern']==pattern and r['transformed']==transformed]
            actor=ReferenceDoorActor(reference,transformed);now=frame=render=offset=0
            for (name,end,speed,allowed),n in zip(PHASES,results):
                while now<end:
                    now=min(end,now+partition[render%len(partition)]);render+=1;row=rows[offset];offset+=1
                    expected_frame=now//20_000;steps=expected_frame-frame;delta=f32(f32(speed)*f32(.02));movement=[0,0,delta] if transformed else [-delta,0,0]
                    for _ in range(steps):state=actor.advance(.02,movement,allowed);ticks+=1
                    assert (row['phase'],row['now'],row['steps'],row['frame'],row['pending'],row['dropped'],row['suspended'],row['fault'])==(name,now,steps,expected_frame,now%20_000,0,0,None)
                    frame=expected_frame;probes+=1
                assert n['phase']==name and n['now']==end
                for source,key in [('frame','frame'),('transfers','total'),('cell','cell'),('retained','retained'),('status','status'),('kind','kind')]:assert n[source]==state[key],(pattern,transformed,name,source,n[source],state[key])
                close(n['local'],state['local']);close(n['world'],state['world']);close(n['fraction'],state['fraction'],1e-6);close(n['curve'],state['curve'],1e-6)
            assert offset==len(rows)
    summary=dict(configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),source_pins=reference['pins'],render_patterns=3,owner_poses=2,clock_updates=probes,independent_ticks=ticks,phase_results=len(phases))
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(f'PASS clock_updates={probes} independent_ticks={ticks} phase_results={len(phases)} render_patterns=3 owner_poses=2')
if __name__=='__main__':main()
