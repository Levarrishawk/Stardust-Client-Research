# Combat playback variable bindings (2026-09-23, block 1 of 3)

## Confirmed executable path

`SNGL` parser `0x00466F40` reads a `NAME` path and an optional `FORM VARS`. For each variable chunk it reads the chunk tag, looks up a registered parser in the `DAT_0190A304` table, creates a typed filler object, and appends that filler to the selector's vector at `+0x14..+0x1C`. `ClientCombatPlaybackManager::install` at `0x00469250` installs the three relevant factories before opening `combat/combat_manager.iff`:

| IFF chunk | Registration | Parser | Filler virtual `+4` | Playback store |
|---|---|---|---|---|
| `STRN` | `0x004665A0` | `0x00466630`: four-byte tag + bounded NUL string | `0x00466320` | string setter `0x00407B00` |
| `FLOT` | `0x00466890` | `0x00466920`: four-byte tag + float32 | `0x004669D0` | float setter `0x00407F20` |
| `INT ` | `0x00466B40` | `0x00466BD0`: four-byte tag + signed/unsigned 32-bit payload (interpretation by consumer) | `0x00466C80` | integer setter `0x00408130` |

The vtable targets were read from `Stardust.exe`; the decompiled parsers show the fixed-width tag/payload reads. `SNGL` runtime `0x00467370` fetches and instantiates the `.pst`, iterates its filler vector, calling each virtual `+4` on that instance, then sets additional combat-context integer and actor variables. Thus file-provided values are applied **before** context-derived values; for a duplicate tag, the later store can overwrite an earlier one. Exact duplicate behavior in every variable map still depends on the relevant setter; do not assume general append semantics.

The typed playback setters maintain separate tag-keyed string (`0x00407B00`), float (`0x00407F20`) and integer (`0x00408130`) maps. Unknown tags return failure and can warn when debug reporting is enabled. Actor bindings use a distinct object/reference setter `0x00408210`. These are client-internal values, not new packet fields.

## Effective asset census

Recursive read-only traversal of the configured effective 474,358-byte `CBTM/0002` asset found **2,641 `SNGL` leaves**, **7,418 variable binding instances**, and **31 distinct `(chunk type, raw four-byte tag)` combinations**. The most common raw on-disk tags are:

| Type/tag (raw byte order) | Count | Observed value character |
|---|---:|---|
| `STRN/TCAA` | 2,539 | attack animation names, sometimes empty |
| `STRN/CAHD` | 2,071 | hit/block/dodge animation names, sometimes empty |
| `STRN/TCAD` | 249 | dodge/duck/hit names |
| `STRN/CAMD` | 240 | block/dodge names |
| `STRN/BDNS`, `EDNS`, `LDNS` | 237 each | `.snd` paths (begin/end/loop naming is inferred from path strings) |
| `STRN/TPHE` | 158 | `hold_l` or `hold_r` |
| `STRN/LTTA` | 155 | textual scalar-looking values (`.0.5`, `3.0`) |
| `INT /LRTP` | 142 | 32-bit values 0/1 |
| `INT /LVEL` | 134 | small 32-bit values (1, 2, 3, 4, 5, 12 observed) |
| `FLOT/CRFG`, `FLOT/CRFD` | 134 each | float32 constants |
| `STRN/TPAP` | 103 | particle-appearance paths |
| `STRN/NTPA` | 72 | `.ltn` appearance paths |
| `STRN/NTOW`, `STRN/TOBF` | 50 each | object-template paths |

Counts are binding occurrences across nested selectors, not distinct playback instances. Four-byte tags are printed in **raw IFF payload byte order** here; code may display a numeric FourCC differently. `STRN/LTTA` and `FLOT/LTTA` both occur, so a future loader must retain *type as well as tag*. Do not infer a tag's semantic role solely from English-looking reversed letters.

## Replacement-client boundary

Treat a selected `SNGL` as `{script_path, initial_actor_index_or_flag, typed_bindings[]}`. Parse `VARS` as typed data; preserve original four-byte tag identity and later context overrides. The renderer/animation/audio consumers should query typed playback state through an engine-neutral interface. The wire protocol only supplies the combat action context and selector key; the server does not transmit this binding vector.

Next block: trace representative `.pst` action implementations to see which of these bindings they read and what original assets they request. No client, TRE, or server file was changed.
