# Portal permission barrier and door controller

Updated: 2026-09-30. Engine-neutral offline collision slice, extending [one-portal floor transfer](PORTAL_FLOOR_TRANSFER_CONTRACT.md). The stopping condition is a validated source-cell actor contact before transfer, with current permission and deterministic door-controller primitives. Full physical door-leaf clearance remains separate.

## Direct original evidence

Executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; addresses below are baseline VAs. Confidence is Confirmed for the instruction/data observations, with interpretations qualified below.

| Observation | Interpretation and consequence |
|---|---|
| `012906F0` allocates `B0` bytes, calls `012A4C40` with the DoorObject and the inverse of a style-record byte at `+30`, stores the object at DoorObject `+F8`. | Barrier construction is distinct from door-leaf appearances and the trigger collision property. The exact style-byte semantic is not newly inferred here. |
| RTTI: vtable `0160039C` is `BarrierObject`; `01600368` is `BarrierCollisionProperty`; `01600034` is `DoorObject`. Constructor stores the linked door at barrier `+A8`, installs the collision property and calls `012A4E60`. | A barrier owns a separate collision component and consults the linked door. Lifetime of the borrowed door pointer requires host-managed teardown; this slice stores no legacy pointers. |
| `012A4E60` calls linked DoorObject vtable `+6C`, then `00B52DD0`, subtracts/adds constant `015DC700` (F32 `0.05`) from the three min/max coordinates, and assigns the resulting extent via `012609A0`. `00B52DD0` transforms portal vertices into its portal frame and accumulates bounds. | Confirmed padded portal-frame bounding box. Actor-box Minkowski expansion and the slab sweep are owned modern collision algorithms, not a recovered legacy narrow-phase implementation. |
| `BarrierObject +64` -> `012A50A0` returns 1 when linked DoorObject `+5C` -> `01290CF0` -> `00B53920` is false. No controller fraction is read. | Permission barrier activity is independent of visual openness. Do not use a guessed fraction threshold for passage clearance. |
| Barrier `+60` -> `012A50C0` requires controller `+11` active and the same door eligibility false. BarrierCollisionProperty `+14` -> `012A4CF0` requires candidate end-cell owner equality, candidate byte `+7A`, then barrier `+64`. | Confirmed candidate filtering path. Broader meaning of candidate byte `+7A` remains Unknown. |
| CollisionProperty `+18` -> `012A4D40` returns true when controller `+11` is inactive, or when permission blocks and raw mode is not 1. `+1C` -> `012A4DA0` requires barrier `+64` then delegates to `012616B0`. | Confirmed mode-dependent predicates; historical mode names and full actor solver ordering remain Unknown. Preserve raw mode 1 separately in the primitive API. |
| DoorObject `01290BB0` checks start/end owner against itself or its pair, records a trigger through `01290D50`, and requests the primary or secondary controller through `012A4B40`. | Contact can request door motion. General candidate/force-field/secondary-controller behavior is not implemented by this normal-controller primitive. |

### Versioned template flags: corrected field association

`00B437B0` initializes template byte 0 to zero. PRTL/0005 loader `00B43C10` writes **LeadingByte to template +0**, **SolidByte to +1**, and **WindingByte to +2**. Older supported references omit the leading byte and keep its initialized zero. `00B53900` reads +0; `00B53910` reads +1. Door creation `00B53500` and permission predicate `00B53920` require +1 nonzero and +0 zero; only then does the permission predicate query the destination callback `00B2BDB0`.

The sole direct caller of inversion helper `00B43E80` is `00B471BF` in the **PRTO/0002 loader `00B46BF0`**, which flips +1 across that loader's references. It is not a universal post-load inversion. Our supported PRTO/0001, /0003 and /0004 retain the normalized serialized byte. `PortalDoorFlags.FromReference` explicitly rejects unsupported layout versions.

This resolves the earlier tentative raw-flag interpretation: the first runtime template byte is the leading exclusion byte, not the serialized SolidByte. The second runtime byte is the supported-version SolidByte, not WindingByte. SolidByte identifies template eligibility in this path; it still does **not** supply current permission, animated leaf clearance, or the mutable portal byte `+8` written by `00B53940`. Historical checkpoints are retained; this dated refinement is canonical.

### Controller and lifecycle observations

Controller constructor `012A49C0` initializes fraction 0, open/close durations from style offsets `+1C/+20`, last request counter -100, active/forward bytes false and event -1. `012A4B40` sets active true and stores counter `00B3C0C0` (global `01946FC8`). `012A4A00` selects forward when counter difference is within -1..1. `012A4A30` adds delta/open-duration or subtracts delta/close-duration, selects the first matching event, clamps to [0,1] and clears active at zero. Events are 0 begin-open, 1 end-open, 2 begin-close, 3 end-close. A large delta beginning at zero reports begin-open even if it reaches one in that update.

