# Creature movement speed writers and readers

Round48, 2026-10-04. The bounded question is which demonstrated writers feed CREO4 fields7 and11, and whether their runtime readers fetch template speed again or consume wrapper storage. **Constructor initialization and receive delivery write the same current slots; the two inspected runtime readers consume those slots and conditionally substitute another source before modifier queries.** This closes a direct dataflow boundary, not an exhaustive later-writer or gameplay arbitration specification.

Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All addresses are baseline VAs. The static verifier (external historical reference) checks109 selected instruction anchors, two complete reader bodies, eight float-wrapper vtable entries and two cast RTTI descriptors. It reuses the [initialization](CREATURE_VIEW4_INITIALIZATION_CONTRACT.md), [delivery](CREATURE_VIEW4_DELIVERY_CONTRACT.md), and [registered scalar](CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md) audits. See the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R48_CREATURE_MOVEMENT_WRITERS.md) and independent checks (external historical reference).

## Storage and demonstrated writers

| Correlated field | Wrapper | Current `+C` | Cached `+10` | Constructor source |
| --- | --- | --- | --- | --- |
| CREO4 index7, run | `+668` | `+674` | `+678` | `011ADE40(0)`, template speed slot0 |
| CREO4 index11, walk | `+654` | `+660` | `+664` | `011ADE40(1)`, template speed slot1 |

Observation, Confirmed: template factory `006126D0` passes its template to creature construction `0042ECF0`. Calls `0042F0AA` and `0042F0E0` return template speed1/0, spilled to F32 at `0042F0AF/E5`. Constructor stores walk current/cached at `0042F0C2/C8`, run at `0042F0F8/FE`, and installs float-wrapper vtable `015DCF0C`. Registration order separately establishes index7 versus11. Interpretation: these are bootstrap assignments to wrapper storage, not a separate runtime authored-input overlay. Consequence: template sampling occurs on this constructor path; later changes to current storage can supersede that sample locally. Whether every runtime incarnation follows this path is not established.

The float-wrapper vtable maps byte offsets `+04/+08/+0C/+14/+18/+1C` respectively to `00439AC0/00439B30/00439A90/00439AE0/00439B50/00439B00`.

| Demonstrated operation | Direct evidence | Storage effect |
| --- | --- | --- |
| Snapshot decode | `00439B39` requests4 bytes, `00439B3B` supplies wrapper+C to `00439910`; `00439B48` invokes virtual+C | Receives current raw word, then copies it into cached slot |
| Delta decode | `00439B59/5B/5F` supplies the same4-byte destination; `00439B64/69/6C` conditionally notifies container at wrapper+8 | Receives current raw word; cached slot is not copied in this body |
| Cached synchronization | `00439A90/93` | Copies current+C to cached+10 as raw U32 |
| Generic local assignment | `00439B03/09/0D/10` compares F32; `00439B14` writes input word; `00439B1C/21` optionally notifies | Can write current+C through virtual+1C; specific run/walk caller remains unresolved |
| Outbound serialization | `00439AC3/C6/CC` supplies wrapper+C, count4, to `0118B0E0` | Copies current word to output stream; it does not replace current storage |

Interpretation, Confirmed for direct storage effects: baseline/delta values replace the current word, rather than being combined with the constructor sample by these codecs. This is local write behavior, not proof of universal authoritative precedence across all later callbacks, subclasses or callers. The generic local assignment compares with x87 status flags, not an integer bit-equality guard; it is not a demonstrated finite/nonnegative speed validator.

## Runtime readers and source substitution

Observation, Confirmed: reader `004388A0` begins with current run word `+674` at `004388A7`; reader `00438950` begins with current walk word `+660` at `00438957`. Their complete60/68-instruction bodies contain no direct call to template speed getter `011ADE40`. Constructor sample and subsequent deliveries therefore feed the same starting operand on these paths. No separate authored-versus-received merge is demonstrated.

Both readers skip alternate base-source branches if their byte argument is nonzero or predicate `00435250(0x1C)` fails. Otherwise they query `00557360(0x10000)` and template predicate `011B5A80`. The meanings of these flags and predicates are not renamed from surrounding strings.

| Branch | Run reader | Walk reader |
| --- | --- | --- |
| Template predicate true | `00648080(this)` result must exist; its `+30` object must cast from Dynamics to VehicleHoverDynamics; `00438906` uses cast result+C | Same cast; `004389BB/BE/C4` loads cast result+C, multiplies by the F32 word at `015DC5E4`, and spills to local F32 |
| Template predicate false | Requires `00438640(this)` nonzero, then nonnull `00425200()` result; `00438922` uses that object's `+674` | Same initial gates; additionally `00438690(other)` must return this receiver at `004389E6/E8`; `004389EA` uses other object's `+660` |
| Any shown null/cast/predicate failure | Keeps original local base word | Keeps original local base word |

