"""Identity-gated static CREO4 template initialization audit; never executes the client."""
import argparse
import hashlib
import json
import runpy
import struct
import sys
from pathlib import Path

EXPECTED_EXE = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS = [
    (0x429819, 'mov', 'esi, dword ptr [ebp + 8]'),
    (0x4298A7, 'cmp', 'byte ptr [esi], 0'),
    (0x4298AA, 'jne', '0x429c29'),
    (0x4299DC, 'push', '0'),
    (0x4299DE, 'call', '0x612600'),
    (0x6125AA, 'call', '0x11ace10'),
    (0x6125AF, 'mov', 'dword ptr [esi], 0x15dfb9c'),
    (0x612632, 'mov', 'byte ptr [0x18de794], al'),
    (0x61263C, 'push', '0x612670'),
    (0x612641, 'push', '0x53434f54'),
    (0x612646, 'call', '0xb28480'),
    (0x6126EA, 'push', '0x9b8'),
    (0x6126EF, 'mov', 'esi, ecx'),
    (0x612707, 'push', 'esi'),
    (0x612708, 'mov', 'ecx, eax'),
    (0x61270A, 'call', '0x42ecf0'),
    (0x61271D, 'call', '0x623530'),
    (0x42ED1F, 'mov', 'eax, dword ptr [ebx + 8]'),
    (0x42ED2A, 'call', '0x63ee10'),
    (0x11ACEA3, 'lea', 'edx, [esi + 0x498]'),
    (0x11ACEAE, 'call', '0x131f460'),
    (0x11ACEE0, 'lea', 'ecx, [esi + 0x514]'),
    (0x11ACEEA, 'call', '0x13fa000'),
    (0x11ACEEF, 'lea', 'ecx, [esi + 0x52c]'),
    (0x11ACEF9, 'call', '0x13fa000'),
    (0x11ACEFE, 'lea', 'ecx, [esi + 0x544]'),
    (0x11ACF08, 'call', '0x13fa000'),
    (0x13FA004, 'mov', 'dword ptr [eax + 8], ecx'),
    (0x13FA007, 'mov', 'byte ptr [eax + 0x10], cl'),
    (0x13FA00A, 'mov', 'byte ptr [eax + 0x14], 0x20'),
    (0x13FA00E, 'mov', 'dword ptr [eax], 0x1609ab8'),
    (0x11ADE4B, 'lea', 'ebx, [esi + esi*2]'),
    (0x11ADE51, 'shl', 'ebx, 3'),
    (0x11ADE54, 'mov', 'eax, dword ptr [edi + 0x18]'),
    (0x11ADE71, 'call', '0x131e6f1'),
    (0x11ADE7C, 'mov', 'al, byte ptr [ebx + edi + 0x4a8]'),
    (0x11ADE87, 'mov', 'al, byte ptr [0x18de794]'),
    (0x11ADE8E, 'je', '0x11ade97'),
    (0x11ADE95, 'je', '0x11ade9c'),
    (0x11ADE9C, 'fld', 'dword ptr [0x15dc6a0]'),
    (0x11ADEBE, 'jmp', 'dword ptr [eax*4 + 0x11adfd4]'),
    (0x11ADEC9, 'call', 'dword ptr [edx + 0x14]'),
    (0x11ADED4, 'call', '0xb59ef0'),
    (0x11ADEDF, 'idiv', 'ecx'),
    (0x11ADEF0, 'sub', 'edx, dword ptr [eax + 4]'),
    (0x11ADEFE, 'fld', 'dword ptr [0x19bbbbc]'),
    (0x11ADF04, 'mov', 'bl, byte ptr [ebx + edi + 0x4ac]'),
    (0x11ADF0B, 'fst', 'dword ptr [ebp - 8]'),
    (0x11ADF41, 'call', '0x11ade40'),
    (0x11ADF46, 'fstp', 'dword ptr [ebp - 4]'),
    (0x11ADF51, 'fadd', 'dword ptr [ebp - 4]'),
    (0x11ADF78, 'call', '0x11a1200'),
    (0x11ADF83, 'call', 'dword ptr [eax + 0x18]'),
    (0x11ADF8F, 'call', 'dword ptr [edx + 0x1c]'),
    (0x11ADFA1, 'fmul', 'dword ptr [0x15de0e4]'),
    (0x11ADFAA, 'fadd', 'dword ptr [0x15dc6f8]'),
    (0x11ADFB0, 'fmul', 'dword ptr [ebp - 4]'),
    (0x11ADFC7, 'fsubr', 'dword ptr [ebp - 4]'),
    (0x11AE730, 'mov', 'al, byte ptr [ebx + 0x524]'),
    (0x11AE745, 'je', '0x11ae74e'),
    (0x11AE75C, 'mov', 'eax, dword ptr [ebx + 0x51c]'),
    (0x11AE763, 'lea', 'esi, [ebx + 0x514]'),
    (0x11AE7B4, 'mov', 'bl, byte ptr [ebx + 0x528]'),
    (0x11AEAC0, 'mov', 'al, byte ptr [ebx + 0x53c]'),
    (0x11AEAEC, 'mov', 'eax, dword ptr [ebx + 0x534]'),
    (0x11AEAF3, 'lea', 'esi, [ebx + 0x52c]'),
    (0x11AEE50, 'mov', 'al, byte ptr [ebx + 0x554]'),
    (0x11AEE7C, 'mov', 'eax, dword ptr [ebx + 0x54c]'),
    (0x11AEE83, 'lea', 'esi, [ebx + 0x544]'),
    (0x13FE640, 'fld', 'dword ptr [ecx + 4]'),
    (0x13FA05A, 'call', '0xb59ef0'),
    (0x13FA068, 'fsub', 'dword ptr [ebp - 4]'),
    (0x13FA06D, 'fmul', 'dword ptr [0x15dc804]'),
    (0x13FA073, 'fadd', 'dword ptr [ebp - 4]'),
    (0x13FE660, 'fld', 'dword ptr [0x19bbb30]'),
    (0x11B16F3, 'lea', 'edi, [ecx + 0x498]'),
    (0x11B170E, 'call', 'dword ptr [edx]'),
    (0x11B1828, 'add', 'ecx, 0x514'),
    (0x11B184C, 'add', 'ecx, 0x52c'),
    (0x11B1870, 'add', 'ecx, 0x544'),
    (0x11B1A68, 'call', 'dword ptr [edx]'),
    (0x42FB38, 'call', '0x11ade40'),
    (0x42FB45, 'call', '0x11aca20'),
    (0x11ACB19, 'call', '0xb32890'),
    (0x11ACB3F, 'call', 'dword ptr [edx + 0x20]'),
    (0x623559, 'push', '0x623570'),
    (0x62355E, 'call', '0x7d79f0'),
    (0x623679, 'call', '0x5f1c60'),
    (0x623A42, 'ret', ''),
    (0x7D683B, 'mov', 'dword ptr [eax], ecx'),
    (0x7D683D, 'mov', 'dword ptr [eax + 4], edx'),
    (0x7D7A25, 'mov', 'edx, dword ptr [eax]'),
    (0x7D7A27, 'mov', 'dword ptr [ecx], edx'),
    (0x7D7A2C, 'mov', 'dword ptr [ecx + 4], eax'),
    (0x7D7AFE, 'call', '0x7d6c10'),
    (0x5F1C75, 'call', '0x5f1cd0'),
    (0x5F1CB8, 'call', '0x5f1cd0'),
    (0x5F1D05, 'call', '0x131e6f1'),
    (0x5F1D27, 'call', '0x11aa310'),
    (0x5F1D2D, 'call', '0x131e6f1'),
    (0x5F1D4A, 'call', '0x605dd0'),
]
TABLES = {
    0x11ADFD4: [0x11ADEC5, 0x11ADECE, 0x11ADF7F, 0x11ADF8B],
    0x11AE880: [0x11AE776, 0x11AE77F, 0x11AE82E, 0x11AE83A],
    0x11AEC10: [0x11AEB06, 0x11AEB0F, 0x11AEBBE, 0x11AEBCA],
    0x11AEFA0: [0x11AEE96, 0x11AEE9F, 0x11AEF4E, 0x11AEF5A],
}
CONSTANTS = {0x15DC6A0: '00000000', 0x15DC6F8: '0000803f',
             0x15DE0E4: '0ad7233c', 0x15DCBD4: 'db0f4940',
             0x15DC5F0: '610bb63b', 0x15DC804: '00000030'}
