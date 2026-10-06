"""Independent synthetic Version13 transport fixtures. No native output is an oracle."""
import copy
import importlib.util
import json
import math
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
FIELDS = {'SoeConnectionId', 'SoeSeed', 'SoeMaximumDatagram', 'SoeCrcWidth',
          'SoeCorrectionHex', 'SoeCorrectionDatagrams', 'SoeCorrectionAcks',
          'SoeDisconnectHex', 'SoeOuterPrefixApplicationHex', 'SoeOuterAckHex',
          'SoeOuterDisconnectHex'}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def uint(value):
    require(type(value) is int and 0 <= value <= 0xffffffff, 'U32 integer required')
    return value


def encoder(seed):
    # Each call loads isolated globals; no mutation of an imported verifier's seed.
    spec = importlib.util.spec_from_file_location('independent81_soe', ROOT / 'poc/round14-tests/Networking/peer.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.SEED = uint(seed)
    return module


def admit(profile):
    require(type(profile) is dict, 'Profile object required')
    require(len(json.dumps(profile, allow_nan=False).encode()) <= 16384, 'Bounded profile required')
    require(type(profile.get('Version')) is int and profile['Version'] == 13 and
            profile.get('Policy') == 'SuppliedConfiguredOfflinePlayground', 'Configured Version13 required')
    require({k for k in profile if k.startswith('Soe')} == FIELDS, 'Exact frozen Version13 SOE fields required')
    uint(profile['SoeConnectionId']); uint(profile['SoeSeed'])
    require(type(profile['SoeMaximumDatagram']) is int and profile['SoeMaximumDatagram'] == 496 and
            type(profile['SoeCrcWidth']) is int and profile['SoeCrcWidth'] == 2, 'Frozen CRC2/max496 required')
    for key in ('InteractiveReplay', 'SyntheticFrameClock', 'OriginalMovementParity', 'OriginalSenderParity', 'InvalidDisconnectConnection'):
        require(type(profile.get(key)) is bool, 'Explicit Boolean required: ' + key)
    require(not profile['OriginalMovementParity'] and not profile['OriginalSenderParity'] and
            (not profile['SyntheticFrameClock'] or profile['InteractiveReplay']), 'Synthetic offline policy required')
    spec = importlib.util.spec_from_file_location('settings81', ROOT / 'poc/round77-tests/Integration/create_configured_profile.py')
    settings = importlib.util.module_from_spec(spec); spec.loader.exec_module(settings)
    settings.settings(profile['PlaygroundSettings'])
    require(profile['MaximumUpdates'] == profile['PlaygroundSettings']['MaximumUpdates'] and
            type(profile['MaximumUpdates']) is int and profile['StepMicroseconds'] == 10000,
            'Matching bounded settings required')
    for key, name in [('SpeedBits', 'WalkSpeed'), ('WalkSpeedBits', 'WalkSpeed'),
                      ('RunSpeedBits', 'RunSpeed'), ('ArrivalRadiusBits', 'ArrivalRadius')]:
        require(uint(profile[key]) == settings.bits(profile['PlaygroundSettings'][name]), 'Settings bits mismatch')
    for key in ('OutboundFlags', 'OutboundTimestampBase', 'OutboundCounterBase'):
        uint(profile[key])
    return encoder(profile['SoeSeed'])


def handshakes(profile):
    admit(profile)
    return dict(RequestHex=struct.pack('>HIII', 1, 2, profile['SoeConnectionId'], 496).hex().upper(),
                ResponseHex=struct.pack('>HII3BI', 2, profile['SoeConnectionId'], profile['SoeSeed'], 2, 1, 4, 496).hex().upper())


def reseed(profile, connection_id, seed):
    """Deep-copy frozen13; preserve raw applications, regenerate every protected field."""
    old = admit(profile); new = encoder(seed); uint(connection_id)
    result = copy.deepcopy(profile)
    def packet(value):
        require(type(value) is str and value == value.upper(), 'Uppercase frame hex required')
        return old.open_frame(bytes.fromhex(value))
    def framed(raw):
        return new.frame(raw).hex().upper()
    app = bytes.fromhex(profile['SoeCorrectionHex'])
    require(len(app) == 62 and struct.unpack_from('<HIII', app) == (5, 0x80ce5e46, profile['OutboundFlags'], 0x71), 'Root correction header/width')
    require(struct.unpack_from('<I', app, 26)[0] == uint(profile['CorrectionCounter']), 'Root correction counter')
    require(type(profile['SoeCorrectionDatagrams']) is list and len(profile['SoeCorrectionDatagrams']) == 2 and
            type(profile['SoeCorrectionAcks']) is list and len(profile['SoeCorrectionAcks']) == 2, 'Two correction fixtures required')
    for i in range(2):
        require(packet(profile['SoeCorrectionDatagrams'][i]) == struct.pack('>HH', 9, i) + app, 'Correction packet mismatch')
        require(packet(profile['SoeCorrectionAcks'][i]) == struct.pack('>HH', 0x15, i), 'Correction ACK mismatch')
    disconnect = struct.pack('>HIH', 5, profile['SoeConnectionId'] ^ int(profile['InvalidDisconnectConnection']), 6)
    require(packet(profile['SoeDisconnectHex']) == disconnect, 'Disconnect mismatch')
    outer = packet(profile['SoeOuterDisconnectHex']); require(outer[:2] == b'\0\3', 'Outer Multi required')
    blocks = []; cursor = 2
    while cursor < len(outer):
        length = outer[cursor]; cursor += 1
        require(0 < length < 255 and cursor + length <= len(outer), 'Bounded short Multi child required')
        blocks.append(outer[cursor:cursor + length]); cursor += length
    prefix = bytes.fromhex(profile['SoeOuterPrefixApplicationHex'])
    expected_prefix = bytearray(app)
    struct.pack_into('<I', expected_prefix, 26, (profile['CorrectionCounter'] + 1) & 0xffffffff)
    require(prefix == expected_prefix, 'Outer prefix application mismatch')
    require(blocks == [struct.pack('>HH', 9, 2) + prefix, disconnect, struct.pack('>HH', 9, 3) + prefix], 'Exact frozen outer children required')
    require(packet(profile['SoeOuterAckHex']) == struct.pack('>HH', 0x15, 2), 'Outer ACK mismatch')
    result.update(SoeConnectionId=connection_id, SoeSeed=seed,
                  SoeCorrectionDatagrams=[framed(struct.pack('>HH', 9, i) + app) for i in range(2)],
                  SoeCorrectionAcks=[framed(struct.pack('>HH', 0x15, i)) for i in range(2)],
                  SoeDisconnectHex=framed(struct.pack('>HIH', 5, connection_id ^ int(profile['InvalidDisconnectConnection']), 6)),
                  SoeOuterAckHex=framed(struct.pack('>HH', 0x15, 2)))
    blocks[1] = struct.pack('>HIH', 5, connection_id ^ int(profile['InvalidDisconnectConnection']), 6)
    result['SoeOuterDisconnectHex'] = framed(b'\0\3' + b''.join(bytes([len(b)]) + b for b in blocks))
    return result


def expect_motion(profile, samples):
    """Before-native authored Native XYZ/Speed/Quaternion XYZW rows; fixed10ms cadence."""
    wire = admit(profile)
    require(type(samples) is list and len(samples) <= profile['MaximumUpdates'], 'Bounded independent samples')
    oid = struct.unpack_from('<Q', bytes.fromhex(profile['SoeCorrectionHex']), 14)[0]
    previous = 0.; last = 0; pose = None; last_speed = 0.; queued = []; cadence = []
    counter = profile['OutboundCounterBase']
    for i, sample in enumerate(samples):
        values = sample['Quaternion'] + sample['Native'] + [sample['Speed']]
        require(len(sample['Quaternion']) == 4 and len(sample['Native']) == 3 and
                all(type(v) in (int, float) and math.isfinite(v) for v in values), 'Finite authored motion required')
        q = struct.unpack('<4f', struct.pack('<4f', *sample['Quaternion']))
        xyz = struct.unpack('<3f', struct.pack('<3f', *sample['Native']))
        speed = struct.unpack('<f', struct.pack('<f', sample['Speed']))[0]
        elapsed = (i + 1) * 10000; current = (q, xyz)
        due = (previous > 0 and speed == 0) or ((pose is None or current != pose or speed != last_speed) and elapsed-last >= 100000) or (speed == 0 and pose is not None and elapsed-last >= 1000000)
        timestamp = (profile['OutboundTimestampBase'] + elapsed // 1000) & 0xffffffff
        if due:
            counter = (counter + 1) & 0xffffffff; last = elapsed; pose = current; last_speed = speed
            app = struct.pack('<HIIIQII', 5, 0x80ce5e46, profile['OutboundFlags'], 0x71, oid, timestamp, counter) + struct.pack('<8f', *q, xyz[0], xyz[2], xyz[1], speed)
            seq = len(queued)
            queued.append(dict(AtStep=i, Sequence=seq, ApplicationHex=app.hex().upper(),
                               Hex=wire.frame(struct.pack('>HH', 9, seq)+app).hex().upper(),
                               AckHex=wire.frame(struct.pack('>HH', 0x15, seq)).hex().upper()))
        cadence.append(dict(Index=i, Status='Queued' if due else 'NotDue', ElapsedMicroseconds=elapsed,
                            Samples=i+1, MovementCounter=counter, Timestamp=timestamp, QueueCount=int(due), ActualIssuedLocalRow=True, Sent=False))
        previous = speed
    count = len(queued)
    return dict(Cadence=cadence, Frames=queued, Completion=dict(Samples=len(samples), Queued=count, Drained=count,
                NextSequence=count, SessionFaulted=False, TransportState='Established', QueueCount=0, QueueBytes=0,
                PendingMessages=0, RetainedPackets=0, CleanDrain=True))


def records(text, key):
    return [json.loads(line.split('=', 1)[1]) for line in text.splitlines() if line.startswith(key+'=')]


def verify_runs(segments, profiles, expectations):
    """Each segment is one full child log; expectations MUST precede native execution.

    expectations entries: {Transport: expect_motion(...), Runtime: exact independently
    authored offline_completion_summary dict}. Healthy roots on runtime faults supported.
    """
    require(0 < len(segments) == len(profiles) == len(expectations) <= 32, 'One bounded segment/profile/oracle per run')
    require(len({p['SoeConnectionId'] for p in profiles}) == len(profiles) and
            len({p['SoeSeed'] for p in profiles}) == len(profiles), 'Distinct synthetic IDs and seeds per run')
    receipts = []
    for generation, (text, p, expected) in enumerate(zip(segments, profiles, expectations), 1):
        h = handshakes(p); bootstrap = records(text, 'soe_root_transport_ready')
        require(bootstrap == [dict(h, Established=True, Synthetic=True, SocketCreated=False, Sent=False, OriginalSenderParity=False)], 'Exact actual bootstrap required')
        t = expected['Transport']; runtime = expected['Runtime']
        actual_cadence = records(text, 'offline_root_cadence')
        require(actual_cadence == t['Cadence'], 'Independent cadence mismatch')
        for row in actual_cadence:
            require(all(type(row[k]) is int for k in ('Index', 'ElapsedMicroseconds', 'Samples', 'MovementCounter', 'Timestamp', 'QueueCount')) and
                    row['ActualIssuedLocalRow'] is True and row['Sent'] is False, 'Strict cadence types')
        raws = records(text, 'point_outbound_root'); frames = records(text, 'soe_root_datagram')
        require(len(raws) == len(frames) == len(t['Frames']), 'Exact root count')
        for raw, frame, want in zip(raws, frames, t['Frames']):
            require(all(type(frame[k]) is int for k in ('AtStep', 'ReadAtStep', 'Sequence', 'AckReleased', 'PendingMessages', 'RetainedPackets')), 'Strict frame types')
            require(raw['Hex'] == want['ApplicationHex'] and raw['AtStep'] == want['AtStep'] and raw['ReadAtStep'] == want['AtStep'], 'Independent application mismatch')
            require(all(frame[k] == want[k] for k in ('AtStep', 'Sequence', 'Hex', 'AckHex')) and
                    frame['ReadAtStep'] == want['AtStep'] and frame['AckReleased'] == 1 and
                    frame['ReliableCopyAccepted'] is True and frame['PendingMessages'] == frame['RetainedPackets'] == 0 and
                    frame['SocketCreated'] is False and frame['Sent'] is False and frame['OriginalSenderParity'] is False, 'Protected root/ACK/buffers mismatch')
        actual_roots = records(text, 'offline_root_completion_summary')
        require(actual_roots == [t['Completion']], 'Actual root completion mismatch')
        require(all(type(actual_roots[0][k]) is int for k in ('Samples', 'Queued', 'Drained', 'NextSequence', 'QueueCount', 'QueueBytes', 'PendingMessages', 'RetainedPackets')) and
                actual_roots[0]['SessionFaulted'] is False and actual_roots[0]['CleanDrain'] is True, 'Strict healthy root report types')
        require(records(text, 'offline_completion_summary') == [runtime], 'Exact runtime completion prefix mismatch')
        require(runtime['CompletedUpdates'] == t['Completion']['Samples'], 'One sample per completed runtime update')
        require((runtime['Reason'] == 'Faulted' and runtime['Faulted'] is True and
                 runtime['AttemptedUpdates'] == runtime['CompletedUpdates'] + 1) or
                (runtime['Reason'] in ('BudgetCompleted', 'FinishRequested') and runtime['Faulted'] is False and
                 runtime['AttemptedUpdates'] == runtime['CompletedUpdates']), 'Strict runtime success/fault prefix')
        cleanup = records(text, 'soe_root_transport_cleanup')
        require(len(cleanup) == 1 and cleanup[0]['Closed'] is True and
                cleanup[0]['PendingMessages'] == cleanup[0]['RetainedPackets'] == 0 and cleanup[0]['SocketCreated'] is False, 'Actual closed empty transport cleanup')
        receipts.append(dict(Generation=generation, Samples=len(t['Cadence']), ProtectedRoots=len(frames), RuntimeReason=runtime['Reason']))
    return dict(Passed=True, Synthetic=True, Runs=receipts, SocketCreated=False, NativeOutputUsedForExpectations=False)


def verify_transport_only(segments, profiles, expected_runtime, expected_queue_steps):
    """Transport boundary only; actual application bytes are borrowed serialization input.

    Runtime dictionaries and zero-based queue-step lists MUST be authored pre-native.
    A separate independent movement/heading oracle must verify those applications.
    This function does not turn native movement into an expected movement fixture.
    """
    require(0 < len(segments) == len(profiles) == len(expected_runtime) == len(expected_queue_steps) <= 32,
            'One bounded runtime/queue-step oracle per segment')
    expectations = []
    for text, p, runtime, steps in zip(segments, profiles, expected_runtime, expected_queue_steps):
        wire = admit(p); n = runtime['CompletedUpdates']
        require(type(n) is int and 0 <= n <= p['MaximumUpdates'] and type(steps) is list and
                all(type(i) is int and 0 <= i < n for i in steps) and steps == sorted(set(steps)), 'Bounded exact queue-step oracle')
        raws = records(text, 'point_outbound_root')
        require(len(raws) == len(steps), 'Independent queued-root count mismatch')
        oid = struct.unpack_from('<Q', bytes.fromhex(p['SoeCorrectionHex']), 14)[0]
        frames = []; cadence = []; counter = p['OutboundCounterBase']; sequence = 0
        for i in range(n):
            due = i in steps; timestamp = (p['OutboundTimestampBase'] + (i+1)*10) & 0xffffffff
            if due:
                counter = (counter + 1) & 0xffffffff
                app = bytes.fromhex(raws[sequence]['Hex'])
                require(len(app) == 62 and struct.unpack_from('<HIIIQII', app) ==
                        (5, 0x80ce5e46, p['OutboundFlags'], 0x71, oid, timestamp, counter) and
                        all(math.isfinite(v) for v in struct.unpack_from('<8f', app, 30)), 'Bounded canonical actual application header')
                frames.append(dict(AtStep=i, Sequence=sequence, ApplicationHex=app.hex().upper(),
                                   Hex=wire.frame(struct.pack('>HH', 9, sequence)+app).hex().upper(),
                                   AckHex=wire.frame(struct.pack('>HH', 0x15, sequence)).hex().upper()))
                sequence += 1
            cadence.append(dict(Index=i, Status='Queued' if due else 'NotDue', ElapsedMicroseconds=(i+1)*10000,
                                Samples=i+1, MovementCounter=counter, Timestamp=timestamp,
                                QueueCount=int(due), ActualIssuedLocalRow=True, Sent=False))
        t = dict(Cadence=cadence, Frames=frames, Completion=dict(Samples=n, Queued=len(steps), Drained=len(steps),
                 NextSequence=len(steps), SessionFaulted=False, TransportState='Established', QueueCount=0,
                 QueueBytes=0, PendingMessages=0, RetainedPackets=0, CleanDrain=True))
        expectations.append(dict(Transport=t, Runtime=runtime))
    result = verify_runs(segments, profiles, expectations)
    result.pop('NativeOutputUsedForExpectations')
    result.update(NativeApplicationBorrowedSerializationInput=True, ExpectedMovementGenerated=False,
                  IndependentExpectedQueueSteps=True, RequiresSeparateMovementOracle=True)
    return result
