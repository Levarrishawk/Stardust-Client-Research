#!/usr/bin/env python3
"""Inventory effective legacy UI tag/property/include vocabulary from configured TREs."""
from __future__ import annotations

import argparse
import collections
import re
from pathlib import Path

from scan_tre_sound_references import archive_records, expand


def configured_archives(root: Path, cfg: Path):
    rows = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            rows.append((int(match.group(1)), root / match.group(2)))
    return sorted(rows, reverse=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument("cfg", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    winners = {}
    errors = []
    for priority, archive in configured_archives(args.root, args.cfg):
        for name, usize, kind, packed in archive_records(archive):
            if name in winners or not name.endswith((".ui", ".inc")):
                continue
            try:
                payload = expand(kind, packed, usize).decode("utf-8", "replace")
                winners[name] = (priority, archive.name, payload)
            except Exception as exc:
                errors.append((archive.name, name, str(exc)))

    tags = collections.Counter()
    properties = collections.Counter()
    tag_properties = collections.defaultdict(collections.Counter)
    includes = collections.Counter()
    events = collections.Counter()
    malformed = []
    tag_re = re.compile(r"<\s*(?!/)([A-Za-z_][\w.]*)\b([^<>]*?)\s*/?>", re.S)
    property_re = re.compile(r"([A-Za-z_][\w.]*)\s*=\s*(?:(['\"])(.*?)\2|([^\s/>]+))", re.S)
    include_re = re.compile(r"<include>\s*([^<]+?)\s*</include>", re.I)
    for name, (_, _, text) in winners.items():
        for include in include_re.findall(text):
            includes[include.strip().lower().replace("\\", "/")] += 1
        for match in tag_re.finditer(text):
            tag = match.group(1).lower()
            if tag.lower() == "include":
                continue
            tags[tag] += 1
            attrs = list(property_re.finditer(match.group(2)))
            for attr in attrs:
                prop = attr.group(1)
                properties[prop] += 1
                tag_properties[tag][prop] += 1
                if tag not in ("data", "namespace") and prop.lower().startswith("on"):
                    events[prop] += 1
            residue = property_re.sub("", match.group(2)).strip()
            if residue:
                malformed.append((name, tag, " ".join(residue.split())[:160]))

    lines = [
        "# Effective legacy UI corpus inventory",
        "",
        f"Configured UI records: {len(winners)}",
        f"Distinct element tags: {len(tags)}",
        f"Distinct attribute/property spellings: {len(properties)}",
        f"Distinct include targets: {len(includes)}",
        f"Decode errors: {len(errors)}",
        f"Unparsed tag residues: {len(malformed)}",
        "",
        "## Element tags",
        "",
        "| Tag | Count | Distinct properties |",
        "|---|---:|---:|",
    ]
    lines += [f"| `{tag}` | {count} | {len(tag_properties[tag])} |" for tag, count in tags.most_common()]
    lines += ["", "## Event-like properties", "", "| Property | Count |", "|---|---:|"]
    lines += [f"| `{prop}` | {count} |" for prop, count in events.most_common()]
    structural_properties = collections.Counter()
    for tag, counter in tag_properties.items():
        if tag not in ("data", "namespace"):
            structural_properties.update(counter)
    lines += ["", "## Structural property spellings", "", f"Distinct structural properties (excluding open-ended `Data` and `Namespace` payload keys): {len(structural_properties)}", "", ", ".join(f"`{name}`" for name in sorted(structural_properties, key=str.lower))]
    lines += ["", "## Include resolution", "", f"Referenced targets: {len(includes)}; present as effective records: {sum(1 for x in includes if (x if x.startswith('ui/') else 'ui/' + x) in winners)}."]
    missing = []
    for include in includes:
        candidate = include if include.startswith("ui/") else "ui/" + include
        if candidate not in winners:
            missing.append(include)
    lines += ["", "Missing effective include targets:", "", *(f"- `{name}`" for name in sorted(missing))]
    if malformed:
        lines += ["", "## Parser residues (first 100)", "", *(f"- `{a}` `{b}`: `{c}`" for a, b, c in malformed[:100])]
    if errors:
        lines += ["", "## Decode errors", "", *(f"- `{a}` `{b}`: {c}" for a, b, c in errors)]
    args.output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"records={len(winners)} tags={len(tags)} properties={len(properties)} includes={len(includes)} missing={len(missing)} residues={len(malformed)} errors={len(errors)}")


if __name__ == "__main__":
    main()
