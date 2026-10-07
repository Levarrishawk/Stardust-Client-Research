"""Independent exact ready wire oracle; no world or visual publication inference."""
import argparse,importlib.util,json,runpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--result',type=Path,required=True);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    fixtures=json.loads(a.fixtures.read_text());result=json.loads(a.result.read_text(encoding='utf-8-sig').strip());assert result['Passed'] is True
    spec=importlib.util.spec_from_file_location('accepted_peer',ROOT/'poc/round14-tests/Networking/peer.py');peer=importlib.util.module_from_spec(spec);spec.loader.exec_module(peer);peer.SEED=0x0BADCAFE
    wires=result['ReadyWires'];assert len(wires)==1
    expected=peer.frame(b'\0\x09\0\x02'+bytes.fromhex(fixtures['ExpectedReady'])).hex().upper();assert wires[0]==expected
    target=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT)
    receipt=dict(Passed=True,Checks=result['Checks'],NegativeCases=result['NegativeCases'],IndependentReadyDatagrams=1,ReadySequence=2,OpaqueBaselineSemantics=True,VisualPublication=False,LiveCompatibility=False)
    target.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
