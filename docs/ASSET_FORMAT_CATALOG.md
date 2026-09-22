# Installed Asset Format Catalog

## Particle visual formats (2026-09-13)

- `PTCL` 0000-0003: common color/alpha/velocity, optional three-axis rotation and attachments.
- `PTQD` detail 0000/0001: rotation, two size waveforms and PTEX; 0001 adds dimension locking.
- `PTMH` detail 0000: a chunk-held `.apt` appearance-template path followed by uniform-scale and three rotation WVFM forms.
- `PTEX` 0000: resource string, inclusive frame range, square sprite-sheet layout/timing and one unresolved Boolean.

These are disk schemas, not legacy pointer-bearing runtime layouts.

`PATT` supports 0000/0001 and occupies 0x24 bytes only in the legacy runtime. Version 0001 serializes appearance path, normalized-age minimum/maximum, kill-with-parent Boolean and U32 trigger. Version 0000 includes five obsolete words that this executable reads and discards, then defaults trigger to 2. Effective assets contain 774 PATT/0001 records and no PATT/0000.

`PTEX/0000` serializes shader path, square-atlas capacity, inclusive frame range, UV-cell extent, cells per row, FPS/mode and enabled flag. The only deployed layouts are 1×1, 2×2 and 4×4. FPS `-1` is lifetime-normalized; other values are explicit frames per second. All 5,973 effective records use version 0000.

## Appearance outer-form registry

Executable registration calls confirm `LSAT`, `MESH`, `MKAT`, `BEAM`, `CMPA`, `DTLA`, `QUAD`, `SPRT`, `SWSH`, `LEFX`, `PEFT`, `SMAT`, `PLNT`, `MPTA`, `PTAT`, `APPR`, `NRND`, and `STAT`. Each maps to a typed factory callback; factories then dispatch their own versions. `APT/0000/NAME` is a one-level logical-path indirection outside the renderable-type registry.

### MESH

Confirmed supported versions are `0002` through `0005`. Versions 0002/0003 contain explicit `CNTR` and `RADI`; versions 0004/0005 derive those values from embedded APPR extent data. Every supported version owns a `FORM SPS ` shader-primitive-set child. SPS supports at least versions `0000` and `0001`. Optional APPR `HPTS` contains repeated transform-plus-name `HPNT` hardpoints.

SPS begins with `CNT ` and repeated shader groups. Shader groups use either external `NAME` or inline shader templates, then `INFO` primitive count. SPS 0001 primitive `0000/INFO` contains topology ID and has-index/has-sorted-index flags, followed by `VTXA`, optional U16 `INDX`, and optional `SIDX` sorted-index ranges. VTXA versions 0001-0003 encode a declaration, vertex count and interleaved DATA supporting position, packed colors/scalars, normal, and up to 15 dimensioned texture-coordinate sets.

Corpus occurrence profile: 29,025 MESH-0005/SPS-0001/VTXA-0003; 11,814 MESH-0004/SPS-0001/VTXA-0003; six legacy MESH-0003/SPS-0000/VTXA-0002. There are 171 unresolved records requiring a separate audit.

Source corpus: the 58 TRE archives configured under `C:\Stardust-DEV`. This inventory was produced by read-only header/index/name-block parsing. Counts include overridden copies in lower-priority archives.

### PEFT particles

Confirmed top-level versions are `0000`, `0001`, and `0002`. All hold an ordered vector of `EMGP` emitter-group descriptions; 0001 adds `PTIM/0001`, and 0002 adds four positional float32 values after the group count. Emitter groups recursively contain versioned `EMTR` records and quad/mesh particle descriptions. Decode into an engine-neutral description graph and instantiate runtime emitters from it; do not serialize the original 32-bit pointer/vector layout.

Current configured-overlay profile: 1,305 `PEFT/0002`, 808 `PEFT/0001`, one `PEFT/0000`, and nine zero-byte tombstones. Thus all 2,114 live `.prt` winners use an executable-supported top-level version.

