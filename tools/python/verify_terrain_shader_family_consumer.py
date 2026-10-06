"""Static-only first-SFAM-string ownership and zero-result consumer witness.

This is an evidence verifier, not a shader resolver or an implementation of the
unexpanded comparison callee. Original bytes and detailed witnesses are written
only to a fresh ignored local-private output directory. No executable is run.
"""
from __future__ import annotations

import argparse
import contextlib
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import struct
import sys

REPO = Path(__file__).resolve().parents[2]
EXE_SHA = "e782bf1c49b3da730f66b54bc558947c46493f7133f672eed1d8e0c56a6b6150"
GROUP_SHA = "c0f9043140db936e3f3f2dd3eec795f1bee5424e6e904081c667cfc9e99a0628"
BODIES = {
    "Setter": (0x00B69310, 90, "1be622270690113c4b456f31d0125fda606bb974deb7b13830c0dfdf92c65088"),
    "Getter": (0x00B69300, 4, "f7e0208f5e988ae39c7a45dfddbd12921d02f9c3deb5695d4c534c8ab0ccdb0f"),
    "Consumer": (0x00B6B360, 71, "8253dabf507f11e0fb35d335c0a3043aa6dbf4fa7630bc221de15b8ef8fd278b"),
}
# Distinct followed function targets; candidate callsites are not body expansions.
# The allocator, releaser and comparison target are terminal, unexpanded leads.
LEADS = (0x00B69310, 0x00B69300, 0x00B6B360, 0x00AC15E0, 0x00AC1660, 0x01320590)
ANCHORS = {
    0x00B69314: "8bd9",
    0x00B69316: "8b4304",
    0x00B69319: "85c0",
    0x00B6931C: "7410",
    0x00B6931F: "e83c83f5ff",
    0x00B69327: "c7430400000000",
    0x00B6932E: "8b7508",
    0x00B69331: "85f6",
    0x00B69333: "742f",
    0x00B6933D: "f2ae",
    0x00B69341: "49",
    0x00B69344: "47",
    0x00B69345: "57",
    0x00B69346: "e89582f5ff",
    0x00B69354: "f3a5",
    0x00B6935E: "f3a4",
    0x00B69360: "894304",
    0x00B69367: "c20400",
    0x00B69300: "8b4104",
    0x00B69303: "c3",
    0x00B6B366: "8bf9",
    0x00B6B368: "8b4708",
    0x00B6B36F: "7e24",
    0x00B6B371: "8b5d08",
    0x00B6B374: "8b4704",
    0x00B6B377: "8b0cb0",
    0x00B6B37A: "e881dfffff",
    0x00B6B37F: "50",
    0x00B6B380: "53",
    0x00B6B381: "e80a527b00",
    0x00B6B386: "83c408",
    0x00B6B389: "85c0",
    0x00B6B38B: "7411",
    0x00B6B390: "46",
    0x00B6B393: "7cdf",
    0x00B6B397: "32c0",
    0x00B6B39B: "c20400",
    0x00B6B3A0: "b001",
    0x00B6B3A4: "c20400",
    0x00B6AB9C: "e80fa6f2ff",
    0x00B6ABA1: "8d85d0fbffff",
    0x00B6ABA7: "50",
    0x00B6ABA8: "8bce",
    0x00B6ABAA: "e861e7ffff",
    0x00B6ABCB: "e8c0e7ffff",
    0x00B6AB15: "8bf9",
    0x00B6AD10: "8b7dd0",
    0x00B6AD13: "8b75e8",
    0x00B6AD2D: "8b4f08",
    0x00B6AD30: "8b5704",
    0x00B6AD33: "89348a",
    0x00B6AD3E: "895708",
}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def gate(data):
    if digest(data) != EXE_SHA:
        raise ValueError("Executable identity mismatch; no VA evidence inspected")


def lead_budget(leads, maximum):
    if type(maximum) is not int or not 0 <= maximum <= 12:
        raise ValueError("Lead cap must be an integer in 0..12")
    if len(set(leads)) != len(leads) or len(leads) > maximum:
        raise ValueError("Duplicate lead or exceeded fresh distinct-function budget")
    if tuple(leads) != LEADS:
        raise ValueError("Unassigned function lead")


