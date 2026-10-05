"""One synthetic late invalid height operand: no terrain visuals before native preflight."""
import json,runpy,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def records(text,prefix):return [json.loads(s[len(prefix):]) for s in text.splitlines() if s.startswith(prefix)]
def verify(fixture,log,receipt):
 d=json.loads(Path(fixture).read_text(encoding='utf-8'))
 assert len(d['Chunks'])==4 and all(c['InitialHeightBits']==0 for c in d['Chunks'][:3]) and d['Chunks'][3]['InitialHeightBits']==0x7fc00000
 text=Path(log).read_text(encoding='utf-8-sig')
 assert 'Finite caller fields required.' in text
 rows=records(text,'family_height_chunk=');assert [r['Chunk'] for r in rows]==[0,1,2]
 assert all(r['FreshComputedMisses']==100 for r in rows)
 assert records(text,'family_height_preflight_failure=')==[dict(CompletedChunks=3,Allocated=0,Disposed=0)]
 assert not records(text,'supplied_terrain_patch=') and not records(text,'family_height_ready=') and not records(text,'local_state=')
 cleanup=records(text,'pump_failure_cleanup=');assert len(cleanup)==1
 assert cleanup[0]['calls']==cleanup[0]['closed']==0 and cleanup[0]['drained'] and cleanup[0]['actors']==0 and cleanup[0]['DriverFault'] is None
 result=dict(Passed=True,CompletedChunks=3,NativeSamplesBeforeRejection=300,TerrainVisualAllocations=0,ArchivesDrained=True,Actors=0,OriginalTerrainParity=False)
 p=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](Path(receipt),ROOT)
 p.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
if __name__=='__main__':verify(*sys.argv[1:4])
