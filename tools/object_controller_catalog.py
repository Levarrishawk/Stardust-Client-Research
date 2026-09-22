#!/usr/bin/env python3
"""Inventory Stardust.exe ObjControllerMessage factory registrations.

The client registration helper at 0x00B0CFC0 receives:
    type, outbound_factory, inbound_factory, flag
This tool recovers direct-immediate registration sites and optionally compares
their type IDs with Stardust-3's server callback registrations.
"""

from __future__ import annotations

import argparse
import collections
import pathlib
import re

import pefile
from capstone import CS_ARCH_X86, CS_MODE_32, Cs
from capstone.x86 import X86_OP_IMM


REGISTER_VA = 0x00B0CFC0


def registrations(image: pathlib.Path):
    pe = pefile.PE(str(image), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    text = next(s for s in pe.sections if s.Name.rstrip(b"\0") == b".text")
    va = base + text.VirtualAddress
    data = text.get_data()
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    # The executable's .text section contains alignment/data islands.  Continue
    # linearly across them so later registration modules are still inventoried.
    md.skipdata = True
    rows = []
    history = collections.deque(maxlen=12)
    for insn in md.disasm(data, va):
        if insn.mnemonic != "call" or not insn.operands:
            history.append(insn)
            continue
        op = insn.operands[0]
        if op.type != X86_OP_IMM or op.imm != REGISTER_VA:
            history.append(insn)
            continue

        pushes = []
        for prior in reversed(history):
            if prior.mnemonic == "push" and prior.operands and prior.operands[0].type == X86_OP_IMM:
                pushes.append(prior.operands[0].imm & 0xFFFFFFFF)
                if len(pushes) == 4:
                    break
            elif prior.mnemonic in {"call", "ret", "jmp"}:
                break
        if len(pushes) == 4:
            # Reverse scan sees the first argument nearest the call.
            type_id, outbound, inbound, flag = pushes
            rows.append((insn.address, type_id, outbound, inbound, flag))
        else:
            rows.append((insn.address, None, None, None, None))
        history.append(insn)
    return rows


def server_types(root: pathlib.Path):
    source = root / "MMOCoreORB" / "src" / "server" / "zone" / "ZonePacketHandler.cpp"
    text = source.read_text(encoding="utf-8", errors="replace")
    start = text.index("void ZonePacketHandler::registerObjectControllerMessages()")
    end = text.index("\nTask* ZonePacketHandler::generateMessageTask", start)
    function_text = text[start:end]
    pattern = re.compile(r"registerObject<([^>]+)>\(0x([0-9A-Fa-f]+)\)")
    return {int(value, 16): name for name, value in pattern.findall(function_text)}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("image", type=pathlib.Path)
    parser.add_argument("--server-root", type=pathlib.Path)
    args = parser.parse_args()

    server = server_types(args.server_root) if args.server_root else {}
    rows = registrations(args.image)
    complete = [row for row in rows if row[1] is not None]
    print(f"registration_calls={len(rows)} direct_immediate_rows={len(complete)}")
    print("site,type,outbound_factory,inbound_factory,flag,server_callback")
    for site, type_id, outbound, inbound, flag in rows:
        if type_id is None:
            print(f"0x{site:08X},UNRESOLVED,,,,")
        else:
            print(
                f"0x{site:08X},0x{type_id:X},0x{outbound:08X},"
                f"0x{inbound:08X},{flag},{server.get(type_id, '')}"
            )

    client_types = {row[1] for row in complete}
    overlap = client_types & set(server)
    if server:
        print(f"client_types={len(client_types)} server_types={len(server)} overlap={len(overlap)}")
        missing = sorted(set(server) - client_types)
        print("server_only=" + ",".join(f"0x{x:X}:{server[x]}" for x in missing))


if __name__ == "__main__":
    main()
