"""Verify strict destination failure retains the independently declared movement/wire prefix."""
import importlib.util,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('fault79_prior76',ROOT/'poc/round76-tests/Integration/idle_playground_peer.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
p,fixture,log,out=sys.argv[1:];p,fixture=[json.loads(Path(x).read_text()) for x in (p,fixture)];text=Path(log).read_text(encoding='utf-8-sig')
assert p['Version']==13 and p['InteractiveReplay'] and p['SyntheticFrameClock'] and not p['PlaygroundSettings']['HoldMissingDestination'] and not fixture['NativeOutputUsed']
n=next(i for i,w in enumerate(fixture['Movement']) if w['Held']);assert n==225
moves=m.records(text,'point_target_state=');loc=m.records(text,'accepted_locomotion_state=');cad=m.records(text,'offline_root_cadence=');assert len(moves)==len(loc)==len(cad)==n
ready,=m.records(text,'point_target_ready=');start=ready['TimestampMicroseconds'];queued=[];last=0;lastpose=None;lastspeed=previous=0.
for i,(row,l,cr,w) in enumerate(zip(moves,loc,cad,fixture['Movement'])):
 assert row['Index']==i and row['CompletedUpdates']==row['AttemptedUpdates']==i+1 and row['TimestampMicroseconds']==start+(i+1)*10000 and row['FaultType'] is None
 m.near(row['Native'],w['Native'],5e-6,('independent prefix pose',i));m.near([row['Speed']],[w['Speed']],2e-6,('prefix speed',i));assert row['Mode']==w['Mode']
 phase=fixture['Phase'][i];assert l['Mode']==phase['Mode'] and l['Events']==phase['Events'] and l['Generation']==phase['Generation']
 for key in ('Frame','NormalizedPhase','AcceptedDistance','UnwrappedPhase'):m.near([l[key]],[phase[key]],3e-5,('prefix phase',i,key))
 speed=m.f(w['Speed']);position=list(map(m.f,w['Native']));elapsed=(i+1)*10000
 due=(previous>0 and speed==0) or ((lastpose is None or lastpose!=position or lastspeed!=speed) and elapsed-last>=100000) or (speed==0 and lastpose is not None and elapsed-last>=1000000)
 if due:last=elapsed;lastpose=position;lastspeed=speed;queued.append(i)
 assert cr['Index']==i and cr['Samples']==i+1 and cr['Status']==('Queued' if due else 'NotDue');previous=speed
roots=m.records(text,'point_outbound_root=');wires=m.records(text,'soe_root_datagram=');assert len(roots)==len(wires)==len(queued)
for seq,(i,row,wire) in enumerate(zip(queued,roots,wires)):
 assert row['AtStep']==wire['AtStep']==i and wire['Sequence']==seq
 raw=bytes.fromhex(row['Hex']);assert len(raw)==62 and struct.unpack_from('<HIII',raw)==(5,0x80ce5e46,0x1b,0x71)
 assert struct.unpack_from('<II',raw,22)==((p['OutboundTimestampBase']+(i+1)*10)&0xffffffff,p['OutboundCounterBase']+seq+1)
 qx,qy,qz,qw,x,z,y,speed=struct.unpack_from('<8f',raw,30);assert [x,y,z,speed]==list(map(m.f,moves[i]['Native']))+[m.f(moves[i]['Speed'])]
 m.near([2*qy*qw,qw*qw-qy*qy],fixture['Movement'][i]['Heading'],2e-6,('prefix wire heading',i))
 assert wire['Hex']==m.old.old.soe.frame(b'\0\x09'+struct.pack('>H',seq)+raw).hex().upper() and wire['AckHex']==m.old.old.soe.frame(b'\0\x15'+struct.pack('>H',seq)).hex().upper()
completion,=m.records(text,'offline_completion_summary=');assert completion==dict(Reason='Faulted',MaximumUpdates=6000,CompletedUpdates=n,AttemptedUpdates=n+1,Stopped=False,Faulted=True,Mode='Faulted',Speed=m.f(fixture['Movement'][n-1]['Speed']),TargetActive=False)
summary,=m.records(text,'offline_root_completion_summary=');assert summary==dict(Samples=n,Queued=len(queued),Drained=len(queued),NextSequence=len(queued),SessionFaulted=False,TransportState='Established',QueueCount=0,QueueBytes=0,PendingMessages=0,RetainedPackets=0,CleanDrain=True)
caption,=m.records(text,'offline_outcome_caption=');assert caption==dict(Reason='Faulted',Caption=f'Offline preview outcome\nReason: Stopped after an error\nUpdates: {n} / 6000\nOffline root frames: {len(queued)}\nBuffers: empty',Source='ActualControllerAndRootSnapshot',SocketCreated=False)
cleanup,=m.records(text,'pump_failure_cleanup=');assert cleanup['calls']==cleanup['closed']==3 and cleanup['drained'] and cleanup['actors']==0 and not cleanup['expectedInputLoss']
observer,=m.records(text,'accepted_locomotion_cleanup=');assert observer['Observations']==n and observer['BorrowedDriverPreserved'] and not observer['BorrowedDriverActive'] and observer['BorrowedDriverFault'] is not None and observer['HistoricStatePreserved']
owner,=m.records(text,'configured_session_owner_cleanup=');assert owner==dict(Disposed=True,OwnedPointDisposed=True,OwnedControllerDisposed=True,BorrowedWorldPreserved=True,BorrowedTerrainPreserved=True)
owner,=m.records(text,'configured_root_owner_cleanup=');assert owner==dict(Disposed=True,Closed=True,Samples=n,Drained=len(queued),HistoryPreserved=True,BorrowedQueuePreserved=True,PendingMessages=0,RetainedPackets=0)
hud,=m.records(text,'offline_outcome_hud_cleanup=');assert hud==dict(Retired=True,RetirementCount=1,RefreshCount=1,BorrowedOwnerPreserved=True)
assert not m.records(text,'offline_session_stopped=') and not m.records(text,'offline_outcome_capture_failed=') and 'PASS: pumped point_target=' not in text
for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','resource still in use'):assert bad not in text
value=dict(Passed=True,ExpectedStrictDestinationFailure=True,CompletedUpdates=n,AttemptedUpdates=n+1,ProtectedRootFrames=len(queued),PrimitiveReason='Faulted',ActualRootBuffersEmpty=True,RuntimeStopped=False,OriginalFootsteps=sum(len(l['Events']) for l in loc),PrefixPreserved=True,AllActorsDrained=True,FailureImageClaimed=False,RollbackClaimed=False)
out=Path(out);assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