Nested support is likewise bounded: `EMGP` accepts 0000-0001 and `EMTR` accepts every version 0000-0014. The effective corpus has 2,113 EMGP/0001 plus one EMGP/0000, and 6,373 total emitters. Only EMTR versions 0000, 0006 and 0009-0014 occur; 0014 is dominant at 3,682 records. The full version distribution is 1/8/78/28/696/1,039/841/3,682 in that order. These replace an undercount caused by incorrectly applying classic IFF pad bytes to SWG containers. Group 0001 adds common PTIM timing before its emitter count. A runtime group creates exactly one runtime emitter per ordered description.

Modern EMTR versions 0009-0014 are append-evolved: common PTIM + sixteen WVFM records + scalar/string data, followed by extensions of 0/29/33/58/59/60 bytes. The emitter-shape enum is `{0 circle, 1 sphere, 2 rectangle, 3 cube, 4 line, 5 X}` and the nested particle-description enum is `{0 quad, 1 mesh}`. Preserve the optional sound reference and late terrain/generation flags even when a field has no current runtime consumer.

All 6,373 emitters contain sixteen structurally valid waveforms: 1,882 use `WVFM/0001` and 4,491 use `WVFM/0002`. Channels 11-13 are zero in every key in both formats and lack a direct runtime consumer. They are semantically dormant for this deployed corpus, although raw records should be retained when lossless asset round-tripping matters.

PEFT/0002's four floats are initial time scale, transition time, sustaining time scale and spatial effect scale. EMTR common `+0x114` enables per-particle random seeds, `+0x118` selects one of three orientation/reference-frame modes, and invariant `+0x11C` remains compatibility metadata. EMTR/0014 `+0x229` selects reserve particle-pool admission under allocation pressure.

## Corpus totals

| Measure | Count |
|---|---:|
| TRE archives | 58 |
| Archive records | 312,860 |
| Unique normalized virtual paths | 203,676 |
| Names present in multiple archives | 101,913 |

## Major extension families

| Extension | Records | Likely role | Confidence |
|---|---:|---|---|
| `.dds` | 48,108 | GPU texture payload | Confirmed |
| `.iff` | 42,926 | object templates, tables and typed data | Confirmed |
| `.msh` | 41,016 | static mesh | Confirmed by `MeshAppearanceTemplate` and sample structure |
| `.sht` | 37,929 | shader/material template | Confirmed |
| `.mgn` | 27,819 | skeletal mesh generator | Strong; client RTTI confirms generator/template classes |
| `.ans` | 14,653 | animation-state data | Strong; client animation-state hierarchy RTTI |
| `.stf` | 12,781 | string table/localization | Strong |
| `.snd` | 11,570 | sound template/descriptor | Strong |
| `.wav` | 10,932 | waveform audio | Confirmed |
| `.lod` | 10,306 | level-of-detail appearance | Strong |
| `.apt` | 9,690 | appearance indirection/template | Confirmed |
| `.cdf` | 8,484 | character data/customization definition | Strong |
| `.flr` | 8,123 | floor/collision/navigation mesh | Strong; `FloorMesh` RTTI |
| `.lmg` | 6,856 | LOD mesh generator | Strong |
| `.sat` | 6,719 | skeletal appearance template | Strong |
| `.prt` | 2,980 | particle template/data | Strong |
| `.cef` | 1,714 | client effect | Strong |
| `.qst` | 1,671 | quest data | Strong |
| `.spr` | 1,282 | sprite appearance/data | Strong |
| `.mp3` | 1,039 | compressed music/audio | Confirmed |
| `.pal` | 708 | customization palette | Strong |
| `.psh` | 694 | pixel shader program | Strong |
| `.vsh` | 491 | vertex shader program | Strong |
| `.pob` | 498 | portalized building/object | Strong |
| `.eft` | 467 | effect/pass definition referenced by `.sht` | Confirmed dependency |
| `.skt` | 435 | skeleton template | Strong |
| `.ilf` | 411 | interior layout/data | Tentative |
| `.lat` | 337 | logical animation table | Strong |
| `.cmp` | 253 | component appearance | Strong |
| `.trn` | 121 | ground/space terrain | Confirmed |
| `.inc` | 511 | declarative UI include fragments plus shader includes | Confirmed by content sampling |
| `.ui` | 9 record copies | declarative root/tool UI documents | Confirmed by content sampling |

