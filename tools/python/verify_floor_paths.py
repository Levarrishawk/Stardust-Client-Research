"""Verify owned piecewise floor traces against pinned installed triangle planes."""
import argparse, collections, contextlib, hashlib, json, math, struct, subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_floor_walks import project

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','fixtures','private_output','core_dll'):
        parser.add_argument(name,type=Path)
    args=parser.parse_args()
    args.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve())
    args.private_output.mkdir(parents=True,exist_ok=True)
    raw=args.fixtures.read_bytes(); fixtures=json.loads(raw); digest=hashlib.sha256(raw).hexdigest().upper()
    run=subprocess.run(['dotnet',str(args.core_dll),'--floor-paths',str(args.root),'--configuration',str(args.configuration),'--fixtures',str(args.fixtures),'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=120)
    (args.private_output/'native-paths.txt').write_text(run.stdout,encoding='utf-8')
    paths={}; walks={}; counts=collections.Counter()
    for line in run.stdout.splitlines():
        if line.startswith('floor_path='):
            row=json.loads(line[11:]); key=(row['member'],row['index']); assert key not in paths; paths[key]=row
        elif line.startswith('walk='):
            row=json.loads(line[5:]); key=(row['member'],row['index']); assert key not in walks; walks[key]=row
    expected={(f['member'],i) for f in fixtures for i in range(len(f['queries']))}
    assert expected==paths.keys()==walks.keys()
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(args.root,args.configuration)]
        for fixture in fixtures:
            data=None
            for index in indices:
                data=index.lookup(fixture['member'])
                if data is not None: break
            assert hashlib.sha256(data).hexdigest().upper()==fixture['sha256']
            root,=nodes(data); version,=root[1]; positions,records=version[1][:2]
            positions,records=positions[1],records[1]
            if version[0]==b'0006': positions,records=positions[4:],records[4:]
            vertices=list(struct.iter_unpack('<3f',positions))
            triangles=[struct.unpack_from('<3iI3i3f4Bi3i',records,i) for i in range(0,len(records),60)]
            for i,query in enumerate(fixture['queries']):
                key=(fixture['member'],i); path=paths[key]; walk=walks[key]; segments=path['segments']
                assert all(path[k]==walk[k] for k in ('status','parameter','position')),(key,'result mismatch')
                assert walk['resume_status']=='Accepted',(key,'unusable locator')
                assert 1<=len(segments)<=4097,(key,'segment budget')
                assert segments[0]['triangle']==query['triangle'] and segments[0]['start_parameter']==0
                assert segments[-1]['triangle']==walk['triangle'] and segments[-1]['end_parameter']==walk['parameter']
                assert max(abs(a-b) for a,b in zip(segments[-1]['end'],walk['position']))<=1e-4,(key,'terminal quantization')
                for j,segment in enumerate(segments):
                    counts['segments']+=1
                    first,last=segment['start_parameter'],segment['end_parameter']
                    assert 0<=first<=last<=1,(key,j,'ordering')
                    points=[vertices[v] for v in triangles[segment['triangle']][:3]]
                    if max(p[1] for p in points)-min(p[1] for p in points)>1e-5: counts['sloped_segments']+=1
                    for which,t in (('start',first),('end',last)):
                        actual=segment[which]
                        assert all(math.isfinite(v) for v in actual)
                        expected_xz=[query['start'][axis]+(query['end'][axis]-query['start'][axis])*t for axis in (0,2)]
                        assert max(abs(actual[axis]-value) for axis,value in zip((0,2),expected_xz))<=1e-4,(key,j,which,'horizontal path')
                    # Midpoint checks height throughout each affine segment, not only terminal height.
                    for weight in (0,.25,.5,.75,1):
                        actual=[segment['start'][axis]*(1-weight)+segment['end'][axis]*weight for axis in range(3)]
                        plane=project(points,actual)
                        assert abs(actual[1]-plane[1])<=1e-4,(key,j,weight,'triangle plane',actual,plane)
                        counts['plane_checks']+=1
                    if j:
                        previous=segments[j-1]
                        assert previous['end_parameter']==first,(key,j,'parameter gap')
                        assert max(abs(a-b) for a,b in zip(previous['end'],segment['start']))<=1e-4,(key,j,'position gap')
                        assert segment['triangle'] in triangles[previous['triangle']][4:7],(key,j,'nonadjacent triangles')
                        counts['triangle_crossings']+=1
                counts['queries']+=1; counts['outcome_'+walk['status']]+=1
    summary=dict(floors=len(fixtures),fixtures_sha256=digest,configuration_sha256=hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper(),**counts)
    (args.private_output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print('PASS independent piecewise triangle planes: '+json.dumps(summary,sort_keys=True))

if __name__=='__main__': main()
