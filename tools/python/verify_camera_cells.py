"""Independent original portal-ray traversal for camera-cell fixtures.

Uses original PRTO geometry, Moller-Trumbore segment intersections and closest
triangle points for boundary ambiguity; does not execute the original client.
"""
import math
import struct
from verify_portal_layout import inspect
from verify_portal_visibility import hit, sub
from verify_triangle_sphere_sweeps import closest


def verify(data, rows):
    layout = inspect(data)
    geometry = {}
    for index, entry in enumerate(layout['geometries']):
        vertices = list(struct.iter_unpack('<3f', bytes.fromhex(entry['vertex_hex'])))
        indices = entry['indices']
        geometry[index] = [tuple(vertices[i] for i in indices[k:k+3]) for k in range(0, len(indices), 3)]
    assert len(rows) == 28
    counts = dict(located=0, changed=0, ambiguous=0); maximum = 0
    for row in rows:
        cell = row['anchorCell']; origin = row['anchor']; end = row['camera']; direction = sub(end, origin)
        previous = -1; crossings = []; ambiguous = False
        for _ in range(8):
            candidates = []
            for ref in layout['cells'][cell]['portals']:
                for triangle in geometry[ref['geometry_index']]:
                    if any(math.dist(point, closest(point, triangle)) <= 1e-5 for point in (origin, end)):
                        ambiguous = True; break
                    parameter = hit(origin, direction, triangle)
                    if parameter is not None and previous+1e-8 < parameter <= 1:
                        candidates.append((parameter, ref['geometry_index'], ref['target_cell']))
                if ambiguous:
                    break
            if ambiguous or not candidates:
                break
            parameter, index, target = min(candidates)
            crossings.append(dict(GeometryIndex=index, SourceCell=cell, TargetCell=target, Parameter=parameter))
            previous, cell = parameter, target
        else:
            raise AssertionError('Independent camera ray exceeded bounded fixture traversal')
        assert row['Status'] == (1 if ambiguous else 0)
        assert row['CellIndex'] == (None if ambiguous else cell)
        assert len(row['Crossings']) == len(crossings)
        for actual, expected in zip(row['Crossings'], crossings):
            assert all(actual[k] == expected[k] for k in ('GeometryIndex', 'SourceCell', 'TargetCell'))
            maximum = max(maximum, abs(actual['Parameter']-expected['Parameter']))
        counts['ambiguous' if ambiguous else 'located'] += 1
        counts['changed'] += not ambiguous and cell != row['anchorCell']
    assert counts == dict(located=26, changed=13, ambiguous=2) and maximum < 2e-5
    return dict(queries=len(rows), **counts, maximum_crossing_parameter_error=maximum,
                original_portal_geometry=True, actor_cell_changes=False)
