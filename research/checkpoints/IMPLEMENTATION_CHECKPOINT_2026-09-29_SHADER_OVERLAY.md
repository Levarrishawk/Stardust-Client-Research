# Configured TRE mounts and external shader references

Date: 2026-09-29. Question: can an explicit configured overlay resolve the
[verified original mesh](IMPLEMENTATION_CHECKPOINT_2026-09-29_STATIC_MESH_SLICE.md)
through its shader, texture and effect references without silently rewriting
archive keys? Stop at dependency availability and bounded reference decoding.

## Input identity and evidence

`swgemu_live.cfg` SHA-256:
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
All 48 configured TREE/0005 archives were opened read-only. Mesh identity and
executable baseline are in the linked checkpoint. Server checkout remained
`9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`; no server behavior is newly inferred.

The [VFS specification](../../docs/ASSET_PIPELINE.md) records normalized request
keys and SearchTree binary search by CRC and name at `0x00A99550`.
The [texture-binding investigation](ANALYSIS_CHECKPOINT_2026-09-12_TEXTURE_BINDING.md)
records SSHT/TXM versions and the tolerated trailing zero in TXM/0001.

**Observed correction:** the first full mount attempt rejected
`stardust_3_02_n.tre`. Independent Python found all 208 raw-name CRCs valid;
exactly one differed after normalization: row 137,
`snapshot/mustafar - Copy.ws`, stored/raw `A61E6938`, normalized `6E3A9B17`.
Archive SHA-256:
`7C3DE3E9CE837B7CC8D2FD913ACCA443B2372579FFA96AD4486F9929491D83AA`.

**Confirmed structure / strongly inferred reachability:** validate CRC against
raw index bytes, retain raw names, and normalize requests. That mixed-case row
must not become a normalized alias. `TreArchive` now accepts its valid archive
while keeping it unavailable under a normalized query. Synthetic tests retain
both raw mixed-case and canonical records, select only the canonical key, and
demonstrate that a raw-only mixed-case record is absent from lookup. Incorrect
stored CRCs still fail. No original execution was used to test this edge.

## Bounded implementation

`ConfiguredTreOverlay` owns archives and borrows an explicitly supplied
TextReader. It parses only `searchTree_00_<priority>` declarations, caps input at
65,536 characters / 256 mounts, rejects duplicate priorities, path separators,
drive prefixes and non-TRE filenames, and disposes all opened archives on failure.
Higher numeric priority wins; tombstones stop fallback. Missing archives fail
explicitly. Includes, alternate numbered sets, TOC and loose files are unsupported.

`StaticShaderReferences` accepts SSHT/0000 or /0001 and one external effect NAME;
optional TXMS contains TXM/0000, /0001 or /0002. It preserves raw binding metadata,
tag and source flag, validates lengths (5, 11 or tolerated 12-with-zero, 12),
rejects duplicate tags and preserves unnamed resources without inventing paths.
Material, sampler, coordinate-set and source-flag semantics remain unresolved
here; their bytes remain owned metadata. This is a reference slice, not a full
shader implementation.

## Results and verification

**Confirmed for one dependency graph:** mesh winner priority 33; nine distinct
SSHT/0000 shader winners at priority 38, each with one named binding. All nine
unique textures resolve to DDS envelopes; their one shared effect resolves to
EFCT. No dependency is missing/masked and no binding is unnamed. DDS pixel
decoding and EFCT pass interpretation were not tested by this envelope probe.
The geometry hash remains the independent Python-matched value.

Representative `shader/stco_tan_as8.sht`: 273 bytes, SHA-256
`7B71CD629CF58E44FFA371D92FC16009F24A3A070BAF02DEBA5F470F3D22B733`,
from `mtg_patch_009_Shader_01.tre`; references `texture/stco_tan.dds` and
`effect/a_simple.eft`. Its TXM/0001 DATA is 11 bytes, tag MAIN, source flag 0.

```powershell
python tools/python/audit_tre_filename_crc.py C:\Stardust-DEV\stardust_3_02_n.tre
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -- --mesh-overlay C:\Stardust-DEV --configuration C:\Stardust-DEV\swgemu_live.cfg --member appearance/mesh/thm_tato_imprv_building_s09_r0_mesh_l4.msh --expected-sha256 CA4F3070B8BBC9CD50DEC617AC2C4C249F3E888E15EB10E6F832113F7C6D126B
```

Core suite passes including every shader truncated prefix, owned metadata,
three TXM versions, tolerated/invalid trailing bytes, unnamed resource,
duplicate tags, mount precedence/masks/missing, invalid declarations, bounds,
idempotent disposal and exclusive reopen after failed mount cleanup.
Original assets remain private; commands print compact identities/counts only.

Next: decode `texture/stco_tan.dds` independently into RGBA, establish its format
and mip bounds, and map `effect/a_simple.eft`'s selected pass before a Godot
material adapter. Coordinate and winding conversion need a separate asymmetric
fixture; successful mounting alone does not close those gates.
