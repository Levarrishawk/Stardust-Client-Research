"""Independent synthetic TREE writer and metadata comparison; no original payload export."""
import argparse
import contextlib
import hashlib
import json
from pathlib import Path
import struct
import sys
import re

REPO = Path(__file__).resolve().parents[3]


def crc(data):
    value = 0xffffffff
    for byte in data:
        value ^= byte << 24
        for _ in range(8):
            value = ((value << 1) ^ (0x04c11db7 if value & 0x80000000 else 0)) & 0xffffffff
    return value ^ 0xffffffff


def archive(path, entries):
    payload = bytearray()
    records = bytearray()
    names = bytearray()
    for name, value, kind in entries:
        raw = name.encode('ascii')
        position = 36 + len(payload)
        payload.extend(value)
        records.extend(struct.pack('<6I', crc(raw), len(value), position, kind, len(value), len(names)))
        names.extend(raw + b'\0')
    header = b'EERT5000' + struct.pack('<7I', len(entries), 36 + len(payload), 0,
                                         len(records), 0, len(names), len(names))
    path.write_bytes(header + payload + records + names + bytes(16 * len(entries)))


def metadata(path, value, priority):
    return dict(LogicalPath=path, Kind=1, Priority=priority, ByteCount=len(value),
                Sha256=hashlib.sha256(value).hexdigest().upper())


def generate(out, asset_root=None, config=None):
    if bool(asset_root) != bool(config):
        raise ValueError('Both explicit original inputs required')
    if asset_root:
        for raw in (asset_root, config):
            path = Path(raw)
            remainder = raw[2:] if len(raw) >= 2 and raw[1] == ':' else raw
            if (not path.is_absolute() or raw != raw.strip() or len(raw) > 4096
                    or raw.startswith(('\\\\', '//')) or ':' in remainder
                    or any(ord(c) < 32 or ord(c) == 127 for c in raw)
                    or any(p in ('.', '..') for p in raw.replace('\\', '/').split('/'))):
                raise ValueError('Explicit original inputs must be local absolute paths')
            for ancestor in (path, *path.parents):
                if ancestor.is_symlink() or getattr(ancestor.stat(), 'st_file_attributes', 0) & 0x400:
                    raise ValueError('Reparse original input rejected')
        if not Path(asset_root).is_dir() or not Path(config).is_file() or Path(config).stat().st_size > 65536:
            raise ValueError('Original root/config admission failed')
        for line in Path(config).read_bytes().decode('utf-8-sig').splitlines():
            if not line.lstrip().startswith('searchTree_00_'):
                continue
            match = re.fullmatch(r'\s*searchTree_00_([0-9]+)\s*=\s*([^\s;]+)\s*(?:;.*)?', line)
            if not match or any(c in match[2] for c in '/\\:'):
                raise ValueError('Original mount admission failed')
            mount = Path(asset_root) / match[2]
            if mount.is_symlink() or getattr(mount.stat(), 'st_file_attributes', 0) & 0x400:
                raise ValueError('Reparse original mount rejected')
    out = out.resolve()
    out.relative_to((REPO / 'local-private').resolve())
    if out.exists():
        raise ValueError('Fresh private fixture directory required')
    out.mkdir(parents=True)
    archive(out / 'low.tre', [('object/test.iff', b'low', 0), ('masked.iff', b'fallback', 0)])
    archive(out / 'high.tre', [('object/test.iff', b'high', 0), ('masked.iff', b'', 0)])
    archive(out / 'broken.tre', [('broken.iff', b'invalid-zlib', 2)])
    cfg = out / 'mounts.cfg'
    cfg.write_bytes(b'searchTree_00_1=low.tre\nsearchTree_00_10=high.tre\n')
    (out / 'broken.cfg').write_bytes(b'searchTree_00_1=low.tre\nsearchTree_00_10=broken.tre\n')
    (out / 'large.cfg').write_bytes(b' ' * 65537)
    (out / 'utf8.cfg').write_bytes(b'\xff')
    (out / 'empty.cfg').write_bytes(b'includeFile=unexpanded.cfg\n')
    (out / 'boundary.cfg').write_bytes(cfg.read_bytes() + b' ' * (65536 - cfg.stat().st_size))
    (out / 'bad.tre').write_bytes(b'bad header')
    (out / 'badarchive.cfg').write_bytes(cfg.read_bytes() + b'searchTree_00_0=bad.tre\n')
    for name, text in [('duplicate.cfg', 'searchTree_00_1=low.tre\nsearchTree_00_1=high.tre\n'),
                       ('remote.cfg', 'searchTree_00_1=https://host/remote.tre\n'),
                       ('malformed.cfg', 'searchTree_00_broken=low.tre\n')]:
        (out / name).write_text(text, encoding='utf-8')
    result = dict(Root=str(out), Config=str(cfg), ConfigurationBytes=cfg.stat().st_size,
                  ConfigurationSha256=hashlib.sha256(cfg.read_bytes()).hexdigest().upper(),
                  Expected=[metadata('object/test.iff', b'high', 10),
                            dict(LogicalPath='masked.iff', Kind=2, Priority=10, ByteCount=None, Sha256=None),
                            dict(LogicalPath='missing.iff', Kind=0, Priority=None, ByteCount=None, Sha256=None)])
    if asset_root:
        sys.path.insert(0, str(REPO / 'tools/python'))
        from verify_textured_mesh_bundle import Index
        from pst_reference_audit import configured_archives
        root, config = Path(asset_root), Path(config)
        mounts = configured_archives(root, config)
        logical = 'object/creature/player/shared_human_male.iff'
        with contextlib.ExitStack() as stack:
            indexes = [(priority, Index(stack.enter_context(path.open('rb')))) for priority, path in mounts]
            for priority, index in indexes:
                value = index.lookup(logical)
                if value is not None:
                    expected = metadata(logical, value, priority)
                    break
            else:
                raise ValueError('Explicit original object absent')
        assert expected['Sha256'] == 'E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
        result['Original'] = dict(Root=str(root), Config=str(config), MountCount=len(mounts), ConfigurationBytes=config.stat().st_size,
                                ConfigurationSha256=hashlib.sha256(config.read_bytes()).hexdigest().upper(), Expected=[expected])
    (out / 'fixtures.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print('PASS independent fixtures generated before native')


def verify(out):
    fixture = json.loads((out / 'fixtures.json').read_text())
    native = json.loads((out / 'receipt.json').read_text())
    for key, expected in [('Synthetic', fixture), ('Original', fixture.get('Original'))]:
        if expected is None:
            assert native[key] is None
            continue
        actual = native[key]
        assert actual['ConfigurationSha256'] == expected['ConfigurationSha256']
        assert actual['ConfigurationBytes'] == expected['ConfigurationBytes']
        assert actual['MountCount'] == expected.get('MountCount', 2)
        assert actual['Resources'] == expected['Expected']
    assert native['NegativeCases'] >= 51 and native['CleanupChecks'] == 4
    print('PASS independent metadata/hash agreement and negative/cleanup suite')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['generate', 'verify'])
    parser.add_argument('out', type=Path)
    parser.add_argument('--asset-root')
    parser.add_argument('--config')
    args = parser.parse_args()
    if args.mode == 'generate':
        generate(args.out, args.asset_root, args.config)
    else:
        verify(args.out)
