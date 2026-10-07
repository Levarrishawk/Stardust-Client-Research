"""Independent synthetic login/zone/world peer. No sockets or original payload corpus."""
import argparse
import importlib.util
import json
import runpy
import struct
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SEED = 0x0BADCAFE


def generate(output, template_crc, object_id, object_path, terrain_path):
    if type(template_crc) is not int or not 0 <= template_crc <= 0xffffffff or type(object_id) is not int or not 0 < object_id <= 0xffffffffffffffff:
        raise ValueError('Explicit bounded templateCRC/nonzeroOID required')
    for path in (object_path, terrain_path):
        if type(path) is not str or not 1 <= len(path) <= 256 or any(ord(c)<32 or ord(c)>126 for c in path):
            raise ValueError('Explicit bounded logical ASCII path required')
    out = runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path'](output, ROOT, directory=True)
    prior = out/'accepted83-fixtures.json'
    subprocess.run([sys.executable, str(ROOT/'poc/round83-tests/Runtime/fixtures.py'), '--out', str(prior),
                    '--client-id-opcode', '0xD5899226', '--permissions-opcode', '0xE00730E5'], check=True, capture_output=True)
    source = json.loads(prior.read_text())
    spec = importlib.util.spec_from_file_location('independent84_soe',ROOT/'poc/round14-tests/Networking/peer.py')
    soe = importlib.util.module_from_spec(spec); spec.loader.exec_module(soe)
    P = struct.pack
    def header(count, opcode): return P('<HI',count,opcode)
    def text(value): raw=value.encode('ascii');return P('<H',len(raw))+raw
    def frame(seed,packet): soe.SEED=seed;return soe.frame(packet).hex().upper()
    def data(seq,app):return frame(SEED,P('>HH',9,seq)+app)
    # The real accepted83 small login grammars, with explicit character/template metadata.
    replies = source['Login'][:]
    soe.SEED=0xA1B2C3D4;plain=bytearray(soe.open_frame(bytes.fromhex(replies[3])))
    if struct.unpack_from('<HI',plain,4)!=(2,0x65EA4574):raise ValueError('Frozen character fixture header changed')
    struct.pack_into('<I',plain,20,template_crc);struct.pack_into('<Q',plain,24,object_id)
    replies[3]=soe.frame(plain).hex().upper()
    start=header(9,0x3AE6DFAE)+b'\1'+P('<Q',object_id)+text(terrain_path)+P('<fff',1.25,-3.75,2.5)+text(object_path)+P('<Q',123456789)
    create=header(5,0xFE89DDEA)+P('<Q7fIB',object_id,0,0,0,1,1.25,-3.75,2.5,template_crc,1)
    baseline=header(5,0x68A75F0C)+P('<QIBIH',object_id,0x4352454F,3,2,0)
    end=header(2,0x2C436037)+P('<Q',object_id)
    sentinel=header(3,0xB5ABF91A)+text('AuthoredNotice')+text('synthetic nonworld sentinel')+b'\0'
    ready=header(1,0x43FD1C22)
    expectedselect=header(2,0xB5098D76)+P('<Q',object_id)
    apps=dict(Start=start,Create=create,OpaqueCreoEnvelope=baseline,End=end,NonworldSentinel=sentinel,ServerReady=ready)
    result=dict(Synthetic=True,OpaqueBaselineSemantics=True,TemplateCrc=template_crc,ObjectId=object_id,
        ObjectPath=object_path,TerrainPath=terrain_path,LoginId=0x12345678,ZoneId=0x87654321,
        LoginSeed=0xA1B2C3D4,ZoneSeed=SEED,LoginResponse=source['LoginResponse'],LoginReplies=replies,
        ZoneResponse=source['ZoneResponse'],Permissions=source['Permissions'],
        WorldFrames=[data(i,app) for i,app in enumerate((start,create,baseline,end,sentinel),1)],
        ServerReady=data(6,ready),Applications={k:v.hex().upper() for k,v in apps.items()},
        ZoneAcks=[frame(SEED,P('>HH',0x15,i)) for i in range(3)],
        ExpectedAuthHex=source['ExpectedAuth'],ExpectedSelectHex=expectedselect.hex().upper(),ExpectedReadyHex=ready.hex().upper(),
        ExpectedAuthWire=data(0,bytes.fromhex(source['ExpectedAuth'])),ExpectedSelectWire=data(1,expectedselect),ExpectedReadyWire=data(2,ready))
    (out/'transcript.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    return result


def verify(transcript, captured):
    """captured dict with AuthWire/SelectWire/ReadyWire uppercase actual hex strings."""
    for key in ('Auth','Select','Ready'):
        if captured[key+'Wire']!=transcript['Expected'+key+'Wire']:raise ValueError('Independent protected '+key+' mismatch')
    return dict(Passed=True,Synthetic=True,ExactAuthSelectReady=3,OpaqueBaselineSemantics=True,LiveCompatibility=False)


def main():
    p=argparse.ArgumentParser();sub=p.add_subparsers(dest='mode',required=True)
    g=sub.add_parser('generate');g.add_argument('--template-crc',type=lambda x:int(x,0),required=True);g.add_argument('--object-id',type=lambda x:int(x,0),required=True)
    g.add_argument('--object-path',required=True);g.add_argument('--terrain-path',required=True);g.add_argument('--output',type=Path,required=True)
    v=sub.add_parser('verify');v.add_argument('--transcript',type=Path,required=True);v.add_argument('--captured',type=Path,required=True)
    a=p.parse_args()
    if a.mode=='generate':generate(a.output,a.template_crc,a.object_id,a.object_path,a.terrain_path);print('PASS authored synthetic transcript')
    else:print(json.dumps(verify(json.loads(a.transcript.read_text()),json.loads(a.captured.read_text()))))


if __name__=='__main__':main()
