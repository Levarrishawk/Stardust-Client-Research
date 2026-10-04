"""Independent supplied-slope, raw patch and ownership oracle for original-host joins."""
import json, math, struct, sys
from pathlib import Path
from generate_terrain_peer import generate

def records(text, prefix):
    return [json.loads(line[len(prefix):]) for line in text.splitlines() if line.startswith(prefix)]
def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]
def close(actual, expected):
    assert len(actual) == len(expected)
    assert all(math.isfinite(a) and abs(a-e) < 2e-5 for a,e in zip(actual,expected)), (actual,expected)
def verify(text):
    fixture=generate()
    patches=records(text,'supplied_terrain_patch=')
    assert len(patches)==16
    for i,p in enumerate(patches):
        chunk=i//4; expected=fixture['Expected'][chunk]['Patches'][i%4]
        assert (p['Chunk'],p['Column'],p['Row'])==(chunk,expected['Column'],expected['Row'])
        assert p['Positions']==expected['Positions'] and p['Indices']==expected['Indices'] and p['TriangleCount']==8
    assert records(text,'supplied_terrain_ready=')==[dict(Synthetic=True,Chunks=4,FullSamples=400,Patches=16,QueryTriangles=128,OriginalTerrainParity=False)]
    states=records(text,'local_state=')
    names=['walk','run','paused','corrected_paused','resumed','idle','unfocused','focus_resumed','reset']
    assert [s['Name'] for s in states]==names
    xz=[(-.5,1.100002),(-.5,1.400008),(-.5,1.400008),(-1.5,-1),(-1.5,-.699994),(-1.5,-.699994),(-1.5,-.699994),(-1.5,-.699994),(-1.5,0)]
    speeds=[2,6,6,1,6,0,0,0,0]; clips=['walk','run','run','walk','run','idle','idle','idle','idle']
    times=[50001,100002,350002,850002,900003,950004,1200004,1250005,1300006]
    steps=[3,3,0,0,3,3,0,3,3]; suspended=[0,0,250000,750000,750000,750000,1000000,1000000,0]
    for i,s in enumerate(states):
        x,z=xz[i]; y=0 if names[i]=='corrected_paused' else f32(z/4)
        close(s['Native'],[x,y,z]); close(s['Engine'],[x,y,-z])
        assert s['Speed']==speeds[i] and s['Clip']==clips[i] and s['TimestampMicroseconds']==times[i]
        assert s['StepsThisUpdate']==steps[i] and s['SuspendedMicroseconds']==suspended[i] and s['DroppedMicroseconds']==0
        assert s['calls']==3 and s['closed']==3 and s['Height']==1.4
    assert records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
    assert 'PASS: pumped local_input=true' in text and 'PASS: pumped creature_view6_full=true' in text
    return dict(Patches=16,Triangles=128,LocalStates=9,ExactResources=True,SyntheticTerrain=True,OriginalActors=True,OriginalTerrainParity=False)
if __name__=='__main__':
    print(json.dumps(verify(Path(sys.argv[1]).read_text(encoding='utf-8'))))
