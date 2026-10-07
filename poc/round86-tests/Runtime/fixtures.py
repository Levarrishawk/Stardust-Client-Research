"""Independent synthetic full-root struct author; accepted85 lifecycle and SOE author."""
import argparse,json,runpy,struct,sys,importlib.util
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    out=guard(a.out,ROOT);base=out.with_name(out.stem+'-base85.json');guard(base,ROOT);argv=sys.argv
    try:
        sys.argv=['fixtures85','--out',str(base)];runpy.run_path(str(ROOT/'poc/round85-tests/Runtime/fixtures.py'),run_name='__main__')
    finally:sys.argv=argv
    f=json.loads(base.read_text());P=struct.pack;oid=0x1122334455667788
    prior=runpy.run_path(str(ROOT/'poc/round15-tests/Networking/peer_model.py'));authored=prior['produce']()
    typed=bytearray.fromhex(authored['Applications']['humanBaseline']);struct.pack_into('<Q',typed,6,oid);f['BaselineHex']=bytes(typed).hex().upper()
    def motion(counter=10,id=oid,kind=0x71):
        return P('<HIIIQfI',5,0x80CE5E46,0x89abcdef,kind,id,1.5,counter)+(P('<Q',99) if kind==0xf1 else b'')+P('<8f',0,0,0,1,4,6,5,2)
    f.update(CorrectionHex=motion().hex().upper(),OlderHex=motion(9).hex().upper(),NewerHex=motion(11).hex().upper(),UnrelatedHex=motion(id=oid+1).hex().upper(),ParentHex=motion(kind=0xf1).hex().upper(),UnknownHex=motion(kind=0x123).hex().upper(),ExpectedLocal=(P('<HIIIQII',5,0x80CE5E46,0x89abcdef,0x71,oid,0x7fc00001,77)+P('<8f',0,0,0,1,7,9,8,3)).hex().upper())
    spec=importlib.util.spec_from_file_location('peer14',ROOT/'poc/round14-tests/Networking/peer.py');peer=importlib.util.module_from_spec(spec);spec.loader.exec_module(peer);peer.SEED=0x0badcafe
    f['ReadyAck']=peer.frame(b'\0\x15\0\x02').hex().upper()
    f['ExpectedMovementWire']=peer.frame(b'\0\x09\0\x03'+bytes.fromhex(f['ExpectedLocal'])).hex().upper()
    out.write_text(json.dumps(f,indent=2)+'\n');print(json.dumps(dict(Generated=True,Synthetic=True,Path=str(out))))
if __name__=='__main__':main()
