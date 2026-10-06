"""Before-native route prefix and explicit final stop expectations for the modern offline host."""
import importlib.util,json,math,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('controls75_prior73',ROOT/'poc/round73-tests/Integration/offline_playground_peer.py');old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
records=old.records;f=old.f;near=old.near

def motion_boundary(p,g,moves,text):
    picks=records(text,'point_camera_pick=');inputs=records(text,'point_input_target=')
    assert len(picks)==len(inputs)==7 and [r['AtStep'] for r in inputs]==[0,10,18,35,150,300,450]
    old.phase.prior.ray.compare(g,[r['Native'] for r in picks]);targets={}
    for ordinal,(pick,row) in enumerate(zip(picks,inputs),1):
        hit=old.phase.prior.ray.expected(g,pick['Native']);assert hit is not None
        target=[f(hit['Position'][0]),f(hit['Position'][2])];near(list(map(f,row['Target'])),target,0.,('issued ray target',ordinal))
        wanted=[old.phase.value(p['TargetNativeXBits']),old.phase.value(p['TargetNativeZBits'])] if ordinal==1 else [-1.5,0.] if row['AtStep'] in (150,450) else [2.,3.]
        near(target,wanted,2e-5,('supplied target',ordinal));targets[row['AtStep']]=target
        assert row['Attempt']==row['TargetRevision']==ordinal and pick['ActualInputEvent'] and pick['ActualReferences']
    x,z=-1.5,0.;target=None;dt=f(.01);radius=old.phase.value(p['ArrivalRadiusBits']);heading=[0.,1.];headings=[]
    for i,m in enumerate(moves):
        if i in targets:target=targets[i]
        if i in (15,25,600):target=None
        if i==20:x,z=-.5,1.;heading=[0.,1.]
        speed=old.phase.value(p['RunSpeedBits' if i>=34 or 27<=i<29 else 'WalkSpeedBits']) if i>=25 else old.phase.value(p['SpeedBits'])
        manual={25:(0,-1),26:(0,-1),27:(1,-1),28:(1,0),30:(0,1),32:(1,0),33:(1,0)}.get(i)
        if manual is not None:manual=old.phase.prior.camera_direction(p,manual,i)
        vx=vz=0.;dx,dz=(target[0]-x,target[1]-z) if target is not None else (0.,0.);distance=math.hypot(dx,dz)
        if manual is not None:
            length=max(1.,math.hypot(*manual));vx,vz=f(manual[0]/length*speed),f(manual[1]/length*speed)
        elif distance>radius and not 5<=i<8:
            travel=min(speed*dt,distance-radius*.5)*(1-1/1048576);vx,vz=f(dx/distance*travel/dt),f(dz/distance*travel/dt)
            for _ in range(8):
                nx,nz=f(x+f(vx*dt)),f(z+f(vz*dt))
                if 0<=(nx-x)*dx+(nz-z)*dz<=distance*distance:break
                vx,vz=f(vx*.5),f(vz*.5)
            else:vx=vz=0.
        expected_speed=f(math.sqrt(f(f(vx*vx)+f(vz*vz))));near([m['Speed']],[expected_speed],2e-6,('exact velocity speed',i))
        near([m['Native'][0],m['Native'][2]],[f(x+f(vx*dt)),f(z+f(vz*dt))],2e-6,('accepted step',i))
        if expected_speed>0:heading=[f(vx/expected_speed),f(vz/expected_speed)]
        headings.append(list(heading));x,z=f(m['Native'][0]),f(m['Native'][2])
    return headings

def generate(p,t,c):
    stop=p['SessionStopAtStep'];assert stop==600
    prior=old.generate(p,t,c);movement=prior['Movement'][:stop]+[prior['Movement'][stop-1]]
    phases=old.phase.phase_from_verified_positions(p,c,movement)
    for i,row in enumerate(phases):row.update(Index=i,NativeXZ=[movement[i]['Native'][0],movement[i]['Native'][2]])
    phase=dict(prior['Locomotion'],Rows=phases)
    factor=math.exp(-.01/p['FollowResponseSeconds']);pivot=prior['InitialNativePivot'];follow=[]
    g=old.phase.prior.ray.surface.fixture_geometry(t)
    for i,m in enumerate(movement):
        if i==20:pivot=list(map(f,old.old.add([-.5,old.old.height(g,-.5,1),1],p['FollowNativeOffset'])))
        target=old.old.add(m['Native'],p['FollowNativeOffset']);pivot=[f(b+(a-b)*factor) for a,b in zip(pivot,target)]
        camera,forward=old.old.pose(pivot,*old.old.angles(i));follow.append(dict(NativePivot=list(pivot),EnginePosition=camera,EngineForward=forward))
    return dict(Version=1,NativeOutputUsed=False,StopAtStep=stop,Movement=movement,Locomotion=phase,Follow=follow)

