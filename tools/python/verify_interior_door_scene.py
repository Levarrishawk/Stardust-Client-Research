"""Independent synthetic three-cell/two-controller scene oracle; no original inputs."""
import argparse
import json
import math
import subprocess
from pathlib import Path
from verify_floor_path_boxes import reference_path, f32
from door_actor_reference import cast, close

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('core_dll',type=Path);p.add_argument('private_output',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    run=subprocess.run(['dotnet',str(a.core_dll),'--synthetic-interior-scene'],capture_output=True,text=True,timeout=90)
    (a.private_output/'native-scene.txt').write_text(run.stdout+run.stderr,encoding='utf-8');run.check_returncode()
    rows=[json.loads(line.split('=',1)[1]) for line in run.stdout.splitlines() if line.startswith('interior_scene=')];assert len(rows)==1440
    transfers=blocked=samples=0
    for transformed in (False,True):
        convert=lambda v:cast([v[2]+20,v[1]+4,-v[0]-9]) if transformed else cast(v)
        inverse=lambda v:cast([-f32(v[2]+9),f32(v[1]-4),f32(v[0]-20)]) if transformed else cast(v)
        def pose(geometry,curve=0):
            local=[[0,0,1],[0,1,0],[-1,0,0],[geometry*4,0,f32(2*curve)]]
            return [local[2],local[1],[-v for v in local[0]],convert(local[3])] if transformed else local
        def reset():return convert([f32(-.8),0,0]),0,0,0,[dict(fraction=0.,request=None,retained=False) for _ in range(2)]
        position,cell,frame,total,doors=reset()
        for sequence,n in enumerate(r for r in rows if r['transformed']==transformed):
            if n['reset']:position,cell,frame,total,doors=reset()
            assert n['sequence']==sequence
            start=inverse(position);dx=f32(min(f32(.08),max(f32(-.08),f32(n['target']-start[0]))))
            expected_delta=cast([0,0,-dx]) if transformed else cast([dx,0,0]);close(expected_delta,n['delta'],1e-4)
            desired=cast([x+y for x,y in zip(position,n['delta'])]);end=inverse(desired)
            curves=[];events=[]
            for g,d in enumerate(doors):
                present=cell in ((0,1) if g==0 else (1,2));allowed=True if g==0 else n['second_permission']
                center=[position[0],position[1]+f32(.9),position[2]];trigger=convert([g*4,1,0])
                if present and allowed is True and math.dist(center,trigger)<=2:d['request']=frame;d['retained']=True
                opening=d['request'] is not None and frame-d['request']<=1
                raw=d['fraction']+(f32(.05) if opening else -f32(.05)/2)
                event=0 if d['fraction']==0 and raw>0 else 1 if d['fraction']<1 and raw>=1 else 2 if d['fraction']==1 and raw<1 else 3 if d['fraction']>0 and raw<=0 else -1
                d['fraction']=f32(min(1,max(0,raw)));curve=f32((1-math.cos(f32(math.pi)*d['fraction']))*.5)
                if not present or math.dist(position,convert([g*4,0,0]))>=3:d['retained']=False
                if d['retained'] and allowed is True:d['request']=frame
                curves.append(curve);events.append(event)
            hits=[]
            for g in range(2):
                allowed=True if g==0 else n['second_permission']
                boxes=[]
                if allowed is not True:boxes.append(([-1.05,-.05,-.05],[1.05,2.05,.05],pose(g),allowed,'PermissionBarrier','EntryDenied' if allowed is False else 'PermissionUnknown'))
                boxes.append(([-1,0,-.1],[1,2,.1],pose(g,curves[g]),False,'DoorLeaf','PortalBlocked'))
                for lo,hi,box_pose,permission,kind,status in boxes:
                    hit=reference_path(dict(minimum=cast(lo),maximum=cast(hi),pose=box_pose,half=cast([.25,.9,.25]),allowed=permission,separating=True),[0.,1.],[position,desired])
                    if hit['status']!='Clear':hits.append((hit['parameter'],kind,status))
            parameter,kind,status=min(hits,key=lambda h:h[0]) if hits else (1.,None,'Accepted')
            lp=cast([start[i]+(end[i]-start[i])*parameter for i in range(3)]);lp[1]=0
            old=cell
            if cell==0 and lp[0]>0:cell=1
            elif cell==1 and lp[0]<0:cell=0
            elif cell==1 and lp[0]>4:cell=2
            elif cell==2 and lp[0]<4:cell=1
            moved=int(cell!=old);total+=moved;frame+=1;position=convert(lp)
            for key,value in dict(frame=frame,total=total,cell=cell,status=status,kind=kind,transfers=moved).items():assert n[key]==value,(sequence,key,n[key],value)
            close(lp,n['local'],8e-5);close(position,n['world'],8e-5);close(parameter,n['parameter'],5e-5)
            for g,d in enumerate(doors):
                observed=n['doors'][g];assert observed['geometry']==g and observed['retained']==int(d['retained']) and observed['transition']==events[g]
                close(d['fraction'],observed['fraction'],1e-6);close(curves[g],observed['curve'],1e-6)
            transfers+=moved;blocked+=int(kind is not None);samples+=1
    summary=dict(synthetic=True,ticks=samples,owner_poses=2,physical_leaves=2,cells=3,transfers=transfers,blocked_ticks=blocked)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8');print('PASS independent interior scene '+json.dumps(summary))

if __name__=='__main__':main()
