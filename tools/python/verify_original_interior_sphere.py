"""Audit original sphere-scene source identity, inherited dimensions and every recorded floor locator.

This checks frame invariants, not an independent dynamic door-controller implementation.
"""
import argparse
import collections
import contextlib
import hashlib
import json
from pathlib import Path
import struct

from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_static_mesh_slice import nodes, indexed_triangles
from verify_triangle_box_sweeps import cross
from verify_creature_template_profile import parse
from verify_floor_walks import f32, project
from verify_portal_floor_boxes import transform32
from verify_skeletal_geometry import inspect as inspect_character_geometry


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path); p.add_argument('configuration', type=Path); p.add_argument('native_output', type=Path)
    p.add_argument('--configuration-sha256', required=True); a = p.parse_args()
    assert hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper() == a.configuration_sha256.upper()
    assert a.native_output.stat().st_size <= 8*1048576
    sources, templates, content, counts, guards, rows, volume = {}, {}, {}, None, {}, [], None
    characters = {}
    for line in a.native_output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith(('interior_sphere_source=', 'interior_sphere_template_source=')):
            target = templates if line.startswith('interior_sphere_template_source=') else sources
            r = json.loads(line.split('=', 1)[1]); assert r['Path'] not in target; target[r['Path']] = r
        elif line.startswith('interior_sphere_tick='):
            rows.append(json.loads(line.split('=', 1)[1]))
        elif line.startswith('interior_sphere_volume='):
            assert volume is None; volume = json.loads(line.split('=', 1)[1])
        elif line.startswith('interior_sphere_content_source='):
            r = json.loads(line.split('=', 1)[1]); assert r['Path'] not in content; content[r['Path']] = r
        elif line.startswith('interior_sphere_content_counts='):
            assert counts is None; counts = json.loads(line.split('=', 1)[1])
        elif line.startswith('interior_sphere_guard_geometry='):
            r = json.loads(line.split('=', 1)[1]); assert r['cell'] not in guards; guards[r['cell']] = r
        elif line.startswith('interior_character_geometry='):
            r = json.loads(line.split('=', 1)[1]); assert r['path'] not in characters; characters[r['path']] = r
    assert len(sources) == 9 and len(templates) == 5 and len(rows) == 2880 and volume
    if content:
        assert len(content) == (77 if characters else 70) and set(counts) == set(content) and set(counts.values()) == {1}
        assert all(content[path] == row for path, row in (sources | templates).items())
        assert sum(row['Bytes'] for row in content.values()) == (9943381 if characters else 9622375)
    floors, chain = {}, []
    with contextlib.ExitStack() as stack:
        indices = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in archives(a.root, a.configuration)]
        def read(path, registry):
            for priority, index in indices:
                data = index.lookup(path)
                if data is not None:
                    pin = registry[path]
                    assert priority == pin['Priority'] and len(data) == pin['Bytes'] and hashlib.sha256(data).hexdigest().upper() == pin['Sha256']
                    return data
            raise ValueError('Missing original scene input')
        for path in content or sources:
            data = read(path, content or sources)
            if path.endswith('.flr'):
                cell = next(c for c in (5, 6, 7) if '_r'+str(c)+'_' in path)
                root, = nodes(data); version, = root[1]
                positions, records = (entry[1] for entry in version[1][:2])
                if version[0] == b'0006':
                    positions, records = positions[4:], records[4:]
                floors[cell] = (list(struct.iter_unpack('<3f', positions)),
                                [struct.unpack_from('<3iI3i3f4Bi3i', records, i) for i in range(0, len(records), 60)])
        if characters:
            assert set(characters) == {'appearance/mesh/hum_m_body_l0.mgn', 'appearance/mesh/hum_m_head_l0.mgn'}
            for path, row in characters.items():
                independent = inspect_character_geometry(read(path, content))
                assert all(row[key] == independent[key] for key in ('source', 'degenerate', 'triangles', 'sha256'))
            assert sum(row['triangles'] for row in characters.values()) == 2810
        if guards:
            assert set(guards) == {5, 6, 7}
            paths = {5:'appearance/mesh/thm_must_mining_outpost_r5_entrance_room_01_msh_r5.msh',
                     6:'appearance/mesh/thm_must_mining_outpost_r6_connecting_hall_mesh_r6.msh',
                     7:'appearance/mesh/thm_must_mining_outpost_r7_entrance_room_02_mesh_r7.msh'}
            for cell, path in paths.items():
                raw = indexed_triangles(read(path, content)); selected = []
                for tid, points in raw:
                    u = [points[1][j]-points[0][j] for j in range(3)]; v = [points[2][j]-points[0][j] for j in range(3)]
                    if cross(u, v) != (0, 0, 0):
                        selected.append((tid, points))
                g = guards[cell]
                assert g['selector'] == 1 and g['source'] == g['eligible'] == len(raw) and g['degenerate'] == 0 and g['triangles'] == len(selected) == len(raw)
                canonical = struct.pack('<4i', len(raw), len(raw), 0, len(selected))
                for tid, points in selected:
                    canonical += struct.pack('<i9f', tid, *(v for point in points for v in point))
                assert hashlib.sha256(canonical).hexdigest().upper() == g['sha256']
        path = 'object/creature/player/shared_human_male.iff'
        while path:
            assert path not in [r[0] for r in chain]
            root, = nodes(read(path, templates)); parent, layers = parse(root)
            chain.append((path, layers)); path = parent
    assert len(chain) == 5 and set(floors) == {5, 6, 7}
    values = {}
    for name in ('stepHeight', 'collisionRadius', 'collisionOffsetX', 'collisionOffsetZ'):
        for _, layers in chain:
            raw = layers.get('SCOT', (None, {}))[1].get(name)
            if raw is None or raw == b'\0 ':
                continue
            assert len(raw) == 6 and raw[:2] == b'\1 '
            values[name], = struct.unpack('<f', raw[2:]); break
        assert name in values
    assert volume['center_offset'] == [values['collisionOffsetX'], values['stepHeight']+values['collisionRadius'], values['collisionOffsetZ']]
    assert volume['radius'] == values['collisionRadius'] == .5
    phases = collections.Counter(); statuses = collections.Counter(); contacts = transfers = held = 0
    for row in rows:
        local = [f32(v) for v in row['local']]; vertices, triangles = floors[row['cell']]
        assert 0 <= row['triangle'] < len(triangles) and 0 <= row['parameter'] <= 1 and row['transfers'] in (0, 1)
        points = [vertices[i] for i in triangles[row['triangle']][:3]]
        assert abs(project(points, local)[1]-local[1]) < 1e-4
        pose = [[0, 0, 1], [0, 1, 0], [-1, 0, 0], [20, 4, -9]] if row['transformed'] else [[1, 0, 0], [0, 1, 0], [0, 0, 1], [0, 0, 0]]
        assert transform32(local, pose) == [f32(v) for v in row['world']]
        assert row['diagnostic'] is None and len(row['doors']) == 2
        assert all(0 <= d['fraction'] <= 1 and 0 <= d['retained'] <= 1 for d in row['doors'])
        if row['phase'] in ('deny', 'retreat', 'unknown'):
            assert row['cell'] in (5, 6)
        if row['phase'] in ('forward', 'recover') and row['index'] == 199:
            assert row['cell'] == 7 and row['status'] == 'Accepted'
        if row['phase'] in ('reverse', 'return') and row['index'] == 199:
            assert row['cell'] == 5 and row['status'] == 'Accepted'
        transfers += row['transfers']; contacts += row['kind'] is not None; held += any(d['held'] for d in row['doors'])
        phases[row['phase']] += 1; statuses[row['status']] += 1
    assert transfers == 24 and contacts == 182
    assert phases == {'forward':800,'reverse':800,'deny':400,'retreat':40,'unknown':40,'recover':400,'return':400}
    print(json.dumps(dict(original_assets=True, sources=len(sources)+len(templates), source_bytes=sum(r['Bytes'] for r in list(sources.values())+list(templates.values())),
                          content_sources=len(content), content_bytes=sum(r['Bytes'] for r in content.values()), content_external_reads_once=bool(content),
                          guard_triangles=sum(g['triangles'] for g in guards.values()), guard_geometry_independently_matches=bool(guards),
                          character_triangles=sum(g['triangles'] for g in characters.values()), character_geometry_independently_matches=bool(characters),
                          ticks=len(rows), floor_plane_checks=len(rows), transfers=transfers, contacts=contacts, closing_hold_ticks=held, phases=phases,
                          statuses=statuses, template_volume_matches=True, reflected_owner_positions_match=True, dynamic_controller_independently_reimplemented=False), indent=2))


if __name__ == '__main__':
    main()
