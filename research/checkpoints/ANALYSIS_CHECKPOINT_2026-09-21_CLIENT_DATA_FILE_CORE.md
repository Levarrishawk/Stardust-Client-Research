# ClientDataFile core: CLDF loader, event map, and object augmentation

Scope: the `ClientDataFile` factory/loader/runtime around `0x006032D0..0x0060Axxx`, plus the effective human-player CDF. This is a major original-asset compatibility boundary.

## Factory and version boundary

`0x006033C0` installs `ClientDataFile`, its 132-byte fixed-block allocator, and registers file tag `CLDF`. `0x00603470` is the factory; `0x006034D0` constructs the client-data object. `0x00603EA0` accepts `FORM CLDF`, dispatches version `FORM 0000` to `0x00606250`, and sorts/indexes loaded records for runtime lookup.

The version-0000 loader has explicit branches for the following observed chunk/form tags:

```text
APPR ASND BANR BRKP CEFT CEFU CHL2 CHLD CNCF CONT CONU
CSND CSSI DAMA DATA DSEF DSEG DSTR ENGS EVNT EVNU FLAG
GLOW HARD HLOB HOBJ HOBK IHOB INFO INTS LOBJ ONOF ONOG
PSOR SUNT TURR VGEF VTHR VTHS WEAR WING
```

This list is confirmed from the loader comparisons. Some expansions are evident (`EVNT`, `GLOW`, `DAMA`, `WEAR`, `WING`, `TURR`, `BRKP`), but provisional names should not be assigned to every acronym without payload/caller evidence.

## Confirmed event-map contract

Within `CLDF/0000`, each `EVNT` chunk is exactly two NUL-terminated strings:

```text
symbolic event name
resource path
```

`0x00605DD0` performs the runtime event lookup used by `ClientEventManager`. It searches the CDF's event map and applies a default/fallback lookup where appropriate. This proves the two-layer event architecture:

```text
animation/network symbolic event
  -> object-specific CLDF EVNT map
  -> resource (.snd/.cef/...)
  -> audio/effect presentation subsystem
```

## Effective human-player example

`clientdata/player/client_shared_player_human_m.cdf` is supplied by `mtg_patch_012_configurable_01.tre` at configured priority 41. It is `CLDF/0000` with 29 `EVNT` entries: 26 `.snd` resources and 3 `.cef` resources.

Confirmed examples include:

| Event | Resource/use |
|---|---|
| `blowkiss`, `cough`, `hiccup`, `sigh`, `sneeze`, `sniff`, `yawn` | gender/species voice `.snd` files |
| `footstep_<surface>` | carpet, grass, hard/soft dirt, metal, mud, rock, sand, snow, stone, surf/water, wood sounds |
| `footstep` | `clienteffect/e3_player_footstep.cef` |
| `hit` | `clienteffect/combat_melee_hit_unarmed.cef` |
| `swing` | `clienteffect/combat_melee_swing_unarmed.cef` |
| `hitground`, `hitheavy`, `hitlight` | body-fall and damage voice sounds |
| `swim` | surface-water sound |

This is direct evidence that correct original animation behavior depends on CDF lookup, not just animation or sound-file support in isolation.

## Corpus-wide effective-schema inventory

Correction (2026-09-22): the original scan used the compressed-size field to slice kind-0 TRE records. That omitted 84 effective live CDF paths and chose a lower-priority duplicate for two others. The corrected configured-overlay scan finds **6,288 effective `.cdf` paths**: 6,284 valid `CLDF/0000` files and four zero-byte overlay tombstones. The added files contain 75 `ASND`, five `CEFT`, and three `HOBJ` records; one valid CDF has no child tag. This establishes both the scale of the compatibility requirement and the fact that only version `0000` is active in the installed live corpus. See `ANALYSIS_CHECKPOINT_2026-09-22_TRE_LEGACY_TOOL_REAUDIT.md`.

Most prevalent records/forms are:

| Tag/form | Files | Instances | Interpretation |
|---|---:|---:|---|
| `CEFT` | 4,023 | 46,966 | client-effect references |
| `CSND` | 4,415 | 15,732 | client sound references |
| `FORM/WEAR` | 3,734 | 12,570 | wearable definitions |
| `MESH` | 3,734 | 17,047 | wearable/mesh references |
| `CSSI` | 3,243 | 52,040 | repeated client presentation records; exact expansion still provisional |
| `WCSI` | 3,142 | 14,273 | wearable-associated records; exact expansion still provisional |
| `ASND` | 1,016 | 2,003 | ambient/attached sound |
| `CHLD` | 615 | 5,985 | child object records |
| `APPR` | 364 | 1,350 | appearance references |
| `FORM/DAMA` | 300 | 1,225 | damage-state groups |
| `FORM/DSTR` | 218 | 221 | destruction groups |
| `FORM/BRKP` | 166 | 511 | breakpoint groups |
| `FORM/VTHR` | 107 | 403 | vehicle-thruster groups |
| `EVNT` | 18 | 522 | symbolic event/resource mappings |

