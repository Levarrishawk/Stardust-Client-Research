"""Independent supplied trajectory fixture and layered accepted-position phase verifier; no phase output seeds."""
import importlib.util,json,math,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def module(name,path):
    spec=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
prior=module('accepted70_phase_prereq','poc/round70-tests/Integration/camera_ingress_peer.py')
f=prior.f
value=prior.value

def expected(profile,clips):
    assert profile['Version']==7 and profile['Policy']=='SuppliedAcceptedLocomotionAndOfflinePeerDisconnect'
    x,z=-1.5,0.;target=[value(profile[k]) for k in ('TargetNativeXBits','TargetNativeZBits')]
    second=[value(profile[k]) for k in ('RetargetNativeXBits','RetargetNativeZBits')]
    radius=value(profile['ArrivalRadiusBits']);dt=f(profile['StepMicroseconds']/1e6)
    phase=distance_total=unwrapped=seconds_in_generation=0.;mode='Idle';generation=1;cycles=switches=0
    heading=[0.,0.,1.];rows=[];arrived=False
    for i in range(profile['MaximumUpdates']):
        stopping=arrived
        if i in (10,18,35):target=list(second)
        if i==15:target=None
        if i==20:x,z=[value(profile[k]) for k in ('CorrectionXBits','CorrectionZBits')]
        baseline=[x,z]
        speed=value(profile['RunSpeedBits' if i>=34 or 27<=i<29 else 'WalkSpeedBits']) if i>=25 else value(profile['SpeedBits'])
        manual={25:(0,-1),26:(0,-1),27:(1,-1),28:(1,0),30:(0,1),32:(1,0),33:(1,0)}.get(i)
        if manual is not None:manual=prior.camera_direction(profile,manual,i)
        if 25<=i<35:target=None
        paused=5<=i<8;dx,dz=(target[0]-x,target[1]-z) if target is not None else (0.,0.)
        distance=math.hypot(dx,dz);vx=vz=0.
        if manual is not None:
            length=max(1.,math.hypot(*manual));vx,vz=f(manual[0]/length*speed),f(manual[1]/length*speed)
        elif distance>radius and not paused:
            travel=min(speed*dt,distance-radius*.5)*(1-1/1048576)
            vx,vz=f(dx/distance*travel/dt),f(dz/distance*travel/dt)
            for _ in range(8):
                nx,nz=f(x+f(vx*dt)),f(z+f(vz*dt))
                if 0<=(nx-x)*dx+(nz-z)*dz<=distance*distance:break
                vx,vz=f(vx*.5),f(vz*.5)
            else:vx=vz=0.
        x,z=f(x+f(vx*dt)),f(z+f(vz*dt))
        delta=[f(x-baseline[0]),0.,f(z-baseline[1])]
        distance=f(math.sqrt(f(f(delta[0]*delta[0])+f(delta[2]*delta[2]))))
        next_mode='Idle' if distance<=1e-6 else 'Run' if distance/.01>=profile['LocomotionRunThreshold'] else 'Walk'
        clip=clips[('Idle','Walk','Run').index(next_mode)]
        increment=.01*clip['FrameRate']*clip['IdleTimeScale']/clip['EndFrame'] if next_mode=='Idle' else distance*clip['FrameRate']/(clip['AuthoredSpeed']*clip['EndFrame'])
        changed=next_mode!=mode
        previous_unwrapped=phase*clip['EndFrame'] if changed else unwrapped
        if changed:generation+=1;cycles=0;seconds_in_generation=0.;switches+=1
        cycles+=math.floor(phase+increment);phase=(phase+increment)%1
        frame=phase*clip['EndFrame'];unwrapped=cycles*clip['EndFrame']+frame;seconds_in_generation+=.01
        events=[]
        for cycle in range(max(0,math.floor(previous_unwrapped/clip['EndFrame'])-1),math.floor(unwrapped/clip['EndFrame'])+1):
            for group_index,group in enumerate(clip['Messages']):
                for occurrence_index,event_frame in enumerate(group['Frames']):
                    at=cycle*clip['EndFrame']+event_frame
                    if previous_unwrapped<at<=unwrapped:
                        events.append(dict(Cycle=cycle,GroupIndex=group_index,OccurrenceIndex=occurrence_index,Name=group['Name'],Frame=event_frame))
        events.sort(key=lambda e:(e['Cycle']*clip['EndFrame']+e['Frame'],0 if e['Frame']==clip['EndFrame'] else 1,e['GroupIndex'],e['OccurrenceIndex']))
        distance_total+=distance
        if distance>1e-6:heading=[f(delta[0]/distance),0.,f(delta[2]/distance)]
        mode=next_mode
        rows.append(dict(Index=i,NativeXZ=[x,z],Delta=delta,Distance=distance,Mode=mode,Frame=frame,NormalizedPhase=phase,AcceptedDistance=distance_total,Switches=switches,Heading=heading,Generation=generation,UnwrappedPhase=unwrapped,GenerationSeconds=seconds_in_generation,CompletedCycles=cycles,Events=events))
        arrived=target is not None and math.hypot(target[0]-x,target[1]-z)<=radius
        if stopping:assert arrived and distance==0;break
    else:raise AssertionError('No bounded arrival/zero step')
    assert [r['Index'] for r in rows if r['Mode']=='Idle']==[5,6,7,15,16,17,29,31,34,len(rows)-1]
    assert any(r['Mode']=='Run' for r in rows) and any(r['Events'] for r in rows)
    return dict(Version=1,SyntheticPolicy=True,NativeOutputUsed=False,Threshold=profile['LocomotionRunThreshold'],Clips=clips,Rows=rows,CorrectionRebasedAt=20,PositionTolerance=3e-6,PhaseTolerance=3e-5,HeadingTolerance=3e-5)

