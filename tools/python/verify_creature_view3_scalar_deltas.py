#!/usr/bin/env python3
"""Independent typed CREO:3 scalar oracle; hand-packed synthetic wire only."""
import argparse
import json
import math
from pathlib import Path
import struct
from verify_replication_envelopes import decode as envelope

FIELDS=bytes.fromhex('0B00FF0C00FE0D0008070605040302FE0E000000E0BF0F00443322F110008796A5B4C3D2E1F00B00070E0000000080')
WIDTH={11:1,12:1,13:8,14:4,15:4,16:8}
KEY={11:'Posture',12:'Rank',13:'LinkId',14:'HeightBits',15:'Shock',16:'States'}


def parse(data,maximum=8):
    if maximum<0: raise ValueError('update budget')
    e=envelope('delta',data,49)
    if e['RawTag']!=0x4352454f or e['View']!=3: raise ValueError('tag/view')
    fields=bytes.fromhex(e['OpaqueHex']); cursor=0; updates=[]
    while cursor<len(fields):
        if len(updates)>=maximum or len(fields)-cursor<2: raise ValueError('budget/partial index')
        index,=struct.unpack_from('<H',fields,cursor); cursor+=2
        if index not in WIDTH: raise ValueError('index')
        width=WIDTH[index]
        if len(fields)-cursor<width: raise ValueError('partial value')
        bits=int.from_bytes(fields[cursor:cursor+width],'little')
        if index==14 and not math.isfinite(struct.unpack_from('<f',fields,cursor)[0]): raise ValueError('height')
        updates.append(dict(Index=index,RawBits=bits)); cursor+=width
    return e,updates


def wire(fields,count=0,tag=0x4352454f,view=3):
    return struct.pack('<HIQIBIH',5,0x12862153,0xf122334455667788,tag,view,len(fields)+2,count)+fields


def reject(data,maximum=8):
    try: parse(data,maximum)
    except ValueError: return
    raise AssertionError('malformed accepted')


def main():
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('--exports',required=True,type=Path); args=parser.parse_args()
    fields=(struct.pack('<HBHBHQHfHIHQHBHI',11,255,12,254,13,0xfe02030405060708,14,-1.75,15,0xf1223344,16,0xf0e1d2c3b4a59687,11,7,14,0x80000000))
    assert fields==FIELDS
    data=wire(fields); assert data==(args.exports/'delta.bin').read_bytes()
    high=wire(fields,65535); assert high==(args.exports/'delta-count65535.bin').read_bytes()
    e,updates=parse(data); high_e,high_updates=parse(high); assert high_e['DeclaredCount']==65535 and high_updates==updates
    actual=json.loads((args.exports/'plan-state.json').read_text(encoding='utf-8'))
    assert actual['ObjectId']==e['ObjectId'] and actual['DeclaredCount']==0 and actual['Updates']==updates
    initial=dict(Posture=1,Rank=2,LinkId=3,HeightBits=struct.unpack('<I',struct.pack('<f',4.5))[0],Shock=5,States=6)
    applied=initial.copy()
    for update in updates: applied[KEY[update['Index']]]=update['RawBits']
    assert actual['Initial']==initial and actual['Applied']==applied and applied['HeightBits']==0x80000000
    assert parse(wire(b'',65535),0)[1]==[]
    for length in range(len(data)): reject(data[:length])
    boundaries={0,3,6,16,22,28,38,41,47}
    for length in range(len(fields)):
        if length in boundaries: parse(wire(fields[:length],65535))
        else: reject(wire(fields[:length],65535))
    for index in (0,10,17,65535): reject(wire(fields[:3]+struct.pack('<H',index)))
    for offset in (18,43):
        for bits in (0x7f800000,0xff800000,0x7fc00000):
            bad=bytearray(fields); struct.pack_into('<I',bad,offset,bits); reject(wire(bad))
    reject(wire(fields,tag=0x4f455243)); reject(wire(fields,view=255)); reject(data,7)
    print('Independent native bytes/ordered plan/immutable state/bits and malformed oracle passed:',args.exports.resolve())

if __name__=='__main__': main()
