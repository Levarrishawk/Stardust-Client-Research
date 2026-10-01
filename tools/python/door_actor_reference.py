"""Shared independent oracle for owned original-doorway fixtures; no native parser/controller imports."""
import contextlib, hashlib, math, struct
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_portal_layout import inspect
from verify_static_mesh_slice import nodes
from verify_single_leaf_door import TABLE, TABLE_SHA, LEAF_SHA, MESH_SHA, table_rows, f32, walk
from verify_interior_door import MEMBER, LAYOUT_SHA, floor, project, point, local

def cast(v):return [f32(x) for x in v]
def close(a,b,tol=8e-5):
    if isinstance(a,(list,tuple)):assert len(a)==len(b);[close(x,y,tol) for x,y in zip(a,b)]
    else:assert abs(a-b)<tol,(a,b)
def hit_box(p,lower,upper,start,end):
    body=[f32(.25),f32(.9),f32(.25)]
    s=local(p,[start[0],start[1]+body[1],start[2]]);e=local(p,[end[0],end[1]+body[1],end[2]])
    extent=[sum(abs(p[j][i])*body[j] for j in range(3)) for i in range(3)]
    lo=[lower[i]-extent[i] for i in range(3)];hi=[upper[i]+extent[i] for i in range(3)]
    for i in range(3):
        if abs(s[i]-lo[i])<=1e-5 and e[i]<s[i] or abs(s[i]-hi[i])<=1e-5 and e[i]>s[i]:return None
    entry=0.;exit=1.
    for i in range(3):
        delta=e[i]-s[i]
        if abs(delta)<1e-12:
            if not lo[i]<=s[i]<=hi[i]:return None
            continue
        near=(lo[i]-s[i])/delta;far=(hi[i]-s[i])/delta
        if near>far:near,far=far,near
        entry=max(entry,near);exit=min(exit,far)
        if entry>exit:return None
    if exit<0 or entry>1:return None
    return max(0,entry)
def load_reference(root,configuration):
    pins={}
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(root,configuration)]
        def read(m):
            for index in indices:
                raw=index.lookup(m)
                if raw is not None:
                    assert raw;pins[m]=hashlib.sha256(raw).hexdigest().upper();return raw
            raise ValueError(m)
        layout=inspect(read(MEMBER));assert pins[MEMBER]==LAYOUT_SHA
        style=next(r for r in table_rows(read(TABLE)) if r['name']=='cantina_door');assert pins[TABLE]==TABLE_SHA
        apt=read(style['primary']);assert pins[style['primary']]==LEAF_SHA
        mesh_path=next(v.rstrip(b'\0').decode('ascii') for t,v in walk(nodes(apt)) if t==b'NAME');mesh=read(mesh_path);assert pins[mesh_path]==MESH_SHA
        appr=next(v for t,v in walk(nodes(mesh)) if t==b'APPR');box=next(v for t,v in walk(appr) if t==b'BOX ');upper=struct.unpack_from('<3f',box);lower=struct.unpack_from('<3f',box,12)
        floors={c:floor(read(layout['cells'][c]['floor'])) for c in [3,12]}
        assert pins[layout['cells'][3]['floor']]=='DC61558BC59587861D0AAD94724783A0400662F302885656C96BE75A4E8DBAD5'
        assert pins[layout['cells'][12]['floor']]=='5A28497651ADAD4FE477BF0DE3245749A2CBC314C242E4B469E7C79F705F9A2C'
    reference=layout['cells'][3]['portals'][10];reverse=layout['cells'][12]['portals'][0];assert reference['transform_hex']==reverse['transform_hex'] and reference['door_name']==reverse['door_name']=='cantina_door'
    values=struct.unpack('<12f',bytes.fromhex(reference['transform_hex']));pose=[list(values[0:3]),list(values[4:7]),list(values[8:11]),[values[3],values[7],values[11]]]
    initial=cast(project(floors[3],point(pose,[0,0,-.8]))[1]);portal_x=floors[3][0][floors[3][1][97][1]][0]
    vertices=list(struct.iter_unpack('<3f',bytes.fromhex(layout['geometries'][13]['vertex_hex'])));vs=[local(pose,v) for v in vertices]
    return dict(pins=pins,style=style,floors=floors,pose=pose,initial=initial,portal_x=portal_x,lower=lower,upper=upper,
        barrier_lower=[min(v[i] for v in vs)-.05 for i in range(3)],barrier_upper=[max(v[i] for v in vs)+.05 for i in range(3)])