def generate(profile_path,clips_path,output):
    fixture=expected(json.loads(Path(profile_path).read_text()),json.loads(Path(clips_path).read_text()))
    Path(output).write_text(json.dumps(fixture,indent=2)+'\n')
    return dict(Updates=len(fixture['Rows']),Events=sum(len(r['Events']) for r in fixture['Rows']),Modes=sorted({r['Mode'] for r in fixture['Rows']}),NativeOutputUsed=False)

def phase_from_verified_positions(profile,clips,movement):
    """Accepted native positions are observer inputs; phase/event outputs never seed this check."""
    phase=distance_total=unwrapped=seconds_in_generation=0.;mode='Idle';generation=1;cycles=switches=0
    distance_error=phase_error=0.
    heading=[0.,0.,1.];x,z=-1.5,0.;rows=[]
    for i,motion in enumerate(movement):
        if i==profile.get('CorrectionAtStep',20):x,z=[value(profile[k]) for k in ('CorrectionXBits','CorrectionZBits')]
        nx,nz=f(motion['Native'][0]),f(motion['Native'][2]);dx,dz=f(nx-x),f(nz-z);x,z=nx,nz
        distance=f(math.sqrt(f(f(dx*dx)+f(dz*dz))))
        bits=struct.unpack('<I',struct.pack('<f',distance))[0]
        norm_error=0. if distance==0 else 2*(struct.unpack('<f',struct.pack('<I',bits+1))[0]-distance)
        distance_error+=norm_error
        next_mode='Idle' if distance<=1e-6 else 'Run' if distance/.01>=f(profile['LocomotionRunThreshold']) else 'Walk'
        clip=clips[('Idle','Walk','Run').index(next_mode)]
        increment=.01*clip['FrameRate']*clip['IdleTimeScale']/clip['EndFrame'] if next_mode=='Idle' else distance*clip['FrameRate']/(clip['AuthoredSpeed']*clip['EndFrame'])
        if next_mode!='Idle':phase_error+=norm_error*clip['FrameRate']/(clip['AuthoredSpeed']*clip['EndFrame'])
        changed=next_mode!=mode;start=phase*clip['EndFrame'] if changed else unwrapped
        if changed:generation+=1;cycles=0;seconds_in_generation=0.;switches+=1
        cycles+=math.floor(phase+increment);phase=(phase+increment)%1
        frame=phase*clip['EndFrame'];unwrapped=cycles*clip['EndFrame']+frame;seconds_in_generation+=.01
        events=[]
        for cy in range(max(0,math.floor(start/clip['EndFrame'])-1),math.floor(unwrapped/clip['EndFrame'])+1):
            for gi,group in enumerate(clip['Messages']):
                for oi,fr in enumerate(group['Frames']):
                    at=cy*clip['EndFrame']+fr
                    if start<at<=unwrapped:events.append(dict(Cycle=cy,GroupIndex=gi,OccurrenceIndex=oi,Name=group['Name'],Frame=fr))
        events.sort(key=lambda e:(e['Cycle']*clip['EndFrame']+e['Frame'],0 if e['Frame']==clip['EndFrame'] else 1,e['GroupIndex'],e['OccurrenceIndex']))
        distance_total+=distance
        if distance>1e-6:heading=[f(dx/distance),0.,f(dz/distance)]
        mode=next_mode
        rows.append(dict(Mode=mode,Generation=generation,Switches=switches,Frame=frame,NormalizedPhase=phase,AcceptedDistance=distance_total,
            Heading=heading,UnwrappedPhase=unwrapped,GenerationSeconds=seconds_in_generation,CompletedCycles=cycles,Events=events,
            DistanceErrorBound=distance_error,PhaseErrorBound=phase_error,EndFrame=clip['EndFrame']))
    return rows

