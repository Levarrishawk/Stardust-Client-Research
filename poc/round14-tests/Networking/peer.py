"""Independent synthetic lifecycle peer and byte/state oracle; no production codecs."""
import argparse
import copy
import hashlib
import json
import struct
from pathlib import Path

H = 0xF122334455667788
T = 0xFE02030405060708
SEED = 0xA1B2C3D4
GEN = 42
MAX = 496
P = struct.pack
U = struct.unpack
OPS = {'create': (5, 0xFE89DDEA), 'destroy': (3, 0x4D45D504),
       'contain': (4, 0x56CBDE9E), 'end': (2, 0x2C436037),
       'baseline': (5, 0x68A75F0C), 'delta': (5, 0x12862153)}


def checksum(path):
    crc = 0xFFFFFFFF
    for c in path.encode('ascii'):
        crc ^= c << 24
        for _ in range(8):
            crc = ((crc << 1) ^ (0x04C11DB7 if crc & 0x80000000 else 0)) & 0xFFFFFFFF
    return (~crc) & 0xFFFFFFFF


def reflected(value):
    for _ in range(8):
        value = (value >> 1) ^ (0xEDB88320 if value & 1 else 0)
    return value


def crc(data):
    c = reflected((~SEED) & 255) ^ 0xFFFFFF
    for shift in (8, 16, 24):
        c = (c >> 8) ^ reflected(((SEED >> shift) ^ c) & 255)
    for b in data:
        c = (c >> 8) ^ reflected((b ^ c) & 255)
    return (~c) & 0xFFFFFFFF


def transform(data, decrypt=False):
    data = bytearray(data)
    cursor = 2 if data[0] == 0 else 1
    chain = SEED
    while cursor + 4 <= len(data):
        value, = struct.unpack_from('<I', data, cursor)
        out = value ^ chain
        struct.pack_into('<I', data, cursor, out)
        chain = value if decrypt else out
        cursor += 4
    for i in range(cursor, len(data)):
        data[i] ^= chain & 255
    return bytes(data)


def frame(packet):
    transformed = transform(packet + b'\0')
    return transformed + P('>H', crc(transformed) & 65535)


def open_frame(wire):
    if len(wire) < 4 or len(wire) > MAX or U('>H', wire[-2:])[0] != crc(wire[:-2]) & 65535:
        raise ValueError('framing')
    decoded = transform(wire[:-2], True)
    if decoded[-1] != 0:
        raise ValueError('marker')
    return decoded[:-1]


def application(kind, *values):
    head = P('<HI', *OPS[kind])
    if kind == 'create':
        oid, bits, template, flag = values
        return head + P('<Q7IIB', oid, *bits, template, flag)
    if kind == 'destroy':
        return head + P('<QB', *values)
    if kind == 'contain':
        return head + P('<QQI', *values)
    if kind == 'end':
        return head + P('<Q', *values)
    oid, fields = values
    return head + P('<QIBIH', oid, 0x54414E4F, 3, len(fields) + 2, 0xFFFF) + fields


def decode(kind, data):
    if len(data) < 6 or U('<HI', data[:6]) != OPS[kind]:
        raise ValueError('header')
    oid, = struct.unpack_from('<Q', data, 6)
    out = {'Kind': kind, 'ObjectId': oid, 'WireHex': data.hex().upper()}
    if kind == 'create':
        if len(data) != 47:
            raise ValueError('create size')
        bits = list(U('<7I', data[14:42]))
        if any((b & 0x7F800000) == 0x7F800000 for b in bits):
            raise ValueError('nonfinite')
        template, flag = U('<IB', data[42:])
        out.update(Bits=bits, TemplateCrc=template, Flag=flag)
    elif kind == 'contain':
        if len(data) != 26:
            raise ValueError('contain size')
        container, arrangement = U('<QI', data[14:])
        out.update(ContainerId=container, ArrangementBits=arrangement)
    elif kind == 'destroy':
        if len(data) != 15:
            raise ValueError('destroy size')
        out['Flag'] = data[14]
    elif kind == 'end':
        if len(data) != 14:
            raise ValueError('end size')
    else:
        if len(data) < 25:
            raise ValueError('envelope size')
        tag, view, size, count = U('<IBIH', data[14:25])
        if size != len(data) - 23 or size < 2 or size > 768:
            raise ValueError('payload size')
        out.update(RawTag=tag, View=view, DeclaredCount=count, OpaqueHex=data[25:].hex().upper())
    return out


