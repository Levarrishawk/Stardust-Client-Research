#!/usr/bin/env python3
"""Independent offline CmdSceneReady header oracle; synthetic fixture only."""
import argparse
from pathlib import Path
import struct

GOLDEN = bytes.fromhex('0100221cfd43')

def decode(data):
    if len(data) != 6:
        raise ValueError('exact six-byte header required')
    if struct.unpack('<HI', data) != (1, 0x43FD1C22):
        raise ValueError('operand count/opcode')

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--exports', required=True, type=Path)
    args = parser.parse_args()
    assert struct.pack('<HI', 1, 0x43FD1C22) == GOLDEN
    assert (args.exports / 'ready.bin').read_bytes() == GOLDEN
    decode(GOLDEN)
    invalid = [GOLDEN[:length] for length in range(6)] + [GOLDEN + b'\0']
    for offset in range(6):
        bad = bytearray(GOLDEN); bad[offset] ^= 1; invalid.append(bad)
    for data in invalid:
        try:
            decode(data)
        except ValueError:
            continue
        raise AssertionError('malformed fixture accepted')
    print(f'Independent CmdSceneReady wire oracle passed: {args.exports.resolve()}')

if __name__ == '__main__':
    main()
