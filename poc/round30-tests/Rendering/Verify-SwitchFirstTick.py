"""Static fresh-registration and first-query audit; never loads or executes original client code."""
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

# Independently selected instruction sites; no original-client execution.
CHECKS = [(11672448,
  11672656,
  [(11672614, 'mov', 'dword ptr [esi + 0x98], edi'),
   (11672627, 'mov', 'byte ptr [esi + 0x94], 1')]),
 (11683440,
  11683578,
  [(11683531, 'call', '0xb57190'), (11683548, 'mov', 'dword ptr [esi + 0x98], eax')]),
 (11891088,
  11891132,
  [(11891099, 'mov', 'dword ptr [eax], ecx'),
   (11891103, 'mov', 'dword ptr [eax + 4], edx'),
   (11891127, 'mov', 'dword ptr [eax + 0x24], ecx')]),
 (11796592,
  11796669,
  [(11796601, 'call', '0xb24cc0'),
   (11796637, 'mov', 'eax, dword ptr [0x1947014]'),
   (11796642, 'mov', 'ecx, dword ptr [0x1947010]'),
   (11796652, 'call', '0xb24670'),
   (11796658, 'call', '0xb40a80')]),
 (11683584,
  11683625,
  [(11683584, 'mov', 'ecx, dword ptr [ecx + 0x98]'), (11683600, 'push', 'ebp')]),
 (11799168, 11799283, [(11799273, 'call', '0xb24760')]),
 (11685056, 11685064, [(11685056, 'mov', 'al, byte ptr [ecx + 0x94]')]),
 (5640752,
  5640797,
  [(5640773, 'call', '0xb3f0d0'),
   (5640782, 'call', 'dword ptr [eax + 8]'),
   (5640786, 'call', '0xb40070')]),
 (11677536,
  11677671,
  [(11677623, 'mov', 'dword ptr [esi + 0x28], edi'),
   (11677633, 'call', 'dword ptr [eax + 0x28]'),
   (11677645, 'call', 'dword ptr [edx + 0x48]'),
   (11677664, 'call', 'dword ptr [edx + 0x50]')]),
 (11715632,
  11715739,
  [(11715645, 'mov', 'dword ptr [esi + 0xc], edi'),
   (11715724, 'call', 'dword ptr [edx + 0x2c]')]),
 (7942592,
  7942740,
  [(7942644, 'mov', 'dword ptr [esi + 0x54], eax'), (7942709, 'call', '0x793360')]),
 (7943008,
  7943093,
  [(7943032, 'call', 'dword ptr [eax + 0x18]'),
   (7943037, 'call', '0x7711e0'),
   (7943042, 'mov', 'dword ptr [esi + 0x54], eax')]),
 (7956483, 7956522, [(7956489, 'call', '0x7968b0'), (7956514, 'call', '0x793360')]),
 (7803360, 7803444, [(7803419, 'call', '0x77e4f0')]),
 (7857791, 7857835, [(7857828, 'call', '0x77e120')]),
 (7856416, 7856672, [(7856472, 'call', '0x7707a0')]),
 (7800736, 7800743, [(7800740, 'jmp', 'dword ptr [eax + 0x20]')]),
 (11906208,
  11906249,
  [(11906214, 'call', '0xb5b310'),
   (11906219, 'mov', 'eax, dword ptr [ebp + 8]'),
   (11906222, 'mov', 'ecx, dword ptr [ebp + 0xc]'),
   (11906225, 'mov', 'dword ptr [esi + 4], eax')]),
 (11800272,
  11800454,
  [(11800408, 'call', '0xb24710'), (11800430, 'call', 'dword ptr [eax + 0x10]')]),
 (11800784, 11800930, [(11800794, 'call', '0xb3fe60'), (11800846, 'call', '0xb40ed0')]),
 (7943104,
  7943130,
  [(7943112, 'mov', 'ecx, dword ptr [esi + 0x54]'),
   (7943121, 'mov', 'eax, dword ptr [ebp + 8]'),
   (7943125, 'call', '0x77e7e0')]),
 (7943408, 7943616, [(7943436, 'call', '0x77e840')]),
 (7858240, 7858311, [(7858259, 'call', '0x773870')]),
 (7813232, 7813267, [(7813245, 'call', 'dword ptr [0x19234a4]')]),
 (7811810,
  7811846,
  [(7811824, 'mov', 'dword ptr [0x19234a4], 0x773f50'), (7811834, 'call', '0xaa21e0')]),
 (7814992, 7815003, [(7815000, 'call', 'dword ptr [eax + 0xc]')]),
 (7857008,
  7857017,
  [(7857008, 'mov', 'ecx, dword ptr [ecx + 0xc]'),
   (7857011, 'mov', 'eax, dword ptr [ecx]'),
   (7857013, 'jmp', 'dword ptr [eax + 0x14]')]),
 (7887920,
  7887952,
  [(7887923, 'mov', 'ecx, dword ptr [esi + 0xc]'),
   (7887926, 'mov', 'eax, dword ptr [ecx]'),
   (7887928, 'call', 'dword ptr [eax + 0xc]'),
   (7887931, 'mov', 'ecx, dword ptr [esi + 0x10]')]),
 (11906432, 11906436, [(11906432, 'mov', 'eax, dword ptr [ecx + 0x10]')]),
 (11673509,
  11673567,
  [(11673522, 'call', '0xb400c0'),
   (11673543, 'call', '0xb571c0'),
   (11673552, 'call', '0xb57170'),
   (11673560, 'mov', 'dword ptr [esi + 0x98], edi')]),
 (11678165,
  11678208,
  [(11678189, 'call', '0xb40a50'), (11678205, 'call', 'dword ptr [eax + 0x10]')]),
 (7859184, 7859266, [(7859239, 'call', '0x785b00')]),
 (11906336,
  11906417,
  [(11906342, 'mov', 'eax, dword ptr [ecx + 4]'),
   (11906348, 'mov', 'dword ptr [ebp + 8], eax'),
   (11906367, 'mov', 'edx, dword ptr [ecx + 0x10]'),
   (11906371, 'mov', 'esi, dword ptr [ecx + 0xc]')])]