class Registry:
    """Independent model of the accepted modern registry policy, not original behavior."""
    def __init__(self, catalog):
        self.entries = {}
        self.revision = 0
        self.generation = GEN
        self.catalog = catalog

    def apply(self, kind, data, generation=GEN):
        v = decode(kind, data)
        if generation != self.generation:
            return False
        oid = v['ObjectId']
        if kind == 'create':
            if v['TemplateCrc'] not in self.catalog or oid == 0:
                raise ValueError('catalog/id')
            if oid in self.entries:
                if self.entries[oid]['CreateHex'] != v['WireHex']:
                    raise ValueError('conflict')
                return True
            if len(self.entries) >= 2:
                raise ValueError('capacity')
            self.revision += 1
            self.entries[oid] = dict(ObjectId=oid, Revision=self.revision,
                CreationRevision=self.revision, SceneGeneration=self.generation,
                Phase='PendingBaselines', ContainerId=0, ArrangementBits=0,
                CreateHex=v['WireHex'], TemplateCrc=v['TemplateCrc'])
        elif kind in ('baseline', 'delta'):
            if oid not in self.entries:
                raise ValueError('missing opaque recipient')
        elif kind == 'destroy':
            if oid not in self.entries:
                return False
            for child in sorted(self.entries):
                if self.entries[child]['ContainerId'] == oid:
                    self.revision += 1
                    self.entries[child].update(ContainerId=0, Revision=self.revision)
            del self.entries[oid]
        else:
            if oid not in self.entries:
                raise ValueError('missing')
            current = self.entries[oid]
            if kind == 'end':
                if current['Phase'] != 'Finalized':
                    self.revision += 1
                    current.update(Phase='Finalized', Revision=self.revision)
            else:
                parent = v['ContainerId']
                if parent and parent not in self.entries:
                    raise ValueError('missing parent')
                cursor = parent
                while cursor:
                    if cursor == oid:
                        raise ValueError('cycle')
                    cursor = self.entries[cursor]['ContainerId']
                if (parent, v['ArrangementBits']) != (current['ContainerId'], current['ArrangementBits']):
                    self.revision += 1
                    current.update(ContainerId=parent, ArrangementBits=v['ArrangementBits'], Revision=self.revision)
        return True

    def snapshots(self):
        return copy.deepcopy([self.entries[k] for k in sorted(self.entries)])


