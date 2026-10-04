"""Synthetic research checks and fresh-process static-audit failure gates, not client execution."""
import argparse
import importlib.util
import json
import math
import struct
import subprocess
import sys
import tempfile
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[3]
    tool = repo / 'tools/python/verify_creature_view4_initialization.py'
    spec = importlib.util.spec_from_file_location('initialization', tool)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    target = module.receipt_path(args.receipt, repo)
    raw = module.verified_bytes(args.exe)
    checks = []

    def check(label, condition):
        if not condition:
            raise AssertionError(label)
        checks.append(label)

    # Independent, explicit numeric examples of the documented operand direction.
    parent, current = 20.0, 5.0
    check('plus-example', parent + current == 25)
    check('minus-parent-first-example', parent - current == 15)
    check('percent-increase-example', parent * (1 + current * 0.01) == 21)
    check('percent-decrease-example', parent - parent * current * 0.01 == 19)
    check('absent-relative-parent-minus-example', 0.0 - current == -5)
    check('absent-relative-parent-percent-example', 0.0 * (1 + current * 0.01) == 0)
    check('speed-index-stride-example', 0x498 + 24 == 0x4B0)
    pi32 = struct.unpack('<f', bytes.fromhex(module.CONSTANTS[0x15DCBD4]))[0]
    inv180 = struct.unpack('<f', bytes.fromhex(module.CONSTANTS[0x15DC5F0]))[0]
    for degrees, expected in [(0, '00000000'), (90, 'db0fc93f'),
                              (-90, 'db0fc9bf'), (180, 'db0f4940')]:
        value = struct.pack('<f', degrees * pi32 * inv180).hex()
        check(f'synthetic-angle-{degrees}', value == expected)
    check('angle-constants-finite', math.isfinite(pi32) and math.isfinite(inv180))

    base = [sys.executable, str(tool), '--packages', str(args.packages),
            '--server-root', str(args.server_root)]
    with tempfile.TemporaryDirectory(prefix='creo4-research-', dir=target.parent) as temp:
        folder = Path(temp)
        for label, payload in [('empty', b''), ('truncated', raw[:256]),
                               ('changed', bytes([raw[0] ^ 1]) + raw[1:])]:
            wrong = folder / (label + '.bin')
            wrong.write_bytes(payload)
            receipt = folder / (label + '.json')
            run = subprocess.run(base + ['--exe', str(wrong), '--receipt', str(receipt)],
                                 capture_output=True, text=True)
            check('fresh-process-' + label + '-identity-gate',
                  run.returncode != 0 and 'Wrong executable SHA256' in run.stderr
                  and not receipt.exists())
        missing = folder / 'missing-packages'
        receipt = folder / 'missing.json'
        run = subprocess.run([sys.executable, str(tool), '--exe', str(args.exe),
                              '--packages', str(missing), '--server-root', str(args.server_root),
                              '--receipt', str(receipt)], capture_output=True, text=True)
        check('fresh-process-missing-package-gate', run.returncode != 0
              and 'package directory is absent' in run.stderr and not receipt.exists())
        public = repo / 'round44-invalid-receipt.json'
        check('public-test-target-absent', not public.exists())
        run = subprocess.run(base + ['--exe', str(args.exe), '--receipt', str(public)],
                             capture_output=True, text=True)
        check('fresh-process-public-receipt-gate', run.returncode != 0
              and 'ignored local-private' in run.stderr and not public.exists())
    target.write_text(json.dumps(dict(Checks=checks, Count=len(checks),
                                     OriginalExecuted=False, LiveContact=False,
                                     SyntheticArithmeticOnly=True,
                                     BitExactRuntimeEquivalenceClaimed=False), indent=2) + '\n',
                      encoding='utf-8')
    print(f'PASS {len(checks)} synthetic and failure-gate research checks')


if __name__ == '__main__':
    main()
