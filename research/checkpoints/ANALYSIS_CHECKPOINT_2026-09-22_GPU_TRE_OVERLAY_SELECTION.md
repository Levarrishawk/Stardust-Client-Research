# Launcher GPU TRE overlay selection

Date: 2026-09-22

## Result

The installed launcher mutates exactly one resource-overlay slot immediately
before file verification completes and again immediately before starting
`Stardust.exe`.

Managed IL for `GpuConfigurationService.Apply(clientRoot, gpuType)` establishes:

- configuration template: `swgemu_live.cfg.template` (preferred when present);
- output: `swgemu_live.cfg`;
- the only replacement target is exactly one `searchTree_00_54` line;
- NVIDIA/default selects `stardust_3_02_n.tre`;
- AMD selects `stardust_3_02_a.tre`;
- zero or multiple slot-54 lines are rejected;
- original text encoding and UTF-8 BOM presence are retained;
- output is written to `.gpu-update` and atomically replaced/moved, with cleanup.

The current configuration and template both select the NVIDIA archive. The
existing effective-overlay inventories therefore describe the current installed
selection correctly.

## Archive relationship

Read-only TRE inventory produces:

| Archive | Records |
|---|---:|
| `stardust_3_02_n.tre` | 208 |
| `stardust_3_02_a.tre` | 26 |
| Common names | 26 |
| Byte-identical common packed records | 26 |
| NVIDIA-only names | 182 |
| AMD-only names | 0 |

The AMD archive is therefore a strict byte-identical subset of the NVIDIA
archive, not a set of alternate AMD shader payloads. Its 26 records comprise
terrain dot3 shader/program assets plus the Mustafar map texture/UI include.

Of the 182 NVIDIA-only records, only three have lower-priority configured
fallbacks under the AMD selection:

- `effect/water_lava_textured.eft` -> priority 50 `mtg_planets.tre`;
- `snapshot/mustafar.ws` -> priority 53 `stardust_3_01.tre`;
- `terrain/mustafar.trn` -> priority 53 `stardust_3_01.tre`.

The other 179 become absent from the effective AMD namespace. They are a
coherent Stardust Mustafar ribbon/affector package: object templates,
APT/LOD/MSH/FLR assets, ribbon effects/shaders/programs and related snapshot
material. The selector therefore acts mainly as a capability/compatibility
content gate, not a renderer-vendor implementation swap.

## Replacement consequence

The asset manager must consume the post-launch effective configuration rather
than assume one fixed archive list. GPU/capability profiles may remove names and
allow lower-priority fallback; they are not merely equivalent archive variants.
For deterministic tests, record the selected slot-54 archive in the fixture
manifest. The new renderer may eventually replace this historical vendor gate
with feature/capability decisions, but it must preserve the selected namespace
when behavioral parity is requested.

## Confidence

- Launcher mutation and atomic-write behavior: **Confirmed by managed IL**.
- Record counts, subset relation and fallback counts: **Confirmed by archive scan**.
- Historic reason for excluding the 179 assets on AMD: **Unknown**.

