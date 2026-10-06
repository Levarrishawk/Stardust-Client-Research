# CKAT root translation scalar binding

CKAT constructor `00822FC0` initializes runtime `+0x4C` with the pointer returned by direct lookup call `007D3790`. It uses the caller-supplied lookup object at explicit constructor argument2 (stack EBP+0C) and argument address `01923F7C`. The runtime owns its pointer field; ownership/lifetime of the pointed scalar storage is still Unknown. This is a concrete typed initializer, not MissingTypedAnchor, but it does not close the lookup container's storage contract.

## Evidence chain and confidence

| Observation | Interpretation | Confidence | Consequence |
| --- | --- | --- | --- |
|008230F5 loads ECX from EBP+0C;008230FA pushes01923F7C;00823109 calls007D3790 | Lookup invoked on externally supplied constructor input using that key/address | Confirmed calling operands; object/key friendly names Unknown | Do not invent AnimationEnvironment, locomotionScale or default-value semantics |
|00823110 stores EAX at[ESI+4C], with ESI=this from00822FE1 | Returned pointer binds the scalar source consumed by accepted73 wrapper | Confirmed store and cross-boundary association | Preserve access to the pointed current float if implementing that wrapper |
|00823113 installs015E5DC4; RTTI COL/type chain names CompressedKeyframeAnimation | This producer constructs the typed runtime of00822D40 | Confirmed | Distinguish it from physical-track+4C/end-of-object fields |
| No pointee load/copy or separate retaining operation between the lookup return and store | Plain pointer binding, not a snapshot of the float | Confirmed locally; ownership transfer itself Unknown | A detached copied scalar/default one needs an explicit mutation/lifetime contract |
|01923F7C is virtual-only .data; raw read rejects | Key/address runtime contents cannot be recovered from file bytes here | Confirmed file mapping; runtime meaning Unknown | Do not promote an uninitialized-file zero into a property name/value |

The complete one-body witness is00822FC0..008232D8 inclusive,793 bytes/273 instructions, SHA decc1b4d66c5d4142fafe6341ef5e25780e228ecdc0527a3570895e459fabdcb. It returns this in EAX and ret0C. The scale lookup/store is at00823109/00823110; actual stored value is EAX unchanged. Other constructor array/evaluator branches and30 calls/25 branches are byte-verified but not reinterpreted as new schema/lifecycle closure; all callees remain unexpanded.

[Accepted73 consumer](COMPRESSED_ROOT_EXTERNAL_CONSUMER_CONTRACT.md) separately loads this pointer and reads its float once after root evaluation, scaling only the output translation triple. No consumer, physical-track or inner-evaluator body was repeated here. A pointer field store alone proves neither borrowed nor transferred ownership; no destructor/accessor implementation or reference thread contract was expanded.

## Candidate budget and unresolved storage owner

Only two explicit candidates were needed from the allowed twelve-candidate bound:

| Candidate | Evidence used | Expansion |
| --- | --- | --- |
|00822FC0 canonical runtime constructor | Cached named constructor and original instructions; typed vtable installation | One producer body,793 bytes |
|007D3790 immediate lookup | Exact constructor call and cached function-inventory row007D3790..007D3895,size262 | Metadata only; not a tiny accessor, no body expansion |

Next scalar probe: authorize a separate bounded007D3790 lookup/storage investigation, identify its object type/key producer and the lifetime/mutation policy of the returned float storage. The pointer's numeric units/default/property name are not established by this constructor. Initializer is now Confirmed; storage owner is Unknown. No global pointer census or twelve-candidate exhaustiveness claim follows.

## Prior actor application is already closed

The [September25 ROOT_MOTION_OBJECT_APPLICATION checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_ROOT_MOTION_OBJECT_APPLICATION.md) already establishes the higher getter/actor path and model/object-local basis. Its original Confirmed by code conclusion is preserved: getter chain0083EC70 ->0082F990 ->007D6A90 ->007D7810 ->007CA220 ->00639100; eligible CreatureController post-composes local rotation as objectLocal'=objectLocal*rotationTransform(qDelta), rotates local translation by current object orientation into parent space, adds it and notifies Object. Friendly labels/gate name are Strongly inferred; original member spellings and future/indirect selection of dormant combined clips remain Unknown.

Recent round73/74 Unknown statements were local-body proof limits and omitted that global prior closure. The [external wrapper](COMPRESSED_ROOT_EXTERNAL_CONSUMER_CONTRACT.md) and [physical-track contract](COMPRESSED_ROOT_PHYSICAL_TRACK_CONSUMER_CONTRACT.md) now explicitly reconcile them. Do not recount the old getter/actor chain as new coverage or change its original confidence merely because this round did not re-prove it. Modern host/runtime parity and actual instance selection are separate from static code basis closure.

The audit finds five exact cached getter/application direct edges, one cached aggregate virtual+0C report and nine existing cached a03-application files with recorded identities. Only metadata presence/hashes are audited; none of those bodies was semantically reopened. The September25 report's raw transform-order arithmetic and sole-caller/global-census claims are inherited, not freshly rerun. This round adds no compact sealed executable-body/hash/numerical regression for the actor path; cached decompilation is reconstructed pseudocode. That is a reproducibility limit of this new audit, not a downgrade of the old report's stated confidence.

## Validation

[Portable static witness](../poc/round75-tests/Assets/Invoke-SelectedRule.ps1) takes explicit Python, EXE, static packages and private cached-evidence root, writes fresh ignored output and verifies EXE SHA E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 before VAs. Existing bounded RawPe and independent pefile mapping agree;273 instructions/11 role anchors/30 calls/25 internal branch targets, one typed slot and17 identity/body/type/virtual-tail/bounds guards pass. Cached reports/file hashes are retained privately. No original execution, asset/corpus read, dynamic probe, producer code implementation or function/registry change occurred. [Checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-06_R75_ROOT_SCALE_SOURCE.md).