def generate(catalog_path):
    source = catalog_path.read_bytes()
    catalog = json.loads(source)
    assert {e['templateCrc'] for e in catalog['entries']} == {2937962913, 2907007746}
    for e in catalog['entries']:
        assert checksum(e['objectPath']) == e['templateCrc']
    # Wire order Qx/Qy/Qz/Qw, X/Z/Y. Signed zero and a subnormal are deliberate.
    human = application('create', H, [0x80000000, 0x3E800000, 0xBF000000, 0x40000000,
                                    0xBFC00000, 1, 0xC2350000], 2937962913, 255)
    terminal = application('create', T, [0, 0, 0, 0x3F800000, 0x3FC00000,
                                       0x80000000, 0x41200000], 2907007746, 165)
    opaque = b'SYNTHETIC OPAQUE TANO3; NO RECOVERED FIELD SCHEMA\0' + bytes(range(256)) * 2
    apps = [('create', human), ('create', terminal), ('baseline', application('baseline', T, opaque)),
        ('delta', application('delta', T, b'SYNTHETIC\0\xff\x7f\x80')),
        ('end', application('end', H)), ('end', application('end', T)),
        ('contain', application('contain', H, T, 0xFFFFFFFE)),
        ('contain', application('contain', H, 0, 0x80000001)),
        ('destroy', application('destroy', H, 255)), ('create', human), ('end', application('end', H)),
        ('create', terminal)]
    packets = []
    for _, app in apps:
        if len(app) <= MAX - 7:
            packets.append(P('>HH', 9, len(packets)) + app)
        else:
            cursor = 0
            while cursor < len(app):
                first = cursor == 0
                n = min(MAX - 7 - (4 if first else 0), len(app) - cursor)
                body = (P('>I', len(app)) if first else b'') + app[cursor:cursor+n]
                packets.append(P('>HH', 13, len(packets)) + body)
                cursor += n
    wires = [frame(p) for p in packets]
    damaged = bytearray(wires[0]); damaged[-1] ^= 1
    order = [1, 1, 0, 3, 2] + list(range(4, len(wires))) + [0]
    transcript = [bytes(damaged)] + [wires[i] for i in order]
    bad = []
    for kind, app in apps[:6]:
        bad.append(dict(Name=f'{kind}-{len(bad)}-truncated', Kind=kind, Hex=app[:-1].hex().upper()))
    nan = bytearray(human); nan[38:42] = P('<I', 0x7FC00000)
    unknown = bytearray(human); unknown[6:14] = P('<Q', H+1); unknown[42:46] = P('<I', 0)
    size = bytearray(apps[2][1]); size[19:23] = P('<I', 2)
    bad += [dict(Name='late-nan', Kind='create', Hex=nan.hex().upper()),
        dict(Name='late-unknown-crc', Kind='create', Hex=unknown.hex().upper()),
        dict(Name='late-opaque-size', Kind='baseline', Hex=size.hex().upper()),
        dict(Name='late-cycle', Kind='contain', Hex=application('contain', T, H, 1).hex().upper())]
    return dict(Version=1, Label='INDEPENDENTLY AUTHORED SYNTHETIC OFFLINE PEER',
        CatalogHex=source.hex().upper(), CatalogSha256=hashlib.sha256(source).hexdigest(),
        SessionResponseHex=P('>HII3BI', 2, 0x12345678, SEED, 2, 1, 4, MAX).hex().upper(),
        Applications=[dict(Kind=k, Hex=b.hex().upper()) for k,b in apps],
        PacketsHex=[p.hex().upper() for p in packets], WiresHex=[w.hex().upper() for w in wires],
        TranscriptHex=[w.hex().upper() for w in transcript], Negatives=bad)


def delivery_oracle(peer):
    expected = 0
    future = {}
    fragment = bytearray()
    length = None
    traces = []
    delivered = []
    for text in peer['TranscriptHex']:
        wire = bytes.fromhex(text)
        try:
            packet = open_frame(wire)
        except ValueError:
            traces.append(dict(Rejected=True, Delivered=[], Controls=[]))
            continue
        opcode, seq = U('>HH', packet[:4])
        batch = []
        if seq < expected:
            ack = seq
            control = 21
        elif seq > expected:
            future.setdefault(seq, (opcode, packet[4:]))
            ack = seq
            control = 17
        else:
            ready = [(opcode, packet[4:])]
            expected += 1
            while expected in future:
                ready.append(future.pop(expected)); expected += 1
            ack = expected - 1
            control = 21
            for op, body in ready:
                if op == 9:
                    batch.append(body)
                else:
                    if length is None:
                        length, = U('>I', body[:4]); body = body[4:]
                    fragment.extend(body)
                    if len(fragment) == length:
                        batch.append(bytes(fragment)); fragment.clear(); length = None
        delivered.extend(batch)
        traces.append(dict(Rejected=False, Delivered=[b.hex().upper() for b in batch],
            Controls=[frame(P('>HH', control, ack)).hex().upper()]))
    assert not future and length is None
    assert delivered == [bytes.fromhex(a['Hex']) for a in peer['Applications']]
    return traces


