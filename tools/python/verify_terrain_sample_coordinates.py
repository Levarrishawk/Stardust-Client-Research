"""Verify bounded terrain coordinate-producer instruction anchors without execution."""
import argparse
import hashlib
import json
import sys
from pathlib import Path

EXPECTED = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'


def verify(executable, packages):
    raw = executable.read_bytes()
    actual = hashlib.sha256(raw).hexdigest().upper()
    if actual != EXPECTED:
        raise ValueError('Executable identity mismatch; rejected before decoding')
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    mapped = pe.get_memory_mapped_image()
    base = pe.OPTIONAL_HEADER.ImageBase
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    ranges = [(0xB6EAB0, 0xB6EE95), (0xB6EEA0, 0xB6F1B3),
              (0x84A530, 0x84A840), (0xB65280, 0xB653B3),
              (0xB63D20, 0xB642AE), (0xB65730, 0xB657F9),
              (0xB6E510, 0xB6E524), (0xB6E6B0, 0xB6E6D7)]
    instructions = {}
    for start, end in ranges:
        for i in decoder.disasm(mapped[start-base:end-base], start):
            instructions[i.address] = (i.mnemonic, i.op_str)
    anchors = {
        0xB6EDE2: ('fild', 'dword ptr [edi + 0x2c]'),
        0xB6EDEA: ('fdivr', 'dword ptr [edi + 0x28]'),
        0xB6EDFA: ('fstp', 'dword ptr [edi + 0x94]'),
        0xB6F100: ('fild', 'dword ptr [esi + 0x2c]'),
        0xB6F108: ('fdivr', 'dword ptr [esi + 0x28]'),
        0xB6F118: ('fstp', 'dword ptr [esi + 0x94]'),
        0xB6E510: ('fld', 'dword ptr [ecx + 0x28]'),
        0xB6E520: ('mov', 'eax, dword ptr [ecx + 0x2c]'),
        0xB6E6B0: ('fld', 'dword ptr [ecx + 0x94]'),
        0x84A5A0: ('fild', 'dword ptr [ebp + 0x10]'),
        0x84A5AE: ('fmul', 'st(1)'),
        0x84A5BA: ('fmul', 'dword ptr [0x15dc5e4]'),
        0x84A5C7: ('fstp', 'dword ptr [ebp - 0x1c]'),
        0x84A5CC: ('fild', 'dword ptr [ebp - 0x10]'),
        0x84A5CF: ('fmul', 'dword ptr [ebp - 0x1c]'),
        0x84A5D2: ('fstp', 'dword ptr [ebp - 0x10]'),
        0x84A5D5: ('fild', 'dword ptr [ebp + 0xc]'),
        0x84A5D8: ('fmul', 'dword ptr [ebp - 0x14]'),
        0x84A5DB: ('fsub', 'dword ptr [ebp - 0x10]'),
        0x84A5DE: ('fild', 'dword ptr [ebp + 8]'),
        0x84A5E1: ('fmul', 'dword ptr [ebp - 0x14]'),
        0x84A5E4: ('fsub', 'dword ptr [ebp - 0x10]'),
        0x84A5E7: ('fstp', 'dword ptr [ebp - 0x28]'),
        0x84A5EA: ('fstp', 'dword ptr [ebp - 0x20]'),
        0x84A75B: ('mov', 'dword ptr [ebp - 0x608], eax'),
        0x84A76D: ('mov', 'dword ptr [ebp - 0x604], ecx'),
        0x84A77C: ('mov', 'dword ptr [ebp - 0x600], edx'),
        0x84A776: ('mov', 'dword ptr [ebp - 0x614], eax'),
        0x84A78B: ('mov', 'dword ptr [ebp - 0x610], ecx'),
        0x84A79E: ('call', '0xb65400'),
        0xB65309: ('fst', 'dword ptr [edi + 0x574]'),
        0xB65316: ('fst', 'dword ptr [edi + 0x578]'),
        0xB65339: ('fstp', 'dword ptr [edi + 0x57c]'),
        0xB65341: ('fstp', 'dword ptr [edi + 0x580]'),
        0xB65366: ('call', '0xb63d20'),
        0xB63F2F: ('fild', 'dword ptr [ebp - 0x1c]'),
        0xB63F32: ('fstp', 'dword ptr [ebp - 0x30]'),
        0xB63F5A: ('fild', 'dword ptr [ebp - 0x18]'),
        0xB63F5D: ('fmul', 'dword ptr [ebx + 4]'),
        0xB63F60: ('fadd', 'dword ptr [ebx + 0xc]'),
        0xB63F63: ('fstp', 'dword ptr [ebp - 0x28]'),
        0xB63F66: ('fld', 'dword ptr [ebp - 0x30]'),
        0xB63F69: ('fmul', 'dword ptr [ebx + 4]'),
        0xB63F6C: ('fadd', 'dword ptr [ebx + 0x14]'),
        0xB63F6F: ('fstp', 'dword ptr [ebp - 0x24]'),
        0xB63FA6: ('push', 'eax'), 0xB63FAA: ('push', 'eax'),
        0xB63FAF: ('call', 'dword ptr [edx + 0x24]'),
        0xB65798: ('fmul', 'dword ptr [ebp - 4]'),
        0xB6579B: ('fadd', 'dword ptr [ebp - 0x58]'),
        0xB6579E: ('fstp', 'dword ptr [ebp - 0x1c]'),
        0xB657B1: ('fmul', 'dword ptr [ebp - 4]'),
        0xB657B6: ('fadd', 'dword ptr [ebp - 0x60]'),
        0xB657B9: ('fstp', 'dword ptr [ebp - 0x24]'),
        0xB657D2: ('mov', 'dword ptr [eax], ebx'),
        0xB657D4: ('fadd', 'dword ptr [ebp - 0x5c]'),
        0xB657E3: ('mov', 'dword ptr [eax + 4], ebx'),
        0xB657E9: ('mov', 'dword ptr [eax + 8], ebx'),
    }
    for address, expected in anchors.items():
        if instructions.get(address) != expected:
            raise ValueError(f'Anchor {address:08X}: {instructions.get(address)} != {expected}')
    assert mapped[0x15DC5E4-base:0x15DC5E8-base] == bytes.fromhex('0000003F')
    return {'ExecutableSha256': actual, 'InstructionAnchors': len(anchors),
            'HalfConstantBits': '3F000000', 'OriginalExecution': False,
            'OriginalGeneratedCoordinateComparison': False,
            'ActivePhaseArithmeticPolicies': 'Unknown'}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--exe', type=Path, required=True)
    p.add_argument('--packages', type=Path, required=True)
    p.add_argument('--receipt', type=Path)
    a = p.parse_args()
    result = verify(a.exe, a.packages)
    if a.receipt:
        if 'local-private' not in a.receipt.parts:
            p.error('Receipt must remain under local-private')
        a.receipt.parent.mkdir(parents=True, exist_ok=True)
        a.receipt.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
