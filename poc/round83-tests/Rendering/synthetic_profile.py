"""Minimal authored Version13 producer fixture; not a runnable asset-backed profile."""
import importlib.util
import json
from pathlib import Path
import struct
import sys

ROOT = Path(__file__).resolve().parents[3]


def create():
    spec = importlib.util.spec_from_file_location('frozen81', ROOT / 'poc/round81-tests/Networking/transport_fixtures.py')
    net = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(net)
    wire = net.encoder(0xa1b2c3d4)
    bits = lambda v: struct.unpack('<I', struct.pack('<f', v))[0]
    app = struct.pack('<HIIIQII8f', 5, 0x80ce5e46, 5, 0x71, 42, 30, 1, 0, 0, 0, 1, 0, 0, 2, 0)
    prefix = bytearray(app)
    struct.pack_into('<I', prefix, 26, 2)
    disconnect = struct.pack('>HIH', 5, 0x12345678, 6)
    blocks = [struct.pack('>HH', 9, 2) + prefix, disconnect, struct.pack('>HH', 9, 3) + prefix]
    frame = lambda raw: wire.frame(raw).hex().upper()
    value = dict(Version=13, Policy='SuppliedConfiguredOfflinePlayground', MaximumUpdates=120, StepMicroseconds=10000,
                 PlaygroundSettings=dict(Version=1, MaximumUpdates=120, MaximumClickAttempts=8, StepMicroseconds=10000,
                                         WalkSpeed=3, RunSpeed=4, ArrivalRadius=.001, HoldMissingDestination=True),
                 SpeedBits=bits(3), WalkSpeedBits=bits(3), RunSpeedBits=bits(4), ArrivalRadiusBits=bits(.001),
                 InteractiveReplay=False, SyntheticFrameClock=False, OriginalMovementParity=False, OriginalSenderParity=False,
                 InvalidDisconnectConnection=False, OutboundFlags=5, OutboundTimestampBase=0, OutboundCounterBase=0, CorrectionCounter=1,
                 SoeConnectionId=0x12345678, SoeSeed=0xa1b2c3d4, SoeMaximumDatagram=496, SoeCrcWidth=2,
                 SoeCorrectionHex=app.hex().upper(), SoeCorrectionDatagrams=[frame(struct.pack('>HH', 9, i) + app) for i in range(2)],
                 SoeCorrectionAcks=[frame(struct.pack('>HH', 0x15, i)) for i in range(2)], SoeDisconnectHex=frame(disconnect),
                 SoeOuterPrefixApplicationHex=prefix.hex().upper(), SoeOuterAckHex=frame(struct.pack('>HH', 0x15, 2)),
                 SoeOuterDisconnectHex=frame(b'\0\3' + b''.join(bytes([len(b)]) + b for b in blocks)))
    assert net.reseed(value, value['SoeConnectionId'], value['SoeSeed']) == value
    return value


if __name__ == '__main__':
    target = Path(sys.argv[1])
    with target.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(create(), indent=2) + '\n')
    print('PASS authored synthetic producer profile; no native resources')
