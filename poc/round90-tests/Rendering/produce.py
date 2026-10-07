"""Independent controlled-root/UI peer; authored synthetic credentials and no sockets."""
import argparse
import importlib.util
import json
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[3]


def module(number, name):
    spec = importlib.util.spec_from_file_location(f'host90_{number}_{name}', ROOT / f'poc/round{number}-tests/Networking/{name}.py')
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def generate(output, template_crc, object_id, object_path, terrain_path):
    t = module(87, 'produce').generate(output, template_crc, object_id, object_path, terrain_path)
    soe = module(14, 'peer')
    motion = module(86, 'produce')
    # Add a second, unsupported character to the actual decoded session list.
    soe.SEED = t['LoginSeed']
    plain = bytearray(soe.open_frame(bytes.fromhex(t['LoginReplies'][3])))
    assert struct.unpack_from('<HI', plain, 4) == (2, 0x65EA4574)
    assert struct.unpack_from('<I', plain, 10)[0] == 1
    struct.pack_into('<I', plain, 10, 2)
    plain += struct.pack('<I', 1) + b'B\0' + struct.pack('<IQII', template_crc ^ 1, object_id + 9, 7, 1)
    t['LoginReplies'][3] = soe.frame(plain).hex().upper()
    def ascii16(value):
        raw = value.encode('ascii')
        return struct.pack('<H', len(raw)) + raw
    login = struct.pack('<HI', 4, 0x41131F96) + ascii16('synthetic') + ascii16('fixture') + ascii16('20260817-SD:01')
    t['ExpectedLoginWire'] = soe.frame(struct.pack('>HH', 9, 0) + login).hex().upper()
    soe.SEED = t['ZoneSeed']
    position = [1.25, 2.5, -3.75]
    wires, positions = [], []
    for index in range(3):
        # Four accepted 100ms local-driver steps per 400ms authored root.
        for _ in range(4):
            position[2] = motion.f32(position[2] + motion.f32(motion.f32(.5) * motion.f32(.1)))
        app = motion.transform(object_id, 0x23, (index + 1) * 400, index + 1, position, .5)
        wires.append(soe.frame(struct.pack('>HH', 9, index + 3) + app).hex().upper())
        positions.append([position[0], position[1], -position[2]])
    t.update(ExpectedMovementWires=wires, ExpectedActorLocalPositions=positions,
             ControlledModernPolicy=True, FreshOriginAsserted=True, PhysicalOSInput=False,
             ExpectedCharacterCount=2, ExpectedUnsupportedIndex=1)
    (Path(output) / 'transcript.json').write_text(json.dumps(t, indent=2) + '\n', encoding='utf-8')
    return t


def main():
    p = argparse.ArgumentParser()
    for name in ('object-path', 'terrain-path', 'output'):
        p.add_argument('--' + name, required=True)
    p.add_argument('--template-crc', type=lambda s: int(s, 0), required=True)
    p.add_argument('--object-id', type=lambda s: int(s, 0), required=True)
    a = p.parse_args()
    generate(a.output, a.template_crc, a.object_id, a.object_path, a.terrain_path)
    print('PASS independent synthetic UI/controlled-root peer')


if __name__ == '__main__':
    main()