SLOTS = [(22987380, 'Object', 4, 11677952),
 (22955820, 'MeshAppearance', 8, 7943104),
 (22955820, 'MeshAppearance', 23, 7943408),
 (22952324, 'LocalShaderPrimitive', 3, 7857008),
 (22952788, 'SwitchTextureShader', 5, 7887920),
 (22989860, 'DeltaTimeSwitcher', 3, 11906432),
 (22952404, 'SwitchTextureShaderTemplate', 8, 7859184)]
receipts = []
for vt, name, slot, target in SLOTS:
    assert name in rtti(vt) and u32(vt+slot*4) == target, name
for lo, hi, expected in CHECKS:
    actual = {i.address: (i.mnemonic, i.op_str) for i in md.disasm(data[lo-base:hi-base], lo)}
    for site, mnemonic, operand in expected:
        assert actual.get(site) == (mnemonic, operand), (hex(site), actual.get(site))
    receipts.append(dict(start=f'{lo:08X}', end=f'{hi:08X}', checks=len(expected),
        sha256=hashlib.sha256(data[lo-base:hi-base]).hexdigest().upper()))
private = Path(__file__).resolve().parents[3]/'local-private'
assert a.receipt.resolve().is_relative_to(private.resolve()) and not a.receipt.exists()
a.receipt.write_text(json.dumps(dict(executable_sha256=identity, static_ranges=receipts,
    rtti_slots=len(SLOTS), first_visible_index='Unknown', original_execution=False), indent=2)+'\n')
print(f'PASS: {len(CHECKS)} ranges; {sum(len(x[2]) for x in CHECKS)} instruction sites; {len(SLOTS)} RTTI slots; original_execution=false')
