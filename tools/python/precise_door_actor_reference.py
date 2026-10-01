"""Independent floor-plane/triangle-edge oracle for a bounded paired-door corridor.

Original vertices provide geometry; no native trace, parser, or controller is imported.
Overlapping disconnected surfaces are selected by height continuity, so callers must
establish that the selected corridor has one continuous floor sheet.
"""
from door_actor_reference import ReferenceDoorActor, cast, point
from verify_original_floor_boxes import coordinates
from verify_floor_path_boxes import reference_path, f32
from verify_floor_walks import supported

class PreciseReferenceDoorActor(ReferenceDoorActor):
    def reset(self):
        super().reset();self.local_position=cast(self.r['initial'])

    def world_double(self,p):
        return [p[2]+30,p[1]+4,-p[0]-11] if self.transformed else list(p)

    def leg(self,cell,start,end):
        vertices,rows=self.r['floors'][cell];cuts=[0.,1.];dx=end[0]-start[0];dz=end[2]-start[2]
        for row in rows:
            if row[13]:continue
            for e in range(3):
                a,b=vertices[row[e]],vertices[row[(e+1)%3]];ex,ez=b[0]-a[0],b[2]-a[2]
                den=dx*ez-dz*ex
                if abs(den)<1e-15:continue
                ax,az=a[0]-start[0],a[2]-start[2];t=(ax*ez-az*ex)/den;u=(ax*dz-az*dx)/den
                if 0<t<1 and -1e-9<=u<=1+1e-9:cuts.append(t)
        cuts.sort();times=[]
        for t in cuts:
            if not times or t-times[-1]>1e-8:times.append(t)
        pieces=[];height=start[1];previous_triangle=None
        for a,b in zip(times,times[1:]):
            m=(a+b)*.5;x=start[0]+dx*m;z=start[2]+dz*m;choices=[]
            for ti,row in enumerate(rows):
                if row[13]:continue
                ps=[vertices[i] for i in row[:3]]
                if not supported(ps):continue
                try:w,y=coordinates(ps,x,z)
                except ZeroDivisionError:continue
                if min(w)>=-1e-8:choices.append((abs(y-height),ti,ps))
            assert choices,(cell,start,end,a,b,'uncovered corridor')
            _,ti,ps=min(choices);pa=[start[0]+dx*a,0,start[2]+dz*a];pb=[start[0]+dx*b,0,start[2]+dz*b]
            pa[1]=coordinates(ps,pa[0],pa[2])[1];pb[1]=coordinates(ps,pb[0],pb[2])[1]
            if abs(pa[1]-height)>=1e-4:
                pieces.extend(self.step_piece(cell,previous_triangle,ti,a,pa,height))
            pieces.append((a,b,pa,pb,ti));height=pb[1]
            previous_triangle=ti
        return pieces

    def step_piece(self,cell,source,target,time,point,height):
        raise AssertionError((cell,target,'disconnected height sheet',point[1],height))

    def precise_move(self,desired,leaf_pose,allowed):
        r=self.r;start=self.local_position;end=self.inverse(desired);old=self.cell
        anchor,normal=r['portal_anchor'],r['portal_normal']
        side=lambda p:sum((p[i]-anchor[i])*normal[i] for i in (0,2))
        s,e=side(start),side(end);owner_side=r['source_side'] if old==r['source'] else -r['source_side']
        cross=s*owner_side>=0 and e*owner_side<0;seam=s/(s-e) if cross else 1.
        stop=[start[i]+(end[i]-start[i])*seam for i in range(3)]
        paths=[]
        for a,b,pa,pb,ti in self.leg(old,start,stop):paths.append((seam*a,seam*b,pa,pb,old,ti))
        if cross:
            target=r['target'] if old==r['source'] else r['source'];landing=cast(paths[-1][3])
            # The public handoff stores a float world point projected onto the
            # paired target edge; reproduce that ABI rounding independently.
            vs,rs=r['floors'][target];portal=r['target_portal'] if target==r['target'] else r['portal']
            edges=[(vs[row[e]],vs[row[(e+1)%3]],row) for row in rs for e in range(3) if row[15+e]==portal and row[4+e]==-1]
            assert len(edges)==1
            wa,wb=[self.convert(p) for p in edges[0][:2]];wc=self.convert(landing);span=cast([wb[i]-wa[i] for i in range(3)]);offset=cast([wc[i]-wa[i] for i in range(3)])
            dot=lambda a,b:f32(f32(f32(a[0]*b[0])+f32(a[1]*b[1]))+f32(a[2]*b[2]))
            fraction=f32(min(1,max(0,dot(offset,span)/dot(span,span))))
            landing=self.inverse(cast([wa[i]+f32(span[i]*fraction) for i in range(3)]))
            ps=[vs[i] for i in edges[0][2][:3]];weights,_=coordinates(ps,landing[0],landing[2])
            if min(weights)<-1e-8:
                import math
                original=landing
                center=[f32(f32(f32(ps[0][i]+ps[1][i])+ps[2][i])/3) for i in range(3)]
                inward=cast([center[i]-landing[i] for i in range(3)]);distance=f32(math.sqrt(f32(sum(f32(v*v) for v in inward))))
                landing=cast([landing[i]+f32(inward[i]*f32(1e-5/distance)) for i in range(3)])
                if min(coordinates(ps,landing[0],landing[2])[0])<-1e-8:
                    directions=[]
                    for i,w in enumerate(weights):
                        if w>=-1e-8:continue
                        a,b,c=ps[(i+1)%3],ps[(i+2)%3],ps[i];nx,nz=-(b[2]-a[2]),b[0]-a[0]
                        if (c[0]-a[0])*nx+(c[2]-a[2])*nz<0:nx,nz=-nx,-nz
                        length=math.hypot(nx,nz);directions.append((nx/length,nz/length))
                    found=None
                    for nx,nz in directions:
                        for d in (1e-6,2e-6,5e-6,1e-5,2e-5,5e-5,1e-4):
                            p=cast([original[0]+nx*d,original[1],original[2]+nz*d]);w,y=coordinates(ps,p[0],p[2]);p[1]=f32(y)
                            if min(w)>=-1e-8 and math.dist(p,original)<=1e-5:found=p;break
                        if found is not None:break
                    assert found is not None,'Unrepresentable bounded portal landing';landing=found
            for a,b,pa,pb,ti in self.leg(target,landing,end):paths.append((seam+(1-seam)*a,seam+(1-seam)*b,pa,pb,target,ti))
        contacts=[]
        boxes=[(cast(r['barrier_lower']),cast(r['barrier_upper']),self.world_pose,allowed,'PermissionBarrier','EntryDenied' if allowed is False else 'PermissionUnknown'),
               (r['lower'],r['upper'],leaf_pose,False,'DoorLeaf','PortalBlocked')]
        for lower,upper,pose,permission,kind,status in boxes:
            for a,b,pa,pb,cell,ti in paths:
                row=dict(minimum=lower,maximum=upper,pose=pose,half=[f32(.25),f32(.9),f32(.25)],allowed=permission,separating=True)
                hit=reference_path(row,[a,b],[self.world_double(pa),self.world_double(pb)])
                if hit['status']!='Clear':contacts.append((hit['parameter'],kind,status,cell,ti,pa,pb,a,b,hit['normal']));break
        parameter=1.;kind=None;status='Accepted'
        if contacts:
            parameter,kind,status,cell,ti,pa,pb,a,b,normal=min(contacts,key=lambda c:c[0]);t=(parameter-a)/(b-a) if b>a else 0
            geometric=[pa[i]+(pb[i]-pa[i])*t for i in range(3)];lp=cast(geometric)
            if parameter>0:
                vertices,rows=r['floors'][cell];ps=[vertices[i] for i in rows[ti][:3]]
                def candidate(p):
                    p=cast(p);p[1]=f32(coordinates(ps,p[0],p[2])[1]);return p
                def valid(p):
                    d=[self.convert(p)[i]-self.world_double(geometric)[i] for i in range(3)]
                    return sum(v*v for v in d)<=1e-8 and sum(d[i]*normal[i] for i in range(3))>=-1e-6
                lp=candidate(lp)
                if not valid(lp):
                    local_normal=[-normal[2],normal[1],normal[0]] if self.transformed else normal
                    length=(local_normal[0]**2+local_normal[2]**2)**.5
                    choices=[candidate([geometric[i]+(local_normal[i]/length*offset if i in (0,2) else 0) for i in range(3)]) for offset in (1e-6,2e-6,5e-6,1e-5,2e-5,5e-5,1e-4)]
                    lp=next(p for p in choices if valid(p))
            if parameter==0:lp=start;cell=old
        else:
            _,_,_,lp,cell,ti=paths[-1];lp=cast(lp)
        vertices,rows=r['floors'][cell];lp[1]=f32(coordinates([vertices[i] for i in rows[ti][:3]],lp[0],lp[2])[1])
        self.cell=cell;self.local_position=lp;self.position=self.convert(lp);transfers=int(old!=cell);self.total+=transfers
        selected_contact=min(contacts,key=lambda c:c[0]) if contacts else None
        step_count=sum(a==b and abs(pa[1]-pb[1])>1e-4 and (a<parameter or a==parameter and (selected_contact is None or selected_contact[7]!=selected_contact[8])) for a,b,pa,pb,_,_ in paths)
        assert step_count<=1,'Reference supports one step per request'
        return dict(status=status,kind=kind,transfers=transfers,parameter=parameter,local=lp,steps=step_count)
