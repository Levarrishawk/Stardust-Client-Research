"""Independent bounded opaque DDS header oracle and Pillow/raw all-mip hashes; no asset export."""
import argparse
import contextlib
import csv
import hashlib
import io
import json
import struct
from pathlib import Path
from PIL import Image
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index

SAMPLE = 'texture/nebula_dant_back.dds'
SAMPLE_SHA = 'A2627501B233EE3A795BEB5B42446BF2F9B2A60854AD3781366720B5631FDC9D'
CONFIG_SHA = '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
INVENTORY_SHA = 'DCEDE8198E48FF68CCD65FDF2C618E50703A13F1F08BF97CA34A1D0E5665D471'
def digest(data): return hashlib.sha256(data).hexdigest().upper()
def need(ok):
    if not ok: raise ValueError('Outside bounded opaque DDS contract')
def inspect(data, limits=None):
    limits = limits or {}
    maximum_dimension = limits.get('MaximumDimension', 8192)
    maximum_input = limits.get('MaximumInputBytes', 67108864)
    maximum_output = limits.get('MaximumDecodedBytes', 134217728)
    need(128 <= len(data) <= maximum_input and data[:4] == b'DDS ')
    u = lambda at: struct.unpack_from('<I', data, at)[0]
    need(u(4) == 124 and u(76) == 32)
    flags, h, w = u(8), u(12), u(16)
    need(flags & 0x1007 == 0x1007 and flags & ~0xA100F == 0 and flags & 0x80008 != 0x80008)
    need(u(108) & 0x1000 and u(24) <= 1 and u(112) == 0 and 0 < min(w, h) and max(w, h) <= maximum_dimension)
    fields = struct.unpack_from('<7I', data, 80)
    if fields[0] == 4 and fields[1] == 0x30315844:
        need(len(data) >= 148 and fields[2:] == (0, 0, 0, 0, 0))
        dxgi, dimension, misc, array, alpha = struct.unpack_from('<5I', data, 128)
        need(dxgi in (88, 93) and (dimension, misc, array) == (3, 0, 1) and alpha <= 4)
        bpp, start, fmt, space = 4, 148, 'Bgrx32', 'Srgb' if dxgi == 93 else 'Linear'
    else:
        need(fields[0] == 0x40 and fields[1] == 0 and fields[2] in (24, 32) and fields[3:] == (0xFF0000, 0xFF00, 0xFF, 0))
        bpp, start = fields[2] // 8, 128
        dxgi, alpha, fmt, space = None, 0, 'Bgr24' if bpp == 3 else 'Bgrx32', 'Unspecified'
    need(not flags & 8 or u(20) == w * bpp)
    need(not flags & 0x80000 or u(20) == w * h * bpp)
    count = max(1, u(28)); need(count <= max(w, h).bit_length())
    extents = [(max(1, w >> n), max(1, h >> n)) for n in range(count)]
    need(sum(a * b * 4 for a, b in extents) <= maximum_output and len(data) == start + sum(a * b * bpp for a, b in extents))
    records = []
    for width, height in extents:
        raw = data[start:start + width * height * bpp]; start += len(raw)
        # Independent Pillow raw decoder: BGRX ignores the unused byte. Cross-check a separate byte-level oracle.
        rgba = Image.frombytes('RGB', (width, height), raw, 'raw', 'BGR' if bpp == 3 else 'BGRX').convert('RGBA').tobytes()
        manual = bytes(c for n in range(0, len(raw), bpp) for c in (raw[n + 2], raw[n + 1], raw[n], 255))
        need(rgba == manual)
        records.append(dict(Width=width, Height=height, sha256=digest(rgba)))
    return dict(format=fmt, dxgi=dxgi, colorSpace=space, alphaMode=('Unknown', 'Straight', 'Premultiplied', 'Opaque', 'Custom')[alpha], mips=records)

