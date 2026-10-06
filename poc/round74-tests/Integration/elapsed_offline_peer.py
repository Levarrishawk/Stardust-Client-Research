"""Integer schedule and reusable offline-drain gates over the unchanged long trajectory."""
import importlib.util,json,math,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=importlib.util.spec_from_file_location('elapsed74_prior73',ROOT/'poc/round73-tests/Integration/offline_playground_peer.py');old=importlib.util.module_from_spec(s);s.loader.exec_module(old)
records=old.records
def expected_clock(elapsed,previous):
    remainder,reserved,dropped=previous;available=remainder+elapsed;due=available//10000;steps=min(due,8,2000-reserved)
    return dict(Steps=steps,StepMicroseconds=10000,TotalReservedSteps=reserved+steps,RemainderMicroseconds=available%10000,
        DroppedMicroseconds=(due-steps)*10000,TotalDroppedMicroseconds=dropped+(due-steps)*10000,Exhausted=reserved+steps==2000)
def produce(out):
    rows=[];state=(0,0,0)
    for i in range(1000):
        elapsed=(0,5000,15000,33000,250000)[i%5];want=expected_clock(elapsed,state);rows.append(dict(Index=i,ElapsedMicroseconds=elapsed,Receipt=want));state=want['RemainderMicroseconds'],want['TotalReservedSteps'],want['TotalDroppedMicroseconds']
        if want['TotalReservedSteps']>=1001:break
    assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(dict(NativeOutputUsed=False,Rows=rows),indent=2)+'\n')
    return dict(Plans=len(rows),ReservedSlots=state[1],CompletedMotionUpdates=1000,CorrectionOnlySlots=1,NativeOutputUsed=False)
