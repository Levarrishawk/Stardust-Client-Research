# Round 33: exact terrain cell sample assembly and seam selector

Date: 2026-10-03. Bounded question: what sample permutation and integer
boundary/mask rule feeds the nine-vertex primitive, and can a small owned
engine-neutral helper preserve it? Stop after direct/static/native and hygiene
gates. No Godot/GPU or unchanged-adapter rerun.

## Inputs and orientation

Read AGENTS, START/latest [accepted Round32](../sessions/PARALLEL_DEVELOPMENT_ROUND_32_2026-10-03.md),
status/ledger/unknown terminology, accepted topology/geometry contracts,
mesh/blend and [seam scheduling evidence](ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_WORK_SCHEDULING_AND_SEAMS.md),
and existing static tools/curated builder, constructor and selector rows.

Executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Cached Python/pefile/Capstone and .NET 8.0.300 only; no downloads or original
executable execution.

Frozen public snapshot: 2831 files, manifest SHA-256
`173d7bf83c221bbea68938d959119dbb0149101e70f797c704a74dbec788b375`,
ZIP SHA-256
`668b57ff1696870da83fd24cb93cc01a84bbb2e675a2662214120a29c00da28f`,
source HEAD `5493338d9cd4255cf58f25de6c4cd15f6507b818`.
Every member/path/casefold uniqueness/size/hash, resolved target and all
ancestor/target symlink/reparse containment was preflighted before overlay.
All listed bytes were verified afterward. Worker HEAD/index and unlisted/private
paths were preserved.

## Findings and confidence

**Confirmed:** nine offset/UV records at `015E71C8`, origin
`sampleBorder + 2*cellCoordinate`, row-stride gathering in `008618D0`,
cell-coordinate storage in `00865DA0`, and seam selection `00865FF0`.
The [durable contract](../../docs/TERRAIN_PATCH_ASSEMBLY_CONTRACT.md)
contains exact table, VAs, bit/boundary mapping, observations and implementation
consequences. No canonical name registry changes are needed.

Exact low bits are row-max1, column-min2, row-min4, column-max8. Original branch
priority is preserved even for count1 overlapping boundaries. The selector
replaces indices/triangle planes without changing vertices. Compass orientation
and original reachability of all synthetic mask/count combinations are not
asserted.

The owned assembly result holds accepted immutable geometry and copied selected
sample indices. The API requires resolved common-stride grids; original
independent channel strides, RGB conversion/baked lighting and material
selection remain outside it. Full-grid extent checks, finite/cap eligibility and
optional coherent-winding enforcement are explicit modern policies. Only
selected samples are read; unused data is not scanned or retained.

## Reproduction and gates

[Runner](../../poc/round33-tests/Rendering/Invoke-Assembly.ps1):

```powershell
.\poc\round33-tests\Rendering\Invoke-Assembly.ps1 -Exe <baseline-exe> -Packages <cached-pefile-capstone> -Python <cached-python> -Dotnet <dotnet-exe>
python .\tools\python\validate_organized_workspace.py .
git diff --check
```

The runner clears package sources and uses task-private APPDATA, CLI home and
build outputs. No external packages are needed by its focused source-linked
project. Detailed static listings, generated synthetic fixtures and receipts
remain private.

Passed 27 instruction sites in five ranges and all nine table records.
A bounded equality/test branch interpreter over decoded static instructions
matches independent literal boundary rules for synthetic inputs; no machine
code/client process is executed. Passed exact native output for 480 assemblies
and 288 larger-count selector cases: 5306 checks, 46 rejection cases, selected
sample/vertex/normal/raw ARGB/UV/index order, winding, aliases, and retained
earlier result after later failures. Count1 overlap and interior masks are covered.

Rejections include unsupported mask bits, count/coordinate/dimension/border
ranges, exact grid lengths, multiplication/origin overflow, sample caps,
selected nonfinite/over-cap data, normal eligibility, degenerate/incoherent
winding, UV scale and auxiliary selector limits. Exact sample cap and explicit
winding bypass are tested; unused invalid sample acceptance is deliberate.
Wrong identity and truncated synthetic input must reject before PE/VA.

Focused native build passes with zero warnings/errors. No Godot/GPU, live
contact, source publication, staging/commit/push or shared/existing production
changes occur in this milestone.

## Exact next action

Join accepted PTAT/TGEN rule evaluation to generated position/normal and
resolved-color grids with explicit dimensions/strides/border. Preserve original
color conversion and baked-lighting policy separately from assembly. Then map
quadtree neighbor flags into the confirmed four-bit selector inputs before any
world/render integration. The helper now closes local sample/seam selection,
not original terrain generation or material/appearance parity.