## Sampled form/version coverage

A bounded read-only sample of 80 archive records per major extension produced:

| Extension | Observed top forms / nested versions in sample |
|---|---|
| `.sht` | `SSHT/0000` (later overriding default sample also contains `0001`) |
| `.eft` | `EFCT/0001` |
| `.msh` | `MESH/0004`, `MESH/0005`; default fallback mesh uses `0003` |
| `.sat` | `SMAT/0003` |
| `.skt` | `SLOD/0000`, `SKTM/0002` |
| `.mgn` | `SKMG/0003`, `SKMG/0004` |
| `.lmg` | `MLOD/0000` |
| `.lat` | `LATT/0000` |
| `.ans` | `CKAT/0001`, `KFAT/0003` |
| `.pob` | `PRTO/0003` |
| `.flr` | `FLOR/0003`, `0004`, `0005`, `0006` |
| `.trn` | ground `PTAT/0013` and `0014`; space `STAT/0000` |
| `.cdf` | `CLDF/0000` |

This is evidence of installed format diversity, not a claim of exhaustive version support. One sampled TRN and one CDF did not expose a normal FORM header and require separate classification.

The effective `.lmg` corpus contains 4,882 `MLOD/0000` files. INFO stores a
signed-16 NAME count followed by that many mesh-generator paths. Files contain
one through eight names (4,405 contain four), while Stardust.exe returns
`min(count, 4)` through its runtime interface. Children are lazy-resolved and
reference-count cached. `ClientSkeletalAnimation.skipL0` can skip the first
child for multi-level files. See
`ANALYSIS_CHECKPOINT_2026-09-12_MLOD_RUNTIME.md`.

The effective `.skt` corpus contains 157 `SLOD/0000` containers with one to
seven inline SKTM/0002 children (four children in 93 files). SLOD parses and
owns children eagerly, exposes `min(count, 4)`, and shares optional
`ClientSkeletalAnimation.skipL0`. See
`ANALYSIS_CHECKPOINT_2026-09-12_SLOD_RUNTIME.md`.

## Shader/effect fallback structure

Installed `effect/a_simple.eft` contains multiple `IMPL/PASS` implementations: a vertex/pixel shader 2.0 pair, a shader 1.1 pair, and a fixed-function pass. This confirms that an EFT selects among capability-specific implementations and that SHT supplies material/texture values into a chosen effect/pass. The replacement should compile these semantics into modern immutable material variants rather than retain runtime D3D9 capability fallback branches.

Executable version dispatch now confirms `EFCT` versions 0000-0001, `IMPL` versions 0000-0007, and `PASS` versions 0000-0009. EFCT contains an ordered set of inline or NAME-referenced implementations and selects the first supported one. Later IMPL forms carry SCAP capability requirements and optional OPTN selectors before their pass count/data. Exact PASS render-state fields are not yet claimed.

The raw installed corpus contains 467 live EFT and 36,686 live SHT records with zero parse errors. Outer versions are EFCT 0000 (4), EFCT 0001 (463), SSHT 0000 (34,302), and SSHT 0001 (2,266). Embedded PASS counts are 0000=2, 0002=2, 0005=4, 0007=86, 0008=658, 0009=1,697, and 0010=108. Embedded IMPL 0008/0009 also occur 82 times. PASS 0010 and IMPL 0008/0009 exceed the traced main executable dispatch ranges and are retained as a compatibility/reachability issue, not discarded as scanner noise.

