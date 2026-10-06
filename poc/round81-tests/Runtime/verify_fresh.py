"""Importable independent fresh-state/reset verifier. Expected policy precedes native logs."""
import argparse,json,math,runpy
from pathlib import Path
ZERO=['CompletedUpdates','AttemptedUpdates','PointIndex','Speed','ManualHeld','RunHeld','ManualX','ManualZ','ClickAttempts','FramePlans','ClampedMicroseconds','FrameSteps','ReservedSteps','RemainderMicroseconds','DroppedMicroseconds','OrbitAccepted','OrbitHandled','RootSamples','RootQueued','RootDrained','RootNextSequence','RootRelativeMicroseconds','QueueCount','QueueBytes','OutboundStepCount','PendingMessages','RetainedPackets','RunDatagrams']
FALSE=['Running','Paused','FinishRequested','TargetActive','SocketCreated']
TRUE=['OwnedPoint','OwnedController','OwnedRoots','OwnedTransport']
def expected(profile):
    assert profile['Version']==13 and profile['Policy']=='SuppliedConfiguredOfflinePlayground'
    settings=profile['PlaygroundSettings'];assert settings['Version']==1 and 1<=settings['MaximumUpdates']<=6000 and 1<=settings['MaximumClickAttempts']<=128
    result={key:0 for key in ZERO};result.update({key:False for key in FALSE});result.update({key:True for key in TRUE})
    result.update(SchemaVersion=1,Mode='Idle',TransportState='Established',ClickRemaining=settings['MaximumClickAttempts'],RootLastNowMilliseconds=-1,
        OrbitYaw=math.remainder(profile['OrbitYaw'],2*math.pi),OrbitPitch=min(max(profile['OrbitPitch'],profile['OrbitMinimumPitch']),profile['OrbitMaximumPitch']),OrbitDistance=min(max(profile['OrbitDistance'],profile['OrbitMinimumDistance']),profile['OrbitMaximumDistance']),OrbitSeedYaw=profile['OrbitYaw'],OrbitSeedPitch=profile['OrbitPitch'],OrbitSeedDistance=profile['OrbitDistance'])
    return result
def records(log):
    rows=[]
    for line in log.splitlines():
        if '=' not in line:continue
        key,value=line.split('=',1)
        try:rows.append((key.strip(),json.loads(value)))
        except json.JSONDecodeError:continue
    return rows
def verify_runs(logs,profiles,require_distinct_instances=False):
    assert len(logs)==len(profiles)>0;ids=[];evidence=[]
    for log,profile in zip(logs,profiles):
        rows=records(log);fresh=[r for k,r in rows if k=='offline_fresh_preview_state'];assert len(fresh)==1
        state=fresh[0];want=expected(profile)
        for key,value in want.items():assert type(state[key])==type(value) or type(value) in (int,float) and type(state[key]) in (int,float),(key,state[key],value)
        for key,value in want.items():assert state[key]==value,(key,state[key],value)
        assert all(type(state[k]) is int for k in ['LocalClock','OfflineStartClock','DriverClock','PreviewInstanceId','CameraInstanceId','SceneGeneration'])
        assert state['LocalClock']==state['OfflineStartClock']==state['DriverClock'] and state['LocalClock']>=0
        assert state['PreviewInstanceId']>0 and state['CameraInstanceId']>0 and state['SceneGeneration']>0
        ids.append((state['PreviewInstanceId'],state['CameraInstanceId']))
        summary=[r for k,r in rows if k=='offline_completion_summary'];assert len(summary)==1 and summary[0]['Reason'] in ['BudgetCompleted','FinishRequested','Faulted']
        keys={k for k,r in rows}
        for key in ['configured_session_owner_cleanup','configured_root_owner_cleanup','manual_outbound_queue_cleanup','manual_input_host_cleanup','run_input_host_cleanup','camera_orbit_cleanup']:assert key in keys, key
        queue=next(r for k,r in rows if k=='manual_outbound_queue_cleanup');assert queue['Count']==queue['QueuedBytes']==0
        controls=next(r for k,r in rows if k=='configured_session_owner_cleanup');assert controls['Disposed'] and controls['OwnedPointDisposed'] and controls['OwnedControllerDisposed']
        roots=next(r for k,r in rows if k=='configured_root_owner_cleanup');assert roots['Disposed']
        for key in ['manual_input_host_cleanup','run_input_host_cleanup','camera_orbit_cleanup']:
            assert next(r for k,r in rows if k==key)['Closed'],key
        assert roots['PendingMessages']==roots['RetainedPackets']==0
        manual=[r for k,r in rows if k=='manual_input_intent'];run=[r for k,r in rows if k=='movement_speed_intent'];orbit=[r for k,r in rows if k=='camera_orbit_state' and r['AtStep']>=0]
        evidence.append(dict(Reason=summary[0]['Reason'],Completed=summary[0]['CompletedUpdates'],Attempted=summary[0]['AttemptedUpdates'],ManualEventRecords=len(manual),RunEventRecords=len(run),PerturbedOrbitRecords=len(orbit),CleanupObserved=True))
    if require_distinct_instances:
        assert len({preview for preview,camera in ids})==len(ids),'Reused preview instance in same-process runs'
        assert len({camera for preview,camera in ids})==len(ids),'Reused camera instance in same-process runs'
    return dict(Passed=True,Runs=len(logs),FreshStates=len(logs),CleanupVerified=True,DistinctInstancesRequired=require_distinct_instances,InputAndCameraEvidence=evidence)
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['generate','verify']);p.add_argument('--profile',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--log',type=Path);a=p.parse_args();profile=json.loads(a.profile.read_text());repo=Path(__file__).resolve().parents[3];a.out=runpy.run_path(str(repo/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,repo)
    value=expected(profile) if a.mode=='generate' else verify_runs([a.log.read_text(encoding='utf-8-sig')],[profile])
    a.out.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8');print(json.dumps(value))
if __name__=='__main__':main()