The RTTI descriptors at `0186AFDC/0186AFC4` identify VehicleHoverDynamics/Dynamics, independently of server names. `00438640` reads receiver byte `+994`; `00438690` jumps to `00438650`, whose nested virtual calls and `+38C` mask establish a relation query, not a recovered ownership authorization. `00425200` begins from global `0190885C`; complete global lifetime/selection semantics remain Unknown.

After selecting its base operand, each reader calls `00438A70`, spills that result to F32, calls `00438A20`, then performs two x87 multiplies with the spilled first query and base operand. Run sites are `0043892D/32/37/3C/40`; walk sites `004389F5/FA/FF`, `00438A04/09`. This is observed arithmetic order, not a formula inferred from field labels. Original units, active x87 controls, nonfinite exception behavior, and final motion use remain Unknown.

`00438A20` initially reads `+638` (registered field5 current storage); `00438A70` initially reads `+64C` (field4 current storage). Each can recursively query the corresponding function on the object from `00425200` when `00435250(0x1C)` and `00438640(this)` pass and that object exists. Otherwise it returns the receiver's own word. No recursion-cycle guard is visible in these bodies. The speed reader's argument does not bypass these separate modifier-query gates. No clamp or normalization is present in the complete speed-reader bodies; transitive callees can still have effects.

Direct caller witnesses: `00431910/20` forward their argument to run/walk readers; `00431930` calls both, spills/compares results and chooses an operand through x87 flags. Another caller explicitly pushes zero at `00629F7A/86`, calls run/walk at `00629F7E/8A`, and stores results at `00629F83/8F`. That establishes executable use of both returned values. The caller's full motion state machine and ordinary gameplay reachability are not closed by this small witness.

## Receive, failure, lifetime and version boundary

The frozen dispatch maps an existing looked-up object to registered view4 wrappers. Null object lookup skips handling. Generic field-index dispatch and malformed-input behavior remain as documented in [replication schemas](REPLICATION_SCHEMA_MAP.md). Fixed reads reach `00439910` then canonical `archive_checked_read_VA_0118B030`; `0118B062/65` checks requested end against stream size, and `0118B076/7D` copies bytes. The overrun branch leads to the established Archive exception. Earlier field mutations can survive a later failure; Round48 does not invent transactional original decoding or safe unknown-index behavior.

These F32 wrapper and runtime-reader bodies have no asset-version discriminator. Version handling remains at the template and message boundaries in the prerequisite contracts. The registered snapshot registry's declared-count/EOF tolerance must not be confused with the modern strict full codec. No new message index or version is added here.

The wrappers are embedded in the concrete creature allocation, not independent owned speed objects. Destructor sites `0043003A/4A` and `0043004F/5F` call `0118AA80`, which clears wrapper+8 at `0118AA86` and continues destruction. Runtime readers borrow receiver/related pointers; they contain no explicit ownership transfer or retention. Lifetime of global and related objects, notification queue effects, and synchronization remain unresolved. A replacement must preserve its independently established incarnation/thread/transaction guards rather than infer them from these legacy pointer reads.

## Server correlation and compatibility consequence

In the separately supplied Stardust-3 tree, pinned `MMOCoreORB/src/server/zone/packets/creature/CreatureObjectDeltaMessage4.h:39–40` emits index7 from `getRunSpeed`; `:55–56` emits index11 from `getWalkSpeed`. `objects/creature/CreatureObjectImplementation.cpp:2058–2068` updates walk storage and sends when notifyClient is true, even when equal; `:2115–2135` run setter returns on equality or disabled notify, with a packet-null check; `:2138–2148` emits a run update separately. These named fields correlate with the independently observed client registration. They do not prove client units, runtime predicates, authentication, or consumer composition.

Confirmed compatibility requirement: retain template-bootstrap provenance separately from actual received values, while recognizing that this original direct path uses one mutable current slot per field. The [modern authored consumer](SHARED_CREATURE_AUTHORED_MOVEMENT_INPUT_CONTRACT.md) and [modern received reader](SCENE_WORLD_RECEIVED_CREATURE_MOVEMENT_INPUT_CONTRACT.md) remain separate and unchanged. Their finite/nonnegative policies, explicit slot choices, and no-fallback behavior are modern guards. This static result does not authorize wiring an inferred original formula into production.

Exact next probe: identify the receiver/type and branch meanings at `00629F7E/8A`, then trace one indirect virtual+1C assignment or notification route that can target embedded `+668/+654`. Bound related-object/global lifetime and writer order before claiming complete arbitration. No new corpus occurrence was necessary to close the direct storage question.

## Round49 compatibility increment

[Original-resource sourced preview](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md) passes authored/received headless and authored GPU joins through the actual original recipe closure with zero extra movement reads. [Current raw inputs](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md) retain explicit constructor provenance and exact received overrides without store seeding; [explicit stop and sourced input](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md) own the modern direction/clock lifecycle. Original full arbitration, modifier/vehicle semantics and units remain separate.