Door alter `01290760` advances controllers, writes mutable portal `+8` using primary fraction >0 except disabled/force-field branches, updates appearances/effects, prunes trigger handles and refreshes permitted controllers for surviving occupants. `01290EC0` removes destroyed handles and distances **greater than or equal to** the stored squared radius; equal-radius contacts are removed. `01290D50` caps the set at 25. This refines the older report's loose “exceeds radius” wording. The subsequent [single-leaf investigation](SINGLE_LEAF_DOOR_CONTRACT.md) confirms that this stored pruning radius is **1.5 times authored triggerRadius**, while new-contact shape input uses authored radius at local `(0,1,0)`.

Portal-record cleanup `00B52D60` removes the registered door and invokes the installed destruction callback before clearing its links; it also destroys an optional owned record at `+1C`. Complete ordering among actor contact, floor-owner changes and global collision dispatch is still unresolved. Nothing here proves that direct `012670E0` floor continuation alone performs all these checks.

## Owned APIs and policy

`PortalDoorController` supplies positive finite caller-authored open/close durations, `RequestOpen(frame)`, `Advance(frame,seconds)`, state snapshots and reset. Frames are explicit nonnegative monotonic 64-bit values; delta is finite/nonnegative. Opening requests live through the current/next frame. This is a modern scheduler boundary, not a promise of original counter wrap semantics. It exposes the recovered permission/candidate/raw-mode predicates. It has no trigger collection, effects, styles, leaf transforms, paired doors or force-field variants.

`PortalBarrierCollider` owns padded min/max values in a finite rigid right-handed portal frame. Vertices are bounded at 100,000. Authored enabled transforms are retained. Without one, inspection derives a rigid frame from the first nondegenerate triangle; this derived frame is an explicit modern choice, not proven identical to every legacy fallback/winding branch. Source arrays can be released after construction.

`Sweep` consumes start/end actor centers, nonnegative finite world-axis half extents and nullable current passage permission. It transforms the enclosing actor box into the portal frame, expands the padded box, and returns first segment contact with parameter/center/world normal. Outcomes: Clear, Blocked, PermissionUnknown, InitialOverlap. Initial overlap stays at parameter zero; there is no depenetration, sliding, capsule or exact rotated-box narrow phase. The enclosing-box method can conservatively stop near angled polygon corners. Allowed permission clears this permission barrier even with a closed controller; leaf geometry is a separate collision requirement.

`PortalBarrierFloorWalker` connects this to the existing one-portal query for **identity owner frames and level source motion**. It prepares barriers only for paired floor links whose template allows door construction. Per-call permission is sampled once per consulted link; entry/barrier controls combine as offline passage policy. The floor query bounds attainable progress, then source-cell barrier contacts can shorten it before owner transfer. A candidate contact must have an attainable source floor locator. Unknown/denied/blocked state retains that cell, with zero transfers. Nonlevel requests stop at the starting locator with `UnsupportedNonlevelBarrierSweep`; no unsupported collision success is reported. Destination-cell barrier sweeps, exterior barriers without paired floors, actor contact events, leaf meshes and full floor-projected sweep paths are deferred.

Prototype host **Actor bounds** uses an explicit inspection box of width/depth 0.5 and height 1.8, centered 0.9 above the picked foot. Toggle with Portal transfer and Walk between picks. Entry allowed and Barrier open remain offline controls; they are not a live cell-permission feed or door animation.

## Verification and measured scope

- Native smoke tests cover pulses/expiration, endpoint events, large-delta event precedence, reversal, reset, invalid timing/frame input, barrier mode predicates, forward/reverse contact, actor height/misses, closed endpoints, initial overlap, unknown/current permission, rotated/translated frames, ownership, malformed inputs and pre-transfer owner retention.
- Independent effective-overlay raw parsing agrees on **291 layouts / 7,642 references** and **7,231 template-eligible barrier candidates**. These are candidates, not a count of instantiated doors; the global creation gate and style/resource loading remain separate.
- Independent portal-frame bounds and analytic axial actor contacts agree on **21,693 probes** (denied/allowed/unknown for each candidate). This proves the owned geometry/policy arithmetic for these samples, not original collision equivalence.
- YT-1300 host: **12 level directed links / 24 current-policy probes**, plus **2 explicit nonlevel stops**, actor control recovery/reset and reflected contact/owner checks. Previous 14-link transfer, floor/path and GPU host gates also pass.

Input pins and reproduction are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_BARRIER_COLLISION.md). The [single-leaf follow-up](SINGLE_LEAF_DOOR_CONTRACT.md) now closes one named style, trigger input and conservative physical clearance. Next action: wire a named original POB door instance into interior walking. Trace the global actor collision caller before treating the prototype ordering as recovered behavior.


## 2026-09-30 owner/leaf integration refinement

The [door movement follow-up](INTERIOR_DOOR_MOVEMENT_CONTRACT.md) extends the walker to supplied rigid yaw owner poses and explicit source-reference leaf bindings. Permission boxes retain recovered padding; leaf boxes are unpadded current-pose snapshots. World actor contacts are projected back to source-floor coordinates, and unsupported source landings retain the start locator. Original global solver ordering remains Unknown.
