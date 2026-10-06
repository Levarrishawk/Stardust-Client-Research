"""Independent follow/pivot oracle; legacy test projections occur only after new boundaries pass."""
import importlib.util,json,math,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('follow72_prior71',ROOT/'poc/round71-tests/Integration/accepted_locomotion_peer.py');old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
f=old.f;records=old.prior.records;soe=old.prior.soe
prior_verify=old.prior.verify
old.prior.verify=lambda *args:prior_verify(*args,accepted_position_boundary=True)

def legacy(p):return dict(p,Version=7,Policy='SuppliedAcceptedLocomotionAndOfflinePeerDisconnect',SoeDisconnectHex=soe.frame(struct.pack('>HIH',5,p['SoeConnectionId']^int(p['InvalidDisconnectConnection']),6)).hex().upper())
def height(g,x,z):return f(old.prior.ray.surface.expected(g,x,z)['Height'])
def add(p,o):return [float(a)+b for a,b in zip(p,o)]
def pose(pivot,yaw,pitch,distance):
 e=[pivot[0],pivot[1],-pivot[2]];d=[distance*math.sin(yaw)*math.cos(pitch),distance*math.sin(pitch),distance*math.cos(yaw)*math.cos(pitch)]
 return [f(a+b) for a,b in zip(e,d)],[-math.sin(yaw)*math.cos(pitch),-math.sin(pitch),-math.cos(yaw)*math.cos(pitch)]
def angles(i):return (0 if i<24 else -.5 if i<26 else -.6,.6 if i<24 else .5,18 if i<33 else 17)
def near(a,b,t,label):assert max(abs(x-y) for x,y in zip(a,b))<=t,(label,a,b,t)
def generate(profile,terrain,clips,out):
 p=json.loads(Path(profile).read_text());t=json.loads(Path(terrain).read_text());c=json.loads(Path(clips).read_text());phase=old.expected(legacy(p),c)
 g=old.prior.ray.surface.fixture_geometry(t);o=p['FollowNativeOffset'];factor=math.exp(-.01/p['FollowResponseSeconds']);pivot=list(map(f,add([-1.5,height(g,-1.5,0),0],o)));initial=list(pivot);rows=[];rebase=None
 for i,r in enumerate(phase['Rows']):
  if i==20:
   x,z=[old.value(p[k]) for k in ('CorrectionXBits','CorrectionZBits')];pivot=list(map(f,add([x,height(g,x,z),z],o)));rebase=list(pivot)
  x,z=r['NativeXZ'];position=[x,height(g,x,z),z];target=add(position,o);pivot=[f(t+(a-t)*factor) for a,t in zip(pivot,target)];camera,forward=pose(pivot,*angles(i))
  rows.append(dict(Index=i,NativePosition=position,NativePivot=list(pivot),NativeTarget=list(map(f,target)),EnginePosition=camera,EngineForward=forward))
 fixture=dict(Version=1,ModernPolicy=True,NativeOutputUsed=False,Locomotion=phase,InitialNativePivot=initial,RebaseNativePivot=rebase,Rows=rows,PositionTolerance=5e-6)
 path=Path(out);assert not path.exists() and path.resolve().is_relative_to((ROOT/'local-private').resolve());path.write_text(json.dumps(fixture,indent=2)+'\n');return dict(Updates=len(rows),Events=sum(len(r['Events']) for r in phase['Rows']),NativeOutputUsed=False)