Other confirmed corpus tags include `CHL2`, `PSOR`, `HOBJ`, `HARD`, `LOBJ`, `INTS`, `ONOF`, `ENGS`, `GLOW`, `TURR`, `VSND`, `HPNT`, `MASP`, `WSCL`, `VGEF`, `BANR`, `LIFT`, `FLAG`, `DSEF`, `WING`, `VLEF`, and several rare specialized forms. The corrected `local-private/generated-evidence/cdf_tags_corrected_2026-09-22.csv` preserves path, winning archive, priority, byte count, version, and per-file tag counts for targeted follow-up. The earlier `effective_cdf_inventory.csv` is superseded for aggregate counts.

Overlay provenance is highly concentrated: 4,930 winners come from `mtg_patch_012_configurable_01.tre`, 1,307 from `mtg_patch_017.tre`, with smaller planet/Stardust contributions. A replacement must therefore reproduce configured overlay resolution rather than assuming base-game CDFs.

### Common payload shapes

Corpus profiling resolves several high-value leaf contracts without guessing their surrounding class names:

- `CEFT` is overwhelmingly two NUL-terminated strings: symbolic event plus resource path. Despite the tag name, resources include both `.cef` and surface-footstep `.snd` mappings. It is the dominant event/effect mapping representation in NPC/creature CDFs.
- `CSND` is likewise two NUL-terminated strings: symbolic event plus sound resource, heavily used for `hitlight`, `hitheavy`, `hitground`, and creature footsteps.
- `EVNT` uses the same two-string semantic contract in the smaller player CDF family.
- `MESH` is one `.lmg` skeletal-mesh path inside `WEAR` forms.
- `CSSI` and `WCSI` contain a customization path such as `/private/index_color_1`, `/private/index_color_skin`, or `/private/blend_muscle`, followed by compact numeric customization data. `WCSI` is wearable-scoped.
- `CHLD` contains an appearance-template path plus a fixed transform/attachment payload; `CHL2` contains an object-template `.iff` path plus the larger transform/flags variant.
- `HARD` is a NUL-terminated hardpoint name. `HOBJ` combines appearance/hardpoint information, with variant payloads in vehicle-thruster forms.
- `PSOR` has a fixed 24-byte numeric payload, consistent with position/orientation scalar data.
- `APPR` and `ASND` are either empty/disabled sentinel payloads or one appearance/sound path.

These shapes explain why CDF support crosses the resource manager, customization system, skeletal wearables, object-template factory, hardpoint resolver, and transform math. It cannot be implemented as a simple event dictionary alone.

## Other confirmed runtime responsibilities

The loader/runtime cluster also contains evidence for:

- appearance/object creation and attachment records;
- child objects and hardpoint objects;
- lights and glow appearances, including named/unnamed glow handling and invalid-hardpoint diagnostics (`0x006052D0`);
- customization flags/configuration and undeclared-customization logging;
- damage-state properties and `damage%i` transitions (`0x00605BB0`, `0x00605EA0`);
- destruction segments/effects;
- breakpoints/hardpoints;
- wearables, wings, turrets, vehicle thrusters and related attached presentation objects;
- ambient/client sound entries and interpolated sound runtime;
- on/off state-driven presentation records.

`0x00604420` applies several loaded records to an object by creating appearances, lights/effects, attachments, and transforms. `0x006052D0` builds glow/light objects and validates hardpoints. `0x00605D10` and `0x006061C0` apply damage and named presentation transitions.

## Replacement-client requirements

- `CLDF/0000` support is non-negotiable for original assets. It is a composition/behavior graph, not optional decoration.
- Implement the loader as typed chunk records behind a versioned parser; never reproduce the legacy 132-byte object or pointer-rich record layouts.
- Start with `EVNT` plus appearance/attachment/light/damage records needed by the first rendered-object milestones, but preserve unknown chunks losslessly or fail with precise diagnostics.
- Resource dispatch must remain suffix/type aware: `.snd` goes to audio, `.cef` to client effects, appearance resources to scene construction.
- Hardpoint lookup failures must be safe and diagnosable. They are content errors, not permission to dereference null attachments.
- Overlay precedence and path normalization are part of behavior.
- CDF state is client-local; no pointer/layout details belong on the network.

## Remaining unknowns

- Full field-level schemas for all observed tags are not yet closed. Switch and corpus coverage are known, but common `CEFT`/`CSND`/`WEAR`/`CSSI`/`WCSI` and complex destruction, damage, vehicle, and attachment payloads require tag-by-tag blocks.
- Several large runtime record types remain only behaviorally classified.
- The sample player CDF exercises only `EVNT`; additional representative building, vehicle, weapon, and destructible CDFs are needed to cover the broader schema.
