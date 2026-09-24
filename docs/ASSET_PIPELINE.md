# Asset Pipeline

## PTCL/PTQD particle visual descriptions (2026-09-13)

Common `PTCL` versions 0000-0003 supply color, alpha, velocity modulation, optional three-axis rotation, resource/policy data and repeated attachments. `PTQD` adds rotation, primary/secondary size curves, a lock-dimensions flag and `PTEX/0000` sprite-sheet data. `PTMH/0000` names a general appearance resource and adds uniform scale plus three rotation curves. Its 399 effective records reference 275 `.apt`, 79 `.msh`, and 45 `.sat` resources, so lookup must use the shared appearance factory. Corrected effective nested counts are PTCL 0000/0002/0003 = 1/1,803/4,569, PTQD 0000/0001 = 8/5,965 and PTMH/0000 = 399. Treat these as explicit IFF schemas and preserve unresolved fields. See the PTCL/PTQD and PTMH checkpoints.

PTCL attachments are `PATT` value records. PATT/0001 stores a generic appearance path, normalized-age min/max, kill-with-parent flag and trigger selector. The effective corpus contains 774 records and 382 unique paths spanning `.prt`, `.swh`, `.apt`, `.msh` and `.sat`; all use version 0001. Trigger 0 correlates exactly with birth/age 0, 1 with death/age 1, 2 with authored/random activation age and 3 is explicitly collision-triggered. Resolve every path through the shared appearance-template registry.

PTEX/0000 is a quad-particle shader binding and square-atlas animator. Capacity values 1/4/16 correspond to 1×1, 2×2 and 4×4 layouts. First/last frames are inclusive. FPS `-1` means select by normalized lifetime; positive values mean wrapped elapsed-time playback. The final Boolean enables shader lookup/rendering. The effective corpus contains 5,973 records: 5,965 enabled and eight disabled, with 5,172 using normalized-lifetime animation.

## Installed Stardust search tree

`C:\Stardust-DEV\swgemu_live.cfg` directly defines a priority-ordered `SharedFile` search tree with `maxSearchPriority=55`. The highest configured archives are `stardust_3_03.tre`, `stardust_3_02_n.tre`, `stardust_3_01.tre`, and `stardust_3_00.tre`, followed by MTG patch archives and the base animation, music, sample, skeletal-mesh, static-mesh, texture, other, and `bottom.tre` archives. **Confirmed local configuration.** This proves the replacement resource manager needs ordered overlay semantics; it does not yet prove the internal TRE lookup implementation in `Stardust.exe`.

The slot-54 archive is launcher-selected rather than permanently fixed. Managed
IL proves NVIDIA/default selects `stardust_3_02_n.tre` and AMD selects
`stardust_3_02_a.tre` by atomically replacing the one slot-54 line in
`swgemu_live.cfg.template`. The AMD archive's 26 records are a byte-identical
subset of the NVIDIA archive's 208; it has no unique alternate payloads. Under
AMD, 179 NVIDIA-only Mustafar ribbon/affector records disappear completely and
only three names obtain lower-priority fallbacks. Thus effective namespace
construction must begin from the post-launch configuration and treat the GPU
choice as a content/capability gate, not a simple shader-vendor substitution.
See `ANALYSIS_CHECKPOINT_2026-09-22_GPU_TRE_OVERLAY_SELECTION.md`.

The same configuration sets `ClientUserInterface.messageOfTheDayTable=live_motd`, and `options.cfg` references `misc/cache_large.iff`. These are initial concrete data-table/cache lookup targets for later archive inspection.

## Confirmed deployment model

`swgemu.cfg` includes login, live, preload, options, and user configuration files. `swgemu_live.cfg` defines a priority-ordered `SharedFile` search tree of 56 TRE archives, from `bottom.tre` through base animation/music/sample/static/skeletal/texture data, MTG patches, and Stardust-specific archives. Higher priority numbers override lower layers.

The executable exports `TreeFile::open` and uses `CrcString` types. The preload configuration names virtual paths for default textures, shader templates, appearance templates, player object templates, and UI cursor textures. This confirms a virtual-path, CRC-aware, layered archive filesystem.

## Observed formats and roles

| Format/path | Role | Confidence |
|---|---|---|
| `.tre` | archive/search-tree container | Confirmed |
| `.iff` | typed chunk/form data used for tables/config/templates | Confirmed |
| `.dds` | texture payload | Confirmed |
| `.sht` | shader template | Confirmed by preload path/strings |
| `.apt` | appearance template | Confirmed by preload path |
| `.trn` | terrain scene input | Confirmed by `CmdStartScene` and client strings |
| object `shared_*.iff` | client-side object templates | Confirmed |
| datatables `*.iff` | music, scan code, faction, mount, quests, maps, etc. | Confirmed by strings |
| `.m3d`, `.asi` | Miles provider/plugin files in `miles` | Strongly inferred |
| `.uis` | Per-character `UIST/0003` owner state: resolution, geometry, Unicode strings, opaque blobs and S32 values | Confirmed by executable load/write and four deployed samples |
| `.wpg` | Ship weapon-group profile | Confirmed separately in the ship weapon-group analysis |
| `.ui`, `.inc` | legacy UI object/style tree, include graph and event-expression data | Confirmed by complete effective-overlay inventory and 2,720-row event fixture |

The effective UI style layer contains 33,695 glyph records, 10,669 image-style
records, 200 image frames, 78 palettes and 32 text styles. Glyphs reference 110
atlas pages with explicit code/advance/rect metrics; image styles reference 191
ordinary or transform-qualified atlas identities. See
`ANALYSIS_CHECKPOINT_2026-09-22_UI_STYLE_FONT_ATLAS_PIPELINE.md` and
`ui_style_rows.tsv`.

## Installed archive corpus inventory

A read-only parse of all 58 installed TRE headers, record blocks and name blocks found:

- 312,860 archive records;
- 203,676 unique normalized virtual paths;
- 101,913 virtual names occurring in more than one archive;
- one observed archive profile across all 58 files: on-disk magic `EERT`, version bytes `5000` (the little-endian representation tested as `TREE` / `0005` by the original-style reader), and compression type 2 for both record and name blocks.

The high duplicate count confirms that overlay priority is fundamental behavior, not an edge case. A resource manager must preserve the configured numeric search priority and record the winning archive for diagnostics/cache identity.

