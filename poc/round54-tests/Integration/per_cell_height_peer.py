"""Independent original FFRA/family consumer joined to explicit synthetic field composition."""
import json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'poc/round53-tests/Integration'))
import original_rule_height_host_peer as old
r=old.r;j=old.j

def filter_selection(generator,entries):
 inspected=0
 meta=r.n.family.load('filter_metadata54','poc/round33-tests/Assets/verify_tgen_input.py')
 for index,row in enumerate(meta.metadata(generator)['Rules']):
  s=row['Section']
  if s['FormType']!='FFRA' or s['Version']!='0005':continue
  inspected+=1;assert inspected<=16
  blob=generator[s['Offset']:s['Offset']+s['EncodedBytes']]
  v,=j.ff.m.records(blob,12,len(blob));hdr,data=list(j.ff.m.records(blob,v[2]+12,v[3]))
  hv,=j.ff.m.records(blob,hdr[2]+12,hdr[3]);hd,=j.ff.m.records(blob,hv[2]+12,hv[3])
  enabled=struct.unpack_from('<I',hd[1])[0];parm,=j.ff.m.records(blob,data[2]+12,data[3])
  family,selector,amount,lo,hi,scale=struct.unpack('<6I',parm[1]);fid=family-(1<<32) if family>=1<<31 else family
  entry=next((e for e in entries if e['Id']==fid),None)
  if not enabled or entry is None or not all(j.a.finite(x) for x in [lo,hi,amount,scale]):continue
  words=entry['Words']
  if words[0]==0x7fffffff or words[1] or words[3] or words[12] not in range(6) or not 1<=words[5]<=16:continue
  if not all(j.a.finite(words[k]) for k in [6,7,8,9,10,11]):continue
  snap=j.ff.snapshot(blob,lo=lo,hi=hi,selector=selector,amount=amount,scale=scale,family=family,enabled=enabled,name=hd[1][4:-1])
  return blob,dict(RuleIndex=index,Offset=s['Offset'],EncodedBytes=len(blob),LayerIndex=row['LayerIndex'],ChildOrdinal=row['ChildOrdinal'],Sha256=r.sha(blob),FamilyId=fid,Snapshot=snap),words
 raise ValueError('No operational FFRA/0005 occurrence')

def influence(sample,snap,incoming):
 scaled=j.a.bits(j.a.precision(j.a.mul(j.raw(sample),j.a.scalar(snap['Scale'])),64))
 q=dict(Version='0002',Lower=snap['Lower'],Upper=snap['Upper'],Amount=snap['Amount'],Height=scaled,Precision=64,Limits=j.ff.fh.QUERY,Action='query',Selector=snap['Selector'],Enabled=1)
 retained=j.ff.old.query_number(q)
 if isinstance(retained,dict):raise ValueError('Query rejected')
 shaped=j.l.shape(retained,snap['Selector'],64)
 local=j.a.scalar(0x3f800000)
 if shaped[0]<local[0]:local=j.a.scalar(j.a.bits(shaped))
 return j.a.bits(j.a.precision(j.a.mul(local,j.a.scalar(incoming)),64))

def generate(assetroot,config,exe):
 d=old.generate(assetroot,config,exe)
 generator=bytes.fromhex(d['GeneratorHex']);_,entries=r.original_group(assetroot,config)
 blob,selected,words=filter_selection(generator,entries)
 heightwords=next(e['Words'] for e in entries if e['Id']==d['SelectedRule']['FamilyId'])
 snap=d['SelectedRule']['Snapshot'];positive=0
 for c,e in zip(d['Chunks'],d['Expected']):
  prior=e['HeightBits'][:];weights=[];samples=[];heights=[]
  for k,(z,x) in enumerate((z,x) for z in e['PositionSecond'] for x in e['PositionFirst']):
   _,sample=r.mode_sample(words,53,64,64,x,z);samples.append(sample)
   weight=influence(sample,selected['Snapshot'],c['InfluenceBits']);weights.append(weight)
   if j.a.scalar(weight)[0]>0:
    _,height_sample=r.mode_sample(heightwords,53,64,64,x,z)
    heights.append(j.height({'Precision':64},j.raw(height_sample),prior[k],weight,snap['Operation'],snap['Scale']));positive+=1
   else:heights.append(prior[k])
  c['PriorHeightBits']=prior;c['CellInfluenceBits']=weights;c['FilterSamples']=samples
  e['HeightBits']=heights
  for patch in e['Patches']:
   for k,source in enumerate(patch['SourceIndices']):patch['Positions'][k][1]=heights[source]
 poses={}
 for name,q in zip(old.h.NAMES,old.h.XZ):
  hit=next((hit for e in d['Expected'] for patch in e['Patches'] if (hit:=old.h.surface(patch,q)) is not None),None)
  assert hit is not None;poses[name]=0 if name=='corrected_paused' else old.h.p.word(hit['Height'])
 d['MotionExpectedBits']=poses
 d.update(Version=6,HeightPolicy='selected_filter_per_cell_synthetic_composition',FilterRuleHex=blob.hex(),SelectedFilterRule=selected,PositiveCells=positive,OriginalTerrainParity=False)
 return d

