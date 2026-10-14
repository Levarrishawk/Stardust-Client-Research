# Tangible view6 owned codec contract

## Scope and API

[Implementation](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/TangibleView6Codec.cs) defines
`TangibleView6Codec.DecodeBaseline/EncodeBaseline` and `DecodeDelta/EncodeDelta`.
Decode overloads accept a whole application span or an owned replication
envelope. DTOs are `TangibleView6Baseline`, `TangibleView6Delta`,
`TangibleView6Update` and `TangibleView6Command`; each collection is frozen.
This is a byte/consume contract without defender-state application, object
resolution, combat callbacks, UI or world integration.

Only generic Baselines/Deltas application opcode with tag`TANO` (`54414E4F`),
view6 is accepted. The established outer application header is23 bytes,
followed by U16 declared operand metadata and fields. All multibyte fields use
little endian. Metadata is preserved, including0/65535; it does not determine
field count. Empty deltas, unknown fields/operations, truncation and trailing
unconsumed bytes reject. Other families/views and known derived legacy-tail
allowances are outside this exact two-field codec.

## Complete baseline and ordered delta grammar

The baseline has exactly two fields in order:

| Field | Bytes | Meaning retained |
|---|---|---|
| 0 | U32 | Passive raw common word; historical semantic name Unknown |
| 1 | U32 count, U32 counter, count times U64 | Ordered raw defender IDs, including duplicates, zero and all bit patterns |

A delta repeats U16 field index followed by its body; field0 consumes U32.
Field1 consumes U32 raw loop extent and U32 counter, followed by commands:

| Opcode | Body after opcode U8 | Raw loop advance |
|---:|---|---:|
| 0 | U16 index | 1 |
| 1 | U16 index, U64 ID | 1 |
| 2 | U16 index, U64 ID | 1 |
| 3 | U16 assignment length N, N U64 IDs | N+1 |
| 4 | Empty | 1 |

Start the raw loop cursor at0 and consume commands while cursor is below the
raw extent. Assignment reads all N IDs and advances N+1, even when the final
cursor exceeds the raw extent. The next bytes then belong to the next U16
field index. A zero raw extent has no commands. U16 assignment length is not
an insertion index; the owned command uses `Index=0` for assignments/clear and
retains values separately. The encoder rejects commands beyond a finished
extent and missing commands before an unfinished extent.

This grammar is directly specialized to `CachedNetworkId`; it is not inferred
from the integer wounds vector. Raw counter values are retained without
applying a counter-skip or vector mutation policy. The original parser has
counter-dependent skipped/applied paths, but both consume the characterized
command bodies. Unknown opcode rejection is the replacement's strict modern
policy; the original applied switch falls through for unknown values.

## Original executable evidence

Baseline SHA-256 is
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
The reproducible [static audit](../../SWG-Godot-Client/poc/round28-tests/Networking/static_audit.py)
verifies40 instruction anchors, RTTI and vtable slots before reporting facts:

- `015E0614` RTTI is `Archive::AutoDeltaVector<CachedNetworkId,Archive::DefaultObjectType>`.
  Slots1/2 are snapshot serialization/parsing `00641690/006421A0`; slots5/6
  are delta serialization/parsing `00641700/006423E0`.
- Tangible constructor `0063F079` takes object`+2F0` and `0063F082` calls the
  view6 registrar `005554C0`. This is separate from the corrected view3
  integer component set. [Earlier ownership correction](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_TANGIBLE_COMPONENT_WIDTH_CORRECTION.md).
- Passive snapshot/delta `00542DA9/00542DC9` read4 bytes. Its null callback
  ownership is established by the [common-field checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_COMMON_VIEW6_OPAQUE_FIELD.md).
- Snapshot parse `00642241/0064225E` reads two4-byte headers; `0064228E`
  reads8 bytes per ID and `006422A3` advances the archive cursor8. Serialization
  at`006416DA` also writes8 bytes per element.
- Delta parse reads two4-byte headers at`00642421/00642435`; skipped commands
  read U8/U16 and U64 at`00642470/00642484/00642502`. Skipped assignment at
  `006424B0` advances the raw loop per ID before its8-byte read;`00642544`
  provides the additional command advance.
