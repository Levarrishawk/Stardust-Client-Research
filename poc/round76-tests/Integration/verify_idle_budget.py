import importlib.util,json,struct,math
from pathlib import Path
import sys
r=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('idle76',r/'poc/round76-tests/Integration/idle_playground_peer.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
profile,terrain,clips,log,output=sys.argv[1:]
p,t,c=[json.loads(Path(x).read_text()) for x in (profile,terrain,clips)]
text=Path(log).read_text(encoding='utf-8-sig')
assert p['InteractiveReplay']==p['SyntheticFrameClock']==False and p['MaximumUpdates']==6000
records=m.records;f=m.f;g=m.phase.prior.ray.surface.fixture_geometry(t);position=[-1.5,m.old.old.height(g,-1.5,0),0.]
moves=records(text,'point_target_state=');d=records(text,'offline_interactive_destination=');loc=records(text,'accepted_locomotion_state=');assert len(moves)==len(d)==len(loc)==6000
ready,=records(text,'point_target_ready=');assert list(map(f,ready['Start']))==position and ready['Target'] is None and not ready['ActualTarget']
for i,(row,dest,l) in enumerate(zip(moves,d,loc)):
 assert row['Index']==i and row['CompletedUpdates']==row['AttemptedUpdates']==i+1 and row['Mode']=='Idle' and row['Speed']==0 and list(map(f,row['Native']))==position and row['FaultType'] is None
 assert row['TimestampMicroseconds']==ready['TimestampMicroseconds']+(i+1)*10000 and row['StepsThisUpdate']==1
 assert list(map(f,dest['Receipt']['Proposed']))==position and dest['Receipt']['Velocity']==[0,0,0] and not dest['Receipt']['HeldAtMissingDestination'] and not dest['Paused'] and dest['GameplayEnabled'] and dest['ClickAttempts']==0
 assert l['Mode']=='Idle' and l['Events']==[] and l['Generation']==1 and l['Observations']==i+1 and l['AcceptedDistance']==0 and l['ActualDriver'] and l['OriginalAssetRecords']
assert not records(text,'offline_interactive_key=') and not records(text,'offline_interactive_focus=') and not records(text,'point_input_target=') and not records(text,'point_correction_queued=')
roots=records(text,'point_outbound_root=');wire=records(text,'soe_root_datagram=');assert len(roots)==len(wire)==60
for seq,(row,w) in enumerate(zip(roots,wire)):
 i=9+seq*100;assert row['AtStep']==i and w['AtStep']==i and w['Sequence']==seq
 raw=bytes.fromhex(row['Hex']);assert len(raw)==62 and struct.unpack_from('<II',raw,22)==((p['OutboundTimestampBase']+(i+1)*10)&0xffffffff,p['OutboundCounterBase']+seq+1)
 assert list(struct.unpack_from('<8f',raw,30))==[0,0,0,1,position[0],position[2],position[1],0]
 assert w['Hex']==m.old.old.soe.frame(b'\0\x09'+struct.pack('>H',seq)+raw).hex().upper() and w['AckHex']==m.old.old.soe.frame(b'\0\x15'+struct.pack('>H',seq)).hex().upper()
stopped,=records(text,'offline_session_stopped=');assert stopped['CompletedUpdates']==6000 and stopped['Mode']=='Idle' and stopped['Speed']==0 and stopped['TargetCleared']
clock,=records(text,'offline_frame_clock_cleanup=');assert clock['State']['Exhausted'] and clock['State']['TotalReservedSteps']==6000 and clock['CompletedDriverUpdates']==6000
cleanup,=records(text,'offline_root_session_cleanup=');assert cleanup['Samples']==6000 and cleanup['Queued']==cleanup['Drained']==60
assert 'PASS: pumped point_target=true idle_start=true grounded=true scripted_correction=false' in text
for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','resource still in use'):assert bad not in text
receipt=dict(Passed=True,CompletedUpdates=6000,IdleUpdates=6000,OriginalFootstepOccurrences=0,ScheduledStationaryFrames=60,ScriptedInputEvents=0,ClickAttempts=0,FinalReservationExhausted=True,FinalZeroTick=True,SocketCreated=False,ExpectedValuesUseNativeOutputs=False,IdlePlanDeclaredInBeforeNativeProfile=True)
out=Path(output);assert not out.exists() and out.resolve().is_relative_to((r/'local-private').resolve());out.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
