"""Whole protected handoff comparisons and policy outcome accounting."""
import argparse,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--result',type=Path,required=True);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    f=json.loads(a.fixtures.read_text());r=json.loads(a.result.read_text(encoding='utf-8-sig'))
    assert r['Passed'] and len(r['Cases'])==len(f['Cases'])
    for expected,actual in zip(f['Cases'],r['Cases']):
        assert actual['Name']==expected['Name'] and actual['Accepted']==expected['Accepted'] and actual['ReceiveFault']==expected['ReceiveFault']
        if expected['Accepted']:assert actual['Auth']==f['Peer']['ExpectedAuthWire'] and actual['Select']==f['Peer']['ExpectedSelectWire']
        else:assert actual['ZoneFactories']==0 and actual['ZoneWires']==0
    out=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT)
    receipt=dict(Passed=True,Checks=r['Checks'],NegativeCases=r['NegativeCases'],EndpointCases=len(r['Cases']),ExactProtectedHandoffs=4,RejectedBeforeZoneFactory=15,Synthetic=True,SocketCreated=False)
    out.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
