"""Independent before-native policy and actual manifest-entry prefix verification."""
import argparse, hashlib, importlib.util, json, math, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]

def module(name,path):
    spec=importlib.util.spec_from_file_location(name,ROOT/path)
    value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def fresh(path):
    path=path.resolve()
    if path.exists() or not path.is_relative_to((ROOT/'local-private').resolve()):raise ValueError('Fresh private output required')
    path.parent.mkdir(parents=True,exist_ok=True);return path

def paths(scenario):
    obj=json.loads(scenario.read_text(encoding='utf-8-sig'))
    resolve=lambda p:Path(p) if Path(p).is_absolute() else scenario.parent/p
    return obj,[resolve(p).resolve() for p in obj['PointTargetProfiles']]

def prepare(a):
    obj,files=paths(a.scenario)
    assert obj['Schema']==1 and obj['RunCount']==2 and len(files)==2
    profiles=[json.loads(p.read_text()) for p in files]
    assert all(p['InteractiveReplay']==a.close for p in profiles)
    assert all(p['MaximumUpdates']==(6000 if a.close else 3) for p in profiles)
    model=json.loads(a.trajectory.read_text());assert not model['NativeOutputUsed']
    receipt=dict(ScenarioSha256=digest(a.scenario),Profiles=[dict(Path=str(p),Sha256=digest(p)) for p in files],
        TrajectorySha256=digest(a.trajectory),CloseSignal=a.close,NativeOutputUsed=False,
        ExpectedReason='Incomplete' if a.close else 'BudgetCompleted',MinimumCompleted=30 if a.close else 3,
        MaximumCompleted=37 if a.close else 3,ExpectedRuns=1 if a.close else 2)
    fresh(a.out).write_text(json.dumps(receipt,indent=2)+'\n');print('Pre-native scenario policy prepared')

