"""Audit FCHS profile and explicit height queries against the configured effective original asset."""
import argparse
import contextlib
import hashlib
import json
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_static_mesh_slice import nodes
from verify_textured_mesh_bundle import Index


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive_root', type=Path)
    parser.add_argument('configuration', type=Path)
    parser.add_argument('native_output', type=Path)
    parser.add_argument('--configuration-sha256', required=True)
    args = parser.parse_args()
    assert hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper() == args.configuration_sha256.upper()
    with contextlib.ExitStack() as stack:
        for priority, path in archives(args.archive_root, args.configuration):
            data = Index(stack.enter_context(path.open('rb'))).lookup('camera/freechasecamera.iff')
            if data is not None:
                break
        else:
            raise ValueError('Missing original camera profile')
    root, = nodes(data)
    version, = root[1]
    assert root[0] == b'FCHS' and version[0] == b'0000'
    parts = dict(version[1])
    zoom = list(struct.unpack('<' + 'f'*(len(parts[b'ZOOM'])//4), parts[b'ZOOM']))
    first, = struct.unpack('<f', parts[b'FRST'])
    default, = struct.unpack('<f', parts[b'DEFH'])
    states = [struct.unpack_from('<iif', parts[b'PSHM'], i) for i in range(0, len(parts[b'PSHM']), 12)]
    postures = [struct.unpack_from('<if', parts[b'PSTM'], i) for i in range(0, len(parts[b'PSTM']), 8)]
    rows, heights = [], []
    for line in args.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('free_chase_profile='):
            rows.append(json.loads(line.split('=', 1)[1]))
        elif line.startswith('free_chase_height='):
            heights.append(json.loads(line.split('=', 1)[1]))
    row, = rows
    same = lambda a, b: struct.pack('<f', a) == struct.pack('<f', b)
    assert row['sha256'] == hashlib.sha256(data).hexdigest().upper() and row['bytes'] == len(data)
    assert len(row['ZoomSettings']) == len(zoom) and all(same(a, b) for a, b in zip(row['ZoomSettings'], zoom))
    assert same(row['FirstPersonThreshold'], first) and same(row['DefaultHeight'], default)
    assert row['invalid_setting_fallback'] == len(zoom)//2
    for actual, expected in zip(row['StateHeights'], states):
        assert (actual['Posture'], actual['State']) == expected[:2] and same(actual['Multiplier'], expected[2])
    for actual, expected in zip(row['PostureHeights'], postures):
        assert actual['Posture'] == expected[0] and same(actual['Multiplier'], expected[1])
    assert len(row['StateHeights']) == len(states) and len(row['PostureHeights']) == len(postures)
    assert len(heights) == 15*3*3
    combinations = set()
    for query in heights:
        posture, state, metric = query['posture'], query['state'], query['metric']
        key = (posture, state, metric)
        assert key not in combinations and posture in range(15) and state in (-1, 19, 27) and metric in (.5, 1, 2)
        combinations.add(key)
        selected = [value for p, s, value in states if p == posture and s == state]
        fallback = dict(postures).get(posture, default)
        assert same(query['height'], (selected[0] if selected else fallback)*metric)
    print(json.dumps({'priority': priority, 'bytes': len(data), 'sha256': row['sha256'], 'zoom_settings': len(zoom),
                      'state_heights': len(states), 'posture_heights': len(postures), 'height_queries': len(heights),
                      'original_fields_and_f32_results_agree': True}))


if __name__ == '__main__':
    main()
