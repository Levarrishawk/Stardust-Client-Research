"""No-I/O completion validation receipt audit, composed with accepted router/original dependency oracles."""
import argparse
import contextlib
import io
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'round13-tests/Assets'))
from verify_router import audit as routed_audit, groups

def audit(root, configuration, catalog_profile, validation_output, router_output, creature_output, static_output):
    with contextlib.redirect_stdout(io.StringIO()):
        original = routed_audit(root, configuration, catalog_profile, router_output, creature_output, static_output)
    actual = groups(validation_output, 'validation_'); routed = groups(router_output, 'router_')['router_route']
    assert len(actual['validation_route']) == 2
    for row, proof in zip(actual['validation_route'], routed):
        for field in ('kind', 'wire', 'Recipe', 'ExternalReads', 'SourceBytes'): assert row[field] == proof[field]
        assert {s['Path']: s for s in row['sources']} == {s['Path']: s for s in proof['sources']}
        assert {t['path']: t['mips'] for t in row['textures']} == {t['path']: t['mips'] for t in proof['textures']}
        assert row['noReads'] and row['archiveClosed'] and row['unchanged'] and row['rawBuffersCleared'] == row['ExternalReads']
    summary, = actual['validation_summary']; cases, = actual['validation_cases']
    assert summary == dict(failures=89, successes=7, reads=66, noSourceIO=True, archiveClosed=True, nativeThreadIndependent=True, gpu=False, registry=False)
    assert len(cases) == 89 and all(c['rejected'] for c in cases)
    names = {c['name'] for c in cases}
    assert {'wrong-idle', 'wrong-walk-pin', 'wrong-walk-route', 'creature-albedo-color', 'wrong-frame', 'wrong-frame-pin', 'static-cycle', 'static-nonfinite-edge', 'static-corrupt-selected-frame', 'static-nested-source-hash'} <= names
    assert 'PASS R14-ASSETS-01:' in validation_output.read_text()
    return dict(routes=original['routes'], completion_negatives=89, completion_positive_checks=7, raw_buffers_cleared=66,
        source_pin_metadata=True, raw_create=True, actual_mip_hashes=True, sourceIO_during_validation=0, native_ownership_thread_fault_receipts=True,
        router_negatives=original['negatives'], creature_negatives=original['accepted_creature_negatives'], static_negatives=original['accepted_static_negatives'],
        gpu=False, registry=False, encoded_origin_attestation=False, fresh_corpus_census=False)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('root', 'configuration', 'catalog_profile', 'validation_output', 'router_output', 'creature_output', 'static_output'): parser.add_argument(name, type=Path)
    args = parser.parse_args(); print(json.dumps(audit(**vars(args)), indent=2))
