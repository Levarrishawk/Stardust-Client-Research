"""Independent camera-ray hit, binary32 point trajectory, full correction and ownership oracle."""
import importlib.util,json,math,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def module(name,path):
    s=importlib.util.spec_from_file_location(name,ROOT/path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
ray=module('point67_ray','poc/round63-tests/Integration/ray_peer.py')
terrain_peer=module('point67_set','poc/round62-tests/Integration/composition_peer.py')
material=module('point67_material','poc/round64-tests/Integration/material_peer.py')
records=terrain_peer.records
def f(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def value(bits):return struct.unpack('<f',struct.pack('<I',bits))[0]
def verify(profile,terrain,wire,material_expected,text):
    assert profile['Version']==3 and profile['Policy']=='SuppliedViewportPointTargetClearAndOfflineRootMotion' and not profile['MalformedCorrection'] and not profile['InvalidHostedClock'] and not profile['OriginalMovementParity']
    geometries=ray.surface.fixture_geometry(terrain)
    # Check the admitted render policy before projecting its label for the unchanged geometry oracle.
    patches=records(text,'supplied_terrain_patch=');assert len(patches)==16 and all(p['RenderPolicy']=='ModernSuppliedMainTexture' for p in patches)
    projected=[]
    for line in text.splitlines():
        if line.startswith('supplied_terrain_patch='):
            row=json.loads(line.split('=',1)[1]);row['RenderPolicy']='ModernLitVertexColor';line='supplied_terrain_patch='+json.dumps(row)
        projected.append(line)
    result=terrain_peer.terrain_host(terrain,'\n'.join(projected))
    fixed=records(text,'modern_set_ray_query_samples=');assert len(fixed)==1
    assert [{k:r[k] for k in ('Origin','Direction','Minimum','Maximum')} for r in fixed[0]]==ray.rays()
    result['FixedRays']=ray.compare(geometries,fixed[0])
    camera=records(text,'point_camera_pick=');assert len(camera)==3;c=camera[0]
    assert c['ActualInputEvent'] and c['ActualReferences'] and c['MarkerBuilds']==1 and c['Owned']==3 and c['Revision']==c['Attempt']==1
    assert all(0<=p<s for p,s in zip(c['Pixel'],c['Viewport']))
    origin=list(map(f,c['EngineOrigin']));direction=list(map(f,c['EngineDirection']))
    r=c['Native'];assert r['Origin']==[origin[0],origin[1],-origin[2]]
    assert r['Direction']==[direction[0],direction[1],-direction[2]]
    camera_result=ray.compare(geometries,[r]);hit=ray.expected(geometries,r);assert hit is not None
    # Target values come from independently intersecting the captured camera ray, never its native hit result.
    target=[f(hit['Position'][0]),f(hit['Position'][2])]
    supplied=[value(profile[k]) for k in ('TargetNativeXBits','TargetNativeZBits')]
    assert all(abs(a-b)<2e-5 for a,b in zip(target,supplied))
    ready=records(text,'point_target_ready=');assert len(ready)==1;start=ready[0]
    assert start['Start'][0]==-1.5 and start['Start'][2]==0 and list(map(f,start['Target']))==target and start['ActualTarget']
    speed=value(profile['SpeedBits']);radius=value(profile['ArrivalRadiusBits']);dt=f(profile['StepMicroseconds']/1e6)
    assert f(start['Speed'])==speed and f(start['Radius'])==radius and start['MaximumUpdates']==128
    retarget=camera[1];assert retarget['Revision']==retarget['Attempt']==2 and retarget['MarkerBuilds']==1 and retarget['Owned']==3
    camera_result=ray.compare(geometries,[c['Native'],retarget['Native']]);second_hit=ray.expected(geometries,retarget['Native']);assert second_hit is not None
    second_target=[f(second_hit['Position'][0]),f(second_hit['Position'][2])]
    assert all(abs(a-value(profile[k]))<2e-5 for a,k in zip(second_target,('RetargetNativeXBits','RetargetNativeZBits')))
    for pick in camera:
        o=list(map(f,pick['EngineOrigin']));d=list(map(f,pick['EngineDirection']))
        assert pick['Native']['Origin']==[o[0],o[1],-o[2]] and pick['Native']['Direction']==[d[0],d[1],-d[2]]
        assert pick['ActualInputEvent'] and pick['ActualReferences']
    third_hit=ray.expected(geometries,camera[2]['Native']);assert third_hit is not None
    third_target=[f(third_hit['Position'][0]),f(third_hit['Position'][2])]
    camera_result=ray.compare(geometries,[c['Native'] for c in camera])
    assert camera[2]['Revision']==camera[2]['Attempt']==3 and camera[2]['MarkerBuilds']==1 and camera[2]['Owned']==3
    assert all(abs(a-value(profile[k]))<2e-5 for a,k in zip(third_target,('RetargetNativeXBits','RetargetNativeZBits')))
    inputs=records(text,'point_input_target=');assert len(inputs)==3
    for i,(r,t) in enumerate(zip(inputs,(target,second_target,third_target))):
        assert r['AtStep']==(0,10,18)[i] and r['Attempt']==r['TargetRevision']==i+1
        assert r['PickerAttemptsBefore']==i and r['PickerAttemptsAfter']==i+1 and list(map(f,r['Target']))==t
        assert r['ActualPickTarget'] and r['Current'] and not r['PriorCurrent'] and not r['Paused'] and r['Failure'] is None
    activity=records(text,'point_activity_intent=');assert len(activity)==2
    for r,at,paused in zip(activity,(5,8),(True,False)):
        assert r['AtStep']==at and r['Paused']==paused and r['MotionPreserved'] and r['DriverStatePreserved'] and r['TargetCurrent']
    hosted=records(text,'point_viewport_input=');assert len(hosted)==3
    for i,(r,t) in enumerate(zip(hosted,inputs)):
        assert r['AtStep']==t['AtStep'] and r['ClockReadCount']==r['AcceptedCount']==r['HandledCount']==i+1
        assert r['ActualViewportEvent'] and r['ActualAdapterResult'] and r['Clock']==t['TimestampMicroseconds'] and not r['Sent']
    clear=records(text,'point_target_cleared=');assert len(clear)==1
    assert clear[0]==dict(AtStep=15,TimestampMicroseconds=start['TimestampMicroseconds']+150000,PreviousRevision=2,SuccessfulTargets=2,TargetNull=True,PreviousCurrent=False,DriverStatePreserved=True,MotionPreserved=True,Paused=False,OriginalMovementParity=False)
    outgoing=records(text,'point_outbound_root=');expected_outgoing=[]
    basis=[1.,0.,0.,0.,1.,0.,0.,0.,1.]
    x,z=-1.5,0.;timestamp=start['TimestampMicroseconds'];rows=records(text,'point_target_state=')
    assert 20<len(rows)<=128
    correction=records(text,'point_queued_correction_ready=');assert len(correction)==1;correct=correction[0]
    obj=struct.unpack_from('<Q',bytes.fromhex(wire['human_create']),6)[0]
    cx,cz=[value(profile[k]) for k in ('CorrectionXBits','CorrectionZBits')]
    cy=f(ray.surface.expected(geometries,cx,cz)['Height'])
    packet=struct.pack('<HIIIQII',5,0x80ce5e46,0x1B,0x71,obj,0,profile['CorrectionCounter'])+struct.pack('<8f',0,0,0,1,cx,cz,cy,0)
    canonical_correct=dict(correct);canonical_correct['Native']=list(map(f,correct['Native']))
    assert canonical_correct==dict(AtStep=20,Hex=packet.hex().upper(),Native=[cx,cy,cz],Counter=profile['CorrectionCounter'],Applied=True,DuplicateRejected=True,ActualMotion=True,AcceptedMessages=1,DroppedMessages=1,QueuedMessages=0,QueuedBytes=0,TimestampMicroseconds=timestamp+20*profile['StepMicroseconds'],DriverUpdates=20,Sent=False,OriginalMovementParity=False)
    queued=records(text,'point_correction_queued=');assert queued==[dict(AtStep=20,Hex=packet.hex().upper(),Messages=2,Bytes=124,CallerBytesCleared=True,Sent=False,OriginalMovementParity=False)]
    assert not records(text,'point_full_correction=')
    visited=set();arrived=False
    for i,row in enumerate(rows):
        assert row['Index']==i and row['StopStep']==arrived
        if i==10:target=second_target
        if i==15:target=None
        if i==18:target=third_target
        if i==20:x,z=cx,cz
        paused=5<=i<8
        dx,dz=(target[0]-x,target[1]-z) if target is not None else (0.,0.);distance=math.hypot(dx,dz);vx=vz=0.
        if distance>radius and not paused:
            travel=min(speed*dt,distance-radius*.5)*(1-1/1048576)
            vx,vz=f(dx/distance*travel/dt),f(dz/distance*travel/dt)
            for _ in range(8):
                nx,nz=f(x+f(vx*dt)),f(z+f(vz*dt))
                if 0<=(nx-x)*dx+(nz-z)*dz<=distance*distance:break
                vx,vz=f(vx*.5),f(vz*.5)
            else:vx=vz=0.
        nx,nz=f(x+f(vx*dt)),f(z+f(vz*dt));assert (nx-x)*dx+(nz-z)*dz<=distance*distance
        x,z=nx,nz;ground=ray.surface.expected(geometries,x,z);assert ground is not None
        y=f(ground['Height']);visited.add((ground['ChunkIndex'],ground['PatchIndex']));timestamp+=profile['StepMicroseconds']
        want_speed=f(math.sqrt(f(f(vx*vx)+f(vz*vz))))
        assert all(math.isclose(a,b,abs_tol=2e-6) for a,b in zip(row['Native'],[x,y,z])),(i,row,[x,y,z])
        assert all(math.isclose(a,b,abs_tol=2e-6) for a,b in zip(row['Engine'],[x,y,-z]))
        assert math.isclose(row['Speed'],want_speed,abs_tol=2e-6)
        assert row['AttemptedUpdates']==row['CompletedUpdates']==i+1 and row['TimestampMicroseconds']==timestamp
        assert row['StepsThisUpdate']==1 and row['DroppedMicroseconds']==row['SuspendedMicroseconds']==0 and row['FaultType'] is None
        assert row['PublicationRetained'] and row['VisualRetained'] and row['calls']==row['closed']==3
        if want_speed>0:
            sin,cos=f(vx/want_speed),f(vz/want_speed);basis=[cos,0.,sin,0.,1.,0.,-sin,0.,cos]
        if i in (4,6,16,21) or row['StopStep']:
            expected_outgoing.append((i,[x,y,z],want_speed,list(basis)))
        arrived=target is not None and math.hypot(target[0]-x,target[1]-z)<=radius
        assert row['Mode']==('Paused' if paused else 'Idle' if target is None else 'Arrived' if arrived else 'Moving')
        if i in (5,8):assert activity[0 if i==5 else 1]['TimestampMicroseconds']==timestamp-profile['StepMicroseconds']
        if i in (10,18):assert inputs[1 if i==10 else 2]['TimestampMicroseconds']==timestamp-profile['StepMicroseconds']
    assert len(outgoing)==len(expected_outgoing)==5
    for packet,(index,position,expected_speed,expected_basis) in zip(outgoing,expected_outgoing):
        frame=bytes.fromhex(packet['Hex']);assert len(frame)==62 and packet['AtStep']==index
        flags=profile['OutboundFlags'];counter=profile['OutboundCounterBase']+index;stamp=profile['OutboundTimestampBase']+index
        assert frame[:30]==struct.pack('<HIIIQII',5,0x80ce5e46,flags,0x71,obj,stamp,counter)
        qx,qy,qz,qw,px,pz,py,ps=struct.unpack_from('<8f',frame,30)
        assert all(math.isfinite(v) for v in (qx,qy,qz,qw,px,pz,py,ps))
        assert all(math.isclose(a,b,abs_tol=2e-6) for a,b in zip((px,py,pz),position)) and math.isclose(ps,expected_speed,abs_tol=2e-6)
        norm=math.hypot(qx,qy,qz,qw);assert abs(norm-1)<2e-7
        qx,qy,qz,qw=[v/norm for v in (qx,qy,qz,qw)]
        matrix=[1-2*(qy*qy+qz*qz),2*(qx*qy-qz*qw),2*(qx*qz+qy*qw),2*(qx*qy+qz*qw),1-2*(qx*qx+qz*qz),2*(qy*qz-qx*qw),2*(qx*qz-qy*qw),2*(qy*qz+qx*qw),1-2*(qx*qx+qy*qy)]
        assert max(abs(a-b) for a,b in zip(matrix,expected_basis))<2e-6,(index,matrix,expected_basis)
        assert packet['Counter']==counter and packet['Timestamp']==stamp and packet['ActualCurrentLocalRow'] and packet['CallerOwnedBytes'] and not packet['Sent'] and not packet['OriginalSenderParity']
        assert all(math.isclose(f(a),b,abs_tol=2e-6) for a,b in zip(packet['Native'],position)) and math.isclose(f(packet['Speed']),expected_speed,abs_tol=2e-6)
    assert [r['Index'] for r in rows if r['Mode']=='Idle']==[15,16,17]
    assert rows[-1]['StopStep'] and rows[-1]['Speed']==0 and arrived
    assert records(text,'point_target_cleanup=')==[dict(Disposed=True,Updates=len(rows),BorrowedWorldPreserved=True,BorrowedStorePreserved=True,BorrowedTerrainPreserved=True)]
    assert records(text,'point_camera_cleanup=')==[dict(Disposed=True,Owned=0,BorrowedCameraPreserved=True,BorrowedTerrainPreserved=True)]
    assert records(text,'point_input_cleanup=')==[dict(Disposed=True,Attempts=3,OwnsBorrowers=False,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert records(text,'point_input_host_cleanup=')==[dict(Closed=True,ClockReadCount=3,AcceptedCount=3,HandledCount=3,IsFaulted=False,OutboundPackets=5,BorrowedAdapterPreserved=True,BorrowedPickerPreserved=True,BorrowedControllerPreserved=True)]
    assert 'PASS: pumped point_target=' in text
    result.update(PointUpdates=len(rows),CameraPick=camera_result,VisitedPatches=len(visited),VisitedChunks=len({a for a,b in visited}),
        ActualViewportInputs=3,ClearIdleTicks=3,OfflineRootPackets=5,IndependentRootBasisAndWireLayout=True,OriginalSenderParity=False,InputRetarget=True,ActivePausedTicks=3,QueuedOwnedCorrection=True,NoncollinearFullRootCorrection=True,DuplicatePreserved=True,Stopped=True,OwnershipDrained=True,OriginalMovementParity=False)
    result['Material']=material.verify(material_expected,text)
    return result
if __name__=='__main__':
    profile,terrain,wire,material_path,log,out=sys.argv[1:7]
    result=verify(*[json.loads(Path(p).read_text()) for p in (profile,terrain,wire,material_path)],Path(log).read_text(encoding='utf-8-sig'))
    p=Path(out);assert not p.exists() and p.resolve().is_relative_to((ROOT/'local-private').resolve());p.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