def verify_phase(fixture,text,profile,expected_updates=109):
    records=prior.records
    actual=records(text,'accepted_locomotion_state=');movement=records(text,'point_target_state=')
    wanted=fixture['Rows'];assert len(actual)==len(movement)==len(wanted)==expected_updates
    event_total=0
    for want,row,motion in zip(wanted,actual,movement):
        i=want['Index'];assert row['Index']==motion['Index']==i and row['Observations']==i+1
        assert row['Mode']==want['Mode'] and row['Generation']==want['Generation'] and row['Switches']==want['Switches']
        end=fixture['Clips'][('Idle','Walk','Run').index(want['Mode'])]['EndFrame']
        for key in ('Frame','NormalizedPhase','AcceptedDistance','UnwrappedPhase','GenerationSeconds'):
            tolerance=2e-13 if key=='GenerationSeconds' else fixture['PhaseTolerance']*(end if key in ('Frame','UnwrappedPhase') else 1)
            assert abs(row[key]-want[key])<=tolerance,(i,key,row[key],want[key])
        assert abs(row['Frame']/end-row['NormalizedPhase'])<=2e-13
        assert row['CompletedCycles']==want['CompletedCycles'] and row['Events']==want['Events']
        event_total+=len(row['Events'])
        assert max(abs(a-b) for a,b in zip(row['Heading'],want['Heading']))<=fixture['HeadingTolerance']
        assert max(abs(a-b) for a,b in zip((motion['Native'][0],motion['Native'][2]),want['NativeXZ']))<=fixture['PositionTolerance']
        assert row['TimestampMicroseconds']==motion['TimestampMicroseconds']
        assert row['Current'] and row['ActualDriver'] and row['ActualLocalRow'] and row['NoSecondElapsedClock']
        assert row['PresentedFrame']==row['Frame'] and row['PresentedClip']==row['Mode'].lower()
        assert row['OriginalAssetRecords'] and row['ModernScheduling'] and not row['RootMotionApplied']
        if i:assert row['MotionRevision']==actual[i-1]['MotionRevision']+(2 if i==20 else 1)
    # Position rows passed an independent before-native trajectory comparison above.
    # Use these verified physical inputs for a tight observer-boundary check. Positive
    # float dot/sqrt implementations may differ by two ULPs; propagate that bound
    # mechanically rather than seed a native norm or choose tolerance from output.
    exact=phase_from_verified_positions(profile,fixture['Clips'],movement)
    for i,(row,want) in enumerate(zip(actual,exact)):
        for key,value_expected in want.items():
            if key in ('DistanceErrorBound','PhaseErrorBound','EndFrame'):continue
            if key=='Heading':assert max(abs(f(a)-b) for a,b in zip(row[key],value_expected))<=2e-7,(i,key,row[key],value_expected)
            elif isinstance(value_expected,float):
                tolerance=want['DistanceErrorBound'] if key=='AcceptedDistance' else want['PhaseErrorBound']*(want['EndFrame'] if key in ('Frame','UnwrappedPhase') else 1) if key in ('Frame','UnwrappedPhase','NormalizedPhase') else 0.
                assert abs(row[key]-value_expected)<=tolerance+2e-13,(i,key,row[key],value_expected,tolerance)
            else:assert row[key]==value_expected,(i,key,row[key],value_expected)
    rebase,=records(text,'accepted_locomotion_rebase=')
    assert rebase['AtStep']==20 and rebase['Observations']==20 and rebase['TimestampMicroseconds']==actual[19]['TimestampMicroseconds']
    assert rebase['PhasePreserved'] and rebase['EventsPreserved'] and rebase['ActualAuthoritativeRow']
    assert rebase['Frame']==actual[19]['Frame'] and rebase['AcceptedDistance']==actual[19]['AcceptedDistance'] and rebase['Generation']==actual[19]['Generation']
    assert rebase['MotionRevision']==actual[19]['MotionRevision']+1
    assert max(abs(a-b) for a,b in zip((rebase['Baseline'][0],rebase['Baseline'][2]),(-.5,1.)))<=fixture['PositionTolerance']
    ready,=records(text,'accepted_locomotion_ready=')
    for mode,clip in zip(('Idle','Walk','Run'),fixture['Clips']):
        assert ready[mode]==clip['Path'] and ready[mode+'Sha256']==clip['Sha256']
    assert abs(ready['RunThreshold']-fixture['Threshold'])<2e-7 and ready['StepMicroseconds']==10000
    assert ready['ActualOwnedPointDriver'] and ready['OriginalAssetRecords'] and ready['ModernScheduling'] and not ready['RootMotionApplied']
    cleanup,=records(text,'accepted_locomotion_cleanup=')
    assert cleanup==dict(Disposed=True,Observations=expected_updates,Events=event_total,Rebases=1,AcceptedDistance=actual[-1]['AcceptedDistance'],
        BorrowedDriverPreserved=True,BorrowedWorldPreserved=True,BorrowedStorePreserved=True,HistoricStatePreserved=True,
        WorldAlreadyClosedByHost=profile['InvalidDisconnectConnection'])
    assert not fixture['NativeOutputUsed'] and event_total==(2 if expected_updates==109 else sum(len(row["Events"]) for row in wanted))
    return dict(AcceptedLocomotionUpdates=expected_updates,OriginalFootstepOccurrences=event_total,CorrectionRebases=1,TeleportDistanceExcluded=True,
        NoSecondElapsedClock=True,ExpectedTrajectoryAndPhaseBeforeNative=True,IndependentPhaseFromVerifiedAcceptedPose=True,
        NoNativePhaseSeededExpected=True,FrameToleranceScalesWithClipLength=True,FloatNormUlpAllowance=2,
        MaximumBoundaryPhaseErrorBound=max(r['PhaseErrorBound'] for r in exact),ModernRunThreshold=fixture['Threshold'],RootMotionApplied=False)

