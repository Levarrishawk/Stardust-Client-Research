"""Before-native long trajectory and layered accepted-position boundary checks."""
import json, math, struct, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
import importlib.util
def module(name,path):
    s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
old=module('playground73_prior72','poc/round72-tests/Integration/camera_follow_peer.py')
phase=old.old;f=old.f;records=old.records
def near(a,b,t,label):
    assert max(abs(x-y) for x,y in zip(a,b))<=t,(label,a,b,t)
def tail_step(x,z,target,p):
    vx=vz=0.;dt=f(.01);radius=phase.value(p['ArrivalRadiusBits'])
    if target is not None:
        dx,dz=target[0]-x,target[1]-z;distance=math.hypot(dx,dz)
        if distance>radius:
            travel=min(4*dt,distance-radius*.5)*(1-1/1048576)
            vx,vz=f(dx/distance*travel/dt),f(dz/distance*travel/dt)
            for _ in range(8):
                nx,nz=f(x+f(vx*dt)),f(z+f(vz*dt))
                if 0<=(nx-x)*dx+(nz-z)*dz<=distance*distance:break
                vx,vz=f(vx*.5),f(vz*.5)
            else:vx=vz=0.
    return f(x+f(vx*dt)),f(z+f(vz*dt)),vx,vz
def verify_motion_boundary(p,g,moves,text):
    picks=records(text,'point_camera_pick=');inputs=records(text,'point_input_target=');assert len(picks)==len(inputs)==10
    assert [r['AtStep'] for r in inputs]==[0,10,18,35,150,300,450,600,750,900]
    phase.prior.ray.compare(g,[r['Native'] for r in picks]);targets={}
    for ordinal,(pick,row) in enumerate(zip(picks,inputs),1):
        hit=phase.prior.ray.expected(g,pick['Native']);assert hit is not None
        target=[f(hit['Position'][0]),f(hit['Position'][2])];near(list(map(f,row['Target'])),target,0.,('independent ray target',ordinal))
        expected=[phase.value(p['TargetNativeXBits']),phase.value(p['TargetNativeZBits'])] if ordinal==1 else [-1.5,0.] if row['AtStep'] in (150,450,750) else [2.,3.]
        near(target,expected,2e-5,('supplied target',ordinal));targets[row['AtStep']]=target
        assert row['Attempt']==row['TargetRevision']==ordinal and pick['ActualInputEvent'] and pick['ActualReferences']
        near(pick['Native']['Origin'],[f(pick['EngineOrigin'][0]),f(pick['EngineOrigin'][1]),-f(pick['EngineOrigin'][2])],0.,'ray origin reflection')
        near(pick['Native']['Direction'],[f(pick['EngineDirection'][0]),f(pick['EngineDirection'][1]),-f(pick['EngineDirection'][2])],0.,'ray direction reflection')
    x,z=-1.5,0.;target=None;dt=f(.01);radius=phase.value(p['ArrivalRadiusBits']);headings=[];heading=[0.,1.]
    for i,m in enumerate(moves):
        if i in targets:target=targets[i]
        if i in (15,25,998):target=None
        if i==20:x,z=-.5,1.;heading=[0.,1.]
        speed=phase.value(p['RunSpeedBits' if i>=34 or 27<=i<29 else 'WalkSpeedBits']) if i>=25 else phase.value(p['SpeedBits'])
        manual={25:(0,-1),26:(0,-1),27:(1,-1),28:(1,0),30:(0,1),32:(1,0),33:(1,0)}.get(i)
        if manual is not None:manual=phase.prior.camera_direction(p,manual,i)
        vx=vz=0.;paused=5<=i<8;dx,dz=(target[0]-x,target[1]-z) if target is not None else (0.,0.);distance=math.hypot(dx,dz)
        if manual is not None:
            length=max(1.,math.hypot(*manual));vx,vz=f(manual[0]/length*speed),f(manual[1]/length*speed)
        elif distance>radius and not paused:
            travel=min(speed*dt,distance-radius*.5)*(1-1/1048576);vx,vz=f(dx/distance*travel/dt),f(dz/distance*travel/dt)
            for _ in range(8):
                nx,nz=f(x+f(vx*dt)),f(z+f(vz*dt))
                if 0<=(nx-x)*dx+(nz-z)*dz<=distance*distance:break
                vx,vz=f(vx*.5),f(vz*.5)
            else:vx=vz=0.
        expected_speed=f(math.sqrt(f(f(vx*vx)+f(vz*vz))));near([m['Speed']],[expected_speed],2e-6,('velocity speed',i))
        near([m['Native'][0],m['Native'][2]],[f(x+f(vx*dt)),f(z+f(vz*dt))],2e-6,('accepted input step',i))
        if expected_speed>0:heading=[f(vx/expected_speed),f(vz/expected_speed)]
        headings.append(list(heading));x,z=f(m['Native'][0]),f(m['Native'][2])
    return headings
