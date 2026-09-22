# Analysis checkpoint — DTLA detail appearances

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Method: static x86 inspection plus effective `swgemu_live.cfg` TRE overlay; no client execution or asset mutation.

## Result

`.lod`/`DTLA` is the **general appearance** detail selector, separate from skeletal `MLOD` (`.lmg`) and terrain LOD. It owns a distance-band table and named child appearance templates. The runtime switches or falls back among child appearances and shares a visibility parent; it is not merely a filename container. The replacement should decode it into an engine-neutral detail-appearance resource, with rendering delegated to child appearance types.

## Executable chain

| Stage | VA | Evidence and result | Confidence |
|---|---:|---|---|
| install | `0x007992E0` | registers `DTLA` with factory `0x00799390`; reads `ClientObject/DetailAppearanceTemplate.skipL0` | Confirmed |
| factory/constructor | `0x00799390` / `0x007993F0` | allocates template and invokes version dispatcher | Confirmed |
| dispatch | `0x0079A370` | recognizes 0001–0007, rejects other versions with unsupported-format diagnostic; sorts loaded 20-byte level records by upper distance bound | Confirmed |
| common level load | `0x007999F0` | reads repeated `INFO` triples, then `DATA/CHLD` records; associates each child by U32 ID and stores its path | Confirmed |
| optional policy forms | `0x0079A100`, `0x0079A1C0`, `0x0079A280` | load `RADR`, `TEST`, `WRIT` `INFO` flags and conditional indexed geometry | Confirmed structure; precise roles open |
| create child template | `0x00799810` | lazily fetches child appearance through common template fetcher `0x00B260F0` | Confirmed |
| instantiate | `0x00799880` | fetches selected child template, calls its runtime appearance factory | Confirmed |
| runtime constructor | `0x00787400` | RTTI `DetailAppearance`; caps exposed children at eight, creates first child and conditionally preloads others | Confirmed |
| runtime selection | `0x00787710` | camera-to-appearance distance, interval/hysteresis checks, forced-detail branches | Confirmed broad behavior |
| availability fallback | `0x00787908`–`0x007879BD` | searches adjacent loaded detail children if chosen one reports unavailable; returns -1 if none | Confirmed |

The loader's `skipL0` branch at `0x00799464`–`0x007994C3` removes the final sorted detail when enabled and more than one level exists. The installed corpus commonly orders far/coarse levels first and near/L0 last. Treat this as a configurable diagnostic/performance policy, not a change to file schema.

## Deployed file contract

```text
FORM DTLA
  FORM 0005|0006|0007          # 0001–0004 supported by code, not deployed
    FORM APPR / FORM 0003      # inherited appearance bounds/base
    [PIVT one byte]            # present in 0006 and 0007
    INFO {U32 id, F32 lowerDistance, F32 upperDistance} × N
    FORM DATA
      CHLD {U32 id, NUL-terminated appearance path} × M
    [FORM RADR / INFO U32 [FORM IDTL / FORM 0000 / VERT / INDX]] # 0007 only
    FORM TEST / INFO U32 [FORM IDTL / FORM 0000 / VERT / INDX]
    FORM WRIT / INFO U32 [FORM IDTL / FORM 0000 / VERT / INDX]
```

IFF container sizes are big-endian; these numeric payload fields are little-endian. `INFO` and `CHLD` IDs are **identifiers**, not necessarily zero-based positions. `0x00799950` searches level records for a matching ID; the CHLD path is stored at record `+0x04`. Runtime accessors `0x007997D0` and `0x007997F0` read the two F32 bounds from record `+0x0C` and `+0x10`. Selector code compares camera distance after a global scale against those bounds and uses the prior selected index as a search hint. It does not treat the file values as squared distances.

`0x0079A340` compares upper bounds; `0x0079A370` sorts the loaded records. The common corpus pattern is far/coarse first, near/detailed last: for example `180:360`, `90:180`, `45:90`, `0:45` with child names L3, L2, L1, L0. Preserve authored IDs and intervals; do not infer L0 solely from ID 0.

The `PIVT` byte is 0 in 5,985 effective files and 1 in 63. Runtime `0x007877A5` checks the template Boolean (`0x00799940`): one branch measures from the appearance transform's translation, the other from a transformed local offset. Therefore PIVT changes the distance reference point, not the band schema. The historical meaning of the flag's label is consistent with this branch, but the exact authoring convention for the local offset remains to be checked against APPR extent consumers.

## Effective overlay audit

`tools/python/dtla_corpus_inventory.py` scans the 48 configured TRE archives with overlay priority. Its output CSV belongs under ignored `local-private/generated-evidence/`.

| Measure | Result |
|---|---:|
| live valid DTLA | 6,198 |
| versions 0005 / 0006 / 0007 | 150 / 66 / 5,982 |
| zero-payload tombstones | 3 |
| `.lod` path with non-DTLA bytes | 1 (`appearance/lod/mun_yoda_fountain.lod`) |
| child appearances per valid file | 1–6 |
| child references | 20,281 |
| `.msh` / `.apt` / `.cmp` / `.mgn` / `.spr` references | 19,784 / 385 / 101 / 4 / 7 |
| CHLD IDs absent from local INFO | 0 |
| duplicate CHLD-ID files | 8 |
| nonfinite intervals | 0 |
| reversed-interval files | 1 |

