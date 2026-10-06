"""Point-target admission failure preserves prior scene history and drains all owners."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('point65_reject_records',ROOT/'poc/round62-tests/Integration/composition_peer.py');old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
def verify(name,text):
    assert name in ('invalid-speed','outside-target')
    for key in ('point_target_ready=','point_target_state=','point_full_correction=','point_target_cleanup='):assert not old.records(text,key)
    assert 'Bounded supplied modern speed and arrival radius' in text if name=='invalid-speed' else 'Supplied point outside terrain' in text
    picks=old.records(text,'point_camera_pick=');assert len(picks)==(1 if name=='invalid-speed' else 0)
    assert old.records(text,'point_camera_cleanup=')==[dict(Disposed=True,Owned=0,BorrowedCameraPreserved=True,BorrowedTerrainPreserved=True)]
    assert old.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
    assert old.records(text,'terrain_supplied_material_cleanup=')==[dict(Allocated=1,Disposed=1,Drained=True,Reads=2)]
    pump=old.records(text,'pump_failure_cleanup=');assert len(pump)==1 and pump[0]['calls']==pump[0]['closed']==3 and pump[0]['drained'] and pump[0]['actors']==0
    return dict(Passed=True,Case=name,NoPointDriverPublished=True,PriorSceneRollbackClaimed=False,PickedBeforeRejection=bool(picks),MarkerDrained=True,TerrainVisualsDrained=16,TexturesDrained=1,ArchivesDrained=True)
if __name__=='__main__':
    name,log,out=sys.argv[1:4];result=verify(name,Path(log).read_text(encoding='utf-8-sig'));p=Path(out)
    assert not p.exists() and p.resolve().is_relative_to((ROOT/'local-private').resolve());p.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
