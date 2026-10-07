"""Exact authored protected disconnect, send order and executed cleanup receipt verification."""
import argparse,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--result',type=Path,required=True);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    f=json.loads(a.fixtures.read_text());r=json.loads(a.result.read_text(encoding='utf-8-sig').strip())
    assert r['Passed'] and r['TransportWires']==[c['Wire'] for c in f['DisconnectCases']]
    zone=[c for c in f['DisconnectCases'] if c['ConnectionId']==0x87654321]
    assert r['SessionWires']==[c['Wire'] for c in zone] and r['PumpDisconnectWires']==[zone[0]['Wire']]
    assert r['Checks']>=80 and r['NegativeCases']>=15 and r['SuccessSends']==1
    assert r['Failures']==['IOException','IOException','TaskCanceledException','TimeoutException','IOException','TaskCanceledException']
    assert r['LateReceiveCleared'] and r['PriorSendPreserved'] and r['UnsentCleared'] and r['NoServerReleaseClaim']
    assert r['ReceiveFailures']==['IOException','TaskCanceledException'] and r['ReceiveFaultDrainObserved'] is True
    assert r['CleanupFaultObserved'] and r['PriorDeadlineObserved']
    out=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT)
    receipt=dict(Passed=True,Checks=r['Checks'],NegativeCases=r['NegativeCases'],TransportVectors=len(r['TransportWires']),SessionVectors=len(r['SessionWires']),SuccessSends=1,FailureCases=len(r['Failures']),ReceiveFailureCases=2,ReceiveFaultDrainObserved=True,Exact71Vector=True,Synthetic=True,SocketCreated=False,ServerReleaseUnverified=True)
    out.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
