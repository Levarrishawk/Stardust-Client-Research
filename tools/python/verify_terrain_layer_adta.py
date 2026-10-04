"""Hash-first LAYR ADTA-to-runtime mapping and one pinned raw occurrence."""
import argparse, contextlib, hashlib, importlib.util, json, struct, sys
from pathlib import Path

EXPECTED='E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
ANCHORS={
    0xB635B6:('mov','byte ptr [edx + 0x50], al'),
    0xB635B9:('mov','byte ptr [edx + 0x51], al'),
    0xB635BC:('mov','byte ptr [edx + 0x52], al'),
    0xB635CE:('mov','byte ptr [edx + 0x68], al'),
    0xB635E8:('mov','dword ptr [edx], 0x15ed1a4'),
    0xB64A6B:('call','0xa94f00'),0xB64A73:('test','ebx, ebx'),
    0xB64A75:('setne','cl'),0xB64A7B:('call','0xb65ff0'),
    0xB64A88:('call','0xa94f00'),0xB64A90:('test','edx, edx'),
    0xB64A92:('setne','al'),0xB64A98:('call','0xb66000'),
    0xB64B1B:('call','0xa94f00'),0xB64B25:('setne','cl'),
    0xB64B2B:('call','0xb65ff0'),0xB64B38:('call','0xa94f00'),
    0xB64B42:('setne','al'),0xB64B48:('call','0xb66000'),
    0xB64B55:('call','0xa94f00'),0xB64B5F:('setne','dl'),
    0xB64B65:('call','0xb66010'),
    0xB64BEB:('call','0xa94f00'),0xB64BF5:('setne','cl'),
    0xB64BFB:('call','0xb65ff0'),0xB64C08:('call','0xa94f00'),
    0xB64C12:('setne','al'),0xB64C18:('call','0xb66000'),
    0xB64C25:('call','0xa94f00'),0xB64C2F:('setne','dl'),
    0xB64C35:('call','0xb66010'),
    0xB64CDB:('call','0xa94f00'),0xB64CE5:('setne','cl'),
    0xB64CEB:('call','0xb65ff0'),0xB64CF8:('call','0xa94f00'),
    0xB64D02:('setne','al'),0xB64D08:('call','0xb66000'),
    0xB64D0F:('lea','ecx, [ebp + 8]'),0xB64D15:('call','0xa94f00'),
    0xB64D1C:('lea','edx, [ebp + 8]'),0xB64D22:('call','0xa94f00'),
    0xB64D27:('mov','eax, dword ptr [ebp + 8]'),
    0xB64D2C:('setne','al'),0xB64D32:('call','0xb66010'),
    0xB65FF6:('mov','byte ptr [ecx + 0x50], al'),
    0xB66006:('mov','byte ptr [ecx + 0x51], al'),
    0xB66016:('mov','byte ptr [ecx + 0x68], al'),
    0xB6402D:('mov','al, byte ptr [esi + 0x50]'),
    0xB6403A:('fsub','dword ptr [ebp - 0x14]'),
    0xB64124:('mov','al, byte ptr [esi + 0x51]'),
    0xB64131:('fsub','dword ptr [ebp - 0x14]'),
    0xB63D51:('mov','al, byte ptr [esi + 0x52]'),
}

