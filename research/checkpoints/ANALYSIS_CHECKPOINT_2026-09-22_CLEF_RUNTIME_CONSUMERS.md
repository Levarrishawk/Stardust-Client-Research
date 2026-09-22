# CLEF runtime consumers and missing-sound behavior

Date: 2026-09-22. Target: original `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. All function bodies cited here are Ghidra-reconstructed pseudocode, checked against the original image; names are provisional.

## Bounded question and result

Question: Which runtime methods consume the five `CLEF` template vectors, and does a null `PSND` sound-template handle prevent later effect playback? Stopping condition: trace template fetch to instance execution and the relevant sound playback null guard.

`0x005F4240`, `0x005F4280`, and `0x005F42C0` fetch a `CLEF` template at `0x005F40D0`, create one of three instance forms, and release the temporary template reference through `0x00699870`. The instance base constructor `0x005F0320` stores the template at instance `+0x04` and adds its own reference. The first form stores a world position/direction pair (`0x005F09F0`); the other two store an object attachment and optional transform (`0x005F1030`, `0x005F1090`). **Confirmed by code.**

Two vtable execution methods, `0x005F05B0` (world-position form) and `0x005F0B70` (attached form), walk the same template vector offsets:

| Template offset | CLEF leaf | Observed runtime action | Confidence |
|---|---|---|---|
| `+0x0C` | `CPAP` | Fetch/create child appearance, attach/orient particle object, apply authored ranges and ownership context | Confirmed; individual numeric field meanings remain partial |
| `+0x10` | `PSND` | Re-fetch sound template by stored path and request positional or object/hardpoint playback | Confirmed |
| `+0x14` | `CLGT` | Allocate a `0x128`-byte light object, copy light record scalars, position/attach it, and register it | Confirmed; exact light-record field semantics not fully closed |
| `+0x18` | `CAMS` | Use first record's four 32-bit fields for camera-distance-gated shake; forward parameters to camera state | Confirmed; exact units/easing unknown |
| `+0x1C` | `FFBK` | Iterate 24-byte records, compare distance, and call the stored DirectInput feedback object with an authored iteration count | Confirmed by code and local Windows SDK header |

The `CAMS` helper `0x005F0350` first checks an availability gate and active game camera. It computes camera-to-effect squared distance; if inside the supplied radius, it forwards one parameter and an attenuation term `(1 - distance_squared/radius_squared) * amplitude` to `0x007886B0`. That function stores the new values in the camera object. The attached form uses the owning object's world transform for the effect origin. Thus `CAMS` is not just an inert parsed tag; it drives camera state. **Confirmed by code.**

The `FFBK` helper `0x005F0410` compares Euclidean distance of two positions with the record radius at `+0x14`; if inside and the feedback-object pointer at `+0x0C` is nonzero, it calls `0x00421B20` with the `+0x10` field. The latter iterates concrete feedback-effect wrappers and invokes their virtual method at `+0x04`, which resolves to `0x004205C0` on the observed wrapper vtable. `0x004205C0` calls its `IDirectInputEffect` pointer's slot `+0x1C` with that field and zero flags. The installed Windows SDK `dinput.h` identifies this slot as `Start(DWORD iterations, DWORD flags)`. Thus the `+0x10` scalar is a **playback iteration count**, not strength/amplitude. **Confirmed by executable code plus local interface declaration.**

The `FFBK` loader calls `0x00421ED0` for the authored `forcefeedback/*.ffe` path. It resolves the path, constructs a feedback object, calls `0x00420010`, and stores the returned object pointer in the 24-byte CEF record at `+0x0C`. `0x00420010` invokes the DirectInput-device vtable slot `+0x6C` with the file path and callback `0x00420360`. The installed `IDirectInputDevice8A` interface declares this slot as `EnumEffectsInFile`; the callback invokes slot `+0x48` (`CreateEffect`) and wraps the returned `IDirectInputEffect`. These `.ffe` paths are therefore native DirectInput effect files, not generic sound/particle references. Whether unsupported devices, corrupt files, or missing files are silently skipped or warned requires separate failure-path audit.

### Missing `PSND` child

The CLEF loader's `PSND` parser (`0x00698560`, likewise v0002/v0003) stores a 16-byte entry containing a copied sound path string and a fetched sound-template handle. If the child file is missing, `0x00417320` returns a null handle, but the path remains in the entry. At runtime, the world form tests whether the stored path string is nonempty before calling `0x0040D9C0`. The attached form tests for non-null/nonempty path before calling `0x00410660`; both converge on `0x0040BFA0`, which **re-fetches by path** via `0x00417320`. When that fetch returns null, it skips the playback branch. The recorded null handle is not directly dereferenced on these traced execution paths. Thus the **ten** configured-overlay CEF references with absent `.snd` children do not directly crash these two methods; available non-audio components can still instantiate, and missing sound playback is skipped unless a resource later becomes available. **Confirmed for these two execution methods; other possible consumers of the cached handle remain unreviewed.**

This refines the earlier `CLEF_PSND_MISSING_SOUND` checkpoint, which correctly established the null loader handle but left runtime behavior unknown. Do not infer general missing-resource tolerance for unrelated asset classes.

## Evidence, caveats, next work

Primary address chain: `0x005F40D0 → 0x005F4240/0x005F4280/0x005F42C0 → 0x005F09F0/0x005F1030/0x005F1090 → 0x005F05B0/0x005F0B70`; camera `0x005F0350 → 0x007886B0`; feedback `0x005F0410 → 0x00421B20`; sound `0x0040D9C0/0x00410660 → 0x0040BFA0 → 0x00417320`. Read-only decompiler exports are private and excluded from publication.

Outstanding: identify light field meanings and lifetime; determine whether any other effect-instance virtual methods consume the stored sound handle; audit `.ffe` loader error handling and no-device behavior; assess malformed `CAMS` radius zero behavior (the branch compares squared distance first, but no conclusion about zero division is asserted). Ten stable names from the first runtime block plus six feedback-specific new names (and one refined name) bring the registry to **2,230 unique addresses / zero duplicates**. This is an asset/runtime result, not a packet-map change; planning percentages remain 96% implementation-grade / 90% comprehensive pending broader denominator review.
