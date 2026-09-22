#!/usr/bin/env python3
"""Verify documented Stardust/SOE transport fixtures without network access."""

import struct


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

    print("ALL 9 TRANSPORT ASSERTIONS PASSED")


if __name__ == "__main__":
    main()
