"""Generate standards-only synthetic static material/mesh/component fixtures for HDR integration."""
import argparse,struct,hashlib
from pathlib import Path

def chunk(tag,payload):return tag+struct.pack('>I',len(payload))+payload
def form(tag,*children):return chunk(b'FORM',tag+b''.join(children))
def name(value):return value.encode('ascii')+b'\0'

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('output',type=Path);a=p.parse_args()
    private=(Path(__file__).resolve().parents[2]/'local-private').resolve();assert a.output.resolve().is_relative_to(private)
    a.output.mkdir(parents=True,exist_ok=True)
    main_tag=struct.pack('<I',0x4d41494e)
    shader=form(b'SSHT',form(b'0000',form(b'TCSS',chunk(b'0000',main_tag+b'\0')),
        form(b'TXMS',form(b'TXM ',form(b'0001',chunk(b'DATA',main_tag+bytes(7)),chunk(b'NAME',name('texture/synthetic.dds'))))),chunk(b'NAME',name('effect/synthetic.eft'))))
    state=bytearray(56);state[0]=1;state[4]=state[5]=1;state[17]=15;state[52:56]=main_tag
    stage=bytearray(33);stage[18:22]=stage[22:26]=main_tag
    effect=form(b'EFCT',form(b'0001',chunk(b'DATA',bytes((1,0))),form(b'IMPL',form(b'0005',
        chunk(b'SCAP',struct.pack('<I',4)),chunk(b'DATA',bytes((1,))+main_tag+bytes((1,1))),
        form(b'PASS',form(b'0009',chunk(b'DATA',state),form(b'PFFP',chunk(b'0001',bytes(7))),form(b'STAG',chunk(b'0000',stage))))))))
    vertices=b''.join(struct.pack('<8f',*v) for v in ((0,0,0,0,0,1,0,0),(1,0,0,0,0,1,1,0),(0,1,0,0,0,1,0,1)))
    appearance=form(b'APPR',form(b'0003',chunk(b'NULL',b'')))
    mesh=form(b'MESH',form(b'0004',appearance,form(b'SPS ',form(b'0001',chunk(b'CNT ',struct.pack('<I',1)),form(b'0001',
        chunk(b'NAME',name('shader/synthetic.sht')),chunk(b'INFO',struct.pack('<I',1)),form(b'0001',
        chunk(b'INFO',struct.pack('<IBB',9,1,0)),form(b'VTXA',form(b'0003',chunk(b'INFO',struct.pack('<II',0x1105,3)),chunk(b'DATA',vertices))),
        chunk(b'INDX',struct.pack('<I3H',3,0,1,2))))))))
    def part(path,x):return chunk(b'PART',name(path)+struct.pack('<12f',1,0,0,x,0,1,0,0,0,0,1,0))
    component=form(b'CMPA',form(b'0003',appearance,part('mesh/first.msh',0),part('mesh/second.msh',2)))
    for filename,data in {'synthetic.sht':shader,'synthetic.eft':effect,'synthetic.msh':mesh,'synthetic.cmp':component}.items():
        target=a.output/filename
        if target.exists():assert target.read_bytes()==data,'Refusing different existing fixture'
        else:target.write_bytes(data)
        print(f'synthetic_material_fixture={filename} bytes={len(data)} sha256={hashlib.sha256(data).hexdigest().upper()}')
if __name__=='__main__':main()
