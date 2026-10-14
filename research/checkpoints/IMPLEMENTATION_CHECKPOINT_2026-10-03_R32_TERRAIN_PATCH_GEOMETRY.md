# Round 32: owned terrain patch geometry and separate prototype host proof

Date: 2026-10-03. Milestone: implement only directly supported supplied-cell
semantics using accepted topology, then prove a bounded owned adapter with
synthetic heights. Stop after static/native/headless/hidden-GPU and scope gates.

## Inputs and orientation

Read repository instructions, START/latest
[accepted Round 31](../sessions/PARALLEL_DEVELOPMENT_ROUND_31_2026-10-03.md),
current status/ledger/unknown terminology, topology contract, terrain
vertex/blend checkpoint, curated semantic rows and existing historical implementation path (omitted)
boundary tools.

Executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Cached Python/pefile/Capstone, .NET 8.0.300 and prototype host 4.7.2 .NET only;
no downloads. The static verifier checks identity before PE loading/VA use.

Round 32 frozen overlay has 2786 manifest files. Manifest SHA-256
`07d6abdbdab081bad0d3fb3c67a4724c6cd2865ac5411e04e9dc6b2821d9dd04`;
ZIP SHA-256
`e98dc53bba40cc13dda5298b5c713a51d870bfc3ee43944239a6d62e5e627487`.
All member paths, uniqueness, sizes, hashes, resolved targets and ancestor
symlink/reparse containment were preflighted before writes and verified after
overlay. The pinned manifest reports source HEAD
`5493338d9cd4255cf58f25de6c4cd15f6507b818`; the assignment's earlier main
HEAD was `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`. The archive/manifest
hashes match the assignment. Worker HEAD
`65de8c88141071744dc75594be126b3bab3696a5` and empty index are preserved.

## Findings and implementation consequences

The [durable contract](../../docs/TERRAIN_PATCH_GEOMETRY_CONTRACT.md) records
exact observation, confidence and consequences. Confirmed semantic anchors:
builder `00865DA0`, batch writer `0084E270`, nine base UV pairs at
`015E69D0`, four supported auxiliary sets at `015E7640`,
lookup `00866DB0`, and ARGB pack `00ABA1C0`. Auxiliary addressing is
`(selector*9+vertex)*8`; only base UV is scaled.

The core owns copied vectors, raw uint colors, generated UV channels and
selected exact topology. It preserves raw CPU normal/color bits and publishes
only owned copies. Fixed sizes, finite caps and normal eligibility are modern
policies. It does not evaluate terrain or infer sample coordinate/seam mapping.

The adapter reflects Z, preserves index order, explicitly normalizes engine
normals, uploads RGBA/base UV/one auxiliary UV, and rejects unsupported channels,
large inspection scale, detached parents and incoherent/degenerate winding
before allocation. Owner-thread resource teardown is explicit and idempotent.
The synthetic diagnostic shader does not reproduce original materials or ALP
sampling. No shared Main/project/launchers or global registries were edited.

## Reproduction and required gates

Owned runner (external historical reference):

> Historical prototype-specific command or code example omitted from the research-only documentation.


Both invocations use the same private RunRoot (default is a task-specific
ignored directory). Build/native outputs, copied offline packages and APPDATA
are isolated; prototype host's own proof-project cache is ignored. Existing packages are
copied rather than mutated. Sources are cleared/offline only. The SDK resolver
initially could not resolve the empty cache; supplying existing cached packages
resolved it without a download.

Passed: 30 exact instruction sites in four ranges, nine base UV pairs, four
auxiliary vectors; 27 independently authored native cases, 158 checks and 103
rejections. Independent expected outputs cover every position/normal/color
word, RGBA float bits, scale/base/auxiliary UV bits, selector/index order and
bounds. Wrong identity/truncated synthetic input rejects before PE/VA.

Passed focused native and prototype host builds, zero warnings/errors. Headless and
hidden GPU proof verifies nine patterns, four selectors, 81 atlas vertices plus
nine varied-ARGB vertices and six boundary/owner rejections. GPU tests additionally
read four front UV/color samples and four culled back samples. Node/resource
allocations and disposals balance; no leaked-object diagnostic remains.
An initial test-only detached Node leak was fixed with explicit Free and
rerun. A single known certificate-store diagnostic occurs before state evidence
and is allowlisted; all other engine warnings/errors fail the runner.

All heights/positions in the proof are synthetic supplied fixtures. The exact
topology/UV vectors are compact compatibility facts; original assets, bulk
static listings, images and detailed receipts are private. No original client
execution, live contact, packet capture, staging, commit, push or download.

## Precise next action

Recover generated terrain cell sample-to-vertex ordering and join actual chunk
seam flags to selector `00865FF0`. Then establish a terrain material descriptor
carrying original level/normal/ALP resource identities and coordinate channel
count. A future adapter must either support all required auxiliary channels or
reject them explicitly before publication. Do not infer original terrain
appearance from this supplied synthetic geometry proof.
