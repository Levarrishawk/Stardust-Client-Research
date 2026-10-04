"""Independent PE byte checks and fresh-process identity negatives for Round48."""
import argparse
import hashlib
import json
import runpy
import struct
import subprocess
import sys
import uuid
from pathlib import Path

EXPECTED = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'temp-root']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args(); repo = Path(__file__).resolve().parents[3]
    if not args.temp_root.resolve().is_relative_to((repo / 'local-private').resolve()):
        parser.error('Private test output required')
    raw = args.exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED:
        parser.error('Wrong executable identity before VA use')
    # Independent PE mapping: no pefile/Capstone or production anchor import.
    pe = struct.unpack_from('<I', raw, 0x3C)[0]
    assert raw[pe:pe+4] == b'PE\0\0'
    count = struct.unpack_from('<H', raw, pe + 6)[0]
    optional = pe + 24; size = struct.unpack_from('<H', raw, pe + 20)[0]
    assert struct.unpack_from('<H', raw, optional)[0] == 0x10B
    base = struct.unpack_from('<I', raw, optional + 28)[0]
    sections = []
    for n in range(count):
        at = optional + size + n * 40
        virtual_size, rva, physical_size, offset = struct.unpack_from('<4I', raw, at + 8)
        sections.append((rva, physical_size, offset))
    def read(va, length):
        rva = va - base
        for start, extent, offset in sections:
            if start <= rva and rva + length <= start + extent:
                return raw[offset+rva-start:offset+rva-start+length]
        raise ValueError('Unmapped bounded bytes')
    checks = 0
    # Explicit independently authored instruction encodings and displacements.
    for va, code in [
        (0x4388A7, '8b8674060000'), (0x438957, '8b8660060000'),
        (0x438922, '8b9074060000'), (0x4389EA, '8b8f60060000'),
        (0x438A27, '8b8638060000'), (0x438A77, '8b864c060000'),
        (0x439B14, '89410c'), (0x439A93, '894110'),
        (0x439B3B, '8d460c'), (0x439B5B, '8d460c'),
        (0x439B39, '6a04'), (0x439B59, '6a04'),
        (0x42F0C2, '898660060000'), (0x42F0F8, '899674060000'),
        (0x4389E6, '3bc6'), (0x43893C, 'd84d08'), (0x438940, 'd84dfc'),
        (0x438A04, 'd84d08'), (0x438A09, 'd84dfc'),
    ]:
        assert read(va, len(bytes.fromhex(code))) == bytes.fromhex(code), hex(va); checks += 1
    for va, target in [(0x42F0AA, 0x11ADE40), (0x42F0E0, 0x11ADE40),
                       (0x439B3F, 0x439910), (0x439B5F, 0x439910),
                       (0x43993F, 0x118B030), (0x43892D, 0x438A70),
                       (0x438937, 0x438A20), (0x4389F5, 0x438A70),
                       (0x4389FF, 0x438A20), (0x431917, 0x4388A0),
                       (0x431927, 0x438950), (0x629F7E, 0x4388A0), (0x629F8A, 0x438950)]:
        instruction = read(va, 5); assert instruction[0] == 0xE8
        assert va + 5 + struct.unpack('<i', instruction[1:])[0] == target; checks += 1
    assert struct.unpack('<8I', read(0x15DCF0C, 32)) == (0x445370, 0x439AC0, 0x439B30, 0x439A90, 0x439AA0, 0x439AE0, 0x439B50, 0x439B00); checks += 8
    work = args.temp_root / ('checks-' + str(uuid.uuid4())); work.mkdir(parents=True)
    tool = repo / 'tools/python/verify_creature_movement_writers.py'
    pins = runpy.run_path(str(repo / 'tools/python/verify_creature_view4_delivery.py'))['SERVER_PINS']
    copy = work / 'server'
    for name in pins:
        target = copy / name; target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes((args.server_root / name).read_bytes())
    rejected = 0
    def reject(exe, server, expected, receipt=None, packages=None):
        nonlocal rejected
        receipt = receipt or work / f'rejected-{rejected}.json'
        result = subprocess.run([sys.executable, str(tool), '--exe', str(exe), '--packages', str(packages or args.packages),
                                 '--server-root', str(server), '--receipt', str(receipt)], capture_output=True, text=True)
        assert result.returncode != 0 and expected in result.stdout + result.stderr and not receipt.exists(), (result.stdout, result.stderr)
        rejected += 1
    wrong = work / 'wrong.bin'
    for payload in [b'', b'MZ' + bytes(1024)]:
        wrong.write_bytes(payload); reject(wrong, args.server_root, 'Wrong executable SHA256', packages=work / 'absent-packages')
    for name in pins:
        target = copy / name; original = target.read_bytes(); target.write_bytes(original + b'\n')
        reject(args.exe, copy, 'Changed server source identity: ' + name); target.write_bytes(original)
    reject(args.exe, work / 'missing-server', 'No such file')
    reject(args.exe, args.server_root, 'Static-analysis package directory is absent', packages=work / 'absent-packages')
    reject(wrong, args.server_root, 'Receipt', repo / 'poc/round48-tests/Networking/rejected-public.json')
    # A deliberately incorrect expected anchor must fail the actual static audit.
    namespace = runpy.run_path(str(tool)); audit = namespace['audit']; globals_ = audit.__globals__
    first = globals_['ANCHORS'][0]; globals_['ANCHORS'] = [(first[0], 'nop', '')] + globals_['ANCHORS'][1:]
    try:
        audit(args.exe, args.packages, args.server_root, repo)
    except ValueError as error:
        assert 'Instruction mismatch' in str(error); rejected += 1
    else:
        raise AssertionError('Incorrect anchor accepted')
    result = dict(IndependentByteChecks=checks, IdentityAndAnchorNegatives=rejected, WrongExecutables=2,
                  ChangedServerFiles=len(pins), MissingServer=1, MissingPackages=1, PublicReceipt=1, WrongAnchor=1,
                  NoRejectedReceipts=True, OriginalExecuted=False, LiveContact=False)
    (args.temp_root / 'peer.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print('PASS', result)

if __name__ == '__main__':
    main()