def audit(root, configuration, output, prior_inventory=None):
    if prior_inventory is not None:
        need(digest(prior_inventory.read_bytes()) == INVENTORY_SHA)
        with prior_inventory.open(newline='') as stream: rows = list(csv.DictReader(stream))
        need(len(rows) == 29906 and sum(r['format'] == 'RGB24:00ff0000/0000ff00/000000ff/00000000:f40' for r in rows) == 113)
    need(digest(configuration.read_bytes()) == CONFIG_SHA)
    groups = {}
    for line in output.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('opaque_'):
            key, value = line.split('=', 1); groups.setdefault(key, []).append(json.loads(value))
    fixtures = groups['opaque_fixture']; need(len(fixtures) == 36)
    prefixes = 0
    representatives = {}
    for row in fixtures:
        data = bytes.fromhex(row['hex']); expected = inspect(data)
        need({k: row[k] for k in expected} == expected)
        kind = (expected['format'], expected['dxgi'])
        representatives.setdefault(kind, data)
    need(set(representatives) == {('Bgr24', None), ('Bgrx32', None), ('Bgrx32', 88), ('Bgrx32', 93)})
    for data in representatives.values():
        for length in range(len(data)):
            try: inspect(data[:length])
            except ValueError: prefixes += 1
            else: raise AssertionError('Truncated opaque fixture accepted by independent oracle')
    bad = groups['opaque_bad']
    for row in bad:
        try: inspect(bytes.fromhex(row['hex']), row['limits'])
        except ValueError: pass
        else: raise AssertionError('Native rejected fixture accepted by independent oracle')
    validation, = groups['opaque_validation']
    need(validation == dict(cases=36, rejects=781, prefixes=657, owned=True, readonlyMips=True, enumStable=True))
    need(prefixes == 657 and len(bad) == 124)
    original, = groups['opaque_original']
    with contextlib.ExitStack() as stack:
        # Read existing indices only; fetch one effective logical member, not a corpus rescan.
        winner = None
        for priority, path in archives(root, configuration):
            index = Index(stack.enter_context(path.open('rb')))
            data = index.lookup(SAMPLE)
            if data is not None:
                winner = (priority, path.name, data); break
        need(winner is not None)
        priority, archive, data = winner
        need(priority == 35 and archive == 'mtg_patch_006_texture_02.tre' and len(data) == 16511 and digest(data) == SAMPLE_SHA)
        need(struct.unpack_from('<I', data, 8)[0] == 0xA1007 and struct.unpack_from('<I', data, 20)[0] == 12288)
        expected = inspect(data)
        need(original == dict(path=SAMPLE, priority=priority, bytes=len(data), sha256=SAMPLE_SHA, configurationSha256=CONFIG_SHA, format='Bgr24', mips=expected['mips']))
        # Rebuild each one-mip DDS envelope and compare Pillow's DDS plugin too, independently of the raw decoder.
        offset = 128
        for level, mip in enumerate(expected['mips']):
            w, h = mip['Width'], mip['Height']; size = w * h * 3
            header = bytearray(data[:128]); struct.pack_into('<II', header, 12, h, w)
            struct.pack_into('<I', header, 8, 0x100F); struct.pack_into('<I', header, 20, w * 3); struct.pack_into('<I', header, 28, 1)
            with Image.open(io.BytesIO(bytes(header) + data[offset:offset + size])) as image:
                need(digest(image.convert('RGBA').tobytes()) == mip['sha256'])
            offset += size
        need(offset == len(data))
    cleanup, = groups['opaque_cleanup']; need(cleanup == dict(sourceMutationSafe=True, archiveDisposed=True, mips=7))
    need('PASS: BC7' in output.read_text() and 'PASS: owned typed BC6H' in output.read_text())
    return dict(synthetic_fixtures=36, synthetic_mips=72, original_mips=7, original_priority=35, original_archive=archive, original_bytes=16511,
                original_sha256=SAMPLE_SHA, original_flags='0x000A1007', top_linear_bytes=12288, negatives=781, explicit_bad_fixtures=124,
                truncated_prefixes=657, pillow_and_manual_pixels=True, source_identity=True, native_ownership_cleanup=True,
                prior_inventory_sha256=INVENTORY_SHA, prior_inventory_rows=29906, prior_inventory_bgr24=113, prior_inventory_verified=prior_inventory is not None, fresh_corpus_census=False)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('root', 'configuration', 'native_output'): parser.add_argument(name, type=Path)
    parser.add_argument('--prior-inventory', type=Path, help='Optional existing ignored CSV: verify pin/counts without a corpus rescan')
    args = parser.parse_args(); print(json.dumps(audit(args.root, args.configuration, args.native_output, args.prior_inventory), indent=2))
