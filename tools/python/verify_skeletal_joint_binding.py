"""Independent original-byte joint mapping and double linear-blend position diagnostic oracle.

The diagnostic pose/deformation policy is modern; this does not execute original animation.
"""
import argparse
import contextlib
import hashlib
import json
import math
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes
from verify_skeletal_descriptors import sat, strings
from verify_skeleton_bind_pose import skeletons, quaternion, rotation, multiply, pose


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path); p.add_argument('configuration', type=Path); p.add_argument('native_output', type=Path)
    p.add_argument('--configuration-sha256', required=True); a = p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper() == a.configuration_sha256.upper()
    assert a.native_output.stat().st_size <= 4*1048576
    sources, bindings, mappings, probes, descriptor, shader_probes = {}, {}, {}, {}, None, {}
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if '=' not in line: continue
        tag, value = line.split('=', 1)
        if tag not in {'skeletal_descriptor_source', 'skeletal_descriptor', 'skeletal_joint_binding', 'skeletal_transform_binding', 'skeletal_position_probe', 'skeletal_shader_probe'}: continue
        r = json.loads(value)
        if tag == 'skeletal_descriptor_source':
            assert r['Path'] not in sources; sources[r['Path']] = r
        elif tag == 'skeletal_descriptor':
            assert descriptor is None; descriptor = r
        elif tag == 'skeletal_joint_binding':
            assert r['joint'] not in bindings; bindings[r['joint']] = r
        elif tag == 'skeletal_transform_binding':
            assert r['path'] not in mappings; mappings[r['path']] = r['joints']
        elif tag == 'skeletal_shader_probe':
            key = (r['path'], r['mode'], r['shader'], r['vertex']); assert key not in shader_probes; shader_probes[key] = r
        else:
            key = (r['path'], r['mode'], r['position']); assert key not in probes; probes[key] = r['value']
    assert descriptor and len(sources) == 12 and set(descriptor['counts']) == set(sources) and set(descriptor['counts'].values()) == {1}
    raw = {}
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(a.root, a.configuration)]
        for path, pin in sources.items():
            for priority, index in indices:
                data = index.lookup(path)
                if data is None: continue
                assert priority == pin['Priority'] and len(data) == pin['Bytes'] and hashlib.sha256(data).hexdigest().upper() == pin['Sha256']
                raw[path] = data; break
            else: raise ValueError('Missing pinned binding source')
    appearance = sat(raw[descriptor['AppearancePath']]); assert appearance == descriptor['Appearance']
    joints, matrix_errors = [], []
    for reference in appearance['Skeletons']:
        path = reference['Path']; basic = skeletons(raw[path])[0]; names, parents, arrays = basic
        attach = reference['AttachmentTransform'].lower()
        parent = -1
        if attach:
            parent, = [i for i, joint in enumerate(joints) if joint['name'].lower() == attach]
        inverses = pose(basic, None if parent < 0 else joints[parent]['inverse']); start = len(joints)
        for i, name in enumerate(names):
            q = quaternion(quaternion(arrays[b'RPST'][i], arrays[b'BPRO'][i]), arrays[b'RPRE'][i])
            local = rotation(q, arrays[b'BPTR'][i]); global_parent = parent if parents[i] < 0 else start+parents[i]
            world = local if global_parent < 0 else multiply(joints[global_parent]['world'], local)
            joint = dict(path=path, source=i, parent=global_parent, name=name.decode('ascii'), world=world, inverse=inverses[i]); index = len(joints)
            native = bindings[index]
            assert all(native[key] == joint[key] for key in ('path', 'source', 'parent', 'name'))
            for key in ('world', 'inverse'):
                expected = [v for row in joint[key][:3] for v in row]; assert len(native[key]) == 12
                matrix_errors.extend(abs(v-w) for v, w in zip(native[key], expected))
            joints.append(joint)
    assert set(bindings) == set(range(len(joints))) and len(joints) == 53
    maximum = 0.; bind_error = 0.; expected_keys = set()
    expected_paths = {'appearance/mesh/hum_m_body_l0.mgn', 'appearance/mesh/hum_m_head_l0.mgn'}
    assert set(mappings) == expected_paths
    for path in expected_paths:
        root, = nodes(raw[path]); version, = root[1]; fields = dict(version[1][:6]); info = struct.unpack('<9i4h', fields[b'INFO'])
        paths = strings(fields[b'SKTM'], info[2]); names = strings(fields[b'XFNM'], info[3])
        mapping = []
        for name in names:
            match, = [i for i, joint in enumerate(joints) if joint['path'] in paths and joint['name'].lower() == name.lower()]; mapping.append(match)
        assert mappings[path] == mapping
        positions = list(struct.iter_unpack('<3f', fields[b'POSN'])); counts = struct.unpack('<'+'i'*len(positions), fields[b'TWHD']); weights = list(struct.iter_unpack('<If', fields[b'TWDT']))
        assert sum(counts) == len(weights)
        for mode in ('bind', 'root-rigid', 'split'):
            worlds = [j['world'] for j in joints]
            if mode == 'root-rigid':
                c, s = math.cos(.4), math.sin(.4); motion = [[c,0,s,.3],[0,1,0,-.2],[-s,0,c,.7],[0,0,0,1]]
                worlds = [multiply(motion, world) for world in worlds]
            elif mode == 'split':
                worlds = [[[v+(i%3)*.01 if r == 0 and col == 3 else v+(i%5)*-.02 if r == 1 and col == 3 else v for col, v in enumerate(row)] for r, row in enumerate(world)] for i, world in enumerate(worlds)]
            palette = [multiply(worlds[i], joints[i]['inverse']) for i in mapping]; cursor = 0
            for index, point in enumerate(positions):
                result = [0., 0., 0.]
                for transform, weight in weights[cursor:cursor+counts[index]]:
                    matrix = palette[transform]
                    for component in range(3): result[component] += weight*(sum(matrix[component][k]*point[k] for k in range(3))+matrix[component][3])
                cursor += counts[index]; key = (path, mode, index); expected_keys.add(key)
                assert len(probes[key]) == 3
                maximum = max(maximum, *(abs(v-w) for v, w in zip(probes[key], result)))
                if mode == 'bind': bind_error = max(bind_error, *(abs(v-w) for v, w in zip(probes[key], point)))
    assert set(probes) == expected_keys and maximum < 2e-5 and max(matrix_errors) < 2e-5 and bind_error < 2e-5
    shader_error = verify_shaders(raw, joints, mappings, shader_probes) if shader_probes else None
    print(json.dumps(dict(sources=len(sources), joints=len(joints), transform_bindings=sum(len(v) for v in mappings.values()),
                          position_checks=len(probes), maximum_matrix_error=max(matrix_errors), maximum_position_error=maximum,
                          shader_vertex_checks=len(shader_probes), maximum_shader_component_error=shader_error,
                          maximum_bind_identity_error=bind_error, tolerance=2e-5, agreement=True, original_animation_execution=False), indent=2))


