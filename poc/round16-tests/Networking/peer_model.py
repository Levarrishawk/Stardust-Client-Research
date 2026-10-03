"""Authored pump obligations plus exact accepted-coordinator per-frame oracle; no pump implementation."""
import argparse
import copy
import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('r15_peer',ROOT/'poc/round15-tests/Networking/peer_model.py')
prior=importlib.util.module_from_spec(spec);spec.loader.exec_module(prior)


def produce():
    base=prior.produce();frames=[]
    def add(name,queue,ingress,complete=(),allow=False,fail=False):
        frames.append(dict(Name=name,Queue=queue,Ingress=ingress,Complete=list(complete),AllowAdapters=allow,Failure=fail))
    add('start-delayed',['stale','heightA'],['stale','heightA'])
    add('held-publication',['heightB'],['heightB'],[0])
    add('terminal-burst',['terminalDestroy','terminalCreate','terminalBaseline','terminalEnd'],['terminalDestroy','terminalCreate'])
    add('stale-terminal-completion',[],['terminalBaseline','terminalEnd'],[1])
    add('retry-latest-state',['heightA'],['heightA'],[2],True)
    add('settle',[],[],allow=True)
    add('late-failure-burst',['heightB','terminalDelta','terminalBadFamily'],['heightB','terminalDelta'],allow=True)
    add('family-failure',[],['terminalBadFamily'],allow=True,fail=True)
    apps=base['Applications'];apps['stale']='00'
    return dict(Version=1,Label='SYNTHETIC OFFLINE PUMP PEER; TANO OPAQUE',Applications=apps,
        Initial=base['Initial'],Frames=frames,Policy=dict(MaximumQueuedMessages=6,MaximumQueuedBytes=1024,
            MaximumMessageBytes=256,MaximumMessagesPerStep=2,MaximumPendingPublications=2,
            MaximumPublicationsPerStep=1,MaximumPublicationAttempts=4,MaximumChangeAttemptsPerStep=1,
            MaximumActors=2,MaximumPendingResourceWeight=67108864),
        Obligations=['FIFO exact APPLICATION identities','accepted+dropped ingress at most2 per step',
            'queued count/bytes and pending count/weight within policy','queue rejection leaves bytes/queue/world unchanged',
            'held current publication retries using latest snapshot','terminal-only recreation preserves human actor/publication',
            'stale completion never becomes actor','late invalid burst exposes no surviving actor/resource prefix',
            'close cancels outstanding tasks and drain completes only after late tasks are observed'])


def baseline(peer):
    model=prior.Model(dict(Applications=peer['Applications'],Initial=peer['Initial']))
    model.event(dict(Name='barrier',Op='barrier'));model.state='Ready'
    rows=[]
    for frame in peer['Frames']:
        for i in frame['Complete']:model.jobs[i]['Completed']=True
        accepted=dropped=0
        for key in frame['Ingress']:
            if key=='stale':dropped+=1
            elif key=='terminalBadFamily':model.state='Failed';model.objects.clear();model.pending.clear()
            else:model.apply(key);accepted+=1
        if frame['Failure']:poll=dict(Prepared=[],Retired=[])
        else:poll=model.event(dict(Name='poll',Op='poll'))['Result']
        rows.append(dict(Name=frame['Name'],AcceptedMessages=accepted,DroppedMessages=dropped,
            WorldState=model.state,Generation=1,Objects=copy.deepcopy([model.objects[k] for k in sorted(model.objects)]),
            OutstandingJobs=sum(not j['Polled'] for j in model.jobs),Poll=poll))
    return rows


def verify(peer,output,require_pump):
    assert peer==produce(),'Authored fixture identity'
    expected=baseline(peer)
    for actual,want in zip(output['BaselineFrames'],expected,strict=True):assert actual==want,(want['Name'],actual,want)
    assert output['WireIdentities']==peer['Applications']
    assert output['BaselineChecks']==dict(DelayedLatestSnapshot=True,TerminalOnlyRecreated=True,StaleCompletionRetired=True,
        FamilyFailureCleared=True,ShutdownOutstandingObserved=True,ClosedDrained=True)
    assert output['OriginalJobs']==2 and output['ArchiveScopes']==0
    if require_pump:assert output['ActualPumpRan'],'Injected actual pump required'
    if output['ActualPumpRan']:
        assert len(output['PumpFrames'])==len(peer['Frames'])
        for row in output['PumpFrames']:
            if row['StepReturned']:
                assert row['AcceptedMessages']>=0 and row['DroppedMessages']>=0 and row['AcceptedMessages']+row['DroppedMessages']<=2
                assert 0<=row['PublishedResources']<=1
            assert 0<=row['PendingMessages']<=6 and 0<=row['QueuedBytes']<=1024
            assert 0<=row['PendingPublications']<=2 and 0<=row['PendingWeight']<=67108864
        assert output['PumpChecks']==dict(QueueCountReject=True,QueueBytesReject=True,MessageSizeReject=True,
            RejectedCallerUnchanged=True,HeldLatestRetry=True,HumanPreserved=True,FailureNoPrefix=True,
            PublicationBurstNoPrefix=True,StaleHeldPublicationRetired=True,OutstandingCloseDrained=True)
    print('PASS exact baseline per-frame bytes/state oracle; actual pump '+('verified' if output['ActualPumpRan'] else 'NOT RUN (awaits injected lead factory)'))


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);p.add_argument('--require-pump',action='store_true');a=p.parse_args()
    if a.generate:
        a.generate.parent.mkdir(parents=True,exist_ok=True);a.generate.write_text(json.dumps(produce(),indent=2)+'\n',encoding='utf-8',newline='\n');baseline(produce());print('PASS independently authored per-frame obligations and lifecycle bytes')
    else:verify(json.loads(a.peer.read_text()),json.loads(a.verify.read_text()),a.require_pump)
