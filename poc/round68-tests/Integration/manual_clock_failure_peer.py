"""Actual manual sink rejection preserves completed motion history and drains pending FIFO."""
import importlib.util,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('manual68_records',ROOT/'poc/round62-tests/Integration/composition_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
def verify(text):
    assert m.records(text,'manual_input_host_failure=')==[dict(AtStep=25,Failure='System.ArgumentOutOfRangeException',IsFaulted=True,ClockReadCount=1,AcceptedCount=0,HandledCount=0,SinkInvoked=True,SinkReturned=False,StateCommitted=False,ActualViewportEvent=True,TargetPreserved=True,ManualInputZero=True,DriverStatePreserved=True)]
    rows=m.records(text,'point_target_state=');assert len(rows)==25 and [r['Index'] for r in rows]==list(range(25))
    assert rows[-1]['CompletedUpdates']==25 and not rows[-1]['StopStep']
    assert not m.records(text,'manual_input_intent=') and not m.records(text,'manual_gui_consumed=')
    assert len(m.records(text,'point_viewport_input='))==3
    assert [p['AtStep'] for p in m.records(text,'point_outbound_root=')]==[4,6,16]
    assert [p['AtStep'] for p in m.records(text,'manual_outbound_queued=')]==[4,6,16,21]
    assert m.records(text,'manual_input_host_cleanup=')==[dict(Closed=True,ClockReadCount=1,AcceptedCount=0,HandledCount=0,IsFaulted=True,IntentSinkBorrowed=True,ControllerPreserved=True,GuiFreed=True)]
    assert m.records(text,'manual_outbound_queue_cleanup=')==[dict(Disposed=True,PendingBefore=1,QueuedBytesBefore=62,Count=0,QueuedBytes=0,Transferred=3,OwnsBorrowedWorld=False,OwnsBorrowedStore=False,WorldAlreadyClosed=True,Sent=False)]
    assert m.records(text,'point_input_host_cleanup=')==[dict(Closed=True,ClockReadCount=3,AcceptedCount=3,HandledCount=3,IsFaulted=False,OutboundPackets=3,BorrowedAdapterPreserved=True,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert m.records(text,'point_input_cleanup=')==[dict(Disposed=True,Attempts=3,OwnsBorrowers=False,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert m.records(text,'point_target_closed_world_cleanup=')==[dict(Disposed=True,Updates=25,DriverInactive=True,WorldAlreadyClosedByPump=True,BorrowedTerrainPreserved=True)]
    assert m.records(text,'point_camera_cleanup=')==[dict(Disposed=True,Owned=0,BorrowedCameraPreserved=True,BorrowedTerrainPreserved=True)]
    assert m.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
    assert m.records(text,'terrain_supplied_material_cleanup=')==[dict(Allocated=1,Disposed=1,Drained=True,Reads=2)]
    pump=m.records(text,'pump_failure_cleanup=');assert len(pump)==1 and pump[0]['calls']==pump[0]['closed']==3 and pump[0]['drained'] and pump[0]['actors']==0
    for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','PASS: pumped point_target='):assert bad not in text
    assert 'Hosted manual input failed: System.ArgumentOutOfRangeException' in text
    return dict(Passed=True,CompletedUpdates=25,ActualViewportSinkClockRejection=True,TargetAndDriverPreserved=True,ManualIntentNotCommitted=True,HistoricalFramesTransferred=3,PendingFrameDrained=1,InitialAndCompletedMotionPrefixClaimed=True,RollbackClaimed=False,OwnersDrained=True,Sent=False)
if __name__=='__main__':
    log,out=sys.argv[1:3];result=verify(Path(log).read_text(encoding='utf-8-sig'));p=Path(out)
    assert not p.exists() and p.resolve().is_relative_to((ROOT/'local-private').resolve())
    p.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