Largest format families by raw record occurrence are documented in `ASSET_FORMAT_CATALOG.md`. The dominant rendering/appearance chain is quantitatively visible: 48,108 DDS textures, 42,926 IFF files, 41,016 static meshes, 37,929 shader templates, 27,819 skeletal mesh generators, 14,653 animation-state assets, 10,306 LOD assets, and 9,690 appearance templates.

## Confirmed TRE 0005 structure

Current Stardust server source `tre3/TreeFile.cpp`, `TreeFileRecord.h`, and `TreeDataBlock.h` provide a directly usable format reference and match the installed headers:

`magic[4] | version[4] | U32 recordCount | U32 dataOffset | U32 recordBlockCompression | U32 recordBlockCompressedSize | U32 nameBlockCompression | U32 nameBlockCompressedSize | U32 nameBlockUncompressedSize`

At `dataOffset`, the record block expands to `recordCount * 24` bytes. Each record contains six little-endian U32 values:

`checksum | uncompressedSize | payloadFileOffset | compressionType | compressedSize | nameOffset`

The name block follows the compressed record block and contains NUL-terminated virtual paths addressed by `nameOffset`. Sixteen MD5 bytes per record follow the name block. Compression type 2 uses zlib; type 0 is stored. Payload offsets are absolute archive offsets.

This is **Confirmed by server source plus mechanical parsing of every installed archive header/index**. Version 0006 appears in the server reader but was not observed locally and is not yet specified sufficiently for implementation.

## Representative dependency chains

Read-only extraction of the highest-observed copies found:

- `appearance/defaultappearance.apt`: `FORM APT / FORM 0000 / NAME`, referencing `appearance/defaultappearance.msh`.
- `appearance/defaultappearance.msh`: `FORM MESH / FORM 0003`, containing `SPS`, center/radius data, and `EXBX` extent data.
- `shader/defaultshader.sht`: `FORM SSHT / FORM 0001`, containing `NAME`, material (`MATS`), texture map (`TXMS`) and texture-coordinate-set (`TCSS`) forms; it references `effect/a_simple.eft` and `texture/defaultshader.dds`.
- `object/creature/player/shared_human_male.iff`: `FORM SCOT`, with derivation data and nested versioned `STOT`/`SHOT` template forms. Retained strings include animation-map, movement, collision, slope and terrain-alignment attributes.
- `terrain/corellia.trn`: `FORM PTAT / FORM 0014`, containing a terrain `DATA` chunk, procedural generator `TGEN/0000`, and a map form with 524,288-byte `WMAP` and `SMAP` chunks.

The human player template directly references `appearance/hum_m.sat` and `clientdata/player/client_shared_player_human_m.cdf`. The SAT in turn references body/head LOD mesh generators, body/face skeletons, and logical animation tables. The CDF references voice sounds, footsteps, body-fall sounds and client effects. This confirms that creating one visible player spans object-template, skeletal appearance, mesh generator, skeleton, logical-animation, client-effect and audio resource managers.

These establish that appearance and shader loading are dependency-graph operations over virtual paths; an engine importer cannot treat each file as an isolated mesh or texture.

## Data-table format reference

Current server `DataTableIff.cpp` provides a format reference matching installed tables: `FORM DTII / FORM version`, followed by `COLS`, `TYPE`, and `ROWS` chunks. `COLS` begins with U32 column count and NUL-terminated names. `TYPE` supplies one NUL-terminated descriptor per column; the leading character selects float, string, hexadecimal U32, binary/bool, or integer-like decoding. `ROWS` begins with U32 row count and then stores cells in row-major column order.

The installed wearable appearance map confirms `DTII/0001`, three string columns and 55 rows. A replacement can share one bounded table decoder across appearance mapping, customization, terrain metadata, UI/game data and other table-driven systems.

## Dependency assessment

World rendering, object appearance, animation, terrain, UI art/layout, audio lookup, effects, and localization depend heavily on the original asset pipeline. Login and minimal network protocol work does not. This strongly supports a staged project in which the first client POC has no TRE dependency beyond optional configuration.

## Replacement approach

1. Implement read-only layered TRE mounting with deterministic priority and path normalization.
2. Implement a bounded IFF reader that preserves four-character tags and rejects malformed sizes.
3. Add format decoders one family at a time, with corpus tests from legally available local assets.
4. Convert decoded data into engine-neutral intermediate descriptions; do not leak packed file structs into runtime models.
5. Cache derived modern GPU/audio data separately, keyed by source archive identity and decoder version.

## Unknowns/blockers

- TRE version 0006 behavior, any compression types beyond observed 0/2, filename checksum semantics, and malformed-data behavior.
- IFF form versions for appearances, meshes, skeletons, animations, terrain, portals, shader templates, and UI.
- Whether some Stardust archives require modified schemas compared with stock client data.
- Exact client fallback/override behavior for duplicate paths and missing assets.
- Shader bytecode/source availability and whether faithful material conversion needs offline preprocessing.

No client/TRE asset was modified in this pass.

## Executable-backed VFS mount and lookup semantics

This block closes the principal uncertainty around mount precedence. `TreeFile::open` is a retained public symbol at `0x00A931E0`. Startup installer `0x00A92800` reads `[SharedFile] maxSearchPriority` (default 20), then enumerates `searchPath_%s_%d`, `searchTree_%s_%d`, and `searchTOC_%s_%d` for every priority from zero through the maximum. Multiple values at the same key are all mounted. Optional numbered configuration sets are also processed before `searchAbsolute` and `searchCache` nodes are added.

All search nodes carry a priority at object offset `+4`. Insertion routine `0x00A92B20`, with comparator `0x00A92B00`, keeps the global node vector in descending numeric priority. `TreeFile::open` walks that vector from beginning to end and returns the first non-null `AbstractFile`. Consequently, **the first resource found at the highest configured priority wins**. This is confirmed by executable code, not merely inferred from configuration order.

The normalized lookup key is produced by `0x00A930D0` before cache or node lookup. It:

- removes all leading `/` and `\\` separators;
- removes leading `./` segments;
- recognizes and advances over leading `../` segments rather than retaining them;
- converts both slash forms to `/`;
- collapses repeated separators; and
- applies the client character-folding helper to non-separator bytes (strongly inferred lowercase/case folding; exact locale behavior remains to be pinned down).

An enabled file cache is checked first under a lock. A hit transfers the retained `AbstractFile` out of the cache entry. Search-node traversal follows only after a miss. Successful opens increment global open-count and byte-count statistics. If no node resolves the path, the third `TreeFile::open` argument controls failure behavior: false invokes fatal `open [%s] not found`; true returns null. Existing call sites therefore establish that this boolean means **allow missing**, despite misleading decompiler parameter names.

