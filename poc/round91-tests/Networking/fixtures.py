"""Independent synthetic advertisements and protected login/zone byte oracle; no DNS."""
import argparse
import importlib.util
import json
import runpy
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
P = struct.pack
OID = 0x1122334455667788
LOGIN_SEED, ZONE_SEED = 0xA1B2C3D4, 0x0BADCAFE


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, ROOT/path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def header(count, opcode):
    return P('<HI', count, opcode)


def text(value):
    raw = value.encode('ascii')
    return P('<H', len(raw))+raw


def generate(out):
    guard = runpy.run_path(str(ROOT/'tools/python/verify_creature_controller_speed_inputs.py'))['fresh_private_path']
    out = guard(out, ROOT)
    sys.path.insert(0, str(ROOT/'tools'))
    assets = load('poc/round88-tests/Assets/peer.py', 'assets91')
    installation = out.with_name(out.stem+'-installation')
    assets.generate(installation)
    source = json.loads((installation/'fixtures.json').read_text())
    soe = load('poc/round14-tests/Networking/peer.py', 'soe91')
    def wire(seed, plain):
        soe.SEED = seed
        result = soe.frame(plain)
        assert soe.open_frame(result) == plain
        return result.hex().upper()
    def data(seed, seq, body):
        return wire(seed, P('>HH', 9, seq)+body)
    def response(identity, seed):
        return (b'\0\2'+P('>II', identity, seed)+bytes([2, 1, 4])+P('>I', 496)).hex().upper()
    credential = bytes.fromhex('0340302010')
    token = header(4, 0xAAB296C6)+P('<I', len(credential))+credential+P('<I', 99)+text('synthetic')
    auth = header(3, 0xD5899226)+P('<II', 0xFE, len(credential))+credential+text('20260817-SD:01')
    selection = header(2, 0xB5098D76)+P('<Q', OID)
    cases = []
    def add(name, expected=False, **changes):
        c = dict(Name=name, ExpectedAccepted=expected, ApprovedLoginHost='login.invalid', ApprovedLoginAddress=0x7F000001,
                 ApprovedLoginPort=44452, ApprovedZoneHost='zone.invalid', ApprovedZoneAddress=0x7F000002,
                 ApprovedZonePort=44453, ApprovedGalaxy=7, ReceivedHost='zone.invalid', ReceivedZonePort=44453,
                 ReceivedPingPort=44454, CharacterGalaxy=7, ClusterGalaxy=7, StatusGalaxy=7,
                 CharacterCount=1, ClusterCount=1, StatusCount=1)
        c.update(changes)
        cluster = P('<I', c['ClusterGalaxy'])+text('Authored')+P('<II', 0xFFFF8F80, 8)
        status = P('<I', c['StatusGalaxy'])+text(c['ReceivedHost'])+P('<HHIIIII', c['ReceivedZonePort'], c['ReceivedPingPort'], 100, 3250, 8, 0xFFFF8F80, 2)+b'\0'
        character = P('<I', 1)+b'A\0'+P('<IQII', soe.checksum('object/root.iff'), OID, c['CharacterGalaxy'], 1)
        bodies = [token, header(2, 0xC11C63B9)+P('<I', c['ClusterCount'])+cluster*c['ClusterCount'],
                  header(3, 0x3436AEB6)+P('<I', c['StatusCount'])+status*c['StatusCount'],
                  header(2, 0x65EA4574)+P('<I', c['CharacterCount'])+character*c['CharacterCount']]
        c['LoginReplies'] = [data(LOGIN_SEED, n, body) for n, body in enumerate(bodies)]
        c['LoginApplicationHex'] = [body.hex().upper() for body in bodies]
        cases.append(c)
    add('canonical', True)
    add('canonical-case-fold', True, ReceivedHost='ZONE.INVALID')
    add('literal', True, ApprovedZoneHost='127.0.0.2', ReceivedHost='127.0.0.2')
    add('literal-does-not-substitute-hostname', ReceivedHost='127.0.0.2')
    add('hostname-mismatch', ReceivedHost='other.invalid')
    add('literal-mismatch', ApprovedZoneHost='127.0.0.2', ReceivedHost='127.0.0.3')
    add('zone-port-mismatch', ReceivedZonePort=44455)
    add('ping-is-not-zone', ReceivedZonePort=44454, ReceivedPingPort=44453)
    add('ping-differs', True, ReceivedPingPort=1)
    add('approved-galaxy-mismatch', ApprovedGalaxy=8)
    add('character-galaxy-mismatch', CharacterGalaxy=8)
    add('status-galaxy-mismatch', StatusGalaxy=8)
    add('cluster-galaxy-mismatch', ClusterGalaxy=8)
    for field in ('Character', 'Cluster', 'Status'):
        add('duplicate-'+field.lower(), **{field+'Count': 2})
        add('missing-'+field.lower(), **{field+'Count': 0})
    add('approved-zone-address-mismatch', ApprovedZoneAddress=0x7F000003)
    add('approved-zone-port-mismatch', ApprovedZonePort=44455)
    add('approved-login-host-mismatch', ApprovedLoginHost='other.invalid')
    add('approved-login-address-mismatch', ApprovedLoginAddress=0x7F000003)
    add('approved-login-port-mismatch', ApprovedLoginPort=44455)
    add('invalid-zone-mapping-uri', ApprovedZoneHost='https://zone.invalid')
    add('invalid-zone-mapping-space', ApprovedZoneHost='zone invalid')
    add('invalid-zone-mapping-zero-port', ApprovedZonePort=0)
    add('invalid-zone-mapping-zero-address', ApprovedZoneAddress=0)
    add('invalid-literal-binding', ApprovedZoneHost='127.0.0.3', ReceivedHost='127.0.0.3')
    result = dict(Synthetic=True, Profile=source['Profile'], ExpectedSources=source['Expected'], Cases=cases,
                  LoginResponse=response(0x12345678, LOGIN_SEED), ZoneResponse=response(0x87654321, ZONE_SEED),
                  ZoneAcks=[wire(ZONE_SEED, P('>HH', 0x15, n)) for n in (0, 1)],
                  Permissions=data(ZONE_SEED, 0, header(4, 0xE00730E5)+bytes([1, 1, 0, 1])),
                  ExpectedAuthWire=data(ZONE_SEED, 0, auth), ExpectedSelectWire=data(ZONE_SEED, 1, selection),
                  ExpectedZoneRequest=(P('>HIII', 1, 2, 0x87654321, 496)).hex().upper())
    out.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    result = generate(args.out)
    print(json.dumps(dict(Generated=True, Synthetic=True, Cases=len(result['Cases']), Path=str(args.out))))


if __name__ == '__main__':
    main()
