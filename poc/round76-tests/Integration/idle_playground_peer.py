"""Independent idle-start movement/focus/edge and protected-byte oracle; generate before native."""
import importlib.util,json,math,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('idle76_prior75',ROOT/'poc/round75-tests/Integration/session_controls_peer.py');prior=importlib.util.module_from_spec(s);s.loader.exec_module(prior)
old=prior.old;phase=old.phase;f=prior.f;records=prior.records;near=prior.near

def generate(p,t,c):
    assert p['Version']==12 and p['InteractiveReplay']
    g=phase.prior.ray.surface.fixture_geometry(t);x,z=-1.5,0.;y=old.old.height(g,x,z);dt=f(.01);speed=3.;manual=0;target=None;paused=False;heading=[0.,1.];movement=[]
    pivot=list(map(f,old.old.add([x,y,z],p['FollowNativeOffset'])));factor=math.exp(-.01/p['FollowResponseSeconds']);follow=[]
    for i in range(401):
        if i in (10,50):manual=1;target=None
        if i in (20,60):speed=4.
        if i==40:manual=0;speed=3.;paused=True
        if i==45:paused=False
        if i==250:manual=-1
        if i==280:manual=0
        if i==300:target=(2.,2.);manual=0
        if i==320:paused=True
        if i==325:paused=False
        if i==400:target=None;manual=0;paused=False
        vx=vz=0.;radius=phase.value(p['ArrivalRadiusBits'])
        if not paused:
            if manual:vz=f(manual*speed)
            elif target:
                dx,dz=target[0]-x,target[1]-z;distance=math.hypot(dx,dz)
                if distance>radius:
                    travel=min(speed*dt,distance-radius*.5)*(1-1/1048576);vx,vz=f(dx/distance*travel/dt),f(dz/distance*travel/dt)
                    for _ in range(8):
                        nx,nz=f(x+f(vx*dt)),f(z+f(vz*dt))
                        if 0<=(nx-x)*dx+(nz-z)*dz<=distance*distance:break
                        vx,vz=f(vx*.5),f(vz*.5)
                    else:vx=vz=0.
        proposed=[f(x+f(vx*dt)),y,f(z+f(vz*dt))]
        hit=phase.prior.ray.surface.expected(g,proposed[0],proposed[2]);held=hit is None
        if held:vx=vz=0.
        x,z=f(x+f(vx*dt)),f(z+f(vz*dt));y=old.old.height(g,x,z)
        magnitude=f(math.sqrt(f(f(vx*vx)+f(vz*vz))))
        if magnitude:heading=[f(vx/magnitude),f(vz/magnitude)]
        mode='Paused' if paused else 'Manual' if manual else 'Idle' if target is None else 'Arrived' if math.hypot(target[0]-x,target[1]-z)<=radius else 'Moving'
        movement.append(dict(Native=[x,y,z],Speed=magnitude,Mode=mode,Proposed=proposed,Velocity=[vx,0.,vz],Held=held,Heading=list(heading),Paused=paused,GameplayEnabled=not 40<=i<45))
        dest=old.old.add([x,y,z],p['FollowNativeOffset']);pivot=[f(b+(a-b)*factor) for a,b in zip(pivot,dest)]
        camera,forward=old.old.pose(pivot,p['OrbitYaw'],p['OrbitPitch'],p['OrbitDistance']);follow.append(dict(Pivot=list(pivot),Camera=camera,Forward=forward))
    rows=phase.phase_from_verified_positions(dict(p,CorrectionAtStep=-1),c,movement)
    assert sum(r['Held'] for r in movement)>10 and movement[250]['Speed']==4. and movement[-1]['Mode']=='Idle'
    return dict(Version=1,NativeOutputUsed=False,Movement=movement,Follow=follow,Phase=rows,Clips=c)

