"""Authored in-memory peer fixtures using the accepted independent Python SOE oracle."""
import argparse,importlib.util,json,runpy,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
def main():
    p=argparse.ArgumentParser();p.add_argument('--out',type=Path,required=True);p.add_argument('--client-id-opcode',type=lambda s:int(s,0),required=True);p.add_argument('--permissions-opcode',type=lambda s:int(s,0),required=True);a=p.parse_args()
    target=runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](a.out,ROOT)
    spec=importlib.util.spec_from_file_location('accepted_soe',ROOT/'poc/round14-tests/Networking/peer.py');peer=importlib.util.module_from_spec(spec);spec.loader.exec_module(peer)
    P=struct.pack;version=b'20260817-SD:01';oid=0x1122334455667788;login_id=0x12345678;zone_id=0x87654321
    def header(operands,opcode):return P('<HI',operands,opcode)
    def text(s):b=s.encode('ascii');return P('<H',len(b))+b
    def response(id,seed,max):return b'\0\2'+P('>II',id,seed)+bytes([2,1,4])+P('>I',max)
    def frame(seed,plain):peer.SEED=seed;return peer.frame(plain).hex().upper()
    def data(seed,seq,body,opcode=9):return frame(seed,b'\0'+bytes([opcode])+P('>H',seq)+body)
    def fragments(seed,body,max=496,start=0):
        if len(body)<=max-7:return [data(seed,start,body)]
        length=len(body);result=[];cursor=0;seq=start
        while cursor<length:
            prefix=P('>I',length) if cursor==0 else b'';n=min(max-7-len(prefix),length-cursor)
            result.append(data(seed,seq,prefix+body[cursor:cursor+n],13));cursor+=n;seq+=1
        return result
    def credential(n):return bytes((i*7+3)%256 for i in range(n-4))+P('<I',0x10203040)
    def login(n=5,host='zone.invalid',port=44453,duplicate=False,cluster=7):
        token=header(4,0xAAB296C6)+P('<I',n)+credential(n)+P('<I',99)+text('synthetic')
        clusters=header(2,0xC11C63B9)+P('<II',1,7)+text('Authored')+P('<II',0xffff8f80,8)
        status=header(3,0x3436AEB6)+P('<II',1,7)+text(host)+P('<HHIIIII',port,44454,100,3250,8,0xffff8f80,2)+b'\0'
        character=P('<I',1)+b'A\0'+P('<IQII',0x11223344,oid,cluster,1)
        chars=header(2,0x65EA4574)+P('<I',2 if duplicate else 1)+character*(2 if duplicate else 1)
        result=[];seq=0
        for body in [token,clusters,status,chars]:
            packets=fragments(0xA1B2C3D4,body,start=seq);result.extend(packets);seq+=len(packets)
        return result
    permissions=header(4,a.permissions_opcode)+bytes([1,1,0,1]);denied=header(4,a.permissions_opcode)+bytes([0,1,0,1])
    malformed=header(4,a.permissions_opcode)+bytes([2,1,0,1])
    multi=b'\0\x19'+bytes([len(permissions)])+permissions+bytes([len(permissions)])+permissions
    scene=header(9,0x3AE6DFAE)+b'\1'+P('<Q',oid)+text('terrain/authored.trn')+P('<fff',1.25,2.5,-3.75)+text('object/creature/player/shared_authored.iff')+P('<Q',123456789)
    cases=dict(Login=login(),LargeLogin=login(65536),OversizedCredentialLogin=login(65537),DuplicateLogin=login(duplicate=True),MissingClusterLogin=login(cluster=8),EmptyHostLogin=login(host=''),UriHostLogin=login(host='https://zone.invalid'),ZeroPortLogin=login(port=0))
    zone_seed=0x0BADCAFE
    cases.update(LoginResponse=response(login_id,0xA1B2C3D4,496).hex().upper(),ZoneResponse=response(zone_id,zone_seed,496).hex().upper(),SmallZoneResponse=response(zone_id,zone_seed,64).hex().upper(),WrongZoneResponse=response(zone_id+1,zone_seed,496).hex().upper(),Permissions=data(zone_seed,0,permissions),Denied=data(zone_seed,0,denied),Malformed=data(zone_seed,0,malformed),DuplicateMultipayload=data(zone_seed,0,multi),PostSelection=data(zone_seed,1,scene),PostSelectionHex=scene.hex().upper(),ExpectedAuth=(header(3,a.client_id_opcode)+P('<II',0xfe,5)+credential(5)+text(version.decode())).hex().upper(),ExpectedLargeAuth=(header(3,a.client_id_opcode)+P('<II',0xfe,65536)+credential(65536)+text(version.decode())).hex().upper(),ExpectedSelect=(header(2,0xB5098D76)+P('<Q',oid)).hex().upper(),ZoneAcks=[frame(zone_seed,b'\0\x15'+P('>H',seq)) for seq in range(2000)])
    cases.update(DuplicateAfterSelection=data(zone_seed,1,permissions),PrematureScene=data(zone_seed,0,scene))
    target.write_text(json.dumps(cases,indent=2)+'\n');print(json.dumps(dict(Generated=True,Synthetic=True,Path=str(target))))
if __name__=='__main__':main()
