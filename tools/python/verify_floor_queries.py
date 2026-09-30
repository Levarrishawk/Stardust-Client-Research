"""Compare deterministic accelerated floor segments to exhaustive plane/barycentric tests."""
import argparse,contextlib,hashlib,json,math,struct,subprocess,time
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes

def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def read_geometry(data):
    r,=nodes(data);v,=r[1];p,t=v[1][:2];p=p[1];t=t[1]
    if v[0]==b'0006':p=p[4:];t=t[4:]
    vertices=list(struct.iter_unpack('<3f',p));triangles=[]
    for i in range(0,len(t),60):
        a,b,c,id=struct.unpack_from('<3iI',t,i);triangles.append((vertices[a],vertices[b],vertices[c],id,t[i+43]))
    return vertices,triangles

def reference(triangles,start,end,include):
    direction=sub(end,start);best=None
    for index,(a,b,c,id,non_solid) in enumerate(triangles):
        if non_solid and not include:continue
        ab=sub(b,a);ac=sub(c,a);normal=cross(ab,ac);den=dot(normal,direction)
        if den==0:continue
        parameter=dot(normal,sub(a,start))/den
        if parameter<0 or parameter>1:continue
        point=tuple(o+d*parameter for o,d in zip(start,direction));ap=sub(point,a)
        aa=dot(ab,ab);bb=dot(ab,ac);cc=dot(ac,ac);d=dot(ap,ab);e=dot(ap,ac);det=aa*cc-bb*bb
        if det==0:continue
        u=(cc*d-bb*e)/det;v=(aa*e-bb*d)/det
        if u < -1e-10 or v < -1e-10 or u+v > 1+1e-10:continue
        if best is None or parameter<best['parameter']-1e-10 or abs(parameter-best['parameter'])<=1e-10 and index<best['triangle']:
            best=dict(triangle=index,id=id,parameter=parameter,position=point)
    return best

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('inventory',type=Path);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    inventory=json.loads(a.inventory.read_text(encoding='utf-8'));fixtures=[];expected={};hits=0
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        for item in inventory:
            for priority,index in indices:
                data=index.lookup(item['member'])
                if data is not None:break
            assert hashlib.sha256(data).hexdigest().upper()==item['sha256'];vertices,triangles=read_geometry(data)
            if not vertices or not triangles:continue
            lo=min(v[1] for v in vertices)-1;hi=max(v[1] for v in vertices)+1;queries=[]
            for ti in sorted({0,len(triangles)//2,len(triangles)-1}):
                tri=triangles[ti];x=f32(sum(v[0] for v in tri[:3])/3);z=f32(sum(v[2] for v in tri[:3])/3)
                for include in (False,True):queries.append(dict(start=[x,f32(hi),z],end=[x,f32(lo),z],include_non_solid=include))
            x=f32(max(v[0] for v in vertices)+10);z=f32(max(v[2] for v in vertices)+10)
            queries.append(dict(start=[x,f32(hi),z],end=[x,f32(lo),z],include_non_solid=True))
            for qi,q in enumerate(queries):
                value=reference(triangles,q['start'],q['end'],q['include_non_solid']);expected[(item['member'],qi)]=value;hits+=value is not None
            fixtures.append(dict(member=item['member'],sha256=item['sha256'],queries=queries))
    fixture=a.private_output/'queries.json';fixture.write_text(json.dumps(fixtures,separators=(',',':')),encoding='utf-8');digest=hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
    print(f'fixtures_sha256={digest} floors={len(fixtures)} queries={len(expected)} hits={hits}',flush=True)
    result=subprocess.run(['dotnet',str(a.core_dll),'--floor-queries',str(a.root),'--configuration',str(a.configuration),'--fixtures',str(fixture),'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=90)
    (a.private_output/'native-query-agreement.txt').write_text(result.stdout,encoding='utf-8');observed={}
    for line in result.stdout.splitlines():
        if not line.startswith('query='):continue
        row=json.loads(line[6:]);observed[(row['member'],row['index'])]=row['hit']
    assert observed.keys()==expected.keys()
    mismatches=[]
    for key,e in expected.items():
        n=observed[key]
        if e is None or n is None:
            if e!=n:mismatches.append((key,e,n))
        elif e['triangle']!=n['triangle'] or e['id']!=n['id'] or abs(e['parameter']-n['parameter'])>1e-8 or max(abs(x-y) for x,y in zip(e['position'],n['position']))>1e-4:mismatches.append((key,e,n))
    (a.private_output/'reference-summary.json').write_text(json.dumps(dict(floors=len(fixtures),queries=len(expected),hits=hits,mismatches=mismatches[:100]),indent=2),encoding='utf-8')
    if mismatches:raise ValueError(f'Query mismatches={len(mismatches)} first={mismatches[:2]}')
    print(f'PASS independent exhaustive plane/barycentric agreement queries={len(expected)} hits={hits}')
if __name__=='__main__':main()
