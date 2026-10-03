"""Static CREO6 inheritance/codec prerequisite audit. Never executes the original client."""
import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path

EXPECTED = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
# (member offset, vtable, snapshot, delta, RTTI identity token): one row per field.
FIELDS = [
 (0x230,0x015DE08C,0x00542DA0,0x00542DC0,'AutoDeltaVariable@K'),
 (0x2F0,0x015E0614,0x006421A0,0x006423E0,'AutoDeltaVector@VCachedNetworkId'),
 (0x90C,0x015DCD74,0x00440950,0x00440970,'AutoDeltaVariable@F'),
 (0x8E0,0x015DCD98,0x004405E0,0x00440740,'AnimatingSkillData'),
 (0x558,0x015DCFA8,0x0043C950,0x0043CB90,'AnimationMoodChanged'),
 (0x5C8,0x015DCF60,0x0043CE80,0x0043CF80,'AutoDeltaVariable@VCachedNetworkId'),
 (0x7A8,0x015DCE4C,0x0043F960,0x0043FA90,'GroupChanged'),
 (0x7E0,0x015DCE28,0x0043FF50,0x00440020,'pair@VNetworkId@@V1'),
 (0x868,0x015DD014,0x004398F0,0x00439A20,'AutoDeltaVariable@H'),
 (0x588,0x015DCF84,0x0043D0C0,0x0043D1F0,'LookAtTargetChanged'),
 (0x530,0x015DCFF0,0x0043C640,0x0043C690,'MoodChanged'),
 (0x834,0x015DD014,0x004398F0,0x00439A20,'AutoDeltaVariable@H'),
 (0x818,0x015DCE04,0x00440160,0x004401B0,'PerformanceTypeChanged'),
 (0x404,0x015DD07C,0x0043B050,0x0043B1A0,'AutoDeltaVector@HVCreatureObject'),
 (0x43C,0x015DD07C,0x0043B050,0x0043B1A0,'AutoDeltaVector@HVCreatureObject'),
 (0x91C,0x015DCD34,0x00441740,0x00441920,'AutoDeltaVector@VWearableEntry'),
 (0x954,0x015DCD10,0x00442080,0x004421E0,'AlternateAppearanceSharedObjectTemplateNameChanged'),
 (0x9A4,0x015DCCEC,0x004423F0,0x00442410,'AutoDeltaVariable@_N'),
]

