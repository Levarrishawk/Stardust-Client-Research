# Round 31: explicit terrain patch topology prerequisite

Date: 2026-10-03. Bounded question: can the nine embedded ushort patterns be
represented as owned engine-neutral definitions and safely rebased while
preserving every original active index? Stop after exact table/native oracle,
bounds/overflow/ownership rejection, and focused workspace gates. No full
terrain or GPU milestone.

## Inputs and method

Original executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
The SHA gate precedes PE loading and VA use. Tools: cached Python,
pefile/Capstone, existing range disassembler and curated selector row
`terrain_primitive_select_seam_indices_VA_00865FF0`; cached .NET 8.0.300.
Prior evidence:
[ClientChunk checkpoint](ANALYSIS_CHECKPOINT_2026-09-12_CLIENTCHUNK_MESH_BLEND.md)
and [terrain architecture](../../docs/WORLD_TERRAIN_ARCHITECTURE.md).
The latest accepted session was
[Round 30](../sessions/PARALLEL_DEVELOPMENT_ROUND_30_2026-10-03.md).

Round 31 snapshot: 2754 manifest files. Manifest SHA-256
`093a193c288ed5889c62317c57028a26f0afffa9c710b104a9c5f9bd2163b0fb`;
archive SHA-256
`d80bb36fd1d71a63ff31d2eaf28a5d9dc7c90de197b5e0abd7bd2bf29f86dc70`.
All archive member identities/sizes/hashes, paths, targets and ancestor
symlink/reparse containment were preflighted before listed-byte overlay.
Snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c` is overlay identity,
not worker HEAD. Worker HEAD/index and unlisted paths were preserved.

## Findings versus interpretation

**Confirmed:** counts at `015E7468`, patterns at `015E7490`, installer
`00865A80` with 48-byte slot stride, and batch append `008668E0` with
ushort-read/base-add/dword-write. There are 180 active indices across nine
patterns; padding is unused. Exact vectors and observation/confidence/
compatibility consequences are consolidated in the
[durable contract](../../docs/TERRAIN_PATCH_TOPOLOGY_CONTRACT.md).

The implementation uses compact independently authored active vectors,
private immutable definitions, owned copied inspection, and bounded checked
32-bit rebasing. It accepts explicit indices 0..8. The int range checks and
complete-cell requirement are declared modern policy; they do not claim
original malformed-input or overflow behavior. A failed call allocates no
returned result and changes no shared definition.

**Unclosed here:** seam-mask-to-pattern integration and original vertex coordinate
order; no compass labels or original winding inferred. Modern synthetic X/Z
fixtures check consistency only. Full PTAT/TGEN generation, chunk lifecycle,
material blending, collision planes and renderer submission are deferred.
Original selector branch evidence is preserved by reference; no registry change
or new name is needed.

## Required gates and reproduction

[Runner](../../poc/round31-tests/Rendering/Invoke-Topology.ps1) uses cleared
package sources, private APPDATA/CLI home, and private build output. It builds
only the owned helper and scenarios; it does not run the shared smoke-test entry
point or install dependencies.

```powershell
.\poc\round31-tests\Rendering\Invoke-Topology.ps1 -Exe <baseline-exe> -Packages <cached-pefile-capstone-directory> -Python <python-executable> -Dotnet <dotnet-executable>
python .\tools\python\validate_organized_workspace.py .
git diff --check
```

The default run output is ignored private material. The runner emits original
table identities, native results, and the independent final receipt. The first
attempt used inaccessible global NuGet configuration and failed before compile;
private APPDATA and cleared sources fixed it without downloads.

Passed focused native build: zero warnings/errors. Passed 362 native checks,
45 rebases (including above-ushort and largest valid signed-int base), 66
pattern/base/destination/overflow rejection cases, 60 synthetic triangle-area
checks, copied-output ownership/no alias/no mutation. The independent oracle
checks nine counts, all 180 active indices, unused padding, twelve instruction
sites across two bounded ranges, and native index order/rebases against literal
ring edges. Identity negatives use synthetic unrelated bytes and `MZ` only;
both must reject before PE loading or VA interpretation.

No original execution, Godot/GPU, external mutation/live server, download,
staging, commit, push, global registry or shared Program edits. Detailed
exploratory listings, generated fixture/native receipts and build logs remain
private. This checkpoint publishes only the compact topology specification and
independently authored implementation.

## Exact next action

At the terrain-generation boundary, recover the mapping from generated cell
samples to the nine local position indices and join chunk boundary/seam flags
to the original selector `00865FF0`. Then test compatible adjacent synthetic
cells before wiring this helper into a generator or engine adapter. Until that
join is proven, callers must supply an explicit pattern index. No additional
terrain investigation is undertaken in this round.
