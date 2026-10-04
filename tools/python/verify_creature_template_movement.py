"""Static-loader-pinned, one-chain serialized movement research; private evidence only."""
import argparse
import contextlib
import hashlib
import json
import math
import runpy
import struct
import sys
from pathlib import Path

EXE_SHA = 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'
CONFIG_SHA = '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
ROOT_PATH = 'object/creature/player/shared_human_male.iff'
# Compact source identities, not extracted asset payloads.
SOURCE_PINS = [
    (ROOT_PATH, 39, 1974, 'E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'),
    ('object/creature/player/base/shared_base_player.iff', 39, 2238,
     '3063381CCAE043B41A279D888D4450B16D96D4F75187ED6EE1621067B8ED7ADB'),
    ('object/creature/base/shared_base_creature.iff', 51, 2027,
     '05F44EF9C42F0DF9C22091E57CCAEEB462BF5CB671D70E3A3803154A1583E3F5'),
    ('object/tangible/base/shared_tangible_base.iff', 53, 1320,
     '2C15F794282685FBF47274E379222E6FC22887741851693B2BB9BCC495B6927E'),
    ('object/object/base/shared_base_object.iff', 53, 731,
     'D4E7366A0ACBA837C4E5B9F7B03DB447166B8EF380FB2D09C9C4270346A83501'),
]
SLOTS = ['speed[0]', 'speed[1]', 'slopeModAngle', 'slopeModPercent', 'waterModPercent']
ANCHORS = [
    (0x13FA228, 'push', '1'), (0x13FA232, 'call', '0xa94f00'),
    (0x13FA237, 'movsx', 'edi, byte ptr [ebp + 0xb]'),
    (0x13FA23B, 'push', '1'), (0x13FA243, 'call', '0xa94f00'),
    (0x13FA24E, 'mov', 'byte ptr [esi + 0x14], dl'),
    (0x13FA251, 'ja', '0x13fa454'),
    (0x13FA257, 'jmp', 'dword ptr [edi*4 + 0x13fa468]'),
    (0x13FA25E, 'push', '4'), (0x13FA266, 'call', '0xa94f00'),
    (0x13FA272, 'fld', 'dword ptr [ebp + 8]'),
    (0x13FA276, 'fstp', 'dword ptr [esi + 4]'),
    (0x13FA279, 'mov', 'dword ptr [esi + 8], 1'),
    (0x13FA280, 'mov', 'byte ptr [esi + 0x10], 1'),
    (0x13FA2DD, 'push', '4'),
    (0x13FA2E5, 'mov', 'dword ptr [esi + 8], 2'),
    (0x13FA2EC, 'mov', 'dword ptr [esi + 0xc], edi'),
    (0x13FA2EF, 'mov', 'byte ptr [esi + 0x10], 1'),
    (0x13FA2FA, 'call', '0xa94f00'),
    (0x13FA304, 'jle', '0x13fa454'),
    (0x13FA310, 'push', '4'), (0x13FA318, 'call', '0xa94f00'),
    (0x13FA327, 'call', 'dword ptr [edx + 0x10]'),
    (0x13FA332, 'call', 'dword ptr [edx]'),
    (0x13FA386, 'push', '4'), (0x13FA38E, 'call', '0xa94f00'),
    (0x13FA393, 'push', '4'), (0x13FA39B, 'call', '0xa94f00'),
    (0x13FA3A9, 'mov', 'dword ptr [esi + 8], 3'),
    (0x13FA3CA, 'mov', 'byte ptr [esi + 0x10], 1'),
    (0x13FA449, 'mov', 'dword ptr [esi + 8], 0'),
    (0x13FA450, 'mov', 'byte ptr [esi + 0x10], 0'),
    (0xA94F2F, 'rep movsd', 'dword ptr es:[edi], dword ptr [esi]'),
    (0xA94F36, 'rep movsb', 'byte ptr es:[edi], byte ptr [esi]'),
    (0xA94F3F, 'mov', 'dword ptr [eax + 8], ecx'),
    (0x11B1423, 'cmp', 'eax, 0x53434f54'),
    (0x11B142D, 'call', '0x12286d0'),
    (0x11B145C, 'cmp', 'eax, 0x44455256'),
    (0x11B14D2, 'call', '0xb286e0'),
    (0x11B14F9, 'mov', 'dword ptr [esi + 0x18], edi'),
    (0x11B156A, 'push', '4'), (0x11B1572, 'call', '0xa94f00'),
    (0x11B1584, 'jle', '0x11b1a7c'),
    (0x11B16E1, 'push', '4'), (0x11B16E9, 'call', '0xa94f00'),
    (0x11B1703, 'jge', '0x11b1a6a'),
    (0x11B1714, 'cmp', 'esi, 2'),
    (0x11B171E, 'jge', '0x11b1a6a'),
    (0x11B1743, 'call', '0x13fa200'),
    (0x11B174E, 'call', '0x13fa0b0'),
]


