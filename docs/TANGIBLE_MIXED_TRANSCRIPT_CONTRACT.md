# Bounded mixed CREO3/TANO3 transcript

## Contract and evidence boundary

This synthetic offline replay asks whether accepted family-specific codecs,
SOE transport and the standalone tangible store preserve one mixed lifecycle
without relying on a world router or engine. It stops after an exact native
export matches an independent Python oracle, the prior named CREO/TANO wire
regressions pass and workspace checks complete.

The producer is peer.py (external historical reference); the native
consumer is TangibleMixedTranscriptScenarios.cs (external historical reference).
The schema builds on [complete TANO3 codecs](TANGIBLE_VIEW3_CODEC_CONTRACT.md)
and the accepted [SOE transport specification](SOE_TRANSPORT_PROTOCOL.md).
It adds no recovered protocol claim or coverage percentage. Original client
reachability, live-server compatibility and packet-capture provenance remain
unverified by this synthetic replay.

## Fixture format and consumer join

Version1 has `Synthetic=true`, `SceneGeneration=42`, a pinned public catalog,
`SessionResponseHex`, ordered `Applications`, `PacketsHex`, `WiresHex`,
reordered/duplicated `TranscriptHex` and `TangibleStage`. Application rows carry
`Name`, `Kind`, `Hex` and optionally a raw tangible `Expected` state.
Whole application headers, declared counts, family tags and byte encodings are
preserved. Metadata counts deliberately include0 and65535; they do not describe
the number of parsed updates.

`TangibleStage` is also exported separately: `Create`, `Baseline`, four named
`Updates` (`Name`, `Hex`), five `Expected` states, `BaselineDecodedBytes`,
`MaximumStoreBytes=512`, `LateUnsupported` and `IntermediateOverBudget`.
This matches the accepted26 stage shape. Names use authored byte references;
there is no recovered STF lookup or display-text assertion. A consumer may
retarget the object ID at offset6 in every lifecycle/replication application,
including negative probes, before transporting or decoding it. Retargeting is
an explicit integration transform and invalidates the original datagram/CRC
expectations; regenerate those for the new transcript.

The pinned catalog (external historical reference) SHA-256 is
`B945D2D4695FF37D54343F11A7DF3539A5AB36A2E5D13246969B04F92657492D`.
The producer checks both path CRCs independently. Creates reference catalog
human2937962913 and mission terminal2907007746, with object IDs
`F122334455667788` and `F122334455667789`. Synthetic transforms retain negative
zero, a subnormal float and the original X/Z/Y wire ordering.

## Successful replay

There are22 applications and23 SOE datagrams, including two fragments for the
complete18-field CREO3 baseline. The TANO3 baseline consumes all11 fields;
its historical components preserve `[9,9,-3]`, while current set storage
normalizes duplicates. Both objects cross end-baseline barriers before deltas.

Four named CREO deltas interleave four TANO updates: custom name/raw bytes and
hide0; string ID and show255; raw UTF-16 units including NUL/unpaired surrogate,
raw bytes and component changes; then restoration of equal original bytes.
A component no-op follows, then a genuine TANO baseline reseed, repeated end
barrier, destruction, recreation, full TANO seed/end and final show255/custom
name. Recreation changes creation incarnation within scene generation42; it
is not a scene-generation reset.

Nine tangible states have revisions1 through9. The no-op advances revision;
the reseed resets current state to baseline aliases. Restoring equal strings
allocates distinct owned storage and therefore still adds retained bytes.
The native test checks raw names, length encodings, customization, normalized
components/counters, visibility, scalar bits and alias-aware retained charges
against independently authored expected rows. Destroy removes the old store
entry; stale incarnation/snapshot application fails; close drains retained
bytes. CREO messages receive exact owned DTO and finalized-recipient checks,
but this fixture does not apply a creature world model.

The first datagram has a corrupted CRC; later sequence1 duplicates precede
sequence0, and sequence3 precedes2. The independent accepted14 oracle predicts
all delivery batches and ACK/selective ACK datagrams. Native framing and receive
output must match every packet, datagram, application and control byte.

## Bounded negative cases and ownership

Late TANO field11 after valid raw customization rejects during decoding.
A valid plan grows customization to513 bytes then clears it; the512-byte store
rejects the intermediate candidate without changing revision, current snapshot
or retained charge. Neither probe is included in the successful transcript.

Selected short prefixes, trailing bytes and wrong count/opcode/tag/view/size
headers reject; explicit CREO/TANO sibling baselines reject. A sequenced packet
without a complete sequence field rejects. A channel rejects sequence51 beyond
its50-packet reorder distance, preserves cursor0 and subsequently accepts0.
Fragment probes cover missing/zero/over-budget length and overrun; failure
leaves the assembler inactive. The continuous oracle covers reordered and
duplicate sequence handling. No exhaustive prefix matrix is added.

Native decode clears its input, clears one encoded result and re-encodes the
owned DTO exactly. Replay caps are modern fixture policy:1MiB fixture file,
32 applications,64 packets,72 received datagrams,8 fragment packets,
4096 application payload bytes and496 wire bytes. They are not recovered
legacy capacity limits.

## Reproduction and confidence

With Python and offline .NET8 available, from the repository root:

```powershell
python poc/round27-tests/Networking/peer.py --generate local-private/round27/peer.json --stage local-private/round27/tangible-stage.json
dotnet build poc/round27-tests/Networking/Networking.csproj -p:UseAppHost=false
dotnet poc/round27-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round27/peer.json local-private/round27/export.json
python poc/round27-tests/Networking/peer.py --peer local-private/round27/peer.json --verify local-private/round27/export.json
```

Observation: the independently packed22 applications, transport controls and
nine store rows match native output; accepted25/26 codec behavior supplies the
underlying implemented boundary. Interpretation: this bounded engine-neutral
combination is reproducible and ready for an explicit host join. Confidence:
Confirmed for the synthetic tested implementation; Unknown for live reachability.
Implementation consequence: retain owned ordered family codecs, current
incarnation checks, alias accounting and intermediate budget rejection when
joining the optional tangible world policy. The next action is the lead's
actual mixed world/presentation join after retargeting the stage identity.
