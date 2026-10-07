"""Independently authored application bytes over frozen15 typed CREO3 fixture."""
import importlib.util
import json
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[3]


def produce():
    spec = importlib.util.spec_from_file_location('frozen15', ROOT / 'poc/round15-tests/Networking/peer_model.py')
    old = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(old)
    baseline = bytes.fromhex(old.produce()['Applications']['humanBaseline'])
    result = {}
    def identity(raw, oid, offset):
        value = bytearray(raw)
        struct.pack_into('<Q', value, offset, oid)
        return bytes(value)
    original = 'object/creature/player/shared_human_male.iff'
    pose = [0, 0, 0, 0x3f800000, 0x3fc00000, 0xc0400000, 0x40000000]
    for prefix, oid in [('player', 42), ('other', 43)]:
        result[prefix + 'Baseline'] = identity(baseline, oid, 6).hex().upper()
        result[prefix + 'End'] = struct.pack('<HIQ', 2, 0x2c436037, oid).hex().upper()
        result[prefix + 'Create'] = old.wire.application('create', oid, pose, old.wire.checksum('object/root.iff'), 0).hex().upper()
        result['original' + prefix.title() + 'Create'] = old.wire.application('create', oid, pose, old.wire.checksum(original), 0).hex().upper()
    raw_start = bytes.fromhex(old.produce()['Applications']['start'])
    result['start'] = identity(raw_start, 42, 7).hex().upper()
    result['otherStart'] = identity(raw_start, 43, 7).hex().upper()
    result['ready'] = old.produce()['Applications']['ready']
    return result


if __name__ == '__main__':
    target = Path(sys.argv[1])
    with target.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(produce(), indent=2) + '\n')
    print('PASS authored selected42/unrelated43 applications; inherited typed CREO3 fixture')