def require(condition, message):
    if not condition:
        raise ValueError(message)


def identity(path, expected, label):
    raw = path.read_bytes()
    require(hashlib.sha256(raw).hexdigest().upper() == expected, label + ' identity mismatch')
    return raw


def private_receipt(path, repo):
    target = path.resolve()
    require(target.is_relative_to((repo / 'local-private').resolve()), 'Receipt must remain under ignored local-private')
    return target


def decode_parameter(raw, offset=0, depth=0):
    """Bounded research inspector; strict limits are not original rejection policy."""
    require(depth < 16 and len(raw) <= 65536, 'Parameter research budget')
    start = offset

    def take(size):
        nonlocal offset
        require(offset + size <= len(raw), 'Truncated parameter')
        value = raw[offset:offset + size]
        offset += size
        return value

    mode = struct.unpack('<b', take(1))[0]
    relative = take(1)[0]
    require(mode in (0, 1, 2, 3), 'Unsupported serialized mode in research inspector')
    result = dict(Mode=mode, RelativeByte=relative)
    if mode == 1:
        bits = take(4)
        value = struct.unpack('<f', bits)[0]
        result.update(F32LE=bits.hex(), Value=value if math.isfinite(value) else None,
                      Finite=math.isfinite(value))
    elif mode == 2:
        count = struct.unpack('<i', take(4))[0]
        require(0 <= count <= 64, 'Weighted count research budget')
        entries = []
        for _ in range(count):
            weight = struct.unpack('<i', take(4))[0]
            child, offset = decode_parameter(raw, offset, depth + 1)
            entries.append(dict(Weight=weight, Parameter=child))
        result.update(Count=count, Entries=entries)
    elif mode == 3:
        bits = take(8)
        result['RangeF32LE'] = [bits[:4].hex(), bits[4:].hex()]
    result.update(Bytes=offset - start, RawHex=raw[start:offset].hex())
    return result, offset


def decode_selected(parameters):
    selected = {slot: None for slot in SLOTS}
    metadata = {}
    for name in ['speed', 'slopeModAngle', 'slopeModPercent', 'waterModPercent']:
        if name not in parameters:
            continue
        raw = parameters[name]
        if name == 'speed':
            require(len(raw) >= 4, 'Truncated speed count')
            count = struct.unpack_from('<i', raw)[0]
            require(0 <= count <= 64, 'Speed count research budget')
            offset = 4
            for index in range(count):
                value, offset = decode_parameter(raw, offset)
                if index < 2:
                    selected[f'speed[{index}]'] = value
            metadata[name] = dict(Count=count, Assigned=min(count, 2), Discarded=max(0, count - 2))
        else:
            value, offset = decode_parameter(raw)
            selected[name] = value
        require(offset == len(raw), 'Trailing selected parameter bytes')
    return selected, metadata


def inspect_document(raw):
    # Reuse the accepted IFF walker and shared-template hierarchy/PCNT parser.
    from verify_static_mesh_slice import nodes
    from verify_creature_template_profile import parse
    require(len(raw) <= 1048576, 'Template research budget')
    try:
        roots = nodes(raw)
        require(len(roots) == 1, 'Expected one template root')
        parent, layers = parse(roots[0])
    except (AssertionError, IndexError, struct.error) as error:
        raise ValueError('Template structural rejection') from error
    parameters = layers.get('SCOT', ('', {}))[1]
    selected, metadata = decode_selected(parameters)
    raw_parameters = {}
    for name in ['speed', 'slopeModAngle', 'slopeModPercent', 'waterModPercent']:
        if name not in parameters:
            continue
        payload = parameters[name]
        needle = name.encode('ascii') + b'\0' + payload
        first = raw.find(needle)
        require(first >= 0 and raw.find(needle, first + 1) < 0, 'Ambiguous selected parameter location')
        raw_parameters[name] = dict(PayloadOffset=first + len(name) + 1,
                                    Bytes=len(payload), RawHex=payload.hex())
    return dict(Parent=parent, Versions={name: value[0] for name, value in layers.items()},
                Selected=selected, ArrayMetadata=metadata, RawParameters=raw_parameters)


def read_chain(lookup, root_path, pins=None):
    from audit_tre_filename_crc import normalize
    chain = []
    path = root_path
    while path:
        require(path not in [row['Path'] for row in chain], 'Template parent cycle')
        require(len(chain) < 16, 'Template chain research budget')
        found = lookup(path)
        require(found is not None, 'Required inherited template missing')
        priority, archive, raw = found
        digest = hashlib.sha256(raw).hexdigest().upper()
        if pins is not None:
            require(len(chain) < len(pins), 'Unexpected inherited source')
            require((path, priority, len(raw), digest) == pins[len(chain)], 'Chain source identity mismatch')
        document = inspect_document(raw)
        chain.append(dict(Path=path, Priority=priority, Archive=archive, Bytes=len(raw),
                          Sha256=digest, **document))
        path = normalize(document['Parent'].encode('ascii')).decode('ascii') if document['Parent'] else None
    if pins is not None:
        require(len(chain) == len(pins), 'Incomplete pinned chain')
    return chain


