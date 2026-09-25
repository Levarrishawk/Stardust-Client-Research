"""Build the conservative UI class/workflow audit register.

This is a discovery denominator, not an automatic completion classifier. Exact
prose mentions and factory RTTI are evidence columns; they never prove a full
activation -> event -> effect -> cleanup workflow by themselves.
"""

from __future__ import annotations

import csv
import pathlib
import re
import sys


SUPPORT_PATTERNS = (
    "Action",
    "Adapter",
    "Callback",
    "Helper",
    "Info",
    "Manager",
    "Parser",
    "Provider",
    "Strategy",
    "TableModel",
    "Transition",
    "Widget",
    "Workspace",
)


def exact_hits(name: str, paths: list[pathlib.Path], root: pathlib.Path) -> list[str]:
    pattern = re.compile(r"(?<![A-Za-z0-9_])" + re.escape(name) + r"(?![A-Za-z0-9_])")
    result = []
    for path in paths:
        if pattern.search(path.read_text(encoding="utf-8", errors="replace")):
            result.append(path.relative_to(root).as_posix())
    return result


def main() -> None:
    root = pathlib.Path(sys.argv[1]).resolve()
    output = pathlib.Path(sys.argv[2]).resolve()
    inventory = root / "local-private/generated-evidence/ui_concrete_class_inventory.tsv"
    override_path = root / "data/curated/ui_workflow_overrides.csv"
    vtables = (root / "local-private/generated-evidence/cui_vtable_inventory.tsv").read_text(
        encoding="utf-8", errors="replace"
    )
    checkpoint_paths = sorted((root / "research/checkpoints").glob("*.md"))
    # The source inventory enumerates class names and would make its own rows
    # appear behaviorally researched. It is denominator evidence, not a
    # workflow checkpoint.
    checkpoint_paths = [
        path
        for path in checkpoint_paths
        if path.name != "ANALYSIS_CHECKPOINT_2026-09-21_UI_MEDIATOR_CLASS_INVENTORY.md"
    ]
    other_paths = sorted((root / "docs").glob("*.md"))
    other_paths += [root / "ANALYSIS_STATUS.md", root / "UNKNOWN_FUNCTIONS.md"]

    with inventory.open(encoding="utf-8", newline="") as handle:
        source_rows = list(csv.DictReader(handle, delimiter="\t"))
    overrides: dict[str, dict[str, str]] = {}
    if override_path.exists():
        with override_path.open(encoding="utf-8", newline="") as handle:
            overrides = {row["class_name"]: row for row in csv.DictReader(handle)}

    rows = []
    for source in source_rows:
        name = source["class_name"]
        if "@" in name:
            continue
        escaped = re.escape(name)
        factory_registered = bool(
            re.search(r"Constructor@V" + escaped + r"@@@CuiMediatorFactory", vtables)
        )
        checkpoint_hits = exact_hits(name, checkpoint_paths, root)
        other_hits = exact_hits(name, other_paths, root)
        support_component = not factory_registered and any(token in name for token in SUPPORT_PATTERNS)
        kind = "support_component" if support_component else "mediator_or_controller"
        if factory_registered:
            reachability = "factory_registered_activation_unverified"
        elif support_component:
            reachability = "owned_support_component"
        else:
            reachability = "no_factory_activation_unverified"
        if checkpoint_hits:
            coverage = "evidence_present_workflow_exit_test_required"
        elif other_hits:
            coverage = "architecture_mention_only"
        else:
            coverage = "untriaged"
        if kind == "support_component":
            next_evidence = "identify owner/callers and classify material behavior"
        elif factory_registered:
            next_evidence = "prove action/owner activation, effective asset, event/effect, failure and cleanup"
        else:
            next_evidence = "identify construction owner before workflow analysis"
        row = {
                "category": source["category"],
                "class_name": name,
                "kind": kind,
                "factory_registered": "yes" if factory_registered else "no",
                "checkpoint_files": ";".join(checkpoint_hits),
                "other_research_files": ";".join(other_hits),
                "reachability_status": reachability,
                "coverage_status": coverage,
                "next_evidence": next_evidence,
                "activation_evidence": "",
                "effective_asset_evidence": "",
                "event_effect_evidence": "",
                "failure_cleanup_evidence": "",
                "manual_evidence_file": "",
            }
        override = overrides.get(name)
        if override:
            for key, value in override.items():
                if key != "class_name" and value:
                    row[key] = value
        rows.append(row)

    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=rows[0].keys())
        writer.writeheader()
        writer.writerows(rows)

    print(f"standalone_rows={len(rows)}")
    print(f"factory_registered={sum(row['factory_registered'] == 'yes' for row in rows)}")
    for status in sorted({row["coverage_status"] for row in rows}):
        print(f"{status}={sum(row['coverage_status'] == status for row in rows)}")


if __name__ == "__main__":
    main()
