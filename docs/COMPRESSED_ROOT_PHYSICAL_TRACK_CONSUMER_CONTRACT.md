# Physical-track root-delta consumer

Physical-track alter `0083E630` calls its active animation through virtual offset `+0x18` at `0083E746`. For the accepted RTTI-typed CKAT vtable `015E5DC4`, that slot at `015E5DDC` resolves to `00822D40`. This is conditional on the active object having that vtable: the physical track uses a generic animation interface and does not hardcode CKAT or prove its dynamic type on every invocation.

The immediate destination is physical-track-owned accumulation fields, not an actor transform. Translation adds into track `+0x20/+0x24/+0x28`; four returned quaternion words are copied into `+0x2C/+0x30/+0x34/+0x38`. Actor basis/application is not established by this local body; the [September25 global application closure](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_ROOT_MOTION_OBJECT_APPLICATION.md) supplies that prior static contract, reconciled below.

## Exact evidence

| Observation | Interpretation/confidence | Compatibility consequence |
| --- | --- | --- |
|0083E735 loads active pointer from track+8;0083E738/73A null-test/skip;0083E73C loads its vtable;0083E746 calls[vtable+18] | Confirmed generic virtual root-extraction call | Keep type-dependent dispatch; do not treat all active animations as CKAT |
|015E5DDC contains00822D40; RTTI COL01618A50/type01896AF0 names CompressedKeyframeAnimation | Confirmed CKAT slot association, reused accepted73 metadata | When this dynamic vtable is selected, call reaches the accepted CKAT wrapper; runtime instantiation/normal reachability not proved here |
| Stack output addresses EBP-34 and EBP-24 passed as first/second arguments | Confirmed forwarding; rotation16-byte and translation12-byte roles from accepted73 ABI | Root outputs are temporary animation-layer values |
| EDI established as track+20 at0083E67C; FADD/FSTP at0083E74C/755,75E/761,767/76A | Confirmed addition of three temporary translation components into track fields | Preserve ordered accumulation over executed substeps; no extra rotation/basis application occurs in these stores |
| EBX established as track+2C at0083E74E; call0083E76D targets00ABA9F0; returned EAX words copied at0083E774/779/77F/788 | Confirmed quaternion-helper call and four-word track destination | Record exact source/destination ordering rather than calling it an actor transform |
| Root extraction/stores precede optional message call0083E79D and ordinary active replacement store0083E7D8 | Confirmed ordering on this branch | Outgoing animation root output is consumed before that branch installs replacement; no blanket guarantee across every completion/error path |

At the quaternion helper call ECX points to temporary delta quaternion EBP-34; the stack supplies result storage EBP-44 and prior track+2C quaternion. Existing [recovered multiply evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_CKAT_COMBINED_ROOT_TRANSFORM_ORDER.md) closes00ABA9F0's product convention. Interpretation `newAccumulator = delta * priorAccumulator` is Strongly inferred from that closed helper plus this operand association; raw operands, call and copy are Confirmed. No helper body was re-expanded, and normalization or world-axis meaning is not inferred from the call name.

The [controller/physical-track contract](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TRACK_ANIMATION_CONTROLLER.md) independently records constructor0083E290: controller owner+0, track template+4, retained active animation+8 and accumulated translation+20/rotation+2C in a0x4C-byte physical-track object. That durable ownership/layout evidence corroborates the new instruction-level destinations. It does not make the owner controller an actor or prove a world-frame interpretation.

## Reset and ownership limits

At entry, four global-cell words01945B38..44 are copied to track+2C..38 and three words01945978..80 to+20..28 before loop gating. The referenced ranges are virtual-only .data in this baseline. The bounded mapper rejects file-byte reads there. Their runtime values/initialization are Unknown in this investigation; no identity/zero runtime value is promoted from raw file absence. Accumulation is per alter invocation/substep path rather than proved persistent across independent calls.

The guarded root-output path uses the currently retained animation before the normal replacement branch. That branch conditionally increments pending object's+4 at0083E7B0, calls existing old-animation helpers and writes track+8 at0083E7D8. Refcount meaning is supported by the prior retained-animation contract; helper implementations/lifetime exceptions were not reanalyzed. SEH prologue and25 branches are present; handler, transition helpers and callback/reentry/thread-affinity behavior remain unexpanded. No full malformed-input or cleanup guarantee follows from a static witness.

## Scope, reproduction and next boundary

Only one physical-track body was expanded: `0083E630..0083E88D` inclusive,606 bytes/204 instructions, SHA aa7d495e3acf45451158cb3adfefd871006de0d60b7d1c54003f5968829f953d. The initial608-byte discovery window stopped at its single ret8; no adjacent body was expanded.19 call sites include ten distinct direct targets and generic indirect slots; all callees remain unexpanded.25 branch destinations are verified as instruction boundaries within this body. Accepted73 wrapper/mode/algebra bodies were not replayed; only necessary typed slot/RTTI metadata was reread. No tiny accessor was required because destination stores are inline.

[Portable static witness](../poc/round74-tests/Assets/Invoke-SelectedRule.ps1) takes explicit Python, baseline EXE and existing private static-package paths, writes fresh ignored private output, and checks EXE SHA E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VAs. Existing bounded RawPe and independent pefile mapping agree.26 role anchors/204 instructions/19 calls/25 branches and18 verifier rejection guards pass. Guards are synthetic verifier inputs, not legacy CPU execution. No corpus/native production build, function-name/registry/shared-ledger change or original execution occurred. [Checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R74_ROOT_PHYSICAL_TRACK.md).

Current resume: getter0083EC70 and controller0082F990 are already in the September25 application chain; do not treat them as a newly missing actor-basis proof. The separate scalar pointer is initialized by constructor00822FC0 per [round75 scale-source evidence](COMPRESSED_ROOT_SCALE_SOURCE_CONTRACT.md); its lookup storage/owner/lifetime and the reset globals remain unresolved.

## September25 scope reconciliation

[ROOT_MOTION_OBJECT_APPLICATION](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_ROOT_MOTION_OBJECT_APPLICATION.md) already closes the higher chain through007D6A90/007D7810/007CA220 to eligible CreatureController00639100. Its original confidence is Confirmed by code for getter chain, affine order, parent-space translation addition and Object notification; Strongly inferred for friendly method/gate names; Unknown for original member spellings/future indirect selection of the two dormant combined clips. It records object-local/model-local animation delta, objectLocal'=objectLocal*rotationTransform(qDelta), then translation rotated by current local orientation into parent space and added to local translation. Round74's Unknown wording described what its one physical-track body did not establish, not a new global gap.

Round75 confirms cached reference reports/application-file presence and identities without reopening the getter/actor bodies. The prior application's arithmetic is inherited evidence, not a fresh numeric or raw-body replay here; no new compact sealed actor-body/hash fixture is introduced. Physical-track dynamic-type conditions, scalar source ownership and modern host parity remain distinct limits. Future work should target those actual gaps rather than redo the closed higher basis chain.
