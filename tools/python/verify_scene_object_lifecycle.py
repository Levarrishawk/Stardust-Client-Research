#!/usr/bin/env python3
"""Independent hand-assembled synthetic lifecycle wire oracle, offline only."""
import argparse
import json
import math
from pathlib import Path
import struct

HEX = {
    'create': '0500EADD89FE88776655443322F10000803E000000BF0000403F0000A03F000020C10000A8410000F4C1443322F1A5',
    'destroy': '030004D5454D88776655443322F1FF',
    'containment': '04009EDECB5688776655443322F108070605040302FEFEFFFFFF',
    'end': '02003760432C88776655443322F1',
}
LAYOUT = {'create': ('<HIQ7fIB', 5, 0xFE89DDEA), 'destroy': ('<HIQB', 3, 0x4D45D504), 'containment': ('<HIQQI',4,0x56CBDE9E), 'end': ('<HIQ',2,0x2C436037)}
OID = 0xF122334455667788


def decode(kind, data):
    fmt, count, opcode = LAYOUT[kind]
    if len(data) != struct.calcsize(fmt): raise ValueError('length')
    fields = struct.unpack(fmt, data)
    if fields[:2] != (count,opcode): raise ValueError('header')
    if kind == 'create':
        if not all(math.isfinite(value) for value in fields[3:10]): raise ValueError('nonfinite')
        keys = ('ObjectId','QuaternionX','QuaternionY','QuaternionZ','QuaternionW','PositionX','PositionZ','PositionY','TemplateCrc','RawHyperspaceFlag')
    elif kind == 'destroy': keys = ('ObjectId','RawHyperspaceFlag')
    elif kind == 'containment': keys = ('ObjectId','ContainerId','ArrangementBits')
    else: keys = ('ObjectId',)
    return dict(zip(keys, fields[2:]))


def reject(kind, data):
    try: decode(kind,data)
    except ValueError: return
    raise AssertionError('malformed accepted')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--exports', type=Path, required=True)
    args = parser.parse_args()
    expected = {'create':struct.pack('<HIQ7fIB',5,0xFE89DDEA,OID,.25,-.5,.75,1.25,-10,21,-30.5,0xF1223344,0xA5),
                'destroy':struct.pack('<HIQB',3,0x4D45D504,OID,255),
                'containment':struct.pack('<HIQQI',4,0x56CBDE9E,OID,0xFE02030405060708,0xFFFFFFFE),
                'end':struct.pack('<HIQ',2,0x2C436037,OID)}
    actual=json.loads((args.exports/'decoded.json').read_text(encoding='utf-8'))
    for kind,data in expected.items():
        assert bytes.fromhex(HEX[kind]) == data
        assert (args.exports/(kind+'.bin')).read_bytes() == data
        assert actual[kind] == decode(kind,data)
        for size in range(len(data)): reject(kind,data[:size])
        reject(kind,data+b'\0')
        for offset in range(6):
            bad=bytearray(data); bad[offset]^=1; reject(kind,bad)
    for offset in range(14,42,4):
        for bits in (0x7f800000,0xff800000,0x7fc00000):
            bad=bytearray(expected['create']); struct.pack_into('<I',bad,offset,bits); reject('create',bad)
    for flag in range(256):
        for kind in ('create','destroy'):
            data=bytearray(expected[kind]); data[-1]=flag
            assert decode(kind,data)['RawHyperspaceFlag']==flag
    print('Independent lifecycle bytes, full decoded fields and malformed oracle passed:',args.exports.resolve())


if __name__ == '__main__': main()
