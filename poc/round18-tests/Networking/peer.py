"""Independent handpacked APPLICATION fixtures and serial/state receipt oracle."""
import argparse
import importlib.util
import json
import struct
import sys
from pathlib import Path
sys.dont_write_bytecode = True
root = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location('prior_wire', root/'poc/round15-tests/Networking/peer_model.py')
prior = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prior)

def produce():
    p = prior.produce()
    apps = p['Applications']
    h = prior.H
    def packet(counter, oid=h, parent=None):
        return (struct.pack('<HIQhhhIbb',8,0x1B24F808,oid,-32768,9,-7,counter,-128,127) if parent is None else
                struct.pack('<HIQQhhhIbb',8,0xC867AB5A,parent,oid,-32768,9,-7,counter,-128,127)).hex().upper()
    for name,counter in [('first',0xFFFFFFFF),('wrap',0),('duplicate',0),('older',0xFFFFFFFF),('half',0x80000000),('next',1)]:
        apps[name] = packet(counter)
    apps['missing'] = packet(1,0)
    apps['parentZero'] = packet(1,parent=0)
    apps['parentNonzero'] = packet(1,parent=0xFEDCBA9876543210)
    apps['malformed'] = apps['next'][:-2]
    return dict(Applications=apps,Initial=p['Initial'])

def verify(peer, result):
    # Independent half-range ordering; rejection never consumes a revision.
    last = None
    revision = 0
    expected = []
    for key in ['first','wrap','duplicate','older','half','next']:
        data = bytes.fromhex(peer['Applications'][key])
        count,op,oid,x,z,y,counter,speed,direction = struct.unpack('<HIQhhhIbb',data)
        assert (count,op,oid,x,z,y,speed,direction)==(8,0x1B24F808,prior.H,-32768,9,-7,-128,127)
        accepted = last is None or 0 < ((counter-last)&0xFFFFFFFF) < 0x80000000
        if accepted: last=counter; revision+=1
        expected.append(dict(Key=key,Accepted=accepted,Revision=revision,Counter=last,X=x*.25,Y=y*.25,Z=z*.25,Speed=2))
    assert result['Serial']==expected, (result['Serial'],expected)
    assert result['Pump']==dict(AcceptedMessages=2,DroppedMessages=1,PendingMessages=1,SecondAccepted=1,FinalCounter=2,Actors=2)
    required=['BeforeBarrier','Pending','Missing','Parents','StaleGeneration','Freshness','Reset','CaughtReentry','ThrownProjection','MalformedCleanup','OwnerThread','SeparateDisposal','Cleanup','MotionMutation','OtherObjectMutation','Recreation','PumpStaleGeneration']
    assert all(result['Gates'].get(k) is True for k in required)
    assert result['OriginalSources']==[39,27]
    print('Independent ingress oracle: 6 serial receipts, pump caps/copies, 17 lifecycle/failure gates passed')

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--generate');ap.add_argument('--peer');ap.add_argument('--verify');a=ap.parse_args()
    if a.generate: Path(a.generate).write_text(json.dumps(produce(),indent=2)+'\n',encoding='utf-8')
    else: verify(json.loads(Path(a.peer).read_text()),json.loads(Path(a.verify).read_text()))
