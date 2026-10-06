"""Portable deterministic guards for the entry-accessor bodies; no CPU run."""
from pathlib import Path
import argparse
import importlib.util
import json
import struct
import sys
from unittest.mock import patch

REPO = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("selector69",REPO/"tools/python/verify_terrain_shader_family_selector.py")
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


def reject(action):
    try:
        action()
    except ValueError:
        return 1
    raise AssertionError("Expected guard rejection")


def run(exe,packages,asset_root,config,run_root):
    prior,records,prerequisite,consumer = m.dependency()
    root = consumer.output_path(run_root)
    data = exe.read_bytes()
    m.gate(data)
    image = prerequisite.RawPe(data)
    sys.path.insert(0,str(packages))
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    decoder = Cs(CS_ARCH_X86,CS_MODE_32)
    decoder.detail = True
    instructions = m.check_body(image.read,decoder)
    guards = {}
    guards["Identity"] = 0
    for at in (0,1,len(data)//2,len(data)-1):
        changed = bytearray(data)
        changed[at] ^= 1
        guards["Identity"] += reject(lambda:m.gate(changed))
    for changed in (b"",data[:-1],data+b"\0"):
        guards["Identity"] += reject(lambda:m.gate(changed))
    guards["SealedInputs"] = 0
    source=(REPO/m.PREREQUISITE).read_bytes()
    changed=bytearray(source);changed[-1]^=1
    for value in (changed,b"",source+b"\0"):
        guards["SealedInputs"]+=reject(lambda:m.sealed_gate(value))
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
    for va,size in ((0,1),(m.BODY_VA,0),(0xFFFFFFFF,4),(m.BODY_VA,0x10000000)):
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
    guards["CompleteBodyAndExtent"] = 0
    for target in (m.BODY_VA,m.RETURNS[0],m.RETURNS[-1],m.BEFORE_VA,m.AFTER_VA):
        def altered(va,size):
            raw=bytearray(image.read(va,size))
            if va<=target<va+size:raw[target-va]^=1
            return raw
        guards["CompleteBodyAndExtent"]+=reject(lambda:m.check_body(altered,decoder))
    for size in (0,1,0x32,m.BODY_BYTES-1):
        def truncated(va,count):
            raw=image.read(va,count)
            return raw[:size] if va==m.BODY_VA else raw
        guards["CompleteBodyAndExtent"]+=reject(lambda:m.check_body(truncated,decoder))
    guards["ApprovedGetterAndFences"]=0
    for target in (m.HELPER_VA,m.HELPER_VA+2,m.HELPER_BEFORE_VA,m.HELPER_AFTER_VA):
        def altered(va,size):
            raw=bytearray(image.read(va,size))
            if va<=target<va+size:raw[target-va]^=1
            return raw
        guards["ApprovedGetterAndFences"]+=reject(lambda:m.check_helper(altered,decoder))
    for size in (0,1,2):
        def truncated(va,count):
            raw=image.read(va,count)
            return raw[:size] if va==m.HELPER_VA else raw
        guards["ApprovedGetterAndFences"]+=reject(lambda:m.check_helper(truncated,decoder))
    guards["ControlEdges"]=0
    for target in (*m.CALLS,*m.BRANCHES,*m.RETURNS):
        guards["ControlEdges"]+=reject(lambda:m.check_control([i for i in instructions if i.address!=target]))
    from types import SimpleNamespace
    # A disconnected ordinary instruction after the return must fail reachability.
    guards["ControlEdges"]+=reject(lambda:m.check_control(instructions+[SimpleNamespace(address=m.BODY_VA+m.BODY_BYTES,mnemonic='nop',size=1)]))
    guards["LeadAndSiteBudget"]=0
    for keyword in ('maximum_leads','maximum_sites'):
        for cap in (0,1,-1,13,True,1.5) if keyword=='maximum_leads' else (0,10,-1,13,True,1.5):
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
    for path in (REPO,REPO/"docs/forbidden69",REPO/"local-private",root,
                 REPO/"local-private/../docs/escape69",REPO/"local-private/.git/forbidden69",sentinel/"child"):
        guards["OutputPaths"] += reject(lambda:consumer.output_path(path))
    actual_lstat = Path.lstat
    class Reparse:
        st_mode = 0o040755
        st_file_attributes = 0x400
    def linked(path,*args,**kwargs):
        return Reparse() if path == root else actual_lstat(path,*args,**kwargs)
    with patch.object(Path,"lstat",linked):
        guards["OutputPaths"] += reject(lambda:consumer.output_path(root/"must-not-publish-linked"))
    # Independent full32-bit equality and captured signed-count model.
    pointer_checks=0;pending=(object(),object());registers=object()
    vectors=[([0,1,0xffffffff],0,0,1),([0,1,0xffffffff],1,1,2),([0,1,0xffffffff],-1,2,3),
             ([0x80000000,0x7fc12345],0x7fc12345,1,2),([9,9,9],9,0,1),([1,2],3,0xffffffff,2)]
    for values,key,wanted,reads in vectors:
        got=m.selector_witness(values,key,nonvolatile=registers,pending=pending)
        assert got['Result']==wanted and len(got['GetterReads'])==reads
        assert got['Nonvolatile'] is registers and got['Pending'] is pending and got['StackCleanupBytes']==4
        pointer_checks+=2
    for count in (0,-1,0x80000000,0xffffffff):
        got=m.selector_witness([],None,count)
        assert got['Result']==0xffffffff and not got['InputRead'] and not got['GetterReads']
        pointer_checks+=1
    for base,index,wanted in ((0x8000,0,0x8000),(0x8000,3,0x800c),(0x8000,0xffffffff,0x7ffc),(0,0xffffffff,0xfffffffc),(0xfffffffc,1,0)):
        assert m.caller_array_address(base,index)==wanted
        pointer_checks+=1
    # Captured positive count cannot be extended by extra supplied reads.
    got=m.selector_witness([1,9],9,count=1)
    assert got['Result']==0xffffffff and got['GetterReads']==[1]
    pointer_checks+=1
    guards["SyntheticSafeWitnessPolicy"]=0
    for action in (lambda:m.selector_witness([],1,count=1),lambda:m.selector_witness([None],1),
                   lambda:m.selector_witness([1],None),lambda:m.signed_count(True),lambda:m.signed_count(1.5)):
        guards["SyntheticSafeWitnessPolicy"]+=reject(action)
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
    guards["IntegrationNoPublication"] += reject(lambda:m.verify(exe,packages,asset_root,config,unpublished,1))
    assert not unpublished.exists()
    result = m.verify(exe,packages,asset_root,config,root/"positive")
    sealed = {path.name:m.sha(path.read_bytes()) for path in (root/"positive").iterdir()}
    guards["IntegrationNoPublication"] += reject(lambda:m.verify(exe,packages,asset_root,config,root/"positive"))
    assert sealed == {path.name:m.sha(path.read_bytes()) for path in (root/"positive").iterdir()}
    receipt = dict(Passed=True,Static=result,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),
                   IndependentSelectorChecks=pointer_checks,OriginalExecution=False)
    (root/"comparison-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(receipt,indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ("exe","packages","asset-root","config","run-root"):
        parser.add_argument("--"+key,type=Path,required=True)
    args = parser.parse_args()
    run(args.exe,args.packages,args.asset_root,args.config,args.run_root)