def verify_join(profile,terrain,wire,material_expected,fixture,text,negative=False):
    assert profile['Version']==7 and profile['InvalidDisconnectConnection']==negative
    result=verify_phase(fixture,text,profile)
    packet=struct.pack('>HIH',5,profile['SoeConnectionId']^(1 if negative else 0),6)
    framed=prior.soe.frame(packet).hex().upper();assert profile['SoeDisconnectHex']==framed
    if not negative:
        old=dict(profile,Version=6,Policy='SuppliedCameraRelativeOrbitAndOfflineSoeIngress')
        result.update(prior.verify(old,terrain,wire,material_expected,text))
        assert prior.records(text,'soe_peer_disconnect=')==[dict(Hex=framed,AtStep=109,PeerDisconnected=True,TransportClosed=True,QueuedApplications=0,Controls=0,
            BorrowedWorldPreserved=True,BorrowedPumpPreserved=True,BorrowedQueuePreserved=True,BridgeDeclinedBeforeTake=True,PendingMessages=0,RetainedPackets=0,
            CompletedUpdates=109,Sent=False,OriginalCloseReasonSemantics=False)]
        assert not prior.records(text,'soe_peer_disconnect_failure=')
        assert prior.records(text,'accepted_locomotion_visual_cleanup=')==[dict(Retired=True,OwnedPresentationDrained=True,OwnsObserver=False)]
        result.update(PeerDisconnected=True,TransportClosedBeforeCallerSceneTeardown=True)
    else:
        assert 'Disconnect connection ID mismatch.' in text and not prior.records(text,'soe_peer_disconnect=')
        assert prior.records(text,'soe_peer_disconnect_failure=')==[dict(Hex=framed,AtStep=109,TransportEstablished=True,IngressFaulted=True,
            BorrowedWorldPreserved=True,BorrowedPumpPreserved=True,BorrowedQueuePreserved=True,CompletedUpdates=109,Sent=False)]
        assert len(prior.records(text,'soe_root_datagram='))==9
        assert prior.records(text,'soe_application_ingress_cleanup=')==[dict(Disposed=True,IsFaulted=True,ApplicationsQueued=2,ControlsTransferred=2,
            BorrowedPumpPreserved=True,BorrowedTransportPreserved=True,Sent=False)]
        assert prior.records(text,'soe_root_bridge_cleanup=')==[dict(Disposed=True,OwnedPendingBefore=0,OwnedPendingAfter=0,Accepted=9,BorrowedQueueCount=0,
            BorrowedQueuePreserved=True,BorrowedTransportPreserved=True,Sent=False)]
        assert prior.records(text,'soe_root_transport_cleanup=')==[dict(Closed=True,Datagrams=9,PendingMessages=0,RetainedPackets=0,OwnsSyntheticTransport=True,SocketCreated=False,Sent=False)]
        assert prior.records(text,'manual_outbound_queue_cleanup=')==[dict(Disposed=True,PendingBefore=0,QueuedBytesBefore=0,Count=0,QueuedBytes=0,Transferred=9,
            OwnsBorrowedWorld=False,OwnsBorrowedStore=False,WorldAlreadyClosed=True,Sent=False)]
        assert prior.records(text,'point_target_closed_world_cleanup=')==[dict(Disposed=True,Updates=109,DriverInactive=True,WorldAlreadyClosedByPump=True,BorrowedTerrainPreserved=True)]
        assert prior.records(text,'supplied_terrain_cleanup=')==[dict(Allocated=16,Disposed=16,Drained=True)]
        assert prior.records(text,'terrain_supplied_material_cleanup=')==[dict(Allocated=1,Disposed=1,Drained=True,Reads=2)]
        failure,=prior.records(text,'pump_failure_cleanup=')
        assert failure['calls']==failure['closed']==3 and failure['drained'] and failure['actors']==0
        for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','PASS: pumped point_target='):assert bad not in text
        result.update(PeerDisconnected=False,MismatchedConnectionRejected=True,CompletedMotionPrefix=109,HistoricalOutboundDatagrams=9,
            IngressTerminalFault=True,TransportPreservedUntilOwnerClose=True,AllOwnersDrained=True,RollbackClaimed=False)
    result.update(Passed=True,Sent=False)
    return result

if __name__=='__main__':
    if sys.argv[1]=='generate':
        assert len(sys.argv)==5
        print(json.dumps(generate(*sys.argv[2:])))
    else:
        assert len(sys.argv)==9 and sys.argv[1] in ('verify','verify-negative')
        profile,terrain,wire,material_expected,fixture,log,out=sys.argv[2:]
        inputs=[json.loads(Path(p).read_text()) for p in (profile,terrain,wire,material_expected,fixture)]
        result=verify_join(*inputs,Path(log).read_text(encoding='utf-8-sig'),negative=sys.argv[1]=='verify-negative')
        output=Path(out);assert not output.exists() and output.resolve().is_relative_to((ROOT/'local-private').resolve())
        output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
