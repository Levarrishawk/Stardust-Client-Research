# Round28 TANO6 codec checkpoint

Date: October3,2026 UTC. Bounded question: what exact bytes do the passive U32
and CachedNetworkId vector consume in TANO6, especially command3 assignment,
and can an owned codec reproduce that grammar under deterministic caps?
Stopping condition: specialization verified, native/independent positive and
negative vectors pass, documentation and scoped workspace validation complete.

## Inputs and tools

Round28 manifestSHA256:
`C6945D7365F1C6D2B3585AEBEEC57D10A29FB9C0FCBD7B0E586A0D58125AB654`.
ZIP SHA256:
`E0EB172E1B2429DE49C55857BC14BAA0CBE75F5AA825BFCBA88762B8483CA011`.
Full2666-member list/path/hash/size/archive+target+ancestor link preflight
preceded overlay; postverify preserved worker HEAD/index/unlisted private data.
Original executable SHA256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Hash-gated PE/Capstone static audit, bundled Python, offline .NET8 and local
server source were used. No original execution, live contact or downloads.
Detailed disassembly remains ignored/private; curated content is independently
written descriptions and compact addresses.

## Evidence and confidence

The [durable contract](../../docs/TANGIBLE_VIEW6_CODEC_CONTRACT.md) records exact
instruction addresses, RTTI/vtables, body grammar, budgets and server symbols.
Observation: vtable015E0614 identifies CachedNetworkId vector; exact specialized
snapshot006421A0/delta006423E0 consumes U32 headers and8-byte IDs. Both skipped
and applied assignment paths consume U16 N and all N U64s, advancing raw loop
N+1. Applied assignment performs all reads before the outer comparison, allowing
final overshoot followed by another field. Confidence: Confirmed by40 static
instruction anchors plus vtable/RTTI. Interpretation: a raw ordered consume
codec is supported independently of vector-state semantics. Consequence:
retain raw extent/counter/order/duplicates; do not reuse integer-vector policy.

Observation: current tangible source serializes passive0x76 and defender count/
counter/IDs, with active0/1/2/4 mutation commands; inspected setDefender swaps
order via two replacement commands. Original command3 acceptance is broader
than these inspected source producers. Confidence: Confirmed for source
behavior; actual wire occurrence/live reachability Unknown. Consequence:
keep original/parser acceptance separate from emitted server corpus.

## Implementation and validation

New owned `TangibleView6Codec.cs` contains models and baseline/delta APIs in one
allowed path. Exact family/view/header/tails and unsupported fields/commands
reject. Count metadata remains raw. Global payload/field/operation/assignment/
ID/decoded preflight precedes typed allocation or encoded buffers. Codec does
not apply defender state, resolve IDs or run callbacks.

Independent Python fixtures: six complete baselines and eleven ordered deltas,
including duplicates/full U64 patterns/zero/all command forms/empty assignment/
zero raw extent/assignment overshoot followed by scalar/repeated fields.
Final native pass:17 exact,394 bounded negatives,148 global cap checks;
independent exact wire/DTO oracle passes. Static audit passes40 anchors and
vtable/RTTI. Prior27 native/independent mixed replay regression passes22 applications/23 datagrams/nine states/224 negatives. Shared smoke builds with zero warnings/errors and passes. Repository validator:1268 Markdown files, zero broken/private-output links;2618 unique provisional addresses and345 packet rows unchanged. Snapshot scope review finds exactly eight allowed additions and no changed baseline bytes among2666 files. Python AST/UTF-8/LF/EOF/whitespace checks and `git diff --check` pass. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`; index remains empty. Existing/unlisted private data is preserved.

## Proposed canonical names for lead review

Existing snapshot/delta parser names at006421A0/006423E0 remain unchanged.
Proposed additions, with Confirmed specialization evidence:

- `autodelta_cached_network_id_vector_snapshot_serialize_VA_00641690`: vtable
  slot1, U32 count/counter,8-byte ID output.
- `autodelta_cached_network_id_vector_delta_serialize_VA_00641700`: slot5,
  U8 operation/U16 index-or-assignment-length/U64 value serialization.
- `cached_network_id_archive_read_VA_0043CEB0`: assignment helper reading8
  bytes and advancing archive cursor8, then constructing cached identity.

No registry/UNKNOWN/global ledger edits are made in this worker scope. Lead
reconciles names and canonical acceptance records after review; metrics do not
change solely from codec fixtures.

## Exact next probe

Trace specialization state effects and counter arithmetic separately, including
stale/overlapping counters, invalid/remove/add/set indices, assignment growth,
callback order and cleanup before any defender model/world integration.
The present milestone closes byte consumption and owned serialization only.
