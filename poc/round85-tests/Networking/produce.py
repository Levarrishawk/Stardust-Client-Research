"""Independent authored server-shaped ingress; no sockets or proprietary payloads."""
import argparse
import importlib.util
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def load(round_number, filename):
    path = ROOT / f'poc/round{round_number}-tests/Networking/{filename}.py'
    spec = importlib.util.spec_from_file_location(f'r85_{round_number}_{filename}', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def envelope(oid, tag, view, body, count=0):
    return struct.pack('<HIQIBIH', 5, 0x68A75F0C, oid, tag, view, len(body)+2, count)+body


def generate(output, template_crc, object_id, object_path, terrain_path, variant=None):
    if variant not in (None, 'missing4', 'missing6', 'truncated6'):
        raise ValueError('Unknown variant')
    result = load(84, 'transcript').generate(output, template_crc, object_id, object_path, terrain_path)
    p4, p6 = load(38, 'peer'), load(32, 'peer')
    m4 = p4.seed()
    m4.update(ObjectId=object_id, DeclaredCount=14, FloatBits=[0x3F800000, 0x40000000, 0x40400000, 0x40800000, 0x3F800000, 0x3F800000, 0x3F800000, 0x3F800000, 0x3F800000, 0x3F800000], ListenTo=0,
              Encumbrances=dict(Counter=4, Values=[1, 2, 3]),
              SkillMods=dict(Counter=5, Values=[dict(RawCommand=0, Name=p4.text(b'precision'), BaseValue=7, Bonus=2)]),
              SpaceMissionObjects=dict(Counter=6, Values=[dict(OwnerId=object_id, MissionId=123)]))
    m6 = p6.baseline()
    m6.update(ObjectId=object_id, Passive=0, Level=12, PerformanceAnimation=p6.text(b'idle'), MoodString=p6.text(b'calm'),
              Ham=dict(Counter=7, Values=[100, 200, 300]), MaximumHam=dict(Counter=8, Values=[1000, 2000, 3000]))
    b4 = envelope(object_id, 0x4352454F, 4, p4.baseline_fields(m4), 14)
    b6 = envelope(object_id, 0x4352454F, 6, p6.baseline_fields(m6), 22)
    p4.OID = object_id
    assert p4.parse(b4, 'Baseline')[0] == m4
    assert p6.parse(b6, 'Baseline')[0] == m6
    b3 = bytearray.fromhex(load(15, 'peer_model').produce()['Applications']['humanBaseline'])
    struct.pack_into('<Q', b3, 6, object_id)
    nearby = object_id % 0xffffffffffffffff + 1
    play = nearby % 0xffffffffffffffff + 1
    def retarget(raw, oid):
        raw = bytearray(raw)
        struct.pack_into('<Q', raw, 6, oid)
        return bytes(raw)
    apps = {k: bytes.fromhex(v) for k, v in result['Applications'].items()}
    apps.update(SelectedCreo1=envelope(object_id, 0x4352454F, 1, b'authored opaque CREO1'),
                SelectedCreo3=bytes(b3), SelectedCreo4=b4, SelectedCreo6=b6,
                NearbyCreate=retarget(apps['Create'], nearby), NearbyCreo3=retarget(b3, nearby), NearbyCreo6=retarget(b6, nearby), NearbyEnd=retarget(apps['End'], nearby),
                PlayCreate=retarget(apps['Create'], play), PlayEnd=retarget(apps['End'], play))
    for view in (3, 6, 8, 9):
        apps[f'Play{view}'] = envelope(play, 0x504C4159, view, f'authored opaque PLAY{view}'.encode('ascii'))
    names = ['Start', 'Create', 'SelectedCreo1', 'SelectedCreo3', 'SelectedCreo4', 'SelectedCreo6',
             'NearbyCreate', 'NearbyCreo3', 'NearbyCreo6', 'NearbyEnd', 'PlayCreate', 'Play3', 'Play6', 'Play8', 'Play9', 'PlayEnd', 'End', 'NonworldSentinel']
    if variant in ('missing4', 'missing6'):
        names.remove('SelectedCreo'+variant[-1])
    elif variant == 'truncated6':
        raw = bytearray(b6[:-1])
        struct.pack_into('<I', raw, 19, len(raw)-23)
        apps['SelectedCreo6'] = bytes(raw)
    deferred_names = [name for name in names if name not in ('Start', 'Create', 'SelectedCreo3', 'SelectedCreo4', 'SelectedCreo6', 'End')]
    soe = load(14, 'peer')
    soe.SEED = result['ZoneSeed']
    def frame(seq, app):
        return soe.frame(struct.pack('>HH', 9, seq)+app).hex().upper()
    apps['OpaqueCreoEnvelope'] = bytes(b3)  # Retained Round84 schema key; now aliases the typed selected3.
    result.update(AuthoredServerShaped=True, AuthoredTypedCreo3=True, OpaqueBaselineSemantics=False, OpaqueDeferredCreo1=True, OpaqueDeferredPlay=True,
                  LiveInterop=False, Variant=variant or 'normal', NearbyObjectId=nearby, PlayObjectId=play,
                  WorldFrames=[frame(i, apps[name]) for i, name in enumerate(names, 1)],
                  ServerReady=frame(len(names)+1, apps['ServerReady']),
                  Applications={k: v.hex().upper() for k, v in apps.items()},
                  IngressOrder=names, ExpectedDeferred=[apps[name].hex().upper() for name in deferred_names],
                  ExpectedSelectedModels=dict(Creo4=m4, Creo6=m6),
                  DeferredClassification=[dict(Name=name, Hex=apps[name].hex().upper(), Reason='opaque selected view' if name=='SelectedCreo1' else 'nonworld notice' if name=='NonworldSentinel' else 'unselected object') for name in deferred_names],
                  CompleteServerBaselineSet=False)
    (Path(output)/'transcript.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--template-crc', type=lambda x: int(x, 0), required=True)
    parser.add_argument('--object-id', type=lambda x: int(x, 0), required=True)
    parser.add_argument('--object-path', required=True)
    parser.add_argument('--terrain-path', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--variant', choices=('missing4', 'missing6', 'truncated6'))
    a = parser.parse_args()
    result = generate(a.output, a.template_crc, a.object_id, a.object_path, a.terrain_path, a.variant)
    print(f"PASS authored {result['Variant']} ingress: {len(result['WorldFrames'])} frames, {len(result['ExpectedDeferred'])} deferred")


if __name__ == '__main__':
    main()
