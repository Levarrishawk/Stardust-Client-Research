"""Static clock-to-SWTS audit; never loads or executes original client code."""
import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--exe', type=Path, required=True)
p.add_argument('--packages', type=Path, required=True)
p.add_argument('--receipt', type=Path, required=True)
a = p.parse_args()
identity = hashlib.sha256(a.exe.read_bytes()).hexdigest().upper()
if identity != 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150':
    raise ValueError('Executable identity mismatch before VA use')
sys.path.insert(0, str(a.packages))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

pe = pefile.PE(str(a.exe))
base = pe.OPTIONAL_HEADER.ImageBase
data = pe.get_memory_mapped_image()
md = Cs(CS_ARCH_X86, CS_MODE_32)
def u32(va):
    return struct.unpack_from('<I', data, va-base)[0]
def rtti(vt):
    offset = u32(u32(vt-4)+12)-base+8
    return data[offset:data.index(b'\0', offset)].decode('ascii')

exports = {s.name: base+s.address for s in pe.DIRECTORY_ENTRY_EXPORT.symbols}
assert exports[b'?frameTime@Clock@@SAMXZ'] == 0xaa4ec0
imports = {i.address: i.name for dll in pe.DIRECTORY_ENTRY_IMPORT for i in dll.imports}
assert imports[0x15dc244] == b'QueryPerformanceFrequency'
assert imports[0x15dc240] == b'QueryPerformanceCounter'
slots = [(0x15ec274, 'Object', 4, 0xb23100),
         (0x15e472c, 'MeshAppearance', 8, 0x7933c0),
         (0x15e3d84, 'DetailAppearance', 8, 0x787cb0),
         (0x15e3984, 'LocalShaderPrimitive@ShaderPrimitiveSet', 2, 0x77e2c0),
         (0x15e3b54, 'SwitchTextureShader', 4, 0x785c10),
         (0x15ecc24, 'DeltaTimeSwitcher', 2, 0xb5ad20),
         (0x15dd988, 'GroundScene', 1, 0x51af10)]
for vt, name, slot, target in slots:
    assert name in rtti(vt) and u32(vt+slot*4) == target, name
assert struct.unpack_from('<f', data, 0x15ec614-base)[0] == 1000.0
assert struct.unpack_from('<f', data, 0x15dc6f8-base)[0] == 1.0

