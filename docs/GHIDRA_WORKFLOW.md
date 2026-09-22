# Persistent Ghidra Workflow

Updated: 2026-09-11

This workflow applies only to `C:\Stardust-DEV\Stardust.exe` with SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. It is local research material and must not be published to GitHub.

## Installed analysis environment

- Ghidra: `toolchain/ghidra_12.1.3_PUBLIC`
- Persistent project: `ghidra/StardustClient`
- Imported program: `/Stardust.exe`, x86 little-endian 32-bit Windows, image base `0x00400000`
- Function inventory: `ghidra/function_inventory.tsv`
- Constructor field-registration inventory: `ghidra/replication_registration_inventory.tsv`
- Registration/RTTI join: `ghidra/replication_registration_rtti.tsv`
- Targeted pseudocode: `ghidra/decompiled/`
- Scripts and address lists: `tools/`

The Ghidra distribution archive SHA-256 is `93A5D11A9AD510622ACAAF908C556A7B9B764D338E78A7567F3689BF5081FD54`.

## Repeatable sequence

1. Re-hash the executable. Stop if it differs from the baseline; create a new program/version rather than silently reusing addresses.
2. Add stable names to `UNKNOWN_FUNCTIONS.md` using `subsystem_role_VA_XXXXXXXX` identifiers.
3. Regenerate `tools/provisional_names.csv` with `tools/function_inventory.py`.
4. Run `ImportProvisionalNames.java` against the saved project.
5. For a bounded subsystem, put one confirmed function entry address per line in a dedicated address-list file.
6. Run `DecompileAddressList.java` into a named subdirectory under `ghidra/decompiled/`.
7. Validate pseudocode against raw control flow, call sites, vtables, RTTI, strings, and current server packet source. Pseudocode is never treated as original source.
8. Record evidence, confidence, and the next unresolved edge in the relevant architecture document and `UNKNOWN_FUNCTIONS.md`.
9. Re-export `ghidra/function_inventory.tsv` after creating functions or importing names.

Run `tools/replication_registration_inventory.py` after constructor pseudocode changes. It extracts view helper calls in exact order, computes x86 object offsets, and joins directly assigned vtables where Ghidra preserved them. Blank vtable cells indicate decompiler-temporary construction and require manual RTTI confirmation; they do not mean the field has no vtable.

Then run `tools/export_registration_rtti.py` against the executable and registration inventory, passing `tools/confirmed_replication_vtables.csv` through `--overrides`. It follows the x86 MSVC complete-object-locator pointer preceding each vtable and records the raw decorated field type. The current export resolves all 86 rows across 52 unique vtables; five decompiler-obscured rows use separately recorded constructor/destructor evidence. Treat undecorated semantic labels as analysis; the raw RTTI string is executable evidence.

## Important analyzer limitation

Ghidra initially produced 243,265 function objects; about half are eight bytes or smaller. This is not an authoritative semantic-function count. Conversely, automatic discovery missed valid functions with nonstandard/aligned-stack prologues, including `0x0043F960`, `0x0118A840`, `0x0118AA00`, and `0x0118A970`.

Create a missing function only after at least two independent indicators agree, such as a vtable entry plus coherent disassembly, or direct calls plus a valid control-flow return. Record manually created entries in `tools/confirmed_missing_functions.csv` and apply them with `CreateConfirmedFunctions.java`.

## Resume point

The highest-value next trace is the installed replicated-wearable element callbacks from vector mutation into appearance attachment/removal. After that, recover remaining historical PLAY:3/8/9 field semantics and specialized codecs, then characterize malformed field-index/archive exception behavior. The view registry; all common/Tangible/Intangible/Creature ordering; PLAY cardinality/type ordering; callback commit model; waypoint precedence; and initial Cell/Static/Building inheritance are established and should not be rediscovered.
