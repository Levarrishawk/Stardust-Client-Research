"""Independent synthetic model of Core3 status close/encrypt/CRC ordering.

No sockets or original-client execution. Values are invented test inputs.
"""
import struct

SEED = 0xA1B2C3D4


def frame(packet, crc_enabled):
    data = bytearray(packet + b"\0\0\0")  # marker plus reserved CRC bytes
    chain = SEED
    cursor = 2
    end = len(data) - 2
    while cursor + 4 <= end:
        chain ^= int.from_bytes(data[cursor:cursor + 4], "little")
        data[cursor:cursor + 4] = chain.to_bytes(4, "little")
        cursor += 4
    while cursor < end:
        data[cursor] ^= chain & 255
        cursor += 1
    if crc_enabled:
        crc = 0xFFFFFFFF
        for value in struct.pack("<I", SEED) + data[:-2]:
            crc ^= value
            for _ in range(8):
                crc = (crc >> 1) ^ (0xEDB88320 if crc & 1 else 0)
        data[-2:] = ((~crc) & 0xFFFF).to_bytes(2, "big")
    return bytes(data)


if __name__ == "__main__":
    request = frame(bytes.fromhex("0007FFFE"), True)
    response = frame(bytes.fromhex("0008FFFE") + bytes(36), False)
    assert len(request) == 7 and len(response) == 43
    assert response[-2:] == bytes(2)
    assert request.hex().upper() == "00072B2AD493D0"
    assert response.hex().upper() == "00082B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B2B2B0000"
    print("request:", request.hex().upper())
    print("response:", response.hex().upper())
    print("PASS: synthetic Core3 status framing model (source correlation only)")
    # Extended client reply layout: distinct high/low halves expose endian mistakes.
    extended = struct.pack(">HH5I2Q", 7, 0xFFFE, 1, 2, 3, 4, 5,
                           0x0102030481828384, 0x1112131491929394)
    reply = struct.pack(">HHI4Q", 8, 0xFFFE, 0xAABBCCDD,
                        0x0102030481828384, 0x1112131491929394,
                        0x21222324A1A2A3A4, 0x31323334B1B2B3B4)
    assert reply[8:24] == extended[24:40]
    assert reply.hex().upper() == "0008FFFEAABBCCDD0102030481828384111213149192939421222324A1A2A3A431323334B1B2B3B4"
    print("extended request:", extended.hex().upper())
    print("client reply:", reply.hex().upper())
    print("protected client reply:", frame(reply, True).hex().upper())