PASS 0009 has an explicit 56-byte DATA payload: one U8 child-stage count, twenty-three additional U8 state values, and eight U32 state/tag values. It then chooses fixed-function `PFFP` or named vertex shader `PVSH`, followed by pixel shader `PPSH` or repeated fixed-function texture stages. Exact state labels are being assigned only through consumers.

Consumer tracing through the configured `s207_r.dll` labels PASS runtime offsets `+0x30/+0x34` as alpha-test enable/reference, `+0x40/+0x44` as texture-factor tags, `+0x48` as texture-scroll tag, `+0x4C/+0x50` as stencil-reference enable/tag, and `+0x7C` as the material tag. These labels are **Confirmed** by calls to named `StaticShader` exports. The backend compiles each logical pass into a cached 0xA4-byte D3D9 record; this size is an implementation detail and must not become an asset-format contract.

Fixed-function texture-stage dispatch `0x00762C20` accepts versions 0000 and 0001. Both populate a stable 0x60-byte engine record. Version 0000 reads fifteen operation/argument U8-or-boolean values, two U32 tags, then seven more U8 values; version 0001 reads the same prefix and two U32 values but only its final U8 field, leaving the intervening legacy values at constructor defaults. Individual color/alpha/sampler labels remain pending downstream consumer correlation.

The D3D9 consumer closes the sampler subset as maximum anisotropy, address U/V/W, and mip/min/mag filtering, with TextureData defaults plus stage overrides. Color/alpha combiners remain structurally present but not yet labeled.

Configuration-aware overlay resolution found 23,393 winning shader/effect paths. Twenty-seven live winners contain IMPL 0009/PASS 0010; no IMPL 0008 winner remains. A replacement must support visible 0009/0010 grammar even though the traced Stardust.exe dispatchers stop at IMPL 0007/PASS 0009.

SPS topology IDs 0-11 are exactly non-indexed then indexed point list, line list, line strip, triangle list, triangle strip, and triangle fan. SIDX stores multiple `(local direction, index buffer)` variants; the client chooses the minimum camera-direction dot product before binding indices.

## Client class families that constrain conversion

Stardust.exe RTTI confirms the following native runtime concepts:

- appearance templates: base, mesh, skeletal, component, detail/LOD, sprite, particle, beam, lightning, lightsaber, planet, terrain, no-render and owner-proxy variants;
- skeletal rendering: `SkeletalAppearance2`, skeletal appearance templates, basic/LOD skeleton templates, skeleton transform-name maps, skeletal mesh generators and batch renderers;
- customization: ranged integers, palette colors, slots, local/remote customization directories, template variable mappings, customizable shader templates and blueprint texture operations;
- animation: keyframe, blend, direction, speed, yaw, time-scale, string-selector, proxy/masked-priority animations, track controllers and state hierarchy templates;
- world data: portal and cell properties, floor/collision meshes, collision properties, sphere-tree spatial subdivisions and DPVS-linked render-world notifications.

This breadth means conversion should first produce typed engine-neutral intermediate resources. Direct one-off import from each legacy extension into an engine scene would lose cross-resource identity, overlay semantics and customization linkages.

## Next format samples

The minimum representative corpus for the later engine evaluation should include one static appearance chain (`APT -> MSH -> SHT/EFT -> DDS`), one customizable skeletal character chain (`CDF/SAT -> SKT/MGN -> animation graph -> customizable shaders/palettes`), one portal building (`POB/FLR`), and one ground terrain (`PTAT/TGEN/maps`).

The portal-building sample is now concrete: highest-priority `appearance/yt1300.pob` is `PRTO/0004` and references an exterior APT, seven cell meshes and seven FLOR collision/navigation resources. Sample `yt1300_r1_hall1_collision_floor0.flr` is `FLOR/0006` with 38 vertices, 36 triangles, BTRE, BEDG and PGRF data. See `INTERIOR_PORTAL_ARCHITECTURE.md`.

## Effective IMPL 0009 / PASS 0010 grammar

