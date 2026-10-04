"""Join accepted independent patch arithmetic and supplied-surface reference models."""
import argparse
import json
import math
import runpy
import struct
import sys
from pathlib import Path

r=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--exe',required=True);p.add_argument('--packages',required=True)
p.add_argument('--root',type=Path,required=True);p.add_argument('--native',type=Path)
a=p.parse_args();a.root.mkdir(parents=True,exist_ok=True)
if 'local-private' not in a.root.parts:p.error('Private output root required')
sys.argv=['verify_patch.py','--exe',a.exe,'--packages',a.packages,'--fixtures',str(a.root/'arithmetic-fixtures.json'),'--receipt',str(a.root/'arithmetic-receipt.json')]
patch=runpy.run_path(str(r/'poc/round37-tests/Rendering/verify_patch.py'))
surface=runpy.run_path(str(r/'poc/round37-tests/Runtime/peer.py'))
selected=[];profiles=set();seams=set();expected=[]
f32=lambda bits:struct.unpack('<f',struct.pack('<I',bits))[0]
for f in patch['fs']:
    e=patch['expected'](f)
    if e['Reject']!='None':continue
    triple=tuple(f[k] for k in ('CachePrecision','QueryPrecision','HeightPrecision'))
    seam=(f['Cells'],f['Mask'])
    if triple in profiles and (f['Cells'] not in (1,3) or seam in seams):continue
    profiles.add(triple);seams.add(seam)
    locations=[];cells=[]
    for cell in e['Patches']:
        pos=[[f32(b) for b in v] for v in cell['Positions']]
        points=[[v[0],v[2]] for v in pos]
        for start in range(0,len(cell['Indices']),3):
            ids=cell['Indices'][start:start+3]
            points.append([sum(pos[i][axis] for i in ids)/3 for axis in (0,2)])
        points.append([max(v[0] for v in pos)+0.25,max(v[2] for v in pos)+0.25])
        reference={'Positions':pos,'Normals':[[0,1,0]]*9,'Pattern':cell['Pattern']}
        cells.append({'Pattern':cell['Pattern'],'Hits':[surface['reference'](reference,q) for q in points]})
        locations.append(points)
    selected.append({**f,'Locations':locations});expected.append({'Name':f['Name'],'Patches':cells})
(a.root/'input.json').write_text(json.dumps({'Cases':selected}),encoding='utf-8')
if a.native:
    n=json.loads(a.native.read_text());assert len(n['Cases'])==len(expected)
    patches=queries=0
    for want,got in zip(expected,n['Cases']):
        assert want['Name']==got['Name'] and len(want['Patches'])==len(got['Patches'])
        for wc,gc in zip(want['Patches'],got['Patches']):
            patches+=1;assert wc['Pattern']==gc['Pattern'] and len(wc['Hits'])==len(gc['Hits'])
            for wh,gh in zip(wc['Hits'],gc['Hits']):
                queries+=1
                if wh is None:assert gh is None;continue
                assert gh is not None and gh['TriangleIndex']==wh['TriangleIndex']
                assert [gh[k] for k in ('VertexA','VertexB','VertexC')]==wh['Vertices']
                for k,v in zip(('WeightA','WeightB','WeightC'),wh['Weights']):assert math.isclose(gh[k],v,rel_tol=2e-13,abs_tol=2e-13)
                assert math.isclose(gh['Height'],wh['Height'],rel_tol=2e-13,abs_tol=2e-13)
                assert all(math.isclose(gh['Normal'][k],v,rel_tol=2e-13,abs_tol=2e-13) for k,v in zip(('X','Y','Z'),wh['Normal']))
    assert n['PatchCount']==patches and n['QueryCount']==n['ReflectionCount']==queries
    result={'Cases':len(selected),'PrecisionTriples':len(profiles),'SeamPairs':len(seams),'Patches':patches,'Queries':queries,'ReflectionRoundTrips':queries,'Passed':True,'OriginalTerrainParity':False}
    (a.root/'join-receipt.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
else:print(json.dumps({'Cases':len(selected),'PrecisionTriples':len(profiles),'SeamPairs':len(seams)}))
