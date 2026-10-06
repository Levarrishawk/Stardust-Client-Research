"""Independent bounded negative and pointer-flow checks for the static verifier."""
from pathlib import Path
import argparse
import importlib.util
import json
import sys
from unittest.mock import patch

REPO = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("shader_consumer62", REPO / "tools/python/verify_terrain_shader_family_consumer.py")
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


def rejected(action):
    try:
        action()
    except ValueError:
        return 1
    raise AssertionError("Expected bounded guard rejection")


def run(exe, packages, asset_root, config, run_root):
    root = m.output_path(run_root)
    data = exe.read_bytes()
    m.gate(data)
    sys.path.insert(0, str(packages))
    import pefile
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    pe = pefile.PE(data=data, fast_load=True)
    def read(va, size):
        start = pe.get_offset_from_rva(va - 0x400000)
        return data[start:start + size]
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    counts = m.check_bodies(read, decoder)
    assert counts == {"Setter": 41, "Getter": 2, "Consumer": 36}
    # These are synthetic in-memory perturbations, never source evidence.
    identity = 0
    for at in (0, 1, len(data) // 2, len(data) - 1):
        changed = bytearray(data)
        changed[at] ^= 1
        identity += rejected(lambda: m.gate(changed))
    for changed in (b"", data[:-1], data + b"\0"):
        identity += rejected(lambda: m.gate(changed))
    anchors = 0
    for target in m.ANCHORS:
        def altered(va, size):
            raw = bytearray(read(va, size))
            if va <= target < va + size:
                raw[target - va] ^= 1
            return raw
        anchors += rejected(lambda: m.check_anchors(altered))
    bodies = 0
    for va, size, _ in m.BODIES.values():
        def altered(start, length):
            raw = bytearray(read(start, length))
            if start == va:
                raw[-1] ^= 1
            return raw
        bodies += rejected(lambda: m.check_bodies(altered, decoder))
    leads = 0
    for maximum in (0, 5, -1, 13, 1.5, True):
        leads += rejected(lambda: m.lead_budget(m.LEADS, maximum))
    leads += rejected(lambda: m.lead_budget(m.LEADS + (m.LEADS[0],), 12))
    leads += rejected(lambda: m.lead_budget((*m.LEADS[:-1], 0x12345678), 12))
    m.lead_budget(m.LEADS, 6)
    # Fixture existence must not depend on this worker's private scratch layout.
    assert not root.exists()
    root.mkdir(parents=True)
    sentinel = root / "ancestor-sentinel.bin"
    sentinel.write_bytes(b"synthetic non-directory ancestor")
    paths = 0
    for destination in (REPO, REPO / "docs/round62-forbidden", REPO / "local-private",
                        root, REPO / "local-private/../docs/escape",
                        REPO / "local-private/.git/forbidden", sentinel / "child"):
        paths += rejected(lambda: m.output_path(destination))
    # Mock metadata exercises linked lexical-ancestor rejection without creating
    # a junction/symlink or mutating any external location.
    actual_lstat = Path.lstat
    class Reparse:
        st_mode = 0o040755
        st_file_attributes = 0x400
    def linked(path, *args, **kwargs):
        return Reparse() if path == REPO / "local-private" else actual_lstat(path, *args, **kwargs)
    with patch.object(Path, "lstat", linked):
        paths += rejected(lambda: m.output_path(root / "linked-must-not-publish"))
    # Independently check pointer order/identity, repeated pointers and signed
    # zero-result control. No comparison or resource lookup is performed.
    query = bytearray(b"caller\0")
    first = bytearray(b"first\xff\0")
    other = bytearray(b"other\0")
    families = [first, first, other]
    pointer_checks = 0
    for results, expected_indices, expected in [([2, 0, 2], [0, 1], True),
                                                ([-2, -1, 0], [0, 1, 2], True),
                                                ([1, 2, 3], [0, 1, 2], False)]:
        result, calls = m.consumer_witness(families, query, results)
        assert result is expected and [row[0] for row in calls] == expected_indices
        assert all(row[1] is query and row[2] is families[row[0]] for row in calls)
        pointer_checks += 2
    for count in (0, -17):
        assert m.consumer_witness(families, query, [], count) == (False, [])
        pointer_checks += 1
    witnesses, entries = m.original_witness(asset_root, config)
    assert len(witnesses) == 15 and entries == 35
    assert all(row["SourceWithNulSha256"] == row["OwnedWithNulSha256"] for row in witnesses)
    # Only explicitly authored private harness fixtures exist before integration.
    assert {path.name for path in root.iterdir()} == {sentinel.name}
    bad_exe = root / "wrong-image.bin"
    bad_exe.write_bytes(b"MZ synthetic wrong identity")
    bad_config = root / "wrong-config.cfg"
    bad_config.write_text("synthetic wrong identity\n", encoding="utf-8")
    integration = 0
    missing = root / "must-not-publish"
    with patch.object(m, "check_bodies", side_effect=AssertionError("VA checks reached before identity gate")):
        integration += rejected(lambda: m.verify(bad_exe, packages, asset_root, config, missing))
    assert not missing.exists()
    integration += rejected(lambda: m.verify(exe, packages, asset_root, bad_config, missing))
    assert not missing.exists()
    integration += rejected(lambda: m.verify(exe, packages, asset_root, config, missing, 5))
    assert not missing.exists()
    receipt = m.verify(exe, packages, asset_root, config, root / "positive")
    sealed = {path.name:m.digest(path.read_bytes()) for path in (root / "positive").iterdir()}
    integration += rejected(lambda: m.verify(exe, packages, asset_root, config, root / "positive"))
    assert {path.name:m.digest(path.read_bytes()) for path in (root / "positive").iterdir()} == sealed
    summary = dict(Passed=True, Static=receipt, WrongIdentityGuards=identity,
                   SyntheticAnchorGuards=anchors, SyntheticBodyGuards=bodies,
                   LeadBudgetGuards=leads, OutputPathGuards=paths,
                   IntegrationNoPublicationGuards=integration, IndependentPointerChecks=pointer_checks,
                   TotalNegativeGuards=identity+anchors+bodies+leads+paths+integration,
                   OriginalOccurrences=1, OriginalExecution=False)
    (root / "comparison-receipt.json").write_text(json.dumps(summary, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for argument in ("exe", "packages", "asset-root", "config", "run-root"):
        parser.add_argument("--"+argument, type=Path, required=True)
    args = parser.parse_args()
    run(args.exe,args.packages,args.asset_root,args.config,args.run_root)
