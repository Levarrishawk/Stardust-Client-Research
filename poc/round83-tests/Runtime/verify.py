"""Independent SOE decode/reassembly of actual large-authentication test datagrams."""
import argparse,importlib.util,json,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--fixtures',type=Path,required=True);p.add_argument('--result',type=Path,required=True);p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    fixture=json.loads(a.fixtures.read_text());result=json.loads(a.result.read_text(encoding='utf-8-sig').strip())
    assert result['Passed'] is True and result['SyntheticInMemoryOnly'] is True
    spec=importlib.util.spec_from_file_location('accepted_peer',ROOT/'poc/round14-tests/Networking/peer.py');peer=importlib.util.module_from_spec(spec);spec.loader.exec_module(peer);peer.SEED=0x0BADCAFE;peer.MAX=64
    payload=bytearray();declared=None;wires=result['LargeAuthDatagrams'];assert len(wires)>50
    for index,wire in enumerate(wires):
        packet=peer.open_frame(bytes.fromhex(wire));assert packet[:2]==b'\0\x0d' and struct.unpack('>H',packet[2:4])[0]==index
        if index==0:declared=struct.unpack('>I',packet[4:8])[0];payload.extend(packet[8:])
        else:payload.extend(packet[4:])
    assert len(payload)==declared and payload==bytes.fromhex(fixture['ExpectedLargeAuth'])
    target=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT)
    receipt=dict(Passed=True,IndependentPythonPeer=True,AuthBytes=len(payload),Datagrams=len(wires),Checks=result['Checks'],NegativeCases=result['NegativeCases'],SyntheticInMemoryOnly=True,LiveCompatibility=False)
    target.write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