RTTI confirms the node family `TreeFile::SearchNode`, `SearchPathA`, `SearchAbsolute`, `SearchTree`, `SearchTOC`, and `SearchCache`. `searchPath` constructor `0x00A98E30` converts its configured directory to an absolute path, normalizes separators, and strips a trailing slash. `SearchTree` constructor `0x00A992E0` accepts TREE versions `0004` and `0005`; anything else is fatal corruption. `SearchTOC` constructor `0x00A99930` requires TOC version `0001`.

### SearchTree index and extraction

`SearchTree` allocates `recordCount * 24` bytes for its index and a separate name block. It reads the two blocks directly when their compression flags are zero; otherwise it reads compressed bytes and invokes the shared decompressor. Its lookup `0x00A99550` computes a filename CRC, performs binary search by `(CRC, normalized path)`, and treats records whose payload-size field is zero as tombstones: the node reports a shadow/mask result to `TreeFile::open` rather than allowing a lower-priority archive to satisfy the same name.

The open virtual `0x00A99780` has two payload paths:

- stored record: construct an archive-backed `AbstractFile` over archive handle, absolute payload offset, and uncompressed length;
- compressed record: read compressed bytes, decompress eagerly into an owned buffer of the declared uncompressed size, then return a memory-backed `AbstractFile`.

`SearchTOC` uses the same CRC-plus-string binary-search shape (`0x00A99CF0`) and equivalent stored/eagerly-decompressed open paths (`0x00A99EC0`), but resolves its backing file through its TOC tables. These results validate a replacement design with normalized logical paths, deterministic overlay nodes, tombstone semantics, archive slices for stored records, and bounded decompression for compressed records.

### Non-negotiable replacement requirement

The replacement client must mount and consume the original configured TRE/TOC/loose-file assets directly at runtime. Derived caches are permitted only as disposable accelerators keyed by source archive identity, record identity, decoder version, and relevant rendering settings. They cannot become the authoritative or required content format.
## Replicated resource-container texture binding

`ResourceContainerObject` provides a concrete bridge from replicated gameplay metadata into appearance resources. Its view-6 fields contain maximum stack size, ASCII resource type, and Unicode resource name. A resource-ID change marks the object dirty at `+0x460`; class virtual `resource_container_refresh_texture_VA_0070A570` later combines the replicated type/name through helper `0x0069D210`, resolves a texture, and applies it to the object's appearance under tag `REP0`. Missing or internally inconsistent strings produce explicit warnings.

For a replacement, resource stacks therefore require a resource-type-to-texture resolver and a material parameter/tag binding equivalent to `REP0`. This should be represented as a presentation adapter observing validated replication state, not embedded in the wire decoder.

The resolver chain is now exact enough to constrain the importer. `resource_icon_resolve_class_VA_004B5480` maps the replicated resource class to `appearance/ui_res_<class>.spr`; when no class mapping exists it warns and retries with `inorganic`. `resource_icon_fetch_texture_for_class_VA_004B5F40` loads that appearance, requires RTTI-compatible `SpriteAppearance`, obtains its shader, requires `StaticShader`, and requests texture tag `MAIN` (`0x4D41494E`). Missing sprite, shader, or `MAIN` texture each have distinct diagnostics. Thus resource icons are not direct DDS references: the legacy dependency is resource class -> SPR appearance -> static shader -> `MAIN` texture -> appearance parameter `REP0`.

## Exact logical-path checksum

`swg_filename_crc32_VA_00AA4790` uses non-reflected CRC-32 polynomial `0x04C11DB7`, initial state `0xFFFFFFFF`, a left-shifting table recurrence, and final complement. It hashes normalized path bytes without the NUL terminator and is identical to current server `String::hashCode`.

Read-only verification covered every record in `stardust_3_03.tre` and `bottom.tre`: 2,285 records and zero mismatches. Example vectors are `string/en/spacequest/patrol/tatooine_privateer_tier3_4.stf` -> `0x00451A4C`, `appearance/mesh/thm_tato_imprv_building_s09_r0_mesh_l4.msh` -> `0x003A7342`, and `texture/frst_conf_redwood_bark_d_n.dds` -> `0x00B7D0CF`.

Normalizer helper `ascii_or_locale_lower_VA_0131D6E8` maps ASCII A-Z to a-z in normal mode. A locale-aware CRT path exists when a process-global mode is enabled. Replacement resource identifiers should specify deterministic ASCII behavior; non-ASCII paths remain a validation case.

## Appearance-template dispatch boundary

The executable path is `appearance_template_fetch_VA_00B263C0 -> TreeFile/IFF open -> outer FORM tag -> appearance_factory_register_tag_VA_00B25E80 registry -> typed factory -> typed version loader`.

Many callers, including lazy DTLA child fetch `0x00799810`, first use `appearance_template_fetch_or_default_VA_00B260F0`. That wrapper checks whether the requested logical path is nonempty and exists in the mounted VFS (`0x00A93030`/`0x00A92FD0`). An absent or masked path is replaced with `appearance/defaultappearance.apt`, whose live redirect points to a live `appearance/defaultappearance.msh`, before calling the strict fetcher. It then increments the fetched template's reference count. This is a path-level substitution, **not** a general license to treat all fetch errors as a default appearance: a live APT with an absent target passes the wrapper's first check and its recursive target open can be fatal. The normal concrete fetch branch calls IFF open `0x00A94210` with allow-missing, then explicitly raises `Could not open appearance file %s` if open fails. A conditional branch active under the `AsynchronousLoader` state can construct a MESH template with a null IFF pointer before the ordinary open. [Later trace](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-24_DEFERRED_MESH_MISSING_TARGET.md) shows that first runtime appearance creation retries that mesh synchronously or by callback. If the new open fails, unchecked parser/close calls reach a frame-buffer underrun; the exact runtime manifestation remains unproved.

`swgemu.cfg` includes `swgemu_live.cfg`, `swgemu_login.cfg`, `swgemu_preload.cfg`, `options.cfg`, and `user.cfg`. None of the installed CFGs declares a `searchPath` or `searchTOC` entry. The startup installer nevertheless creates a `SearchAbsolute` node at one priority above the highest mounted TRE (56 under this configuration) and a `SearchCache` node after that. The absolute node tests `GetFileAttributesA` and opens through `CreateFileA`, so a loose file resolvable from the process working directory can override TRE contents. No copies of the three currently unresolved APT targets were found under `C:\Stardust-DEV`; actual process working directory and external overrides remain environment-dependent. See `ANALYSIS_CHECKPOINT_2026-09-22_APPEARANCE_FETCH_FALLBACK.md`.

