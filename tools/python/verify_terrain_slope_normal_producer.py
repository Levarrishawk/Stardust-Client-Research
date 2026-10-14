"""Hash-first, static-only check of the FSLP current-height normal producer. No original execution."""
import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path

EXPECTED='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
BODIES={
    (0x849040,0x849064):'1965ED4C87E2810DC98743D74E8355FE124C0BBACD79A054E24F5A35FBED120F',
    (0xB5CF20,0xB5CF67):'1B6016F597D546704AF30DAA6FA944AFB7D986E60E70BBDA678DAF9436445346',
    (0xB65730,0xB65AEA):'081AA87C019BA78CD777215C8B8F76AE0B9DFFE798AA66CE2F3C57DFCEEAF4CB',
    (0x413760,0x41377A):'204472FDE081CBD193ADC34DAD5B003D18F093A1A537856CA14D93797C67A3CA',
    (0x507820,0x507844):'AC5A4B23AE37AA608B00E72C113088FA7E0617ADA95C50E71CB4742DB5219212',
    (0xB66580,0xB6659B):'6F547EB07711E97567B17C32D0FBAA22320E4D690E03F59F9880240534A539F6',
    (0xAB5C40,0xAB5C8F):'626FFC7BAAB77947E0A63D269F091AFE323FF12BD85A44D7F79E5062E02DF4A0',
    (0xB632D0,0xB63309):'D721CF2DCAE6C7C0C502F3CE284803001F24032C4C69085D1AB0ADA3F83A60A9',
    (0xB63250,0xB632CE):'B2DDCDC8B16483F578672EBA6052730918F42DB1186545BC062273D5FD4A06BF',
    (0xB63D20,0xB642A0):'FAF4101C1E12E004C624948971AA18EEDB5167F3A27D2C7661AD9AD38A3969E3',
    (0x84A530,0x84A83E):'0CC78C112717309442C7AFF7B75A7021419C541BF323B1B94B26898D41805C6F',
    (0xB65280,0xB653B3):'0C55A473655118D85ED484EEA02839B1A0687094101945B03BB649BC2E0DEE51',
    (0xB78A90,0xB78B41):'DF68FD729A8E86E04E03C63039063165B295207502A20D3EF6341775A3EAB89D',
}
ANCHORS={
    0x84904A:('call','0xb5cf20'),
    0xB5CF28:('mov','dword ptr [eax + 4], ecx'),
    0xB65739:('mov','eax, dword ptr [ecx + 0x18]'),
    0xB6573C:('mov','edx, dword ptr [ecx + 0x38]'),
    0xB65740:('mov','ebx, dword ptr [ecx + 0x3c]'),
    0xB65744:('mov','esi, dword ptr [ecx]'),
    0xB65747:('mov','edi, dword ptr [ecx + 4]'),
    0xB657BC:('fld','dword ptr [eax + ebx*4]'),
    0xB657D2:('mov','dword ptr [eax], ebx'),
    0xB657D4:('fadd','dword ptr [ebp - 0x5c]'),
    0xB657E3:('mov','dword ptr [eax + 4], ebx'),
    0xB657E9:('mov','dword ptr [eax + 8], ebx'),
    0xB657F9:('mov','eax, dword ptr [ebx + 4]'),
    0xB657FC:('imul','eax, dword ptr [ebx]'),
    0xB65839:('rep stosd','dword ptr es:[edi], eax'),
    0xB65842:('lea','eax, [esi - 1]'),
    0xB65884:('lea','ecx, [ecx + ecx*2 + 3]'),
    0xB65899:('lea','ebx, [eax + eax*2 + 3]'),
    0xB658C1:('call','0x413760'),
    0xB658E7:('call','0x413760'),
    0xB65927:('call','0x413760'),
    0xB65962:('call','0x413760'),
    0xB65988:('call','0x413760'),
    0xB659C8:('call','0x413760'),
    0xB659FB:('fadd','dword ptr [eax]'),
    0xB659FD:('fstp','dword ptr [eax]'),
    0xB65A05:('fstp','dword ptr [eax + 4]'),
    0xB65A0E:('fstp','dword ptr [eax + 8]'),
    0xB65A52:('call','0x507820'),
    0xB65A65:('call','0xb66580'),
    0xB65A6C:('call','0x507820'),
    0xB65A79:('call','0xb66580'),
    0xB65A80:('call','0x507820'),
    0xB65A90:('call','0xb66580'),
    0xB65A97:('call','0x507820'),
    0xB65AD4:('call','0xab5c40'),
    0xB65ADA:('cmp','edi, esi'),
    0xB65ADF:('cmp','ebx, esi'),
    0xB65AE9:('ret',''),
    0xAB5C58:('fsqrt',''),
    0xAB5C60:('fcom','dword ptr [0x15eaaf8]'),
    0xAB5C6B:('jp','0xab5c72'),
    0xAB5C6F:('xor','al, al'),
    0xAB5C72:('fdivr','dword ptr [0x15dc6f8]'),
    0xAB5C7E:('fstp','dword ptr [ecx]'),
    0xAB5C85:('fstp','dword ptr [ecx + 4]'),
    0xAB5C8B:('fstp','dword ptr [ecx + 8]'),
    0xB66585:('imul','eax, dword ptr [ebp + 0xc]'),
    0xB66591:('lea','eax, [eax + eax*2]'),
    0xB66594:('lea','eax, [ecx + eax*4]'),
    0xB63E98:('push','ebx'),
    0xB63E99:('call','0xb65730'),
    0xB63EA1:('mov','byte ptr [ebx + 0x570], 0'),
    0xB640A6:('push','ebx'),
    0xB640B8:('call','dword ptr [edx + 0x14]'),
    0xB641A1:('mov','byte ptr [eax + 0x570], 1'),
    0xB65324:('mov','byte ptr [edi + 0x570], al'),
    0xB65381:('call','0xb65730'),
    0xB78A99:('mov','edx, dword ptr [eax + 0x3c]'),
    0xB78AB7:('fld','dword ptr [edx + eax*4 + 4]'),
    0x84A627:('call','0x849040'),
    0x84A6F0:('lea','ecx, [eax + 0xc]'),
    0x84A729:('lea','edx, [eax + 0x60]'),
    0x84A732:('lea','ecx, [eax + 0x6c]'),
    0x84A735:('mov','dword ptr [ebp - 0x5fc], eax'),
    0x84A73B:('mov','dword ptr [ebp - 0x5dc], edx'),
    0x84A752:('mov','dword ptr [ebp - 0x5d8], ecx'),
    0x84A785:('lea','eax, [ebp - 0x614]'),
    0x84A79E:('call','0xb65400'),
    0xB632EA:('mov','dword ptr [ecx + 0x38], eax'),
    0xB632ED:('mov','dword ptr [ecx + 0x3c], eax'),
}


