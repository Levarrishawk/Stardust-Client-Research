"""Verify pinned auction-query type evidence and synthetic Core3 alignment cases.

Static input only. No original-client execution, network, assets or credentials.
The callback model below reproduces field reads, not server gameplay behavior.
"""
import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'local-private/toolchain/python-static-analysis-packages'))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

SHA = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'


def fixture(any_text):
    def wide(text):
        encoded = text.encode('utf-16-le')
        return struct.pack('<I', len(encoded) // 2) + encoded
    return (struct.pack('<H6I', 14, 0x679E0D00, 3, 7, 2, 0x400, 0x12345678)
            + wide('synthetic ore') + wide(any_text)
            + struct.pack('<iiBQBH', 11, 99, 1, 0x1122334455667788, 0, 25))


def core3_callback_fields(wire):
    offset = 6
    def take(fmt):
        nonlocal offset
        value = struct.unpack_from(fmt, wire, offset)
        offset += struct.calcsize(fmt)
        return value[0] if len(value) == 1 else value
    location, counter, search, category, unknown1 = take('<5I')
    count = take('<I')
    text_bytes = count * 2
    if text_bytes > len(wire) - offset:
        raise ValueError('truncated first Unicode filter')
    text = wire[offset:offset + text_bytes].decode('utf-16-le')
    offset += text_bytes
    unknown2, low, high, fee, vendor, own, page = take('<IiiBQBH')
    return dict(location=location, counter=counter, search=search, category=category,
                unknown1=unknown1, filter=text, unknown2=unknown2, low=low,
                high=high, fee=fee, vendor=vendor, own=own, page=page,
                remaining=len(wire) - offset)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('exe', type=Path)
    args = parser.parse_args()
    assert hashlib.sha256(args.exe.read_bytes()).hexdigest().upper() == SHA, 'Executable identity mismatch'
    pe = pefile.PE(str(args.exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    def instructions(start, end):
        return list(md.disasm(pe.get_data(start - base, end - start), start))
    slots = struct.unpack('<III', pe.get_data(0x015EAD30 - base, 12))
    assert slots == (0x00ACEA90, 0x00ACEA50, 0x00ACEA70)
    constructor = instructions(0x00ACFFA0, 0x00AD02F0)
    unicode_assignments = [i.address for i in constructor if i.mnemonic == 'mov' and i.op_str.endswith(', 0x15ead30')]
    assert unicode_assignments == [0x00AD00EC, 0x00AD0119]
    registrations = [i for i in constructor if i.mnemonic == 'call' and i.op_str == '0x118b5f0']
    assert len(registrations) == 13
    for start, end, target in [(0x00ACEA50, 0x00ACEA65, '0x11949d0'),
                               (0x00ACEA70, 0x00ACEA85, '0x11948b0')]:
        calls = [i.op_str for i in instructions(start, end) if i.mnemonic == 'call']
        assert calls == [target]
    reader = instructions(0x011948B0, 0x011949CD)
    assert any(i.mnemonic == 'push' and i.op_str == '4' for i in reader)
    assert any(i.mnemonic == 'lea' and i.op_str == 'esi, [edx + eax*2]' for i in reader)
    initializer = instructions(0x010BD390, 0x010BD3AA)
    assert any(i.mnemonic == 'push' and i.op_str == '0x18d780c' for i in initializer)
    assert any(i.mnemonic == 'mov' and i.op_str == 'ecx, 0x199472c' for i in initializer)
    assert pe.get_data(0x018D780C - base, 16).split(b'\0')[0] == b'ItemTemplateCrc'

    empty = core3_callback_fields(fixture(''))
    assert empty['unknown2'] == 0 and empty['low'] == 11 and empty['high'] == 99
    assert empty['vendor'] == 0x1122334455667788 and empty['page'] == 25 and empty['remaining'] == 0
    nonempty = core3_callback_fields(fixture('iron'))
    assert nonempty['unknown2'] == 4 and nonempty['low'] != 11 and nonempty['remaining'] == 8
    print(json.dumps(dict(result='PASS', executable_sha256=SHA,
        original_payload_members=13, original_unicode_fields=2,
        first_extra_scalar_property='ItemTemplateCrc',
        core3_empty_second_filter_aligned=True,
        core3_nonempty_second_filter_misaligned=True,
        fixtures='synthetic; callback field model only; no live query'), indent=2))


if __name__ == '__main__':
    main()