def verify(p,t,fixture,text):
    assert not fixture['NativeOutputUsed'] and p['Version']==12 and p['InteractiveReplay']
    moves=records(text,'point_target_state=');dest=records(text,'offline_interactive_destination=');follow=records(text,'camera_follow_state=');loc=records(text,'accepted_locomotion_state=');hud=records(text,'offline_playground_hud=');cad=records(text,'offline_root_cadence=')
    assert len(moves)==len(dest)==len(follow)==len(loc)==len(hud)==len(cad)==401
    ready,=records(text,'point_target_ready=');assert ready['Target'] is None and not ready['ActualTarget'] and ready['Speed']==3 and ready['MaximumUpdates']==6000
    start=ready['TimestampMicroseconds'];queued=[];previous=0.;last=0;lastpose=None;lastspeed=0.;counter=p['OutboundCounterBase']
    for i,(m,d,fr,l,h,cr,w) in enumerate(zip(moves,dest,follow,loc,hud,cad,fixture['Movement'])):
        near(m['Native'],w['Native'],5e-6,('before-native pose',i));near([m['Speed']],[w['Speed']],2e-6,('speed',i));assert m['Mode']==w['Mode']
        assert m['Index']==i and m['AttemptedUpdates']==m['CompletedUpdates']==i+1 and m['TimestampMicroseconds']==start+(i+1)*10000 and m['StepsThisUpdate']==1 and m['FaultType'] is None
        assert m['PublicationRetained'] and m['VisualRetained'] and m['calls']==m['closed']==3
        near(m['Engine'],[m['Native'][0],m['Native'][1],-m['Native'][2]],2e-7,('reflection',i))
        near(d['Receipt']['Proposed'],w['Proposed'],5e-6,('candidate',i));near(d['Receipt']['Velocity'],w['Velocity'],2e-6,('accepted velocity',i))
        assert d['Receipt']['HeldAtMissingDestination']==w['Held'] and d['Paused']==w['Paused'] and d['GameplayEnabled']==w['GameplayEnabled'] and d['SimulationBudgetRemaining']==5999-i
        fw=fixture['Follow'][i];near(fr['NativePivot'],fw['Pivot'],6e-6,('follow',i));near(fr['EnginePosition'],fw['Camera'],7e-6,('camera',i));near(fr['EngineForward'],fw['Forward'],4e-7,('forward',i))
        assert fr['Updates']==i+1 and fr['Rebases']==0 and fr['TimestampMicroseconds']==m['TimestampMicroseconds'] and fr['ActualDriverPosition']
        assert h['RemainingUpdates']==5999-i and 'P pause' in h['Caption'] and 'Escape' in h['Caption'] and h['Offline']
        want=fixture['Phase'][i]
        assert l['Mode']==want['Mode'] and l['Generation']==want['Generation'] and l['Switches']==want['Switches'] and l['Events']==want['Events'] and l['CompletedCycles']==want['CompletedCycles']
        for k in ('Frame','NormalizedPhase','AcceptedDistance','UnwrappedPhase'):near([l[k]],[want[k]],3e-5,('before-native phase',i,k))
        assert l['Observations']==i+1 and l['ActualDriver'] and l['ActualLocalRow'] and l['NoSecondElapsedClock'] and not l['RootMotionApplied']
        if i:assert l['MotionRevision']==loc[i-1]['MotionRevision']+1
        speed=f(m['Speed']);position=list(map(f,m['Native']));elapsed=(i+1)*10000
        due=(previous>0 and speed==0) or ((lastpose is None or lastpose!=position or lastspeed!=speed) and elapsed-last>=100000) or (speed==0 and lastpose is not None and elapsed-last>=1000000)
        if due:counter=(counter+1)&0xffffffff;last=elapsed;lastpose=position;lastspeed=speed;queued.append(i)
        assert cr==dict(Index=i,Status='Queued' if due else 'NotDue',ElapsedMicroseconds=elapsed,Samples=i+1,MovementCounter=counter,Timestamp=(p['OutboundTimestampBase']+elapsed//1000)&0xffffffff,QueueCount=int(due),ActualIssuedLocalRow=True,Sent=False)
        previous=speed
    tight=phase.phase_from_verified_positions(dict(p,CorrectionAtStep=-1),fixture['Clips'],moves)
    for i,(row,want) in enumerate(zip(loc,tight)):
        for k,w in want.items():
            if k in ('DistanceErrorBound','PhaseErrorBound','EndFrame'):continue
            if k=='Heading':near(row[k],w,2e-7,('heading',i))
            elif isinstance(w,float):
                tolerance=want['DistanceErrorBound'] if k=='AcceptedDistance' else want['PhaseErrorBound']*(want['EndFrame'] if k in ('Frame','UnwrappedPhase') else 1) if k in ('Frame','UnwrappedPhase','NormalizedPhase') else 0.
                near([row[k]],[w],tolerance+2e-13,('tight observer',i,k))
            else:assert row[k]==w,(i,k,row[k],w)
    focus=records(text,'offline_interactive_focus=');assert [(r['AtStep'],r['Gained'],r['Paused']) for r in focus]==[(40,False,True),(45,True,False)]
    assert all(r['HeldManualEmpty'] and r['RunHeldEmpty'] and r['SpeedIntent']==3 and r['SyntheticWindowSignal'] and not r['OsFocusProof'] for r in focus)
    keys=records(text,'offline_interactive_key=');assert [(r['AtStep'],r['Key'],r['Paused']) for r in keys]==[(320,'P',True),(325,'P',False),(400,'Escape',False)]
    picks=records(text,'point_camera_pick=');inputs=records(text,'point_input_target=');assert len(picks)==len(inputs)==1 and inputs[0]['AtStep']==300 and inputs[0]['Attempt']==1
    g=phase.prior.ray.surface.fixture_geometry(t);phase.prior.ray.compare(g,[r['Native'] for r in picks]);near(inputs[0]['Target'],[2,2],2e-5,'actual ray target')
    outgoing=records(text,'point_outbound_root=');wires=records(text,'soe_root_datagram=');assert len(outgoing)==len(wires)==len(queued)
    for sequence,(i,rawrow,wire) in enumerate(zip(queued,outgoing,wires)):
        raw=bytes.fromhex(rawrow['Hex']);assert len(raw)==62 and struct.unpack_from('<HIII',raw)==(5,0x80ce5e46,0x1b,0x71)
        assert struct.unpack_from('<II',raw,22)==((p['OutboundTimestampBase']+(i+1)*10)&0xffffffff,(p['OutboundCounterBase']+sequence+1)&0xffffffff)
        qx,qy,qz,qw,x,z,y,speed=struct.unpack_from('<8f',raw,30);near([x,y,z,speed],list(map(f,moves[i]['Native']))+[f(moves[i]['Speed'])],0.,('wire values',i))
        near([2*qy*qw,qw*qw-qy*qy],fixture['Movement'][i]['Heading'],2e-6,('wire heading',i))
        assert wire['Hex']==old.old.soe.frame(b'\0\x09'+struct.pack('>H',sequence)+raw).hex().upper()
        assert wire['AckHex']==old.old.soe.frame(b'\0\x15'+struct.pack('>H',sequence)).hex().upper() and wire['Sequence']==sequence and wire['AckReleased']==1 and not wire['Sent']
    roots,=records(text,'offline_root_session_cleanup=');assert roots['Samples']==401 and roots['Queued']==roots['Drained']==len(queued) and not roots['IsFaulted']
    stopped,=records(text,'offline_session_stopped=');assert stopped['CompletedUpdates']==401 and stopped['Mode']=='Idle' and stopped['Speed']==0 and stopped['TargetCleared'] and not stopped['PeerDisconnectInjected']
    assert moves[-1]['Native']==moves[-2]['Native'] and not records(text,'point_correction_queued=') and not records(text,'accepted_locomotion_rebase=')
    plans=records(text,'offline_frame_clock_state=');reserved=remainder=dropped=0
    for i,row in enumerate(plans):
        assert row['Index']==i and row['CompletedDriverUpdates']==reserved
        raw=row['RawMicroseconds'];admitted=min(raw,250000);assert row['AdmittedMicroseconds']==admitted
        if p['SyntheticFrameClock']:assert raw==(0,5000,15000,33000,250000)[i%5]
        else:assert raw==round(row['MeasuredSeconds']*1000000)
        due=(remainder+admitted)//10000;steps=min(due,8,6000-reserved);reserved+=steps;remainder=(remainder+admitted)%10000;delta=(due-steps)*10000;dropped+=delta
        assert row['Receipt']==dict(Steps=steps,StepMicroseconds=10000,TotalReservedSteps=reserved,RemainderMicroseconds=remainder,DroppedMicroseconds=delta,TotalDroppedMicroseconds=dropped,Exhausted=reserved==6000)
    clock,=records(text,'offline_frame_clock_cleanup=');assert clock['CompletedDriverUpdates']==401 and clock['State']==plans[-1]['Receipt'] and 401<=reserved<=408
    cleanup,=records(text,'offline_interactive_focus_cleanup=');assert cleanup['Closed'] and not cleanup['IsFaulted']
    for prefix in ('manual_outbound_queue_cleanup=','soe_root_transport_cleanup='):
        row,=records(text,prefix);assert row.get('PendingMessages',0)==row.get('RetainedPackets',0)==row.get('Count',0)==row.get('QueuedBytes',0)==0
    assert 'PASS: pumped point_target=' in text
    lines=text.splitlines();errors=[];shader_diagnostics=0
    for ix,line in enumerate(lines):
        if 'ERROR:' not in line:continue
        assert ix<next(j for j,v in enumerate(lines) if v.startswith('terrain_supplied_material_cpu=')),(ix,line)
        if line=='ERROR: Condition "err != OK" is true.':
            assert lines[ix+1]=='   at: initialize (drivers/gles3/shader_gles3.cpp:802)';shader_diagnostics+=1
        else:assert line=='ERROR: Failed to read the root certificate store.',line;errors.append(line)
    for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','resource still in use'):assert bad not in text
    return dict(Passed=True,CompletedUpdates=401,HeldDestinationUpdates=sum(r['Held'] for r in fixture['Movement']),FocusTransitions=2,ActualSessionKeys=3,OriginalFootstepOccurrences=sum(len(r['Events']) for r in loc),ScheduledRootFrames=len(queued),FramePlans=len(plans),ReservedSlots=reserved,DroppedMicroseconds=dropped,IdleStart=True,ReverseRecovery=True,FinalZeroTick=True,NativeOutputUsed=False,SocketCreated=False,OriginalParityClaimed=False,OfflineStartupCertificateWarnings=len(errors),ExistingGlesStartupDiagnostics=shader_diagnostics)

def verify_negative(p,t,fixture,text):
    assert p['InvalidSessionInput'] and not fixture['NativeOutputUsed']
    moves=records(text,'point_target_state=');assert len(moves)==40
    for i,(row,want) in enumerate(zip(moves,fixture['Movement'])):
        near(row['Native'],want['Native'],5e-6,('rejected focus prefix',i));assert row['CompletedUpdates']==i+1 and row['FaultType'] is None
    failure,=records(text,'offline_interactive_failure=')
    assert failure['AtStep']==failure['CompletedUpdates']==failure['RootSamples']==40 and not failure['Paused'] and not failure['RollbackClaimed']
    assert failure['FocusDiagnostic']==dict(Kind='Loss',PriorPaused=False,GameplayMayHaveRun=True,GameplayReturned=False,PauseMayHaveRun=False,PauseReturned=False,StateCommitted=False,FailureType='System.InvalidOperationException',FromWindowSignal=True)
    roots,=records(text,'offline_root_session_cleanup=');assert roots['Samples']==40 and roots['Queued']==roots['Drained']==4 and not roots['IsFaulted']
    controller,=records(text,'offline_session_controller_cleanup=');assert controller['State']['CompletedUpdates']==40 and not controller['State']['IsFaulted'] and not controller['State']['Paused'] and not controller['State']['Stopped']
    focus,=records(text,'offline_interactive_focus_cleanup=');assert focus['Closed'] and focus['IsFaulted']
    for prefix in ('manual_outbound_queue_cleanup=','soe_root_transport_cleanup='):
        row,=records(text,prefix);assert row.get('PendingMessages',0)==row.get('RetainedPackets',0)==row.get('Count',0)==row.get('QueuedBytes',0)==0
    cleanup,=records(text,'pump_failure_cleanup=');assert cleanup['drained'] and cleanup['actors']==0 and cleanup['calls']==cleanup['closed']==3
    assert not records(text,'offline_session_stopped=') and 'PASS: pumped point_target=' not in text
    return dict(Passed=True,CompletedUpdates=40,RootSamples=40,ProtectedRootFrames=4,FocusSinkMayHaveRun=True,PauseSinkNotCalled=True,NoCompletedTickAtFailure=True,BorrowedPrefixPreserved=True,AllOwnersDrained=True,RollbackClaimed=False)

if __name__=='__main__':
    mode,p,t,c,fixture,*rest=sys.argv[1:];p,t,c=[json.loads(Path(x).read_text()) for x in (p,t,c)];out=Path(fixture)
    assert out.resolve().is_relative_to((ROOT/'local-private').resolve())
    if mode=='generate':
        assert not out.exists();value=generate(p,t,c);out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(dict(Updates=401,Held=sum(r['Held'] for r in value['Movement']),NativeOutputUsed=False)))
    else:
        assert mode in ('verify','verify-negative');log,destination=rest;verifier=verify if mode=='verify' else verify_negative;value=verifier(p,t,json.loads(out.read_text()),Path(log).read_text(encoding='utf-8-sig'));destination=Path(destination);assert not destination.exists() and destination.resolve().is_relative_to((ROOT/'local-private').resolve());destination.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(value))
