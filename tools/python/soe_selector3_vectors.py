#!/usr/bin/env python3
"""Reference model and golden vectors for Stardust.exe SOE transform selector 3.

The algorithm is reconstructed from VA 0x012C6240 (encode), 0x012C62B0
(decode), and 0x012C8CD0 (keystream byte).  This is documentation tooling,
not replacement-client production code.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass


@dataclass
class PadStream:
    state: int

    def next_byte(self) -> int:
        quotient, remainder = divmod(self.state, 127_773)
        value = remainder * 16_807 - quotient * 2_836 + 123
        if value <= 0:
            value += 2_147_483_647
        self.state = value
        return value & 0xFF

    def bytes(self, count: int) -> bytes:
        return bytes(self.next_byte() for _ in range(count))


def transform(data: bytes, session_seed: int, decode: bool = False) -> bytes:
    stream = PadStream(session_seed)
    pad = stream.bytes((len(data) + 3) & ~3)
    output = bytearray(len(data))
    chain = session_seed & 0xFFFFFFFF
    word_count = len(data) // 4

    for index in range(word_count):
        offset = index * 4
        incoming = int.from_bytes(data[offset : offset + 4], "little")
        pad_word = int.from_bytes(pad[offset : offset + 4], "little")
        outgoing = incoming ^ chain ^ pad_word
        output[offset : offset + 4] = (outgoing & 0xFFFFFFFF).to_bytes(4, "little")
        chain = incoming if decode else outgoing

    # Stardust.exe does not carry the word-chain into the 1-3 byte tail.
    tail = word_count * 4
    for index in range(tail, len(data)):
        output[index] = data[index] ^ pad[index]
    return bytes(output)


def emit_vectors() -> None:
    cases = [
        (0x00000001, b""),
        (0x00000001, bytes.fromhex("00")),
        (0x00000001, bytes.fromhex("000102")),
        (0x00000001, bytes.fromhex("00010203")),
        (0x00000001, bytes.fromhex("0001020304")),
        (0x12345678, bytes(range(15))),
        (0x7FFFFFFE, bytes(range(32))),
    ]
    print("seed\tplain\tcipher\tround_trip")
    for seed, plain in cases:
        cipher = transform(plain, seed)
        decoded = transform(cipher, seed, decode=True)
        print(f"0x{seed:08X}\t{plain.hex()}\t{cipher.hex()}\t{decoded == plain}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--vectors", action="store_true", help="print fixed golden vectors")
    args = parser.parse_args()
    if args.vectors:
        emit_vectors()
        return
    parser.error("specify --vectors")


if __name__ == "__main__":
    main()