def generate(p,t,c):
    initial=phase.expected(dict(p,Version=7,Policy='SuppliedAcceptedLocomotionAndOfflinePeerDisconnect',MaximumUpdates=128),c)['Rows']
    assert len(initial)==109
    g=phase.prior.ray.surface.fixture_geometry(t);positions=[r['NativeXZ'] for r in initial];x,z=positions[-1];target=[2.,3.]
    for i in range(109,1000):
        if i in (150,300,450,600,750,900):target=[-1.5,0.] if i%300 else [2.,3.]
        if i==998:target=None
        x,z,_,_=tail_step(x,z,target,p);positions.append([x,z])
    movement=[dict(Native=[x,old.height(g,x,z),z]) for x,z in positions]
    expected_phase=phase.phase_from_verified_positions(p,c,movement)
    rows=[dict(r,Index=i,NativeXZ=positions[i]) for i,r in enumerate(expected_phase)]
    fixture_phase=dict(Version=1,SyntheticPolicy=True,NativeOutputUsed=False,Threshold=p['LocomotionRunThreshold'],Clips=c,Rows=rows,
        CorrectionRebasedAt=20,PositionTolerance=5e-6,PhaseTolerance=6e-5,HeadingTolerance=6e-5)
    offset=p['FollowNativeOffset'];factor=math.exp(-.01/p['FollowResponseSeconds']);pivot=list(map(f,old.add([-1.5,old.height(g,-1.5,0),0],offset)));initial_pivot=list(pivot);follow=[]
    for i,m in enumerate(movement):
        if i==20:pivot=list(map(f,old.add([-.5,old.height(g,-.5,1),1],offset)))
        target=old.add(m['Native'],offset);pivot=[f(t+(a-t)*factor) for a,t in zip(pivot,target)];camera,forward=old.pose(pivot,*old.angles(i))
        follow.append(dict(NativePivot=list(pivot),EnginePosition=camera,EngineForward=forward))
    return dict(Version=1,NativeOutputUsed=False,Locomotion=fixture_phase,InitialNativePivot=initial_pivot,Movement=movement,Follow=follow)
