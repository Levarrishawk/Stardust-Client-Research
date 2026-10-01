"""Independent synthetic closing-leaf translation/actor slab oracle."""
import argparse
import json
import math
import subprocess
from pathlib import Path
from door_actor_reference import cast, close
from verify_floor_path_boxes import reference_path, f32

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('core_dll',type=Path);p.add_argument('private_output',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    run=subprocess.run(['dotnet',str(a.core_dll),'--door-closing-safety'],capture_output=True,text=True,timeout=90)
    (a.private_output/'native-closing.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    records=[json.loads(s.split('=',1)[1]) for s in run.stdout.splitlines() if s.startswith('door_closing=')];assert len(records)==240
    held_ticks=0
    for transformed in (False,True):
        convert=lambda v:cast([v[2]+20,v[1]+4,-v[0]-9]) if transformed else cast(v)
        rows=[[0,0,1],[0,1,0],[-1,0,0]] if transformed else [[1,0,0],[0,1,0],[0,0,1]]
        def curve(fraction):return f32((1-math.cos(f32(math.pi)*fraction))*.5)
        def translation(fraction):return convert([f32(-2*curve(fraction)),0,0])
        for safety in (False,True):
            fraction=1.
            selected=[n for n in records if n['transformed']==transformed and n['safety']==safety]
            for step,n in enumerate(selected):
                assert n['step']==step;feet=convert([0 if step<40 else 1,0,0]);close(feet,n['feet'])
                raw=fraction-f32(.1)/2;candidate=f32(max(0,raw));current=translation(fraction);proposed=translation(candidate)
                displacement=cast([b-a for a,b in zip(current,proposed)]);end=cast([feet[i]-displacement[i] for i in range(3)])
                hit=reference_path(dict(minimum=cast([-.1,0,-1]),maximum=cast([.1,2,1]),pose=rows+[current],half=cast([.25,.9,.25]),allowed=False,separating=True),[0.,1.],[feet,end])
                held=safety and candidate<fraction and hit['status']!='Clear'
                event=-1 if held else 2 if fraction==1 and raw<1 else 3 if fraction>0 and raw<=0 else -1
                if not held:fraction=candidate
                assert n['held']==held and n['transition']==event and n['retained']==1,(transformed,safety,step,n,held,event)
                close(fraction,n['fraction'],1e-6);close(curve(fraction),n['curve'],1e-6);close(translation(fraction),n['position'],8e-5)
                held_ticks+=int(held)
    summary=dict(synthetic=True,ticks=len(records),owner_poses=2,guarded_and_historical=True,held_ticks=held_ticks)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8');print('PASS independent closing safety '+json.dumps(summary))

if __name__=='__main__':main()
