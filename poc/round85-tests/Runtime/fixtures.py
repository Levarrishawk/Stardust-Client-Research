"""Authored selected ingress fixtures; accepted full4/full6 Python field authors reused."""
import argparse,json,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'];out=guard(a.out,ROOT);base=out.with_name(out.stem+'-base84.json');guard(base,ROOT)
    argv=sys.argv
    try:sys.argv=['base84','--out',str(base)];runpy.run_path(str(ROOT/'poc/round84-tests/Runtime/fixtures.py'),run_name='__main__')
    finally:sys.argv=argv
    value=json.loads(base.read_text());P=struct.pack;oid=0x1122334455667788
    def envelope(view,fields=b'',id=oid,tag=0x4352454F,delta=False,count=0):return P('<HIQIBIH',5,0x12862153 if delta else 0x68A75F0C,id,tag,view,len(fields)+2,count)+fields
    four=runpy.run_path(str(ROOT/'poc/round38-tests/Networking/peer.py'));m=four['seed']();m['FloatBits']=[0x3f800000]*10;m['ListenTo']=123;m['Encumbrances']['Values']=[1,2,3];m['SkillMods']['Values']=[];m['SpaceMissionObjects']['Values']=[]
    six=runpy.run_path(str(ROOT/'poc/round32-tests/Networking/peer.py'));n=six['baseline']();n['Passive']=0x76
    value.update(Baseline1Hex=envelope(1).hex().upper(),Baseline4Hex=envelope(4,four['baseline_fields'](m),count=14).hex().upper(),Baseline6Hex=envelope(6,six['baseline_fields'](n),count=18).hex().upper(),Delta3Hex=envelope(3,delta=True).hex().upper(),Unsupported8Hex=envelope(8).hex().upper(),ExpectedListenTo=123,ExpectedEncumbrances=[1,2,3],ExpectedPassive=0x76)
    neighbor=bytearray.fromhex(value['CreateHex']);struct.pack_into('<Q',neighbor,6,oid+1);struct.pack_into('<I',neighbor,42,0xDEADBEEF)
    end=P('<HIQ',2,0x2C436037,oid+1);player=bytearray(neighbor);struct.pack_into('<Q',player,6,oid+2)
    value.update(Nearby=[bytes(neighbor).hex().upper(),envelope(3,id=oid+1).hex().upper(),envelope(6,id=oid+1).hex().upper(),end.hex().upper()],Player=[bytes(player).hex().upper(),*[envelope(view,id=oid+2,tag=0x504C4159).hex().upper() for view in [3,6,8,9]],P('<HIQ',2,0x2C436037,oid+2).hex().upper()],ContainmentHex=P('<HIQQI',4,0x56CBDE9E,oid,0,0xffffffff).hex().upper(),DestroyHex=P('<HIQB',3,0x4D45D504,oid,0).hex().upper(),ExpectedDeferred=12,Synthetic=True,OpaqueCREO1=True,OpaquePLAY=True)
    out.write_text(json.dumps(value,indent=2)+'\n');print(json.dumps(dict(Generated=True,Path=str(out),ExpectedDeferred=12)))
if __name__=='__main__':main()
