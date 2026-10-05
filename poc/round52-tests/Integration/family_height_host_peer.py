"""Original generator metadata, synthetic height rule/coordinates, independent host oracle."""
import importlib.util,json,math,runpy,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
g=load('height_patch_generator51','poc/round51-tests/Integration/generator_family_height_peer.py')
h=load('height_patch_host45','poc/round45-tests/Integration/height_filtered_host_peer.py')
t=load('height_patch_geometry39','poc/round39-tests/Integration/generate_terrain_peer.py')
r=g.r;j=r.j

def generate(assetroot,config,exe):
 r.n.gate(Path(exe).read_bytes(),r.n.EXE);r.n.setup()
 generator=g.generator(assetroot,config);group,entries=r.original_group(assetroot,config)
 assert generator[6602:12235]==group
 words=entries[0]['Words'];assert words[1]==words[3]==words[12]==0
 rule=j.ah.encoded(1,0,0x3e800000)
 fixture=t.generate();expected=fixture.pop('Expected');chunks=fixture['Chunks']
 for c,e in zip(chunks,expected):
  xs=e['PositionFirst'];zs=e['PositionSecond'];heights=[]
  for z in zs:
   for x in xs:
    _,sample=r.mode_sample(words,53,64,64,x,z)
    heights.append(j.height({'Precision':64},j.raw(sample),0,0x3f800000,0,0x3e800000))
  c.pop('HeightBits');c.pop('NormalBits');c.pop('Argb');c['InitialHeightBits']=0;c['InfluenceBits']=0x3f800000
  e['HeightBits']=heights
  for patch in e['Patches']:
   for index,k in enumerate(patch['SourceIndices']):patch['Positions'][index][1]=heights[k]
 poses={}
 for name,q in zip(h.NAMES,h.XZ):
  hit=next((hit for e in expected for patch in e['Patches'] if (hit:=h.surface(patch,q)) is not None),None)
  assert hit is not None;poses[name]=0 if name=='corrected_paused' else h.p.word(hit['Height'])
 return dict(Version=4,Synthetic=True,OriginalTerrainParity=False,HeightPolicy='original_family_synthetic_ahfr_fresh_patch',
  FootOffsetBits=0,Profiles=[24]*4,Controls=[53,64,64,64],GeneratorHex=generator.hex(),GeneratorSha256=r.sha(generator),
  GroupSha256=r.sha(group),GroupBytes=len(group),Entries=len(entries),RuleHex=rule.hex(),Chunks=chunks,
  MotionExpectedBits=poses,Expected=expected)

def verify(d,text):
 rows=h.records(text,'family_height_chunk=');assert len(rows)==4
 for index,(row,e) in enumerate(zip(rows,d['Expected'])):
  assert row['Chunk']==index and row['HeightBits']==e['HeightBits']
  assert row['XBits']==e['PositionFirst'] and row['ZBits']==e['PositionSecond']
  assert row['FreshComputedMisses']==100
 patches=h.records(text,'supplied_terrain_patch=');assert len(patches)==16
 for index,row in enumerate(patches):
  p=d['Expected'][index//4]['Patches'][index%4]
  assert (row['Chunk'],row['Column'],row['Row'])==(index//4,p['Column'],p['Row'])
  assert row['Positions']==p['Positions'] and row['Indices']==p['Indices'] and row['TriangleCount']==8
 states=h.records(text,'local_state=');assert [s['Name'] for s in states]==h.NAMES
 for state,q in zip(states,h.XZ):
  x,y,z=state['Native'];assert math.isclose(x,q[0],abs_tol=2e-6) and math.isclose(z,q[1],abs_tol=2e-6)
  hit=next((hit for e in d['Expected'] for p in e['Patches'] if (hit:=h.surface(p,[x,z])) is not None),None)
  assert hit is not None;ey=0 if state['Name']=='corrected_paused' else h.p.f32(h.p.word(hit['Height']))
  assert math.isclose(y,ey,abs_tol=2e-6),(state['Name'],y,ey)
  assert all(math.isclose(a,b,abs_tol=2e-6) for a,b in zip(state['Engine'],[x,y,-z]))
 ready=h.records(text,'family_height_ready=');assert len(ready)==1
 assert ready[0]==dict(Samples=400,Chunks=4,Patches=16,FamilyId=1,GeneratorSha256=d['GeneratorSha256'],GroupSha256=d['GroupSha256'],Entries=43,FreshComputedMisses=400,OriginalTerrainParity=False)
 assert h.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
 assert 'PASS: pumped local_input=true' in text and 'PASS: pumped creature_view4_partial=true' in text
 return dict(Passed=True,Samples=400,Patches=16,Triangles=128,GroundedStates=9,FreshComputedMisses=400,OriginalTerrainParity=False)

if __name__=='__main__':
 action,assetroot,config,exe,fixture=sys.argv[1:6]
 assert Path.cwd().resolve()==ROOT
 for path in [fixture]+(sys.argv[7:8] if action=='verify' else []):assert Path(path).resolve().is_relative_to(ROOT/'local-private')
 guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if action=='generate':guard(Path(fixture),ROOT)
 elif action=='verify':guard(Path(sys.argv[7]),ROOT)
 d=generate(assetroot,config,exe)
 if action=='generate':
  assert not Path(fixture).exists();Path(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print('Generated400 independent family-height samples')
 elif action=='verify':
  assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
  result=verify(d,Path(sys.argv[6]).read_text(encoding='utf-8'));Path(sys.argv[7]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
 else:raise ValueError('generate/verify')
