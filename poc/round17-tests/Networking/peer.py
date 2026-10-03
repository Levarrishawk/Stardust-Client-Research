"""Independent compact transform byte/coordinate oracle; no production codec or original input."""
import argparse
import json
import struct
from pathlib import Path


def wire(parent,oid,x,z,y,counter,speed,direction):
    return struct.pack('<HI',8,0x1B24F808 if parent is None else 0xC867AB5A)+(b'' if parent is None else struct.pack('<Q',parent))+struct.pack('<QhhhIbb',oid,x,z,y,counter,speed,direction)


def produce():
    rows=[]
    for parent in [None,0,0xFE02030405060708]:
        for x,z,y,counter,speed,direction in [(-32768,32767,-1,0xF1234567,-128,127),
            (1,-1,32767,0xFFFFFFFF,127,-128),(0,0,0,0,0,0)]:
            rows.append(dict(Name=f'edge-{len(rows)}',Hex=wire(parent,0xF122334455667788,x,z,y,counter,speed,direction).hex().upper()))
    rows.append(dict(Name='zero-id',Hex=wire(None,0,-12345,23456,-30000,0x01020304,-1,-1).hex().upper()))
    for parent in [None,0x0102030405060708]:
        for bits in range(256):
            s=bits if bits<128 else bits-256;d=(bits+128)%256;d=d if d<128 else d-256
            rows.append(dict(Name=f'bytes-{len(rows)}',Hex=wire(parent,0x8877665544332211,-257,258,-259,bits,s,d).hex().upper()))
    return dict(Version=1,Label='INDEPENDENTLY HANDPACKED SYNTHETIC COMPACT APPLICATION FIXTURES',Cases=rows)


def decode(text):
    b=bytes.fromhex(text);count,op=struct.unpack_from('<HI',b);assert count==8
    cursor=6;parent=None
    if op==0xC867AB5A:parent,=struct.unpack_from('<Q',b,cursor);cursor+=8
    else:assert op==0x1B24F808
    oid,x,z,y,counter,speed,direction=struct.unpack_from('<QhhhIbb',b,cursor)
    assert len(b)==cursor+20
    scale=0.25 if parent is None else 0.125
    return dict(ObjectId=oid,ParentId=parent,RawX=x,RawZ=z,RawY=y,MovementCounter=counter,SpeedRaw=speed,DirectionRaw=direction,
        CoordinateScale=scale,NativePosition=[x*scale,y*scale,z*scale],NativePositionBits=[struct.unpack('<I',struct.pack('<f',v*scale))[0] for v in (x,y,z)])


def verify(peer,output):
    assert peer==produce()
    for source,result in zip(peer['Cases'],output['Cases'],strict=True):
        assert result==dict(Name=source['Name'],Hex=source['Hex'],Value=decode(source['Hex']))
    assert output['MalformedProbes']==sum(len(bytes.fromhex(r['Hex']))+7 for r in peer['Cases'])+2
    assert output['OwnershipAndNullChecks'] is True
    print(f"PASS independent exact transform bytes/raw signs/scales/native X-Y-Z: {len(peer['Cases'])} fixtures")


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--generate',type=Path);p.add_argument('--peer',type=Path);p.add_argument('--verify',type=Path);a=p.parse_args()
    if a.generate:
        a.generate.parent.mkdir(parents=True,exist_ok=True);a.generate.write_text(json.dumps(produce(),indent=2)+'\n',encoding='utf-8',newline='\n');print('PASS 522 handpacked compact transform fixtures')
    else:verify(json.loads(a.peer.read_text()),json.loads(a.verify.read_text()))
