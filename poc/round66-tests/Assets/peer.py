"""Portable deterministic guards for the entry-accessor bodies; no CPU run."""
from pathlib import Path
import argparse
import importlib.util
import json
import struct
import sys
from unittest.mock import patch

REPO = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("entry66",REPO/"tools/python/verify_terrain_shader_family_entry_accessor.py")
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


def reject(action):
    try:
        action()
    except ValueError:
        return 1
    raise AssertionError("Expected guard rejection")


def run(exe,packages,asset_root,config,run_root):
    prerequisite,consumer = m.dependency()
    root = consumer.output_path(run_root)
    data = exe.read_bytes()
    m.gate(data)
    image = prerequisite.RawPe(data)
    sys.path.insert(0,str(packages))
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    decoder = Cs(CS_ARCH_X86,CS_MODE_32)
    decoder.detail = True
    instructions = m.check_bodies(image.read,decoder)
    guards = {}
    guards["Identity"] = 0
    for at in (0,1,len(data)//2,len(data)-1):
        changed = bytearray(data)
        changed[at] ^= 1
        guards["Identity"] += reject(lambda:m.gate(changed))
    for changed in (b"",data[:-1],data+b"\0"):
        guards["Identity"] += reject(lambda:m.gate(changed))
    guards["SealedInputs"] = 0
    for name in m.SEALED:
        source=(REPO/name).read_bytes()
        changed=bytearray(source);changed[-1]^=1
        for value in (changed,b"",source+b"\0"):
            guards["SealedInputs"]+=reject(lambda:m.sealed_gate(name,value))
    guards["PeTruncation"] = 0
    for changed in (b"",b"MZ",data[:63],data[:64],data[:128],data[:-1]):
        guards["PeTruncation"] += reject(lambda:prerequisite.RawPe(changed))
    guards["PeHeaders"] = 0
    header, = struct.unpack_from("<I",data,60)
    for at,length in ((60,4),(header,4),(header+4,2),(header+6,2),(header+24,2),(header+24+28,4)):
        changed = bytearray(data)
        changed[at:at+length] = bytes(length)
        guards["PeHeaders"] += reject(lambda:prerequisite.RawPe(changed))
    guards["MappedExtent"] = 0
    for va,size in ((0,1),(0x00B692A0,0),(0xFFFFFFFF,4),(0x00B692A0,0x10000000)):
        guards["MappedExtent"] += reject(lambda:image.read(va,size))
    for va in (0x019AA110,0x019BCFC4,0x019BCFC8):
        guards["MappedExtent"] += reject(lambda:image.read(va,4))
    guards["RawAndOperandAnchors"] = 0
    for target in m.ANCHORS:
        def altered(va,size):
            raw = bytearray(image.read(va,size))
            if va <= target < va+size:
                raw[target-va] ^= 1
            return raw
        guards["RawAndOperandAnchors"] += reject(lambda:m.check_anchors(altered,decoder))
    # Alter a decoder interpretation without changing raw bytes: operand gate
    # must fail independently of the raw opcode comparison.
    class WrongOperands:
        def disasm(self,raw,va):
            from types import SimpleNamespace
            actual = next(decoder.disasm(raw,va))
            return iter([SimpleNamespace(mnemonic=actual.mnemonic,op_str="synthetic wrong operands")])
    guards["RawAndOperandAnchors"] += reject(lambda:m.check_anchors(image.read,WrongOperands()))
    guards["CompleteBodies"] = 0
    for name,(start,length,_,_) in m.BODIES.items():
        for target in (start,start+length-1):
            def altered(va,size):
                raw=bytearray(image.read(va,size))
                if va<=target<va+size:raw[target-va]^=1
                return raw
            guards["CompleteBodies"]+=reject(lambda:m.check_bodies(altered,decoder))
        for size in (0,1,length-1):
            def truncated(va,count):
                raw=image.read(va,count)
                return raw[:size] if va==start else raw
            guards["CompleteBodies"]+=reject(lambda:m.check_bodies(truncated,decoder))
    guards["ControlEdges"]=0
    for target in m.CALLS:
        guards["ControlEdges"]+=reject(lambda:m.check_control([i for i in instructions if i.address!=target]))
    from types import SimpleNamespace
    guards["ControlEdges"]+=reject(lambda:m.check_control(instructions+[SimpleNamespace(mnemonic='jmp')]))
    guards["RawCallCandidates"]=0
    for site in m.RAW_CANDIDATES:
        changed=bytearray(data);changed[image.location(site,5)['FileOffset']]=0x90
        guards["RawCallCandidates"]+=reject(lambda:m.raw_candidates(prerequisite.RawPe(changed)))
    # An unrelated additional candidate must not silently widen the inventory.
    section=next(s for s in image.sections if s[5]&0x20000000)
    va=image.base+section[1]+0x100
    changed=bytearray(data)
    at=section[3]+0x100
    changed[at:at+5]=b"\xe8"+((0x00B692A0-va-5)&0xffffffff).to_bytes(4,'little')
    guards["RawCallCandidates"]+=reject(lambda:m.raw_candidates(prerequisite.RawPe(changed)))
    guards["LeadAndSiteBudget"]=0
    for keyword in ('maximum_leads','maximum_sites'):
        for cap in (0,3,-1,13,True,1.5):
            guards["LeadAndSiteBudget"]+=reject(lambda:m.budget(**{keyword:cap}))
    for key,values in (('leads',m.LEADS),('sites',m.SITES)):
        for changed in (values+(values[0],),values[:-1],(*values[:-1],0x12345678)):
            guards["LeadAndSiteBudget"]+=reject(lambda:m.budget(**{key:changed}))
    m.budget()
    # Fresh portable harness fixtures, independent of worker private layout.
    root.mkdir(parents=True)
    sentinel = root/"sentinel-file.bin"
    sentinel.write_bytes(b"synthetic ancestor file")
    guards["OutputPaths"] = 0
    for path in (REPO,REPO/"docs/forbidden66",REPO/"local-private",root,
                 REPO/"local-private/../docs/escape66",REPO/"local-private/.git/forbidden66",sentinel/"child"):
        guards["OutputPaths"] += reject(lambda:consumer.output_path(path))
    actual_lstat = Path.lstat
    class Reparse:
        st_mode = 0o040755
        st_file_attributes = 0x400
    def linked(path,*args,**kwargs):
        return Reparse() if path == root else actual_lstat(path,*args,**kwargs)
    with patch.object(Path,"lstat",linked):
        guards["OutputPaths"] += reject(lambda:consumer.output_path(root/"must-not-publish-linked"))
    # Independently supplied raw words and pointer identity, wrapper return only.
    pointer_checks=0
    for receiver in (None,object()):
        local=object()
        init=m.initializer_witness(local,receiver)
        assert init['Eax'] is local and init['Ecx']==0
        assert init['Edx'] is receiver and init['LocalWords']==(0,0,0)
        pointer_checks+=2
    for family_word,entry_word in ((0,0xffffffff),(0x80000000,0x7fc12345),(0xffffffff,0x80000000)):
        for pointer in (None,bytearray(b"\0"),bytearray(b"MiX/\\\xff\0tail")):
            destination=object()
            got=m.record_witness(family_word,pointer,entry_word,destination)
            assert got['Destination'] is destination and got['Return'] is destination
            assert got['Words'][0]==family_word and got['Words'][1] is pointer and got['Words'][2]==entry_word
            pointer_checks+=2
    for base,index,stride,wanted in ((0x8000,2,12,0x8018),(0,-1,12,0xfffffff4),(0xfffffffc,1,4,0),(0,0x80000000,12,0)):
        assert m.index_address(base,index,stride)==wanted
        pointer_checks+=1
    pointer=bytearray(b"source\0")
    result=m.record_witness(7,pointer,8,object())
    pointer[:]=b"change\0"
    assert result['Words'][1] is pointer and bytes(result['Words'][1])==b"change\0"
    pointer_checks+=1
    guards["IntegrationNoPublication"] = 0
    wrong = root/"wrong-exe.bin"
    wrong.write_bytes(b"MZ synthetic wrong identity")
    wrong_config = root/"wrong-config.cfg"
    wrong_config.write_text("synthetic wrong configuration\n",encoding="utf-8")
    unpublished = root/"must-not-publish"
    with patch.object(prerequisite,"RawPe",side_effect=AssertionError("PE parsing reached before SHA gate")):
        guards["IntegrationNoPublication"] += reject(lambda:m.verify(wrong,packages,asset_root,config,unpublished))
    assert not unpublished.exists()
    guards["IntegrationNoPublication"] += reject(lambda:m.verify(exe,packages,asset_root,wrong_config,unpublished))
    assert not unpublished.exists()
    guards["IntegrationNoPublication"] += reject(lambda:m.verify(exe,packages,asset_root,config,unpublished,3))
    assert not unpublished.exists()
    result = m.verify(exe,packages,asset_root,config,root/"positive")
    sealed = {path.name:m.sha(path.read_bytes()) for path in (root/"positive").iterdir()}
    guards["IntegrationNoPublication"] += reject(lambda:m.verify(exe,packages,asset_root,config,root/"positive"))
    assert sealed == {path.name:m.sha(path.read_bytes()) for path in (root/"positive").iterdir()}
    receipt = dict(Passed=True,Static=result,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),
                   IndependentRecordChecks=pointer_checks,OriginalExecution=False)
    (root/"comparison-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(receipt,indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ("exe","packages","asset-root","config","run-root"):
        parser.add_argument("--"+key,type=Path,required=True)
    args = parser.parse_args()
    run(args.exe,args.packages,args.asset_root,args.config,args.run_root)
