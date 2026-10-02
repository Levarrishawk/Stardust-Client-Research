"""Audit authored triangle orientation against stored normals; retain original index order.

This is a geometry observation, not proof that every stored normal defines an
outward surface. Z reflection changes the cross/normal sign without an index swap.
"""
import struct
import numpy as np
from verify_static_mesh_slice import nodes


def inspect(data):
    root, = nodes(data)
    version, = root[1]
    triangles = []
    if root[0] == b'SKMG':
        fields = dict(version[1])
        positions = np.array(list(struct.iter_unpack('<3f', fields[b'POSN'])))
        normals = np.array(list(struct.iter_unpack('<3f', fields[b'NORM'])))
        for tag, records in version[1]:
            if tag != b'PSDT':
                continue
            fields = dict(records)
            pmap = [v for v, in struct.iter_unpack('<I', fields[b'PIDX'][4:])]
            nmap = [v for v, in struct.iter_unpack('<I', fields[b'NIDX'])]
            for kind, raw in fields[b'PRIM'][1:]:
                for record in struct.iter_unpack('<h3I' if kind == b'OITL' else '<3I', raw[4:]):
                    indices = record[-3:]
                    triangles.append((positions[[pmap[i] for i in indices]], normals[[nmap[i] for i in indices]]))
    elif root[0] == b'MESH':
        sps, = version[1][1][1]
        for _, group in sps[1][1:]:
            for primitive_version, primitive in group[2:]:
                _, vtxa, indx, *_ = primitive
                vv, = vtxa[1]
                vi, vd = vv[1]
                _, count = struct.unpack('<II', vi[1])
                stride = len(vd[1]) // count
                vertices = np.array([struct.unpack_from('<6f', vd[1], i*stride) for i in range(count)])
                number, = struct.unpack_from('<I', indx[1])
                indices = struct.unpack_from('<'+('I' if primitive_version == b'0000' else 'H')*number, indx[1], 4)
                for i in range(0, number, 3):
                    selected = vertices[[v & 65535 for v in indices[i:i+3]]]
                    triangles.append((selected[:, :3], selected[:, 3:]))
    else:
        raise ValueError('Unsupported geometry')
    positions = np.array([p for p, _ in triangles])
    normals = np.array([n for _, n in triangles]).sum(axis=1)
    crosses = np.cross(positions[:, 1]-positions[:, 0], positions[:, 2]-positions[:, 0])
    dots = (crosses*normals).sum(axis=1)
    reflected = positions*np.array([1, 1, -1])
    reflected_cross = np.cross(reflected[:, 1]-reflected[:, 0], reflected[:, 2]-reflected[:, 0])
    reflected_dots = (reflected_cross*normals*np.array([1, 1, -1])).sum(axis=1)
    assert np.allclose(reflected_dots, -dots, rtol=0, atol=1e-12)
    return dict(triangles=len(triangles), positive=int((dots > 1e-12).sum()), negative=int((dots < -1e-12).sum()),
                near_zero=int((abs(dots) <= 1e-12).sum()), reflected_sign_inverts_without_index_swap=True)


def verify(raw):
    results = {path: inspect(data) for path, data in raw.items() if path.endswith(('.mgn', '.msh'))}
    assert results
    for path in ('appearance/mesh/hum_m_body_l0.mgn', 'appearance/mesh/hum_m_head_l0.mgn'):
        if path in results:
            assert results[path]['positive'] > .99*results[path]['triangles']
    return results
