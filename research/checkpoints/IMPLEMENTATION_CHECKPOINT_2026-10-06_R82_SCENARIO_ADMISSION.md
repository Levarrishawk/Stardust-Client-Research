# Round82 strict scenario admission, 2026-10-06

## Question and stopping condition

Can one engine-neutral immutable parser reject malformed configuration before preview construction and compile deterministic per-run argument arrays without retaining mutable input? Stop after the exact shared Schema1 compiler, independent authored positive/negative tests, full Core build and API handoff. User authorization is exactly Round82; stale frozen entry wording about no82 does not override that authorization.

## Input identity and tools

Accepted81 frozen input has 4687 public files, manifest SHA256 `e142a5e7e1c2846a9f7727a1fe05c82a0aab7735b47aff0167ccc7604ac01ddf`, archive SHA256 `a4a13af44ae9b35c42eb87f7f1a361f11c7e810fc63ce2f65147e7cc84e29990`. Trusted sync verified prior81 seals and preserved HEAD/index/private state. Orientation included START_HERE/latest81 session, status/ledger/unknown, configured and persistent contracts/checkpoints, curated registry header and existing strict settings parser. No EXE/VA or original asset bytes are inputs.

Tools: .NET SDK8.0.300, cached packages, cached Python workspace validator; all build artifacts and authored fixture files remain private. Standalone tests compile the actual new parser source without external packages or Godot. Full Core integration uses its exact cached BCnEncoder dependency. No shared host, launcher, point target or canonical registry changed.

## Findings and implementation consequences

- Confirmed: exact keys and recursive duplicate detection reject decoded duplicate spellings; strict UTF8, byte/depth limits and JSON types reject coercion. Consequence: caller receives a complete admitted configuration or an exception.
- Confirmed: all required paths resolve against the explicit fully qualified manifest directory, independent of cwd. Directory/file existence checks occur during Parse, before a caller can construct a preview from the result. No shell or socket is invoked.
- Confirmed: the output order is sixteen input pairs, selected point profile pair, then true flags. Every array is a fresh defensive copy; mutating bytes or a prior array does not alter the manifest.
- Confirmed: metadata admission does not pin files or parse actual Version13 semantics. Consequence: existing child admission remains authoritative, and launch-time missing/changed files remain possible.

See [durable schema/API contract](../../docs/OFFLINE_SCENARIO_MANIFEST_CONTRACT.md) and [test source](../../poc/round82-tests/Runtime/Program.cs). Confidence applies to this authored modern parser, not legacy parity, protocol coverage or function labels.

## Verification and limits

Standalone test output: Passed=true, Checks=28, NegativeCases=90. Full Core and standalone builds: zero warnings/errors. Positive cases include run32, inclusive262144-byte admission, all/mixed/no flags, punctuation paths and cwd independence. Negatives include missing every required key, malformed UTF8/JSON/depth/duplicates, unknown fields, URI/device/drive-relative paths, missing/wrong-kind inputs, run/index bounds and scalar coercion.

Initial build failed reading inaccessible user NuGet.Config; explicit private APPDATA resolved it without network. An initial baseline copy used the wrong directory; the verified frozen manifest was then copied from its actual frozen subdirectory. No data was overwritten by those failed reads.

No factory, native scene, asset decoding, scenario producer or full-resource launcher replay was executed. Symlink resolution, launch-time filesystem changes and physical OS input are outside this bounded result. Canonical status/ledger integration belongs to lead.

Lead review added the same4096-character cap to resolved full paths before existence checks, with one focused lexical-within-cap/resolved-over-cap negative. Both build targets and all28/90 final checks were rerun after this correction.

## Exact next action

Lead consumes `Stardust.Core.Offline.OfflineScenarioManifest.Parse(byte[] utf8, string manifestDirectory)`, `RunCount`, and `ArgumentsForRun(int oneBased)` before preview creation, rejects mixed raw overrides, then executes the independently authored scenario/native join. Worker stops after the exact assigned seal.
