# Analysis checkpoint: comprehensive 50 percent

Date: 2026-09-12

## Outcome

The strict comprehensive whole-client estimate has reached **50%**. This means half of the weighted material behavior needed for an implementation-grade architectural reconstruction has durable evidence and resume points. It does not mean half of all machine functions have been named. Replacement-relevant specification coverage is **67%**.

## Completed in this block

### PASS render states

Configured D3D9 backend compiler `0x62A0B340` maps PASS fields directly to D3DRS identifiers. Confirmed roles cover shade mode, dithering, depth enable/write/compare, blend operation/source/destination, alpha test/compare, color-write channels, complete one- and two-sided stencil operations, and stencil masks.

### Texture samplers

`0x62A0E570` resolves each StaticShader texture and emits maximum anisotropy, address U/V/W, and mip/min/mag filters. `0x62A0E0E0` binds the cached texture; `0x62A0E910` applies the sampler pairs. TextureData defaults and stage overrides are explicitly separated.

### Effective asset overlay

The read-only overlay audit implements confirmed precedence: descending configured priority, first winner, with tombstones preventing fallback. Across 48 archives it found 23,393 winning `.sht/.eft` paths, comprising 23,286 live records and 107 tombstones, with zero scan errors.

Twenty-seven live winners contain IMPL 0009/PASS 0010: 24 SHT assets in priority-47 `mtg_patch_018.tre` and three EFT assets in priority-40 `mtg_patch_011_files_01.tre`. Raw IMPL 0008 assets are shadowed. The newer forms are visible to `TreeFile::open`; whether every asset is requested in normal gameplay remains Unknown.

## Confidence and consequence

- D3DRS/D3DSAMP roles: **Confirmed by code**.
- Overlay counts and winners: **Confirmed by configuration and archive data**.
- Session-level use of every newer asset: **Unknown**.
- Need for replacement decoder support: **Confirmed** under full original-asset compatibility.

The replacement should decode versioned forms into GPU-neutral logical passes, then resolve StaticShader parameters and compile immutable modern pipeline/binding variants. Legacy object sizes and D3D numeric states are semantic evidence, not layouts to reproduce.

## Next recommended block

Diff PASS 0010 against PASS 0009 and IMPL 0009 against the latest supported IMPL using the 27 winning assets. Then trace the fixed-function color/alpha combiner builder consuming the remaining texture-stage fields.
