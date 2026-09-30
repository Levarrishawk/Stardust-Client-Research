"""Independent Bellman-Ford agreement for modern directed Euclidean inspection routes."""
import argparse,contextlib,hashlib,json,math,subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_path_graph import inspect

def reference(ns,es,source,target,allowed):
    if not allowed[source] or not allowed[target]:return None
    distances=[math.inf]*len(ns);distances[source]=0
    weights=[math.dist(ns[e[0]][4:7],ns[e[1]][4:7]) for e in es]
    for _ in range(len(ns)-1):
        changed=False
        for edge,weight in zip(es,weights):
            a,b=edge[:2]
            if not allowed[a] or not allowed[b]:continue
            candidate=distances[a]+weight
            if candidate<distances[b]:distances[b]=candidate;changed=True
        if not changed:break
    return distances[target] if math.isfinite(distances[target]) else None

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('root',type=Path);p.add_argument('configuration',type=Path);p.add_argument('inventory',type=Path);p.add_argument('private_output',type=Path);p.add_argument('core_dll',type=Path);a=p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());a.private_output.mkdir(parents=True,exist_ok=True)
    fixtures=[];graphs={};expected={}
    with contextlib.ExitStack() as stack:
        indices=[(priority,Index(stack.enter_context(path.open('rb')))) for priority,path in archives(a.root,a.configuration)]
        for item in json.loads(a.inventory.read_text(encoding='utf-8')):
            if item['nodes']==0:continue
            for priority,index in indices:
                data=index.lookup(item['member'])
                if data is not None:break
            assert hashlib.sha256(data).hexdigest().upper()==item['sha256'];r,=nodes(data);v,=r[1];g=next(value for tag,value in v[1] if tag==b'PGRF');record,ns,es,_,_=inspect(g);graphs[item['member']]=(ns,es)
            count=len(ns);pairs=sorted({(0,count-1),(count-1,0),(0,count//2),(count//2,count-1),(count//2,0),(0,0)})
            queries=[dict(**{'from':s,'to':t}) for s,t in pairs]
            queries.append(dict(**{'from':0,'to':count-1},available=[i%3!=1 for i in range(count)]))
            for qi,q in enumerate(queries):expected[(item['member'],qi)]=reference(ns,es,q['from'],q['to'],q.get('available',[True]*count))
            fixtures.append(dict(member=item['member'],sha256=item['sha256'],queries=queries))
    fixture=a.private_output/'routes.json';fixture.write_text(json.dumps(fixtures,separators=(',',':')),encoding='utf-8');digest=hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
    print(f'fixtures_sha256={digest} graphs={len(fixtures)} queries={len(expected)} reachable={sum(v is not None for v in expected.values())}',flush=True)
    result=subprocess.run(['dotnet',str(a.core_dll),'--path-routes',str(a.root),'--configuration',str(a.configuration),'--fixtures',str(fixture),'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=90);(a.private_output/'native-route-agreement.txt').write_text(result.stdout,encoding='utf-8')
    observed={}
    for line in result.stdout.splitlines():
        if not line.startswith('route='):continue
        row=json.loads(line[6:]);observed[(row['member'],row['index'])]=row['route']
    assert observed.keys()==expected.keys()
    queries={(item['member'],i):q for item in fixtures for i,q in enumerate(item['queries'])}
    for key,cost in expected.items():
        route=observed[key];q=queries[key];ns,es=graphs[key[0]]
        if cost is None:assert route is None;continue
        assert route is not None and abs(route['distance']-cost)<=1e-8*max(1,cost)
        assert route['nodes'][0]==q['from'] and route['nodes'][-1]==q['to'] and len(route['edges'])+1==len(route['nodes']) and len(set(route['nodes']))==len(route['nodes'])
        if 'available' in q:assert all(q['available'][node] for node in route['nodes'])
        measured=0
        for source,target,edge_index in zip(route['nodes'],route['nodes'][1:],route['edges']):
            edge=es[edge_index];assert edge[:2]==(source,target);measured+=math.dist(ns[source][4:7],ns[target][4:7])
        assert abs(measured-cost)<=1e-8*max(1,cost)
    print(f'PASS independent Bellman-Ford directed routes queries={len(expected)}')
if __name__=='__main__':main()