checks = {
    0xaa4b80: (0xaa4c00, [('call', 'dword ptr [0x15dc244]'), ('fild', 'qword ptr [ebp - 0x1c]'), ('fstp', 'dword ptr [0x19446d4]')]),
    0xaa4de0: (0xaa4ead, [('call', 'dword ptr [0x15dc240]'), ('fdiv', 'dword ptr [0x19446d4]'), ('mov', 'dword ptr [0x1944720], ecx')]),
    0xaa4ec0: (0xaa4ec7, [('fld', 'dword ptr [0x1944720]')]),
    0x4237c0: (0x423af0, [('call', '0xaa0720'), ('call', '0xaa4ec0'), ('mov', 'ebx, dword ptr [ebp - 0x10]'), ('call', '0xab5310'), ('call', '0xab54f0')]),
    0xab5310: (0xab54e0, [('push', '4'), ('call', '0xab5670'), ('call', 'dword ptr [eax + 4]')]),
    0xab5620: (0xab5650, [('mov', 'edx, dword ptr [ebp + 0x14]'), ('mov', 'dword ptr [eax + 0x10], edx')]),
    0xab5670: (0xab5792, [('mov', 'eax, dword ptr [ebp + 0x14]'), ('call', '0xab5620')]),
    0x51af10: (0x51b264, [('mov', 'eax, dword ptr [edi + 0x10]'), ('mov', 'ebx, dword ptr [ebp + 8]'), ('call', '0x5616d0')]),
    0x5616d0: (0x561740, [('mov', 'al, byte ptr [0x19136bc]'), ('call', '0xb401c0'), ('call', '0xb401e0')]),
    0xb401c0: (0xb40200, [('call', '0xb410d0')]),
    0xb410d0: (0xb41162, [('call', '0xb3fe60'), ('call', '0xb40ed0')]),
    0xb3f6c0: (0xb3f6d3, [('fdiv', 'dword ptr [0x15ec614]'), ('fstp', 'dword ptr [0x194702c]')]),
    0xb3fe60: (0xb3feec, [('fmul', 'dword ptr [ebp + 8]'), ('call', '0x131e27c'), ('add', 'ecx, 1')]),
    0x131e27c: (0x131e2a3, [('or', 'ah, 0xc'), ('fistp', 'qword ptr [ebp - 0xc]'), ('fldcw', 'word ptr [ebp - 2]')]),
    0xb40ed0: (0xb40f86, [('call', '0xb24700'), ('fmul', 'dword ptr [0x194702c]'), ('fstp', 'dword ptr [ebp - 4]'), ('call', 'dword ptr [eax + 0x10]')]),
    0xb23100: (0xb2315a, [('test', 'al, 4'), ('test', 'al, 2'), ('call', 'dword ptr [eax + 0x20]')]),
    0x787cb0: (0x787d00, [('call', '0xb2c270'), ('call', 'dword ptr [edx + 0x20]'), ('jl', '0x787cd3')]),
    0x7933c0: (0x7933da, [('mov', 'ecx, dword ptr [esi + 0x54]'), ('call', '0x77e7e0')]),
    0x77e7e0: (0x77e82b, [('mov', 'ebx, dword ptr [ebp + 8]'), ('push', 'ebx'), ('call', 'dword ptr [edx + 8]')]),
    0x77e2c0: (0x77e2d3, [('call', 'dword ptr [eax + 0x10]')]),
    0x785c10: (0x785c29, [('call', 'dword ptr [eax + 8]')]),
    0xb5ad20: (0xb5ad71, [('fadd', 'dword ptr [ecx + 8]'), ('fst', 'dword ptr [ecx + 8]'), ('fsub', 'st(1)')]),
    0xaa7b00: (0xaa7bc0, [('and', 'eax, 0xf0ff'), ('or', 'eax, 0x3f'), ('call', '0xaa7c20')]),
    0xaa7bd0: (0xaa7bf3, [('cmp', 'ax, cx'), ('call', '0xaa7c20')]),
    0xaa0720: (0xaa07bf, [('call', '0xaa7bd0'), ('call', '0xaa4de0')]),
    0xa9fe20: (0xa9feb0, [('call', '0xaa7b00')]),
}
receipt_ranges = []
for lo, (hi, expected) in checks.items():
    instructions = [(i.mnemonic, i.op_str) for i in md.disasm(data[lo-base:hi-base], lo)]
    for instruction in expected:
        assert instruction in instructions, (hex(lo), instruction)
    receipt_ranges.append(dict(start=f'{lo:08X}', end=f'{hi:08X}',
        sha256=hashlib.sha256(data[lo-base:hi-base]).hexdigest().upper(), checks=len(expected)))
# Pin ordering separately from membership in the frame function.
frame = {i.address: (i.mnemonic, i.op_str) for i in md.disasm(data[0x4237c0-base:0x423af0-base], 0x4237c0)}
for site, target in [(0x4237dc, '0xaa0720'), (0x42383c, '0xaa4ec0'),
                     (0x423979, '0xab5310'), (0x423aa6, '0xab54f0')]:
    assert frame[site] == ('call', target)
private = Path(__file__).resolve().parents[3]/'local-private'
assert a.receipt.resolve().is_relative_to(private.resolve()) and not a.receipt.exists()
a.receipt.write_text(json.dumps(dict(executable_sha256=identity, static_ranges=receipt_ranges,
    rtti_slots=len(slots), frame_units='seconds', ticks_per_second=1000,
    minimum_tick_advance=1, configured_pc=0, configured_rc=0,
    actual_callback_control_word='unobserved', first_visible_index='Unknown',
    original_execution=False), indent=2)+'\n')
print(f'PASS: {len(checks)} static ranges; {len(slots)} RTTI slots; original_execution=false')