def output_path(value):
    """Check lexical ancestors before resolving; never permit a linked output."""
    lexical = Path(value)
    if any(part == ".." or part.casefold() == ".git" or ":" in part.strip("\\/")
           for part in lexical.parts[1:]):
        raise ValueError("Output contains a forbidden lexical component")
    path = Path(os.path.abspath(value))
    private = REPO / "local-private"
    for candidate in [path, *path.parents]:
        try:
            metadata = candidate.lstat()
        except FileNotFoundError:
            continue
        if candidate.is_symlink() or getattr(metadata, "st_file_attributes", 0) & 0x400:
            raise ValueError("Output has a linked/reparse ancestor")
        if candidate != path and not candidate.is_dir():
            raise ValueError("Output ancestor is not a directory")
    if not path.is_relative_to(private) or path == private or path.exists():
        raise ValueError("Output must be fresh and below this checkout's local-private")
    if not path.resolve().is_relative_to(private.resolve()):
        raise ValueError("Output escapes private root")
    # Filesystem policy plus actual Git ignore policy, before any output writes.
    import subprocess
    probe = subprocess.run(["git", "-c", f"safe.directory={REPO.as_posix()}",
                            "-C", str(REPO), "check-ignore", "-q", str(path / "receipt.json")])
    if probe.returncode != 0:
        raise ValueError("Output is not Git-ignored")
    return path


def check_anchors(read):
    for va, encoded in ANCHORS.items():
        expected = bytes.fromhex(encoded)
        if read(va, len(expected)) != expected:
            raise ValueError(f"Instruction witness mismatch at {va:08X}")


def check_bodies(read, decoder):
    counts = {}
    for name, (va, size, expected) in BODIES.items():
        body = read(va, size)
        if len(body) != size or digest(body) != expected:
            raise ValueError(f"Complete bounded body mismatch: {name}")
        instructions = list(decoder.disasm(body, va))
        if sum(item.size for item in instructions) != size or instructions[-1].mnemonic != "ret":
            raise ValueError("Body does not end at the verified return")
        counts[name] = len(instructions)
    check_anchors(read)
    return counts


def owned_string_witness(source):
    """Synthetic byte-copy model of the observed setter, not original execution.

    The source must be a bounded witness with a terminator. Original length scan
    is unbounded. Null means clear; a non-null empty string still copies one NUL.
    """
    if source is None:
        return None
    stop = source.index(0)
    return bytearray(source[:stop + 1])


def consumer_witness(families, query, results, count=None):
    """Inject callee results; deliberately implement no comparison semantics."""
    count = len(families) if count is None else count
    if count > len(families):
        raise ValueError("Synthetic witness extent is insufficient")
    visited = []
    if count <= 0:
        return False, visited
    for index in range(count):
        # Argument one is the opaque caller value; argument two is the owned+4 pointer.
        visited.append((index, query, families[index]))
        result = results[index]
        if not -(1 << 31) <= result < (1 << 32):
            raise ValueError("Synthetic result is not a raw 32-bit value")
        if result == 0:
            return True, visited
    return False, visited


def synthetic_witnesses():
    checks = 0
    for source, expected in [(bytearray(b"a\x80\xff\0ignored"), b"a\x80\xff\0"),
                             (bytearray(b"\0"), b"\0"), (bytearray(b"x" * 999 + b"\0"), b"x" * 999 + b"\0")]:
        owned = owned_string_witness(source)
        assert bytes(owned) == expected
        source[0] ^= 1
        assert bytes(owned) == expected
        owned[0] ^= 1
        assert owned is not source
        checks += 3
    assert owned_string_witness(None) is None
    checks += 1
    try:
        owned_string_witness(bytearray(b"unterminated"))
    except ValueError:
        checks += 1
    else:
        raise AssertionError("Unterminated synthetic witness accepted")
    query = object()
    families = [object(), object(), object()]
    # Equal/unequal bytes are never used to invent the unresolved callee result.
    for results, wanted, indices in [([0, 7, 7], True, [0]), ([7, 0, 7], True, [0, 1]),
                                    ([7, -1, 0], True, [0, 1, 2]),
                                    ([7, -1, 0xFFFFFFFF], False, [0, 1, 2])]:
        accepted, visits = consumer_witness(families, query, results)
        assert accepted == wanted and [x[0] for x in visits] == indices
        assert all(x[1] is query and x[2] is families[x[0]] for x in visits)
        checks += 2
    for count in (0, -1, -(1 << 31)):
        assert consumer_witness(families, query, [], count) == (False, [])
        checks += 1
    accepted, visits = consumer_witness(families, query, [7], 1)
    assert not accepted and len(visits) == 1
    checks += 1
    return checks


