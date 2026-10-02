"""Independent double-precision audit of explicitly modern cell-light selection/math.
Input rows must first pass verify_cell_lights against the original POB.
"""
import argparse
import json
import numpy as np


def verify(lights, cases,legacy=False,slot_rows=()):
    maximum = 0.0
    def unit(v):
        return v / max(np.linalg.norm(v), 1e-6)
    f32 = lambda v: np.asarray(v, dtype=np.float32).astype(float)
    for row in cases:
        records = sorted((r for r in lights if r['cell'] == row['cell']), key=lambda r: r['index'])
        if legacy:
            slots=next(r for r in slot_rows if r['cell']==row['cell'] and r['baked']==row['baked'] and r['scaled']==row.get('scaledColors',False));primary=slots['specularDirectional']
            selected=[r for r in records if r['index'] in [slots['specularDirectional'],slots['specularPoint']]+slots['diffuseDirectional']+slots['diffusePoint']]
            specular_keys=[slots['specularDirectional'],slots['specularPoint']]
            records=[r|{'diffuse':[0,0,0,0] if row['baked'] else r['diffuse'],'specular':r['specular'] if r['index'] in specular_keys else [0,0,0,0]} for r in selected]
            assert primary==row['primary'] and row['additional']==len(records)-int(primary is not None)
        else:
            primary = next(r['index'] for r in records if r['kind'] == 1)
            assert primary == row['primary'] and row['additional'] == len(records)-1
        anchor = f32(next(r for r in lights if r['cell']==row['cell'] and r['kind'] == 2)['runtime']).reshape(3, 4)[:, 3]
        rotation = np.array([[0,0,1],[0,1,0],[-1,0,0]]) if row['rotated'] else np.eye(3)
        translation = np.array([20,4,-9]) if row['rotated'] else np.zeros(3)
        position = rotation @ (anchor+f32(row['offset']))+translation
        viewer = rotation @ (anchor+np.array([0,0,-5]))+translation
        normal = unit(rotation @ f32(row['localNormal']))
        diffuse = np.full(3, 0.0 if legacy and row['baked'] else float(np.float32(.1))); specular = np.zeros(3)
        for light in records:
            if light['index'] == primary and not row['includePrimary']:
                continue
            if light['kind'] == 0:
                diffuse += f32(light['diffuse'])[:3]
                continue
            point = light['kind'] == 2
            pose = f32(light['runtime']).reshape(3,4)
            delta = rotation @ pose[:,3]+translation-position if point else unit(rotation @ (-pose[:,2]))
            distance = np.linalg.norm(delta) if point else 0
            direction = unit(delta)
            denominator=np.dot(f32(light['attenuation']),[1,distance,distance*distance]) if point else 1
            if point and row.get('sourcePointAttenuation') and (distance<=1e-6 or denominator<=0):continue
            attenuation=1/(denominator if row.get('sourcePointAttenuation') else max(float(np.float32(.01)),denominator)) if point else 1
            nl = np.dot(normal,direction)
            diffuse += max(nl,0)*attenuation*f32(light['diffuse'])[:3]
            if nl > 0:
                half = unit(direction+unit(viewer-position))
                intensity = np.clip(np.dot(normal,half),0,1)**row['power']
                specular += intensity*attenuation*f32(light['specular'])[:3]*f32([.2,.3,.4])
        error = max(np.max(abs(diffuse-f32(row['diffuse']))), np.max(abs(specular-f32(row['specular']))))
        maximum = max(maximum,float(error))
    assert len(cases) == (576 if legacy else 288) and maximum < .002, (len(cases),maximum)
    return dict(original_cell_lighting_cases=len(cases), maximum_double_precision_error=maximum, explicit_modern_selection=not legacy,recovered_slots=legacy, original_runtime_lighting_equivalence=False)


if __name__ == '__main__':
    parser=argparse.ArgumentParser();parser.add_argument('native_output');args=parser.parse_args()
    text=__import__('pathlib').Path(args.native_output).read_text(encoding='utf-8')
    collect=lambda key:[json.loads(line.split('=',1)[1]) for line in text.splitlines() if line.startswith(key+'=')]
    print(json.dumps(verify(collect('original_cell_light'),collect('original_cell_lighting')),indent=2))
