"""Portable deterministic guards for the second-string bodies; no CPU run."""
from pathlib import Path
import argparse
import importlib.util
import json
import struct
import sys
from unittest.mock import patch

REPO = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("second_string64",REPO/"tools/python/verify_terrain_shader_family_second_string.py")
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


def reject(action):
    try:
        action()
    except ValueError:
        return 1
    raise AssertionError("Expected guard rejection")


def run(exe,packages,asset_root,config,run_root):
    prerequisite,consumer = m.dependencies()
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
    for relative in m.SEALED:
        changed = bytearray((REPO/relative).read_bytes())
        changed[-1] ^= 1
        guards["SealedInputs"] += reject(lambda:m.sealed_gate(relative,changed))
    guards["SealedInputs"] += reject(lambda:m.sealed_gate("unassigned-input",b"synthetic"))
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
    for va,size in ((0,1),(0x00B69390,0),(0xFFFFFFFF,4),(0x00B69390,0x10000000)):
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
    for target in (0x00B69390,0x00B69370,0x00B6B480):
        def altered(va,size):
            raw = bytearray(image.read(va,size))
            if va <= target < va+size:
                raw[target-va] ^= 1
            return raw
        guards["CompleteBodies"] += reject(lambda:m.check_bodies(altered,decoder))
    for size in (0,1,90-1):
        def truncated(va,length):
            raw = image.read(va,length)
            return raw[:size] if va == 0x00B69390 else raw
        guards["CompleteBodies"] += reject(lambda:m.check_bodies(truncated,decoder))
    guards["ControlEdges"] = 0
    for target in (*m.CALLS,*m.BRANCHES):
        # Removing an exact call/branch makes the independently decoded edge set
        # fail; ordinary instructions are left untouched.
        guards["ControlEdges"] += reject(lambda:m.check_control([i for i in instructions if i.address != target]))
    guards["LeadAndSiteBudget"] = 0
    for cap in (0,5,-1,13,True,1.5):
        guards["LeadAndSiteBudget"] += reject(lambda:m.budget(maximum=cap))
    guards["LeadAndSiteBudget"] += reject(lambda:m.budget(leads=m.LEADS+(m.LEADS[0],)))
    guards["LeadAndSiteBudget"] += reject(lambda:m.budget(sites=m.SITES+(m.SITES[0],)))
    guards["LeadAndSiteBudget"] += reject(lambda:m.budget(leads=(*m.LEADS[:-1],0x12345678)))
    guards["LeadAndSiteBudget"] += reject(lambda:m.budget(sites=(*m.SITES[:-1],0x12345678)))
    m.budget(maximum=6)
    # Fresh portable harness fixtures, independent of worker private layout.
    root.mkdir(parents=True)
    sentinel = root/"sentinel-file.bin"
    sentinel.write_bytes(b"synthetic ancestor file")
    guards["OutputPaths"] = 0
    for path in (REPO,REPO/"docs/forbidden64",REPO/"local-private",root,
                 REPO/"local-private/../docs/escape64",REPO/"local-private/.git/forbidden64",sentinel/"child"):
        guards["OutputPaths"] += reject(lambda:consumer.output_path(path))
    actual_lstat = Path.lstat
    class Reparse:
        st_mode = 0o040755
        st_file_attributes = 0x400
    def linked(path,*args,**kwargs):
        return Reparse() if path == root else actual_lstat(path,*args,**kwargs)
    with patch.object(Path,"lstat",linked):
        guards["OutputPaths"] += reject(lambda:consumer.output_path(root/"must-not-publish-linked"))
    # Independent byte preservation and returned-object identity witnesses.
    pointer_checks = 0
    inputs = [bytearray(b"MiX/\\\\\xff\0tail"),bytearray(b"\0"),bytearray(b"x"*999+b"\0")]
    for source in inputs:
        expected = bytes(source).split(b"\0",1)[0]+b"\0"
        owned = m.copy_witness(source)
        assert bytes(owned)==expected and owned is not source
        source[0]^=1
        assert bytes(owned)==expected
        pointer_checks+=2
    selected=[bytearray(b"duplicate\0"),None,bytearray(b"duplicate\0")]
    opaque=object()
    for index in range(3):
        value,call=m.pointer_witness(selected,opaque,'supplied synthetic selector',index)
        assert value is selected[index] and call['InputCall'][0] is selected and call['InputCall'][1] is opaque
        pointer_checks+=2
    guards["FiniteSyntheticWitnesses"]=reject(lambda:m.copy_witness(bytearray(b"unterminated")))
    for index in (-1,3,True,1.5):
        guards["FiniteSyntheticWitnesses"]+=reject(lambda:m.pointer_witness(selected,opaque,'synthetic',index))
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
    guards["IntegrationNoPublication"] += reject(lambda:m.verify(exe,packages,asset_root,config,unpublished,5))
    assert not unpublished.exists()
    result = m.verify(exe,packages,asset_root,config,root/"positive")
    sealed = {path.name:m.sha(path.read_bytes()) for path in (root/"positive").iterdir()}
    guards["IntegrationNoPublication"] += reject(lambda:m.verify(exe,packages,asset_root,config,root/"positive"))
    assert sealed == {path.name:m.sha(path.read_bytes()) for path in (root/"positive").iterdir()}
    receipt = dict(Passed=True,Static=result,NegativeGuards=guards,TotalNegativeGuards=sum(guards.values()),
                   IndependentCopyPointerChecks=pointer_checks,OriginalExecution=False)
    (root/"comparison-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(receipt,indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ("exe","packages","asset-root","config","run-root"):
        parser.add_argument("--"+key,type=Path,required=True)
    args = parser.parse_args()
    run(args.exe,args.packages,args.asset_root,args.config,args.run_root)