`.apt` is an indirection format. The fetcher opens `FORM APT / FORM 0000 / NAME`, reads one target logical path, rejects APT-to-APT indirection, caches the association, and recursively fetches the target. Non-APT resources are opened as IFF, dispatched by outer FORM tag, and cached by `CrcString`. Unknown tags are reported as four printable characters rather than guessed.

The corrected kind-0 TRE size rule was applied to all 48 archives configured by `swgemu_live.cfg` (203,617 effective paths). Of 6,155 effective `.apt` paths, 6,153 contain a valid `APT/0000/NAME` redirect, two are genuinely zero-payload overlay entries, and none fails IFF parsing. Redirect targets are 5,052 `.lod`, 1,085 `.msh`, 13 `.cmp`, one `.iff`, one `.ssa`, and one `.prt`; none redirects to another `.apt`. Three target paths are unavailable in the configured overlay: `appearance/lod/speeder_usv5_s02.lod` and `appearance/mesh/eft_cloud_cumulus_01.msh` are absent, while `appearance/ssa/wp_rifle_dlt20a.ssa` is a zero-payload overlay winner. This is path availability, not proof that each APT is used at runtime or that loose files cannot supply a path. See `ANALYSIS_CHECKPOINT_2026-09-22_APT_IFF_OVERLAY_REVALIDATION.md` and `tools/python/apt_redirect_inventory.py`.

For uncompressed TRE records (`kind == 0`), a zero compressed-size field alone does **not** signify a zero-payload overlay entry: payload length follows the uncompressed-size field. The effective overlay has 939 `.apt` and 19 `.iff` records with `kind == 0`, compressed size zero, and nonzero uncompressed size. Earlier inventories using compressed size unconditionally could misclassify these records; the corrected APT inventory above supersedes such classifications.

Base loader `appearance_template_load_appr_VA_00B2CB60` enters `FORM APPR` and accepts versions `0001`, `0002`, or `0003`; other versions are fatal. Concrete factories allocate their type and invoke its loader. `MESH`, for example, is registered at `0x00796250` with factory `0x007962D0`.

Confirmed tags are `LSAT`, `MESH`, `MKAT`, `BEAM`, `CMPA`, `DTLA`, `QUAD`, `SPRT`, `SWSH`, `LEFX`, `PEFT`, `SMAT`, `PLNT`, `MPTA`, `PTAT`, `APPR`, `NRND`, and `STAT`. RTTI and diagnostics identify the corresponding lightsaber, mesh, marker, beam, component, detail/LOD, sprite/quad, swoosh, lightning, particle, skeletal, planet, procedural terrain, base/shared, no-render, and space-terrain families.

`CMPA` versions 0001-0005 compose resolved child appearance templates with one 3x4 local transform per `PART`. The configured overlay has 165 effective live records (13 v0003, six v0004, 146 v0005), 779 parts and zero parse failures. Child types span LOD, particle, mesh, sprite and nested CMPA resources. Runtime `component_appearance_construct_VA_007A0320` owns child instances, attaches their visibility objects under one parent and composes each authored transform during child operations. See `../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_COMPONENT_APPEARANCE_CMPA.md`.

`SPRT` and `QUAD` share one v0000 loader and runtime: local center/offset (3 F32), radius/half-size (F32), RGBA (4 F32), then a shader path. There are 469 effective live `.spr` winners—369 QUAD and 100 SPRT—with zero parse errors and 385 distinct shaders. The runtime creates a shader-backed quad and explicit bounds; engine-native billboards are suitable only if shader indirection, authored geometry/color and bounds remain intact. See `../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_SPRITE_QUAD_APPEARANCE.md`.

The replacement should preserve this separation: generic bounded IFF framing, explicit tag-to-decoder registration, per-type version validation, and engine-neutral decoded templates. Original assets remain runtime source-of-truth; derived caches are disposable accelerators only.

## PEFT particle-template boundary

`SetupClientParticle::install` at `0x007A1100` installs the particle family in dependency order. `particle_effect_appearance_template_install_VA_007B6DB0` registers tag `PEFT` with the central appearance factory and factory `0x007B6D50`. The factory allocates a 0x24-byte legacy template; `0x007B6C20` initializes the base `AppearanceTemplate`, allocates the separate 0x38-byte effect description, and dispatches its IFF loader.

`particle_effect_description_load_VA_007C3990` accepts exactly `PEFT/0000`, `/0001`, and `/0002`. Version 0000 contains a count chunk followed by that many `EMGP` emitter-group descriptions. Version 0001 first loads `PTIM/0001` timing data, then the count and group sequence. Version 0002 adds initial time scale, transition time, sustaining time scale and overall effect spatial scale as four float32 values after the group count. They populate `+0x28/+0x2C/+0x30/+0x34`, defaulting to `1/0/1/1`. Runtime functions `0x007A9060` and `0x007A90D0` consume them and recursively compose nested effects.

Every group is held as a 0x1C-byte description value and loaded through `particle_emitter_group_description_load_VA_007C5460`. Runtime `particle_effect_appearance_ctor_materialize_groups_VA_007A85A0` retains the template, creates one runtime `ParticleEmitterGroup` per decoded group, and stores them in an owned vector. Group descriptions recursively own versioned `EMTR` emitter records, which then select quad/mesh particle descriptions and their wave/color functions. This establishes the engine-neutral boundary: `{effect timing, four v2 scalars, ordered emitter groups}` feeds runtime instances; legacy pointers, vector capacity, memory pools and local shader primitives do not belong in serialized IR.

A configuration-aware scan of `swgemu_live.cfg` found 2,123 effective `.prt` paths: 1,305 valid `PEFT/0002`, 808 valid `PEFT/0001`, one valid legacy `PEFT/0000`, and nine zero-byte overlay tombstones. There were no extraction errors or unsupported live top-level versions. The first representative fixtures are `appearance/unshaded_geyser_test.prt` (0000), `appearance/pt_bls_1_red_miss_terrain.prt` (0001), and `appearance/pt_trailing_space_explosion_md03.prt` (0002).

