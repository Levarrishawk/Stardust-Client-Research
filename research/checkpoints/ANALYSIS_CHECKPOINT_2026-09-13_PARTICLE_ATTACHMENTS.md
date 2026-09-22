# Particle attachment records and lifecycle checkpoint

Date: 2026-09-13

Authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Outcome

The common PTCL attachment vector and its 0x24-byte `PATT` elements are now mapped from disk through runtime spawning and cleanup. Attachments reference ordinary appearance-template resources, activate on birth, death, an authored normalized-age interval, or collision, inherit the particle transform at creation, and optionally die with the parent particle/group.

## PATT object and dispatch

The constructor `0x007C23C0` initializes a cached appearance-template pointer at `+0x04`, resource-name storage at `+0x08`, normalized-age minimum/maximum `+0x14/+0x18`, Boolean `+0x1C`, and trigger enum `+0x20`. Defaults are ages 0/0, Boolean true, trigger 2.

Virtual loader `0x007C2880` accepts PATT 0000 and 0001:

- 0000 (`0x007C2A00`) reads resource name, two floats, five legacy U32 values, then a Boolean; runtime trigger defaults to 2. The five legacy words are intentionally not retained by this executable.
- 0001 (`0x007C2B90`) reads resource name, two floats, Boolean and U32 trigger selector. Save function `0x007C2CF0` emits 0001.

The min/max setter `0x007C27B0` swaps reversed endpoints, proving an unordered authored range normalized at load/set time. Resource assignment `0x007C26E0` resolves the logical path through the generic appearance-template manager and caches the result at `+0x04`.

## Field map

| Offset | Meaning | Confidence |
|---|---|---|
| `+0x04` | cached appearance-template pointer | Confirmed ownership / Strong narrow type |
| `+0x08` | appearance-template logical path | Confirmed |
| `+0x14` | minimum normalized activation age | Confirmed |
| `+0x18` | maximum normalized activation age | Confirmed |
| `+0x1C` | kill spawned attachment with parent | Confirmed |
| `+0x20` | trigger classification | Strong enum labels; exact editor spelling unknown |

## Trigger behavior

At particle initialization/update, the client samples a uniform random activation threshold between minimum and maximum. When normalized particle age reaches it and no attachment instance exists, `0x007A72F0` constructs the referenced appearance, gives it the particle transform, registers it, and retains it. A type-3 record is additionally checked in the collision-success path and spawned there immediately.

| Value | Provisional trigger name | Corpus evidence | Confidence |
|---:|---|---|---|
| 0 | birth | 187/187 records use range 0..0 | Strong |
| 1 | death | 28/28 records use range 1..1 | Strong |
| 2 | authored/random normalized age | 557 records; most 0..0 but all varied ranges occur here | Strong |
| 3 | collision | only two records; paths are ground-collision/explosion effects; executable explicitly checks value 3 inside collision handling | Confirmed behavior / Strong name |

Values 0-2 are not separately switched on in runtime code; their authored ranges implement their timing. Preserve both selector and range because editors/data may rely on classification even where execution converges.

The Boolean is copied to the spawned particle-attachment wrapper by `0x007B5760`. Destructor `0x007C4B40` reads it through `0x007B5770`; if true, it calls the child appearance's kill/terminate virtual before releasing the watcher. This confirms kill-with-parent behavior.

## Effective corpus

`scan_particle_attachments.py` found 774 effective records, all PATT/0001, with zero asset scan errors and 382 unique referenced paths.

| Category | Counts |
|---|---|
| Trigger | 0: 187; 1: 28; 2: 557; 3: 2 |
| Kill with parent | true: 754; false: 20 |
| Path extension | `.prt`: 608; `.swh`: 157; `.apt`: 5; `.msh`: 3; `.sat`: 1 |

The resource path is intentionally generic appearance-template input. Direct `.msh` and `.sat` occurrences coexist with `.prt`, `.swh`, and `.apt`, so a replacement must use the normal appearance registry/factory rather than hard-code particle-effect recursion.

## PTCL prefix closure

All PTCL versions begin with a chunk containing a NUL-terminated description string and random-initial-orientation Boolean; PTCL/0002 and /0003 then include attachment count. A deployed sample contains `Default`, confirming `+0x98` is an editor-facing description/name rather than a resource path. `+0xA4` is consumed when initializing quad rotation and all three mesh rotation axes: true permits random sign/orientation initialization. PTCL/0001 reads an additional legacy string but no deployed PTCL/0001 record exists; its exact editor label remains Unknown.

## Replacement contract

Represent attachments as value records containing logical appearance path, normalized age range, trigger enum and kill-with-parent policy. Resolve through the shared appearance-template system. Each particle may retain a child appearance instance; spawn once, initialize from the particle transform, update according to attachment/appearance behavior, and honor explicit termination policy. Do not persist cached pointers or the legacy 0x24-byte in-memory layout.

## Remaining narrow unknowns

1. Exact editor terminology for trigger values and the PTCL/0001 extra string.
2. Whether any unavailable/non-effective legacy asset depends on PATT/0000's five discarded words.
3. Precise distinction between particle/group destruction paths for children whose kill-with-parent flag is false; the observed path deliberately omits explicit kill before watcher release.

Overall weighted client-research progress remains approximately 74%. This closes another contained asset/runtime family without inflating the whole-client estimate.