All 6,198 valid files carry base `APPR/0003`, `DATA`, `TEST` and `WRIT`. Version 0007 also carries `RADR`; versions 0006/0007 carry a one-byte `PIVT`. Optional nested `IDTL/0000` records occur 887 times in `RADR`, 335 in `TEST`, and 614 in `WRIT`. Their `VERT` chunks contain packed 12-byte vectors and `INDX` chunks contain U32 indices. All 1,836 deployed indexed-geometry records have in-range indices (4–279 vertices, 6–1,047 indices). The shared loader `0x00ABC980`/`0x00ABCAE0` confirms the widths. `0x00766700` turns that geometry into an imported `DPVS::MeshModel`. Template code builds `TEST` and `WRIT` models through `0x0079AA50`/`0x007994C7`. Runtime `0x00787523` calls imported `DPVS::Object::setTestModel`; `0x007880EF` calls `setWriteModel` with the template's WRIT model. Thus TEST/WRIT are confirmed visibility-model roles, not render meshes. The precise role of `RADR` and policy flags remains open; `0x00788460` exposes its auxiliary pointer.

The eight duplicate-ID cases are Wroshyr tree files with four `CHLD` paths but three `INFO` records; their final child path repeats ID 0. This is a real authored irregularity, not a parser error. One valid file has a reversed distance pair; retain raw intervals and report a diagnostic rather than globally rejecting such assets. The non-DTLA `.lod` path begins with bytes `cd ab 00 00`, not `FORM`, and should remain a typed-rejection case.

Of 20,281 child references, **290 distinct `.msh` paths** in 69 DTLA parents do not resolve to any name in the configured archive overlay. In 60 of those parents, **every** child path is absent, so adjacent-level fallback alone cannot recover an available child if one of those parents is instantiated. All 385 `appearance/no_render.apt` references **do** resolve: priority-52 `stardust_3_00.tre` stores a valid 57-byte APT redirect to a valid 12-byte `NRND` IFF, also in that archive. The initial inventory helper incorrectly used the compressed-size field for uncompressed TRE records, making those two valid records appear empty. The corrected helper uses `usize` when compression kind is zero. This correction is recorded because it affects asset-compatibility conclusions. The 290 absent meshes remain an **asset lookup audit**, not proof that the executable successfully renders them. The child factory is lazy and the runtime checks availability when selecting. Shared appearance fetcher `0x00B263C0` contains a `Could not open appearance file %s` diagnostic and can return zero when no typed factory matches. `0x00799880` does not itself null-check the fetched template before virtual instantiation. Thus a missing child actively selected may be a hard failure unless another earlier loader/fallback branch substitutes a resource. Exact behavior for those 290 paths remains untested; do not invent replacement geometry.

A targeted read-only cross-family audit found **939 effective `.apt` and 19 `.iff` records** with kind 0, compressed-size field zero, and nonzero actual uncompressed data. No effective `.cmp`, `.ltn`, `.spr`, `.lod` or `.msh` record has that combination. This bounds the immediate blast radius of the inventory-helper bug: this DTLA scan and prior CMPA/LEFX/sprite counts are not reduced by it, but older APT/IFF scans using compressed size unconditionally should be reviewed before treating zero-size classifications as facts.

## Replacement contract and remaining work

- Parse versioned DTLA, inherited APPR, optional PIVT, ID-keyed distance bands and child paths. Preserve PIVT's choice of distance origin. Preserve `RADR/TEST/WRIT` flags plus versioned IDTL indexed geometry. Map TEST/WRIT to a modern visibility/culling abstraction, not visible triangle drawing; do not assign an unproven RADR or flag semantic.
- Keep child templates immutable and lazily resolved; create separate runtime child appearances, with at most eight exposed in this original client. Use authored distance intervals plus a configurable distance scale, hysteresis/previous-detail hint and neighboring-available-detail fallback.
- Preserve the possibility of missing children, duplicate IDs, reversed intervals and the non-DTLA extension outlier as explicit diagnostics/test fixtures. Define safe modern failure behavior rather than copying a possible null-dereference.
- Keep DTLA, skeletal MLOD/SAT and procedural-terrain LOD as separate policies behind a compatible resource/visibility interface.
- The shared TRE helper is corrected. Follow up by revalidating older APT/IFF zero-payload conclusions, then on `RADR` and flag effects, visibility-model fidelity, and the missing-child fetch/null path. These are fidelity/failure-policy unknowns, not blockers to the core DTLA schema.

The corpus, version dispatch, ID mapping, range fields, IDTL geometry widths, DPVS TEST/WRIT model roles, eight-child cap, lazy creation and neighboring fallback are **confirmed by code and assets**. Exact RADR/flag effects and behavior of unresolved child paths are **unknown**. The broad replacement architecture is **strongly inferred** from the observed template/runtime split.