class ReferenceDoorActor:
    def __init__(self,reference,transformed):
        self.r=reference;self.transformed=transformed;pose=reference['pose']
        self.world_pose=[[pose[2][i] for i in range(3)],pose[1],[-pose[0][i] for i in range(3)],self.convert(pose[3])] if transformed else pose
        self.reset()
    def convert(self,v):return cast([v[2]+30,v[1]+4,-v[0]-11]) if self.transformed else cast(v)
    def inverse(self,v):return cast([-f32(v[2]+11),f32(v[1]-4),f32(v[0]-30)]) if self.transformed else cast(v)
    def reset(self):self.position=self.convert(self.r['initial']);self.cell=self.r.get('source',3);self.fraction=0.;self.request=None;self.retained=False;self.frame=0;self.total=0
    def advance(self,seconds,delta,allowed):
        r=self.r;style=r['style'];desired=cast([x+y for x,y in zip(self.position,delta)])
        center=[self.position[0],self.position[1]+f32(.9),self.position[2]];trigger=point(self.world_pose,[0,1,0])
        if allowed is True and math.dist(center,trigger)<=style['radius']:self.request=self.frame;self.retained=True
        opening=self.request is not None and self.frame-self.request<=1
        raw=self.fraction+(f32(seconds)/style['open'] if opening else -f32(seconds)/style['close'])
        event=0 if self.fraction==0 and raw>0 else 1 if self.fraction<1 and raw>=1 else 2 if self.fraction==1 and raw<1 else 3 if self.fraction>0 and raw<=0 else -1
        self.fraction=f32(min(1,max(0,raw)));powered=self.fraction**(1/style['spring']);curve=f32(powered*(1-style['smooth'])+(1-math.cos(f32(math.pi)*powered))*.5*style['smooth'])
        if math.dist(self.position,self.world_pose[3])>=1.5*style['radius']:self.retained=False
        if self.retained and allowed is True:self.request=self.frame
        leaf_pose=self.world_pose[:3]+[cast(point(self.world_pose,[f32(v*curve) for v in style['move']]))];contacts=[]
        if hasattr(self,'precise_move'):
            result=self.precise_move(desired,leaf_pose,allowed);self.frame+=1
            return dict(frame=self.frame,total=self.total,cell=self.cell,retained=int(self.retained),transition=event,fraction=self.fraction,curve=curve,world=self.position,**result)
        if allowed is not True:
            t=hit_box(self.world_pose,r['barrier_lower'],r['barrier_upper'],self.position,desired)
            if t is not None:contacts.append((t,'PermissionBarrier','EntryDenied' if allowed is False else 'PermissionUnknown'))
        t=hit_box(leaf_pose,r['lower'],r['upper'],self.position,desired)
        if t is not None:contacts.append((t,'DoorLeaf','PortalBlocked'))
        old_cell=self.cell;parameter=1.;kind=None;status='Accepted'
        if contacts:parameter,kind,status=min(contacts,key=lambda c:c[0]);desired=cast([x+(y-x)*parameter for x,y in zip(self.position,desired)])
        lp=self.inverse(desired)
        source,target=r.get('source',3),r.get('target',12)
        if 'portal_normal' in r:
            side=sum((lp[i]-r['portal_anchor'][i])*r['portal_normal'][i] for i in range(3))*r['source_side']
            crossed=(self.cell==source and side<0) or (self.cell==target and side>0)
        else:crossed=(self.cell==source and lp[0]<r['portal_x']) or (self.cell==target and lp[0]>r['portal_x'])
        if not contacts and crossed:
            assert allowed is True;self.cell=target if self.cell==source else source;self.total+=1
        _,lp=r.get('project',project)(r['floors'][self.cell],lp);lp=cast(lp);self.position=self.convert(lp);self.frame+=1
        return dict(frame=self.frame,total=self.total,cell=self.cell,retained=int(self.retained),transition=event,status=status,kind=kind,transfers=int(old_cell!=self.cell),parameter=parameter,fraction=self.fraction,curve=curve,local=lp,world=self.position)