- Applied switch table`00642A00` maps0..4 to
  `00642581/00642673/00642756/00642891/006429F4`. Assignment reads U16 at
  `00642891`, increments the raw cursor at`0064297A` per ID and calls
  `0043CEB0` at`0064297F`; that helper reads8 bytes at`0043CEEA` and advances8
  at`0043CEFB`. The full assignment loop finishes at`006429A3` before the
  outer advance/comparison at`00642643/00642649`. This directly establishes
  final assignment overshoot rather than a guessed operation-count model.

Observation: exact original instructions establish widths, ownership and
consume extents. Interpretation: ordered raw IDs and counts can be decoded
without executing state callbacks. Confidence: Confirmed for these byte
boundaries; counter/state application and live reachability remain unimplemented.

## Server correlation

Current local Stardust/Core3 source paths and symbols provide a separate source:

- `MMOCoreORB/src/server/zone/packets/tangible/TangibleObjectMessage6.h`,
  constructor: inserts0x76 then `insertDefenders`; the latter delegates to
  `DeltaVector<ManagedReference<SceneObject*>>::insertToMessage`.
- `MMOCoreORB/src/server/zone/packets/tangible/TangibleObjectDeltaMessage6.h`,
  constructor: generic delta tag/view6, no specialized convenience methods.
- `MMOCoreORB/src/server/zone/objects/scene/variables/DeltaVector.h`,
  `set/add/remove/removeAll/insertToMessage`: U32 count/counter, U16 indices,
  opcodes2/1/0/4, typed item serialization. `insertItemToMessage` line231
  delegates to `TypeInfo`.
- `MMOCoreORB/utils/engine3/MMOEngine/src/engine/core/ManagedReference.h`,
  `toBinaryStream` line267: writes `getObjectID()` as a long.
- `MMOCoreORB/src/server/zone/objects/tangible/TangibleObjectImplementation.cpp`,
  `setDefender` lines700–735: starts field1 then emits two replacements to
  swap the primary defender. This confirms server ordering semantics; the
  codec preserves order but does not implement that gameplay behavior.

No assignment producer was found in these inspected server paths. Original
assignment acceptance and source emission are distinct observations, neither
proves a captured packet occurrence. The prior [defender report](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_TANGIBLE_VIEW6_DEFENDERS.md)
provides source context; its integer-component defect wording is superseded by
the later correction.

## Modern budgets and ownership

`TangibleView6Limits` caps payload, fields, actual command records, aggregate
assignment values, aggregate IDs and retained decoded byte charge. Baseline
charge is12+8N; delta charge is its complete field-byte extent, including field
indices and command framing but excluding the outer U16 metadata/header.
These deterministic wire-based charges are modern budgeting units, not managed
heap sizes or original legacy capacities. Assignment overshoot affects raw loop
extent; it does not bypass actual operation, ID or assignment budgets.

Decode scans the complete bounded grammar before allocating typed collections;
the existing envelope owns a bounded copy of fields. Encode preflights every
model and aggregate before serializing. Both paths reject one-less global caps,
including totals spread across repeated field1 records. Public constructors
bound/freeze collection enumeration; ID bit patterns are U64 data, never pointers.
Hard policy ceilings are8MiB payload,4096 fields,65536 commands,65535 aggregate
assignment values,1048576 IDs and16MiB decoded charge.

## Reproduction and next boundary

[Python producer/oracle](../../SWG-Godot-Client/poc/round28-tests/Networking/peer.py) hand-packs17
positive fixtures and6 malformed bodies. [Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TangibleView6CodecScenarios.cs)
check exact owning round trips, DTO rows, metadata, envelope overloads,
selected malformed headers/prefixes, source-array freeze and exact/one-less
budgets. No exhaustive million-prefix matrix is added.

```powershell
python poc/round28-tests/Networking/peer.py --generate local-private/round28/peer.json
dotnet build poc/round28-tests/Networking/Networking.csproj -p:UseAppHost=false
dotnet poc/round28-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round28/peer.json local-private/round28/export.json
python poc/round28-tests/Networking/peer.py --peer local-private/round28/peer.json --verify local-private/round28/export.json
python poc/round28-tests/Networking/static_audit.py C:/Stardust-DEV/Stardust.exe --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages
```

Next: separately specify defender-state application with direct specialization
counter/index/growth/callback evidence and negative state transitions. Do not
attach generic integer-vector application rules or world/UI behavior to this
codec. This milestone changes no research/function/protocol coverage denominator.
