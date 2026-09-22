#!/usr/bin/env python3
"""Bounded CEF -> PRT/PATT -> PTEX dependency closure for local research."""
from __future__ import annotations

import csv
import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

from scan_tre_sound_references import archive_records


def rows(path: Path):
    with path.open(newline="", encoding="utf-8") as handle:
        return list(csv.DictReader(handle))


def normalize(value: str) -> str:
    return value.replace("\\", "/").lower()


def main(root: Path, cfg: Path, cef_csv: Path, particles_csv: Path, attachments_csv: Path, textures_csv: Path, output: Path) -> None:
    configured = []
    for line in cfg.read_text(errors="replace").splitlines():
        match = re.search(r"searchTree_00_(\d+)\s*=\s*([^\s;]+)", line, re.I)
        if match and (root / match.group(2)).exists():
            configured.append((int(match.group(1)), root / match.group(2)))
    effective = {}
    for _, archive in sorted(configured, reverse=True):
        for name, usize, _, packed in archive_records(archive):
            effective.setdefault(normalize(name), bool(usize and packed))

    cef = rows(cef_csv)
    particle_rows = rows(particles_csv)
    attachment_rows = rows(attachments_csv)
    texture_rows = rows(textures_csv)
    particle_live = {normalize(r["name"]) for r in particle_rows if r["outer"] == "PEFT"}
    roots = []
    source_refs = []
    for row in cef:
        if row["form"] != "CLEF":
            continue
        for ref in row["references"].split(";"):
            ref = normalize(ref)
            if ref.rstrip().endswith(".prt"):
                roots.append(ref)
                source_refs.append((row["name"], ref))
    edges = defaultdict(list)
    for row in attachment_rows:
        edges[normalize(row["particle"])].append(normalize(row["path"]))
    textures = defaultdict(list)
    for row in texture_rows:
        textures[normalize(row["particle"])].append(normalize(row["resource"]))

    visited = set()
    queue = list(dict.fromkeys(roots))
    while queue:
        particle = queue.pop()
        if particle in visited or particle not in particle_live:
            continue
        visited.add(particle)
        for target in edges[particle]:
            if target.endswith(".prt") and target not in visited:
                queue.append(target)
    reached_edges = [(source, target) for source in visited for target in edges[source]]
    reached_shaders = [(source, shader) for source in visited for shader in textures[source]]
    missing_roots = [(source, target) for source, target in source_refs if target not in particle_live]
    missing_edges = [(source, target) for source, target in reached_edges if not effective.get(target)]
    empty_shader_slots = [(source, target) for source, target in reached_shaders if not target]
    missing_shaders = [(source, target) for source, target in reached_shaders if target and not effective.get(target)]

    color = {}
    cycles = []
    def dfs(node: str, stack: list[str]):
        color[node] = 1
        for child in edges[node]:
            if not child.endswith(".prt") or child not in visited:
                continue
            if color.get(child) == 1:
                cycles.append(stack + [child])
            elif color.get(child, 0) == 0:
                dfs(child, stack + [child])
        color[node] = 2
    for root_name in dict.fromkeys(roots):
        if root_name in visited and color.get(root_name, 0) == 0:
            dfs(root_name, [root_name])

    result = {
        "configured_tres": len(configured),
        "effective_names": len(effective),
        "cef_files": sum(r["form"] == "CLEF" for r in cef),
        "cpap_refs": len(roots),
        "cpap_unique": len(set(roots)),
        "cpap_missing": missing_roots,
        "particle_nodes_reached": len(visited),
        "attachment_edges_reached": len(reached_edges),
        "attachment_targets_by_extension": dict(Counter(Path(t).suffix for _, t in reached_edges)),
        "attachment_missing": missing_edges,
        "particle_cycles": cycles,
        "ptex_shader_edges_reached": len(reached_shaders),
        "ptex_unique_shaders": len(set(t for _, t in reached_shaders)),
        "ptex_empty_shader_slots": len(empty_shader_slots),
        "ptex_empty_shader_enabled": sum(normalize(r["particle"]) in visited and not normalize(r["resource"]) and r["enabled"] == "1" for r in texture_rows),
        "ptex_empty_shader_disabled": sum(normalize(r["particle"]) in visited and not normalize(r["resource"]) and r["enabled"] == "0" for r in texture_rows),
        "ptex_unique_nonempty_shaders": len(set(t for _, t in reached_shaders if t)),
        "ptex_shader_names": sorted(set(t for _, t in reached_shaders if t)),
        "ptex_missing_shaders": missing_shaders,
        "ptex_particle_nodes": sum(bool(textures[n]) for n in visited),
    }
    output.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps({key: value for key, value in result.items() if key != "ptex_shader_names"}, indent=2))


if __name__ == "__main__":
    main(*(Path(value) for value in sys.argv[1:8]))
