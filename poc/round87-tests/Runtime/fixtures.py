"""Independent synthetic disconnect struct author and accepted login/SOE fixtures."""
import argparse,importlib.util,json,runpy,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);a=p.parse_args()
    guard=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    out=guard(a.out,ROOT);base=out.with_name(out.stem+'-base85.json');guard(base,ROOT);argv=sys.argv
    try:
        sys.argv=['fixtures85','--out',str(base)];runpy.run_path(str(ROOT/'poc/round85-tests/Runtime/fixtures.py'),run_name='__main__')
    finally:sys.argv=argv
    f=json.loads(base.read_text());spec=importlib.util.spec_from_file_location('accepted14',ROOT/'poc/round14-tests/Networking/peer.py');peer=importlib.util.module_from_spec(spec);spec.loader.exec_module(peer)
    cases=[]
    for id,seed in [(0x12345678,0xa1b2c3d4),(0x87654321,0x0badcafe)]:
        peer.SEED=seed
        for reason in [6,0,0xfedc,0xffff]:
            raw=struct.pack('>HIH',5,id,reason);cases.append(dict(ConnectionId=id,Seed=seed,Reason=reason,CrcWidth=2,Maximum=496,Response=struct.pack('>HII3BI',2,id,seed,2,1,4,496).hex().upper(),Raw=raw.hex().upper(),Wire=peer.frame(raw).hex().upper()))
    assert cases[0]['Raw']=='0005123456780006' and cases[0]['Wire']=='0005C6F7E4D9C6C0C6D27E'
    peer.SEED=0xa1b2c3d4
    for width in [1,2,3,4]:
        raw=struct.pack('>HIH',5,0x12345678,0xfedc);protected=peer.transform(raw+b'\0');wire=protected+(peer.crc(protected)&((1<<(8*width))-1)).to_bytes(width,'big')
        cases.append(dict(ConnectionId=0x12345678,Seed=peer.SEED,Reason=0xfedc,CrcWidth=width,Maximum=16,Response=struct.pack('>HII3BI',2,0x12345678,peer.SEED,width,1,4,16).hex().upper(),Raw=raw.hex().upper(),Wire=wire.hex().upper()))
    f.update(DisconnectCases=cases,Disconnect71Response=struct.pack('>HII3BI',2,0x12345678,0xa1b2c3d4,2,1,4,496).hex().upper(),Synthetic=True)
    out.write_text(json.dumps(f,indent=2)+'\n');print(json.dumps(dict(Generated=True,Cases=len(cases),Path=str(out))))
if __name__=='__main__':main()
