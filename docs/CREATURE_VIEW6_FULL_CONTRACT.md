# Ordered complete CREO6 delta codec and immutable state

Round37, 2026-10-03. This bounded adapter composes all eighteen fields of the accepted complete baseline. It uses the frozen public [scalar state](../poc/Stardust.Core/Protocol/CreatureView6State.cs), [HAM kernel](CREATURE_HAM_VIEW6_CONTRACT.md), [wearable kernel](CREATURE_WEARABLES_VIEW6_CONTRACT.md) and [defender kernel](CREATURE_DEFENDERS_VIEW6_CONTRACT.md). The earlier [scalar/HAM composite](CREATURE_VIEW6_COMPOSITE_CONTRACT.md) retains its API and twenty-four-byte journal policy.

## Evidence and compatibility boundary

Observation: the accepted baseline/scalar, HAM, wearable and defender contracts establish the eighteen registrations, serializers, counter/skip rules, equality, defaults and local notification ordering independently. The [complete codec contract](CREATURE_VIEW6_CODEC_CONTRACT.md) and linked narrow contracts retain the original executable identity and exact evidence. This round makes no new executable-address or original callback claim. Interpretation: ordered public projections can compose those already established boundaries into a complete immutable model. Confidence: Confirmed for the independent synthetic fixtures; original field semantics inherit the confidence and limitations of those contracts. Consequence: preserve wire bytes and ordered mutation behavior, while leaving target resolution, owner callbacks, world incarnation and live interoperability to separate work.

[CreatureView6FullCodec](../poc/Stardust.Core/Protocol/CreatureView6FullCodec.cs) exposes a frozen ordered Delta and a four-way Update union. A union contains exactly one Scalar, Ham, Defender or Wearable projection; Index is derived from that child. The raw U16 declared count remains independent of actual field count. Decode and Encode accept only nonempty CREO/view6 deltas and retain repeated/interleaved fields. Full baselines continue through the existing complete codec.

All multibyte values are little endian. Envelope structure remains the accepted replication envelope, including U64 recipient, raw CREO tag, view6 and U16 metadata. Field bodies are:

| Field | Body |
|---|---|
| 0, 8, 11 | U32 raw scalar |
| 2 | U16 raw scalar |
| 3, 4, 16 | byte string: U16 short length or FFFF plus U32 extended length, then exact bytes |
| 5, 6, 9 | U64 raw identity |
| 7 | ordered U64 inviter identity and U64 invitation counter |
| 10, 17 | U8 raw scalar |
| 12 | raw S32 performance type bits |
| 1 | U32 extent/counter and defender commands containing raw U64 values |
| 13, 14 | U32 extent/counter and commands containing S32 HAM values |
| 15 | U32 extent/counter and commands containing customization string, U32 arrangement, U64 identity, U32 template CRC |

Vector commands use U8 opcodes: remove0 has U16 index, insert1/set2 have U16 index plus one element, assign3 has U16 count and that many elements, clear4 has no body. Cursor advances one per command and another k for assign. A zero extent has no commands; the final assignment may overshoot the declared extent. Empty plans, unsupported fields/opcodes, incomplete bodies, extra commands beyond extent, trailing partial fields and wrong envelope kind/tag/view/size reject. Raw string spelling, embedded zero/nontext bytes, full-width IDs, duplicates and ordering survive roundtrip.

The generic envelope owns a bounded field copy before specialized scanning; the public envelope adapter then takes a bounded inspection copy. Specialized Decode scans all fields and budgets before allocating command/value models, then performs an ownership pass. Encode and Apply measure the whole typed plan before stream/candidate copying. Constructor freezing is separately hard bounded. These are logical budget policies, not measured CLR heap ceilings.

## Full state, counters and receipts

[CreatureView6FullState](../poc/Stardust.Core/Protocol/CreatureView6FullState.cs) retains only the immutable complete Model, checked U64 Revision, DecodedBytes and copied policy. FromBaseline advances revisionSeed once; Reseed requires the same recipient and advances once. Apply computes a private candidate and advances the outer revision once, including valid skipped/no-op plans. The receipt contains Current, raw DeclaredCount, owned ordered Events and WorkUsed; it retains no Previous, input plan, private kernel state or callback/resource handles. Failures preserve the input and previously published results.

Each vector field occurrence computes `skip=min((extent-incoming+actualLocalCounter) mod 2^32,extent)`. A command is wholly skipped when its starting cursor is below skip, including assignment crossing skip. Local counters never resynchronize to incoming counters. Applied commands are delegated one at a time to the corresponding public frozen kernel with a private forced-zero-skip extent/counter. Those private counters preserve actual mutation increments and wrap; repeated fields start from the current model. Skipped commands still contribute to whole-message consumption, and each vector occurrence has an explicit seed charge even if entirely skipped.

