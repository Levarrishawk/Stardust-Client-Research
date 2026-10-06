"""Malformed owned queued correction closes outer owners after a checked motion prefix."""
import json,sys,importlib.util,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('queued66_records',ROOT/'poc/round62-tests/Integration/composition_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
def verify(text):
    assert 'Transform length mismatch.' in text
    rows=m.records(text,'point_target_state=');assert len(rows)==20
    assert [r['Index'] for r in rows]==list(range(20))
    assert all(r['AttemptedUpdates']==r['CompletedUpdates']==r['Index']+1 and r['FaultType'] is None for r in rows)
    assert [r['Index'] for r in rows if r['Mode']=='Paused']==[5,6,7]
    assert all(r['Speed']==0 for r in rows[5:8])
    picks=m.records(text,'point_camera_pick=');assert len(picks)==2 and picks[-1]['MarkerBuilds']==1
    queued=m.records(text,'point_correction_queued=');assert len(queued)==1
    q=queued[0];packet=bytes.fromhex(q['Hex']);assert q['AtStep']==20 and len(packet)==61
    assert q['Messages']==1 and q['Bytes']==61 and q['CallerBytesCleared'] and not q['Sent']
    assert struct.unpack_from('<HI',packet)==(5,0x80ce5e46) and struct.unpack_from('<I',packet,10)[0]==0x71
    assert not m.records(text,'point_queued_correction_ready=') and not m.records(text,'point_full_correction=')
    assert not m.records(text,'point_target_cleanup=')
    assert m.records(text,'point_target_closed_world_cleanup=')==[dict(Disposed=True,Updates=20,DriverInactive=True,WorldAlreadyClosedByPump=True,BorrowedTerrainPreserved=True)]
    assert m.records(text,'point_input_cleanup=')==[dict(Disposed=True,Attempts=2,OwnsBorrowers=False,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert m.records(text,'point_camera_cleanup=')==[dict(Disposed=True,Owned=0,BorrowedCameraPreserved=True,BorrowedTerrainPreserved=True)]
    assert m.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
    assert m.records(text,'terrain_supplied_material_cleanup=')==[dict(Allocated=1,Disposed=1,Drained=True,Reads=2)]
    failure=m.records(text,'pump_failure_cleanup=');assert len(failure)==1
    assert failure[0]['calls']==failure[0]['closed']==3 and failure[0]['drained'] and failure[0]['actors']==0
    assert 'Input cleanup:' not in text and 'Terrain cleanup:' not in text and 'Exit cleanup:' not in text
    assert 'PASS: pumped point_target=' not in text
    return dict(Passed=True,CompletedMotionPrefix=20,ActivePausedTicks=3,InputTargets=2,MalformedQueuedBytes=61,
        NoFullCorrectionPublished=True,OuterWorldAlreadyClosed=True,InputAndControllerAndMarkerDrained=True,TerrainVisualsDrained=16,TexturesDrained=1,ArchiveJobsDrained=3,RollbackClaimed=False)
if __name__=='__main__':
    log,out=sys.argv[1:3];result=verify(Path(log).read_text(encoding='utf-8-sig'));p=Path(out)
    assert not p.exists() and p.resolve().is_relative_to((ROOT/'local-private').resolve())
    p.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
