"""Independent synthetic outbound disconnect oracle atop the accepted movement peer."""
import argparse
import importlib.util
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def load(number, name):
    spec = importlib.util.spec_from_file_location(f'networking87_{number}_{name}', ROOT/f'poc/round{number}-tests/Networking/{name}.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def generate(output, template_crc, object_id, object_path, terrain_path):
    t = load(86, 'produce').generate(output, template_crc, object_id, object_path, terrain_path)
    soe = load(14, 'peer')
    soe.SEED = t['ZoneSeed']
    raw = struct.pack('>HIH', 5, t['ZoneId'], 6)
    wire = soe.frame(raw)
    bad_crc = bytearray(wire)
    bad_crc[-1] ^= 1
    packets = dict(Disconnect=wire.hex().upper(), WrongId=soe.frame(struct.pack('>HIH', 5, t['ZoneId'] ^ 1, 6)).hex().upper(),
                   BadCrc=bad_crc.hex().upper(), ShortDecoded=soe.frame(raw[:-1]).hex().upper(), ExtraDecoded=soe.frame(raw+b'\0').hex().upper(),
                   AlternateReason=soe.frame(raw[:-2]+b'\xFE\xDC').hex().upper())
    t.update(ExpectedDisconnectPlain=raw.hex().upper(), ExpectedDisconnectWire=wire.hex().upper(), DisconnectReasonBytes='0006',
             GracefulDisconnectExpected=True, DisconnectTestPackets=packets, DisconnectReliableSequence=None, DisconnectReplyRequired=False)
    (Path(output)/'transcript.json').write_text(json.dumps(t, indent=2)+'\n', encoding='utf-8')
    return t


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--template-crc', type=lambda x: int(x, 0), required=True)
    parser.add_argument('--object-id', type=lambda x: int(x, 0), required=True)
    parser.add_argument('--object-path', required=True)
    parser.add_argument('--terrain-path', required=True)
    parser.add_argument('--output', type=Path, required=True)
    a = parser.parse_args()
    generate(a.output, a.template_crc, a.object_id, a.object_path, a.terrain_path)
    print('PASS authored disconnect peer; complete86 movement prefix retained')


if __name__ == '__main__':
    main()
