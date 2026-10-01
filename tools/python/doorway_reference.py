"""Independent pinned normal-door instance/reference selection for direct or rigid detail primaries."""
import contextlib, hashlib, math, struct
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_portal_layout import inspect
from verify_static_mesh_slice import nodes
from verify_single_leaf_door import TABLE, TABLE_SHA, table_rows, walk
from verify_interior_door import floor, project, point, local
from door_actor_reference import cast

def vertical_hit(f,p):
    hits=[]
    for ti,row in enumerate(f[1]):
        if row[13]:continue
        try:_,position=project((f[0],[row]),p)
        except AssertionError:continue
        if p[1]-5<=position[1]<=p[1]+5:hits.append((ti,position))
    assert hits,'No independent vertical segment floor hit'
    # Original inspection casts downward from authored approach +5, choosing the nearest segment hit.
    return min(hits,key=lambda h:(-h[1][1],h[0]))

def project_near_height(f,p):
    hits=[]
    for ti,row in enumerate(f[1]):
        if row[13]:continue
        try:_,position=project((f[0],[row]),p)
        except AssertionError:continue
        hits.append((ti,position))
    assert hits,'No independent floor projection'
    return min(hits,key=lambda h:(abs(h[1][1]-p[1]),h[0]))

def load_doorway_reference(root,configuration,member,layout_sha,source,portal,leaf_sha,connected=False):
    pins={}
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(root,configuration)]
        def read(path):
            for index in indices:
                raw=index.lookup(path)
                if raw is not None:
                    assert raw,'Missing/masked doorway input';pins[path]=hashlib.sha256(raw).hexdigest().upper();return raw
            raise ValueError('Missing doorway input '+path)
        layout=inspect(read(member));assert pins[member]==layout_sha
        ref=layout['cells'][source]['portals'][portal];target=ref['target_cell'];others=[(i,r) for i,r in enumerate(layout['cells'][target]['portals']) if r['geometry_index']==ref['geometry_index'] and r['target_cell']==source]
        assert len(others)==1;target_portal,reverse=others[0]
        assert ref['door_name']==reverse['door_name'] and ref['transform_flag']==reverse['transform_flag']==1 and ref['transform_hex']==reverse['transform_hex']
        style=next(r for r in table_rows(read(TABLE)) if r['name']==ref['door_name']);assert pins[TABLE]==TABLE_SHA
        path=style['primary'];raw=read(path);assert pins[path]==leaf_sha;primary_nodes,=nodes(raw);visited=set()
        while primary_nodes[0]==b'APT ':
            assert path not in visited and len(visited)<16;visited.add(path);path=next(v.rstrip(b'\0').decode('ascii') for tag,v in walk([primary_nodes]) if tag==b'NAME');raw=read(path);primary_nodes,=nodes(raw)
        assert primary_nodes[0] in (b'MESH',b'DTLA')
        if primary_nodes[0]==b'DTLA':
            for tag,value in walk([primary_nodes]):
                if tag==b'CHLD':
                    child=value[4:].rstrip(b'\0').decode('ascii').replace('\\','/').lower();child=child if child.startswith('appearance/') else 'appearance/'+child
                    child_nodes,=nodes(read(child));assert child_nodes[0]==b'MESH'
        appr=primary_nodes[1][0][1][0];assert appr[0]==b'APPR';extent=appr[1][0][1][0];assert extent[0]==b'EXBX'
        box=next(v for tag,v in walk([extent]) if tag==b'BOX ');upper=list(struct.unpack_from('<3f',box));lower=list(struct.unpack_from('<3f',box,12))
        floors={c:floor(read(layout['cells'][c]['floor'])) for c in (source,target)}
    values=struct.unpack('<12f',bytes.fromhex(ref['transform_hex']));pose=[list(values[:3]),list(values[4:7]),list(values[8:11]),[values[3],values[7],values[11]]]
    def edges(c,portal):
        vs,rows=floors[c]
        return [(ti,e,[vs[row[e]],vs[row[(e+1)%3]]]) for ti,row in enumerate(rows) for e in range(3) if row[15+e]==portal and row[4+e]==-1]
    matches=[(a,b) for a in edges(source,portal) for b in edges(target,target_portal) if max(math.dist(p,q) for p,q in zip(a[2],reversed(b[2])))<=1e-4]
    assert len(matches)==1,'Selected reference requires one matched floor edge'
    a,b=matches[0];vertices,rows=floors[source];center=[sum(vertices[i][k] for i in rows[a[0]][:3])/3 for k in range(3)]
    normal=[pose[i][2] for i in range(3)];side=sum((center[i]-pose[3][i])*normal[i] for i in range(3));assert abs(side)>1e-6;side=1 if side>0 else -1
    initial=cast(vertical_hit(floors[source],point(pose,[0,0,side*.8]))[1]);end=cast(vertical_hit(floors[target],point(pose,[0,0,-side*.8]))[1])
    if connected:
        from step_door_actor_reference import StepReferenceDoorActor
        oracle=StepReferenceDoorActor.__new__(StepReferenceDoorActor);oracle.r={'floors':floors};oracle.local_ray=True
        def endpoint(cell,edge):
            vs,rows=floors[cell];ti,_,(a,b)=edge;mid=[(u+v)*.5 for u,v in zip(a,b)]
            center=[sum(vs[i][k] for i in rows[ti][:3])/3 for k in range(3)]
            nx,nz=-(b[2]-a[2]),b[0]-a[0];length=math.hypot(nx,nz)
            if nx*(center[0]-mid[0])+nz*(center[2]-mid[2])<0:nx,nz=-nx,-nz
            desired=[mid[0]+nx/length*.8,mid[1],mid[2]+nz/length*.8]
            return cast(oracle.leg(cell,mid,desired)[-1][3])
        initial,end=endpoint(source,a),endpoint(target,b)
    delta=[end[i]-initial[i] for i in range(3)];length=math.hypot(delta[0],delta[2]);assert length>1e-6;direction=[delta[0]/length,0,delta[2]/length]
    anchor=a[2][0];edge=[a[2][1][i]-anchor[i] for i in range(3)];n=[edge[2],0,-edge[0]];length=math.hypot(n[0],n[2]);assert length>1e-6;n=[v/length for v in n]
    sign=sum((center[i]-anchor[i])*n[i] for i in range(3));assert abs(sign)>1e-6
    vertices=list(struct.iter_unpack('<3f',bytes.fromhex(layout['geometries'][ref['geometry_index']]['vertex_hex'])));vs=[local(pose,v) for v in vertices]
    return dict(pins=pins,style=style,floors=floors,pose=pose,initial=initial,end=end,direction=direction,source=source,target=target,portal=portal,target_portal=target_portal,
                portal_anchor=anchor,portal_normal=n,source_side=1 if sign>0 else -1,lower=lower,upper=upper,
                barrier_lower=[min(v[i] for v in vs)-.05 for i in range(3)],barrier_upper=[max(v[i] for v in vs)+.05 for i in range(3)],layout=layout,project=project_near_height)