ANCHORS = [
    (0x0042ED2A, 'call', '0x63ee10'),
    (0x0063EE3C, 'call', '0x554240'),
    (0x00554420, 'mov', 'dword ptr [edi], 0x15de08c'),
    (0x0063EEF3, 'mov', 'dword ptr [edi], 0x15e0614'),
    (0x00554661, 'lea', 'eax, [esi + 0x230]'),
    (0x0055466A, 'call', '0x5554c0'),
    (0x0063F079, 'lea', 'eax, [esi + 0x2f0]'),
    (0x0063F082, 'call', '0x5554c0'),
    (0x005554C6, 'mov', 'eax, dword ptr [ecx + 0x144]'),
    (0x005554CC, 'add', 'ecx, 0x144'),
    (0x005554D3, 'call', 'dword ptr [eax + 0x10]'),
    (0x0118A846, 'sub', 'edx, eax'),
    (0x0118A84C, 'sar', 'edx, 2'),
    (0x0118A84F, 'mov', 'word ptr [eax + 4], dx'),
    (0x0118A85A, 'jmp', '0x118b5f0'),
    (0x0118B747, 'push', '2'),
    (0x0118B752, 'call', '0x439910'),
    (0x0118B757, 'mov', 'esi, dword ptr [edi + 4]'),
    (0x0118B75A, 'cmp', 'esi, dword ptr [edi + 8]'),
    (0x0118B765, 'call', 'dword ptr [edx + 8]'),
    (0x0118B76B, 'add', 'esi, 4'),
    (0x0118B770, 'jne', '0x118b760'),
    (0x0118AA09, 'push', '2'),
    (0x0118AA14, 'call', '0x439910'),
    (0x0118AA2A, 'sub', 'ecx, dword ptr [esi]'),
    (0x0118AA30, 'je', '0x118aa53'),
    (0x0118AA32, 'push', '2'),
    (0x0118AA40, 'movzx', 'eax, word ptr [esp + 0x14]'),
    (0x0118AA48, 'mov', 'ecx, dword ptr [ecx + eax*4]'),
    (0x0118AA4E, 'call', 'dword ptr [edx + 0x18]'),
    (0x0118AA51, 'jmp', '0x118aa20'),
    (0x00554D84, 'mov', 'edx, dword ptr [esi + 0x144]'),
    (0x00554D8A, 'lea', 'ecx, [esi + 0x144]'),
    (0x00554F22, 'add', 'esi, 0x144'),
    (0x00554F0B, 'call', 'dword ptr [eax + 0x18]'),
    (0x00554F12, 'call', 'dword ptr [edx + 0x1c]'),
    (0x0042F521, 'mov', 'dword ptr [edi], 0x15dcd74'),
    (0x0042F500, 'mov', 'dword ptr [edi], 0x15dcd98'),
    (0x0042EFAD, 'mov', 'dword ptr [edi], 0x15dcfa8'),
    (0x0042F007, 'mov', 'dword ptr [edi], 0x15dcf60'),
    (0x0042F33A, 'mov', 'dword ptr [edi], 0x15dce4c'),
    (0x0042F37E, 'mov', 'dword ptr [edi], 0x15dce28'),
    (0x0042F435, 'mov', 'dword ptr [edi], 0x15dd014'),
    (0x0042EFCB, 'mov', 'dword ptr [edi], 0x15dcf84'),
    (0x0042EF6B, 'mov', 'dword ptr [edi], 0x15dcff0'),
    (0x0042F3DA, 'mov', 'dword ptr [edi], 0x15dd014'),
    (0x0042F3BB, 'mov', 'dword ptr [edi], 0x15dce04'),
    (0x0042EDDB, 'mov', 'dword ptr [edi], 0x15dd07c'),
    (0x0042EE2E, 'mov', 'dword ptr [edi], 0x15dd07c'),
    (0x0042F570, 'mov', 'dword ptr [edi], 0x15dcd34'),
    (0x0042F58C, 'mov', 'dword ptr [edi], 0x15dcd10'),
    (0x0042F5EB, 'mov', 'dword ptr [edi], 0x15dccec'),
    (0x00440959, 'push', '2'),
    (0x004398F9, 'push', '4'),
    (0x00440171, 'push', '4'),
    (0x0043C651, 'push', '1'),
    (0x004423F9, 'push', '1'),
    (0x0043CE8E, 'call', '0x43ceb0'),
    (0x0043F9B4, 'push', '8'),
    (0x0043D114, 'push', '8'),
    (0x0043FF9B, 'call', '0xaa5830'),
    (0x0043FFA5, 'call', '0xaa5830'),
    (0x0044006B, 'call', '0xaa5830'),
    (0x00440075, 'call', '0xaa5830'),
    (0x00AA57F9, 'push', '8'),
    (0x00AA57FC, 'call', '0x439910'),
    (0x00AA5851, 'call', '0xaa57f0'),
    (0x0044063C, 'push', '2'),
    (0x00440654, 'cmp', 'ax, 0xffff'),
    (0x00440664, 'push', '4'),
    (0x004406A6, 'add', 'edx, eax'),
    (0x004407B4, 'cmp', 'ax, 0xffff'),
    (0x0043C9C4, 'cmp', 'ax, 0xffff'),
    (0x0043CC04, 'cmp', 'ax, 0xffff'),
    (0x004420F4, 'cmp', 'ax, 0xffff'),
    (0x00442254, 'cmp', 'ax, 0xffff'),
    (0x00439F8D, 'push', '2'),
    (0x00439FA2, 'cmp', 'ax, 0xffff'),
    (0x00439FBF, 'push', '4'),
    (0x00B94C6D, 'call', '0x439f60'),
    (0x00B94C75, 'push', '4'),
    (0x00B94C87, 'call', '0xaa5830'),
    (0x00B94C8F, 'push', '4'),
    (0x0043B07F, 'push', '4'),
    (0x0043B08C, 'push', '4'),
    (0x0043B0B0, 'push', '4'),
    (0x0043B1C1, 'push', '4'),
    (0x0043B1D0, 'push', '4'),
    (0x0043B1E8, 'sub', 'ebx, ecx'),
    (0x0043B1EA, 'add', 'ebx, edx'),
    (0x0043B1F1, 'jbe', '0x43b1f8'),
    (0x0043B203, 'push', '1'),
    (0x0043B217, 'push', '2'),
    (0x0043B227, 'cmp', 'al, 3'),
    (0x0043B236, 'push', '4'),
    (0x0043B280, 'jmp', 'dword ptr [eax*4 + 0x43b5a0]'),
    (0x004417CD, 'push', '4'),
    (0x004417E7, 'push', '4'),
    (0x0044180D, 'call', '0xb94c60'),
    (0x0044194E, 'push', '4'),
    (0x00441962, 'push', '4'),
    (0x00441975, 'sub', 'ebx, eax'),
    (0x0044197A, 'add', 'ebx, eax'),
    (0x00441981, 'jbe', '0x441988'),
    (0x00441993, 'push', '1'),
    (0x004419A7, 'push', '2'),
    (0x004419B7, 'cmp', 'al, 3'),
    (0x004419CF, 'call', '0xb94c60'),
    (0x00441A1A, 'jmp', 'dword ptr [eax*4 + 0x441e70]'),
]

