# Palette-factor bundles and explicit state — 2026-09-30

## Question and stopping condition

Can a bounded owned CSHD/palette consumer reproduce every authored and extreme supplied factor lookup while retaining missing, empty and malformed distinctions? Close on independent full-corpus agreement and synthetic ownership/resource/pin/namespace gates. Rendering and live customization are separate.

## Inputs and evidence

Baseline executable SHA-256 remains `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; 48 TREE0005 mounts. No original execution or external contact. CSHD member manifest `9B2FEE105ABF0CABC41E7A79DADE3526BF612D888A41712A8B49C331F274576A`; palette manifest `08A9C87D0D0ED14561E45E98F76F30FF4D2D26EEC2D9824B8E10372BD9FB08A3`.

Original namespace matching, loader field widths, factor application and missing/index behavior are established in the [CSHD reader](IMPLEMENTATION_CHECKPOINT_2026-09-30_CUSTOMIZABLE_SHADER_READER.md) and [palette reader](IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_READER.md). Original factor application `0077BFB0` calls getter `00AB7B10`, then shader factor setter `0076AB70`. Missing cache `00AB69FC` stores opaque cyan. These are behavior constraints, not copied implementation.

## Findings and consequences

**Confirmed:** `CustomizableShaderPaletteQuery` snapshots supplied exact-case name/private keys; missing keys explicitly use serialized authored defaults. Repeated palette references resolve once per query. Invalid indices select zero; missing/masked resources produce flagged cyan; structurally valid empty palettes produce no color. Raw flags and requested/selected indices remain inspectable. Authored defaults are not inferred to be current live object state.

**Confirmed by implementation gates:** `CustomizableShaderPaletteBundleReader` owns one pinned CSHD root and distinct palette dependencies, applies shared source/resource budgets, and retains diagnostics/provenance. Malformed present palettes fail rather than receiving missing fallback. Root mismatch, unavailable root and aggregate budgets fail. Textures/effects/geometry are not needed by this consumer. Default limits are 256 attempted resources and 128 MiB source bytes; no peak-memory guarantee is implied.

**Confirmed corpus agreement:** all 5,173 shaders and 7,908 factors agree for authored defaults, Int32 minimum and maximum overrides: **23,724 cases**. Independent Python derives ordered field/color hashes from raw asset bytes; native reports matching hashes and exact distinct-source/palette/missing counts. One factor uses missing `palette/swamp_trooper.pal`; sixteen defaults exceed present ranges and select entry zero. No default is silently rewritten.

## Validation and reproduction

Full native smoke suite passes. Synthetic cases cover asymmetric RGB/raw flags, exact case/private distinction, owned result after caller mutation, repeated missing lookup, missing/masked fallback, malformed present rejection, empty palette, root pins and source limits.

Run `verify_customizable_shaders.py ROOT CONFIG PRIVATE_OUTPUT --core-dll DLL --palette-inventory PRIVATE_PALETTE_JSON` after the palette producer. Native `--cshd-palette-list` requires the pinned member list. Output stays private; the canonical conclusion is the [palette contract](../../docs/PALETTE_DECODER_CONTRACT.md). Workspace link/schema checks and focused diff checks are part of the session gate. No registry, protocol or research-score change.

## Remaining uncertainty and next probe

Factor queries do not translate original lighting, material factor semantics or live owner/customization state. Next: a bounded Godot swatch inspector showing target tags, exact variable namespaces, requested/selected indices and fallback provenance, verified on a real shader and synthetic color fixture.
