"""Independent pinned APPR primary/secondary extent comparison; original bytes stay private."""
import argparse
import contextlib
import hashlib
import json
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes


def extent(node):
    tag, children = node
    if tag == b'NULL':
        assert children == []
        return None
    assert tag in (b'EXSP', b'EXBX') and len(children) == 1
    version, fields = children[0]
    assert version == b'0001'
    if tag == b'EXSP':
        assert len(fields) == 1 and fields[0][0] == b'SPHR' and len(fields[0][1]) == 16
        values = struct.unpack('<4f', fields[0][1])
        return dict(Kind='EXSP', Version='0001', center=list(values[:3]), Radius=values[3], minimum=None, maximum=None)
    assert len(fields) == 2 and fields[1][0] == b'BOX ' and len(fields[1][1]) == 24
    sphere = extent(fields[0])
    assert sphere is not None
    values = struct.unpack('<6f', fields[1][1])
    return dict(sphere, Kind='EXBX', maximum=list(values[:3]), minimum=list(values[3:]))


def same(expected, actual):
    if expected is None:
        assert actual is None
        return
    assert set(expected) == set(actual)
    for key, value in expected.items():
        if isinstance(value, list):
            assert len(actual[key]) == len(value)
            assert struct.pack('<' + 'f'*len(value), *value) == struct.pack('<' + 'f'*len(value), *actual[key])
        elif isinstance(value, float):
            assert struct.pack('<f', value) == struct.pack('<f', actual[key])
        else:
            assert value == actual[key]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    parser.add_argument('configuration', type=Path)
    parser.add_argument('native_output', type=Path)
    parser.add_argument('--configuration-sha256', required=True)
    args = parser.parse_args()
    assert hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper() == args.configuration_sha256.upper()
    assert args.native_output.stat().st_size <= 1048576
    records = [json.loads(line.split('=', 1)[1]) for line in args.native_output.read_text(encoding='utf-8-sig').splitlines() if line.startswith('authored_extent=')]
    assert 1 <= len(records) <= 16 and len({r['source']['Path'] for r in records}) == len(records)
    null_count = 0
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(args.root, args.configuration)]
        for record in records:
            source = record['source']
            for priority, index in indices:
                data = index.lookup(source['Path'])
                if data is not None:
                    break
            else:
                raise ValueError('Pinned extent source missing')
            assert priority == source['Priority'] and len(data) == source['Bytes'] and hashlib.sha256(data).hexdigest().upper() == source['Sha256']
            root, = nodes(data)
            assert root[0].decode() == record['family'] and root[0] in (b'MESH', b'DTLA')
            version, = root[1]
            appr = version[1][0]
            assert appr[0] == b'APPR'
            av, = appr[1]
            assert av[0] == b'0003' and len(av[1]) >= 2
            primary, collision = extent(av[1][0]), extent(av[1][1])
            same(primary, record['primary'])
            same(collision, record['collision'])
            null_count += collision is None
    print(json.dumps(dict(original_assets=True, sources=len(records), primary_values_match=True, secondary_values_match=True, null_secondary=null_count, mounts=len(indices), configuration_sha256=args.configuration_sha256.upper()), indent=2))


if __name__ == '__main__':
    main()