def verify_shaders(raw, joints, mappings, probes):
    maximum, expected_keys = 0., set()
    for path, mapping in mappings.items():
        root, = nodes(raw[path]); version, = root[1]; fields = dict(version[1][:6]); positions = list(struct.iter_unpack('<3f', fields[b'POSN']))
        counts = struct.unpack('<'+'i'*len(positions), fields[b'TWHD']); weights = list(struct.iter_unpack('<If', fields[b'TWDT']))
        starts, cursor = [], 0
        for count in counts: starts.append(cursor); cursor += count
        chunks = dict(version[1]); normals = list(struct.iter_unpack('<3f', chunks[b'NORM']))
        tangents = list(struct.iter_unpack('<4f', chunks[b'DOT3'][4:])) if b'DOT3' in chunks else []
        shaders = [dict(fields) for tag, fields in version[1] if tag == b'PSDT']
        for mode in ('bind', 'root-rigid', 'split-rotation'):
            worlds = []
            for i, joint in enumerate(joints):
                motion = [[float(r == c) for c in range(4)] for r in range(4)]
                if mode == 'root-rigid':
                    c, s = math.cos(.4), math.sin(.4); motion = [[c,0,s,.3],[0,1,0,-.2],[-s,0,c,.7],[0,0,0,1]]
                elif mode == 'split-rotation':
                    c, s = math.cos((i%3)*.15), math.sin((i%3)*.15); motion = [[c,-s,0,0],[s,c,0,(i%5)*-.02],[0,0,1,0],[0,0,0,1]]
                worlds.append(multiply(motion, joint['world']))
            palette = [multiply(worlds[i], joints[i]['inverse']) for i in mapping]
            for si, shader in enumerate(shaders):
                pmap = list(struct.iter_unpack('<I', shader[b'PIDX'][4:])); nmap = list(struct.iter_unpack('<I', shader[b'NIDX']))
                tmap = list(struct.iter_unpack('<I', shader[b'DOT3'])) if b'DOT3' in shader else None
                assert len(pmap) == len(nmap) and (tmap is None or len(tmap) == len(pmap))
                for vertex, ((position,), (normal,)) in enumerate(zip(pmap, nmap)):
                    group = weights[starts[position]:starts[position]+counts[position]]
                    def deform(vector, translation):
                        return [sum(weight*(sum(palette[index][component][k]*vector[k] for k in range(3))+(palette[index][component][3] if translation else 0)) for index, weight in group) for component in range(3)]
                    expected = dict(position=deform(positions[position], True), normal=deform(normals[normal], False), tangent=None)
                    if tmap is not None:
                        tangent = tangents[tmap[vertex][0]]; expected['tangent'] = deform(tangent, False)+[tangent[3]]
                    key = (path, mode, si, vertex); expected_keys.add(key); native = probes[key]
                    for name, values in expected.items():
                        if values is None: assert native[name] is None; continue
                        assert len(values) == len(native[name]); maximum = max(maximum, *(abs(v-w) for v, w in zip(native[name], values)))
    assert set(probes) == expected_keys and len(probes) == 5370 and maximum < 2e-5
    return maximum


if __name__ == '__main__': main()
