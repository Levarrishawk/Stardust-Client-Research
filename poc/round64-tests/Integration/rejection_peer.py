"""Joined failure cleanup without claiming rollback of prior scene work."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('rejection64_records',ROOT/'poc/round63-tests/Integration/terrain_playback_peer.py')
old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
def verify(name,text):
    assert name in ('received-nan','late-direction','material-sha')
    for prefix in ('terrain_playback_slot_ready=','current_terrain_playback_ready=','current_plan_playback_ready=','nominal_speed_ready=','terrain_full_transform='):
        assert not old.records(text,prefix),(name,prefix)
    pump=old.records(text,'pump_failure_cleanup=');assert len(pump)==1
    p=pump[0];assert p['drained'] and p['actors']==0 and p['calls']==p['closed']==(0 if name=='material-sha' else 3)
    if name=='material-sha':
        assert 'Pinned shader identity mismatch' in text
        assert not old.records(text,'terrain_supplied_material_cpu=') and not old.records(text,'terrain_supplied_material_gpu=') and not old.records(text,'supplied_terrain_patch=')
        visuals=textures=0
    else:
        assert old.records(text,'terrain_supplied_material_cleanup=')==[dict(Allocated=1,Disposed=1,Drained=True,Reads=2)]
        assert old.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
        light=old.records(text,'sparse_terrain_light_cleanup=');assert len(light)==1 and light[0]['Disposed']
        assert not old.records(text,'current_plan_composition_ready=')
        if name=='received-nan':assert not old.records(text,'paired_current_speed_ready=') and old.records(text,'current_nominal_source_rejected=')==[dict(PublishedReaderResults=0,DriverConstructed=False,MotionPublication=False)]
        else:assert len(old.records(text,'paired_current_speed_ready='))==1 and old.records(text,'nominal_speed_preflight_failure=')==[dict(CompletedSteps=0,DriverConstructed=False,MotionPublication=False)]
        visuals,textures=16,1
    return dict(Passed=True,Case=name,OwnedPlaybackPublished=False,NewPlaybackMotionPublished=False,TerrainVisualsDrained=visuals,TexturesDrained=textures,Actors=0,ArchivesDrained=True,PriorSceneRollbackClaimed=False)
if __name__=='__main__':
    name,log,out=sys.argv[1:4];result=verify(name,Path(log).read_text(encoding='utf-8-sig'))
    old.nominal.fresh(out).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
