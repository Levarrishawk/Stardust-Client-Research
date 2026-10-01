"""Independently resolve a pinned original deterministic creature template profile."""
import argparse
import contextlib
import hashlib
import json
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes


def text(data):
    assert data.endswith(b'\0') and b'\0' not in data[:-1]
    return data[:-1].decode('ascii')


def parse(root):
    layers = {}
    paths = set()
    current = root
    while True:
        family, children = current
        assert family in (b'SCOT', b'STOT', b'SHOT') and family.decode() not in layers
        offset = 0
        parent = None
        if children[0][0] == b'DERV':
            record, = children[0][1]
            assert record[0] == b'XXXX'
            parent = text(record[1]) or None
            offset = 1
        paths.add(parent)
        version, fields = children[offset]
        assert fields[0][0] == b'PCNT' and struct.unpack('<I', fields[0][1])[0] == len(fields)-1
        parameters = {}
        for tag, raw in fields[1:]:
            assert tag == b'XXXX'
            name, value = raw.split(b'\0', 1)
            name = name.decode('ascii')
            assert name not in parameters
            parameters[name] = value
        layers[family.decode()] = (version.decode(), parameters)
        if family == b'SHOT':
            assert len(children) == offset+1
            break
        assert len(children) == offset+2
        current = children[-1]
    assert len(paths) == 1
    return paths.pop(), layers


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    parser.add_argument('configuration', type=Path)
    parser.add_argument('native_output', type=Path)
    parser.add_argument('--configuration-sha256', required=True)
    parser.add_argument('--expected-root-sha256', required=True)
    args = parser.parse_args()
    assert hashlib.sha256(args.configuration.read_bytes()).hexdigest().upper() == args.configuration_sha256.upper()
    assert args.native_output.stat().st_size <= 1048576
    sources = {}
    profile = None
    for line in args.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('creature_template_source='):
            row = json.loads(line.split('=', 1)[1]); assert row['Path'] not in sources; sources[row['Path']] = row
        elif line.startswith('creature_template_profile='):
            assert profile is None; profile = json.loads(line.split('=', 1)[1])
    assert profile and 1 <= len(sources) <= 16 and sources[profile['RootPath']]['Sha256'] == args.expected_root_sha256.upper()
    chain = []
    path = profile['RootPath']
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(archive.open('rb')))) for priority, archive in archives(args.root, args.configuration)]
        while path:
            assert path not in [entry[0] for entry in chain] and len(chain) < 16
            for priority, index in indices:
                data = index.lookup(path)
                if data is not None:
                    break
            else:
                raise ValueError('Inherited template missing')
            pin = sources[path]
            assert priority == pin['Priority'] and len(data) == pin['Bytes'] and hashlib.sha256(data).hexdigest().upper() == pin['Sha256']
            root, = nodes(data)
            parent, layers = parse(root)
            chain.append((path, layers))
            path = parent
    assert len(chain) == len(sources)
    mapping = dict(StepHeight='stepHeight', CollisionHeight='collisionHeight', Radius='collisionRadius', OffsetX='collisionOffsetX', OffsetZ='collisionOffsetZ', Length='collisionLength', WarpTolerance='warpTolerance')
    resolved = {}; provenance = {}
    for output, name in mapping.items():
        for source, layers in chain:
            if 'SCOT' not in layers or name not in layers['SCOT'][1]:
                continue
            version, parameters = layers['SCOT']; raw = parameters[name]
            if raw == b'\0 ':
                continue
            assert len(raw) == 6 and raw[:2] == b'\1 '
            value, = struct.unpack('<f', raw[2:])
            assert struct.pack('<f', profile['Collision'][output]) == raw[2:]
            resolved[output] = value
            provenance[name] = dict(SourcePath=source, Family='SCOT', Version=version)
            break
        else:
            raise ValueError('Unresolved deterministic collision field')
    for source, layers in chain:
        version, parameters = layers['SHOT']
        raw = parameters.get('appearanceFilename', b'\0')
        if raw == b'\0':
            continue
        assert raw[:1] == b'\1'
        assert text(raw[1:]).lower().replace('\\', '/') == profile['AppearancePath']
        provenance['appearanceFilename'] = dict(SourcePath=source, Family='SHOT', Version=version)
        break
    else:
        raise ValueError('Unresolved appearance')
    assert provenance == profile['Fields']
    center = [resolved['OffsetX'], resolved['StepHeight']+resolved['Radius'], resolved['OffsetZ']]
    assert struct.pack('<3f', *center) == struct.pack('<3f', *profile['sphere']['center']) and struct.pack('<f', resolved['Radius']) == struct.pack('<f', profile['sphere']['Radius'])
    print(json.dumps(dict(original_assets=True, sources=len(chain), source_bytes=sum(r['Bytes'] for r in sources.values()), fields=len(provenance), collision=resolved, appearance=profile['AppearancePath'], sphere_bottom=center[1]-resolved['Radius'], mounts=len(indices), values_and_provenance_match=True), indent=2))


if __name__ == '__main__':
    main()
