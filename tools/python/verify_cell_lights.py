"""Independent selected CELL/LGHT field and original loader yaw audit."""
import math
import struct
import numpy as np
from verify_static_mesh_slice import nodes


def verify(data, rows):
    root, = nodes(data); version, = root[1]
    cells = dict(version[1])[b'CELS']
    observed = {(r['cell'], r['index']): r for r in rows}
    assert len(observed) == len(rows) == 19
    angle = float(np.float32(math.pi)); sine = float(np.float32(math.sin(angle))); cosine = float(np.float32(math.cos(angle)))
    yaw = np.array([[cosine, 0, sine], [0, 1, 0], [-sine, 0, cosine]])
    maximum = 0; count = 0; directional = point = specular = 0
    def same(a, b):
        return struct.pack('<f', a) == struct.pack('<f', b)
    for cell in (5, 6, 7):
        entry, = cells[cell][1]
        payload, = [raw for tag, raw in entry[1] if tag == b'LGHT']
        number, = struct.unpack_from('<i', payload)
        assert len(payload) == 4+93*number
        for index in range(number):
            kind, *values = struct.unpack_from('<b23f', payload, 4+index*93)
            row = observed[cell, index]; assert row['kind'] == kind and row['RuntimeLightType'] == (3 if kind == 2 else kind)
            for key, expected in [('diffuse', [*values[1:4], values[0]]), ('specular', [*values[5:8], values[4]]),
                                  ('authored', values[8:20]), ('attenuation', values[20:23])]:
                assert len(row[key]) == len(expected) and all(same(a, b) for a, b in zip(row[key], expected))
            authored = np.array(values[8:20]).reshape(3, 4)
            runtime = np.column_stack((authored[:, :3]@yaw, authored[:, 3]))
            maximum = max(maximum, np.max(abs(runtime.flatten()-np.array(row['runtime'],dtype=np.float32).astype(float))), np.max(abs(-runtime[:, 2]-np.array(row['toward'],dtype=np.float32).astype(float))))
            has_specular = any(v != 0 for v in values[5:8]); assert row['HasSpecularRgb'] == has_specular
            count += 1; directional += kind == 1; point += kind == 2; specular += has_specular
    assert (count, directional, point, specular) == (19, 9, 10, 5) and maximum < 2e-6
    return dict(original_records=count, directional=directional, point=point, nonzero_specular_records=specular,
                original_hdr_fields_f32_equal=True, maximum_loader_yaw_error=float(maximum), runtime_lights_from_split=24)
