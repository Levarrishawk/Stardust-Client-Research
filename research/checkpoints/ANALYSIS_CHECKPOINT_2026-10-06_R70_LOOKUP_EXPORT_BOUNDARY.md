# Round70 lookup export boundary — 2026-10-06

Question: does accepted wrapper `00B6B580` have an exact named, ordinal or forwarded PE export, or defensible export containment? Stop after the approved bounded metadata inventory and one local anchor assessment.

The SHA-256-pinned baseline executable is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The verifier checks identity before using VAs. Its frozen round69 prerequisite SHA-256 is `1f120c8f398e11f7d80d56b3433316f3f65e1cf27cc5019b28b87668b1a750e3`; only PE mapping and output-path dependencies are used, without replaying earlier body or corpus suites.

Observation: the PE export directory has RVA 21281856, size 3853, ordinal base 1, 78 address rows and 78 name rows. Every nonzero export target differs from wrapper VA `00B6B580`; none of the rows is a forwarder. Raw bounded parsing agrees with independent `pefile` metadata parsing for every ordinal, RVA, name and forwarder.

Confirmed conclusion: `00B6B580` has no exact direct named or ordinal export entry in this baseline. There is no forwarder entry to resolve here. This does not establish absence of indirect callers or runtime unreachability. Export targets are addresses, not independently established semantic function bodies.

The nearest lower and higher export entries are `00B22C80` and `013A4BD0`. The EAT supplies entry addresses, not function lengths; containment within another exported function remains Unknown. No body at either neighboring address was expanded.

Local concept searches in curated rows, docs and tools for ShaderGroup/shader_group supplied no defensible terrain-group API or vtable anchor. No indirect caller is claimed. An arbitrary whole-section pointer scan or whole-executable cross-reference census was not performed. This is a bounded negative result, not a complete caller census.

Compatibility consequence: an exported entry cannot currently serve as the integration anchor for this wrapper. Preserve the accepted lookup boundary while leaving its producer and resource semantics unresolved. No replacement resolver or new function name is introduced.

The complete denominator is 79 address leads (wrapper plus 78 distinct nonzero export targets) and 315 logical metadata sites: two directory/header starts, 234 indexed address/name-pointer/ordinal fields, and 79 module/name string starts. String bytes are bounded to 256 and array rows to 128; the sites count string starts rather than individual character bytes. There are zero new function bodies and zero tiny helpers. The earlier wrapper/nearest-entry subset is not the complete denominator.

Validation: 10 independently authored synthetic metadata checks and 73 negative guards passed. Fixtures cover direct named/ordinal export, forwarder ordinal, alias names and absence; guards cover identity, prerequisite identity, PE/header truncation, table bounds/ordinal ranges, strings/forwarders, mapped bounds, budgets, fresh ignored paths and publication failure. Fixtures are synthetic PE metadata, not recovered protocol. Original executable execution, asset corpus reads and old body suite reruns are all false. The bounded reader's rejection policy is a verifier policy, not a recovered client parser policy.

Next probe: inspect whether the baseline PE base-relocation directory exists and can provide typed pointer locations; then correlate one terrain-group RTTI/vtable or constructor-registration anchor from existing inventories before selecting a bounded indirect producer window. Stop with explicit absence if relocations or a typed anchor are unavailable; do not infer a caller from neighboring export addresses.

## Authorization and reproducibility

Lead approved exact70 METADATA-ONLY amendment: 79 distinct address leads (wrapper plus 78 nonzero export targets) and 315 raw metadata sites (directory entry/header, 234 indexed table fields, at most 79 name/module strings). No export-target body expansion or new indirect-reference census. Original 12/12 instruction-body discovery cap remains separate.

The round70 frozen public baseline contains 4261 files, manifest SHA-256 `9abb3d6f79d4e1e910399db06eb62d6713b966338c7fe4c8bdfdfff8f4a6f722`, ZIP SHA-256 `dacae463495e3cb15506e0bff08817336a1ed62c374b9ed5d97990105cd639de`. Sync verified prior accepted work and preserved the worker HEAD and empty index. Static input: original `C:/Stardust-DEV/Stardust.exe`; no assets required. Cached Python, existing private pefile package, raw PE mapper, and independent synthetic fixtures were used.

The initial negative fixture used incorrect header field offsets; correcting the independently authored mutations made the intended malformed cases exercise the correct fields. Final replay passes 73 guards and 10 positive checks. No evidence claim depends on the failed fixture attempt.

Canonical conclusion: [export boundary contract](../../docs/TERRAIN_SHADER_LOOKUP_EXPORT_BOUNDARY_CONTRACT.md). Reproduce with [portable replay](../../../SWG-Godot-Client/poc/round70-tests/Assets/Invoke-SelectedRule.ps1), supplying explicit Python/Exe/Packages; output is fresh ignored private evidence. Shared ledgers/status and registries are reserved for lead reconciliation, not modified by this specialist.
