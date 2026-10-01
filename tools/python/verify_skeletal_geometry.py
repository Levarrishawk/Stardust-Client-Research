"""Audit original MGN shader maps, triangles, UVs, tangents and retained texture-renderer bindings."""
import argparse
import contextlib
import hashlib
import json
from pathlib import Path
import struct
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes


def inspect(data):
    root, = nodes(data); assert root[0] == b'SKMG'
    version, = root[1]; assert version[0] == b'0004'
    parts = version[1]; prefix = dict(parts[:7]); positions = list(struct.iter_unpack('<3f', prefix[b'POSN']))
    global_dot = [raw for tag, raw in parts[7:] if tag == b'DOT3']
    assert len(global_dot) <= 1
    tangents = global_dot[0] if global_dot else struct.pack('<I', 0)
    tangent_count, = struct.unpack_from('<I', tangents); assert len(tangents) == 4+tangent_count*16
    shaders, source_count, degenerate = [], 0, 0
    for tag, records in parts:
        if tag != b'PSDT': continue
        fields = dict(records); assert len(fields) == len(records)
        count, = struct.unpack_from('<I', fields[b'PIDX']); mapping = struct.unpack('<'+'I'*count, fields[b'PIDX'][4:])
        primitive = fields[b'PRIM']; pcount, = struct.unpack('<I', primitive[0][1]); assert pcount == len(primitive)-1
        serialized, retained = [], []
        for kind, raw in primitive[1:]:
            assert kind in (b'ITL ', b'OITL'); number, = struct.unpack_from('<I', raw)
            fmt = '<h3I' if kind == b'OITL' else '<3I'; stride = struct.calcsize(fmt); assert len(raw) == 4+number*stride
            for record in struct.iter_unpack(fmt, raw[4:]):
                zone, indices = (record[0], record[1:]) if kind == b'OITL' else (None, record)
                assert all(i < count for i in indices)
                serialized.append((source_count, zone, indices))
                a, b, c = [positions[mapping[i]] for i in indices]
                u = [b[j]-a[j] for j in range(3)]; v = [c[j]-a[j] for j in range(3)]
                cross = [u[1]*v[2]-u[2]*v[1], u[2]*v[0]-u[0]*v[2], u[0]*v[1]-u[1]*v[0]]
                if any(cross): retained.append(source_count)
                else: degenerate += 1
                source_count += 1
        shaders.append((fields, count, serialized, retained))
    canonical = struct.pack('<5i', source_count, degenerate, source_count-degenerate, len(shaders), tangent_count)+tangents[4:]
    for fields, count, serialized, retained in shaders:
        canonical += fields[b'NAME']+struct.pack('<i', count)+fields[b'PIDX'][4:]
        for tag in (b'NIDX', b'DOT3', b'VDCL'):
            raw = fields.get(tag); canonical += struct.pack('<?', raw is not None)
            if raw is not None:
                assert len(raw) == count*4; canonical += raw
        if b'TXCI' in fields:
            dimensions = list(struct.iter_unpack('<I', fields[b'TXCI']))
            sets = dimensions[0][0]; dimensions = [v[0] for v in dimensions[1:]]; assert len(dimensions) == sets
            records = fields[b'TCSF']; assert len(records) == sets
            canonical += struct.pack('<i', sets)
            for dimension, (tag, raw) in zip(dimensions, records):
                assert tag == b'TCSD' and len(raw) == count*dimension*4
                canonical += struct.pack('<i', dimension)+raw
        else: canonical += struct.pack('<i', 0)
        canonical += struct.pack('<i', len(serialized))
        for index, zone, indices in serialized:
            canonical += struct.pack('<i?', index, zone is not None)
            if zone is not None: canonical += struct.pack('<h', zone)
            canonical += struct.pack('<3I', *indices)
        canonical += struct.pack('<i', len(retained))+b''.join(struct.pack('<i', i) for i in retained)
    trts = [records for tag, records in parts if tag == b'TRTS']; assert len(trts) <= 1
    renderers = 0
    if trts:
        info, *records = trts[0]; assert info[0] == b'INFO'
        renderers, total = struct.unpack('<2I', info[1]); assert renderers == len(records)
        canonical += struct.pack('<i', renderers); used = 0
        for tag, raw in records:
            assert tag == b'TRT '; end = raw.index(0)+1; count, = struct.unpack_from('<I', raw, end)
            assert len(raw) == end+4+count*8
            for shader, texture_tag in struct.iter_unpack('<2I', raw[end+4:]): assert shader < len(shaders)
            used += count; canonical += raw
        assert used == total
    else: canonical += struct.pack('<i', 0)
    return dict(source=source_count, degenerate=degenerate, triangles=source_count-degenerate,
                vertices=sum(s[1] for s in shaders), shaders=len(shaders), tangents=tangent_count,
                renderers=renderers, sha256=hashlib.sha256(canonical).hexdigest().upper())


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path); p.add_argument('configuration', type=Path); p.add_argument('native_output', type=Path)
    p.add_argument('--configuration-sha256', required=True); a = p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper() == a.configuration_sha256.upper()
    assert a.native_output.stat().st_size <= 4*1048576
    sources, rows = {}, {}
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('skeletal_descriptor_source='):
            r = json.loads(line.split('=', 1)[1]); sources[r['Path']] = r
        elif line.startswith('skeletal_geometry='):
            r = json.loads(line.split('=', 1)[1]); assert r['path'] not in rows; rows[r['path']] = r
    assert rows and set(rows) == {path for path in sources if path.endswith('.mgn')}
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(a.root, a.configuration)]
        for path, row in rows.items():
            pin = sources[path]
            for priority, index in indices:
                data = index.lookup(path)
                if data is None: continue
                assert priority == pin['Priority'] and len(data) == pin['Bytes'] and hashlib.sha256(data).hexdigest().upper() == pin['Sha256']
                assert row == dict(path=path, **inspect(data)); break
            else: raise ValueError('Missing original MGN geometry')
    print(json.dumps(dict(meshes=len(rows), vertices=sum(r['vertices'] for r in rows.values()),
                          source_triangles=sum(r['source'] for r in rows.values()), degenerate_triangles=sum(r['degenerate'] for r in rows.values()),
                          triangles=sum(r['triangles'] for r in rows.values()), shaders=sum(r['shaders'] for r in rows.values()),
                          texture_renderer_references=sum(r['renderers'] for r in rows.values()), agreement=True), indent=2))


if __name__ == '__main__': main()