The next nested boundary is also version-complete. `particle_emitter_group_description_load_VA_007C5460` accepts `EMGP/0000` and `/0001`. Version 0000 is a count plus repeated `EMTR`; version 0001 prepends common `PTIM/0001` timing data. Each decoded emitter occupies 0x24C bytes in the legacy description vector, but must become a typed host object rather than a copied structure. `particle_emitter_description_load_VA_007B9B90` accepts every `EMTR` version from `0000` through `0014`, selecting fifteen explicit loaders and rejecting other tags with a printable-version diagnostic.

The effective corpus contains exactly 2,114 `EMGP` records—one per live effect—and 6,373 `EMTR` records. Group versions are one `0000` and 2,113 `0001`. Emitter versions are: one `0000`, eight `0006`, 78 `0009`, 28 `0010`, 696 `0011`, 1,039 `0012`, 841 `0013`, and 3,682 `0014`. No other version occurs. These corrected counts use SWG IFF's direct declared-end traversal; a prior scanner incorrectly assumed classic external padding after odd-sized chunks and undercounted sibling emitters. Runtime `particle_emitter_group_ctor_materialize_emitters_VA_007B6030` retains the group description and owning effect appearance, derives timing state, and allocates one 0x364-byte legacy `ParticleEmitter` per 0x24C-byte emitter description. These sizes are evidence for ownership/cardinality only, not portable ABI.

Field differencing closes the modern EMTR structural boundary. Versions 0009-0014 share PTIM, sixteen ordered WVFM channels, a fixed scalar prefix, optional sound path and a 39-byte common tail. Their extensions are respectively 0, 29, 33, 58, 59 and 60 bytes. Shape values 0-5 resolve by factory vtable/RTTI to circle, sphere, rectangle, cube, line and X; particle-description values 0/1 resolve to quad/mesh. Version 0012 adds surface-query, height-offset, normal-alignment and direction-randomization controls. Version 0013 adds a runtime-consumed generation/backlog policy flag; the final 0014 byte selects normal versus reserve particle-pool admission pressure.

The sixteen modern EMTR waveforms now have a runtime map. Channels 0-2 are emitter translation XYZ; 3-5 are the three ordered emitter rotation axes; 6-8 build shape extent and spawn direction/spread; 9 is particle generation rate; 10 is initial speed; 14 is lifetime; and 15 is gravity scale. Channels 11-13 are dormant zero-valued compatibility records across all 6,373 effective emitters and have no direct executable consumer; only their original editor labels remain unknown. Lifetime produces per-particle normalized age, which then drives nested quad/mesh visual waveforms.

Common `+0x114` enables random initialization of three per-particle interpolation seeds. `+0x118` is a three-way particle orientation/reference-frame mode, with serialized value 3 normalized to mode 2. Invariant `+0x11C` has no demonstrated runtime read and remains a compatibility Boolean.

## MESH to shader-primitive-set boundary

`mesh_appearance_factory_VA_007962D0` allocates a 0x48-byte legacy `MeshAppearanceTemplate`; constructor/loader `0x00796340` initializes base `AppearanceTemplate` state and dispatches `FORM MESH`. `mesh_appearance_load_VA_007968B0` supports versions `0002`, `0003`, `0004`, and `0005`. Unknown versions are reported through the generic IFF current-name diagnostic rather than parsed heuristically.

Versions `0004` and `0005` first load embedded `APPR` data, then copy the extent-derived center and radius into mesh bounds. Versions `0002` and `0003` read legacy explicit `CNTR` (three floats) and `RADI` (one float) chunks and then load older APPR fragments. If an extent is a `BoxExtent`, the client constructs a DPVS test model from it; otherwise it constructs a fallback sphere/box model from the derived bounds. The visibility/culling representation is therefore derived runtime state, not part of the portable mesh asset model.

All four versions construct a shader-primitive-set container through `shader_primitive_set_ctor_load_VA_00770C30`. Its loader requires `FORM SPS ` and accepts versions `0000` and `0001`. Later SPS/VTXA/topology blocks close shader grouping, primitive/index envelopes, vertex declarations and all twelve topology IDs; these are no longer open decoding boundaries.

Base APPR trailing data can contain optional `FORM HPTS`. It declares a count and repeated `HPNT` chunks, each containing a transform and a length-prefixed/name string. Loaded hardpoints are sorted after parsing. These names and transforms are required semantic attachment data for equipment, particles, lights and client effects and must not be discarded by a modern importer.

## Shader primitive-set and vertex-array structure

Both `SPS / 0000` and `0001` begin with `CNT `, then repeat anonymous shader groups. Each group first resolves a shader template: a `NAME` chunk fetches an external `.sht` through the shader-template list, while a non-NAME child is parsed as an inline shader template. Group `INFO` supplies its primitive count. Each primitive becomes a 0x18-byte legacy record retaining the group shader pointer plus topology, vertex buffer, optional index buffer, and optional sorted-index ranges.

SPS 0000 primitive records use an older flat layout: U32 topology ID, `VTXA`, and for topology IDs 6-11 an `INDX` chunk whose count and entries are stored as U32 but truncated into a 16-bit runtime index buffer. SPS 0001 moves each primitive under its own `0000` form. Its `INFO` is U32 topology ID plus two boolean bytes: has-index-buffer and has-sorted-index-data. `INDX` then stores U32 count followed by U16 indices. Optional `SIDX` begins with U32 range count and contains repeated 12-byte Vector3 plus a 16-bit-index-buffer payload. The loader disables SIDX when a graphics capability/limit is below 0x101.

`VTXA` supports versions 0001, 0002, and 0003. Version 0001 expands an older flags/count description; 0002/0003 use a packed U32 declaration bitfield followed by U32 vertex count and `DATA`. Confirmed semantics include optional position (three floats), primary packed color, normal (three floats), additional packed scalar/color channels, up to 15 texture-coordinate sets, and 1-4 floats per texture coordinate according to two-bit dimensions in the declaration. Version 0003 adds one optional packed channel absent from 0002. An optional trailing four-float value is read and discarded/converted when a declared DOT3 texture coordinate is unsupported by the active graphics path; exact tangent semantics remain strongly inferred rather than claimed.

All runtime index buffers remain 16-bit even where legacy files encode each index in four bytes. This is a file-format compatibility constraint, not a reason to use 16-bit internal limits globally in the replacement.

A corrected corpus inventory of all 41,016 installed `.msh` records found `MESH 0005 / SPS 0001 / VTXA 0003` (29,131 occurrences), `MESH 0004 / SPS 0001 / VTXA 0003` (11,814), six legacy `0003/0000/0002`, and 65 true zero-payload masks. The earlier count of 29,025 omitted 106 uncompressed SKU meshes whose compressed-size field is zero but whose uncompressed-size field is nonzero. See the appearance-fetch fallback checkpoint for the corrective audit.

