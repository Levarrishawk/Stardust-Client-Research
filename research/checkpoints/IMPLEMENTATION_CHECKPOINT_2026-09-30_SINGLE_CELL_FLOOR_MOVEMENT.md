# Single-cell floor movement — 2026-09-30

## Question and stopping condition

Which original triangle-edge rules constrain a bounded single-cell horizontal movement slice? Close on a static predicate/caller contract, owned core movement, deterministic negative cases, independent installed-floor probes, and reflected host inspection. The user requested the next milestone after the completed overnight session.

## Inputs and tools

Executable SHA-256 rechecked: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 TREE mounts under `C:\Stardust-DEV`. Floor inventory SHA-256 `09CBEEDE9C6EB84EE4AE9FB2F24937D104A99C17AC9BC7D2C87B4CA6ADFA8A9E`. Original YT-1300 POB SHA-256 `8B4B59335CF34BA91DAA5CCB092AF60C9A996849DB29E77EB154FD4DF202DF4A`.

Capstone/pefile and `disasm_range.py` were used for static analysis only. .NET 8 smoke tests and Godot 4.7.2 .NET x64 executed only the owned offline prototype. Raw disassembly, generated proprietary-derived fixtures, logs and capture remain ignored/private. No original execution, live contact, commit or publication.

## Findings versus interpretation

[Durable contract](../../docs/SINGLE_CELL_FLOOR_MOVEMENT_CONTRACT.md) records exact VAs, observations, confidence and consequences. Confirmed: `0x0126A600` is a conversion/output wrapper; full traversal is below `0x01277400` / `0x012777B0`; simple classifier `0x01276670` follows neighbor edges using `0x012761F0`. Portals override flags; flags zero/one directly reject/permit; other flags invoke additional collision/contact work. Unconnected open/portal edges produce classifier results 10/7. Sequential disk fields map to expanded runtime slots through `0x012A2570` / `0x012A2850`; geometry helper `0x013EB720` uses consecutive modulo-three vertices.

Caller `0x012647B0` returns false for results 2/4/10 and true for certain owner-resolved collision outputs; this is not a general legal-movement boolean. Broader caller `0x01267380` horizontalizes contact motion, handles portal/open-boundary outcomes separately and caps recursive owner continuation at five. Full result-family names, special-edge geometry and permission enforcement remain Unknown. `FLT_MAX` at `0x015FFCA4` was verified directly, correcting the initial working assumption that it was one. No public claim was based on that mistaken assumption.

Confirmed implementation: `SingleCellFloorWalker` is independently authored from the durable edge contract and geometric fixtures. It owns topology, returns immutable progress, traverses reciprocal solid same-floor neighbors, projects contact Y, and explicitly stops at missing-neighbor portal/open exits or unsupported collision policies. Its statuses, numeric tolerance, nearly vertical guard and crossing cap are modern policies. The Godot adapter adds a two-pick line/marker inspector with mode/cell reset and reused resources.

## Validation and corrections

Full native smoke suite passes. The new cases include 500 seeded multi-triangle analytic-plane walks, malformed adjacency, blocked/open/portal/special outcomes, slope/contact policy, boundary starts, degenerate/vertical/near-vertical surfaces, owned arrays and traversal limits.

Independent raw floor bytes plus XZ segment/edge intersections pass **17,658 probes / 4,441 eligible floors**, selected from all **4,730** effective floors. Status/triangle/edge/portal/progress and contact positions agree. Counts: accepted 8,846, blocked 4,370, portal boundary 3,970, open boundary 384, unsupported edge 88. Generated fixture SHA-256 `2FFC7933774409C5886E6AF26AD34B965F7640C72D74E945DCFCE2DB9510D85C`.

Initial validation corrected a synthetic portal-parameter assertion, independent fixture contact-height rounding and vertical-neighbor selection. Position checks exposed an authored face with horizontal-normal ratio approximately `6.5051e-6`; float XZ rounding changed inferred height by 0.5 units. The bounded native policy now rejects nearly vertical height fields explicitly, and fixtures choose supported surfaces. No original slope limit is inferred. The final corpus denominator still covers 4,441 eligible floors; no claim is made for excluded surfaces or every path within each floor.

Godot headless and hidden GPU runs pass seven nonzero reflected cell walks, two-pick control/reset, existing seven surface-to-route controls, eight cell selectors and graph readback. Build has zero warnings/errors. GPU capture was visually inspected: named cell, floor/route rendering and movement-mode control are readable. The existing certificate-store startup error remains nonfatal; it is not movement verification. User mouse interaction was not manually performed; the same selection/control callbacks were exercised programmatically.

Workspace validator passes 765 Markdown files with zero broken/private links, 2,590 unique address names and 345 packet rows. `git diff --check` passes. Existing unrelated modifications remain intact and staging is empty. The capture launcher correctly refused to overwrite an earlier capture; the final GPU gate used a fresh filename.

## Reproduction

From repository root, using the local Python runtime and an ignored output directory:

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
python tools/python/verify_floor_mesh.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg PRIVATE_FLOOR_OUTPUT --core-dll CORE_SMOKE_DLL
python tools/python/verify_floor_walks.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg FLOOR_INVENTORY_JSON PRIVATE_WALK_OUTPUT CORE_SMOKE_DLL
dotnet build --no-restore poc/Stardust.Godot
```

`FLOOR_INVENTORY_JSON` is the producer's `floors.json`; output directories must resolve under the ignored private workspace directory. The verifier pins generated fixtures and every source member before native evaluation. Smoke DLL is `poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll`.

Run `Invoke-StardustAssetPreview.ps1` with the pinned YT-1300 identity and `-Interior -InteriorFloors -InteriorPaths -FloorOnly -Cell 1`; add `-Headless` for headless gates or `-CapturePath` under ignored private output for a hidden GPU capture. Interactive inspection uses the existing `-Interactive` option and the **Walk between picks** control.

## Remaining uncertainty and exact next action

Recover `0x012792B0` / `0x01275FE0` special-edge continuation, then `0x0126AA80` candidate owner selection and entry permission around `CellObject+0x29C`. Stop on one evidence-backed paired-cell transfer contract with contact-height/transform/remaining-displacement and rejection fixtures. This milestone does not establish avatar locomotion, body collision, original enum equivalence, network movement or cross-cell permission. Research scores and canonical registry counts remain unchanged.
