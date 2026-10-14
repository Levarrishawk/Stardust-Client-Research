# Single normal door leaf: style, contacts, motion and clearance

## Boundary and confidence

This bounded offline implementation loads one original named style, retains explicit actor handles, advances its primary controller and moves one rigid primary leaf. Complete static-mesh detail bands are now accepted under the [fixed root-box gate](DOOR_STYLE_CAPABILITY_MATRIX.md). The prototype host adapter renders neutral geometry and provides contact, permission, step and reset controls. Original controller/curve/trigger facts below are **Confirmed** by static executable evidence. Physical leaf clearance against the primary appearance box is an **owned conservative policy**; original narrow-phase shape selection and global actor ordering remain **Unknown**. This is separate from the permission barrier and mutable portal state described in the [barrier contract](PORTAL_BARRIER_COLLISION_CONTRACT.md).

## Original style schema

`00B52A50` opens the path string at `018B77EC`, `datatables/appearance/door_style.iff`, and installs a table at `0194708C`. Effective installed input is DTII/0001 with ordered COLS, TYPE and ROWS chunks, 18 columns and 124 unique named rows. String fields are NUL-terminated ASCII; numeric scalars are little-endian. Boolean columns occupy signed I32 storage, not one byte. ROWS begins with an I32 count.

| Column | Encoding | Meaning |
|---|---|---|
| 0 | string | doorStyleName lookup key |
| 1–3 | string | frameAppearance, doorAppearance, doorAppearance2 |
| 4 | I32 | doorFlip2 raw flag |
| 5–7 | F32 | doorMoveX, doorMoveY, doorMoveZ |
| 8–9 | F32 | openTime, closeTime |
| 10–12 | F32 | springiness, smoothness, triggerRadius |
| 13 | I32 | isForceField raw flag |
| 14–17 | string | openBeginEffect, openEndEffect, closeBeginEffect, closeEndEffect |

`00B53500`, including column reads at `00B535ED–00B538F0`, consumes this ordering. A missing nonempty named key falls back to row zero. An empty name follows separate no-style defaults; the owned selector rejects empty input. Exact original lookup case rules remain Unknown; the owned selector uses ordinal matching and reports fallback explicitly. Server correlation is `MMOCoreORB/sql/datatables.sql`, `appearance_door_style` definition at line 67 and columns 68–85; this supports vocabulary, not proof of client behavior.

The strict owned decoder requires this schema/version, finite floats, printable ASCII strings of at most 1,024 bytes, 1–4,096 uniquely named rows and a maximum 1 MiB input. It rejects trailing data and malformed sizes. Decoder acceptance is separate from runtime eligibility. The single-leaf runtime requires a normal, nonempty primary appearance, no secondary or flip, EXBX bounds, rigid right-handed pose, positive durations/springiness/radius and smoothness within 0–1. Force fields and paired leaves are rejected explicitly.

## Original contact and motion evidence

| Observation | Interpretation and consequence |
|---|---|
| `01290560` builds a four-float record with center `(0,1,0)` and authored triggerRadius; `013EA170` copies its center and radius into the shape. | New contact uses the authored local sphere input, independently of the movement vector. Full original shape class dispatch remains outside this slice. |
| `0128FFFD` multiplies triggerRadius by F32 1.5 from `01600028`, squares it and stores DoorObject `+120`; `01290EC0` removes distances >= that value. | Retention uses 1.5 times authored radius, with equality removed. Pruning is relative to door origin, not the trigger center. This refines earlier “configured trigger radius” summaries. |
| `01290D50` caps retained objects at 25; `01290760` advances controllers before pruning and permitted-contact refresh. | Modern host supplies stable nonzero actor handles and current positions; null means destroyed. Advance, prune and pulse refresh order is preserved. |
| `012901E0` calls DoorObject slot +64 (`01290B60`) then multiplies by movement at D0/D4/D8. | Controller fraction and displacement fraction are distinct. |
| `01290B60` uses reciprocal springiness, power helper `0131E2B0`, F32 pi at `01600024`, cosine and smoothness blending. | Implement the curve below; use the original F32 pi value. Modern double intermediates are not claimed bit-identical to x87. |
| `012902D0` creates primary leaf F0 and assigns the primary appearance; secondary F4 is optional and may rotate about Y by pi. | Selected normal single leaf follows the primary branch. `01290760` applies negated displacement to a second leaf; that branch is deferred. |

For controller fraction `f`, springiness `s`, smoothness `m`:

```text
p = f^(1/s)
displacementFraction = p*(1-m) + (1-cos(F32(pi)*p))*0.5*m
localTranslation = authoredMovement * displacementFraction
leafPose = doorPose composed with localTranslation
```

The existing normal controller (external historical reference) supplies frame pulses, duration updates and first-matching transition indices 0–3. Effects are returned as authored reference strings; playback is deferred. The caller consumes transition metadata once per advance. New contacts require known allowed entry in this prototype. Original denied-contact secondary/candidate branches and callback +7A semantics remain Unknown here. Revoking permission stops pulse refresh; an existing current/next-frame pulse can expire before closing begins.

## Owned physical envelope and lifecycle

`SingleLeafDoorRuntime` uses the leaf's primary APPR EXBX box at its displaced pose, without the permission barrier's 0.05 padding. A world-axis actor box is conservatively enclosed in the leaf frame and swept against expanded bounds. Clearance follows actual box overlap at the current pose; no arbitrary opening threshold is used. This query does not resolve sliding or a moving-door collision over an elapsed time interval. The selected mesh has a NULL secondary extent; that does not prove the original solver uses its primary box.

External actor callbacks are resolved and validated before a step commits. Reset clears controller and retained handles; disposal does likewise and rejects subsequent operations. Construction validates closed and open poses to reject displacement overflow before contact changes state. No raw pointer ownership crosses the engine boundary. `SingleLeafDoorAssetReader` uses existing owned source caching and pinned table/appearance identity, and requires a direct static mesh with a primary box. The table's optional frame appearance is retained metadata, not loaded or rendered by this slice.

## Validated selected style

`cantina_door` has one primary `appearance/cantina_door.apt`, no frame/secondary/flip/force field, movement approximately `(-2.1,0,0)`, open/close 0.5 seconds, springiness approximately 1.3, smoothness 1 and trigger radius 3. It resolves to `appearance/mesh/cantina_door.msh`. Primary box minimum is approximately `(-1.175978,-0.037567,-0.073922)` and maximum `(1.215925,3.210477,0.078029)`. Its open-begin and close-begin reference metal style 01 effects; end references are empty.

All 124 original rows match an independent DTII reader. A 100-frame scenario, stepping 0.025 seconds with the actor released at frame 40, matches independent timing, curve, translation, effect references and axial swept-box calculation: 60 blocked and 40 clear queries. These counts describe one scenario, not coverage of 124 runtime variants. Synthetic tests cover every truncated table prefix, duplicate/type/nonfinite/trailing rejection, fallback, owner/permission filters, capacity, radius hysteresis/equality, callback failure, permission revocation, reset/dispose and transformed owners. Headless and GPU prototype host control/pose tests pass; capture was visually inspected. Input hashes, exact reproduction and remaining questions are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_LEAF_DOOR.md).

## Next probe

The [interior movement follow-up](INTERIOR_DOOR_MOVEMENT_CONTRACT.md) now binds one original cantina/private-room doorway and validates those states before one owner transfer. Next: persistent actor ownership and repeated movement through both directional references. Keep original general solver ordering explicitly unresolved until its callers are traced. Paired leaves, force fields, effects playback and moving-leaf response remain separate work.
