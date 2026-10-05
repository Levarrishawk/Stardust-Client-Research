"""Selected original generator rule/family; synthetic coordinates and controls; independent oracle."""
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
 meta=r.n.family.load('original_rule_meta53','poc/round33-tests/Assets/verify_tgen_input.py')
 rules=meta.metadata(generator)['Rules'];selected=None
 for index,row in enumerate(rules):
  section=row['Section']
  if section['FormType']!='AHFR':continue
  blob=generator[section['Offset']:section['Offset']+section['EncodedBytes']];snap=j.ah.snapshot(blob)
  if not snap['HasDecodedFields'] or not snap['Enabled'] or snap['Operation'] not in range(4):continue
  fid=snap['Family'];fid=fid-(1<<32) if fid>=1<<31 else fid
  family=next((e for e in entries if e['Id']==fid),None)
  if family is None:continue
  words=family['Words']
  if words[1] or words[3] or words[12] not in range(6) or not 1<=words[5]<=16:continue
  selected=dict(RuleIndex=index,Offset=section['Offset'],EncodedBytes=len(blob),LayerIndex=row['LayerIndex'],ChildOrdinal=row['ChildOrdinal'],Sha256=r.sha(blob),FamilyId=fid,Snapshot=snap)
  rule=blob;break
 assert selected is not None and selected['RuleIndex']==0 and selected['Offset']==12346
 initial=j.word(.25);influence=j.word(.001)
 fixture=t.generate();expected=fixture.pop('Expected');chunks=fixture['Chunks']
 for c,e in zip(chunks,expected):
  xs=e['PositionFirst'];zs=e['PositionSecond'];heights=[]
  for z in zs:
   for x in xs:
    _,sample=r.mode_sample(words,53,64,64,x,z)
    heights.append(j.height({'Precision':64},j.raw(sample),initial,influence,snap['Operation'],snap['Scale']))
  c.pop('HeightBits');c.pop('NormalBits');c.pop('Argb');c['InitialHeightBits']=initial;c['InfluenceBits']=influence
  e['HeightBits']=heights
  for patch in e['Patches']:
   for index,k in enumerate(patch['SourceIndices']):patch['Positions'][index][1]=heights[k]
 poses={}
 for name,q in zip(h.NAMES,h.XZ):
  hit=next((hit for e in expected for patch in e['Patches'] if (hit:=h.surface(patch,q)) is not None),None)
  assert hit is not None;poses[name]=0 if name=='corrected_paused' else h.p.word(hit['Height'])
 return dict(Version=5,Synthetic=True,OriginalTerrainParity=False,OriginalRuleUsed=True,SelectedRule=selected,HeightPolicy='original_rule_single_sampler_synthetic_coordinates',
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
 sampling=h.records(text,'original_rule_height_sampling=');assert len(sampling)==4
 for i,row in enumerate(sampling):
  assert row['Chunk']==i and row['Policy']=='SingleFreshSamplerUniqueCells'
  counts=row['SamplingCounts'];assert counts['RuleDecodes']==counts['FamilyLookups']==counts['SamplerConstructions']==counts['SamplerQueryBatches']==counts['SamplerDisposals']==1
  assert counts['ComputedCells']==counts['HeightApplications']==100
 original=h.records(text,'original_rule_height_ready=');assert len(original)==1
 selected=d['SelectedRule'];assert original[0]==dict(RuleIndex=selected['RuleIndex'],RuleOffset=selected['Offset'],RuleBytes=selected['EncodedBytes'],RuleSha256=selected['Sha256'],LayerIndex=selected['LayerIndex'],ChildOrdinal=selected['ChildOrdinal'],FamilyId=selected['FamilyId'],OriginalRuleUsed=True,SyntheticCoordinatesAndControls=True,OriginalTerrainParity=False,Samplers=4,ComputedMisses=400)
 ready=h.records(text,'family_height_ready=');assert len(ready)==1
 assert ready[0]==dict(Samples=400,Chunks=4,Patches=16,FamilyId=d['SelectedRule']['FamilyId'],GeneratorSha256=d['GeneratorSha256'],GroupSha256=d['GroupSha256'],Entries=43,FreshComputedMisses=400,OriginalTerrainParity=False)
 assert h.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
 assert 'PASS: pumped local_input=true' in text and 'PASS: pumped creature_view4_partial=true' in text
 return dict(Passed=True,Samples=400,Patches=16,Triangles=128,GroundedStates=9,FreshComputedMisses=400,Samplers=4,OriginalRuleUsed=True,OriginalTerrainParity=False)

def verify_native(d,actual,assetroot,config):
 _,entries=r.original_group(assetroot,config)
 words=next(e['Words'] for e in entries if e['Id']==d['SelectedRule']['FamilyId'])
 selected=d['SelectedRule']
 assert actual['RuleIndex']==selected['RuleIndex'] and actual['RuleOffset']==selected['Offset'] and actual['RuleSha256']==selected['Sha256'] and actual['FamilyId']==selected['FamilyId']
 assert actual['OriginalRuleUsed'] and not actual['OriginalTerrainParity'] and len(actual['Chunks'])==4
 for i,(row,expected) in enumerate(zip(actual['Chunks'],d['Expected'])):
  assert row['Chunk']==i and row['HeightBits']==expected['HeightBits']
  samples=[r.mode_sample(words,53,64,64,x,z)[1] for z in expected['PositionSecond'] for x in expected['PositionFirst']]
  assert row['Samples']==samples
  assert row['BatchWork']<row['ReferenceWork'] and row['BatchBytes']<row['ReferenceBytes']
  assert row['BatchCounts']['SamplerConstructions']==row['BatchCounts']['SamplerDisposals']==1
  assert row['ReferenceCounts']['SamplerConstructions']==row['ReferenceCounts']['SamplerDisposals']==100
 return dict(Passed=True,RetainedSamples=400,Heights=400,BatchSamplers=4,ReferenceSamplers=400,OriginalRuleUsed=True,OriginalTerrainParity=False,BatchWork=sum(r['BatchWork'] for r in actual['Chunks']),ReferenceWork=sum(r['ReferenceWork'] for r in actual['Chunks']))

if __name__=='__main__':
 action,assetroot,config,exe,fixture=sys.argv[1:6]
 assert Path.cwd().resolve()==ROOT
 for path in [fixture]+(sys.argv[7:8] if action=='verify' else []):assert Path(path).resolve().is_relative_to(ROOT/'local-private')
 guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if action=='generate':guard(Path(fixture),ROOT)
 elif action in ('verify','verify_native'):guard(Path(sys.argv[7]),ROOT)
 d=generate(assetroot,config,exe)
 if action=='generate':
  assert not Path(fixture).exists();Path(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print('Generated400 independent original-rule height samples')
 elif action in ('verify','verify_native'):
  assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
  result=verify(d,Path(sys.argv[6]).read_text(encoding='utf-8')) if action=='verify' else verify_native(d,json.loads(Path(sys.argv[6]).read_text(encoding='utf-8')),assetroot,config);Path(sys.argv[7]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
 else:raise ValueError('generate/verify')
