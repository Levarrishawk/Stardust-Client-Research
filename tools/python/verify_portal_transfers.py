"""Verify pinned original paired-floor transfers with independent local-ID mapping and segment/edge geometry."""
import argparse,contextlib,hashlib,json,struct,subprocess,math,collections
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_portal_layout import inspect
from verify_static_mesh_slice import nodes
from verify_floor_walks import exit_edge,project,f32

def center(points):return [f32(f32(f32(points[0][i]+points[1][i])+points[2][i])/3) for i in range(3)]
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('member');p.add_argument('sha256');p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    source_pins={};expected={};edge_rows=[]
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(a.root,a.configuration)]
        def read(member):
            for index in indices:
                value=index.lookup(member)
                if value is not None:
                    source_pins[member]=hashlib.sha256(value).hexdigest().upper();return value
            raise ValueError('Missing original source '+member)
        layout=inspect(read(a.member));assert source_pins[a.member]==a.sha256.upper();sides=collections.defaultdict(list);floors={}
        for ci,cell in enumerate(layout['cells']):
            for pi,ref in enumerate(cell['portals']):sides[ref['geometry_index']].append((ci,pi))
            if not cell['floor']:continue
            data=read(cell['floor']);root,=nodes(data);version,=root[1];vp,tr=version[1][:2];vp=vp[1];tr=tr[1]
            if version[0]==b'0006':vp=vp[4:];tr=tr[4:]
            vertices=list(struct.iter_unpack('<3f',vp));rows=[struct.unpack_from('<3iI3i3f4Bi3i',tr,i) for i in range(0,len(tr),60)];floors[ci]=(vertices,rows)
            for ti,row in enumerate(rows):
                for e in range(3):
                    portal=row[15+e]
                    if portal>=0 and row[4+e]==-1:
                        assert portal<len(cell['portals']);edge_rows.append(dict(cell=ci,triangle=ti,edge=e,portal=portal,geometry=cell['portals'][portal]['geometry_index'],points=[vertices[row[i]] for i in (e,(e+1)%3)]))
        for edge in edge_rows:
            paired=sides[edge['geometry']]
            if len(paired)!=2 or paired[0][0]==paired[1][0]:continue
            target,tp=next(side for side in paired if side[0]!=edge['cell'])
            if target not in floors:continue
            assert layout['cells'][edge['cell']]['portals'][edge['portal']]['target_cell']==target
            assert layout['cells'][target]['portals'][tp]['target_cell']==edge['cell']
            candidates=[other for other in edge_rows if other['cell']==target and other['portal']==tp and max(math.dist(x,y) for x,y in zip(edge['points'],reversed(other['points'])))<=1e-4]
            if len(candidates)!=1:continue
            other=candidates[0];sv,sr=floors[edge['cell']];tv,tr=floors[target];sp=[sv[i] for i in sr[edge['triangle']][:3]];ep=[tv[i] for i in tr[other['triangle']][:3]]
            start=center(sp);end=center(ep);hit=exit_edge(sp,start,end);assert hit and hit[1]==edge['edge'];t=hit[0]
            point=[start[i]+(end[i]-start[i])*t for i in range(3)];boundary=[f32(x) for x in project(sp,point)]
            link=dict(SourceCell=edge['cell'],SourceTriangle=edge['triangle'],SourceEdge=edge['edge'],SourcePortal=edge['portal'],GeometryIndex=edge['geometry'],TargetCell=target,TargetTriangle=other['triangle'],TargetEdge=other['edge'],TargetPortal=tp)
            for policy,status in [('allow','Accepted'),('deny','EntryDenied'),('barrier','PortalBlocked'),('unknown','PermissionUnknown')]:
                expected[(edge['cell'],edge['triangle'],edge['edge'],policy)]=dict(link=link,status=status,cell=target if policy=='allow' else edge['cell'],triangle=other['triangle'] if policy=='allow' else edge['triangle'],parameter=1 if policy=='allow' else t,transfers=1 if policy=='allow' else 0,position=project(ep,end) if policy=='allow' else boundary)
    run=subprocess.run(['dotnet',str(a.core_dll),'--portal-transfers',str(a.root),'--configuration',str(a.configuration),'--member',a.member,'--expected-sha256',a.sha256],capture_output=True,text=True,check=True,timeout=90)
    (a.private_output/'native-transfers.txt').write_text(run.stdout,encoding='utf-8');observed={};observed_pins={}
    for line in run.stdout.splitlines():
        if line.startswith('transfer_source='):
            row=json.loads(line[16:]);observed_pins[row['Path']]=row['Sha256']
        elif line.startswith('transfer='):
            row=json.loads(line[9:]);l=row['link'];observed[(l['SourceCell'],l['SourceTriangle'],l['SourceEdge'],row['policy'])]=row
    assert all(observed_pins[k]==v for k,v in source_pins.items());assert observed.keys()==expected.keys();mismatches=[]
    for key,e in expected.items():
        n=observed[key]
        if any(n[k]!=e[k] for k in ('link','status','cell','triangle','transfers')) or abs(n['parameter']-e['parameter'])>1e-6 or max(abs(x-y) for x,y in zip(n['position'],e['position']))>1e-4:mismatches.append((key,e,n))
    summary=dict(member=a.member,sha256=a.sha256.upper(),configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),source_pins=source_pins,directed_links=len(expected)//4,probes=len(expected),mismatches=mismatches)
    (a.private_output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    if mismatches:raise ValueError(f'Transfer disagreements {mismatches[:2]}')
    print(f'PASS root={a.root.resolve()} member={a.member} sha256={a.sha256.upper()} directed_links={len(expected)//4} independent_probes={len(expected)}')
if __name__=='__main__':main()
