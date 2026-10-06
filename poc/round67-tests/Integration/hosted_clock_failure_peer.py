"""Actual viewport invalid-clock admission retains initial driver prefix and drains owners."""
import json,sys,importlib.util
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('hosted67_records',ROOT/'poc/round62-tests/Integration/composition_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
def verify(text):
    failure=m.records(text,'point_input_host_failure=');assert failure==[dict(AtStep=0,Failure='System.ArgumentOutOfRangeException',IsFaulted=True,ClockReadCount=1,AcceptedCount=0,HandledCount=0,PickerAttempts=0,AdapterAttempts=0,TargetPublished=False,ActualViewportEvent=True)]
    for key in ('point_camera_pick=','point_input_target=','point_target_state=','point_target_ready=','point_target_cleared=','point_viewport_input=','point_outbound_root=','point_correction_queued='):
        assert not m.records(text,key)
    assert m.records(text,'point_input_host_cleanup=')==[dict(Closed=True,ClockReadCount=1,AcceptedCount=0,HandledCount=0,IsFaulted=True,OutboundPackets=0,BorrowedAdapterPreserved=True,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert m.records(text,'point_input_cleanup=')==[dict(Disposed=True,Attempts=0,OwnsBorrowers=False,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert m.records(text,'point_target_closed_world_cleanup=')==[dict(Disposed=True,Updates=0,DriverInactive=True,WorldAlreadyClosedByPump=True,BorrowedTerrainPreserved=True)]
    assert m.records(text,'point_camera_cleanup=')==[dict(Disposed=True,Owned=0,BorrowedCameraPreserved=True,BorrowedTerrainPreserved=True)]
    assert m.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
    assert m.records(text,'terrain_supplied_material_cleanup=')==[dict(Allocated=1,Disposed=1,Drained=True,Reads=2)]
    pump=m.records(text,'pump_failure_cleanup=');assert len(pump)==1 and pump[0]['calls']==pump[0]['closed']==3 and pump[0]['drained'] and pump[0]['actors']==0
    assert 'Input cleanup:' not in text and 'Terrain cleanup:' not in text and 'Exit cleanup:' not in text
    assert 'Hosted point input failed: System.ArgumentOutOfRangeException' in text and 'PASS: pumped point_target=' not in text
    return dict(Passed=True,ActualViewportClockRejection=True,PickerAndAdapterAttempts=0,NoTargetPublished=True,
        InitialDriverPublicationPrefixClaimed=True,RollbackClaimed=False,OwnedInputNodeDrained=True,ControllerAndMarkerDrained=True,
        TerrainVisualsDrained=16,TextureDrained=1,ArchiveJobsDrained=3,ActorsDrained=True)
if __name__=='__main__':
    log,out=sys.argv[1:3];result=verify(Path(log).read_text(encoding='utf-8-sig'));p=Path(out)
    assert not p.exists() and p.resolve().is_relative_to((ROOT/'local-private').resolve())
    p.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
