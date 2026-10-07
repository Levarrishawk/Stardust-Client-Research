"""Independent authored lifecycle envelopes, reusing accepted83 login author and SOE oracle."""
import argparse,importlib.util,json,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT)
    base=out.with_name(out.stem+'-login83.json');guard(base,ROOT)
    original=sys.argv
    try:
        sys.argv=['fixtures83','--out',str(base),'--client-id-opcode','0xD5899226','--permissions-opcode','0xE00730E5']
        runpy.run_path(str(ROOT/'poc/round83-tests/Runtime/fixtures.py'),run_name='__main__')
    finally:sys.argv=original
    spec=importlib.util.spec_from_file_location('peer14',ROOT/'poc/round14-tests/Networking/peer.py');peer=importlib.util.module_from_spec(spec);spec.loader.exec_module(peer);peer.SEED=0x0BADCAFE
    P=struct.pack;oid=0x1122334455667788;crc=peer.checksum('object/synthetic/fixture.iff')
    header=lambda operand,opcode:P('<HI',operand,opcode)
    create=header(5,0xFE89DDEA)+P('<QfffffffIB',oid,0,0,0,1,1.25,2.5,-3.75,crc,0)
    baseline=header(5,0x68A75F0C)+P('<QIBIH',oid,0x4352454F,3,2,0)
    end=header(2,0x2C436037)+P('<Q',oid);ready=header(1,0x43FD1C22)
    sentinel=header(1,0xCAFEBABE)+b'authored-unhandled'
    unknown=bytearray(baseline);struct.pack_into('<I',unknown,14,0x554E4B4E)
    def frame(seq,body):return peer.frame(b'\0\x09'+P('>H',seq)+body).hex().upper()
    value=json.loads(base.read_text());value.update(Create=frame(2,create),Baseline=frame(3,baseline),End=frame(4,end),Sentinel=frame(5,sentinel),ServerReady=frame(6,ready),SentinelHex=sentinel.hex().upper(),UnknownFamilyHex=bytes(unknown).hex().upper(),CreateHex=create.hex().upper(),BaselineHex=baseline.hex().upper(),EndHex=end.hex().upper(),ExpectedReady=ready.hex().upper(),TemplateCrc=crc,SyntheticOpaqueBaseline=True)
    out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(dict(Generated=True,Synthetic=True,TemplateCrc=crc,Path=str(out))))
if __name__=='__main__':main()