## Primitive topology, sorted indices, and shader/effect envelope

The previously open topology table is now confirmed by initializer `0x0076F890`, the twelve consecutive graphics wrappers at `0x00759B60`-`0x00759CE0`, indexed/non-indexed loader behavior, and primitive-count arithmetic at `0x007709F0`. On-disk topology IDs are:

| ID | Semantic topology | Index source |
|---:|---|---|
| 0 | point list | vertex order |
| 1 | line list | vertex order |
| 2 | line strip | vertex order |
| 3 | triangle list | vertex order |
| 4 | triangle strip | vertex order |
| 5 | triangle fan | vertex order |
| 6 | point list | INDX/SIDX |
| 7 | line list | INDX/SIDX |
| 8 | line strip | INDX/SIDX |
| 9 | triangle list | INDX/SIDX |
| 10 | triangle strip | INDX/SIDX |
| 11 | triangle fan | INDX/SIDX |

`static_shader_primitive_prepare_VA_007707D0` confirms the purpose of `SIDX`. Each 16-byte entry is a local-space direction vector plus an index-buffer reference. At render preparation, the client converts the active camera displacement into primitive-local coordinates, computes a dot product against every stored direction, selects the entry with the minimum dot product, and binds that entry's index buffer. If no SIDX vector exists it binds the ordinary INDX buffer. This is precomputed view-dependent index ordering, principally relevant to correctly ordering translucent geometry. A modern importer must retain all variants and choose one using the decoded local direction; it must not collapse them into the first index buffer.

The external material path is now anchored as `NAME -> shader_template_fetch -> tag registry -> concrete shader template`. Missing SHT files warn and fall back to `shader/defaultshader.sht`. The matching effect cache opens EFT resources and falls back to `effect/defaulteffect.eft`. An effect may itself be inline `EFCT` or a `NAME`; a non-qualified effect name is normalized beneath `effect/`.

`EFCT` accepts versions `0000` and `0001`. Its DATA chunk starts with a U8 implementation count; 0001 adds a U8 boolean `containsPrecalculatedVertexLighting`. Implementations are tried in file order until one passes the active graphics-capability predicate. Remaining alternatives are still parsed when graphics validation is enabled, but are discarded. An implementation may be inline `IMPL` or external `NAME`; IMPL accepts versions `0000` through `0007`. Newer versions add capability (`SCAP`) and option (`OPTN`) filtering plus versioned DATA fields. Each selected implementation owns counted passes. `PASS` accepts versions `0000` through `0009`; detailed render-state and program-binding fields remain the next decoding boundary.

The EFCT boolean is confirmed by code and symbol evidence. `shader_effect_load_v0001_VA_00783630` stores DATA byte 1 at effect offset `+0x18`; `StaticShaderTemplate::containsPrecalculatedVertexLighting` at `0x0077FFF0` returns that byte; and the executable-exported `StaticShader::containsPrecalculatedVertexLighting()` at `0x0076A4C0` dispatches `ShaderTemplate` virtual slot 7. The v0000 loader leaves the constructor default false. The configured live overlay contains 356 winning EFCT/0001 records: 316 false and 40 true. Every true winner currently has an `effect/c_*.eft` name, a useful corpus consistency check but not the basis for the semantic identification.

Correction to the original 171-anomaly interpretation: 106 SKU records (88 in `data_sku1_00.tre`, 11 in `data_sku1_06.tre`, seven in `data_sku1_07.tre`) are **live** uncompressed MESH/0005, SPS/0001, VTXA/0003 assets. Their `kind=0`, `csize=0`, `usize>0` records have valid IFF payloads. Only 65 records in `mtg_patch_004_appearance_04.tre` have `usize=0` and are true SearchTree masks. The corrected `tools/mesh_schema_inventory.py` returns 41,016 records, zero extraction errors and precisely these four categories; `tools/mesh_anomaly_audit.py` returns just 65 tombstones. The earlier claim that all 171 were tombstones was false.

## PASS wire layout and installed shader corpus

The main shader implementation dispatcher `shader_pass_version_dispatch_VA_0075E840` accepts PASS versions `0000` through `0009`. Version 0009 loader `shader_pass_load_v0009_VA_007619D0` gives the current executable-supported superset. Its `DATA` payload is tightly serialized field-by-field rather than copied as a C++ struct:

```text
U8 childStageCount
U8 state_10, state_14, state_18, state_19, state_1A
U8 state_1C, state_20, state_24, state_28, state_2C, state_30
U32 state_34
U8 state_38, state_3C
U32 state_40, state_44, state_48
U8 state_4C, state_4D
U32 state_50
U8 state_54, state_58, state_5C, state_60, state_64, state_68, state_6C, state_70
U32 state_74, state_78, state_7C
```

The DATA payload therefore totals 24 U8 values including the child count and eight U32 values. The offset suffixes are stable runtime-member offsets, not asserted semantic names. Consumer correlation confirms `state_3C` is a mask-like field initialized to `0x0F`, but the exact D3D render-state name remains open. After DATA, the pass may contain `PFFP` (fixed-function vertex-lighting data) or `PVSH/0000` naming a vertex shader. It then contains either `PPSH` pixel-shader data or `childStageCount` repeated texture-stage records. `PFFP` accepts versions 0000 and 0001. The effective overlay contains 704 v0001 records and one v0000 record. Both carry three enable bytes followed by four material-source selectors; v0000 additionally carries a U32 material tag. Its sole value is `MAIN`, and v0001 removes the field after material selection moved to the PASS-level tag. The seven-byte v0001 shape matches D3D9 fixed-function lighting/specular/vertex-color enablement plus diffuse/ambient/emissive/specular material sources. Exact source-channel ordering remains Strong and readers must preserve serialized order. The texture-stage object initializes color/alpha operations and argument selectors before its own version loader.

The version family is additive: older PASS loaders populate a subset of the same 0x80-byte runtime object while constructor defaults fill absent fields. This directly argues against deserializing into packed host structs in the replacement. Use explicit readers per version that populate a stable engine-neutral `LegacyPassDescription`.

