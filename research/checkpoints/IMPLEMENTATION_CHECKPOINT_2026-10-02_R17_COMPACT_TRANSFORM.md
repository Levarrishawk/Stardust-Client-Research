# 2026-10-02 R17-N: bounded compact transform codec

Question: can the two documented top-level compact APPLICATION boundaries decode/encode exact raw values and native coordinates with deterministic negative/ownership tests, without inventing yaw/interpolation or integrating an unfinished motion store? Stop after standalone codec, independent fixtures and exact API handoff.

## Inputs and orientation

Round17 public snapshot:2,272 files; manifest SHA-256 `f7af15cbcd8ac9a219a03cf7ae21502c5c69b9fecca16ed3ef3a20eb1be6d832`; ZIP SHA-256 `c4b37e54c60d74c8b737a5918b97eca0d08dcc60e50ecc5c10929e258db5ae08`; snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`. Checkout HEAD `65de8c88141071744dc75594be126b3bab3696a5` preserved separately. Full hash/member/count/path containment/archive and destination-parent no-symlink/byte preflight completed before overlay, all outputs verified afterward. Listed bytes only; ignored/unlisted/.git/index preserved, no deletions.

Read AGENTS, START/latest16 report, frozen motion interface, packet/controller contracts and existing movement checkpoints, current status/ledger/UNKNOWN and curated packet rows. Reviewed exact local Stardust-3 constructors and their heading helper as bounded correlation, not a new original-client probe. Server HEAD `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`; exact file hashes/symbols are recorded in the [durable contract](../../docs/COMPACT_SCENE_TRANSFORM_CONTRACT.md). No VA/direct original probe was needed, so no original executable identity claim. No original assets or executable execution, live/socket/DNS/server/capture/fuzz work, main/shared/Git mutation or downloads.

## Observations, confidence and consequence

Observation: both constructors and inherited client durable evidence agree on count8, fixed little-endian identity/coordinate/counter fields and two signed bytes; parent form prefixes parent ID and changes scale4 to8. Interpretation: one nullable-parent immutable raw DTO discriminates exact fixed26/34-byte APPLICATION messages, including present parent0. Confidence: Confirmed inherited structure, server source correlation and independently verified replacement execution. Consequence: preserve rawS16/S8/U64/U32 and map X/Z/Y to nativeX/Y/Z; no object lookup, ordering or resolution inside the codec.

Observation: server special-angle helper produces a0..100-scaled value using6.283, but that alone does not prove client quaternion interpretation. Lead requested the raw-compatible API and external yaw policy; yaw/speed conversion is deliberately absent. Partial coordinate-only server parse helper arithmetic/cursor assumptions are preserved as an unresolved source concern rather than emulated. Encode uses raw values, avoiding invented float quantization/overflow. Unknown interpolation, speed units and noncanonical direction semantics remain explicit.

## Implementation and validation

Public `CompactSceneTransformMessage` has ObjectId, ParentId, RawX/RawZ/RawY, MovementCounter, SpeedRaw, DirectionRaw, CoordinateScale and NativePosition. `CompactSceneTransformCodec.Decode(ReadOnlySpan<byte>)` and `Encode(CompactSceneTransformMessage)` are stateless, bounded and engine-neutral. Header/opcode/exact-length checks precede field reads; no variable allocation or external lifecycle. Whole message fixtures are independently authored synthetic bytes.

Native offline .NET restore/run and Python oracle pass522 positives and19,324 malformed probes. Both scales/endian/sign extrema and all256 speed/direction byte values per branch, parent0, zero/high IDs/counters, every truncated prefix, trailing bytes, header mutations, wrong branch sizes, null encode and caller/output ownership pass. Fixture hash `2FC85EDF2CE45148096E11F0099463D2D36F6CBA59E04B7521D630F94F83ABCA` (UTF-8/LF). Exact producer/runner/oracle commands and export API are in the durable contract; logs/exports remain private. Source fixture peer has no C# imports and uses no original inputs.

Shared Core.SmokeTests restore/build passes offline with zero warnings/errors, without shared project/Program edits. Workspace validator passes:1,157 Markdown files, zero broken/private links,2,602 provisional rows/zero duplicates,345 packet rows and no public files at least25 MiB. Format/Python syntax and `git diff --check` pass; baseline line-ending advisories retained privately. Mechanical scope audit finds exactly seven authorized additions and zero changed baseline bytes, including frozen motion interface and canonical registries. Final status857 entries inspected and preserved relative to older checkout; checkout HEAD65de8c88141071744dc75594be126b3bab3696a5 and empty index unchanged. New files are ASCII-compatible UTF-8/LF without trailing whitespace. Motion routing, yaw and integration execution are intentionally unrun/outside this bounded codec proof.

## Exact scope and lead handoff

```json
[
  "poc/Stardust.Core/Protocol/CompactSceneTransformCodec.cs",
  "poc/Stardust.Core.SmokeTests/CompactSceneTransformScenarios.cs",
  "docs/COMPACT_SCENE_TRANSFORM_CONTRACT.md",
  "research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R17_COMPACT_TRANSFORM.md",
  "poc/round17-tests/Networking/Networking.csproj",
  "poc/round17-tests/Networking/Program.cs",
  "poc/round17-tests/Networking/peer.py"
]
```

Lead registration: `CompactSceneTransformScenarios.Run(peerPath, exportPath)`. Driver takes two paths; Python uses `--generate` or `--peer ... --verify ...`. Motion integration should consume the DTO's NativePosition and retain SpeedRaw/DirectionRaw; no worker world.Accept integration or same-round dependency. Shared packet count, canonical files and frozen motion interface are untouched. Next suggested probe is explicit incarnation-aware routing into the joined motion store with externally chosen diagnostic yaw/parent policy. Stop after Round17 handoff.