def verify(p,t,trajectory,clock_fixture,text,negative=False):
    assert p['Version']==10 and p['Policy']=='SuppliedElapsedOfflinePlayground' and p['InvalidOfflineDrainAck']==negative
    plans=records(text,'offline_frame_clock_state=');assert plans and not clock_fixture['NativeOutputUsed'];state=(0,0,0);last_completed=0;total_clamped=0
    for i,row in enumerate(plans):
        assert row['Index']==i and row['Synthetic']==p['SyntheticFrameClock'] and row['ModernPolicy']
        supplied=row['RawMicroseconds'];admitted=row['AdmittedMicroseconds'];clamped=row['ClampedMicroseconds'];assert admitted==min(supplied,250000) and clamped==supplied-admitted
        if p['SyntheticFrameClock']:assert supplied==(0,5000,15000,33000,250000)[i%5]
        else:assert supplied==round(row['MeasuredSeconds']*1000000) and math.isfinite(row['MeasuredSeconds']) and 0<=row['MeasuredSeconds']<=60
        previous_reserved=state[1]
        assert row['CompletedDriverUpdates']==previous_reserved-int(previous_reserved>=21),(i,'actual completed slot prefix',row,previous_reserved)
        want=expected_clock(admitted,state);assert row['Receipt']==want,(i,row,want);state=want['RemainderMicroseconds'],want['TotalReservedSteps'],want['TotalDroppedMicroseconds']
        total_clamped+=clamped;assert row['TotalClampedMicroseconds']==total_clamped
        assert last_completed<=row['CompletedDriverUpdates']<=1000;last_completed=row['CompletedDriverUpdates']
    cleanup,=records(text,'offline_frame_clock_cleanup=');assert cleanup['Plans']==len(plans) and cleanup['State']==plans[-1]['Receipt'] and cleanup['Disposed'] and cleanup['HistoricStatePreserved'] and not cleanup['ReservedSlotsAreCompletedUpdates']
    assert cleanup['TotalClampedMicroseconds']==total_clamped
    if not negative:
        assert cleanup['CompletedDriverUpdates']==cleanup['ReportedPointIndex']==1000 and 1001<=state[1]<=1008
        if p['SyntheticFrameClock']:assert [r['Receipt'] for r in plans]==[r['Receipt'] for r in clock_fixture['Rows']]
        # Only profile identifier changes; actual trajectory/ray/phase/HUD/wire/cleanup inputs are unprojected.
        projected=dict(p,Version=9,Policy='SuppliedLongOfflinePlayground');result=old.verify(projected,t,trajectory,text)
        drain,=records(text,'offline_root_drain_cleanup=');assert drain['AcceptedCount']==drain['BridgeAcceptedCount']==drain['NextSequence']==90 and not drain['IsFaulted'] and drain['BorrowedQueueCount']==0 and drain['BorrowedQueuePreserved'] and drain['BorrowedTransportPreserved']
        assert not records(text,'soe_root_bridge_cleanup=') and not records(text,'offline_root_drain_failure=')
    else:
        # Pause at prescribed step5 is the first immediate moving-to-stopped packet.
        moves=records(text,'point_target_state=');gaits=records(text,'accepted_locomotion_state=');assert len(moves)==len(gaits)==6
        for i,(motion,gait,wanted) in enumerate(zip(moves,gaits,trajectory['Locomotion']['Rows'])):
            old.near([motion['Native'][0],motion['Native'][2]],wanted['NativeXZ'],5e-6,('negative independent prefix',i));assert gait['Mode']==wanted['Mode'] and gait['Generation']==wanted['Generation'] and gait['Events']==wanted['Events']
            assert abs(gait['NormalizedPhase']-wanted['NormalizedPhase'])<6e-5 and gait['Observations']==motion['CompletedUpdates']==i+1
        assert cleanup['CompletedDriverUpdates']==6 and cleanup['ReportedPointIndex']==5
        failed,=records(text,'offline_root_drain_failure=');assert failed['CompletedDriverUpdates']==6 and failed['AtStep']==5 and failed['QueueCount']==1 and failed['QueueBytes']==62
        assert failed['AcceptedCount']==failed['BridgeAcceptedCount']==failed['PendingMessages']==failed['RetainedPackets']==0 and not failed['Sent'] and not failed['RollbackClaimed']
        assert not failed['IsFaulted']
        drain,=records(text,'offline_root_drain_cleanup=');assert drain['AcceptedCount']==drain['BridgeAcceptedCount']==drain['NextSequence']==0 and drain['BorrowedQueueCount']==1 and drain['BorrowedQueuePreserved'] and drain['BorrowedTransportPreserved'] and not drain['IsFaulted'] and drain['LastNowMilliseconds']==-1
        queue,=records(text,'manual_outbound_queue_cleanup=');assert queue['PendingBefore']==1 and queue['QueuedBytesBefore']==62 and queue['Count']==queue['QueuedBytes']==queue['Transferred']==0
        observer,=records(text,'accepted_locomotion_cleanup=');assert observer['Observations']==6 and observer['Rebases']==0 and observer['Events']==0 and observer['WorldAlreadyClosedByHost']
        failure,=records(text,'pump_failure_cleanup=');assert failure['drained'] and failure['actors']==0 and failure['calls']==failure['closed']==3
        assert not records(text,'point_outbound_root=') and not records(text,'soe_root_datagram=') and 'PASS: pumped point_target=' not in text
        for bad in ('Input cleanup:','Terrain cleanup:','Exit cleanup:'):assert bad not in text
        result=dict(Passed=True,InvalidAckBeforeTake=True,ImmediatePauseStopPacket=True,CompletedMotionPrefix=6,ReportedIndexPrefix=5,PendingFrameExplicitlyDisposed=True,AllOwnersDrained=True,RollbackClaimed=False)
    result.update(FramePlans=len(plans),ReservedSlots=state[1],CompletedDriverUpdates=cleanup['CompletedDriverUpdates'],UnusedOrCorrectionSlots=state[1]-cleanup['CompletedDriverUpdates'],
        IntegerClockVerified=True,SyntheticFrameInput=p['SyntheticFrameClock'],DroppedMicroseconds=state[2],ClampedMicroseconds=total_clamped,ReuseOfPriorProfileIdentifierExplicit=True,SocketCreated=False,Sent=False)
    return result
if __name__=='__main__':
    if sys.argv[1]=='generate':print(json.dumps(produce(Path(sys.argv[2]))))
    else:
        mode,p,t,trajectory,clock_fixture,log,out=sys.argv[1:];assert mode in ('verify','verify-negative')
        result=verify(*[json.loads(Path(path).read_text()) for path in (p,t,trajectory,clock_fixture)],Path(log).read_text(encoding='utf-8-sig'),mode=='verify-negative')
        out=Path(out);assert not out.exists() and out.resolve().is_relative_to((ROOT/'local-private').resolve());out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