DIRTY_PREDICATES = {0x15ED678:0xB76640, 0x15ED698:0xB76980,
                    0x15ED6BC:0xB771A0, 0x15EDD70:0xB87DF0}


def gate(raw):
    if hashlib.sha256(raw).hexdigest().upper()!=EXPECTED:raise ValueError('Executable identity mismatch before VA interpretation')


def check(image,base,decoder):
    for table,function in DIRTY_PREDICATES.items():
        if struct.unpack_from('<I',image,table+0x14-base)[0]!=function:
            raise ValueError(f'Height dirty predicate slot mismatch {table:08X}')
        if image[function-base:function-base+3]!=bytes.fromhex('B001C3'):
            raise ValueError(f'Height dirty predicate body mismatch {function:08X}')
    instructions={}
    for (start,end),expected in BODIES.items():
        data=image[start-base:end-base]
        if hashlib.sha256(data).hexdigest().upper()!=expected:raise ValueError(f'Body identity mismatch {start:08X}')
        for ins in decoder.disasm(data,start):instructions[ins.address]=(ins.mnemonic,ins.op_str)
    for va,expected in ANCHORS.items():
        if instructions.get(va)!=expected:raise ValueError(f'Anchor mismatch {va:08X}: {instructions.get(va)}')
    assert struct.unpack_from('<I',image,0x15EAAF8-base)[0]==0x3727C5AC
    assert struct.unpack_from('<I',image,0x15DC6F8-base)[0]==0x3F800000
    assert not any(m in ('fldcw','fnstcw','fstcw') for va,(m,_) in instructions.items() if 0xB65730<=va<0xB65AEA or 0xAB5C40<=va<0xAB5C8F)
    return dict(ExecutableSha256=EXPECTED,ProducerBytes=954,BodyIdentities=len(BODIES),InstructionAnchors=len(ANCHORS),
                EpsilonBits='3727C5AC',HeightDirtyPredicates=len(DIRTY_PREDICATES),OriginalExecution=False,EffectiveArithmeticControls='Unknown',OriginalNormalParity=False)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--exe',type=Path,required=True);parser.add_argument('--packages',type=Path,required=True)
    args=parser.parse_args();raw=args.exe.read_bytes();gate(raw)
    wrong=0
    for candidate in (b'',raw[:-1],bytes([raw[0]^1])+raw[1:]):
        try:gate(candidate)
        except ValueError:wrong+=1
        else:raise AssertionError('Wrong executable identity accepted')
    sys.path.insert(0,str(args.packages));import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=raw,fast_load=True);image=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase
    for start,end in BODIES:
        section=next(s for s in pe.sections if s.VirtualAddress<=start-base<s.VirtualAddress+s.SizeOfRawData)
        offset=section.PointerToRawData+start-base-section.VirtualAddress
        assert raw[offset:offset+end-start]==image[start-base:end-base]
    decoder=Cs(CS_ARCH_X86,CS_MODE_32);result=check(image,base,decoder);negative=0
    for va in (0xB65739,0xAB5C58,0xB78A99,*DIRTY_PREDICATES.values(),*(table+0x14 for table in DIRTY_PREDICATES)):
        mutated=bytearray(image);mutated[va-base]^=1
        try:check(mutated,base,decoder)
        except ValueError:negative+=1
        else:raise AssertionError('Synthetic mapped-image mutation accepted')
    result.update(WrongIdentityRejections=wrong,SyntheticBodyMutationRejections=negative)
    print(json.dumps(result))


if __name__=='__main__':main()
