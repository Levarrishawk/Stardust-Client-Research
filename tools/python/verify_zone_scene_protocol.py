#!/usr/bin/env python3
"""Independent offline struct wire oracle for R1-NET-01 synthetic vectors."""
import argparse
import json
import math
from pathlib import Path
import struct

SELECT = bytes.fromhex('0200768d09b588776655443322f1')
SCENE = bytes.fromhex('0900aedfe63aa588776655443322f10300412f420000c03f000010c0000070400300545c4308070605040302fe')


def parse(data, scene):
    cursor = 0
    def read(fmt):
        nonlocal cursor
        size = struct.calcsize(fmt)
        if len(data) - cursor < size:
            raise ValueError('truncated')
        value = struct.unpack_from(fmt, data, cursor)
        cursor += size
        return value
    def text():
        nonlocal cursor
        size, = read('<H')
        if len(data) - cursor < size:
            raise ValueError('string length')
        value = data[cursor:cursor + size].decode('ascii')
        cursor += size
        return value
    count, opcode = read('<HI')
    if (count, opcode) != ((9, 0x3AE6DFAE) if scene else (2, 0xB5098D76)):
        raise ValueError('header')
    if scene:
        flag, oid = read('<BQ')
        terrain = text()
        x, z, y = read('<fff')
        if not all(map(math.isfinite, (x, z, y))):
            raise ValueError('coordinate')
        template = text()
        time, = read('<Q')
        result = dict(RawFlag=flag, ObjectId=oid, Terrain=terrain, X=x, Z=z, Y=y, Template=template, GalacticTime=time)
    else:
        oid, = read('<Q')
        result = dict(ObjectId=oid)
    if cursor != len(data):
        raise ValueError('trailing')
    return result


def reject(data, scene):
    try:
        parse(data, scene)
    except ValueError:
        return
    raise AssertionError('malformed accepted')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--exports', type=Path, required=True, help='Native Networking --export directory')
    args = parser.parse_args()
    assert SELECT == struct.pack('<HIQ', 2, 0xB5098D76, 0xF122334455667788)
    assert SCENE == (struct.pack('<HIBQH', 9, 0x3AE6DFAE, 0xA5, 0xF122334455667788, 3) + b'A/B' + struct.pack('<fffH', 1.5, -2.25, 3.75, 3) + b'T\\C' + struct.pack('<Q', 0xFE02030405060708))
    assert (args.exports / 'select.bin').read_bytes() == SELECT
    assert json.loads((args.exports / 'scene.json').read_text()) == parse(SCENE, True)
    for vector, scene in ((SELECT, False), (SCENE, True)):
        for size in range(len(vector)):
            reject(vector[:size], scene)
        reject(vector + b'\0', scene)
        for offset in (0, 2):
            bad = bytearray(vector); bad[offset] ^= 1; reject(bad, scene)
    for offset in (15, 32):
        bad = bytearray(SCENE); bad[offset:offset + 2] = b'\xff\xff'; reject(bad, True)
        bad = bytearray(SCENE); bad[offset + 2] = 128; reject(bad, True)
    for offset in (20, 24, 28):
        for bits in (0x7f800000, 0xff800000, 0x7fc00000):
            bad = bytearray(SCENE); struct.pack_into('<I', bad, offset, bits); reject(bad, True)
    print(f'Independent wire oracle passed; native exports: {args.exports.resolve()}')


if __name__ == '__main__':
    main()
