"""Host/core filter-only pipeline against the accepted independent field oracle."""
import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'poc/round54-tests/Integration'))
import per_cell_height_peer as old
def generate(assetroot,config,exe):
 d=old.generate(assetroot,config,exe);d.update(Version=7,HeightPolicy='owned_filter_to_field_synthetic_composition');return d
def verify(d,text):
 result=old.verify_host(d,text)
 rows=old.old.h.records(text,'owned_filter_pipeline_sampling=');assert len(rows)==4
 for i,row in enumerate(rows):
  assert row['Chunk']==i;c=row['SamplingCounts']
  assert c['FilterSamplerConstructions']==c['FilterSamplerDisposals']==c['FilterQueryBatches']==1
  assert c['FilterComputedCells']==c['FilterQueries']==c['Products']==100
  assert c['Height']['ComputedCells']==100 and c['Height']['SamplerConstructions']==c['Height']['SamplerDisposals']==1
 result.update(OwnedFilterOnlyPipeline=True,FilterSamplerConstructions=4,HeightSamplerConstructions=4)
 return result
if __name__=='__main__':
 action,assetroot,config,exe,fixture=sys.argv[1:6]
 assert Path.cwd().resolve()==ROOT;d=generate(assetroot,config,exe)
 guard=old.old.runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
 if action=='generate':
  guard(Path(fixture),ROOT);Path(fixture).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8');print('Generated400 owned filter-only pipeline expectations')
 elif action=='verify':
  assert json.loads(Path(fixture).read_text(encoding='utf-8'))==d
  guard(Path(sys.argv[7]),ROOT);result=verify(d,Path(sys.argv[6]).read_text(encoding='utf-8'));Path(sys.argv[7]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
 else:raise ValueError('Unknown action')
