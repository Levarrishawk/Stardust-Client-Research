"""Independent joined peer: accepted protected transcript plus authored typed CREO3."""
import argparse
import importlib.util
import json
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[3]


def module(name, relative):
    spec = importlib.util.spec_from_file_location(name, ROOT / relative)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--template-crc', type=lambda x: int(x, 0), required=True)
    p.add_argument('--object-id', type=lambda x: int(x, 0), required=True)
    p.add_argument('--object-path', required=True)
    p.add_argument('--terrain-path', required=True)
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    peer = module('peer84', 'poc/round84-tests/Networking/transcript.py')
    result = peer.generate(a.output, a.template_crc, a.object_id, a.object_path, a.terrain_path)
    typed = module('typed15', 'poc/round15-tests/Networking/peer_model.py')
    baseline = bytearray.fromhex(typed.produce()['Applications']['humanBaseline'])
    struct.pack_into('<Q', baseline, 6, a.object_id)
    soe = module('soe14', 'poc/round14-tests/Networking/peer.py')
    soe.SEED = result['ZoneSeed']
    result['WorldFrames'][2] = soe.frame(struct.pack('>HH', 9, 3) + baseline).hex().upper()
    del result['Applications']['OpaqueCreoEnvelope']
    result['Applications']['TypedCreo3'] = baseline.hex().upper()
    result.update(OpaqueBaselineSemantics=False, AuthoredTypedCreo3=True, CompleteServerBaselineSet=False)
    (a.output / 'transcript.json').write_text(json.dumps(result, indent=2) + '\n')
    print('PASS independently authored typed CREO3 joined transcript')


if __name__ == '__main__':
    main()
