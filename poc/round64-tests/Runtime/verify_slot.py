"""Focused independent three-replacement traces from sealed frozen62 recipes, without native seeds."""
import argparse, hashlib, json, math, runpy, struct
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
RUNTIME = 'c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN = '85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'

def bits(x): return struct.unpack('<I', struct.pack('<f', x))[0]
def value(x): return struct.unpack('<f', struct.pack('<I', x))[0]

def main():
    p = argparse.ArgumentParser(); p.add_argument('mode', choices=['generate', 'verify'])
    for k in ['runtime', 'terrain', 'out']: p.add_argument('--'+k, type=Path, required=True)
    p.add_argument('--native', type=Path); a = p.parse_args()
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest() == RUNTIME
    assert hashlib.sha256(a.terrain.read_bytes()).hexdigest() == TERRAIN
    guard = runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    out = guard(a.out, ROOT); runtime = json.loads(a.runtime.read_text()); terrain = json.loads(a.terrain.read_text())
    original = terrain['Chunks'][:4]; changed = json.loads(json.dumps(original)); outside = json.loads(json.dumps(original))
    for chunk in changed: chunk['Heights'] = [bits(value(h)+2) for h in chunk['Heights']]
    for chunk in outside: chunk['Request']['First'] += 100
    selected = [next(c for c in runtime['Cases'] if c.get('SixtyStep') and c['Profile'] == 0 and c['Precision'] == precision) for precision in [24,53,64]]
    cases = [dict(Precision=c['Precision'], Wire=c['Wire'], Segments=[dict(Initial=0, Steps=c['Steps'][start:start+4]) for start in [0,20,40]]) for c in selected]
    fixture = dict(Baseline=runtime['Baseline'], Profiles=runtime['Profiles'], Cases=cases, Terrain=original, ChangedTerrain=changed, OutsideTerrain=outside)
    if a.mode == 'generate':
        out.write_text(json.dumps(fixture,separators=(',',':'))+'\n',encoding='utf-8'); print('PINNED_SLOT_GENERATED'); return
    assert a.native
    reference = runpy.run_path(str(ROOT/'poc/round62-tests/Runtime/verify_playback.py')); composition = reference['composition']
    profiles = composition['pairmodel']['expected_profiles'](runtime)
    surface = runpy.run_path(str(ROOT/'poc/round62-tests/Rendering/verify_surface.py')); q,f,n,m = surface['load']()
    sets = []
    for recipe in [original,changed]:
        chunks = [q.enrich(c,f.expected(c,n,m),f,n,m) for c in recipe]
        for chunk in chunks:
            for patch in chunk['Patches']: patch['NormalBits'] = [[bits(float(v)) for v in row] for row in patch['Normals']]
        sets.append(dict(Chunks=chunks))
    native = json.loads(a.native.read_text()); assert len(native['Traces']) == 3; count = 0
    for case,got in zip(cases,native['Traces']):
        assert got['Precision'] == case['Precision'] and got['FinalRevision'] == 5 and len(got['Segments']) == 3
        z = 7.5; clock = 0; xrow = [bits(1),0,0]; zrow = [0,0,bits(1)]
        for index,(segment,actual) in enumerate(zip(case['Segments'],got['Segments'])):
            c = selected[[24,53,64].index(case['Precision'])].copy(); c.update(segment)
            plan = composition['evaluate'](c, profiles[0], runtime['Profiles'][0]['RootSha256'])['Plan']
            rows = reference['expected_rows'](plan)
            assert actual['Index'] == index and actual['Revision'] == index+1 and len(actual['Rows']) == 4
            for step,(row,expected) in enumerate(zip(actual['Rows'],rows)):
                if index == 2 and step == 1: z = value(bits(z+.125))
                speed = value(expected['StoredBits']); z = value(bits(z + value(bits(speed*value(bits(.01))))))
                hit = surface['hit'](sets[index==1], [surface['b64'](1.25),surface['b64'](z)],surface['child_options']({})); assert hit
                if value(expected['SpeedBits']) > 0:
                    xrow = [bits(speed/value(expected['SpeedBits'])),0,0]; zrow = [bits(-0.0),0,xrow[0]]
                clock += 10000
                want = dict(Segment=index,Step=step,StoredBits=expected['StoredBits'],Time=clock,Speed=expected['SpeedBits'],Position=[bits(1.25),bits(hit['Height']+.25),bits(z)],X=xrow,Y=[0,bits(1),0],Z=zrow)
                assert row == want,(case['Precision'],index,step,row,want); count += 1
    assert native['NegativeChecks'] >= 70
    report = dict(Passed=True,Traces=3,ReplacementsPerTrace=5,ComparedSteps=count,IndependentPlanPoseSpeedTimeHeight=True,ChangedSurfaceHeightOffset=2,CorrectionZOffset=.125,NativeChecks=native['NativeChecks'],NegativeChecks=native['NegativeChecks'],RuntimeSha256=RUNTIME,TerrainSha256=TERRAIN,OutputSeedUsed=False,OriginalExecuted=False)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('SLOT_REFERENCE_PASS',json.dumps(report))

if __name__ == '__main__': main()
