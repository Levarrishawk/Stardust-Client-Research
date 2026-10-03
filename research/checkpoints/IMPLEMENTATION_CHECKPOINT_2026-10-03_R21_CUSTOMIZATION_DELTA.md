# Round21 field4 delta codec - October3,2026

## Question and stopping condition

Can a bounded CREO:3 field4-only delta codec preserve exact outer string bytes/spelling, repeated ordered updates and raw declared metadata while rejecting malformed extents/budgets atomically? Stop at frozen-plan API, owned span/envelope codec, independent fixtures and reviewable isolated handoff. Mixed-field routing, journal/current-incarnation state and Assets projection are separate milestones.

## Inputs, identity and scope

Round21 public snapshot2,406 files. Manifest SHA-256 `b154746d47a2facecbbb7a9f527d29d0c076fd45685a5a06e36d0c78934cd17d`; ZIP `6cf73b0a1ad5e32465ade4f9699bee405a29e6f1f315bf01a01f654eac8e6076`. Exact unique entries/size/hash, path containment and archive/ancestor/target symlink absence were checked before overlay, then all written members verified. Snapshot HEAD1fc55dd89cd23ce3af49e3fe471210bdebe9735c is authoritative independently of unchanged checkout HEAD65de8c88141071744dc75594be126b3bab3696a5. Private/unlisted bytes and empty index preserved.

Required orientation: START/latest accepted20, inherited ingress/packed/tangible contracts and checkpoints, current status/ledger/UNKNOWN, curated packet/name rows and existing scalar/component/envelope codec count/budget policy. Frozen CreatureView3CustomizationDeltaPlan, ReplicationByteString.Create and raw-source interface were read without edits. Only owned new files are used; no dependency on another worker's same-round implementation. No new VA claims/probe, original execution, asset extraction or live inputs.

Server source correlation HEAD9a8529ef8291efc5ca1b41d9927f0e88d64b79bb. TangibleObjectDeltaMessage3.h:23..26 emits addAsciiUpdate(4,app); SHA-256 C7883C45E68863D8ADDBBC197389D798A7C43FEC0453D3D5327D79ED8383ED58. DeltaMessage.h:78..80 calls startUpdate(type), insertAscii(val.toCharArray()); SHA-256005F4E15FE6232D75569C37B26B9B659011EA0750605DE80AA397C1593D1036A. Paths are exact local MMOCoreORB/src/server/zone/packets/tangible and packets respectively; inherited client outer evidence remains the primary grammar authority. Server char-pointer emission can truncate embedded NUL and does not narrow client outer acceptance.

## Findings, confidence and consequence

Observation: new CreatureView3CustomizationDeltaCodec offers Decode(span,limits), Decode(owned envelope,limits), Encode(plan,limits) and EncodeEnvelope(plan,limits). It accepts only Delta/CREO/view3, at least one U16 index4/string update and exact whole extents. Strings use U16LE length orFFFF/U32LE extended length, retaining raw bytes and UsesExtendedLength. Extended-small/empty and embedded zero/high/invalid-inner bytes round-trip exactly. Every repeated update is kept in order.

Observation: scalar/component prior codecs parse until field-span exhaustion and preserve DeclaredCount as raw metadata. New codec follows that implemented policy and frozen plan; mismatched0/1/2/255/256/FFFF counts are accepted/retained in fixtures, not guessed into a field-loop bound. Confidence: Confirmed prototype source/execution policy; no new original generic-dispatch edge claim.

Observation: independent caps cover payload including count/index/prefix/content; maximum fields0..4096; per-string nonnegative int and aggregate nonnegative long retained content bytes. Aggregate excludes fixed metadata/CLR overhead; field and wire caps independently bound those allocations. All copies follow per-string/aggregate/remaining-extent checks, and encode preflights complete length before output allocation. Any unsupported late field/incomplete prefix/extent/tail/zero-update/budget error publishes no plan. Input/list/output/envelope copies remain independent.

Interpretation/confidence: Confirmed bounded engine-neutral byte behavior through native and independent fixtures. Atomic failure and field4-only routing are explicit replacement policy. Consequence: feed ordered plans to the later single-consumer journal; do not collapse raw updates and lose earlier valid writes before an invalid later application. Inner packed validation, empty bypass/default/range semantics, generation/freshness/reentry/capacity lifecycle belong to that later adapter. Mixed4+otherfields reject here without changing accepted composite codec or claiming original mixed packets illegal.

## Executed verification

- New runner:21 exact cases;2,098 negative probes;8,376 ownership/exact-cap checks.
- Independent Python producer/oracle imports no production code: matches all object/count/update/byte/prefix/order receipts and rejects every malformed producer fixture.
- Extent coverage: empty/embedded/all-octets and repeated updates; lengths1/2/254/255/256/65534/65535/65536;4096 updates; raw declared mismatch. Exhaustive prefixes for small fixtures; beginning/header and final16 prefixes for large fixtures. Late mixed index/truncated short+extended/UINT_MAX extent and all independent caps reject.
- Shared smoke project: zero warnings/errors; default offline suite passes.
- Relevant baseline regressions: accepted packed773/11,995/6,699 and outer baseline6/947 pass unchanged.
- Workspace/link/whitespace and exact baseline-relative scope pass; seven allowed additions, no changed baseline bytes; preserved HEAD/index. No staged/generated/private material promoted.

The [durable contract](../../docs/CREATURE_VIEW3_CUSTOMIZATION_DELTA_CONTRACT.md) records complete limits/ownership/error/count policy and copy-paste producer/native/oracle commands. Public CreatureView3CustomizationDeltaScenarios.Run(peerPath,exportPath) is standalone; lead owns shared registration. Existing toolchain/cached .NET8.0.300 and source-cleared NuGet config only, no download. Logs/peer/exports and full diff/status/scope receipts stay ignored/private.

## Changed paths and exact resume

1. `poc/Stardust.Core/Protocol/CreatureView3CustomizationDeltaCodec.cs`
2. `poc/Stardust.Core.SmokeTests/CreatureView3CustomizationDeltaScenarios.cs`
3. `docs/CREATURE_VIEW3_CUSTOMIZATION_DELTA_CONTRACT.md`
4. `research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_CUSTOMIZATION_DELTA.md`
5. `poc/round21-tests/Networking/Networking.csproj`
6. `poc/round21-tests/Networking/Program.cs`
7. `poc/round21-tests/Networking/peer.py`

Next lead-owned action: register the scenario and pass issued plans to the current-incarnation ordered raw journal. Mixed-field dispatch and full live appearance projection remain separate. No shared Program/project/navigation/status/ledger/registry/frozen interface edit was made. Original player values remain Unknown; metrics89%/94%,2602 names and345 matrix rows unchanged. Worker stops after this one milestone.
