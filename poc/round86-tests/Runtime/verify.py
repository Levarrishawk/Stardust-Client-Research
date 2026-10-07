"""Compare actual protected movement and correction primitives to independent fixtures."""
import argparse,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--result',type=Path,required=True);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    f=json.loads(a.fixtures.read_text());r=json.loads(a.result.read_text(encoding='utf-8-sig').strip())
    assert r['Passed'] and r['MovementWires']==[f['ExpectedMovementWire']]
    assert r['CorrectionPosition']==[4,5,6] and r['CorrectionCounter']==11
    assert r['Applied']==2 and r['CounterRejected']==2 and r['Unhandled']==3
    assert r['Checks']>=20 and r['NegativeCases']>=10
    out=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT)
    receipt=dict(Passed=True,Checks=r['Checks'],NegativeCases=r['NegativeCases'],ProtectedMovementFrames=1,Applied=2,CounterRejected=2,Unhandled=3,Synthetic=True,LiveInterop=False)
    out.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