STRINGS = {0x18DE898: b'speed', 0x18DE864: b'slopeModAngle',
           0x18DE854: b'slopeModPercent', 0x18DE844: b'waterModPercent',
           0x1882330: b'event_', 0x1882324: b'hpevent_',
           0x1880820: b'footstep', 0x188231C: b'swing_',
           0x1882314: b'uswing_', 0x188230C: b'hit_', 0x1882304: b'uhit_'}
RTTI = {0x186B44C: b'.?AVObjectTemplate@@',
        0x186B6EC: b'.?AVSharedCreatureObjectTemplate@@',
        0x1881420: b'.?AVClientCreatureObjectTemplate@@',
        0x187A08C: b'.?AVRangedIntCustomizationVariable@@',
        0x187A068: b'.?AVCustomizationVariable@@',
        0x186944C: b'.?AVSkeletalAppearance2@@',
        0x1869430: b'.?AVAppearance@@',
        0x1878D90: b'.?AVSharedObjectTemplate@@',
        0x1878D70: b'.?AVClientDataFile@@',
        0x1878D40: b'.?AVSharedObjectTemplateClientData@@',
        0x18DDEF8: b'.?AV?$TemplateBase@MM@@'}


def verified_bytes(exe):
    raw = exe.read_bytes()
    if hashlib.sha256(raw).hexdigest().upper() != EXPECTED_EXE:
        raise ValueError('Wrong executable SHA256; rejected before imports/VA use')
    return raw


