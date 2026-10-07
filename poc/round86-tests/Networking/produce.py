"""Independent synthetic selected-player movement peer; no sockets or native oracle seeds."""
import argparse
import importlib.util
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def load(number, name):
    spec = importlib.util.spec_from_file_location(f'networking86_{number}_{name}', ROOT/f'poc/round{number}-tests/Networking/{name}.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def transform(oid, flags, word, counter, position, speed, kind=0x71, parent=None):
    # Frozen66/67 independently hand-authored full-root layout, identity quaternion.
    raw = struct.pack('<HIIIQII', 5, 0x80CE5E46, flags, kind, oid, word, counter)
    if kind == 0xF1:
        raw += struct.pack('<Q', parent)
    return raw+struct.pack('<8f', 0, 0, 0, 1, position[0], position[2], position[1], speed)


def generate(output, template_crc, object_id, object_path, terrain_path, variant=None):
    if variant not in (None, 'malformed-correction'):
        raise ValueError('Unknown movement variant')
    t = load(85, 'produce').generate(output, template_crc, object_id, object_path, terrain_path)
    soe = load(14, 'peer')
    soe.SEED = t['ZoneSeed']
    def frame(seq, app):
        return soe.frame(struct.pack('>HH', 9, seq)+app).hex().upper()
    identity = [1, 0, 0, 0, 1, 0, 0, 0, 1]
    position = [1.25, 2.5, -3.75]
    flags, counters, timestamps = 0x23, [10, 11, 12], [100, 200, 300]
    poses, apps = [], []
    displacement = f32(f32(2)*f32(.1))
    for index in range(3):
        position = [position[0], position[1], f32(position[2]+displacement)]
        poses.append(dict(ObjectId=object_id, Position=position[:], Rows=identity, Speed=2, Revision=index+2, MovementCounter=None, Authoritative=False, Current=True))
        apps.append(transform(object_id, flags, timestamps[index], counters[index], position, 2).hex().upper())
    definitions = [('newer', 0xffffffff, [10, 2.5, -8], 3, True),
                   ('duplicate', 0xffffffff, [99, 99, 99], 4, False),
                   ('older', 0xfffffffe, [-99, -99, -99], 5, False),
                   ('wrap-newer', 0, [20, 2.5, -9], 4, True)]
    correction_apps, observations, revision, last = [], [], 4, poses[-1]
    for name, counter, xyz, speed, applied in definitions:
        raw = transform(object_id, 0xF1234567, 0, counter, xyz, speed)
        correction_apps.append(raw)
        if applied:
            revision += 1
            last = dict(ObjectId=object_id, Position=xyz, Rows=identity, Speed=speed, Revision=revision, MovementCounter=counter, Authoritative=True, Current=True)
        observations.append(dict(Name=name, Applied=applied, Status='Applied' if applied else 'CounterRejected', Motion=last))
    unrelated = transform(t['NearbyObjectId'], 0xF1234567, 0, 1, [7, 8, 9], 1)
    parent = transform(object_id, 0xF1234567, 0, 1, [7, 8, 9], 1, 0xF1, t['PlayObjectId'])
    unknown = bytearray(transform(object_id, 0xF1234567, 0, 1, [7, 8, 9], 1))
    struct.pack_into('<I', unknown, 10, 0x99)
    unhandled = [unrelated, parent, bytes(unknown)]
    if variant:
        correction_apps[0] = correction_apps[0][:-1]
    inbound = correction_apps+unhandled
    first_seq = len(t['WorldFrames'])+2
    actor_positions = [[p['Position'][0], p['Position'][1], -p['Position'][2]] for p in poses]
    actor_corrections = [[o['Motion']['Position'][0], o['Motion']['Position'][1], -o['Motion']['Position'][2]] for o in observations]
    t.update(Synthetic=True, Variant=variant or 'normal', AuthoredLocalMotionPolicy=True, LiveInterop=False,
             LocalMotionPolicy=dict(InitialTimestampMicroseconds=0, StepMicroseconds=100000, MaximumStepsPerUpdate=1, Velocity=[0, 0, 2], AdvanceMicroseconds=[100000, 200000, 300000],
                                    InitialNativePosition=[1.25, 2.5, -3.75], NativePositionCap=1000, NativeSpeedCap=10, Flags=flags, MovementCounters=counters, TimestampBits=timestamps),
             ExpectedMovementApplications=apps, ExpectedMovementWires=[frame(i+3, bytes.fromhex(app)) for i, app in enumerate(apps)],
             MovementAcks=[soe.frame(struct.pack('>HH', 0x15, i)).hex().upper() for i in (3, 4, 5)],
             CorrectionApplications=[raw.hex().upper() for raw in inbound], CorrectionFrames=[frame(first_seq+i, raw) for i, raw in enumerate(inbound)],
             CorrectionEvents=[dict(Index=i, AfterMovementCount=3, Sequence=first_seq+i, Classification='selected correction' if i<4 else ('unrelated root', 'unsupported parent', 'unknown kind')[i-4]) for i in range(7)],
             ExpectedLocalMotions=poses, ExpectedCorrectionObservations=observations, ExpectedActorLocalPositions=actor_positions, ExpectedActorCorrectionPositions=actor_corrections,
             ExpectedAppliedCorrections=2, ExpectedCounterRejectedCorrections=2, ExpectedUnhandledMovement=[raw.hex().upper() for raw in unhandled], ExpectedReadySubmissions=1)
    (Path(output)/'transcript.json').write_text(json.dumps(t, indent=2)+'\n', encoding='utf-8')
    return t


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--template-crc', type=lambda x: int(x, 0), required=True)
    parser.add_argument('--object-id', type=lambda x: int(x, 0), required=True)
    parser.add_argument('--object-path', required=True)
    parser.add_argument('--terrain-path', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--variant', choices=('malformed-correction',))
    a = parser.parse_args()
    t = generate(a.output, a.template_crc, a.object_id, a.object_path, a.terrain_path, a.variant)
    print(f"PASS authored movement peer: {t['Variant']}, 3 outbound/7 inbound applications")


if __name__ == '__main__':
    main()
