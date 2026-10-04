"""Independent computed-layer host heights, geometry, actor grounding and cleanup."""
import json,math,struct,sys
from pathlib import Path
import height_peer as h
p=h.prior;t=p.t
NAMES=['walk','run','paused','corrected_paused','resumed','idle','unfocused','focus_resumed','reset']
XZ=[(-.5,1.100002),(-.5,1.400008),(-.5,1.400008),(-1.5,-1),(-1.5,-.699994),(-1.5,-.699994),(-1.5,-.699994),(-1.5,-.699994),(-1.5,0)]
def records(text,prefix):return [json.loads(line[len(prefix):]) for line in text.splitlines() if line.startswith(prefix)]
def surface(patch,q):
    return t.reference(dict(Positions=[[p.f32(v) for v in point] for point in patch['Positions']],Normals=[[0,1,0]]*9,Pattern=0),q)
def generate():
    case=h.generate()['Cases'][0];expected=case.pop('Expected');chunks=case.pop('Chunks')
    for chunk,e in zip(chunks,expected):
        chunk['SeamMask']=0
        for patch in e['Patches']:
            patch['Pattern']=0;patch['Indices']=t.PATTERNS[0]
            patch['Hits']=[surface(patch,q) for q in patch['Locations']]
    assert len(NAMES)==len(XZ)==9
    poses={}
    for name,q in zip(NAMES,XZ):
        hit=next((hit for e in expected for patch in e['Patches'] if (hit:=surface(patch,q)) is not None),None);assert hit is not None
        poses[name]=0 if name=='corrected_paused' else p.word(hit['Height'])
    return dict(Version=2,Synthetic=True,HeightPolicy='supplied_layer_ahcn',FootOffsetBits=0,Profiles=[24]*4,Input=case,Chunks=chunks,MotionExpectedBits=poses,Expected=expected)
def verify(fixture,text):
    assert fixture==generate();expected=fixture['Expected'];actual=records(text,'calculated_layer_chunk=');assert len(actual)==4
    for index,(e,g) in enumerate(zip(expected,actual)):
        assert g['Chunk']==index
        for key in ['HeightBits','LocalBits','ProductBits','HeightVisits']:assert g[key]==e[key],(index,key)
    patches=records(text,'supplied_terrain_patch=');assert len(patches)==16
    for index,got in enumerate(patches):
        ep=expected[index//4]['Patches'][index%4]
        assert (got['Chunk'],got['Column'],got['Row'])==(index//4,ep['Column'],ep['Row'])
        assert got['Positions']==ep['Positions'] and got['Indices']==ep['Indices'] and got['TriangleCount']==8
    states=records(text,'local_state=');assert [s['Name'] for s in states]==NAMES
    times=[50001,100002,350002,850002,900003,950004,1200004,1250005,1300006]
    steps=[3,3,0,0,3,3,0,3,3];suspended=[0,0,250000,750000,750000,750000,1000000,1000000,0]
    speeds=[2,6,6,1,6,0,0,0,0];clips=['walk','run','run','walk','run','idle','idle','idle','idle']
    for index,(state,q) in enumerate(zip(states,XZ)):
        x,y,z=state['Native'];assert math.isclose(x,q[0],abs_tol=2e-6) and math.isclose(z,q[1],abs_tol=2e-6)
        hit=next((hit for e in expected for patch in e['Patches'] if (hit:=surface(patch,[x,z])) is not None),None);assert hit is not None
        ey=0 if state['Name']=='corrected_paused' else p.f32(p.word(hit['Height']))
        assert math.isclose(y,ey,abs_tol=2e-6),(state['Name'],y,ey)
        assert all(math.isclose(a,b,abs_tol=2e-6) for a,b in zip(state['Engine'],[x,y,-z]))
        assert state['TimestampMicroseconds']==times[index] and state['StepsThisUpdate']==steps[index]
        assert state['SuspendedMicroseconds']==suspended[index] and state['DroppedMicroseconds']==0
        assert state['Speed']==speeds[index] and state['Clip']==clips[index] and state['calls']==state['closed']==3 and state['Height']==1.4
    assert records(text,'calculated_layer_ready=')==[dict(Rules=3,Samples=400,ActualExecutor=True,SyntheticQueries=True,OriginalTerrainParity=False)]
    assert records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
    assert 'PASS: pumped local_input=true' in text and 'PASS: pumped creature_view6_full=true' in text
    return dict(Passed=True,ActualExecutor=True,Samples=400,OrderedRules=3,Patches=16,Triangles=128,GroundedMotionStates=9,ExactCleanup=True,OriginalActors=True,OriginalTerrainParity=False)
if __name__=='__main__':
    mode,path=sys.argv[1:3];path=Path(path)
    if mode=='generate':path.write_text(json.dumps(generate(),indent=2)+'\n',encoding='utf-8');print('Generated independent computed-layer original-host fixture')
    elif mode=='verify':print(json.dumps(verify(json.loads(path.read_text()),Path(sys.argv[3]).read_text(encoding='utf-8'))))
    else:raise SystemExit('generate fixture | verify fixture log')