def verify(p,t,fixture,text):
    assert p['Version']==11 and p['Policy']=='SuppliedOfflineSessionControls' and not p['InvalidSessionInput']
    assert not fixture['NativeOutputUsed'];n=fixture['StopAtStep']+1;moves=records(text,'point_target_state=');assert len(moves)==n==601
    result=old.phase.verify_phase(fixture['Locomotion'],text,p,expected_updates=n)
    g=old.phase.prior.ray.surface.fixture_geometry(t);headings=motion_boundary(p,g,moves,text);ready,=records(text,'point_target_ready=');start=ready['TimestampMicroseconds']
    follow=records(text,'camera_follow_state=');hud=records(text,'offline_playground_hud=');cadence=records(text,'offline_root_cadence=')
    assert len(follow)==len(hud)==len(cadence)==n
    previous_speed=0.;last_success=0;last_pose=None;last_speed=0.;counter=p['OutboundCounterBase'];queued=[]
    for i,(m,fr,h,cr) in enumerate(zip(moves,follow,hud,cadence)):
        position=list(map(f,m['Native']));near(position,fixture['Movement'][i]['Native'],5e-6,('before-native trajectory',i))
        near([position[1]],[old.old.height(g,position[0],position[2])],2e-7,('ground',i));near(m['Engine'],[position[0],position[1],-position[2]],2e-7,('engine reflection',i))
        assert m['Index']==i and m['AttemptedUpdates']==m['CompletedUpdates']==i+1 and m['TimestampMicroseconds']==start+(i+1)*10000
        assert m['StepsThisUpdate']==1 and m['DroppedMicroseconds']==m['SuspendedMicroseconds']==0 and m['FaultType'] is None
        assert m['PublicationRetained'] and m['VisualRetained'] and m['calls']==m['closed']==3
        near(fr['NativePivot'],fixture['Follow'][i]['NativePivot'],5e-6,('before-native follow',i))
        near(fr['EnginePosition'],fixture['Follow'][i]['EnginePosition'],6e-6,('camera',i));near(fr['EngineForward'],fixture['Follow'][i]['EngineForward'],4e-7,('forward',i))
        assert fr['Updates']==i+1 and fr['Rebases']==int(i>=20) and fr['TimestampMicroseconds']==m['TimestampMicroseconds']
        assert h['Index']==i and h['RefreshCount']==i+2 and h['RemainingUpdates']==999-i and h['ActualAcceptedDriverState'] and h['Offline']
        assert 'P pause' in h['Caption'] and 'Escape' in h['Caption'] and 'Speed '+format(m['Speed'],'.2f')+'\n' in h['Caption']
        speed=f(m['Speed']);elapsed=(i+1)*10000
        due=(previous_speed>0 and speed==0) or ((last_pose is None or last_pose!=position or last_speed!=speed) and elapsed-last_success>=100000) or (speed==0 and last_pose is not None and elapsed-last_success>=1000000)
        if due:counter=(counter+1)&0xffffffff;last_success=elapsed;last_pose=position;last_speed=speed;queued.append(i)
        assert cr==dict(Index=i,Status='Queued' if due else 'NotDue',ElapsedMicroseconds=elapsed,Samples=i+1,MovementCounter=counter,Timestamp=(p['OutboundTimestampBase']+elapsed//1000)&0xffffffff,QueueCount=int(due),ActualIssuedLocalRow=True,Sent=False),(i,cr,due)
        previous_speed=speed
    assert moves[-1]['Mode']=='Idle' and moves[-1]['Speed']==0 and moves[-1]['Native']==moves[-2]['Native']
    assert [r['AtStep'] for r in records(text,'offline_playground_target=')]==[150,300,450]
    assert [r['AtStep'] for r in records(text,'point_input_target=')]==[0,10,18,35,150,300,450]
    keys=records(text,'offline_session_input=');assert [(r['AtStep'],r['Key']) for r in keys]==[(5,'P'),(8,'P'),(600,'Escape')]
    assert all(r['ActualViewportEvent'] and r['Handled'] and not r['DriverStateChangedByIntent'] for r in keys)
    outgoing=records(text,'point_outbound_root=');wire=records(text,'soe_root_datagram=');assert len(outgoing)==len(wire)==len(queued)
    for sequence,(i,raw_row,datagram) in enumerate(zip(queued,outgoing,wire)):
        raw=bytes.fromhex(raw_row['Hex']);assert len(raw)==62 and struct.unpack_from('<HIII',raw)==(5,0x80ce5e46,0x1b,0x71)
        timestamp,wire_counter=struct.unpack_from('<II',raw,22);assert timestamp==(p['OutboundTimestampBase']+(i+1)*10)&0xffffffff and wire_counter==(p['OutboundCounterBase']+sequence+1)&0xffffffff
        qx,qy,qz,qw,x,z,y,speed=struct.unpack_from('<8f',raw,30);near([x,y,z],list(map(f,moves[i]['Native'])),0.,('wire position',i));near([speed],[f(moves[i]['Speed'])],0.,('wire speed',i))
        near([2*qy*qw,qw*qw-qy*qy],headings[i],2e-6,('independent root heading',i))
        protected=old.old.soe.frame(b'\0\x09'+struct.pack('>H',sequence)+raw).hex().upper();ack=old.old.soe.frame(b'\0\x15'+struct.pack('>H',sequence)).hex().upper()
        assert datagram['Hex']==protected and datagram['AckHex']==ack==p['SoeCadenceAckHex'][sequence] and datagram['Sequence']==sequence
        assert raw_row['AtStep']==raw_row['ReadAtStep']==datagram['AtStep']==datagram['ReadAtStep']==i and datagram['AckReleased']==1 and not datagram['Sent']
    cleanup,=records(text,'offline_root_session_cleanup=');assert cleanup['Samples']==n and cleanup['Queued']==cleanup['Drained']==len(queued) and not cleanup['IsFaulted'] and cleanup['BorrowedQueuePreserved'] and cleanup['BorrowedTransportPreserved']
    stopped,=records(text,'offline_session_stopped=');assert stopped['CompletedUpdates']==n and stopped['Mode']=='Idle' and stopped['Speed']==0 and stopped['TargetCleared'] and not stopped['PeerDisconnectInjected']
    controller,=records(text,'offline_session_controller_cleanup=');assert controller['HistoricStatePreserved'] and controller['BorrowedDriverPreserved']
    for prefix in ('manual_outbound_queue_cleanup=','soe_root_transport_cleanup='):
        row,=records(text,prefix);assert row.get('PendingMessages',0)==row.get('RetainedPackets',0)==row.get('Count',0)==row.get('QueuedBytes',0)==0
    plans=records(text,'offline_frame_clock_state=');remainder=reserved=dropped=0;clamped=0
    for i,row in enumerate(plans):
        assert row['Index']==i and row['CompletedDriverUpdates']==reserved-int(reserved>=21)
        raw=row['RawMicroseconds'];admitted=min(raw,250000);assert row['AdmittedMicroseconds']==admitted and row['ClampedMicroseconds']==raw-admitted
        if p['SyntheticFrameClock']:assert raw==(0,5000,15000,33000,250000)[i%5]
        else:assert raw==round(row['MeasuredSeconds']*1000000)
        available=remainder+admitted;due=available//10000;steps=min(due,8,2000-reserved);reserved+=steps;remainder=available%10000;delta=(due-steps)*10000;dropped+=delta
        assert row['Receipt']==dict(Steps=steps,StepMicroseconds=10000,TotalReservedSteps=reserved,RemainderMicroseconds=remainder,DroppedMicroseconds=delta,TotalDroppedMicroseconds=dropped,Exhausted=reserved==2000)
        clamped+=raw-admitted;assert row['TotalClampedMicroseconds']==clamped
    clock,=records(text,'offline_frame_clock_cleanup=');assert clock['CompletedDriverUpdates']==clock['ReportedPointIndex']==n and clock['State']==plans[-1]['Receipt'] and n+1<=reserved<=n+8
    assert not records(text,'soe_outer_peer_disconnect=') and 'PASS: pumped point_target=' in text
    for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:'):assert bad not in text
    result.update(Passed=True,CompletedUpdates=n,ActualSessionKeyEvents=3,ScheduledRootFrames=len(queued),FramePlans=len(plans),ReservedSlots=reserved,DroppedMicroseconds=dropped,FinalZeroTick=True,SocketCreated=False,Sent=False,OriginalParityClaimed=False)
    return result

def verify_negative(p,t,fixture,text):
    assert p['InvalidSessionInput'] and not fixture['NativeOutputUsed']
    moves=records(text,'point_target_state=');assert len(moves)==5
    for i,(m,want) in enumerate(zip(moves,fixture['Movement'])):
        near(m['Native'],want['Native'],5e-6,('independent rejected-input prefix',i));assert m['CompletedUpdates']==i+1
    key,=records(text,'offline_session_input=');assert key['AtStep']==5 and key['Key']=='P' and key['ActualViewportEvent'] and not key['Handled'] and not key['DriverStateChangedByIntent']
    diagnostic=key['Diagnostic'];assert diagnostic['SinkMayHaveRun'] and not diagnostic['SinkReturned'] and not diagnostic['StateCommitted'] and diagnostic['FailureType']=='System.InvalidOperationException'
    assert key['State']['CompletedUpdates']==5 and not key['State']['Paused'] and not key['State']['StopRequested'] and not key['State']['Stopped'] and not key['State']['IsFaulted']
    input_cleanup,=records(text,'offline_session_input_cleanup=');assert input_cleanup['Closed'] and input_cleanup['IsFaulted'] and input_cleanup['AcceptedCount']==input_cleanup['HandledCount']==0
    roots,=records(text,'offline_root_session_cleanup=');assert roots['Samples']==5 and roots['Queued']==roots['Drained']==roots['NextSequence']==0 and not roots['IsFaulted'] and roots['BorrowedQueuePreserved'] and roots['BorrowedTransportPreserved']
    controller,=records(text,'offline_session_controller_cleanup=');assert controller['State']['CompletedUpdates']==5 and controller['HistoricStatePreserved'] and controller['BorrowedDriverPreserved']
    clock,=records(text,'offline_frame_clock_cleanup=');assert clock['CompletedDriverUpdates']==clock['ReportedPointIndex']==5 and clock['State']['TotalReservedSteps']==13
    queue,=records(text,'manual_outbound_queue_cleanup=');assert queue['PendingBefore']==queue['QueuedBytesBefore']==queue['Count']==queue['QueuedBytes']==0
    failure,=records(text,'pump_failure_cleanup=');assert failure['drained'] and failure['actors']==0 and failure['calls']==failure['closed']==3
    assert not records(text,'offline_session_stopped=') and not records(text,'point_outbound_root=') and 'PASS: pumped point_target=' not in text
    for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:'):assert bad not in text
    return dict(Passed=True,CompletedUpdates=5,ReservedSlots=13,InputSinkRejected=True,BorrowedDriverUnchangedByIntent=True,RootOwnersPreservedUntilCallerCleanup=True,AllOwnersDrained=True,RollbackClaimed=False)

if __name__=='__main__':
    mode,p,t,c,fixture,*rest=sys.argv[1:];p,t,c=[json.loads(Path(x).read_text()) for x in (p,t,c)];out=Path(fixture)
    assert out.resolve().is_relative_to((ROOT/'local-private').resolve())
    if mode=='generate':
        assert not out.exists();value=generate(p,t,c);out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(dict(Updates=len(value['Movement']),NativeOutputUsed=False)))
    else:
        assert mode in ('verify','verify-negative');log,destination=rest;verifier=verify if mode=='verify' else verify_negative;result=verifier(p,t,json.loads(out.read_text()),Path(log).read_text(encoding='utf-8-sig'));destination=Path(destination);assert not destination.exists() and destination.resolve().is_relative_to((ROOT/'local-private').resolve());destination.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