def static(exe,packages):
    raw=exe.read_bytes();actual=hashlib.sha256(raw).hexdigest().upper()
    if actual!=EXPECTED:raise ValueError('Executable identity mismatch; rejected before decoding')
    sys.path.insert(0,str(packages))
    import pefile
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    pe=pefile.PE(data=raw,fast_load=True);mapped=pe.get_memory_mapped_image();base=pe.OPTIONAL_HEADER.ImageBase
    md=Cs(CS_ARCH_X86,CS_MODE_32);rows={}
    ranges=[(0xB63560,0xB635FB),(0xB64A30,0xB64ADC),(0xB64AE0,0xB64BA9),
        (0xB64BB0,0xB64C94),(0xB64CA0,0xB64D94),(0xB65FF0,0xB6601B),(0xB63D20,0xB642A0)]
    decoded=[]
    for start,end in ranges:
        ins=list(md.disasm(mapped[start-base:end-base],start));decoded.append(ins)
        rows.update({i.address:(i.mnemonic,i.op_str) for i in ins})
    for va,want in ANCHORS.items():
        if rows.get(va)!=want:raise ValueError(f'{va:08X}: {rows.get(va)} != {want}')
    for ins,count in zip(decoded[1:5],[2,3,3,4]):
        reads=[i for i in ins if i.mnemonic=='call' and i.op_str=='0xa94f00']
        assert len(reads)==count
        for read in reads:
            earlier=[i for i in ins if i.address<read.address][-5:]
            assert any(i.mnemonic=='push' and i.op_str=='4' for i in earlier)
    # The third v4 read is overwritten in the same temporary before its first use.
    middle=[(i.mnemonic,i.op_str) for i in decoded[4] if 0xB64D1A<=i.address<0xB64D27]
    assert middle==[('push','4'),('lea','edx, [ebp + 8]'),('push','edx'),('mov','ecx, esi'),('call','0xa94f00')]
    assert rows[0xB63584]==('xor','eax, eax')
    return dict(ExecutableSha256=actual,InstructionAnchors=len(ANCHORS),LoaderReadCounts=[2,3,3,4],
        RuntimeOffsets=['50','51','68'],Version4DiscardedWordIndex=2,OriginalExecution=False)

def occurrence(root,cfg):
    repo=Path(__file__).resolve().parents[2]
    spec=importlib.util.spec_from_file_location('frozen_tgen_bytes',repo/'poc/round33-tests/Assets/verify_tgen_input.py')
    m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
    assert m.sha(cfg.read_bytes())==m.CONFIG
    with contextlib.ExitStack() as stack:
        for priority,path in m.archives(root,cfg):
            raw=m.Index(stack.enter_context(path.open('rb'))).lookup('terrain/tatooine.trn')
            if raw is not None:break
        else:raise ValueError('Pinned terrain absent')
    assert priority==42 and len(raw)==1188446 and m.sha(raw)==m.SOURCE
    version,=m.records(raw,12,len(raw));_,generator,_=m.records(raw,version[2]+12,version[3])
    data=raw[generator[2]:generator[3]]
    assert generator[2]==210 and len(data)==139608 and m.sha(data)==m.GENERATOR
    groups=list(m.records(data,24,len(data)))
    lyrs=next(row for row in groups if row[0]==b'FORM' and row[1][:4]==b'LYRS')
    first=next(m.records(data,lyrs[2]+12,lyrs[3]));v,=m.records(data,first[2]+12,first[3])
    assert first[1][:4]==b'LAYR' and v[1][:4]==b'0003'
    header,adta,*_=m.records(data,v[2]+12,v[3]);assert header[1][:4]==b'IHDR' and adta[0]==b'ADTA'
    words=struct.unpack_from('<3I',adta[1])
    return dict(SourceSha256=m.SOURCE,GeneratorSha256=m.GENERATOR,Priority=priority,
        LayerOffset=first[2],LayerSha256=m.sha(data[first[2]:first[3]]),Version='0003',
        RawWords=list(words),BoundaryInverted=bool(words[0]),FilterInverted=bool(words[1]),
        Runtime68=bool(words[2]),Runtime68Meaning='Unknown',RawOccurrences=1,GeneratedTerrainCompared=False)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--exe',type=Path,required=True)
    p.add_argument('--packages',type=Path,required=True);p.add_argument('--receipt',type=Path,required=True)
    p.add_argument('--asset-root',type=Path);p.add_argument('--config',type=Path);a=p.parse_args()
    if 'local-private' not in a.receipt.parts:p.error('Private output required')
    if bool(a.asset_root)!=bool(a.config):p.error('Asset root and config must be paired')
    result=static(a.exe,a.packages)
    if a.asset_root:result['OriginalOccurrence']=occurrence(a.asset_root,a.config)
    a.receipt.parent.mkdir(parents=True,exist_ok=True);a.receipt.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k!='OriginalOccurrence'}|({'OriginalRawOccurrences':1} if a.asset_root else {})))
if __name__=='__main__':main()
