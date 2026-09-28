#!/usr/bin/env python3
"""Verify synthetic Stardust login/zone application fixtures offline.

The vectors contain no credentials and perform no network access.  They pin the
server-source serializers and Stardust.exe parser/state-machine assumptions.
"""

from __future__ import annotations

import struct


def swg_hash(text: str) -> int:
    table = []
    for value in range(256):
        crc = value << 24
        for _ in range(8):
            crc = ((crc << 1) ^ 0x04C11DB7) & 0xFFFFFFFF if crc & 0x80000000 else (crc << 1) & 0xFFFFFFFF
        table.append(crc)
    crc = 0xFFFFFFFF
    for byte in text.encode("ascii"):
        crc = table[((crc >> 24) ^ byte) & 0xFF] ^ ((crc << 8) & 0xFFFFFFFF)
    return (~crc) & 0xFFFFFFFF


def ascii16(value: str) -> bytes:
    raw = value.encode("ascii")
    return struct.pack("<H", len(raw)) + raw


def unicode32(value: str) -> bytes:
    raw = value.encode("utf-16-le")
    return struct.pack("<I", len(raw) // 2) + raw


def message(operands: int, opcode: int, body: bytes) -> bytes:
    return struct.pack("<HI", operands, opcode) + body


def read_ascii16(data: bytes, offset: int) -> tuple[str, int]:
    length = struct.unpack_from("<H", data, offset)[0]
    start = offset + 2
    end = start + length
    return data[start:end].decode("ascii"), end


def parse_enum(data: bytes) -> dict:
    operands, opcode, count = struct.unpack_from("<HII", data, 0)
    assert operands == 2 and opcode == 0xC11C63B9
    cursor = 10
    rows = []
    for _ in range(count):
        galaxy_id = struct.unpack_from("<I", data, cursor)[0]
        name, cursor = read_ascii16(data, cursor + 4)
        metadata = struct.unpack_from("<I", data, cursor)[0]
        cursor += 4
        rows.append((galaxy_id, name, metadata))
    footer = struct.unpack_from("<I", data, cursor)[0]
    assert cursor + 4 == len(data)
    return {"rows": rows, "footer": footer}


def parse_status(data: bytes) -> dict:
    operands, opcode, count = struct.unpack_from("<HII", data, 0)
    assert operands == 3 and opcode == 0x3436AEB6
    cursor = 10
    rows = []
    for _ in range(count):
        galaxy_id = struct.unpack_from("<I", data, cursor)[0]
        host, cursor = read_ascii16(data, cursor + 4)
        zone_port, ping_port = struct.unpack_from("<HH", data, cursor)
        population, capacity, reserved, metadata, status = struct.unpack_from("<IIIII", data, cursor + 4)
        recommended = data[cursor + 24]
        cursor += 25
        rows.append((galaxy_id, host, zone_port, ping_port, population, capacity, reserved, metadata, status, recommended))
    assert cursor == len(data)
    return {"rows": rows}


def parse_permissions(data: bytes) -> dict:
    operands, opcode = struct.unpack_from("<HI", data, 0)
    assert operands == 4 and opcode == swg_hash("ClientPermissionsMessage")
    flags = tuple(bool(value) for value in data[6:])
    assert len(flags) in (3, 4)
    return {"can_connect": flags[0], "can_create_regular": flags[1], "can_create_jedi": flags[2], "skip_tutorial": flags[3] if len(flags) == 4 else None}


def parse_characters(data: bytes) -> list[tuple[str, int, int, int, int]]:
    operands, opcode, count = struct.unpack_from("<HII", data, 0)
    assert operands == 2 and opcode == 0x65EA4574
    cursor = 10
    rows = []
    for _ in range(count):
        units = struct.unpack_from("<I", data, cursor)[0]
        cursor += 4
        name = data[cursor:cursor + units * 2].decode("utf-16-le")
        cursor += units * 2
        race_crc, object_id, galaxy_id, character_type = struct.unpack_from("<IQII", data, cursor)
        cursor += 20
        rows.append((name, race_crc, object_id, galaxy_id, character_type))
    assert cursor == len(data)
    return rows


def main() -> None:
    assert swg_hash("LoginEnumCluster") == 0xC11C63B9
    assert swg_hash("LoginClusterStatus") == 0x3436AEB6

    enum = message(2, 0xC11C63B9, struct.pack("<I", 1) + struct.pack("<I", 7) + ascii16("Stardust") + struct.pack("<II", 0xFFFF8F80, 8))
    status = message(3, 0x3436AEB6, struct.pack("<I", 1) + struct.pack("<I", 7) + ascii16("127.0.0.1") + struct.pack("<HHIIIIIB", 44453, 44462, 100, 0xCB2, 8, 0xFFFF8F80, 2, 0))
    permission_opcode = swg_hash("ClientPermissionsMessage")
    denied = message(4, permission_opcode, bytes((0, 0, 0, 0)))
    allowed = message(4, permission_opcode, bytes((1, 1, 0, 0)))
    historical = message(4, permission_opcode, bytes((1, 1, 0)))
    characters = message(2, 0x65EA4574, struct.pack("<I", 1) + unicode32("A") + struct.pack("<IQII", 0x11223344, 0x1122334455667788, 7, 1))

    assert parse_enum(enum) == {"rows": [(7, "Stardust", 0xFFFF8F80)], "footer": 8}
    assert parse_status(status)["rows"] == [(7, "127.0.0.1", 44453, 44462, 100, 0xCB2, 8, 0xFFFF8F80, 2, 0)]
    assert parse_permissions(denied)["can_connect"] is False
    assert parse_permissions(allowed) == {"can_connect": True, "can_create_regular": True, "can_create_jedi": False, "skip_tutorial": False}
    assert parse_permissions(historical)["skip_tutorial"] is None
    assert parse_characters(characters) == [("A", 0x11223344, 0x1122334455667788, 7, 1)]

    print(f"PASS ClientPermissionsMessage hash 0x{permission_opcode:08X}")
    print(f"PASS LoginEnumCluster {enum.hex(' ')}")
    print(f"PASS LoginClusterStatus {status.hex(' ')}")
    print(f"PASS ClientPermissions denied {denied.hex(' ')}")
    print(f"PASS ClientPermissions allowed {allowed.hex(' ')}")
    print(f"PASS EnumerateCharacterId {characters.hex(' ')}")
    print("ALL LOGIN/ZONE APPLICATION ASSERTIONS PASSED")


if __name__ == "__main__":
    main()
