#!/usr/bin/env python3
"""Independent synthetic peer and source-wire oracle; no network or original assets."""
import argparse
import json
from pathlib import Path
import struct
from verify_status_vectors import frame, SEED
from verify_zone_scene_protocol import parse as parse_scene

SCENE = bytes.fromhex('0900aedfe63aa588776655443322f10300412f420000c03f000010c0000070400300545c4308070605040302fe')
READY = bytes.fromhex('0100221cfd43')
CREATE = b'SYNTH-CREATE\0\1'
BASELINE = b'SYNTH-BASELINE' + bytes(range(57))


def fragments(payload, sequence):
    pieces = [struct.pack('>I', len(payload)) + payload[:21]]
    pieces += [payload[start:start + 25] for start in range(21, len(payload), 25)]
    return [frame(struct.pack('>HH', 13, sequence + i) + piece, True) for i, piece in enumerate(pieces)]


def peer():
    start = fragments(SCENE, 0)
    baseline = fragments(BASELINE, 3)
    wrong = bytearray(SCENE); struct.pack_into('<Q', wrong, 7, 7)
    wrong_frames = fragments(wrong, 0)
    return dict(response=bytes.fromhex('000212345678A1B2C3D402010400000020'),
                start0=start[0], start1=start[1], create=frame(struct.pack('>HH', 9, 2) + CREATE, True),
                baseline0=baseline[0], baseline1=baseline[1], baseline2=baseline[2],
                ready=frame(struct.pack('>HH', 9, 6) + READY, True),
                ackSelect=frame(bytes.fromhex('00150000'), True), ackReady=frame(bytes.fromhex('00150001'), True),
                earlyReady=frame(struct.pack('>HH', 9, 0) + READY, True),
                badStart=frame(struct.pack('>HH', 9, 0) + SCENE[:10], True),
                wrong0=wrong_frames[0], wrong1=wrong_frames[1])


def unframe(wire):
    if len(wire) < 5 or len(wire) > 32:
        raise ValueError('framing length')
    # Reuse source-backed peer transform for CRC verification after independent decryption.
    data = bytearray(wire[:-2]); chain = SEED; cursor = 2
    while cursor + 4 <= len(data):
        cipher, = struct.unpack_from('<I', data, cursor)
        struct.pack_into('<I', data, cursor, cipher ^ chain)
        chain = cipher; cursor += 4
    while cursor < len(data):
        data[cursor] ^= chain & 255; cursor += 1
    if data[-1] != 0 or frame(bytes(data[:-1]), True) != wire:
        raise ValueError('CRC/marker')
    return bytes(data[:-1])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--generate', type=Path)
    parser.add_argument('--exports', type=Path)
    args = parser.parse_args(); fixtures = peer()
    assert len(BASELINE) == 71 and len(CREATE) == 14
    assert parse_scene(SCENE, True)['ObjectId'] == 0xF122334455667788
    for name, wire in fixtures.items():
        if name != 'response': unframe(wire)
    malformed_start = unframe(fixtures['badStart'])[4:]
    try: parse_scene(malformed_start, True)
    except ValueError: pass
    else: raise AssertionError('truncated scene fixture accepted by independent oracle')
    wrong_parts = [unframe(fixtures[name])[4:] for name in ('wrong0','wrong1')]
    assert parse_scene(wrong_parts[0][4:] + wrong_parts[1], True)['ObjectId'] == 7
    assert unframe(fixtures['earlyReady'])[4:] == READY
    if args.generate:
        args.generate.parent.mkdir(parents=True, exist_ok=True)
        args.generate.write_text(json.dumps({k:v.hex().upper() for k,v in fixtures.items()}, indent=2)+'\n', encoding='utf-8')
        print('Independent peer generated:', args.generate.resolve())
    if args.exports:
        result = json.loads((args.exports/'transcript.json').read_text(encoding='utf-8'))
        assert result['Peer'] == {k:v.hex().upper() for k,v in fixtures.items()}
        select = struct.pack('<HIQ',2,0xB5098D76,0xF122334455667788)
        assert bytes.fromhex(result['SessionRequest']) == struct.pack('>HIII',1,2,0x12345678,32)
        assert unframe(bytes.fromhex(result['Select'])) == struct.pack('>HH',9,0)+select
        ready = bytes.fromhex(result['Ready'])
        assert unframe(ready) == struct.pack('>HH',9,1)+READY
        assert ready == bytes.fromhex(result['Retransmission'])
        assert result['Staged'] == [CREATE.hex().upper(),BASELINE.hex().upper()]
        assert result['FinalState'] == 'Ready'
        controls = [unframe(bytes.fromhex(value)) for value in result['Controls']]
        assert all(len(value)==4 and value[0]==0 and value[1] in (17,21) for value in controls)
        assert controls == [struct.pack('>HH',opcode,sequence) for opcode,sequence in ((17,1),(17,2),(21,2),(21,0),(17,5),(21,3),(21,5),(21,6),(21,6))]
        assert result['ReadyPublications'] == 1 and result['RejectedFraming'] == 2
        # Fragment wire headers/declared total and concatenated payload independently agree.
        for names,payload in ((('start0','start1'),SCENE),(('baseline0','baseline1','baseline2'),BASELINE)):
            pieces=[unframe(fixtures[n])[4:] for n in names]
            assert struct.unpack('>I',pieces[0][:4])[0]==len(payload)
            assert pieces[0][4:]+b''.join(pieces[1:])==payload
        bad=bytearray(fixtures['create']); bad[-1]^=1
        for wire in (bytes(bad),fixtures['create'][:-1]):
            try: unframe(wire)
            except ValueError: continue
            raise AssertionError('damaged frame accepted')
        print('Independent transport-to-scene wire oracle passed:', args.exports.resolve())
    if not args.generate and not args.exports: parser.error('provide --generate or --exports')


if __name__ == '__main__': main()