def classify(chain):
    results = {}
    for slot in SLOTS:
        visited = []
        for source in chain:
            parameter = source['Selected'][slot]
            visited.append(dict(Path=source['Path'], Mode=parameter['Mode'] if parameter else None,
                                RelativeByte=parameter['RelativeByte'] if parameter else None))
            if parameter is None or parameter['Mode'] == 0:
                continue
            relative = parameter['RelativeByte'] in (0x2B, 0x2D, 0x3D, 0x5F)
            sampled = parameter['Mode'] in (2, 3)
            kind = 'sampled/global' if sampled else ('inherited/relative' if relative else 'deterministic literal')
            results[slot] = dict(Classification=kind, Inherited=len(visited) > 1, Relative=relative,
                                 SourcePath=source['Path'], Parameter=parameter, Provenance=visited)
            break
        else:
            results[slot] = dict(Classification='absent/unknown', Provenance=visited)
    return results


def audit(args, repo):
    raw_exe = identity(args.exe, EXE_SHA, 'Executable')
    identity(args.configuration, CONFIG_SHA, 'Configuration')
    require(args.packages.is_dir(), 'Static-analysis package directory absent')
    prerequisite = runpy.run_path(str(repo / 'tools/python/verify_creature_view4_initialization.py'))['audit'](
        args.exe, args.packages, args.server_root, repo)
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=raw_exe, fast_load=True)
    read = lambda va, n: pe.get_data(va - pe.OPTIONAL_HEADER.ImageBase, n)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    for va, mnemonic, operands in ANCHORS:
        ins = next(md.disasm(read(va, 15), va))
        require((ins.mnemonic, ins.op_str) == (mnemonic, operands), f'Loader anchor mismatch {va:08X}')
    require(struct.unpack('<4I', read(0x13FA468, 16)) ==
            (0x13FA3E0, 0x13FA25E, 0x13FA296, 0x13FA386), 'Serialized mode table mismatch')
    require(struct.unpack('<I', read(0x1609AB8, 4))[0] == 0x13FA200, 'Float parameter loader vtable mismatch')
    sys.path.insert(0, str(repo / 'tools/python'))
    from cmpa_corpus_inventory import archives
    from verify_textured_mesh_bundle import Index
    from audit_tre_filename_crc import normalize
    with contextlib.ExitStack() as stack:
        mounts = archives(args.root, args.configuration)
        require(len(mounts) == 48 and len({p for p, _ in mounts}) == 48, 'Configured mount identity/count mismatch')
        indices = [(priority, archive.name, Index(stack.enter_context(archive.open('rb'))))
                   for priority, archive in mounts]

        def lookup(path):
            logical = normalize(path.encode('ascii')).decode('ascii')
            for priority, archive, index in indices:
                data = index.lookup(logical)  # existing resolver raises on tombstone; no lower fallback
                if data is not None:
                    return priority, archive, data
            return None

        chain = read_chain(lookup, ROOT_PATH, SOURCE_PINS)
    results = classify(chain)
    require(all(result['Classification'] == 'deterministic literal' and result['Inherited']
                and result['SourcePath'] == SOURCE_PINS[1][0]
                and result['Parameter']['RelativeByte'] == 0x20 for result in results.values()),
            'Pinned movement classification mismatch')
    return dict(ExeSha256=EXE_SHA, ConfigurationSha256=CONFIG_SHA, Mounts=48,
                LoaderAnchors=len(ANCHORS), SerializedModeTables=1, FloatLoaderVtables=1,
                Prerequisite=prerequisite, Chain=chain, Sources=len(chain),
                SourceBytes=sum(row['Bytes'] for row in chain), Results=results,
                FinalRuntimeValuesClaimed=False, OriginalExecuted=False, LiveContact=False,
                LaterWritesClosed=False, FullCorpusSurvey=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['exe', 'packages', 'server-root', 'root', 'configuration', 'receipt']:
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    try:
        target = private_receipt(args.receipt, repo)
        result = audit(args, repo)
        target.write_text(json.dumps(result, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    except (ValueError, AssertionError, OSError, ImportError, StopIteration, struct.error) as error:
        parser.exit(1, str(error) + '\n')
    print(f"PASS {result['LoaderAnchors']} loader anchors / one chain / five sources /8290 bytes /five inherited literal inputs; runtime values unclaimed")


if __name__ == '__main__':
    main()