def verify(peer, output):
    catalog = json.loads(bytes.fromhex(peer['CatalogHex']))
    assert output['CatalogSha256'].lower() == peer['CatalogSha256']
    assert output['Transport'] == delivery_oracle(peer)
    assert output['PacketsHex'] == peer['PacketsHex']
    assert output['WiresHex'] == peer['WiresHex']
    model = Registry({e['templateCrc']: e for e in catalog['entries']})
    records = []
    for app in peer['Applications']:
        b = bytes.fromhex(app['Hex']); decoded = decode(app['Kind'], b)
        assert model.apply(app['Kind'], b)
        records.append(dict(Message=decoded, Snapshots=model.snapshots()))
    assert output['Records'] == records
    creates = [r['Message'] for r in records if r['Message']['Kind'] == 'create']
    assert output['Placements'] == [dict(ObjectId=v['ObjectId'], RawBits=v['Bits'],
        TranslationBits=[v['Bits'][4], v['Bits'][6], v['Bits'][5]]) for v in creates]
    assert output['MalformedProbes'] == sum(len(bytes.fromhex(a['Hex'])) + 7 for a in peer['Applications'])
    model.entries.clear(); model.generation += 1
    epochs = [dict(Name='reset', Snapshots=model.snapshots())]
    for app in [peer['Applications'][0], peer['Applications'][1], peer['Applications'][4], peer['Applications'][5]]:
        model.apply(app['Kind'], bytes.fromhex(app['Hex']), GEN+1)
        epochs.append(dict(Name=app['Kind'], Snapshots=model.snapshots()))
    model.entries.clear()
    epochs.append(dict(Name='close', Snapshots=[]))
    assert output['Epochs'] == epochs
    assert output['IdentityChecks'] == dict(ContainmentKeepsIncarnation=True, ContainmentStalesHandle=True,
        RecreateStalesIncarnation=True, OtherObjectPreserved=True, ForeignIncarnationRejected=True,
        StaleGenerationRejected=True, ResetStalesIncarnations=True, Closed=True)
    for row, bad in zip(output['Negatives'], peer['Negatives'], strict=True):
        fresh = Registry(model.catalog)
        for app in peer['Applications'][:2]: fresh.apply(app['Kind'], bytes.fromhex(app['Hex']))
        fresh.apply('contain', application('contain', H, T, 0xFFFFFFFE))
        before = fresh.snapshots()
        try:
            fresh.apply(bad['Kind'], bytes.fromhex(bad['Hex']))
        except (ValueError, struct.error):
            pass
        else:
            raise AssertionError('negative oracle accepted')
        assert fresh.snapshots() == before
        assert row == dict(Name=bad['Name'], Rejected=True, UnchangedBeforeClose=True,
                           Closed=True, Remaining=0, SuffixApplied=False)
    assert len(output['Negatives']) == len(peer['Negatives'])
    print(f"PASS independent exact application/transport/order/registry oracle: {len(records)} records, {len(peer['Negatives'])} late failures")


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--generate', type=Path)
    parser.add_argument('--catalog', type=Path)
    parser.add_argument('--verify', type=Path)
    parser.add_argument('--peer', type=Path)
    args = parser.parse_args()
    if args.generate:
        args.generate.parent.mkdir(parents=True, exist_ok=True)
        peer = generate(args.catalog)
        delivery_oracle(peer)
        args.generate.write_text(json.dumps(peer, indent=2)+'\n', encoding='utf-8', newline='\n')
        print('PASS independently hand-packed mixed lifecycle and fragmented/reordered SOE peer')
    else:
        verify(json.loads(args.peer.read_text()), json.loads(args.verify.read_text()))
