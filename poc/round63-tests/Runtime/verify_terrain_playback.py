"""Pinned frozen62 inputs; independent rational plan, binary32 motion and triangle-height oracle."""
import argparse, hashlib, json, math, runpy, struct
from pathlib import Path
from fractions import Fraction
ROOT = Path(__file__).resolve().parents[3]
RUNTIME = 'c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21'
TERRAIN = '85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd'

def bits(x):
    return struct.unpack('<I', struct.pack('<f', x))[0]

def value(x):
    return struct.unpack('<f', struct.pack('<I', x))[0]

def main():
    p = argparse.ArgumentParser()
    p.add_argument('mode', choices=['generate', 'verify'])
    for key in ['runtime', 'terrain', 'out']:
        p.add_argument('--' + key, type=Path, required=True)
    p.add_argument('--native', type=Path)
    a = p.parse_args()
    # Pin both borrowed sealed byte streams before imports or any output.
    assert hashlib.sha256(a.runtime.read_bytes()).hexdigest() == RUNTIME
    assert hashlib.sha256(a.terrain.read_bytes()).hexdigest() == TERRAIN
    guard = runpy.run_path(str(ROOT / 'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    out = guard(a.out, ROOT)
    runtime = json.loads(a.runtime.read_text())
    terrain = json.loads(a.terrain.read_text())
    cases = [c.copy() for c in runtime['Cases'] if c.get('SixtyStep') or c.get('NegativeZ')]
    for i, c in enumerate(cases):
        c['StartZ'] = 3.5 if i % 2 == 0 else 7.5
        c['Correction'] = i % 3 == 0
    fixture = dict(Baseline=runtime['Baseline'], Profiles=runtime['Profiles'], Cases=cases,
                   Terrain=terrain['Chunks'][:4], RuntimeSha256=RUNTIME, TerrainSha256=TERRAIN)
    if a.mode == 'generate':
        out.write_text(json.dumps(fixture, separators=(',', ':')) + '\n', encoding='utf-8')
        print('PINNED_GENERATED', len(cases))
        return
    assert a.native
    reference = runpy.run_path(str(ROOT / 'poc/round62-tests/Runtime/verify_playback.py'))
    composition = reference['composition']
    profiles = composition['pairmodel']['expected_profiles'](runtime)
    surface = runpy.run_path(str(ROOT / 'poc/round62-tests/Rendering/verify_surface.py'))
    q, f, n, m = surface['load']()
    chunks = [q.enrich(c, f.expected(c, n, m), f, n, m) for c in terrain['Chunks'][:4]]
    for chunk in chunks:
        for patch in chunk['Patches']:
            patch['NormalBits'] = [[bits(float(x)) for x in row] for row in patch['Normals']]
    actual = json.loads(a.native.read_text())
    assert len(actual['Cases']) == len(cases)
    comparisons = 0
    visited = set()
    for c, got in zip(cases, actual['Cases']):
        plan = composition['evaluate'](c, profiles[c['Profile']], runtime['Profiles'][c['Profile']]['RootSha256'])['Plan']
        assert got['Name'] == c['Name'] and len(got['Rows']) == len(plan['Rows'])
        z = value(bits(c['StartZ']))
        rows = reference['expected_rows'](plan)
        facing = [bits(0), bits(0), bits(1)]
        xrow = [bits(1), bits(0), bits(0)]
        for i, (row, want) in enumerate(zip(got['Rows'], rows)):
            if i == 20 and c['Correction']:
                z = value(bits(z + .25))
            speed = value(want['StoredBits'])
            displacement = value(bits(speed * value(bits(.01))))
            z = value(bits(z + displacement))
            h = surface['hit'](dict(Chunks=chunks), [surface['b64'](1.25), surface['b64'](z)], surface['child_options']({}))
            assert h is not None
            if value(want['SpeedBits']) > 0:
                cosine, sine = bits(speed / value(want['SpeedBits'])), 0
                xrow, facing = [cosine, 0, sine], [bits(-value(sine)), 0, cosine]
            expected = dict(Index=i, StoredBits=want['StoredBits'], Time=(i+1)*10000,
                            Speed=want['SpeedBits'], Position=[bits(1.25), bits(h['Height']+.25), bits(z)],
                            X=xrow, Y=[0,bits(1),0], Z=facing, Chunk=h['Chunk'], Patch=h['Patch'], Triangle=h['TriangleIndex'])
            # Positive velocity MathF.Sin(0) preserves negative zero in RowZ.X.
            for key, val in expected.items():
                assert row[key] == val, (c['Name'], i, key, row[key], val)
            assert math.isclose(row['Height'], h['Height'], rel_tol=2e-11, abs_tol=1e-9)
            visited.add((row['Chunk'], row['Patch']))
            comparisons += 1
    assert len(visited) >= 3 and len({chunk for chunk, patch in visited}) >= 2 and actual['NegativeChecks'] >= 600
    report = dict(Passed=True, Cases=len(cases), SixtyStepTraces=60, ComparedSteps=comparisons,
                  VisitedChunkPatchPairs=sorted(visited), NativeChecks=actual['NativeChecks'], NegativeChecks=actual['NegativeChecks'],
                  RuntimeSha256=RUNTIME, TerrainSha256=TERRAIN, IndependentPlanSpeedPoseTimeHeight=True,
                  OutputSeedUsed=False, OriginalExecuted=False, OriginalMovementParity=False)
    out.write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print('TERRAIN_PLAYBACK_REFERENCE_PASS', json.dumps(report))

if __name__ == '__main__':
    main()
