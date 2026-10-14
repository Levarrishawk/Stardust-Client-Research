# Research-only cleanup — October 14, 2026

The user requested removal of Godot client work from this research repository.

## Scope and observations

Removed the duplicate `poc/` tree (2,728 public files), 148 prototype-dependent
support scripts and seven implementation runbooks/contracts already present in
SWG-Godot-Client. All removed non-Markdown public files matched the sibling copy
byte-for-byte. Eleven prototype README files differed because migration rewrote
links. An ignored backup and SHA-256 manifest preserve all 2,883 removed public
files, including uncommitted source, under local-private/research-only-cleanup.
The sibling repository was not changed.

Research specifications, dated implementation evidence, static-analysis tools,
registries and unrelated NGE research remain. Historical evidence links now
point to the sibling implementation. Entry points describe research-only scope;
old development authorizations are explicitly historical. No coverage counts or
canonical function names changed. The executable was not used, so no VA-based
claims or executable-hash validation are introduced.

## Reproduction and next action

Run `tools/python/validate_organized_workspace.py .` from this repository.
Run historical prototype-dependent verification from SWG-Godot-Client, where
its original relative layout remains intact. See the
[scope document](../../docs/RESEARCH_ONLY_SCOPE.md).

Next research probe: select an unresolved question in the
[slope normal producer contract](../../docs/TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md),
confirm the executable baseline and inspect the documented producer/caller
boundary. Original numeric parity remains unresolved; cleanup adds no behavioral
coverage or implementation acceptance.

## Validation

Workspace/link validation passes with zero broken links, zero private links,
2,626 unique provisional addresses and 345 packet rows. All curated CSVs are
unchanged. `git diff --check` passes. No public C# projects or Godot scenes remain.
The validator now prunes private/Git directories before traversal rather than
scanning and then discarding their contents; its validation rules are unchanged.
No client/GPU suites were rerun for removal. Standalone research and historical
evidence remain; generated private build/evidence material is retained.
