"""Independent modern small-riser reference over pinned original triangles."""
import math
from precise_door_actor_reference import PreciseReferenceDoorActor

class StepReferenceDoorActor(PreciseReferenceDoorActor):
    def __init__(self,reference,transformed,local_ray=False):
        self.local_ray=local_ray;super().__init__(reference,transformed)
    def step_piece(self,cell,source,target,time,point,height):
        assert source is not None and abs(point[1]-height)<=.2
        vertices,rows=self.r['floors'][cell];row=rows[source]
        # Only reciprocal solid vertical components may connect the two sheets.
        def vertical(ti):
            a,b,c=[vertices[i] for i in rows[ti][:3]];u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
            n=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
            return abs(n[1])<1e-4*math.sqrt(sum(x*x for x in n))
        def chord(ti):
            ps=[vertices[i] for i in rows[ti][:3]];edges=[(ps[i],ps[(i+1)%3]) for i in range(3)]
            a,b=max(edges,key=lambda e:math.hypot(e[1][0]-e[0][0],e[1][2]-e[0][2]));dx,dz=b[0]-a[0],b[2]-a[2];length=math.hypot(dx,dz)
            if not length:return None
            dx/=length;dz/=length;h=point[0]*dx+point[2]*dz;cuts=[]
            for a,b in edges:
                ha,hb=a[0]*dx+a[2]*dz,b[0]*dx+b[2]*dz
                if abs(hb-ha)<1e-12:
                    if abs(h-ha)<1e-8:cuts.extend((a[1],b[1]))
                else:
                    t=(h-ha)/(hb-ha)
                    if -1e-8<=t<=1+1e-8:cuts.append(a[1]+t*(b[1]-a[1]))
            if not cuts:return None
            a,b,c=ps;u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)];normal=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];norm=math.sqrt(sum(x*x for x in normal))
            if not norm or any(abs((point[0]-a[0])*normal[0]+(y-a[1])*normal[1]+(point[2]-a[2])*normal[2])/norm>1e-5 for y in (min(cuts),max(cuts))):return None
            return min(cuts),max(cuts)
        starts=[]
        for e,n in enumerate(row[4:7]):
            if n>=0 and vertical(n) and row[10+e]==1 and row[15+e]==-1:
                a,b=vertices[row[e]],vertices[row[(e+1)%3]];ex,ez=b[0]-a[0],b[2]-a[2];length=math.hypot(ex,ez)
                if length and abs((point[0]-a[0])*ez-(point[2]-a[2])*ex)/length<1e-7:starts.append((n,a,b))
        assert len(starts)==1,'Ambiguous/missing riser entrance'
        first,a,b=starts[0];nx,nz=-(b[2]-a[2]),b[0]-a[0];length=math.hypot(nx,nz);nx/=length;nz/=length
        pending=[first];visited=set();reaches=False
        while pending:
            ti=pending.pop()
            if ti in visited:continue
            visited.add(ti);assert len(visited)<=8;row=rows[ti];assert row[13]==0
            for vi in row[:3]:
                p=vertices[vi];assert abs(p[1]-height)<=.2
                if not self.local_ray:assert abs((p[0]-point[0])*nx+(p[2]-point[2])*nz)<=.001
            for e,n in enumerate(row[4:7]):
                if n<0 or row[10+e]==0:continue
                assert row[10+e]==1 and row[15+e]==-1;other=rows[n];a,b=row[e],row[(e+1)%3]
                assert any(other[j]==b and other[(j+1)%3]==a and other[4+j]==ti and other[10+j]==1 and other[15+j]==-1 for j in range(3))
                if n==target:reaches=True
                elif vertical(n) and (not self.local_ray or chord(n) is not None):pending.append(n)
        assert reaches,'No reciprocal supported step landing'
        if self.local_ray:
            intervals=[chord(i) for i in visited];assert all(i is not None for i in intervals);reached=min(height,point[1]);end=max(height,point[1])
            for lo,hi in sorted(intervals):
                if hi<reached:continue
                assert lo<=reached+1e-6;reached=max(reached,hi)
                if reached>=end-1e-6:break
            assert reached>=end-1e-6
        return [(time,time,[point[0],height,point[2]],point,source)]