A configuration-aware scan recorded every `IMPL` and `PASS` DATA payload and child sequence in the 23,286 live winning SHT/EFT records. It found 82 `IMPL 0009` records, all with seven-byte DATA, and 108 `PASS 0010` records, all with 57-byte DATA. The corresponding supported late schemas are 102 `IMPL 0007` records at seven bytes and 1,325 `PASS 0009` records at 56 bytes.

`IMPL 0009` preserves the supported outer grammar: mandatory `SCAP`, optional `OPTN`, seven-byte `DATA`, then one or more `PASS` forms. Its DATA remains `{U8 passCount, U32 phaseTag, U8 flag, U8 flag}` by structural continuity; whether the two flags retain identical semantics is **Strongly inferred**, not confirmed by executable code because Stardust.exe has no 0009 loader.

`PASS 0010` likewise preserves both supported child alternatives exactly: `DATA -> PFFP -> zero or more STAG`, or `DATA -> PVSH -> PPSH`. Its extra byte occurs in the scalar prefix before the first U32 tag: the late layout is 13 scalar bytes, four U32 tags/values interleaved with four scalar bytes, eight more scalar bytes, then three U32 values. The added scalar's precise ordinal and meaning remain **Unknown**. No same-name older definition exists in a lower-priority configured archive for any affected winning asset, so archive differencing cannot safely assign it.

## Fixed-function STAG schema

The effective overlay contains 951 `STAG 0000` records and no winning `STAG 0001` records. Every 0000 payload is exactly 33 bytes. Loader order and constructor defaults establish this engine record:

| Serialized bytes | Provisional semantic group | Confidence |
|---:|---|---|
| 0-1 | color operation, argument 1 | Strongly inferred from exact D3DTSS ordering and corpus values |
| 2-3 | color argument-1 complement and alpha-replicate flags | Strongly inferred |
| 4-6 | color argument 2 and its two modifier flags | Strongly inferred |
| 7-9 | color argument 0 and its two modifier flags | Strongly inferred |
| 10-12 | alpha operation, argument 1 and complement flag | Strongly inferred |
| 13-14 | alpha argument 2 and complement flag | Strongly inferred |
| 15-16 | alpha argument 0 and complement flag | Strongly inferred |
| 17 | result argument | Strongly inferred |
| 18-21 | texture-data parameter tag | Confirmed by `StaticShader::getTextureData` lookup |
| 22-25 | texture-coordinate-set parameter tag | Confirmed by `StaticShader::getTextureCoordinateSet` lookup |
| 26-28 | address U/V/W overrides | Strongly inferred from defaults and sampler compiler |
| 29-31 | mip/min/mag filter overrides | Confirmed by D3DSAMP 7/6/5 emission |
| 32 | late trailing enum | Unknown |

Corpus ranges support the interpretation. Color operations use internal values 1,2,3,4,6,12,13 and 15; the dominant value 3 corresponds to classic D3DTOP_MODULATE when translated by `+1`, while 1 and 2 correspond to SELECTARG1/SELECTARG2. Address values are 0/2 with sentinel 5, matching wrap/clamp plus an inherited/default sentinel. Filter values are 0/1/2 with sentinel 6, matching none/point/linear plus inherited/default. These numeric mappings are **Strongly inferred**, because the recovered s207 build does not contain a statically reachable STAG compiler.

`STAG 0001` has a reduced 19-byte payload in code: the same first 18 scalar fields and only the final trailing enum. The two tags and six address/filter overrides remain at constructor defaults. It is supported by Stardust.exe but absent from the effective installed corpus.

## Static-shader texture map (`TXMS/TXM`) schema

`SSHT` versions 0000 and 0001 both iterate `FORM TXMS` children through `0x007813E0`. Each child is `FORM TXM ` with versions 0000, 0001 or 0002. The effective configured corpus contains 41,741 records: two v0000 records of five bytes, 39,668 standard v0001 records of 11 bytes, 11 v0001 records with a tolerated twelfth zero byte, and 2,060 v0002 records of 12 bytes.

