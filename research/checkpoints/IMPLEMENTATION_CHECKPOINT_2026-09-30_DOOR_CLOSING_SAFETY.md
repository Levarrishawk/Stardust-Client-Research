# 2026-09-30 modern closing sweep safety

## Question and evidence

Can an explicit optional closing policy prevent a translating leaf sweeping into a stationary incident actor without granting entry or changing the default? Close after independent geometric/controller comparison, failure/event gates and scene denial/retreat.

Inputs: explicitly synthetic normal leaf with a fixed rigid orientation, authored movement and finite box, stationary actor and cleared actor position; identity and rotated/translated owner. Tools: native smoke and [independent slab/controller verifier](../../../SWG-Godot-Client/tools/python/verify_door_closing_safety.py). Original default regression uses the unchanged [Mustafar input pins](IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_INTERIOR_SCENE.md).

## Confirmed results

Native gates pass complete candidate close obstruction, repeated previous-pose/fraction holds, clear release and actual close events, pure controller preview, unchanged unguarded default, unrelated cell exclusion, malformed/duplicate body and failed callback rejection, body snapshot mutation isolation and reset. Scene gate verifies held closure never grants denied entry; actor retreat releases the sweep and closure finishes. The fixture was placed outside the permission barrier to respect its existing genuine-initial-overlap rule.

Independent original-style synthetic easing/slabs match **240 ticks / two owner poses / guarded and unguarded modes / 52 held ticks**. The original multi-door default trace still matches **2,880 ticks / 24 transfers / 172 blocked / zero steps**. No tolerance changes or original solver claims.

Confidence: Confirmed bounded modern optional safety. Original moving-leaf ordering, narrow-phase collision, opening sweeps and penetration response remain Unknown. See the [durable contract](../../docs/DOOR_CLOSING_SAFETY_CONTRACT.md).

## Next probe

Exercise occupied closing, denied entry and retreat on the existing pinned original second doorway. Verify guarded and historical traces independently, then expose an explicit host option.
