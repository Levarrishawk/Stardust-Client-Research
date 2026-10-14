# Client Research Status

Updated October 14, 2026. This repository contains reverse-engineering research,
compatibility specifications, evidence reports, curated registries and analysis
tools. Client implementation and build work are outside its scope.

## How much is researched and reversed?

Approximately **89% of the observable compatibility behavior is researched**, with
a reasonable judgment range of **86–91%**. This is the latest formal weighted
assessment, dated September 25, 2026, retained without an unsupported increase.
Subsequent targeted findings add evidence but have not produced a new weighted
rescore. The estimate has medium confidence and is not a statistical interval.

There is **no defensible percentage of executable code fully reversed**. The
function inventory has not been normalized into reachable application routines,
library code, thunks and analyzer artifacts. A behavior can be understood across
many unnamed helpers; naming an address does not establish complete semantics.

| Measure | Current report | Interpretation and limit |
|---|---|---|
| Behavioral research coverage | About **89%**, judgment range **86–91%** | Weighted compatibility-contract assessment; not source-code recovery |
| Research readiness for implementation decisions | **94%** | Separate September 25 assessment of specification/decision inputs; not implementation completion |
| Stable semantic-address registry | **2,626 unique addresses**, zero duplicates | Current curated evidence count; names may have provisional semantics |
| Source packet inventory | **345/345 rows documented**, zero reachable inventory gaps | All rows in the recorded server-source matrix; not all possible messages or end-to-end wire validation |
| Executable function completion | **Unknown / uncalibrated** | No valid reachable-application denominator |
| UI workflow register | **217 entries**: 3 closed to replacement depth, 163 with evidence requiring exit tests, 48 untriaged, 3 architecture mentions only | Conservative workflow bookkeeping; not a whole-UI completion percentage |

The current address count divided by the historical 93,287 non-unwind analyzer
entries is approximately **2.81%**. This is only a raw registry/inventory ratio:
neither 2.81% nor 89% is a measured percentage of all meaningful functions reversed.
The analyzer inventory also has known omitted targets and uncertain boundaries.

Sources: [weighted assessment and arithmetic](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_EVIDENCE_BASED_COVERAGE_RESCORE.md),
[measurement policy](RESEARCH_COVERAGE_REBASELINE_2026-09-25.md),
[function registry](data/curated/provisional_names.csv),
[packet matrix](data/curated/server_packet_gap_matrix.csv),
[UI workflow register](data/curated/ui_workflow_register.csv), and
[function inventory audit](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_FUNCTION_INVENTORY_STRATIFICATION.md).

## Established research

- **Executable and architecture:** baseline identity, PE/imports, RTTI/vtables,
  startup/frame sequencing, principal scene and subsystem responsibilities.
- **Network and state:** SOE framing/reliability/transforms, login and zone
  sequencing, named/controller messages, common replication schemas, containment
  and object lifecycle. Server correlation and client evidence remain distinct.
- **Original data:** archive precedence, TRE/IFF structures, templates/tables,
  appearance, meshes, textures, shaders, skeletons, animation and localization.
- **World and runtime:** terrain rules, collision, cells/portals, camera/input,
  principal ownership boundaries, audio and representative UI/gameplay workflows.

The latest terrain investigations recovered the
[slope endpoint operation graph](docs/TERRAIN_FILTER_SLOPE_CONTRACT.md) and
[normal producer geometry and ordered cache timing](docs/TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md).
Those structural findings do not establish exact original floating-point outputs.
Recent [containment evidence](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-14_CONTAINMENT_ADMISSION.md)
separates original parent/arrangement paths from a modern cycle-rejection policy;
its source-client experiment is not proof of complete replacement compatibility.

## Remaining research priorities

1. Close reachable UI/gameplay workflows through activation, event/effect,
   failure and cleanup evidence, starting with untriaged registered entries.
2. Establish deployed-profile protocol timing, failure/reconnect and adverse-order
   fixtures. A closed packet-source inventory does not prove interoperability.
3. Resolve original numerical and visual fidelity where it matters: terrain
   arithmetic and cache/lifecycle tails, shader/material behavior and conditional
   asset paths. Preserve explicit modern-policy versus original-behavior labels.
4. Audit cancellation, stale callbacks, ownership transfer and teardown across
   representative loaders, scenes and UI managers.
5. Normalize the function inventory and recover missing callback/vtable roots
   before assigning any function-completion percentage.

Use the [workstream inventory](WORKSTREAM_INVENTORY.md) and relevant current
contract to avoid reopening already-closed questions. Default work remains static
analysis; dynamic execution and server contact require their own authorization.

## Provenance and next action

The analyzed executable baseline remains SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
This documentation update did not analyze or execute it, change registries, or
rescore coverage. Current counts were read directly from the curated CSVs.

Next bounded research question: which effective x87 controls and ownership edges
govern the documented terrain normal producer? Follow the residuals in the
[normal producer contract](docs/TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md), confirm
the executable identity, and stop at an evidence-backed closure or precise gap.

Prior development reports and status claims are preserved in the
[historical status snapshot](research/archive/ANALYSIS_STATUS_BEFORE_RESEARCH_ONLY_2026-10-14.md)
and dated checkpoints. They do not define current scope or authorization.
See the [research-only documentation policy](docs/RESEARCH_ONLY_SCOPE.md).
