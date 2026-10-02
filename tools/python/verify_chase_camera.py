"""Independent original camera-boom audit using convex distance minimization.

Room triangles are independently decoded from the effective original assets.
Door boxes are supplied owned live snapshots, not a reimplemented controller.
"""
import math
import numpy as np
from verify_triangle_sphere_sweeps import earliest as triangle_entry
from verify_solid_box_sphere import earliest as box_entry


def verify(rows, door_rows, triangles):
    assert len(rows) == 360 and len(door_rows) == 2 and len(triangles) == 42986
    geometry = np.array(triangles)
    low, high = geometry.min(axis=1), geometry.max(axis=1)
    doors = {r['transformed']: r['doors'] for r in door_rows}
    radius, padding = float(np.float32(.12)), float(np.float32(.025))
    zoom = [0, .5, .75, 1.25, 2, 3, 4, 5, 6, 7.25, 8.5, 9.75, 11, 12.5, 14]
    keys = set(); maximum = 0; tested = 0; blocked = 0
    for row in rows:
        key = row['transformed'], row['yaw'], row['pitch'], row['setting']
        assert key not in keys; keys.add(key)
        target, desired, direction, position = [np.array(row[k]) for k in ('target', 'desired', 'direction', 'position')]
        expected_direction = np.array([math.sin(row['yaw'])*math.cos(row['pitch']), math.sin(row['pitch']), math.cos(row['yaw'])*math.cos(row['pitch'])])
        rotation = np.array([[0, 0, 1], [0, 1, 0], [-1, 0, 0]]) if row['transformed'] else np.eye(3)
        translation = np.array([20, 4, -9]) if row['transformed'] else np.zeros(3)
        assert np.max(abs(direction-rotation@expected_direction)) < 2e-6
        assert row['requested'] == zoom[row['setting']]
        assert np.max(abs(desired-(target-direction*row['requested']))) < 1e-5
        assert np.max(abs(position-(target-direction*row['resolved']))) < 1e-5
        assert 0 <= row['resolved'] <= row['requested']
        a, b = rotation.T@(target-translation), rotation.T@(desired-translation)
        candidates = np.flatnonzero(np.all(high >= np.minimum(a, b)-radius-1e-5, axis=1) & np.all(low <= np.maximum(a, b)+radius+1e-5, axis=1))
        contact = None
        for index in candidates:
            parameter = triangle_entry(dict(start=a.tolist(), end=b.tolist(), radius=radius, triangle=geometry[index].tolist()))
            tested += 1
            if parameter is not None and (contact is None or parameter < contact):
                contact = parameter
        for door in doors[row['transformed']]:
            parameter = box_entry(dict(door, start=target.tolist(), end=desired.tolist(), radius=radius))
            if parameter is not None and (contact is None or parameter < contact):
                contact = parameter
        expected = row['requested'] if contact is None else max(0, contact*row['requested']-padding)
        maximum = max(maximum, abs(expected-row['resolved']))
        assert abs(expected-row['resolved']) < 2e-4, (key, expected, row)
        assert (contact is None) == (row['obstacle'] == 'None')
        assert (contact == 0) == row['InitialOverlap']
        blocked += contact is not None
    return dict(queries=len(rows), independent_triangle_distance_queries=tested, blocked=blocked,
                maximum_distance_error=maximum, tolerance=.0002, original_room_geometry=True,
                live_door_snapshot=True, independent_dynamic_controller=False)
