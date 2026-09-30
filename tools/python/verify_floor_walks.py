"""Check owned single-cell movement against independent XZ line/edge intersections on pinned installed floors."""
import argparse,contextlib,hashlib,json,struct,subprocess,collections,math
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes

def cross(a,b):return a[0]*b[1]-a[1]*b[0]
def sub(a,b):return (a[0]-b[0],a[1]-b[1])
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def center(points):return [f32(sum(v[i] for v in points)/3) for i in range(3)]
def area(points):return cross(sub((points[1][0],points[1][2]),(points[0][0],points[0][2])),sub((points[2][0],points[2][2]),(points[0][0],points[0][2])))
def supported(points):
    a,b,c=points;ab=[b[i]-a[i] for i in range(3)];ac=[c[i]-a[i] for i in range(3)]
    n=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
    return abs(n[1])>=1e-4*math.sqrt(sum(x*x for x in n)) and n[1]!=0
def project(points,p):
    p=list(p);a,b,c=points;ab=[b[i]-a[i] for i in range(3)];ac=[c[i]-a[i] for i in range(3)]
    nx=ab[1]*ac[2]-ab[2]*ac[1];ny=ab[2]*ac[0]-ab[0]*ac[2];nz=ab[0]*ac[1]-ab[1]*ac[0]
    p[1]=f32(a[1]-(nx*(p[0]-a[0])+nz*(p[2]-a[2]))/ny);return p
def contact(points):return project(points,center(points))
def exit_edge(points,start,end):
    s=(start[0],start[2]);d=(end[0]-start[0],end[2]-start[2]);hits=[]
    for e in range(3):
        a=(points[e][0],points[e][2]);b=(points[(e+1)%3][0],points[(e+1)%3][2]);v=sub(b,a);den=cross(d,v)
        if not den:continue
        t=cross(sub(a,s),v)/den;u=cross(sub(a,s),d)/den
        if 1e-6<t<1-1e-6 and 1e-6<u<1-1e-6:hits.append((t,e))
    return min(hits) if hits else None

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','inventory','private_output','core_dll'):p.add_argument(name,type=Path)
    a=p.parse_args();a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    inventory=json.loads(a.inventory.read_text(encoding='utf-8'));fixtures=[];expected={};counts=collections.Counter()
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(a.root,a.configuration)]
        for item in inventory:
            data=None
            for index in indices:
                data=index.lookup(item['member'])
                if data is not None:break
            assert hashlib.sha256(data).hexdigest().upper()==item['sha256']
            root,=nodes(data);version,=root[1];positions,records=version[1][:2];positions=positions[1];records=records[1]
            if version[0]==b'0006':positions=positions[4:];records=records[4:]
            vertices=list(struct.iter_unpack('<3f',positions));rows=[struct.unpack_from('<3iI3i3f4Bi3i',records,i) for i in range(0,len(records),60)]
            queries=[];seen=set()
            for ti,row in enumerate(rows):
                if row[13]:continue
                points=[vertices[i] for i in row[:3]];normal=cross(sub((points[1][0],points[1][2]),(points[0][0],points[0][2])),sub((points[2][0],points[2][2]),(points[0][0],points[0][2])))
                if abs(normal)<1e-6 or not supported(points):continue
                start=contact(points)
                # No-op verifies locator/projection independently of adjacency.
                if 'noop' not in seen:
                    queries.append(dict(triangle=ti,start=start,end=start));expected[(item['member'],len(queries)-1)]=dict(status='Accepted',triangle=ti,parameter=1,edge=-1,portal=-1,position=start);seen.add('noop')
                for edge in range(3):
                    neighbor=row[4+edge];flag=row[10+edge];portal=row[15+edge]
                    status=('PortalBoundary' if neighbor==-1 else 'Accepted') if portal!=-1 else 'BlockedEdge' if flag==0 else 'UnsupportedEdgePolicy' if flag!=1 else 'OpenBoundary' if neighbor==-1 else 'Accepted'
                    if status in seen:continue
                    if status=='Accepted':
                        next_row=rows[neighbor]
                        if next_row[13]:continue
                        if not any(next_row[e]==row[(edge+1)%3] and next_row[(e+1)%3]==row[edge] and next_row[4+e]==ti for e in range(3)):continue
                        next_points=[vertices[i] for i in next_row[:3]]
                        if abs(area(next_points))<1e-6 or not supported(next_points):continue
                        end=contact(next_points)
                    else:
                        midpoint=[(points[edge][i]+points[(edge+1)%3][i])/2 for i in range(3)];end=[f32(2*midpoint[i]-start[i]) for i in range(3)]
                    hit=exit_edge(points,start,end)
                    if not hit or hit[1]!=edge:continue
                    if status=='Accepted' and exit_edge([vertices[i] for i in rows[neighbor][:3]],start,end) is None:continue
                    parameter=1 if status=='Accepted' else hit[0];point=[start[i]+(end[i]-start[i])*parameter for i in range(3)]
                    position=project(next_points if status=='Accepted' else points,point)
                    position=[f32(x) for x in position]
                    queries.append(dict(triangle=ti,start=start,end=end));expected[(item['member'],len(queries)-1)]=dict(status=status,triangle=neighbor if status=='Accepted' else ti,parameter=parameter,edge=-1 if status=='Accepted' else edge,portal=portal if status=='PortalBoundary' else -1,position=position);seen.add(status)
                if len(seen)>=6:break
            if queries:fixtures.append(dict(member=item['member'],sha256=item['sha256'],queries=queries))
    fixture=a.private_output/'walks.json';fixture.write_text(json.dumps(fixtures,separators=(',',':')),encoding='utf-8');digest=hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
    print(f'root={a.root.resolve()} configuration_sha256={hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper()} inventory_sha256={hashlib.sha256(a.inventory.read_bytes()).hexdigest().upper()}',flush=True)
    print(f'fixtures_sha256={digest} input_floors={len(inventory)} eligible_floors={len(fixtures)} queries={len(expected)}',flush=True)
    run=subprocess.run(['dotnet',str(a.core_dll),'--floor-walks',str(a.root),'--configuration',str(a.configuration),'--fixtures',str(fixture),'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=120)
    (a.private_output/'native-walks.txt').write_text(run.stdout,encoding='utf-8');observed={}
    for line in run.stdout.splitlines():
        if line.startswith('walk='):
            row=json.loads(line[5:]);observed[(row['member'],row['index'])]=row
    assert expected.keys()==observed.keys();mismatches=[]
    for key,value in expected.items():
        row=observed[key];counts[value['status']]+=1
        if any(row[k]!=value[k] for k in ('status','triangle','edge','portal')) or abs(row['parameter']-value['parameter'])>1e-6 or max(abs(x-y) for x,y in zip(row['position'],value['position']))>1e-4:mismatches.append((key,value,row))
    (a.private_output/'summary.json').write_text(json.dumps(dict(floors=len(fixtures),queries=len(expected),outcomes=counts,mismatches=mismatches[:30]),indent=2),encoding='utf-8')
    if mismatches:raise ValueError(f'Walk disagreements={len(mismatches)} first={mismatches[:2]}')
    print(f'PASS independent line/edge agreement: {dict(counts)}')
if __name__=='__main__':main()
