"""Valid offline SOE carries malformed application; preserve transport/pump motion prefix."""
import importlib.util,json,sys,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('failure70_records',ROOT/'poc/round62-tests/Integration/composition_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
def verify(profile,text):
    assert profile['Version']==6 and profile['MalformedSoeCorrection']
    assert 'Transform length mismatch.' in text
    rows=m.records(text,'point_target_state=');assert len(rows)==20 and [r['Index'] for r in rows]==list(range(20))
    assert all(r['AttemptedUpdates']==r['CompletedUpdates']==r['Index']+1 and r['FaultType'] is None for r in rows)
    assert [r['Index'] for r in rows if r['Mode']=='Paused']==[5,6,7] and all(r['Speed']==0 for r in rows[5:8])
    assert [r['Index'] for r in rows if r['Mode']=='Idle']==[15,16,17]
    assert len(m.records(text,'point_camera_pick='))==3
    queued=m.records(text,'point_correction_queued=');assert len(queued)==1
    frame=bytes.fromhex(profile['SoeCorrectionHex'])[:-1]
    assert queued[0]==dict(AtStep=20,Hex=frame.hex().upper(),Messages=1,Bytes=61,CallerBytesCleared=True,Sent=False,OriginalMovementParity=False)
    assert struct.unpack_from('<HI',frame)==(5,0x80ce5e46) and struct.unpack_from('<I',frame,10)[0]==0x71
    assert m.records(text,'soe_point_correction_ingress=')==[dict(AtStep=20,Sequence=0,Hex=profile['SoeCorrectionDatagrams'][0],QueuedApplications=1,QueuedMessages=1,QueuedBytes=61,ControlHex=[profile['SoeCorrectionAcks'][0]],RejectedFraming=False,CallerControlsCleared=True,Sent=False)]
    assert not m.records(text,'point_queued_correction_ready=') and not m.records(text,'point_full_correction=') and not m.records(text,'manual_input_intent=')
    assert m.records(text,'soe_application_ingress_cleanup=')==[dict(Disposed=True,IsFaulted=False,ApplicationsQueued=1,ControlsTransferred=1,BorrowedPumpPreserved=True,BorrowedTransportPreserved=True,Sent=False)]
    assert m.records(text,'camera_orbit_cleanup=')==[dict(Closed=True,IsFaulted=False,AcceptedCount=0,HandledCount=0,BorrowedCameraPreserved=True,ExternalPosePreserved=True)]
    assert m.records(text,'run_input_host_cleanup=')==[dict(Closed=True,ClockReadCount=0,SinkCallCount=0,AcceptedCount=0,HandledCount=0,IsFaulted=False,ControllerPreserved=True,SpeedPreserved=True,Speed=4)]
    assert m.records(text,'manual_input_host_cleanup=')==[dict(Closed=True,ClockReadCount=0,AcceptedCount=0,HandledCount=0,IsFaulted=False,IntentSinkBorrowed=True,ControllerPreserved=True,GuiFreed=True)]
    assert m.records(text,'manual_outbound_queue_cleanup=')==[dict(Disposed=True,PendingBefore=0,QueuedBytesBefore=0,Count=0,QueuedBytes=0,Transferred=3,OwnsBorrowedWorld=False,OwnsBorrowedStore=False,WorldAlreadyClosed=True,Sent=False)]
    assert m.records(text,'soe_root_bridge_cleanup=')==[dict(Disposed=True,OwnedPendingBefore=0,OwnedPendingAfter=0,Accepted=3,BorrowedQueueCount=0,BorrowedQueuePreserved=True,BorrowedTransportPreserved=True,Sent=False)]
    assert m.records(text,'soe_root_transport_cleanup=')==[dict(Closed=True,Datagrams=3,PendingMessages=0,RetainedPackets=0,OwnsSyntheticTransport=True,SocketCreated=False,Sent=False)]
    assert m.records(text,'point_input_host_cleanup=')==[dict(Closed=True,ClockReadCount=3,AcceptedCount=3,HandledCount=3,IsFaulted=False,OutboundPackets=3,BorrowedAdapterPreserved=True,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert m.records(text,'point_input_cleanup=')==[dict(Disposed=True,Attempts=3,OwnsBorrowers=False,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert m.records(text,'point_target_closed_world_cleanup=')==[dict(Disposed=True,Updates=20,DriverInactive=True,WorldAlreadyClosedByPump=True,BorrowedTerrainPreserved=True)]
    assert m.records(text,'point_camera_cleanup=')==[dict(Disposed=True,Owned=0,BorrowedCameraPreserved=True,BorrowedTerrainPreserved=True)]
    assert m.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
    assert m.records(text,'terrain_supplied_material_cleanup=')==[dict(Allocated=1,Disposed=1,Drained=True,Reads=2)]
    failure=m.records(text,'pump_failure_cleanup=');assert len(failure)==1 and failure[0]['calls']==failure[0]['closed']==3 and failure[0]['drained'] and failure[0]['actors']==0
    for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','PASS: pumped point_target='):assert bad not in text
    return dict(Passed=True,CompletedMotionPrefix=20,MalformedApplicationBytes=61,ValidSoeEnvelope=True,TransportSequenceConsumed=True,UnsentControlTransferred=True,IngressCopiedApplication=True,LaterPumpClosedWorld=True,HistoricalOutboundDatagrams=3,AllOwnersDrained=True,RollbackClaimed=False,Sent=False)
if __name__=='__main__':
    profile,log,out=sys.argv[1:4];result=verify(json.loads(Path(profile).read_text()),Path(log).read_text(encoding='utf-8-sig'));p=Path(out)
    assert not p.exists() and p.resolve().is_relative_to((ROOT/'local-private').resolve());p.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
