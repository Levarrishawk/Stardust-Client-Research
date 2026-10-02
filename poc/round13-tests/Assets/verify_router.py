"""Compact router raw-create/catalog/source/mip oracle; compose the accepted original dependency audits."""
import argparse
import contextlib
import hashlib
import io
import json
import struct
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[3] / 'tools/python'))
from cmpa_corpus_inventory import archives
from verify_textured_mesh_bundle import Index
from verify_scene_creature_albedo_resources import audit as creature_audit
from verify_switch_texture_static_resources import audit as static_audit
from verify_scene_creature_resources import CONFIG_PIN


def groups(path, prefix):
    assert path.stat().st_size < 1048576
    result = {}
    for line in path.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith(prefix):
            key, value = line.split('=', 1); result.setdefault(key, []).append(json.loads(value))
    return result


def audit(root, configuration, catalog_profile, router_output, creature_output, static_output):
    assert hashlib.sha256(configuration.read_bytes()).hexdigest().upper() == CONFIG_PIN
    with contextlib.redirect_stdout(io.StringIO()):
        creature_proof = creature_audit(root, configuration, catalog_profile, creature_output)
        static_proof = static_audit(root, configuration, static_output)
    routed = groups(router_output, 'router_'); creatures = groups(creature_output, 'creature_albedo_'); statics = groups(static_output, 'switch_')
    catalog = json.loads(catalog_profile.read_text(encoding='utf-8')); assert catalog['version'] == 1
    choices = {r['templateCrc']: r for r in catalog['entries']}; assert len(choices) == len(catalog['entries']) == 2
    routes = routed['router_route']; assert [r['kind'] for r in routes] == ['CreatureDetail0', 'StaticAppearance']
    expected_sources = [creatures['creature_albedo_source'], statics['switch_frame'][0]['sources']]
    expected_textures = [creatures['creature_albedo_texture'], statics['switch_frame'][0]['textures']]
    summaries = []
    with contextlib.ExitStack() as stack:
        indices = [(p, Index(stack.enter_context(f.open('rb')))) for p, f in archives(root, configuration)]
        for index, row in enumerate(routes):
            crc, count, size = (2937962913, 39, 4686371) if index == 0 else (2907007746, 27, 1401794)
            entry = choices[crc]; assert entry['recipe'] == ('creatureDetail0' if index == 0 else 'staticAppearance')
            assert row['Recipe'] == dict(TemplateCrc=crc, ObjectPath=entry['objectPath'], ObjectSha256=entry['objectSha256'], AppearanceSha256=entry['appearanceSha256'], Recipe=index)
            wire = bytes.fromhex(row['wire']); assert len(wire) == 47
            assert struct.unpack_from('<HIQ', wire) == (5, 0xfe89ddea, 0xfedcba9876543210)
            assert struct.unpack_from('<7I', wire, 14) == (0x80000000, 0x3e800000, 0xbf000000, 0x40000000, 0x42f70000, 0xc2350000, 1)
            assert struct.unpack_from('<I', wire, 42)[0] == crc and wire[46] == 255
            sources = {s['Path']: s for s in row['sources']}; assert len(sources) == len(row['sources']) == count
            assert sources == {s['Path']: s for s in expected_sources[index]}
            assert row['counts'] == dict.fromkeys(sources, 1) and row['ExternalReads'] == count and row['SourceBytes'] == size
            assert sum(s['Bytes'] for s in sources.values()) == size and sources[entry['objectPath']]['Sha256'] == entry['objectSha256']
            for path, source in sources.items():
                for priority, archive in indices:
                    raw = archive.lookup(path)
                    if raw is not None: break
                else: raise AssertionError('Required routed source absent: ' + path)
                assert source == dict(Path=path, Priority=priority, Bytes=len(raw), Sha256=hashlib.sha256(raw).hexdigest().upper())
            textures = {t['path']: t['mips'] for t in row['textures']}; assert len(textures) == len(row['textures'])
            assert textures == {t['path']: t['mips'] for t in expected_textures[index]}
            assert row['albedo'] == (index == 0) and row['frame'] == (None if index == 0 else 0)
            assert row['workerThread'] != row['callerThread'] and row['archiveDisposed'] and row['ownedPayload'] and row['exactlyOnePayload']
            summaries.append(dict(kind=row['kind'], sources=count, source_bytes=size, textures=len(textures), mips=sum(map(len, textures.values())), raw_create=True))
    validation, = routed['router_validation']
    assert validation == dict(rejects=65, preflightReads=0, routes=2, borrowedChecks=66, cancellationCalls=2, optionSnapshot=True, offThread=True, rawCreate=True, noPublication=True, gpu=False)
    assert 'PASS R13-ASSETS-01:' in router_output.read_text()
    return dict(routes=summaries, negatives=65, borrowed_source_checks=66, zero_preflight_reads=True, exact_catalog=True, independent_source_union=True,
                inherited_creature_templates=creature_proof['original_inherited_templates'], static_dependency_audit=static_proof['exact_source_union'],
                accepted_creature_negatives=creature_proof['native_rejections'], accepted_static_negatives=static_proof['native_rejections'],
                native_worker_ownership_cleanup_receipts=True, gpu=False, world_publication=False, fresh_corpus_census=False)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('root', 'configuration', 'catalog_profile', 'router_output', 'creature_output', 'static_output'): parser.add_argument(name, type=Path)
    args = parser.parse_args(); print(json.dumps(audit(**vars(args)), indent=2))