def audit(image, packages):
    raw = image.read_bytes()
    digest = hashlib.sha256(raw).hexdigest().upper()
    if digest != EXPECTED:
        raise ValueError('Executable SHA-256 mismatch; rejected before PE imports or VA analysis')
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw, fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    assert base == 0x00400000
    read = lambda va, n: pe.get_data(va-base, n)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    instruction = lambda va: next(md.disasm(read(va,15),va))
    for va, mnemonic, operand in ANCHORS:
        i = instruction(va)
        assert (i.mnemonic,i.op_str) == (mnemonic,operand), (hex(va),i.mnemonic,i.op_str)
    rows = []
    for index,(offset,vt,snapshot,delta,token) in enumerate(FIELDS):
        slots = struct.unpack('<8I',read(vt,32))
        assert (slots[2],slots[6]) == (snapshot,delta), index
        col = struct.unpack('<I',read(vt-4,4))[0]
        td = struct.unpack('<I',read(col+12,4))[0]
        name = read(td+8,512).split(b'\0')[0].decode('ascii')
        assert token in name and name.startswith('.?AV?$AutoDelta'), (index,name)
        rows.append(dict(Index=index,Member=f'{offset:X}',Vtable=f'{vt:08X}',Snapshot=f'{snapshot:08X}',Delta=f'{delta:08X}',RttiToken=token))
    # Verify every creature registration, not just nearby strings/RTTI.
    registered = []
    ctor = list(md.disasm(read(0x0042ECF0,0xD0D),0x0042ECF0))
    for pos,i in enumerate(ctor):
        if i.mnemonic == 'call' and i.op_str == '0x5554c0':
            preceding = ctor[pos-3:pos]
            assert [(x.mnemonic,x.op_str) for x in preceding[1:]] == [('push','eax'),('mov','ecx, esi')]
            assert preceding[0].mnemonic == 'lea'
            registered.append(preceding[0].op_str)
    assert registered == [f'eax, [esi + 0x{r[0]:x}]' for r in FIELDS[2:]]
    assert struct.unpack('<9I',read(0x00554E68,36))[5] == 0x00554D84
    assert struct.unpack('<9I',read(0x00554FE4,36))[5] == 0x00554F22
    assert len(set(v for _,v,_,_,_ in FIELDS)) == 16
    return dict(ExeSha256=digest,InstructionAnchors=len(ANCHORS),FieldRegistrations=18,
                CreatureAddedRegistrations=len(registered),DistinctRttiVtables=16,Fields=rows,
                SharedPrefixIndices=[0,1],FirstCreatureField=2,ClientFields=18,
                DeclaredCountIsLoopBound=False,OriginalExecuted=False)

if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--exe',type=Path,required=True)
    p.add_argument('--packages',type=Path,required=True)
    p.add_argument('--receipt',type=Path,required=True)
    a = p.parse_args()
    # Detailed machine-readable observations remain ignored/private.
    repo = Path(__file__).resolve().parents[3]
    if not a.receipt.resolve().is_relative_to((repo/'local-private').resolve()):
        p.error('receipt must be under repository local-private')
    try:
        result = audit(a.exe,a.packages)
    except ValueError as error:
        p.exit(1,str(error)+'\n')
    a.receipt.parent.mkdir(parents=True,exist_ok=True)
    a.receipt.write_bytes((json.dumps(result,indent=2)+'\n').encode('utf-8'))
    print(f"PASS {result['InstructionAnchors']} anchors, 18 registrations, 16 RTTI vtables; static only")