def verify(p,t,w,m,fixture,text,negative=False):
 assert p['Version']==8 and p['Policy']=='SuppliedFollowingCameraAndOuterPeerDisconnect' and p['InvalidDisconnectConnection']==negative and not fixture['NativeOutputUsed']
 lp=legacy(p);movement=records(text,'point_target_state=');follow=records(text,'camera_follow_state=');assert len(movement)==len(follow)==len(fixture['Rows'])==109
 old.verify_phase(fixture['Locomotion'],text,lp) # Verify independent movement before accepted positions seed tight camera boundary.
 ready,=records(text,'camera_follow_ready=');assert ready['NativeOffset']==p['FollowNativeOffset'] and ready['ActualDriverPosition'] and ready['ModernFollowPolicy'] and not ready['OriginalCameraParity'] and ready['StepMicroseconds']==10000 and ready['ResponseSeconds']==.1
 near(ready['NativePivot'],fixture['InitialNativePivot'],5e-6,'independent initial')
 rebase,=records(text,'camera_follow_rebase=');assert rebase['AtStep']==20 and rebase['Updates']==20 and rebase['Rebases']==1 and rebase['TimestampMicroseconds']==ready['TimestampMicroseconds']+200000 and rebase['ClockPreserved'] and rebase['UpdateBudgetPreserved'] and rebase['ActualAuthoritativeRow']
 near(rebase['NativePivot'],fixture['RebaseNativePivot'],5e-6,'independent snap')
 correction,=records(text,'point_queued_correction_ready=');start,=records(text,'point_target_ready=');o=p['FollowNativeOffset'];factor=math.exp(-.01/p['FollowResponseSeconds']);tight=list(map(f,add(list(map(f,start['Start'])),o)));near(ready['NativePivot'],tight,2e-7,'accepted initial')
 by_index={};max_error=0
 for i,(r,movement_row,want) in enumerate(zip(follow,movement,fixture['Rows'])):
  if i==20:tight=list(map(f,add(list(map(f,correction['Native'])),o)));near(rebase['NativePivot'],tight,2e-7,'accepted snap')
  target=add(list(map(f,movement_row['Native'])),o);tight=[f(t+(a-t)*factor) for a,t in zip(tight,target)];by_index[i]=list(tight)
  assert r['Index']==i and r['Updates']==i+1 and r['Rebases']==int(i>=20) and r['TimestampMicroseconds']==ready['TimestampMicroseconds']+(i+1)*10000
  near(r['NativePivot'],want['NativePivot'],5e-6,'independent follow');near(list(map(f,r['NativePivot'])),tight,2e-7,'tight follow');near(list(map(f,r['NativeTarget'])),list(map(f,target)),2e-7,'target')
  near(list(map(f,r['EnginePivot'])),[tight[0],tight[1],-tight[2]],2e-7,'single reflection');yaw,pitch,distance=angles(i)
  assert abs(r['Yaw']-yaw)<2e-7 and abs(r['Pitch']-pitch)<2e-7 and r['Distance']==distance
  camera,forward=pose(tight,yaw,pitch,distance);near(r['EnginePosition'],camera,3e-6,'Godot position');near(r['EngineForward'],forward,4e-7,'Godot direction')
  assert r['AcceptedCount']==r['HandledCount']==(0 if i<24 else 1 if i<26 else 2 if i<33 else 3)
  assert all(r[k] for k in ('ActualDriverPosition','ActualBorrowedCamera','EngineZReflectedOnce','ModernFollowPolicy')) and not r['OriginalCameraParity']
  max_error=max(max_error,max(abs(a-b) for a,b in zip(r['NativePivot'],tight)))
 assert records(text,'camera_follow_cleanup=')==[dict(Disposed=True,Updates=109,Rebases=1,HistoricStatePreserved=True,BorrowedCameraPreserved=True,BorrowedOrbitPreserved=True)]
 orbit=records(text,'camera_orbit_state=');assert len(orbit)==4
 for r,at,yaw,pitch,distance in zip(orbit,(-1,24,26,33),(0,-.5,-.6,-.6),(.6,.5,.5,.5),(18,18,18,17)):
  pivot=[p['OrbitPivot'][0],p['OrbitPivot'][1],-p['OrbitPivot'][2]] if at==-1 else by_index[at-1];camera,forward=pose(pivot,yaw,pitch,distance);near(r['EnginePosition'],camera,3e-6,'orbit pivot');near(r['EngineForward'],forward,4e-7,'orbit direction')
 app=bytes.fromhex(p['SoeOuterPrefixApplicationHex']);expected=bytearray.fromhex(p['SoeCorrectionHex']);struct.pack_into('<I',expected,26,p['CorrectionCounter']+1);assert app==expected and len(app)==62
 blocks=[b'\0\x09'+struct.pack('>H',2)+app,struct.pack('>HIH',5,p['SoeConnectionId']^int(negative),6),b'\0\x09'+struct.pack('>H',3)+app]
 framed=soe.frame(b'\0\x03'+b''.join(bytes([len(b)])+b for b in blocks)).hex().upper();ack=soe.frame(b'\0\x15'+struct.pack('>H',2)).hex().upper();assert p['SoeOuterDisconnectHex']==framed and p['SoeOuterAckHex']==ack
 if not negative:
  assert records(text,'soe_outer_peer_disconnect=')==[dict(Hex=framed,AtStep=109,PeerDisconnected=True,TransportClosed=True,ReturnedApplications=1,QueuedApplications=1,QueuedBytes=62,ControlHex=[ack],CallerControlsCleared=True,CallerPolicy='DropQueuedPrefixOnSceneClose',BorrowedWorldPreserved=True,BorrowedQueuePreserved=True,BridgeDeclinedBeforeTake=True,PendingMessages=0,RetainedPackets=0,CompletedUpdates=109,SuffixDelivered=False,Sent=False,OriginalCloseReasonSemantics=False)]
  assert not records(text,'soe_outer_peer_disconnect_failure=')
 else:
  assert records(text,'soe_outer_peer_disconnect_failure=')==[dict(Hex=framed,AtStep=109,TransportEstablished=True,IngressFaulted=True,BorrowedWorldPreserved=True,BorrowedPumpPreserved=True,BorrowedQueuePreserved=True,CompletedUpdates=109,ReliabilityPrefixConsumed=True,UntransferredControlsZeroingSourceInspected=True,RollbackClaimed=False,Sent=False)]
  assert not records(text,'soe_outer_peer_disconnect=')
 cleanup,=records(text,'soe_application_ingress_cleanup=');assert cleanup['ApplicationsQueued']==(2 if negative else 3) and cleanup['ControlsTransferred']==(2 if negative else 3) and cleanup['IsFaulted']==negative
 # New dynamic camera and disconnect semantics checked above; explicitly project only changed metadata for prior reusable gates.
 projected=[]
 for line in text.splitlines():
  if line.startswith('camera_orbit_state='):
   r=json.loads(line.split('=',1)[1]);pivot=p['OrbitPivot'];d=r['Distance'];y=r['Yaw'];angle=r['Pitch'];r['EnginePosition']=[f(pivot[0]+d*math.sin(y)*math.cos(angle)),f(pivot[1]+d*math.sin(angle)),f(pivot[2]+d*math.cos(y)*math.cos(angle))];r['EngineForward']=[-math.sin(y)*math.cos(angle),-math.sin(angle),-math.cos(y)*math.cos(angle)];line='camera_orbit_state='+json.dumps(r)
  elif line.startswith('soe_application_ingress_cleanup='):
   r=json.loads(line.split('=',1)[1]);r['ApplicationsQueued']=r['ControlsTransferred']=2;line='soe_application_ingress_cleanup='+json.dumps(r)
  projected.append(line)
 if negative:projected.append('soe_peer_disconnect_failure='+json.dumps(dict(Hex=lp['SoeDisconnectHex'],AtStep=109,TransportEstablished=True,IngressFaulted=True,BorrowedWorldPreserved=True,BorrowedPumpPreserved=True,BorrowedQueuePreserved=True,CompletedUpdates=109,Sent=False)))
 else:projected.append('soe_peer_disconnect='+json.dumps(dict(Hex=lp['SoeDisconnectHex'],AtStep=109,PeerDisconnected=True,TransportClosed=True,QueuedApplications=0,Controls=0,BorrowedWorldPreserved=True,BorrowedPumpPreserved=True,BorrowedQueuePreserved=True,BridgeDeclinedBeforeTake=True,PendingMessages=0,RetainedPackets=0,CompletedUpdates=109,Sent=False,OriginalCloseReasonSemantics=False)))
 result=old.verify_join(lp,t,w,m,fixture['Locomotion'],'\n'.join(projected),negative);result.update(Passed=True,FollowUpdates=109,CorrectionSnaps=1,CameraNativePivotMaxError=max_error,IndependentBeforeNativeFollow=True,TightAcceptedPositionBoundary=True,LegacyTestProjectionExplicit=True,TightAcceptedPositionMovementBoundary=True,OuterDisconnectPrefixQueued=0 if negative else 1,UnsentControlsTransferred=2 if negative else 3,CallerDropsQueuedPrefixOnClose=not negative,RootMotionApplied=False);return result
if __name__=='__main__':
 if sys.argv[1]=='generate':assert len(sys.argv)==6;print(json.dumps(generate(*sys.argv[2:])))
 else:
  assert sys.argv[1] in ('verify','verify-negative') and len(sys.argv)==9
  p,t,w,m,fixture,log,out=sys.argv[2:];result=verify(*[json.loads(Path(x).read_text()) for x in (p,t,w,m,fixture)],Path(log).read_text(encoding='utf-8-sig'),sys.argv[1]=='verify-negative');o=Path(out);assert not o.exists() and o.resolve().is_relative_to((ROOT/'local-private').resolve());o.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
