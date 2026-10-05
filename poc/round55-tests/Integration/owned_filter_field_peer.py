"""Owned filter-only pipeline versus the accepted independent selected-rule field oracle."""
import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'poc/round54-tests/Integration'))
import per_cell_height_peer as old

def verify(d,actual):
 assert actual['OriginalRuleUsed'] is True and actual['OriginalTerrainParity'] is False
 assert actual['FilterRuleIndex']==d['SelectedFilterRule']['RuleIndex'] and actual['FilterRuleSha256']==d['SelectedFilterRule']['Sha256']
 assert len(actual['Chunks'])==4
 for i,(got,c,e) in enumerate(zip(actual['Chunks'],d['Chunks'],d['Expected'])):
  assert got['Chunk']==i and got['PriorHeightBits']==c['PriorHeightBits'] and got['InfluenceBits']==c['CellInfluenceBits']
  assert got['HeightBits']==e['HeightBits'] and got['FilterSamples']==c['FilterSamples'] and got['PositiveCells']==100 and got['SkippedCells']==0
  counts=got['SamplingCounts'];assert counts['FilterSamplerConstructions']==counts['FilterSamplerDisposals']==counts['FilterQueryBatches']==1
  assert counts['FilterComputedCells']==counts['FilterQueries']==counts['Products']==100
  assert counts['Height']['SamplerConstructions']==counts['Height']['SamplerDisposals']==1
 return dict(Passed=True,Cells=400,RetainedFilterCells=400,OriginalSelectedRulesUsed=True,FilterOnlyModernPolicy=True,OriginalTerrainParity=False)
if __name__=='__main__':
 assetroot,config,exe,fixture,native,receipt=sys.argv[1:7]
 assert Path.cwd().resolve()==ROOT;d=old.generate(assetroot,config,exe)
 assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
 guard=old.old.runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];guard(Path(receipt),ROOT)
 result=verify(d,json.loads(Path(native).read_text(encoding='utf-8')));Path(receipt).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))