def original_witness(asset_root, config):
    spec = importlib.util.spec_from_file_location("shader_consumer_tgen33", REPO / "poc/round33-tests/Assets/verify_tgen_input.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    if module.sha(config.read_bytes()) != module.CONFIG:
        raise ValueError("Pinned configuration identity mismatch")
    original = None
    with contextlib.ExitStack() as stack:
        for priority, archive in module.archives(asset_root, config):
            original = module.Index(stack.enter_context(archive.open("rb"))).lookup("terrain/tatooine.trn")
            if original is not None:
                break
    if priority != 42 or original is None or len(original) != 1188446 or module.sha(original) != module.SOURCE:
        raise ValueError("Pinned original terrain identity mismatch")
    generator = original[210:210 + 139608]
    if module.sha(generator) != module.GENERATOR:
        raise ValueError("Pinned generator identity mismatch")
    blob = generator[24:24 + 1977]
    if digest(blob) != GROUP_SHA:
        raise ValueError("Pinned shader group identity mismatch")
    root, = module.records(blob, 0, len(blob))
    version, = module.records(blob, 12, len(blob))
    if root[0] != b"FORM" or root[1][:4] != b"SGRP" or version[0] != b"FORM" or version[1][:4] != b"0006":
        raise ValueError("Pinned group/version mismatch")
    witnesses = []
    entries = 0
    for index, (tag, payload, start, end) in enumerate(module.records(blob, 24, len(blob))):
        if tag != b"SFAM":
            raise ValueError("Pinned family tag mismatch")
        first_end = payload.index(0, 4)
        second_end = payload.index(0, first_end + 1)
        count, = struct.unpack_from("<I", payload, second_end + 12)
        position = second_end + 16
        if count > 1024:
            raise ValueError("Pinned entry witness cap")
        for _ in range(count):
            position = payload.index(0, position) + 5
        if position != len(payload):
            raise ValueError("Pinned family consumption mismatch")
        first = payload[4:first_end + 1]
        owned = owned_string_witness(bytearray(first))
        if bytes(owned) != first:
            raise ValueError("First-string byte witness mismatch")
        witnesses.append(dict(Index=index,FamilyOffset=start,FamilyEncodedBytes=end-start,
                              FirstStringOffset=start+12,FirstStringBytes=len(first)-1,
                              SourceWithNulSha256=digest(first),OwnedWithNulSha256=digest(owned),
                              FirstStringHex=first.hex(),Setter="00B69310",FieldOffset=4,
                              Getter="00B69300",Consumer="00B6B360",UnresolvedCallee="01320590"))
        entries += count
    if len(witnesses) != 15 or entries != 35:
        raise ValueError("Pinned group count mismatch")
    # Structural byte identity only. No original query/callee result is invented.
    return witnesses, entries


def verify(executable, packages, asset_root, config, destination, maximum_leads=12):
    lead_budget(LEADS, maximum_leads)
    output = output_path(destination)
    data = executable.read_bytes()
    gate(data)  # before imports/parsing/mapping/address reads
    sys.path[:0] = [str(packages), str(REPO / "tools/python")]
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=data, fast_load=True)
    if pe.OPTIONAL_HEADER.ImageBase != 0x400000 or pe.FILE_HEADER.Machine != 0x14C:
        raise ValueError("Unexpected PE32 ABI")
    def read(va, size):
        offset = pe.get_offset_from_rva(va - 0x400000)
        return data[offset:offset + size]
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    counts = check_bodies(read, decoder)
    witnesses, entries = original_witness(asset_root, config)
    synthetic = synthetic_witnesses()
    receipt = dict(Passed=True,ExecutableSha256=EXE_SHA.upper(),Version="0006",
                   BodyInstructions=counts,CompleteBodyInstructions=sum(counts.values()),
                   InstructionAnchors=len(ANCHORS),FreshDistinctFunctionLeads=len(LEADS),LeadCap=maximum_leads,
                   OriginalOccurrences=1,OriginalFamilies=len(witnesses),OriginalEntries=entries,
                   OriginalGroupSha256=GROUP_SHA.upper(),SyntheticWitnessChecks=synthetic,
                   FirstStringFieldOffset=4,Setter="00B69310",Getter="00B69300",Consumer="00B6B360",
                   StopAtUnexpandedCallee="01320590",ResultGate="zero returns AL=1; otherwise continue",
                   OriginalConsumerExecution=False,ComparisonSemanticsProved=False,
                   AssetNameSemanticsProved=False,ProductionResolver=False,OriginalExecution=False,
                   Bodies={name:dict(Va=f"{va:08X}",Bytes=size,Sha256=sha)
                           for name,(va,size,sha) in BODIES.items()})
    # All evidence and input/output/lead checks finish before publication.
    output_path(output)
    output.mkdir(parents=True)
    (output / "raw-byte-witnesses.json").write_text(json.dumps(witnesses,indent=2)+"\n",encoding="utf-8")
    (output / "receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe",type=Path,required=True)
    parser.add_argument("--packages",type=Path,required=True)
    parser.add_argument("--asset-root",type=Path,required=True)
    parser.add_argument("--config",type=Path,required=True)
    parser.add_argument("--output",type=Path,required=True)
    parser.add_argument("--maximum-leads",type=int,default=12)
    args = parser.parse_args()
    print(json.dumps(verify(args.exe,args.packages,args.asset_root,args.config,args.output,args.maximum_leads),indent=2))
