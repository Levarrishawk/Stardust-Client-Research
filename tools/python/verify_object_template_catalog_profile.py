"""Independent strict JSON/property/path/CRC oracle and canonical byte agreement. No archive or native execution."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_scene_creature_resources import checksum

DEFAULTS = dict(MaximumBytes=1048576, MaximumDepth=4, MaximumEntries=256, MaximumPathCharacters=262144)
CONFIG = '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'
EXPECTED = [
    dict(templateCrc=2907007746, objectPath='object/tangible/terminal/shared_terminal_mission.iff',
         objectSha256='E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281',
         appearanceSha256='74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021', recipe='staticAppearance'),
    dict(templateCrc=2937962913, objectPath='object/creature/player/shared_human_male.iff',
         objectSha256='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243',
         appearanceSha256='951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C', recipe='creatureDetail0')]


class Number:
    def __init__(self, token): self.token = token


def require(value):
    if not value: raise ValueError('Profile contract rejection')


def pairs(items):
    result = {}
    for key, value in items:
        require(key not in result); result[key] = value
    return result


def unsigned(value):
    require(type(value) is Number and value.token and all(c in '0123456789' for c in value.token))
    result = int(value.token); require(result <= 0xffffffff); return result


def fields(value, names):
    require(type(value) is dict and set(value) == set(names)); return value


def depth(value):
    if type(value) is dict: return 1 + max((depth(v) for v in value.values()), default=0)
    if type(value) is list: return 1 + max(map(depth, value), default=0)
    return 0


def path(value):
    require(type(value) is str and len(value) <= 1024 and all(0x20 <= ord(c) <= 0x7e for c in value))
    value = value.replace('\\', '/')
    require(not any(segment in ('.', '..') for segment in value.split('/')))
    # Independently spell the accepted logical archive normalization, not operating-system resolution.
    value = value.lstrip('/').lower()
    while '//' in value: value = value.replace('//', '/')
    require(value.startswith('object/') and value.endswith('.iff') and len(value.rsplit('/', 1)[-1]) > 4)
    return value


def pin(value):
    require(type(value) is str and len(value) == 64 and all(c in '0123456789abcdefABCDEF' for c in value))
    return value.upper()


def decode(raw, limits=None):
    limits = DEFAULTS if limits is None else limits
    require(set(limits) == set(DEFAULTS))
    for key, ceiling in dict(MaximumBytes=16777216, MaximumDepth=16, MaximumEntries=4096, MaximumPathCharacters=4194304).items():
        require(type(limits[key]) is int and 1 <= limits[key] <= ceiling)
    require(len(raw) <= limits['MaximumBytes'])
    value = json.loads(raw.decode('utf-8', errors='strict'), object_pairs_hook=pairs,
                       parse_int=Number, parse_float=Number, parse_constant=lambda _: require(False))
    require(depth(value) <= limits['MaximumDepth'])
    fields(value, ('version', 'entries')); require(unsigned(value['version']) == 1)
    entries = value['entries']; require(type(entries) is list and len(entries) <= limits['MaximumEntries'])
    paths = set(); crcs = set(); characters = 0; result = []
    for entry in entries:
        fields(entry, ('templateCrc', 'objectPath', 'objectSha256', 'appearanceSha256', 'recipe'))
        logical = path(entry['objectPath']); crc = unsigned(entry['templateCrc'])
        require(crc == checksum(logical) and logical not in paths and crc not in crcs)
        paths.add(logical); crcs.add(crc); characters += len(logical)
        require(characters <= limits['MaximumPathCharacters'])
        recipe = entry['recipe']; require(type(recipe) is str and recipe in ('creatureDetail0', 'staticAppearance'))
        result.append(dict(templateCrc=crc, objectPath=logical, objectSha256=pin(entry['objectSha256']),
                           appearanceSha256=pin(entry['appearanceSha256']), recipe=recipe))
    return sorted(result, key=lambda e: e['templateCrc'])


def canonical(entries):
    return json.dumps(dict(version=1, entries=entries), separators=(',', ':'), ensure_ascii=True).encode('utf-8')


def audit(profile, native_output, canonical_output):
    require(profile.stat().st_size <= DEFAULTS['MaximumBytes'] and native_output.stat().st_size < 1048576)
    original = profile.read_bytes(); entries = decode(original); require(entries == EXPECTED)
    expected = canonical(entries); require(canonical_output.read_bytes() == expected and len(expected) == 598)
    content = native_output.read_text(encoding='utf-8-sig'); require('PASS R9-ASSETS-01:' in content)
    groups = {}
    for line in content.splitlines():
        if line.startswith('catalog_profile_'):
            key, value = line.split('=', 1); groups.setdefault(key, []).append(json.loads(value))
    cases = groups['catalog_profile_case']; labels = set(); accepted = rejected = 0
    for case in cases:
        require(case['label'] not in labels); labels.add(case['label']); raw = bytes.fromhex(case['hex'])
        try: result = decode(raw, case['limits'])
        except (ValueError, UnicodeError):
            rejected += 1; require(not case['accept'] and case['canonical'] is None)
        else:
            accepted += 1; require(case['accept'] and bytes.fromhex(case['canonical']) == canonical(result))
    validation, = groups['catalog_profile_validation']
    require(validation == dict(rejects=rejected, accepted=accepted, optionRejects=16, encoderRejects=4, entries=2,
                               inputSha256=hashlib.sha256(original).hexdigest().upper(), canonicalSha256=hashlib.sha256(expected).hexdigest().upper(),
                               canonicalBytes=len(expected), pathCharacters=sum(len(e['objectPath']) for e in entries), owned=True, deterministic=True, configSha256=CONFIG))
    require(rejected == 126 and accepted == 12)
    readers, = groups['catalog_profile_readers']; require(not readers['gpu'])
    for key, crc, reads, size in [('creature', 2937962913, 37, 4683848), ('stat', 2907007746, 27, 1401794)]:
        got = readers[key]; entry, = [e for e in entries if e['templateCrc'] == crc]
        require(got['recipe'] == dict(TemplateCrc=crc, ObjectPath=entry['objectPath'], ObjectSha256=entry['objectSha256'], AppearanceSha256=entry['appearanceSha256'], Recipe=0 if key == 'creature' else 1))
        require(got['ExternalReads'] == reads and got['SourceBytes'] == size and len(got['counts']) == reads and set(got['counts'].values()) == {1})
        require(entry['objectPath'] in got['counts'])
    require(readers['stat']['meshes'] == 4)
    require(checksum('object/synthetic/pppppppppppppppppppp.iff') == checksum('object/synthetic/qqqpprrpqqprrssspppp.iff') == 0xbfcb749d)
    return dict(entries=entries, decoder_acceptances=accepted, decoder_rejections=rejected, limit_option_rejections=16,
                encoder_budget_rejections=4, canonical_bytes=len(expected), canonical_sha256=hashlib.sha256(expected).hexdigest().upper(),
                independent_crc=True, exact_native_acceptance_agreement=True, reader_composition=readers, original_asset_reaudit=False)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('profile', 'native_output', 'canonical_output'): parser.add_argument(name, type=Path)
    args = parser.parse_args(); print(json.dumps(audit(args.profile, args.native_output, args.canonical_output), indent=2))
