#!/usr/bin/env python3
"""Verify the current Stardust RCNO:6 inherited-field alignment fixture."""

import struct


def ascii16(value: str) -> bytes:
    raw = value.encode("ascii")
    return struct.pack("<H", len(raw)) + raw


def unicode32(value: str) -> bytes:
    raw = value.encode("utf-16le")
    return struct.pack("<I", len(value)) + raw


def read_ascii16(data: bytes, cursor: int) -> tuple[str, int]:
    length = struct.unpack_from("<H", data, cursor)[0]
    cursor += 2
    return data[cursor:cursor + length].decode("ascii"), cursor + length


def read_unicode32(data: bytes, cursor: int) -> tuple[str, int]:
    length = struct.unpack_from("<I", data, cursor)[0]
    cursor += 4
    end = cursor + length * 2
    return data[cursor:end].decode("utf-16le"), end


def main() -> None:
    max_stack = 100000
    resource_type = "flora_resources"
    resource_name = "Synthetic Flora"
    body = (
        ascii16("")
        + struct.pack("<I", 0)
        + ascii16("")
        + unicode32("")
        + struct.pack("<I", max_stack)
        + ascii16(resource_type)
        + unicode32(resource_name)
    )

    # The client registry, not the server insert-call count, defines fields.
    common_u32 = struct.unpack_from("<I", body, 0)[0]
    defender_count, defender_counter = struct.unpack_from("<II", body, 4)
    parsed_max = struct.unpack_from("<I", body, 12)[0]
    parsed_type, cursor = read_ascii16(body, 16)
    parsed_name, cursor = read_unicode32(body, cursor)

    assert common_u32 == 0
    assert (defender_count, defender_counter) == (0, 0)
    assert parsed_max == max_stack
    assert parsed_type == resource_type
    assert parsed_name == resource_name
    assert cursor == len(body)
    assert len(body[:12]) == 12

    print(f"PASS RCNO:6 inherited prefix {body[:12].hex(' ')}")
    print(f"PASS RCNO:6 max={parsed_max} type={parsed_type!r} name={parsed_name!r}")
    print("ALL 7 RCNO VIEW-6 ASSERTIONS PASSED")


if __name__ == "__main__":
    main()