A corrected read-only scan of all raw installed TREE records found 37,842 live `.sht`, 87 true SHT tombstones, and 467 live `.eft`, with no extraction/IFF framing errors. Live SHT outer versions are 35,293 `SSHT/0000` and 2,395 `SSHT/0001`; some `.sht`-named records have other outer forms. EFT outer versions are four `EFCT/0000` and 463 `EFCT/0001`. The earlier 36,686/1,243 SHT live/mask split used compressed size for uncompressed records and was false. The corrected configured overlay has 23,393 winning shader/effect names: 23,306 live and 87 masks (20 more live than previously reported). See the appearance-fetch fallback checkpoint.

Raw embedded form counts are dominated by PASS 0009 (1,703 across EFT/SHT) and PASS 0008 (658), followed by PASS 0007 (86). However, 108 raw PASS 0010 forms and 82 IMPL 0008/0009 forms also occur, principally in later patch assets. The traced main executable dispatchers stop at PASS 0009 and IMPL 0007. These are real well-framed asset forms, but no corresponding Stardust.exe shader dispatcher was found; similar `0010` immediates elsewhere belong to unrelated particle-emitter versions. Twenty-seven live overlay winners contain IMPL 0009/PASS 0010; their actual runtime reachability remains unproven. A replacement consuming the full original asset set should plan to decode them rather than silently reject them.

PTAT terrain templates also own environment-cycle timing. Constructor `0x00B6E2F0` defaults member `+0x40` to `1800.0` seconds, while PTAT/0013 and /0014 load it from DATA. The effective overlay-selected `terrain/tatooine.trn` (`mtg_patch_013_configurable_02.tre`, priority 42) contains `11280.0` seconds, or 188 minutes. Its DATA stream becomes unaligned after a NUL-terminated path, so readers must advance field-by-field exactly as the IFF schema dictates rather than align or overlay a host struct.

The 256×8 environment lighting-ramp image has code-confirmed row semantics. Row 0 supplies ambient RGB and repeated-CELS opacity; rows 1–2 supply main-light primary and secondary RGB/intensity; rows 3–4 supply fill and bounce RGB/intensity; row 5 supplies fallback solid-sky RGB and SUN/SSUN/MOON/SMOO visibility; row 6 supplies fog RGB and stars/sky/cloud opacity; row 7 supplies shadow-volume RGBA. When a sky source is absent, the client synthesizes six one-pixel cube faces from row 5 RGB and registers the texture as `_SKY`. Exact row order is therefore mandatory compatibility data, not an opaque artistic lookup.

Outdoor environment data has two complementary assets sharing the terrain basename. `datatables/environment/<name>.iff` is a 25-column DTII table keyed by PTAT family name plus weather index; it names gradient sky, two cloud shaders/scales/speeds, a 256×8 lighting ramp, shadow/fog controls, camera-attached appearance, day/night environment maps, seven audio paths and wind scale. `terrain/environment/<name>.iff` is ENVM/0000 and defines shared night sky, stars, sun/moon variants and repeated celestial bodies. The configured corpus contains 50 effective environment DTII tables, all structurally valid, totaling 1,337 rows and 290 family groups. See `effective_environment_tables.tsv`.

The ENVM overlay contains 63 effective members and 116 base CELS records. It also contains seven flat `TLOK` chunks (`U8 + F32`), one flat `SKYB` chunk (`U8 + cstring`), and four nested Mustafar `DIST/0000` records (`cstring appearance + X/Z/Y + three angles`). Stardust.exe has no literal recognizer for those extension tags; current server `SpaceTerrainAppearance` code independently confirms SKYB and DIST body schemas. The replacement asset IR should parse and preserve these extensions even while its initial compatibility renderer matches the executable's skip behavior.

PTAT `MAPS/0001` is now exactly decoded. Its DATA is `F32 worldSize`, `F32 metersPerBit`, `U32 bytesPerRow`, `U32 bitDimension`, followed by `WMAP` and `SMAP`, each `bytesPerRow * bitDimension` bytes. Bits are LSB-first and coordinates are centered on the world origin. Effective Tatooine uses 16384 meters, 8 meters/bit, 256 bytes/row and 2048 rows, making each mask 524,288 bytes. Stardust server source names these the water and seed maps. Stardust.exe has three established WMAP consumers, including structure-placement camera behavior, but no direct SMAP consumer. Both remain required lossless compatibility inputs.

## Ship-hit presentation tables (2026-09-15)

The executable's `ShipHitEffectsManagerClient` requires two original data-table
assets: `datatables/space/ship_hit_effects.iff` and
`datatables/space/ship_hit_sounds.iff`. ObjectController subtype `0x432`
supplies hit type 0..3 plus old/new condition percentages; the manager converts
the percentage transition into a severity band and indexes the corresponding
CEF and SND entries. A replacement renderer/audio layer must consume these
tables rather than hard-code a newly invented effect matrix. Missing rows or
resources are recoverable diagnostic no-ops in the original client.

