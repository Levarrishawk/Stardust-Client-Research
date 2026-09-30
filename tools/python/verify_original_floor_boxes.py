"""Check synthetic box contacts and reusable landings on independently audited original floor traces."""
import argparse,collections,contextlib,hashlib,json,math,random,struct,subprocess
from pathlib import Path
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_floor_path_boxes import f32,reference_path

def coordinates(points,x,z):
    a,b,c=points;bx,bz=b[0]-a[0],b[2]-a[2];cx,cz=c[0]-a[0],c[2]-a[2];px,pz=x-a[0],z-a[2]
    det=bx*cz-bz*cx;v=(px*cz-pz*cx)/det;w=(bx*pz-bz*px)/det
    weights=[1-v-w,v,w];return weights,sum(p[1]*weight for p,weight in zip(points,weights))

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('root','configuration','walk_fixtures','audited_paths','private_output','core_dll'):p.add_argument(name,type=Path)
    args=p.parse_args();args.private_output.resolve().relative_to((Path(__file__).resolve().parents[2]/'local-private').resolve());args.private_output.mkdir(parents=True,exist_ok=True)
    fixtures=json.loads(args.walk_fixtures.read_text(encoding='utf-8'));by_member={f['member']:f for f in fixtures};paths={}
    for line in args.audited_paths.read_text(encoding='utf-8').splitlines():
        if line.startswith('floor_path='):
            path=json.loads(line[11:]);paths[(path['member'],path['index'])]=path
    selected=[];seen=set()
    for key,path in sorted(paths.items(),key=lambda item:('yt1300' not in item[0][0] and 'yacht' not in item[0][0],item[0])):
        if key[0] in seen or path['status']!='Accepted' or len(path['segments'])<2:continue
        if not any(abs(s['end'][1]-s['start'][1])>1e-4 for s in path['segments']):continue
        selected.append((key,path));seen.add(key[0])
        if len(selected)==600:break
    assert len(selected)>=100;native_fixtures=[];expected={};rng=random.Random(2026093016);counts=collections.Counter()
    with contextlib.ExitStack() as stack:
        indices=[Index(stack.enter_context(path.open('rb'))) for _,path in archives(args.root,args.configuration)]
        for number,(key,path) in enumerate(selected):
            original=by_member[key[0]];query=dict(original['queries'][key[1]]);data=None
            for index in indices:
                data=index.lookup(key[0])
                if data is not None:break
            assert hashlib.sha256(data).hexdigest().upper()==original['sha256']
            root,=nodes(data);version,=root[1];positions,records=version[1][:2];positions,records=positions[1],records[1]
            if version[0]==b'0006':positions,records=positions[4:],records[4:]
            vertices=list(struct.iter_unpack('<3f',positions));triangles=[struct.unpack_from('<3iI3i3f4Bi3i',records,i) for i in range(0,len(records),60)]
            times=[path['segments'][0]['start_parameter']]+[s['end_parameter'] for s in path['segments']];precise=[]
            for j,t in enumerate(times):
                segment=path['segments'][min(j,len(path['segments'])-1)];points=[vertices[i] for i in triangles[segment['triangle']][:3]]
                x=query['start'][0]+(query['end'][0]-query['start'][0])*t;z=query['start'][2]+(query['end'][2]-query['start'][2])*t
                _,y=coordinates(points,x,z);precise.append([x,y,z])
                authored=segment['start'] if j<len(path['segments']) else segment['end']
                assert max(abs(a-b) for a,b in zip(precise[-1],authored))<=1e-7,(key,'audit plane mismatch')
            j=number%len(path['segments']);t=rng.uniform(.2,.8);feet=[precise[j][i]*(1-t)+precise[j+1][i]*t for i in range(3)]
            yaw=rng.uniform(-math.pi,math.pi);c,s=math.cos(yaw),math.sin(yaw);half=[.05,.2,.05]
            box=dict(minimum=[-.1,-.1,-.1],maximum=[.1,.1,.1],half=half,
                pose=[[c,0,s],[0,1,0],[-s,0,c],[feet[0],feet[1]+half[1]+(10 if number%5==0 else 0),feet[2]]],allowed=False,separating=True)
            for field in ('minimum','maximum','half'):box[field]=[f32(v) for v in box[field]]
            box['pose']=[[f32(v) for v in row] for row in box['pose']];query['box']=box
            hit=reference_path(box,times,precise)
            if hit['status']=='Clear':geometric=precise[-1];wanted='Accepted';parameter=1
            else:geometric=[hit['center'][i]-(box['half'][1] if i==1 else 0) for i in range(3)];wanted='BoxStopped';parameter=hit['parameter']
            x,z=f32(geometric[0]),f32(geometric[2]);landing=None
            for segment in path['segments']:
                points=[vertices[i] for i in triangles[segment['triangle']][:3]];weights,y=coordinates(points,x,z)
                if min(weights)>=-1e-8:landing=(segment['triangle'],[x,f32(y),z]);break
            correction_required=False
            if wanted=='BoxStopped':
                displacement=None if landing is None else [landing[1][i]-geometric[i] for i in range(3)]
                if displacement is None or sum(v*v for v in displacement)>1e-8 or sum(displacement[i]*hit['normal'][i] for i in range(3))< -1e-6:
                    correction_required=True;wanted='ContactLandingUnsupported';parameter=0;landing=(query['triangle'],query['start'])
            assert landing is not None,(key,'missing expected landing')
            expected[key[0]]=dict(status=wanted,parameter=parameter,triangle=landing[0],position=landing[1],contact=hit,
                correction_required=correction_required,half_height=box['half'][1],planes={segment['triangle']:[vertices[i] for i in triangles[segment['triangle']][:3]] for segment in path['segments']})
            native_fixtures.append(dict(member=key[0],sha256=original['sha256'],queries=[query]))
    fixture=args.private_output/'fixtures.json';fixture.write_text(json.dumps(native_fixtures,separators=(',',':')),encoding='utf-8');digest=hashlib.sha256(fixture.read_bytes()).hexdigest().upper()
    run=subprocess.run(['dotnet',str(args.core_dll),'--floor-box-corpus',str(args.root),'--configuration',str(args.configuration),'--fixtures',str(fixture),'--expected-sha256',digest],capture_output=True,text=True,check=True,timeout=120)
    (args.private_output/'native-corpus.txt').write_text(run.stdout,encoding='utf-8');observed={}
    for line in run.stdout.splitlines():
        if line.startswith('floor_box_corpus='):
            row=json.loads(line[17:]);assert row['member'] not in observed;observed[row['member']]=row
    assert observed.keys()==expected.keys()
    for key,wanted in expected.items():
        row=observed[key];row['position']=[f32(v) for v in row['position']];assert row['resume_status']=='Accepted',(key,'unusable locator',row)
        if wanted['correction_required'] and row['status']=='BoxStopped':
            hit=wanted['contact'];feet=[hit['center'][i]-(wanted['half_height'] if i==1 else 0) for i in range(3)]
            displacement=[row['position'][i]-feet[i] for i in range(3)]
            assert sum(v*v for v in displacement)<=1e-8 and sum(displacement[i]*hit['normal'][i] for i in range(3))>=-1e-6,(key,'correction violates contact budget',row)
            weights,y=coordinates(wanted['planes'][row['triangle']],row['position'][0],row['position'][2])
            assert min(weights)>=-1e-8 and abs(row['position'][1]-f32(y))<=1e-5 and abs(row['parameter']-hit['parameter'])<=1e-7,(key,'corrected landing geometry',row)
            counts['corrected_contacts']+=1
        else:
            assert row['status']==wanted['status'] and row['triangle']==wanted['triangle'],(key,wanted,row)
            assert abs(row['parameter']-wanted['parameter'])<=1e-7 and max(abs(a-b) for a,b in zip(row['position'],wanted['position']))<=1e-4,(key,wanted,row)
        hit=wanted['contact']
        if hit['status']=='Clear':assert row['contact'] is None
        else:
            actual=row['contact'];assert actual['status']==hit['status'] and actual['segment']==hit['segment'] and abs(actual['parameter']-hit['parameter'])<=1e-7,(key,hit,actual)
            assert max(abs(a-b) for a,b in zip(actual['center'],hit['center']))<=1e-7,(key,hit,actual)
        counts[row['status']]+=1
        assert row['repeat_reusable'] and row['repeat_distance']<=1e-4,(key,'unstable repeated contact',row)
        counts['retreat_'+row['retreat_status']]+=1
        if row['retreat_status']=='Accepted':assert row['retreat_source_distance']<=1e-4,(key,'retreat missed source',row)
    summary=dict(original_floors=len(selected),synthetic_boxes=True,seed=2026093016,fixtures_sha256=digest,
        original_walk_fixtures_sha256=hashlib.sha256(args.walk_fixtures.read_bytes()).hexdigest().upper(),audited_path_output_sha256=hashlib.sha256(args.audited_paths.read_bytes()).hexdigest().upper(),
        outcomes={k:v for k,v in counts.items() if not k.startswith('retreat_') and k!='corrected_contacts'},corrected_contacts=counts['corrected_contacts'],
        retreat_outcomes={k[8:]:v for k,v in counts.items() if k.startswith('retreat_')},reusable_locators=len(observed),repeat_queries=len(observed)*20)
    (args.private_output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print('PASS original sloped planes / synthetic box contacts and landing: '+json.dumps(summary,sort_keys=True))

if __name__=='__main__':main()