def verify(p,t,fixture,text,negative=False):
    assert p['Version']==9 and p['MaximumUpdates']==1000 and p['Policy']=='SuppliedLongOfflinePlayground' and p['InvalidDisconnectConnection']==negative
    assert not fixture['NativeOutputUsed'];moves=records(text,'point_target_state=');assert len(moves)==1000
    result=phase.verify_phase(fixture['Locomotion'],text,p,expected_updates=1000)
    g=phase.prior.ray.surface.fixture_geometry(t);headings=verify_motion_boundary(p,g,moves,text)
    follow=records(text,'camera_follow_state=');hud=records(text,'offline_playground_hud=');cadence=records(text,'offline_root_cadence=')
    assert len(follow)==len(hud)==len(cadence)==1000
    ready,=records(text,'point_target_ready=');start=ready['TimestampMicroseconds'];assert ready['MaximumUpdates']==1000
    pivot=list(map(f,old.add(list(map(f,ready['Start'])),p['FollowNativeOffset'])));factor=math.exp(-.01/p['FollowResponseSeconds'])
    previous_speed=0.;last_success=0;last_pose=None;last_speed=0.;counter=p['OutboundCounterBase'];queued=[]
    for i,(m,follow_row,h,c) in enumerate(zip(moves,follow,hud,cadence)):
        position=list(map(f,m['Native']));near(position,fixture['Movement'][i]['Native'],5e-6,('independent trajectory',i))
        near([position[1]],[old.height(g,position[0],position[2])],2e-7,('ground',i));near(m['Engine'],[position[0],position[1],-position[2]],2e-7,('engine reflection',i))
        assert m['Index']==i and m['AttemptedUpdates']==m['CompletedUpdates']==i+1 and m['TimestampMicroseconds']==start+(i+1)*10000
        assert m['StepsThisUpdate']==1 and m['DroppedMicroseconds']==m['SuspendedMicroseconds']==0 and m['FaultType'] is None
        assert m['PublicationRetained'] and m['VisualRetained'] and m['calls']==m['closed']==3
        if i==20:pivot=list(map(f,old.add([-.5,old.height(g,-.5,1),1],p['FollowNativeOffset'])))
        target=old.add(position,p['FollowNativeOffset']);pivot=[f(t+(a-t)*factor) for a,t in zip(pivot,target)]
        near(follow_row['NativePivot'],fixture['Follow'][i]['NativePivot'],5e-6,('independent follow',i));near(list(map(f,follow_row['NativePivot'])),pivot,2e-7,('tight follow',i))
        near(list(map(f,follow_row['NativeTarget'])),list(map(f,target)),2e-7,('target',i));near(follow_row['EnginePivot'],[pivot[0],pivot[1],-pivot[2]],3e-7,('pivot reflection',i))
        camera,forward=old.pose(pivot,*old.angles(i));near(follow_row['EnginePosition'],camera,3e-6,('camera',i));near(follow_row['EngineForward'],forward,4e-7,('forward',i))
        assert follow_row['Updates']==i+1 and follow_row['Rebases']==int(i>=20) and follow_row['TimestampMicroseconds']==m['TimestampMicroseconds']
        assert follow_row['AcceptedCount']==follow_row['HandledCount']==(0 if i<24 else 1 if i<26 else 2 if i<33 else 3)
        assert h['Index']==i and h['RefreshCount']==i+2 and h['RemainingUpdates']==999-i and h['ActualAcceptedDriverState'] and h['Offline']
        assert h['Caption'].startswith('Offline preview\nModern supplied movement\n'+m['Mode']+'  |  Speed ') and 'WASD move' in h['Caption']
        # Custom .NET numeric format rounds these decimal tenths halfway upward.
        remaining_tenths=(999-i+5)//10
        assert 'Speed '+format(m['Speed'],'.2f')+'\n' in h['Caption'] and 'Preview time remaining: '+format(remaining_tenths/10,'.1f')+'s\n' in h['Caption']
        assert ('Target: active' in h['Caption'])==(m['Mode'] in ('Moving','Arrived','Paused'))
        # Cadence model consumes independently verified current pose/speed values, never status, counter or bytes.
        speed=f(m['Speed']);elapsed=(i+1)*10000
        due=(previous_speed>0 and speed==0) or ((last_pose is None or last_pose!=position or last_speed!=speed) and elapsed-last_success>=100000) or (speed==0 and last_pose is not None and elapsed-last_success>=1000000)
        status='Queued' if due else 'NotDue'
        if due:counter=(counter+1)&0xffffffff;last_success=elapsed;last_pose=position;last_speed=speed;queued.append(i)
        assert c==dict(Index=i,Status=status,ElapsedMicroseconds=elapsed,Samples=i+1,MovementCounter=counter,Timestamp=(p['OutboundTimestampBase']+elapsed//1000)&0xffffffff,QueueCount=int(due),ActualIssuedLocalRow=True,Sent=False),(i,c,status)
        previous_speed=speed
    assert moves[-1]['Mode']=='Idle' and moves[-1]['Speed']==0 and moves[-2]['Mode']=='Idle'
    assert [r['AtStep'] for r in records(text,'offline_playground_target=')]==[150,300,450,600,750,900]
    assert [r['AtStep'] for r in records(text,'point_target_cleared=')]==[15,998]
    outgoing=records(text,'point_outbound_root=');wire=records(text,'soe_root_datagram=');assert len(outgoing)==len(wire)==len(queued)
    for sequence,(i,raw_row,datagram) in enumerate(zip(queued,outgoing,wire)):
        raw=bytes.fromhex(raw_row['Hex']);assert len(raw)==62 and struct.unpack_from('<HIII',raw)==(5,0x80ce5e46,0x1b,0x71)
        timestamp,counter=struct.unpack_from('<II',raw,22);assert timestamp==(p['OutboundTimestampBase']+(i+1)*10)&0xffffffff and counter==(p['OutboundCounterBase']+sequence+1)&0xffffffff
        qx,qy,qz,qw,x,z,y,speed=struct.unpack_from('<8f',raw,30);near([x,y,z],list(map(f,moves[i]['Native'])),0.,('wire position',i));near([speed],[f(moves[i]['Speed'])],0.,('wire speed',i))
        assert abs(qx)<2e-7 and abs(qz)<2e-7 and abs(qy*qy+qw*qw-1)<3e-7
        # Independent observer heading has already passed phase and accepted-pose checks; root basis must match it.
        near([2*qy*qw,qw*qw-qy*qy],headings[i],2e-6,('wire heading',i))
        protected=old.soe.frame(b'\0\x09'+struct.pack('>H',sequence)+raw).hex().upper();ack=old.soe.frame(b'\0\x15'+struct.pack('>H',sequence)).hex().upper()
        assert datagram['Hex']==protected and datagram['AckHex']==ack==p['SoeCadenceAckHex'][sequence] and datagram['Sequence']==sequence
        assert raw_row['AtStep']==raw_row['ReadAtStep']==datagram['AtStep']==datagram['ReadAtStep']==i
        assert datagram['AckReleased']==1 and datagram['PendingMessages']==datagram['RetainedPackets']==0 and not datagram['Sent'] and not datagram['SocketCreated']
    cleanup,=records(text,'offline_root_cadence_cleanup=');assert cleanup==dict(Disposed=True,Samples=1000,MovementCounter=counter,QueuedCount=len(queued),ElapsedMicroseconds=10000000,LastSuccessfulQueueMicroseconds=last_success,IsFaulted=False,BorrowedWorldPreserved=True,BorrowedQueuePreserved=True,Sent=False)
    assert records(text,'offline_playground_hud_cleanup=')==[dict(Retired=True,RetirementCount=1,RefreshCount=1001,BorrowedOwnerPreserved=True)]
    assert records(text,'camera_follow_cleanup=')==[dict(Disposed=True,Updates=1000,Rebases=1,HistoricStatePreserved=True,BorrowedCameraPreserved=True,BorrowedOrbitPreserved=True)]
    for prefix in ('manual_outbound_queue_cleanup=','soe_root_transport_cleanup='):
        row,=records(text,prefix);assert row.get('PendingMessages',0)==row.get('RetainedPackets',0)==row.get('Count',0)==row.get('QueuedBytes',0)==0
    ingress,=records(text,'soe_application_ingress_cleanup=');assert ingress['ApplicationsQueued']==ingress['ControlsTransferred']==(2 if negative else 3) and ingress['IsFaulted']==negative
    disconnect,=records(text,'soe_outer_peer_disconnect_failure=' if negative else 'soe_outer_peer_disconnect=');assert disconnect['AtStep']==disconnect['CompletedUpdates']==1000 and disconnect['BorrowedQueuePreserved'] and disconnect['BorrowedWorldPreserved']
    if negative:
        assert 'Disconnect connection ID mismatch.' in text and not records(text,'soe_outer_peer_disconnect=')
        failure,=records(text,'pump_failure_cleanup=');assert failure['drained'] and failure['actors']==0 and failure['calls']==failure['closed']==3
    else:assert disconnect['SuffixDelivered']==False and disconnect['CallerPolicy']=='DropQueuedPrefixOnSceneClose' and 'PASS: pumped point_target=' in text
    for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:'):assert bad not in text
    result.update(Passed=True,OfflineUpdates=1000,Retargets=6,HudRefreshes=1001,ScheduledRootFrames=len(queued),MatchedProtectedFrames=len(queued),ModernPolicy=True,SocketCreated=False,Sent=False,MismatchedDisconnectRejected=negative,OriginalParityClaimed=False)
    return result
if __name__=='__main__':
    mode,p,t,c,fixture,*rest=sys.argv[1:];p,t,c=[json.loads(Path(x).read_text()) for x in (p,t,c)];out=Path(fixture)
    assert out.resolve().is_relative_to((ROOT/'local-private').resolve())
    if mode=='generate':
        assert not out.exists();value=generate(p,t,c);out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(dict(Updates=1000,Events=sum(len(r['Events']) for r in value['Locomotion']['Rows']),NativeOutputUsed=False)))
    else:
        assert mode in ('verify','verify-negative')
        log,destination=rest;result=verify(p,t,json.loads(out.read_text()),Path(log).read_text(encoding='utf-8-sig'),mode=='verify-negative');destination=Path(destination);assert not destination.exists() and destination.resolve().is_relative_to((ROOT/'local-private').resolve());destination.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