def receipt_path(path, repo):
    target = path.resolve()
    if not target.is_relative_to((repo / 'local-private').resolve()):
        raise ValueError('Receipt must remain under ignored local-private')
    return target


def audit(exe, packages, server_root, repo):
    raw = verified_bytes(exe)
    if not packages.is_dir():
        raise ValueError('Static-analysis package directory is absent')
    # Reuse the frozen delivery/registration evidence, including all 58 constructor anchors.
    delivery = runpy.run_path(str(repo / 'tools/python/verify_creature_view4_delivery.py'))
    prerequisite = delivery['audit'](server_root, exe, packages, repo)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    read = lambda va, n: pe.get_data(va - pe.OPTIONAL_HEADER.ImageBase, n)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    for va, mnemonic, operands in ANCHORS:
        ins = next(md.disasm(read(va, 15), va))
        if (ins.mnemonic, ins.op_str) != (mnemonic, operands):
            raise ValueError(f'Instruction mismatch at {va:08X}: {ins.mnemonic} {ins.op_str}')
    for va, values in TABLES.items():
        if list(struct.unpack('<4I', read(va, 16))) != values:
            raise ValueError(f'Jump table mismatch at {va:08X}')
    for va, expected in CONSTANTS.items():
        if read(va, 4).hex() != expected:
            raise ValueError(f'Constant mismatch at {va:08X}')
    for va, expected in STRINGS.items():
        if read(va, len(expected) + 1) != expected + b'\0':
            raise ValueError(f'String mismatch at {va:08X}')
    for va, expected in RTTI.items():
        if read(va + 8, len(expected) + 1) != expected + b'\0':
            raise ValueError(f'RTTI mismatch at {va:08X}')
    # Follow complete-object locator to its type descriptor; do not name from nearby strings.
    for vt, td, factory in [(0x15DFB9C, 0x1881420, 0x6126D0),
                            (0x15FEBE4, 0x186B6EC, 0xB2E7A0)]:
        col = struct.unpack('<I', read(vt - 4, 4))[0]
        if struct.unpack('<I', read(col + 12, 4))[0] != td:
            raise ValueError('Vtable RTTI mismatch')
        if struct.unpack('<I', read(vt + 24, 4))[0] != factory:
            raise ValueError('Template factory slot mismatch')
    if struct.unpack('<3I', read(0x1609AB8 + 20, 12)) != (0x13FE640, 0x13FA040, 0x13FE660):
        raise ValueError('Float parameter vtable mismatch')
    return dict(ExeSha256=EXPECTED_EXE, InstructionAnchors=len(ANCHORS),
                GetterJumpTables=len(TABLES), Constants=len(CONSTANTS),
                Strings=len(STRINGS), RttiDescriptors=len(RTTI),
                TemplateVtables=2, FloatParameterVtable=1,
                ConstructorAnchors=prerequisite['Constructor']['InstructionAnchors'],
                PrerequisiteDispatchAnchors=prerequisite['NewClientAnchors'],
                PrerequisiteRegistrationAnchors=prerequisite['PrerequisiteAnchors'],
                Registrations=prerequisite['Registrations'],
                PinnedServerFiles=len(prerequisite['ServerFileSha256']),
                SourceTokenChecks=prerequisite['SourceTokenChecks'],
                CompleteInitialModel=False, EffectiveCorpusValuesVerified=False,
                ObservedInstallerArgument=0, InstallerConditional=True,
                LaterOverrides='Transitive callees, effective startup path and globals remain open',
                OriginalExecuted=False, LiveContact=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    try:
        target = receipt_path(args.receipt, repo)
        result = audit(args.exe, args.packages, args.server_root, repo)
        target.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    except (ValueError, AssertionError, OSError, ImportError, StopIteration) as error:
        parser.exit(1, str(error) + '\n')
    print(f"PASS {result['InstructionAnchors']} initialization anchors / four getter tables / two template RTTI-vtables; effective values remain unverified")


if __name__ == '__main__':
    main()
