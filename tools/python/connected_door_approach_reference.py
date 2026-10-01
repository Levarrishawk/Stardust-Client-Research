"""Independent selected-edge/continuous-sheet checks for bounded connected fixtures."""
import math
import struct
from verify_original_floor_boxes import coordinates
from verify_floor_path_boxes import f32
from step_door_actor_reference import StepReferenceDoorActor

def verify_connected_approach(layout, candidate, native, floors):
    source, target = candidate['cell'], candidate['target']
    ref = layout['cells'][source]['portals'][candidate['portal']]
    values = struct.unpack('<12f', bytes.fromhex(ref['transform_hex']))
    door = [values[3], values[7], values[11]]
    sf, tf = [floors[layout['cells'][cell]['floor']] for cell in (source, target)]
    def edges(f, portal):
        vs, rows = f
        return [(ti, e, vs[r[e]], vs[r[(e+1)%3]]) for ti, r in enumerate(rows) for e in range(3)
                if r[15+e] == portal and r[4+e] == -1]
    pairs = []
    for s in edges(sf, candidate['portal']):
        matches = [t for t in edges(tf, candidate['target_portal']) if max(math.dist(s[2], t[3]), math.dist(s[3], t[2])) <= 1e-4]
        if len(matches) == 1:
            mid = [f32(f32(a+b)*.5) for a, b in zip(s[2], s[3])]
            delta = [f32(a-b) for a, b in zip(mid, door)]
            rank = f32(f32(f32(delta[0]*delta[0])+f32(delta[1]*delta[1]))+f32(delta[2]*delta[2]))
            pairs.append((rank, s[0], s[1], s, matches[0]))
    _, _, _, se, te = min(pairs)
    oracle = StepReferenceDoorActor.__new__(StepReferenceDoorActor)
    oracle.local_ray = True
    oracle.r = {'floors': {source: sf, target: tf}}
    for cell, f, edge, endpoint in ((source, sf, se, native['initial']['local']), (target, tf, te, native['initial']['end'])):
        ti, _, a, b = edge; vs, rows = f; ps = [vs[i] for i in rows[ti][:3]]
        mid = [(x+y)*.5 for x,y in zip(a,b)]
        nx, nz = -(b[2]-a[2]), b[0]-a[0]; length = math.hypot(nx,nz)
        center = [sum(p[i] for p in ps)/3 for i in range(3)]
        if nx*(center[0]-mid[0])+nz*(center[2]-mid[2]) < 0: nx,nz=-nx,-nz
        expected = [mid[0]+nx/length*.8, 0, mid[2]+nz/length*.8]
        assert math.hypot(endpoint[0]-expected[0],endpoint[2]-expected[2]) <= 1e-4, (candidate, 'edge-normal endpoint')
        path = oracle.leg(cell, mid, endpoint)
        assert math.dist(path[-1][3], endpoint) <= 1e-4, (candidate, 'connected sheet endpoint')
        if cell == source:
            stored = rows[native['initial']['triangle']]
            weights,y = coordinates([vs[i] for i in stored[:3]], endpoint[0],endpoint[2])
            assert min(weights) >= -1e-6 and abs(y-endpoint[1]) <= 1e-4
    return 2