def verify_native(d,actual):
 assert actual['OriginalTerrainParity'] is False and actual['OriginalRuleUsed'] is True
 assert actual['FilterRuleIndex']==d['SelectedFilterRule']['RuleIndex'] and actual['FilterRuleSha256']==d['SelectedFilterRule']['Sha256']
 assert len(actual['Chunks'])==4
 for i,(got,c,e) in enumerate(zip(actual['Chunks'],d['Chunks'],d['Expected'])):
  assert got['Chunk']==i and got['PriorHeightBits']==c['PriorHeightBits'] and got['InfluenceBits']==c['CellInfluenceBits']
  assert got['HeightBits']==e['HeightBits'] and got['FilterSamples']==c['FilterSamples']
  positive=sum(j.a.scalar(w)[0]>0 for w in c['CellInfluenceBits'])
  assert got['PositiveCells']==positive and got['SkippedCells']==100-positive
  for k,w in enumerate(got['MixedInfluenceBits']):
   expected=e['HeightBits'][k] if j.a.scalar(w)[0]>0 else c['PriorHeightBits'][k]
   assert got['MixedHeightBits'][k]==expected
  assert got['MixedCounts']['ComputedCells']==sum(j.a.scalar(w)[0]>0 for w in got['MixedInfluenceBits'])
  assert got['ZeroCounts']['ComputedCells']==got['ZeroCounts']['SamplerConstructions']==got['ZeroCounts']['SamplerDisposals']==0
 return dict(Passed=True,Cells=400,PositiveCells=d['PositiveCells'],SkippedCells=400-d['PositiveCells'],OriginalFilterRuleUsed=True,OriginalTerrainParity=False)

def verify_host(d,text):
 import math
 h=old.h
 rows=h.records(text,'field_height_chunk=');assert len(rows)==4
 sampling=h.records(text,'field_height_sampling=');assert len(sampling)==4
 for i,(got,c,e,s) in enumerate(zip(rows,d['Chunks'],d['Expected'],sampling)):
  assert got['Chunk']==s['Chunk']==i and got['HeightBits']==e['HeightBits']
  assert got['XBits']==e['PositionFirst'] and got['ZBits']==e['PositionSecond']
  assert got['PriorHeightBits']==c['PriorHeightBits'] and got['InfluenceBits']==c['CellInfluenceBits']
  assert got['PositiveCells']==100 and got['SkippedCells']==0
  assert s['FilterSamples']==c['FilterSamples']
  counts=s['SamplingCounts'];assert counts['ComputedCells']==counts['HeightApplications']==100
  assert counts['SamplerConstructions']==counts['SamplerDisposals']==counts['SamplerQueryBatches']==1
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
 assert h.records(text,'field_height_ready=')==[dict(SelectedHeightRule=d['SelectedRule']['Sha256'],SelectedFilterRule=d['SelectedFilterRule']['Sha256'],FilterRuleIndex=8,OriginalRuleUsed=True,SyntheticComposition=True,OriginalTerrainParity=False,Cells=400)]
 assert h.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
 assert 'PASS: pumped local_input=true' in text and 'PASS: pumped creature_view4_partial=true' in text
 return dict(Passed=True,Cells=400,SelectedOriginalRules=2,Patches=16,Triangles=128,GroundedStates=9,OriginalTerrainParity=False)

if __name__=='__main__':
 action,assetroot,config,exe,fixture=sys.argv[1:6]
 guard=old.runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 assert Path.cwd().resolve()==ROOT
 d=generate(assetroot,config,exe)
 if action=='generate':
  guard(Path(fixture),ROOT);Path(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print(json.dumps(dict(PositiveCells=d['PositiveCells'],FilterIndex=d['SelectedFilterRule']['RuleIndex'],FilterSha256=d['SelectedFilterRule']['Sha256'])))
 elif action in ('verify_native','verify'):
  assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
  guard(Path(sys.argv[7]),ROOT);result=verify_native(d,json.loads(Path(sys.argv[6]).read_text(encoding='utf-8'))) if action=='verify_native' else verify_host(d,Path(sys.argv[6]).read_text(encoding='utf-8'));Path(sys.argv[7]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
 else:raise ValueError('Unknown action')