| Version | DATA payload | Confirmed interpretation |
|---|---|---|
| 0000 | U8 special/dynamic flag, U32 tag | address defaults 5/5/5; filter defaults 6/6/6; anisotropy 1 |
| 0001 | U32 tag, U8 special/dynamic flag, six U8 values | address U/V/W, then mip/min/mag filter; anisotropy 1 |
| 0002 | v0001 plus one U8 | final byte is maximum anisotropy |

If the special flag is clear and the tag is not `ENVM`, `0x00764F90` consumes an optional `NAME` chunk and `0x00764F20` interns the filename through a `TemporaryCrcString` before fetching the texture resource. Otherwise the fixed texture pointer is initially null and can be supplied dynamically. Texture-data entries are keyed by the four-character tag and occupy 36 bytes internally, but that pointer-bearing 32-bit layout is not a file format.

The dominant effective tags are `MAIN` (22,893), `SPEC` (4,975), `NRML` (2,936), `CNRM` (2,806), `LKUP` (2,657), `ENVM` (1,435), `DETA` (1,379), `HUEB` (669), `MASK` (552) and `EMIS` (494). These are stable resource identities that a modern material system must preserve.

The s207 translation tables confirm address values 0-4 map to D3DTADDRESS WRAP/MIRROR/CLAMP/BORDER/MIRRORONCE; value 5 is the inheritance sentinel and falls back to WRAP if retained. Filter values 0-3 map to NONE/POINT/LINEAR/ANISOTROPIC, while sentinel 6 falls back to LINEAR after inheritance resolution.

## DDS texture payloads

`Texture` construction at `0x00769170` opens the logical texture name through `TreeFile`, reads the four-byte `DDS ` magic followed by the standard 124-byte DDS header, and derives width, height, mip count, cubemap state and pixel format. The deployed effective overlay contains 30,109 `.dds` namespace winners: 29,903 valid DDS resources, 203 tombstones and three IFF static-shader records misnamed with a `.dds` suffix.

The valid corpus uses only seven physical encodings:

| Encoding | Effective resources |
|---|---:|
| DXT5 / BC3 | 21,673 |
| DXT1 / BC1 | 4,874 |
| 32-bit BGRA masks with alpha | 2,325 |
| DXT3 / BC2 | 910 |
| 24-bit BGR masks | 113 |
| 16-bit A1R5G5B5 masks | 4 |
| 8-bit luminance | 4 |

The executable additionally recognizes DXT2, DXT4 and `PNM1`, but none is an effective installed DDS payload. `PNM1` maps to internal texture format 13 and is treated as block-compressed. There are 174 cubemaps, eight non-power-of-two textures and 1,668 resources without a mathematically complete mip chain. A compatible importer must respect the declared mip count rather than synthesize assumptions from dimensions.

The three invalid extension records are `shader/pt_anim_atom_ring.dds`, `shader/pt_dust_rocks.dds`, and `shader/pt_rays_hourglass.dds` from `mtg_patch_009_Shader_01.tre`; their payload begins `FORM SSHT`, not `DDS `. This is a namespace/content anomaly, not a corrupt DDS decoder result.

## Environment tables and celestial definitions

`datatables/environment/<terrain>.iff` uses DTII/0001 with 25 explicit typed columns. The runtime loader consumes family name, weather index, gradient-sky texture, two cloud shader/size/speed triplets, a 256×8 lighting-ramp TGA, shadow/fog controls, two fog-density scalars, an optional attached-camera appearance template, day/night environment maps, seven audio paths and wind scale. Rows are indexed by the PTAT-resolved key `(familyId<<16)|weather`, with same-family weather-zero fallback. Fifty effective tables parse cleanly in the configured overlay, totaling 1,337 rows.

`terrain/environment/<terrain>.iff` is a separate ENVM/0000 resource. The loader recognizes NSKY, STAR, SUN, SSUN, MOON, SMOO and repeated CELS chunks to construct shared night-sky and celestial resources. These two environment formats must not be conflated: DTII selects regional/weather descriptors, while ENVM defines planet-wide celestial content.