Defender equality compares full U64 IDs; HAM compares S32 values; wearable set equality compares existing identity only, so changed customization/arrangement/CRC with the same identity is ignored. Beyond-end set still grows even for a default value. Defender/HAM gaps are zero; wearable gaps retain the accepted short-empty customization, arrangement FFFFFFFF, identity0 and CRC0. Assignment changes and advances k+1 even when equal/empty. Nonempty clear advances1; empty clear and invalid remove ignore. Original ownership/notification effects are represented as modern descriptions only.

A new full event has Kind, Field, UpdateOrdinal, nullable Opcode/Index, nullable U64 OldId/NewId and nullable S32 OldValue/NewValue. Scalar assignment emits Assignment once. Defender remove/add/set emit the respective item description followed by DefenderChanged; assignment emits DefenderChanged; clear emits old removals in ascending index then DefenderChanged. HAM/wearable emit one mutation description for each actual mutation. Skips, equality ignores, invalid removes and empty clears emit none. Wearable descriptions carry old/new identity; their complete resulting values are in Current.Model, and events do not retain historical customization buffers.

Every full event costs **48 logical output bytes**, including its discriminators and nullable payload allowance. This fixed modern charge is independent of struct layout and leaves existing eight/twenty-four/thirty-two-byte child journal policies intact. Final receipt charge is full DecodedBytes +48 times Events.Count. No original notification is invoked.

## Aggregate limits and intermediate construction

The message policy bounds whole payload, fields, commands, elements across all vector families, maximum individual string, aggregate string contents, decoded bytes and consumption work. Decoded message units are field bytes+10; payload includes its two metadata bytes. Message work is fields+commands+elements+string content bytes. Decode/Encode use the same exact accounting. All consumed values count, including skipped/equality-ignored commands.

The model policy bounds updates, each vector, all four vectors together, each retained string, aggregate retained string contents, complete model bytes, events, logical output and work. Complete model bytes are102 fixed bytes +8 per defender +4 per HAM/maxHAM +16 per wearable, plus actual short/extended prefixes and content for the three scalar strings and every wearable customization.

Before each applied command, the adapter predicts the candidate vector count, complete bytes/strings and exact event multiplicity. Remove subtracts the removed width/entry; clear removes the vector contents. Candidate state plus prior and newly mapped48-byte events must fit. Untouched partitions and prior full events are reserved from each child's element/string/state/output allowances. Every current/candidate model is checked; growth before later shrink cannot hide a peak.

Frozen kernels additionally retain their accepted conservative **construction** check: valid remove/clear reserves the old narrow vector plus the newly constructed child journal (24 per HAM event,32 per defender/wearable event). This remains enforced by the unmodified child output limit. Therefore the exact full output ceiling is the maximum of initial/current full state plus prior full48 events, child construction state plus prior full48 events plus its child journal, and candidate/final full state plus all full48 events. The adapter does not invent an old-state-plus-new48-event reservation, and does not widen child limits to evade the narrower construction check. Long wearable removal/clear fixtures distinguish these peaks.

Full Apply work is explicitly: (1) initial full validation18+initial elements+initial string contents; (2) whole message consumption work; (3) per vector occurrence seed copy (defender count, combined HAM counts, or wearable count+customization content); (4) every applied command's frozen one-command Apply work, including its consumption/copies/mutation/event charges; (5) for each scalar, seed validation18+current full elements+strings and frozen Apply19+resulting full elements+strings. Skipped commands have no child Apply charge. Repeated consumption/copies are intentional public-adapter overhead, not an assertion of minimal original work. Remaining work is passed to each child. All arithmetic/revision checks reject overflow. Fixed hard ceilings bound validation scans and wrapper overhead separately from logical work.

## Reproduction and limits

The [Python oracle](../poc/round37-tests/Networking/peer.py) independently packs/parses all eighteen fields and derives the complete raw model, counters, ordered descriptions, work and every capacity peak. It reuses accepted independent Python vector oracles only; native exports never generate expected values. [Native scenarios](../poc/Stardust.Core.SmokeTests/CreatureView6FullScenarios.cs) exercise exact/one-less caps for Decode, Encode and Apply, malformed/truncated adjusted prefixes, aliases at every collection/string boundary, immutable branches, foreign recipients, reseed/revision overflow and late failure preservation.

The deterministic corpus has97 cases: accepted vector edge corpora with scalar interleaving, earlier mixed scalar/HAM cases, all18 fields with counter wrap, cross-partition growth/shrink, and remove/clear for all families with prior events and retained long strings. Separate actual-Core-assembly and source-linked standalone runners agree with the oracle. Detailed results, input pins and commands are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_CREO6_FULL.md).

Next action: prepare an incarnation-aware full-union store/world publication seam that retains this48-byte policy, rejects stale preparations and validates historical event/resource ownership. Original callbacks, target/resource resolution, complete gameplay effects, live traffic and complete replacement-client readiness remain outside this milestone.
