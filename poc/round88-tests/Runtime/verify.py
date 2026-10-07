"""Compare actual loop wires and transferred remainders against accepted independent peer."""
import argparse,json,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--result',type=Path,required=True);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    f=json.loads(a.fixtures.read_text());r=json.loads(a.result.read_text(encoding='utf-8-sig').strip())
    assert r['Passed'] and r['Wires']==[f['ExpectedAuthWire'],f['ExpectedSelectWire'],f['ExpectedReadyWire'],*f['ExpectedMovementWires'],f['ExpectedDisconnectWire']]
    assert r['Unhandled']==f['ExpectedDeferred']+f['ExpectedUnhandledMovement']
    assert r['Applied']==2 and r['CounterRejected']==2 and r['ReadySubmissions']==1 and r['DisconnectSendCompleted']
    assert r['CorrectionPosition']==[20,2.5,-9] and r['CorrectionCounter']==0
    assert r['MinimumProviderCalls']==1 and r['BorrowedOwnersPreserved'] and r['ProviderCanceled'] and r['FaultedDrainObserved'] and r['FailureSuffixDropped']
    assert r['Checks']>=100 and r['NegativeCases']>=10
    out=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT)
    receipt=dict(Passed=True,Checks=r['Checks'],NegativeCases=r['NegativeCases'],ExactApplicationWires=7,Unhandled=len(r['Unhandled']),Applied=2,CounterRejected=2,AsyncProviderBarrier=True,FaultedDrainObserved=True,FailureSuffixDropped=True,BorrowedOwnersPreserved=True,Synthetic=True,SocketCreated=False,ServerReleaseUnverified=True)
    out.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
