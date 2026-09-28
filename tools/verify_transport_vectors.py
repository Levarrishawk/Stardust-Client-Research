#!/usr/bin/env python3
"""Verify documented Stardust/SOE transport fixtures without network access."""

import struct
import hashlib


def crc_table():
    result = []
    for value in range(256):
        crc = value
        for _ in range(8):
            crc = (crc >> 1) ^ (0xEDB88320 if crc & 1 else 0)
        result.append(crc & 0xFFFFFFFF)
    return result


TABLE = crc_table()


def seeded_crc(data: bytes, seed: int) -> int:
    crc = TABLE[(~seed) & 0xFF] ^ 0x00FFFFFF
    for shift in (8, 16, 24):
        index = (seed >> shift) ^ crc
        crc = ((crc >> 8) & 0x00FFFFFF) ^ TABLE[index & 0xFF]
    for byte in data:
        crc = ((crc >> 8) & 0x00FFFFFF) ^ TABLE[(byte ^ crc) & 0xFF]
    return (~crc) & 0xFFFFFFFF


def selector4(data: bytes, seed: int) -> bytes:
    out = bytearray(data)
    chain = seed
    cursor = 2
    while cursor + 4 <= len(out):
        word = struct.unpack_from("<I", out, cursor)[0] ^ chain
        struct.pack_into("<I", out, cursor, word)
        chain = word
        cursor += 4
    while cursor < len(out):
        out[cursor] ^= chain & 0xFF
        cursor += 1
    return bytes(out)


def expand_sequence_near(reference: int, wire_sequence: int) -> int:
    candidate = (reference & ~0xFFFF) | wire_sequence
    if candidate - reference > 0x7FFF:
        candidate -= 0x10000
    elif reference - candidate > 0x8000:
        candidate += 0x10000
    return candidate


def check(name: str, actual, expected):
    if actual != expected:
        raise AssertionError(f"{name}: {actual!r} != {expected!r}")
    print(f"PASS {name}")


def main():
    seed = 0x12345678
    check("seeded CRC primitive", seeded_crc(bytes.fromhex("00 09 00 00"), seed), 0xD0883317)

    primitive = selector4(bytes.fromhex("00 09 00 01 10 20 30 40 50"), seed)
    check("selector-4 primitive", primitive.hex(), "000978572432483828")
    check("selector-4 primitive CRC", seeded_crc(primitive, seed), 0x64AEB373)

    data = selector4(bytes.fromhex("00 09 00 00 AA BB 00"), seed)
    check("uncompressed established transform", data.hex(), "000978569ea978")
    check("uncompressed established CRC", seeded_crc(data, seed), 0x00729231)

    login_app = bytes.fromhex(
        "04 00 96 1F 13 41 01 00 75 01 00 70 "
        "0E 00 32 30 32 36 30 38 31 37 2D 53 44 3A 30 31"
    )
    framed = bytes.fromhex("00 09 00 00") + login_app + b"\x00"
    transformed = selector4(framed, seed)
    check(
        "LoginClientId transform",
        transformed.hex(),
        "000978563012ee492353ef495652ef395852dd096a64ed315b53c0621f69f0f1c0",
    )
    check("LoginClientId CRC", seeded_crc(transformed, seed), 0x55CCF704)

    ack = selector4(bytes.fromhex("00 15 00 00 00"), seed)
    check("ACK transform", ack.hex(), "0015787878")
    check("ACK CRC", seeded_crc(ack, seed), 0xA8143946)

    application = bytes(value & 0xFF for value in range(600))
    fragment_frames = (
        bytes.fromhex("00 0D 00 00") + struct.pack(">I", len(application)) + application[:485] + b"\x00",
        bytes.fromhex("00 0D 00 01") + application[485:] + b"\x00",
    )
    fragment_hashes = (
        "4ae6870e65d8c69dc50305226e489dc1364336ac647718a40909ff485198a6ba",
        "0f3a79ad759b761a16aa6ed5d10c69024b062c823cadc41fe1d1cadcac04fa26",
    )
    fragment_lengths = (496, 122)
    for index, (frame, expected_hash, expected_length) in enumerate(zip(fragment_frames, fragment_hashes, fragment_lengths), 1):
        transformed_fragment = selector4(frame, seed)
        crc = seeded_crc(transformed_fragment, seed).to_bytes(4, "big")[-2:]
        wire_fragment = transformed_fragment + crc
        check(f"fragment {index} length", len(wire_fragment), expected_length)
        check(f"fragment {index} SHA-256", hashlib.sha256(wire_fragment).hexdigest(), expected_hash)

    large_application = bytes(value & 0xFF for value in range(1100))
    three_frames = (
        bytes.fromhex("00 0D 00 00") + struct.pack(">I", len(large_application)) + large_application[:485] + b"\x00",
        bytes.fromhex("00 0D 00 01") + large_application[485:974] + b"\x00",
        bytes.fromhex("00 0D 00 02") + large_application[974:] + b"\x00",
    )
    three_hashes = (
        "1970b660cc4019fd8cc99cae115932c20f81fad03c05b0a0e3fa6a1131e0039f",
        "3b5dd8e17138ba36a1f414ad047df6111570cc3e2dd0d36119886c3382488ea4",
        "be71a3c08dae3227c01bd8f8d36382b480ee8360de68b983c1c5199385f01b63",
    )
    for index, (frame, expected_hash, expected_length) in enumerate(zip(three_frames, three_hashes, (496, 496, 133)), 1):
        transformed_fragment = selector4(frame, seed)
        wire_fragment = transformed_fragment + seeded_crc(transformed_fragment, seed).to_bytes(4, "big")[-2:]
        check(f"three-part fragment {index} length", len(wire_fragment), expected_length)
        check(f"three-part fragment {index} SHA-256", hashlib.sha256(wire_fragment).hexdigest(), expected_hash)

    check("sequence wrap forward", expand_sequence_near(0xFFFF, 0), 0x10000)
    check("sequence wrap backward", expand_sequence_near(0x10000, 0xFFFF), 0xFFFF)
    retained = [0xFFFF, 0x10000]
    retained.remove(expand_sequence_near(retained[0], 0))
    check("selective wrap release", retained, [0xFFFF])

    print("ALL 22 TRANSPORT ASSERTIONS PASSED")


if __name__ == "__main__":
    main()
