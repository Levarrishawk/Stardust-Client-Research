"""Compare synthetic native sweeps with independent 3-variable LP vertex enumeration."""
import argparse
import hashlib
import itertools
import json
import math
from pathlib import Path
import random
import struct
import subprocess


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def earliest(case):
    # Unknowns t,u,v: point A+u(B-A)+v(C-A) lies in moving box.
    a, b, c = case['triangle']
    start, end, half = (case[k] for k in ('start', 'end', 'half'))
    planes = [((-1,0,0),0), ((1,0,0),1), ((0,-1,0),0),
              ((0,0,-1),0), ((0,1,1),1)]
    for i in range(3):
        row = (start[i]-end[i], b[i]-a[i], c[i]-a[i])
        offset = a[i]-start[i]
        planes.extend([(row, half[i]-offset),
                       (tuple(-x for x in row), half[i]+offset)])
    best = None
    for (r0,h0),(r1,h1),(r2,h2) in itertools.combinations(planes,3):
        cofactors = (cross(r1,r2),cross(r2,r0),cross(r0,r1))
        determinant = dot(r0,cofactors[0])
        if abs(determinant) < 1e-12:
            continue
        point = tuple(sum(cofactors[j][i]*h for j,h in enumerate((h0,h1,h2)))/determinant for i in range(3))
        if all(dot(row,point) <= bound+1e-9 for row,bound in planes):
            t = max(0,min(1,point[0]))
            if best is None or t < best:
                best = t
    return best


def f32(x):
    return struct.unpack('<f',struct.pack('<f',x))[0]


def generate(paths=False):
    rng = random.Random(20260930)
    cases = []
    for index in range(512):
        triangle = [[f32(rng.uniform(-3,3)) for _ in range(3)] for _ in range(3)]
        start = [f32(rng.uniform(-4,4)) for _ in range(3)]
        end = [f32(rng.uniform(-4,4)) for _ in range(3)]
        half = [0.0]*3 if index%4==0 else [f32(rng.uniform(.01,.8)) for _ in range(3)]
        cases.append(dict(triangle=triangle,start=start,end=end,half=half))
        if paths:
            middle = [f32(rng.uniform(-4,4)) for _ in range(3)]
            cases[-1] = dict(triangle=triangle,half=half,parameters=[0,.35,1],
                             points=[[p[0],p[1]-half[1],p[2]] for p in (start,middle,end)])
    return cases


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('native_dll',type=Path)
    parser.add_argument('output',type=Path,help='Ignored private generated-evidence directory')
    parser.add_argument('--paths',action='store_true',help='Compare two-segment precise feet paths')
    args = parser.parse_args()
    output = args.output.resolve()
    if 'local-private' not in output.parts:
        parser.error('Output must be under local-private')
    output.mkdir(parents=True,exist_ok=True)
    cases = generate(args.paths)
    fixture = output/'synthetic-sweeps.json'
    data = (json.dumps(dict(kind='synthetic triangle-box LP comparison',seed=20260930,cases=cases),indent=2)+'\n').encode()
    fixture.write_bytes(data)
    pin = hashlib.sha256(data).hexdigest().upper()
    mode = '--triangle-box-paths' if args.paths else '--triangle-box-sweeps'
    run = subprocess.run(['dotnet',str(args.native_dll.resolve()),mode,str(fixture),'--expected-sha256',pin],capture_output=True,text=True,timeout=60)
    (output/'native.txt').write_text(run.stdout+'\n'+run.stderr,encoding='utf-8')
    if run.returncode:
        raise RuntimeError(f'Native verifier failed: {run.returncode}; see private native.txt')
    rows = [json.loads(line.removeprefix('triangle_box=')) for line in run.stdout.splitlines() if line.startswith('triangle_box=')]
    if len(rows)!=len(cases):
        raise AssertionError(f'Expected {len(cases)} native rows; got {len(rows)}')
    counts = dict(clear=0,contact=0,initial=0)
    maximum_error = 0
    for i,(case,row) in enumerate(zip(cases,rows)):
        expected_segment = -1
        if args.paths:
            expected = None
            for segment in range(2):
                points = case['points']
                center = lambda p: [p[0],p[1]+case['half'][1],p[2]]
                local = earliest(dict(triangle=case['triangle'],half=case['half'],
                                      start=center(points[segment]),end=center(points[segment+1])))
                if local is not None:
                    expected = case['parameters'][segment]+local*(case['parameters'][segment+1]-case['parameters'][segment])
                    expected_segment = segment
                    break
            assert row['segment']==expected_segment, (i,expected_segment,row)
        else:
            expected = earliest(case)
        assert row['index']==i
        if expected is None:
            assert row['status']=='Clear', (i,expected,row)
            counts['clear']+=1
        else:
            assert row['status']!='Clear', (i,expected,row)
            error = abs(expected-row['parameter'])
            assert error<=1e-8, (i,expected,row,error)
            maximum_error = max(maximum_error,error)
            counts['initial' if expected==0 else 'contact']+=1
            if expected>0:
                assert row['status']=='Blocked'
                assert abs(math.sqrt(dot(row['normal'],row['normal']))-1)<1e-6
    result = dict(cases=len(cases),fixture_sha256=pin,counts=counts,maximum_parameter_error=maximum_error)
    (output/'summary.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result))


if __name__=='__main__':
    main()