Whole-player-ship destruction additionally references
`clienteffect/combat_player_ship_hit_death.cef` and
`sound/music_space_player_death.snd`. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_DAMAGE_DESTRUCTION.md`.

## Space-zone nebula lightning (2026-09-15)

`CreateNebulaLightningMessage` carries only nebula row ID, synchronized timing
and endpoints. The client must load `datatables/space/nebula/<zone>.iff` to
resolve the lightning appearance, one-shot/loop sounds and presentation
metadata. Stardust reads the same original table for radius/density, timing,
damage and hit effects. See
`ANALYSIS_CHECKPOINT_2026-09-15_NEBULA_LIGHTNING.md`.

## Skeletal mesh dynamic face-texture bridge (2026-09-22)

The optional SKMG `FORM TRTS` suffix is a blueprint texture-renderer binding
table, not another geometry stream. Each `TRT ` record stores a template path,
a binding count and repeated `{U32 primitiveIndex, U32 textureTag}` pairs. All
sixteen effective users are head meshes, reference face `.trt` assets, and bind
one varying primitive index to tag `MAIN`. The target BTRT asset is an authored
dynamic render graph. A compatible importer must preserve and eventually
execute this indirection. See
`ANALYSIS_CHECKPOINT_2026-09-22_MGN_TEXTURE_RENDERER_BINDINGS.md`.

The configured overlay contains exactly eight `.trt` assets, all BTRT/0001 and
all referenced by those head meshes. The executable loader confirms ordered
destination, shader, texture, vertex/index buffer, camera, render-command and
prepare-command sections. This is a compact dynamic render graph rather than
an opaque static texture. See
`ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_ENVELOPE.md`.

The live command subset is now finite: 37 shader render sets each contain one
triangle-fan command, while 67 unconditional prepare lists contain only 20
constant-texture, 17 one-dimensional-texture and 30 palette-factor operations.
The executable also supports clear, indexed-list, conditional/false-branch,
2D-texture and other factor operations, but none occurs in the eight effective
face templates. See
`ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_COMMANDS.md`.

Runtime execution confirms the live dataflow: constant textures bind directly;
1D texture ranges are selected by a clamped customization integer; palettes are
selected by a clamped customization integer and bound as texture factors;
shader render sets iterate passes; and each live RTFC selects one vertex buffer
and issues a triangle fan. The effective face-composition path is therefore
implementation-grade even though dormant command variants remain documented
for later expansion.

## Lightning appearances (LEFX)

The effective overlay contains 49 live `.ltn` resources: two LEFX/0000, six
LEFX/0001 and 41 LEFX/0002, with no tombstones or parse failures. Every version
embeds the shared PTEX/0000 shader/atlas descriptor and a final scalar/string
chunk. Versions 0001 and 0002 add two matching WVFM channels; deployed pairs
are 36×WVFM/0001 and 11×WVFM/0002. The three strings identify
start appearance, end/target appearance and optional sound. Runtime code owns
five fixed procedural branch records and generates dynamic shader geometry.

Import LEFX into an engine-neutral lightning description that retains version,
ordered scalar fields, PTEX, both optional waveform channels, attachment
resources and sound. Rendering may use modern GPU ribbons, but asset decoding
must preserve the original packed-IFF rule: odd-sized chunks have no RIFF pad
byte. See `ANALYSIS_CHECKPOINT_2026-09-22_LEFX_LIGHTNING_APPEARANCE.md`.

## General detail appearances (DTLA)

General `.lod` resources are `DTLA` detail-appearance templates, not skeletal
MLOD generators. The configured overlay has 6,198 valid effective DTLA assets
in versions 0005–0007, three tombstones, and one `.lod`-named non-DTLA outlier.
The loader associates `INFO` distance intervals and `DATA/CHLD` paths by
authored U32 ID; all live child IDs match an INFO record. Runtime caps exposed
children at eight, creates children lazily and searches adjacent available
details when the selected child is unavailable. The replacement resource
manager must retain the authored distance bands and child paths. The `RADR`,
`TEST` and `WRIT` forms carry flags and sometimes an `IDTL/0000` indexed model
with 12-byte vertices and U32 indices; 1,836 such geometry records validate.
Executable imports and call sites prove TEST and WRIT build DPVS visibility
models (`setTestModel` and `setWriteModel`); RADR and flag effects remain open.
The current
overlay has 290 distinct `.msh` child paths absent by direct virtual-path
lookup. All 385 `appearance/no_render.apt` references resolve through a valid
high-priority APT-to-NRND redirect. A directly missing DTLA mesh child sent
through appearance fetch receives the default-appearance APT fallback; a
missing NAME target inside an existing APT follows a different recursive
path and remains a separate risk. See
`ANALYSIS_CHECKPOINT_2026-09-22_DTLA_DETAIL_APPEARANCE.md` and
`ANALYSIS_CHECKPOINT_2026-09-22_APPEARANCE_FETCH_FALLBACK.md`.

## Corrected short-asset overlay boundary (2026-09-22)

TRE kind-0 payloads use the uncompressed-size field even when compressed size
is zero. Corrected configured-overlay scans show 1,145 valid `CLEF` effects
plus one true mask, all in versions 0001–0003 supported by Stardust.exe. The
five leaf families are `PSND` sound, `CPAP` appearance/particle, `CAMS`
camera-effect numeric data, `CLGT` light numeric data, and `FFBK`
`forcefeedback/*.ffe` references. There are 1,889 recognized direct child
references, 13 absent from configured TREs after preserving one authored
trailing space. A modern renderer/audio/input
backend may redesign implementation, but it must preserve versioned leaf
behavior when consuming original CEF assets. See
`ANALYSIS_CHECKPOINT_2026-09-22_FULL_CLEF_OVERLAY.md` and
`ANALYSIS_CHECKPOINT_2026-09-22_CLEF_LOADER_VERSIONS.md`.

Runtime execution now confirms that CLEF's `CPAP`, `PSND`, `CLGT`, `CAMS`, and
`FFBK` vectors all feed effect instances, not just parsing. `CAMS` drives a
distance-attenuated camera shake; `CLGT` supplies normalized RGB, lifetime,
three still-unidentified light scalars and a visibility/range radius; `FFBK`
attempts DirectInput `.ffe` enumeration and starts any created effect with an
authored iteration count inside a distance gate. Whether archive-backed
`archive[member]` notation is accepted by DirectInput remains unproven.
A missing `PSND` child remains a path entry but
is skipped by the traced playback functions when re-fetch returns null. See
`../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CLEF_RUNTIME_CONSUMERS.md`
and `../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CLEF_LIGHT_RUNTIME.md`.

The CLEF direct-reference audit's 13 absent children comprise **ten** sounds
and **three** particles; the third particle path ends in an authored trailing
space that TreeFile does not trim.
Following all CEF `CPAP` paths through recursive particle attachments reaches
576 live `.prt` assets, 253 live attachment edges, 278 nonempty shader names,
270 distinct named DDS textures (all live), and 11 distinct named effect
programs. One EFT path is absent and referenced by seven shaders; the original
effect-template fallback is already documented above. This is a bounded asset
closure, not proof that every effect PASS has been reconstructed. See
`../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CEF_PARTICLE_SHADER_CLOSURE.md`.
All 87 reached empty-name PTEX slots are enabled and use the installed
`shader/3d_vertexcolorac.sht` through the primitive's fallback shader accessor;
see `../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_PTEX_EMPTY_SHADER_FALLBACK.md`.
`CPAP` also carries particle lifetime, an expiration policy, and (in CLEF/0003)
separate randomized geometry-scale and time-scale ranges; see
`../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CLEF_CPAP_RUNTIME_AND_PATH_CORRECTION.md`.

The corrected CDF corpus has 6,284 valid `CLDF/0000` files and four true
masks. All 15 recovered MKAT marker appearances parse as `MKAT/0000/INFO`
and reference live shaders. Fifty-three recovered TGA files are 1-row terrain
color ramps (24- or 32-bit). Twenty-five named cockpit-frame APT references
resolve through immediate targets and, where applicable, live DTLA children.
Path closure is not proof of complete runtime rendering. See the respective
2026-09-22 CDF, marker, terrain-ramp, and cockpit-frame checkpoints.