def verify(a):
    plan=json.loads(a.expected.read_text());assert not plan['NativeOutputUsed']
    assert a.expected.stat().st_mtime<a.log.stat().st_mtime
    assert digest(a.scenario)==plan['ScenarioSha256'] and digest(a.trajectory)==plan['TrajectorySha256']
    obj,files=paths(a.scenario);profiles=[json.loads(p.read_text()) for p in files]
    assert [dict(Path=str(p),Sha256=digest(p)) for p in files]==plan['Profiles']
    net=module('scenario82_net','poc/round81-tests/Networking/transport_fixtures.py')
    fresh_verify=module('scenario82_fresh','poc/round81-tests/Runtime/verify_fresh.py')
    text=a.log.read_text(encoding='utf-8-sig');ends=net.records(text,'round82_run_end')
    assert len(ends)==plan['ExpectedRuns'] and net.records(text,'round82_run_begin')==list(range(1,len(ends)+1))
    segments=[];checks=[];fixture=json.loads(a.trajectory.read_text())
    for i,end in enumerate(ends):
        start=text.index('round82_run_begin='+str(i+1));stop=text.index('round82_run_end=',start)
        segment=text[start:stop];segments.append(segment);r=end['Report'];n=r['CompletedUpdates']
        assert end['Run']==i+1 and r['Reason']==plan['ExpectedReason'] and plan['MinimumCompleted']<=n<=plan['MaximumCompleted']
        assert r['AttemptedUpdates']==n and r['CleanupSucceeded'] and r['ReadyReached']
        assert r['Actors']==r['QueuedPublications']==r['PendingPublications']==r['OutstandingArchiveJobs']==r['QueueCount']==r['PendingMessages']==r['RetainedPackets']==0
        assert r['ArchiveCalls']==r['ArchiveClosed']==3
        moves=net.records(segment,'point_target_state');assert len(moves)==n
        for j,row in enumerate(moves):
            want=fixture['Movement'][j];assert row['Index']==j and row['CompletedUpdates']==row['AttemptedUpdates']==j+1
            assert row['Mode']==want['Mode'] and all(math.isclose(x,y,abs_tol=5e-6,rel_tol=0) for x,y in zip(row['Native'],want['Native']))
            assert len(row['Native'])==3 and math.isclose(row['Speed'],want['Speed'],abs_tol=2e-6,rel_tol=0)
        h,=net.records(segment,'soe_root_transport_ready');assert h==dict(net.handshakes(profiles[i]),Established=True,Synthetic=True,SocketCreated=False,Sent=False,OriginalSenderParity=False)
        if not plan['CloseSignal']:assert net.records(segment,'point_outbound_root')==[]
        checks.append(dict(Run=i+1,CompletedUpdates=n,ProtectedRoots=r['RootFrames']))
    if not plan['CloseSignal']:
        runtime=dict(Reason='BudgetCompleted',MaximumUpdates=3,CompletedUpdates=3,AttemptedUpdates=3,Stopped=True,Faulted=False,Mode='Idle',Speed=0,TargetActive=False)
        transport=net.verify_transport_only(segments,profiles,[runtime,runtime],[[],[]])
        resets=fresh_verify.verify_runs(segments,profiles,require_distinct_instances=True)
        s,=net.records(text,'round82_scenario_summary');assert s['Passed'] and s['Runs']==2 and s['CompletedUpdates']==6 and s['ProtectedRoots']==0 and s['FreshPreviewsRetired']==2 and s['NativeLauncher'] and s['NoSocket'] and not s['PhysicalOsEvent']
    else:
        r=ends[0]['Report'];assert r['WindowClosing'] and not r['ValidOutcome'] and not r['Restartable']
        s,=net.records(text,'round82_close_summary');assert s['Passed'] and s['Completed']==1 and s['NoSuccessor'] and s['SyntheticGodotWindowSignal'] and not s['PhysicalOsEvent']
        signal,=net.records(text,'round82_close_signal');assert signal==dict(SyntheticGodotWindowSignal=True,PhysicalOsEvent=False)
        raws=net.records(text,'point_outbound_root');frames=net.records(text,'soe_root_datagram');assert len(raws)==len(frames)==r['RootFrames']==3
        for sequence,(raw,wire) in enumerate(zip(raws,frames)):
            app=bytes.fromhex(raw['Hex']);codec=net.encoder(profiles[0]['SoeSeed'])
            assert len(app)==62 and wire['Sequence']==sequence and wire['Hex']==codec.frame(struct.pack('>HH',9,sequence)+app).hex().upper()
            assert wire['AckHex']==codec.frame(struct.pack('>HH',0x15,sequence)).hex().upper() and wire['AckReleased']==1 and wire['PendingMessages']==wire['RetainedPackets']==0
            want=fixture['Movement'][raw['AtStep']];values=struct.unpack_from('<8f',app,30)
            assert all(math.isclose(x,y,abs_tol=5e-6,rel_tol=0) for x,y in zip([values[4],values[6],values[5]],want['Native']))
            assert math.isclose(values[7],want['Speed'],abs_tol=2e-6,rel_tol=0)
        transport=dict(Passed=True,ProtectedRoots=3,ActualApplicationBorrowedSerializationInput=True,IndependentMovementPrefix=True)
        resets=dict(PhysicalOsEvent=False)
    assert all(x not in text for x in ('Input cleanup:','Terrain cleanup:','Exit cleanup:','resource still in use'))
    costs=net.records(text,'round82_reload_cost');assert len(costs)==len(ends)
    assert all(c['DiagnosticWallClock'] and c['IncludesSimulationAndDrain'] and
        0<c['ReadyMilliseconds']<=c['TotalMilliseconds'] and c['Run']==i+1 for i,c in enumerate(costs))
    statuses=net.records(text,'round82_inspection');assert len(statuses)==len(ends)
    for status,end in zip(statuses,ends):
        assert status['CompletedUpdates']==end['Report']['CompletedUpdates'] and status['AttemptedUpdates']==end['Report']['AttemptedUpdates']
        assert status['CleanupSucceeded'] is True and status['Actors']==status['OutstandingArchiveJobs']==0 and status['ChildPresent'] is False
    if not plan['CloseSignal']:
        assert statuses[-1]['State']=='RunLimit' and statuses[-1]['RemainingRuns']==0 and statuses[-1]['RestartEnabled'] is False and statuses[-1]['CloseEnabled'] is True
    receipt=dict(Passed=True,ActualManifestLauncher=True,Runs=checks,Transport=transport,FreshState=resets,ReloadCosts=costs,
        InspectionMatchesActualReports=True,IndependentMovementPrefix=True,ScenarioAndProfilesUnchanged=True,SyntheticCloseSignal=plan['CloseSignal'],PhysicalOsEvent=False)
    fresh(a.out).write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({k:v for k,v in receipt.items() if k not in ('Transport','FreshState')}))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('mode',choices=['prepare','verify'])
    for key in ('scenario','trajectory','out'):parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--expected',type=Path);parser.add_argument('--log',type=Path);parser.add_argument('--close',action='store_true')
    args=parser.parse_args();(prepare if args.mode=='prepare' else verify)(args)
