"""Independent preparation identity and ownership outcome comparisons."""
import argparse,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--result',type=Path,required=True);p.add_argument('--out',type=Path,required=True);a=p.parse_args();f=json.loads(a.fixtures.read_text());r=json.loads(a.result.read_text(encoding='utf-8-sig').strip())
    assert r['Passed'] and r['Sources']==f['Expected'] and r['AuthWire']==f['Peer']['ExpectedAuthWire'] and r['SelectWire']==f['Peer']['ExpectedSelectWire']
    assert r['Checks']>=30 and r['NegativeCases']>=8 and r['DelayedClose'] and r['FaultFinalized'] and r['RollbackObserved'] and r['StaleResolverRejected']
    out=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT);receipt=dict(Passed=True,Checks=r['Checks'],NegativeCases=r['NegativeCases'],PreparationMatched=True,ExactAuthSelect=True,DelayedClose=True,FaultFinalized=True,RollbackObserved=True,StaleResolverRejected=True,Synthetic=True,SocketCreated=False)
    out.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
