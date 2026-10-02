#!/usr/bin/env python3
"""Independent synthetic 23-byte application replication envelope oracle."""
import argparse
import json
from pathlib import Path
import struct

GOLDEN = {
    'baseline':'05000C5FA76888776655443322F1443322F1FF06000000FFFFA1B200FF',
    'delta':'05005321861208070605040302FE4F455243030300000000007F',
}
OPCODES={'baseline':0x68A75F0C,'delta':0x12862153}

def decode(kind,data,budget):
    if budget<2 or budget>0x7fffffff-23: raise ValueError('budget configuration')
    if len(data)<25: raise ValueError('truncated')
    count,opcode,oid,tag,view,size=struct.unpack_from('<HIQIBI',data)
    if count!=5 or opcode!=OPCODES[kind]: raise ValueError('header')
    if size<2 or size>budget or size!=len(data)-23: raise ValueError('size')
    declared,=struct.unpack_from('<H',data,23)
    return dict(ObjectId=oid,RawTag=tag,View=view,DeclaredCount=declared,PayloadSize=size,OpaqueHex=data[25:].hex().upper())

def reject(kind,data,budget=100):
    try: decode(kind,data,budget)
    except ValueError: return
    raise AssertionError('malformed envelope accepted')

def main():
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--exports',required=True,type=Path); args=parser.parse_args()
    fixtures={'baseline':struct.pack('<HIQIBIH',5,OPCODES['baseline'],0xF122334455667788,0xF1223344,255,6,65535)+bytes.fromhex('A1B200FF'),
              'delta':struct.pack('<HIQIBIH',5,OPCODES['delta'],0xFE02030405060708,0x4352454F,3,3,0)+b'\x7f'}
    outputs=json.loads((args.exports/'decoded.json').read_text(encoding='utf-8'))
    assert struct.calcsize('<HIQIBI')==23
    for kind,data in fixtures.items():
        assert data==bytes.fromhex(GOLDEN[kind])==(args.exports/(kind+'.bin')).read_bytes()
        assert decode(kind,data,100)==outputs[kind]
        for length in range(len(data)): reject(kind,data[:length])
        reject(kind,data+b'\0')
        for offset in range(6):
            bad=bytearray(data); bad[offset]^=1; reject(kind,bad)
        for size in (0,1,2,100,0xffffffff):
            bad=bytearray(data); struct.pack_into('<I',bad,19,size); reject(kind,bad)
        reject(kind,data,len(data)-24)
        for view in range(256):
            empty=struct.pack('<HIQIBIH',5,OPCODES[kind],0,0xffffffff,view,2,65535)
            parsed=decode(kind,empty,2)
            assert parsed['DeclaredCount']==65535 and parsed['OpaqueHex']=='' and parsed['View']==view
        # Server builder offsets include SOE's four bytes, which are excluded from application codec.
        framed=bytes.fromhex('00090000')+data
        assert struct.unpack_from('<I',framed,23)[0]==len(framed)-27
        assert struct.unpack_from('<H',framed,27)[0]==outputs[kind]['DeclaredCount']
    reject('delta',fixtures['baseline']); reject('baseline',fixtures['delta'])
    print('Independent envelope bytes/scalars/owned outputs and malformed oracle passed:',args.exports.resolve())

if __name__=='__main__': main()
