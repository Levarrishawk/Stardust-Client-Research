"""Synthetic serialized-input research gates; no production parser or original execution."""
import argparse
import hashlib
import importlib.util
import json
import struct
import subprocess
import sys
import tempfile
from pathlib import Path


def chunk(tag, data):
    return tag + struct.pack('>I', len(data)) + data


def form(tag, children):
    return chunk(b'FORM', tag + b''.join(children))


def template(parameters, parent=None, family=b'SCOT'):
    # Explicitly synthetic, independently authored minimal hierarchy.
    base = template({}, parent, b'STOT') if family == b'SCOT' else (
        template({}, parent, b'SHOT') if family == b'STOT' else None)
    records = [chunk(b'PCNT', struct.pack('<I', len(parameters)))]
    records += [chunk(b'XXXX', name.encode() + b'\0' + value) for name, value in parameters.items()]
    children = [form(b'DERV', [chunk(b'XXXX', parent.encode() + b'\0')])] if parent else []
    children.append(form({b'SCOT': b'0013', b'STOT': b'0009', b'SHOT': b'0010'}[family], records))
    if base:
        children.append(base)
    return form(family, children)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'root', 'configuration', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[3]
    sys.path.insert(0, str(repo / 'tools/python'))
    tool = repo / 'tools/python/verify_creature_template_movement.py'
    spec = importlib.util.spec_from_file_location('movement_research', tool)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    target = module.private_receipt(args.receipt, repo)
    original = module.identity(args.exe, module.EXE_SHA, 'Executable')
    module.identity(args.configuration, module.CONFIG_SHA, 'Configuration')
    checks = []

    def check(label, condition):
        if not condition:
            raise AssertionError(label)
        checks.append(label)

    def reject(label, operation):
        try:
            operation()
        except (ValueError, AssertionError, struct.error):
            checks.append(label)
        else:
            raise AssertionError('Expected rejection: ' + label)

    literal = lambda value, relative=0x20: bytes([1, relative]) + struct.pack('<f', value)
    absent = b'\0 '
    speeds = struct.pack('<i', 2) + literal(7) + literal(2)
    authored = dict(speed=speeds, slopeModAngle=literal(45),
                    slopeModPercent=literal(0.25), waterModPercent=literal(0.5))
    empty = dict(speed=struct.pack('<i', 2) + absent * 2,
                 slopeModAngle=absent, slopeModPercent=absent, waterModPercent=absent)
    files = {'object/root.iff': template(empty, 'object/base.iff'),
             'object/base.iff': template(authored, 'object/end.iff'),
             'object/end.iff': template({}, family=b'SHOT')}
    lookup = lambda path: (10, 'synthetic.tre', files[path]) if path in files else None
    chain = module.read_chain(lookup, 'object/root.iff')
    results = module.classify(chain)
    check('five-inherited-literal-provenance', len(results) == 5 and all(
        v['Classification'] == 'deterministic literal' and v['SourcePath'] == 'object/base.iff'
        and v['Inherited'] for v in results.values()))
    check('speed-order-independent-numbers', results['speed[0]']['Parameter']['Value'] == 7
          and results['speed[1]']['Parameter']['Value'] == 2)
    check('literal-exact-serialized-bits', results['slopeModAngle']['Parameter']['F32LE'] == '00003442')
    check('explicit-absent-not-zero', all(v['Classification'] == 'absent/unknown'
          for v in module.classify([module.inspect_document(template(empty)) | {'Path': 'synthetic'}]).values()))
    override = module.inspect_document(template(dict(authored, slopeModAngle=literal(12)))) | {'Path': 'near'}
    check('near-authored-overrides-parent', module.classify([override] + chain)['slopeModAngle']['SourcePath'] == 'near')
    relative = module.inspect_document(template(dict(authored, slopeModAngle=literal(12, 0x2D)))) | {'Path': 'relative'}
    check('relative-preserved-no-evaluation', module.classify([relative])['slopeModAngle']['Classification'] == 'inherited/relative')
    ranged = b'\x03 ' + struct.pack('<2f', 1, 4)
    range_node, end = module.decode_parameter(ranged)
    check('range-two-f32-widths', end == 10 and range_node['RangeF32LE'] == ['0000803f', '00008040'])
    weighted = b'\x02 ' + struct.pack('<i', 2) + struct.pack('<i', 30) + literal(1) + struct.pack('<i', 70) + ranged
    node, end = module.decode_parameter(weighted)
    check('weighted-order-weight-then-nested', end == len(weighted)
          and [v['Weight'] for v in node['Entries']] == [30, 70]
          and [v['Parameter']['Mode'] for v in node['Entries']] == [1, 3])
    sampled = module.inspect_document(template(dict(authored, slopeModAngle=weighted))) | {'Path': 'sampled'}
    check('weighted-classified-without-rng', module.classify([sampled])['slopeModAngle']['Classification'] == 'sampled/global')
    extra, metadata = module.decode_selected({'speed': struct.pack('<i', 3) + literal(1) + literal(2) + literal(3)})
    check('speed-surplus-consumed-not-assigned', metadata['speed']['Discarded'] == 1
          and extra['speed[0]']['Value'] == 1 and extra['speed[1]']['Value'] == 2)
    short, metadata = module.decode_selected({'speed': struct.pack('<i', 1) + literal(1)})
    check('short-speed-second-slot-absent', short['speed[1]'] is None)
    check('empty-weight-list-retains-sampled-structure', module.decode_parameter(b'\x02 ' + struct.pack('<i', 0))[0]['Entries'] == [])
    nonfinite = module.decode_parameter(b'\x01 ' + bytes.fromhex('0000807f'))[0]
    check('nonfinite-raw-preserved-without-runtime-value', nonfinite['F32LE'] == '0000807f' and not nonfinite['Finite'] and nonfinite['Value'] is None)
    for label, data in [('missing-mode', b''), ('missing-relative', b'\1'),
                        ('truncated-literal', literal(1)[:-1]), ('truncated-range', ranged[:-1]),
                        ('truncated-weighted-count', b'\x02 \1'),
                        ('truncated-weight', b'\x02 ' + struct.pack('<i', 1)),
                        ('negative-mode', b'\xff '), ('mode4-not-serialized-loader-mode', b'\x04 '),
                        ('negative-list-budget', b'\x02 ' + struct.pack('<i', -1)),
                        ('oversize-list-budget', b'\x02 ' + struct.pack('<i', 65))]:
        reject(label, lambda data=data: module.decode_parameter(data))
    reject('parameter-depth-budget', lambda: module.decode_parameter(absent, depth=16))
    reject('trailing-literal', lambda: module.decode_selected({'slopeModAngle': literal(1) + b'x'}))
    reject('truncated-speed-count', lambda: module.decode_selected({'speed': b'\0'}))
    reject('negative-speed-inspector-budget', lambda: module.decode_selected({'speed': struct.pack('<i', -1)}))
    reject('truncated-iff', lambda: module.inspect_document(files['object/root.iff'][:-1]))
    bad = bytearray(template(authored));location = bad.index(b'PCNT') + 8;bad[location:location + 4] = struct.pack('<I', 99)
    reject('pcnt-mismatch', lambda: module.inspect_document(bytes(bad)))
    duplicated = template({})
    duplicate_records = [chunk(b'PCNT', struct.pack('<I', 2)), chunk(b'XXXX', b'speed\0' + speeds)] * 1
    duplicate_records += [chunk(b'XXXX', b'speed\0' + speeds)]
    duplicate = form(b'SCOT', [form(b'0013', duplicate_records), template({}, family=b'STOT')])
    reject('duplicate-field', lambda: module.inspect_document(duplicate))
    reject('missing-parent', lambda: module.read_chain(lambda path: lookup(path) if path != 'object/base.iff' else None, 'object/root.iff'))
    cyclic = template(empty, 'object/root.iff')
    reject('parent-cycle', lambda: module.read_chain(lambda path: (1, 'synthetic.tre', cyclic), 'object/root.iff'))
    def masked(path):
        if path == 'object/base.iff':
            raise ValueError('Required logical asset is masked')
        return lookup(path)
    reject('masked-parent-stops-chain', lambda: module.read_chain(masked, 'object/root.iff'))
    reject('wrong-root-source-pin', lambda: module.read_chain(lookup, 'object/root.iff', module.SOURCE_PINS))

    common = [sys.executable, str(tool), '--packages', str(args.packages), '--server-root', str(args.server_root),
              '--root', str(args.root), '--configuration', str(args.configuration)]
    with tempfile.TemporaryDirectory(prefix='movement-research-', dir=target.parent) as temp:
        folder = Path(temp)
        for label, raw in [('empty', b''), ('truncated', original[:256]),
                           ('changed', bytes([original[0] ^ 1]) + original[1:])]:
            wrong = folder / (label + '.bin');wrong.write_bytes(raw);receipt = folder / (label + '.json')
            run = subprocess.run(common + ['--exe', str(wrong), '--receipt', str(receipt)], capture_output=True, text=True)
            check('fresh-' + label + '-exe-gate', run.returncode != 0 and 'Executable identity mismatch' in run.stderr and not receipt.exists())
        wrong_cfg = folder / 'wrong.cfg';wrong_cfg.write_bytes(b'synthetic configuration');receipt = folder / 'config.json'
        run = subprocess.run(common + ['--exe', str(args.exe), '--configuration', str(wrong_cfg), '--receipt', str(receipt)], capture_output=True, text=True)
        check('fresh-config-gate', run.returncode != 0 and 'Configuration identity mismatch' in run.stderr and not receipt.exists())
        public = repo / 'round45-invalid-receipt.json';check('public-target-absent', not public.exists())
        run = subprocess.run(common + ['--exe', str(args.exe), '--receipt', str(public)], capture_output=True, text=True)
        check('fresh-public-receipt-gate', run.returncode != 0 and 'ignored local-private' in run.stderr and not public.exists())
    target.write_text(json.dumps(dict(Checks=checks, Count=len(checks), Synthetic=True,
                                     OriginalExecuted=False, LiveContact=False,
                                     FinalRuntimeValuesClaimed=False), indent=2) + '\n', encoding='utf-8')
    print(f'PASS {len(checks)} serialized-input and fresh-process research gates')


if __name__ == '__main__':
    main()
