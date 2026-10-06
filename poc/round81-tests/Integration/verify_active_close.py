"""Verify controlled close through the interactive launcher against a pre-native policy/model."""
import argparse, json, struct
from pathlib import Path
import playground_restart_peer as prior

def verify(a):
    text=a.log.read_text(encoding='utf-8-sig');summary,=prior.records(text,'round81_active_close_summary=')
    e=json.loads(a.expected.read_text());report=summary['Report']
    assert not e['NativeOutputUsed'] and a.expected.stat().st_mtime<a.log.stat().st_mtime
    assert summary['Passed'] and summary['ActualInteractiveLauncher'] and summary['SuccessNotInvented'] and summary['NoSuccessor'] and summary['AllActorsAndJobsDrained']
    assert report['Reason']==e['Reason'] and e['MinimumCompleted']<=report['CompletedUpdates']<=e['MaximumCompleted']
    assert report['AttemptedUpdates']==report['CompletedUpdates'] and report['WindowClosing'] and report['CleanupSucceeded'] and not report['Restartable'] and not report['ValidOutcome']
    assert report['Actors']==report['QueuedPublications']==report['PendingPublications']==report['OutstandingArchiveJobs']==report['QueueCount']==report['PendingMessages']==report['RetainedPackets']==0
    assert report['ArchiveCalls']==report['ArchiveClosed']==e['ArchiveCalls']
    args=json.loads(a.arguments.read_text());p=json.loads(Path(args[0][args[0].index('--terrain-point-target-profile')+1]).read_text())
    net=prior.module('close81_wire','poc/round81-tests/Networking/transport_fixtures.py')
    model=prior.module('close81_pose','poc/round76-tests/Integration/idle_playground_peer.py')
    fixture=json.loads(a.trajectory.read_text());assert not fixture['NativeOutputUsed']
    moves=prior.records(text,'point_target_state=');assert len(moves)==report['CompletedUpdates']
    for i,row in enumerate(moves):
        want=fixture['Movement'][i];model.near(row['Native'],want['Native'],5e-6,('close pose',i));model.near([row['Speed']],[want['Speed']],2e-6,('close speed',i))
        assert row['Index']==i and row['Mode']==want['Mode'] and row['CompletedUpdates']==row['AttemptedUpdates']==i+1
    handshake=net.handshakes(p);boot,=net.records(text,'soe_root_transport_ready');assert all(boot[k]==v for k,v in handshake.items())
    raws=net.records(text,'point_outbound_root');frames=net.records(text,'soe_root_datagram')
    assert len(raws)==len(frames)==report['RootFrames']
    for sequence,(raw,wire) in enumerate(zip(raws,frames)):
        encoder=net.encoder(p['SoeSeed']);app=bytes.fromhex(raw['Hex'])
        assert len(app)==62 and wire['Sequence']==sequence and wire['Hex']==encoder.frame(struct.pack('>HH',9,sequence)+app).hex().upper() and wire['AckHex']==encoder.frame(struct.pack('>HH',0x15,sequence)).hex().upper()
    receipt=dict(Passed=True,ActualInteractiveLauncher=True,CompletedUpdates=report['CompletedUpdates'],Reason='Incomplete',WindowClosing=True,AllActorsAndJobsDrained=True,IndependentMovementPrefix=True,ProtectedRoots=len(frames),SocketCreated=False)
    prior.fresh(a.out).write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))

if __name__=='__main__':
    p=argparse.ArgumentParser()
    for key in ('arguments','trajectory','expected','log','out'):p.add_argument('--'+key,type=Path,required=True)
    verify(p.parse_args())
