"""Independent original one-door actor reference with explicit modern closing sweep."""
import math
from door_actor_reference import cast, point
from step_door_actor_reference import StepReferenceDoorActor
from verify_floor_path_boxes import reference_path, f32

class ClosingReferenceDoorActor(StepReferenceDoorActor):
    def __init__(self,reference,transformed,guarded):
        self.guarded=guarded;super().__init__(reference,transformed,True)
        self.convert=lambda v:cast([v[2]+20,v[1]+4,-v[0]-9]) if transformed else cast(v)
        self.inverse=lambda v:cast([-f32(v[2]+9),f32(v[1]-4),f32(v[0]-20)]) if transformed else cast(v)
        self.world_double=lambda v:[v[2]+20,v[1]+4,-v[0]-9] if transformed else list(v)
        pose=reference['pose'];self.world_pose=[pose[2],pose[1],[-v for v in pose[0]],self.convert(pose[3])] if transformed else pose
        self.reset()

    def advance(self,seconds,delta,allowed):
        r=self.r;style=r['style'];desired=cast([a+b for a,b in zip(self.position,delta)])
        center=[self.position[0],self.position[1]+f32(.9),self.position[2]]
        if allowed is True and math.dist(center,point(self.world_pose,[0,1,0]))<=style['radius']:self.request=self.frame;self.retained=True
        opening=self.request is not None and self.frame-self.request<=1
        raw=self.fraction+(f32(seconds)/style['open'] if opening else -f32(seconds)/style['close']);candidate=f32(min(1,max(0,raw)))
        event=0 if self.fraction==0 and raw>0 else 1 if self.fraction<1 and raw>=1 else 2 if self.fraction==1 and raw<1 else 3 if self.fraction>0 and raw<=0 else -1
        def curve(fraction):
            powered=fraction**(1/style['spring']);return f32(powered*(1-style['smooth'])+(1-math.cos(f32(math.pi)*powered))*.5*style['smooth'])
        def leaf_pose(fraction):return self.world_pose[:3]+[cast(point(self.world_pose,cast([v*curve(fraction) for v in style['move']])))]
        held=False
        if self.guarded and candidate<self.fraction:
            before,after=leaf_pose(self.fraction),leaf_pose(candidate);displacement=cast([b-a for a,b in zip(before[3],after[3])])
            end=cast([self.position[i]-displacement[i] for i in range(3)])
            hit=reference_path(dict(minimum=r['lower'],maximum=r['upper'],pose=before,half=cast([.25,.9,.25]),allowed=False,separating=True),[0.,1.],[self.position,end])
            held=hit['status']!='Clear'
        if held:event=-1
        else:self.fraction=candidate
        if math.dist(self.position,self.world_pose[3])>=1.5*style['radius']:self.retained=False
        if self.retained and allowed is True:self.request=self.frame
        pose=leaf_pose(self.fraction);movement=self.precise_move(desired,pose,allowed);self.frame+=1
        return dict(frame=self.frame,total=self.total,cell=self.cell,retained=int(self.retained),transition=event,fraction=self.fraction,curve=curve(self.fraction),held=held,
                    world=self.position,leaf_position=pose[3],**movement)
