"""Verify a configured three-update no-input session against its declared initial pose."""
import importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('short77_prior76',ROOT/'poc/round76-tests/Integration/idle_playground_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
p,terrain,log,out=sys.argv[1:];p=json.loads(Path(p).read_text());terrain=json.loads(Path(terrain).read_text());text=Path(log).read_text(encoding='utf-8-sig')
assert p['Version']==13 and not p['InteractiveReplay'] and not p['SyntheticFrameClock']
assert p['PlaygroundSettings']['MaximumUpdates']==3 and p['PlaygroundSettings']['MaximumClickAttempts']==128
f=lambda v:struct.unpack('<f',struct.pack('<f',v))[0]
g=m.phase.prior.ray.surface.fixture_geometry(terrain);position=[-1.5,m.old.old.height(g,-1.5,0),0.]
ready,=m.records(text,'point_target_ready=');assert list(map(f,ready['Start']))==position and ready['Target'] is None and not ready['ActualTarget']
rows=m.records(text,'point_target_state=');assert len(rows)==3
for i,row in enumerate(rows):
 assert row['Index']==i and row['CompletedUpdates']==row['AttemptedUpdates']==i+1 and row['Mode']=='Idle' and row['Speed']==0 and list(map(f,row['Native']))==position and row['FaultType'] is None
 assert row['TimestampMicroseconds']==ready['TimestampMicroseconds']+(i+1)*10000
for key in ('offline_interactive_key=','offline_interactive_focus=','point_input_target=','point_correction_queued=','point_outbound_root='):assert not m.records(text,key)
stopped,=m.records(text,'offline_session_stopped=');assert stopped['CompletedUpdates']==3 and stopped['Mode']=='Idle' and stopped['Speed']==0 and stopped['TargetCleared']
clock,=m.records(text,'offline_frame_clock_cleanup=');assert clock['State']['Exhausted'] and clock['State']['TotalReservedSteps']==clock['CompletedDriverUpdates']==3
controls,=m.records(text,'offline_session_controller_cleanup=');assert controls['State']['Stopped'] and not controls['State']['IsFaulted'] and controls['BorrowedDriverPreserved']
ready,=m.records(text,'offline_interactive_ready=');assert ready['MaximumUpdates']==3 and ready['MaximumClickAttempts']==128 and not ready['Replay']
assert 'PASS: pumped point_target=true idle_start=true grounded=true scripted_correction=false' in text
for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','resource still in use'):assert bad not in text
value=dict(Passed=True,CompletedUpdates=3,ConfiguredMaximumClicks=128,IssuedClicks=0,ScriptedEvents=0,FinalReservationExhausted=True,FinalZeroTick=True,OriginalParityClaimed=False)
out=Path(out);assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
