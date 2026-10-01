"""Compare bounded original mesh collision predicates with native candidate output.

Reads unchanged winning assets; emits aggregates, never asset payloads. All effect
candidates are inspected independently; this does not select GPU capabilities.
"""
import argparse
import collections
import contextlib
import hashlib
import json
import struct
from pathlib import Path

from cmpa_corpus_inventory import archives
from verify_static_mesh_slice import nodes, inspect
from verify_textured_mesh_bundle import Index, name


def predicates(effect):
    root, = nodes(effect)
    assert root[0] == b'EFCT'
    version, = root[1]
    assert version[0] == b'0001'
    result = []
    for tag, children in version[1][1:]:
        assert tag == b'IMPL'
        implementation, = children
        version_tag, parts = implementation
        assert version_tag in (b'0004', b'0005', b'0006', b'0007')
        data, = [payload for tag, payload in parts if tag == b'DATA']
        passes = [children for tag, children in parts if tag == b'PASS']
        assert len(data) == (5 if version_tag == b'0004' else 7) and data[0] == len(passes)
        opaque = False
        if passes:
            first, = passes[0]
            assert first[0] in (b'0008', b'0009')
            payload = first[1][0][1]
            assert payload[7] <= 1 and payload[11] <= 1
            opaque = payload[7] == 0 and payload[11] == 0
        one = opaque if version_tag == b'0004' else data[6] != 0
        result.append((version_tag.decode('ascii'), opaque, one))
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path)
    p.add_argument('configuration', type=Path)
    p.add_argument('source_manifest', type=Path)
    p.add_argument('native_output', type=Path)
    p.add_argument('--configuration-sha256', required=True)
    a = p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper() == a.configuration_sha256.upper()
    assert a.source_manifest.stat().st_size <= 262144 and a.native_output.stat().st_size <= 4194304
    records = json.loads(a.source_manifest.read_text(encoding='utf-8'))
    pins = {r['Path']: r for r in records}
    meshes = sorted(path for path in pins if path.startswith('appearance/mesh/') and path.endswith('.msh'))
    assert 1 <= len(meshes) <= 3
    actual = []
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('portal_collision_predicate='):
            actual.append(json.loads(line.split('=', 1)[1]))
    expected = []
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(a.root, a.configuration)]
        cache = {}
        def read(path):
            if path not in cache:
                for priority, index in indices:
                    payload = index.lookup(path)
                    if payload is not None:
                        pin = pins[path]
                        assert priority == pin['Priority'] and len(payload) == pin['Bytes'] and hashlib.sha256(payload).hexdigest().upper() == pin['Sha256']
                        cache[path] = payload
                        break
                else:
                    raise ValueError('Required source missing')
            return cache[path]
        for mesh in meshes:
            payload = read(mesh)
            inspect(payload)
            root, = nodes(payload); version, = root[1]; sps_version, = version[1][1][1]
            primitive_index = 0
            for _, children in sps_version[1][1:]:
                shader = name(children[0][1]); shader_root, = nodes(read(shader))
                if shader_root[0] == b'CSHD':
                    customization, = shader_root[1]
                    shader_root = customization[1][0]
                assert shader_root[0] == b'SSHT'
                shader_version, = shader_root[1]
                effect_name, = [value for tag, value in shader_version[1] if tag == b'NAME']
                effect = name(effect_name)
                if '/' not in effect: effect = 'effect/' + effect
                candidates = predicates(read(effect))
                for _, primitive in children[2:]:
                    count, = struct.unpack_from('<I', primitive[2][1])
                    for i, (v, opaque, one) in enumerate(candidates):
                        expected.append(dict(mesh=mesh, primitive=primitive_index, shader=shader, effect=effect, implementation=i, version=v, opaque=opaque, selector_one=one, triangles=count // 3))
                    primitive_index += 1
    key = lambda r: (r['mesh'], r['primitive'], r['implementation'])
    assert sorted(actual, key=key) == sorted(expected, key=key), 'Native/reference candidate mismatch'
    by_primitive = collections.defaultdict(list)
    for row in expected: by_primitive[(row['mesh'], row['primitive'])].append(row)
    summary = {}
    for mesh in meshes:
        counts = collections.Counter()
        for (path, _), candidates in by_primitive.items():
            if path != mesh: continue
            flags = {(r['opaque'], r['selector_one']) for r in candidates}
            assert len(flags) == 1, 'Candidate selection is materially ambiguous'
            r = candidates[0]
            counts['primitives'] += 1; counts['triangles'] += r['triangles']
            for flag in ('opaque', 'selector_one'):
                if r[flag]: counts[flag + '_primitives'] += 1; counts[flag + '_triangles'] += r['triangles']
        summary[mesh] = dict(counts)
    print(json.dumps(dict(configuration_sha256=a.configuration_sha256.upper(), mounts=len(indices), matched_candidates=len(expected), read_sources=len(cache), meshes=summary), indent=2))


if __name__ == '__main__': main()
