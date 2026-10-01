"""Independent double affine oracle for the pinned original body/face bind-transform probe."""
import argparse
import contextlib
import hashlib
import json
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes


def multiply(a, b):
    return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def inverse(m):
    result = [[m[j][i] if i < 3 and j < 3 else float(i == j) for j in range(4)] for i in range(4)]
    for i in range(3): result[i][3] = -sum(result[i][j]*m[j][3] for j in range(3))
    return result


def quaternion(a, b):
    w, x, y, z = a; v, p, q, r = b
    return [w*v-x*p-y*q-z*r, w*p+x*v+y*r-z*q, w*q-x*r+y*v+z*p, w*r+x*q-y*p+z*v]


def rotation(q, t):
    w, x, y, z = q
    if w+1.0000000272452012e-27 >= 1:
        result = [[float(i == j) for j in range(4)] for i in range(4)]
    else:
        # Derived standard scalar-first quaternion action on column vectors.
        result = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w), 0],
                  [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w), 0],
                  [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y), 0], [0, 0, 0, 1]]
    for i in range(3): result[i][3] = t[i]
    return result


def skeletons(data):
    root, = nodes(data)
    if root[0] == b'SLOD':
        version, = root[1]; assert version[0] == b'0000'
        info, *levels = version[1]; assert len(levels) == struct.unpack('<h', info[1])[0]
    else: levels = [root]
    result = []
    for level in levels:
        assert level[0] == b'SKTM'; version, = level[1]; assert version[0] == b'0002'
        fields = dict(version[1]); count, = struct.unpack('<I', fields[b'INFO'])
        names = fields[b'NAME'].split(b'\0')[:-1]; assert len(names) == count
        parents = struct.unpack('<'+'i'*count, fields[b'PRNT'])
        arrays = {tag: list(struct.iter_unpack(fmt, fields[tag])) for tag, fmt in [(b'RPRE', '<4f'), (b'RPST', '<4f'), (b'BPTR', '<3f'), (b'BPRO', '<4f')]}
        result.append((names, parents, arrays))
    return result


def pose(skeleton, external=None):
    names, parents, arrays = skeleton
    external = external or [[float(i == j) for j in range(4)] for i in range(4)]
    result = []
    for i, parent in enumerate(parents):
        assert parent == -1 or 0 <= parent < i
        q = quaternion(quaternion(arrays[b'RPST'][i], arrays[b'BPRO'][i]), arrays[b'RPRE'][i])
        local = rotation(q, arrays[b'BPTR'][i])
        result.append(multiply(inverse(local), external if parent < 0 else result[parent]))
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path); p.add_argument('configuration', type=Path); p.add_argument('native_output', type=Path)
    p.add_argument('--configuration-sha256', required=True); a = p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper() == a.configuration_sha256.upper()
    assert a.native_output.stat().st_size <= 4*1048576
    sources, rows, descriptor = {}, {}, None
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('skeletal_descriptor_source='):
            row = json.loads(line.split('=', 1)[1]); sources[row['Path']] = row
        elif line.startswith('skeletal_descriptor='): descriptor = json.loads(line.split('=', 1)[1])
        elif line.startswith('skeletal_bind_pose='):
            row = json.loads(line.split('=', 1)[1]); key = (row['detail'], row['kind'], row['joint']); assert key not in rows; rows[key] = row['matrix']
    assert descriptor and rows
    references = descriptor['Appearance']['Skeletons']; raw = {}
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(a.root, a.configuration)]
        for ref in references:
            path = ref['Path']; pin = sources[path]
            for priority, index in indices:
                data = index.lookup(path)
                if data is None: continue
                assert priority == pin['Priority'] and len(data) == pin['Bytes'] and hashlib.sha256(data).hexdigest().upper() == pin['Sha256']
                raw[path] = skeletons(data); break
            else: raise ValueError('Missing skeleton')
    body, face = raw[references[0]['Path']], raw[references[1]['Path']][0]
    attachment = references[1]['AttachmentTransform'].encode('ascii').lower()
    expected = {}
    for detail, skeleton in enumerate(body[:4]):
        b = pose(skeleton); head, = [i for i, name in enumerate(skeleton[0]) if name.lower() == attachment]
        f = pose(face, b[head])
        for kind, matrices in [('body', b), ('face', f)]:
            for joint, m in enumerate(matrices): expected[detail, kind, joint] = [v for row in m[:3] for v in row]
    assert set(rows) == set(expected)
    maximum = max(abs(v-w) for key in rows for v, w in zip(rows[key], expected[key]))
    assert all(len(m) == 12 for m in rows.values()) and maximum < 2e-5
    print(json.dumps(dict(body_details=len(body[:4]), matrices=len(rows), maximum_component_error=maximum,
                          tolerance=2e-5, agreement=True, original_runtime_execution=False), indent=2))


if __name__ == '__main__': main()
